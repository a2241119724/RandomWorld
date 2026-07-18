[CmdletBinding()]
param(
    [string]$Branch,
    [string]$CommitMessage = "Full sync: all resources (auto-generated)",
    [switch]$SkipPublic,
    [switch]$SkipPrivate
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-GitRoot {
    $gitRoot = (& git rev-parse --show-toplevel 2>$null)
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($gitRoot)) {
        throw "This script must run inside a Git repository."
    }
    return (Resolve-Path -LiteralPath $gitRoot.Trim()).Path
}

$repoRoot = Get-GitRoot

$publicRemote = "public"
$privateRemote = "origin"
$publicUrl = "git@github.com:a2241119724/RandomWorld.git"
$privateUrl = "git@github.com:a2241119724/Private-RandomWorld.git"

$currentBranch = (& git -C $repoRoot rev-parse --abbrev-ref HEAD 2>$null).Trim()
if ([string]::IsNullOrWhiteSpace($Branch)) {
    $Branch = $currentBranch
}

# --- Ensure remotes are configured ---
$remoteLines = (& git -C $repoRoot remote -v 2>$null) -join "`n"

if ($remoteLines -notmatch [regex]::Escape($publicUrl)) {
    $addError = & git -C $repoRoot remote add $publicRemote $publicUrl 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to add remote '$publicRemote': $addError" -ForegroundColor Red
        throw "Cannot configure public remote. Does the repo exist on GitHub?"
    }
    Write-Host "Added remote: $publicRemote -> $publicUrl" -ForegroundColor Green
}

if ($remoteLines -notmatch [regex]::Escape($privateUrl)) {
    $addError = & git -C $repoRoot remote add $privateRemote $privateUrl 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to add remote '$privateRemote': $addError" -ForegroundColor Red
        throw "Cannot configure private remote. Does the repo exist on GitHub?"
    }
    Write-Host "Added remote: $privateRemote -> $privateUrl" -ForegroundColor Green
}

# ============================================================
# PUSH TO PUBLIC REPO (uses restrictive .gitignore)
# ============================================================
if (-not $SkipPublic) {
    Write-Host "`n=== Pushing to PUBLIC repo ($publicRemote) [$Branch] ===" -ForegroundColor Cyan
    & git -C $repoRoot push $publicRemote "${Branch}" --follow-tags
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to push to public repo ($publicRemote)."
    }
    Write-Host "Public push complete." -ForegroundColor Green
}

# ============================================================
# PUSH TO PRIVATE REPO (uploads everything)
# ============================================================
if (-not $SkipPrivate) {
    Write-Host "`n=== Pushing to PRIVATE repo ($privateRemote) [$Branch] ===" -ForegroundColor Cyan

    $tempDir = Join-Path ([IO.Path]::GetTempPath()) ("rw-private-push-" + [Guid]::NewGuid().ToString("N"))

    try {
        Write-Host "Creating temporary working copy..."
        & git -C $repoRoot clone --local --branch $Branch . $tempDir 2>$null
        if ($LASTEXITCODE -ne 0) {
            & git -C $repoRoot clone --local . $tempDir
            if ($LASTEXITCODE -ne 0) {
                throw "Failed to create temporary clone."
            }
            Push-Location $tempDir
            & git checkout $Branch 2>$null
        }
        else {
            Push-Location $tempDir
        }

        Write-Host "Replacing .gitignore for full upload..."
        @"
/Library/
/Temp/
/Logs/
/Obj/
/obj/
/.vs/
/UserSettings/
/MemoryCaptures/
*.csproj
*.sln
*.suo
*.user
*.pidb
*.booproj
*.svd
*.pdb
*.mdb
*.opendb
*.VC.db
*.pyc
*.pyo
*.log
hs_err_pid*.log
"@ | Set-Content -LiteralPath ".gitignore" -Encoding UTF8

        Write-Host "Staging all files..."
        & git add -A
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to stage files."
        }

        Write-Host "Creating commit..."
        & git commit -m $CommitMessage --allow-empty
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to create commit."
        }

        Write-Host "Pushing to private remote..."
        & git push $privateRemote $Branch --force --follow-tags
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to push to private repo ($privateRemote)."
        }

        Write-Host "Private push complete." -ForegroundColor Green
    }
    finally {
        Pop-Location -ErrorAction SilentlyContinue
        if (Test-Path -LiteralPath $tempDir) {
            Write-Host "Cleaning up temporary directory..."
            Remove-Item -Recurse -Force -LiteralPath $tempDir -ErrorAction SilentlyContinue
        }
    }
}

Write-Host "`nDone - pushed to both repos [$Branch]." -ForegroundColor Green
