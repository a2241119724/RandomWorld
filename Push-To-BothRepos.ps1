[CmdletBinding()]
param(
    [string]$Branch,
    [switch]$SkipPublic,
    [switch]$SkipPrivate
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [Text.Encoding]::UTF8

function Get-GitRoot {
    $gitRoot = (& git rev-parse --show-toplevel 2>$null)
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($gitRoot)) {
        throw "This script must run inside a Git repository."
    }
    return (Resolve-Path -LiteralPath $gitRoot.Trim()).Path
}

$repoRoot = Get-GitRoot
Push-Location $repoRoot

$publicRemote = "public"
$privateRemote = "origin"
$publicUrl = "git@github.com:a2241119724/RandomWorld.git"
$privateUrl = "git@github.com:a2241119724/Private-RandomWorld.git"

$currentBranch = (& git rev-parse --abbrev-ref HEAD 2>$null).Trim()
if ([string]::IsNullOrWhiteSpace($Branch)) {
    $Branch = $currentBranch
}

# --- Ensure remotes ---
$remoteLines = (& git remote -v 2>$null) -join "`n"

if ($remoteLines -notmatch [regex]::Escape($publicUrl)) {
    $err = & git remote add $publicRemote $publicUrl 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to add remote '${publicRemote}': $err" -ForegroundColor Red
        throw "Cannot configure public remote."
    }
    Write-Host "Added remote: ${publicRemote} -> ${publicUrl}" -ForegroundColor Green
}

if ($remoteLines -notmatch [regex]::Escape($privateUrl)) {
    $err = & git remote add $privateRemote $privateUrl 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to add remote '${privateRemote}': $err" -ForegroundColor Red
        throw "Cannot configure private remote."
    }
    Write-Host "Added remote: ${privateRemote} -> ${privateUrl}" -ForegroundColor Green
}

# --- Check clean working directory ---
$dirty = & git status --porcelain 2>$null
if ($dirty) {
    throw "Working directory has uncommitted changes. Commit or stash first."
}

$origCommit = (& git rev-parse HEAD 2>$null).Trim()

$restrictiveIgnore = @"
# Public source snapshot -- only publish code, not assets or resources.
*

!*/
!/.gitignore
!/README.md
!/Push-To-BothRepos.ps1

/Agent
!/AgentFull/**
**/__pycache__/
**/__pycache__/**
*.pyc
*.pyo

!/Scripts/2D/*.cs
!/Scripts/2D/**/*.cs

/AddressableAssetsData/
/AddressableAssetsData.meta
/TextMesh Pro/
/TextMesh Pro.meta
/Scripts/Reference/
/Scripts/Reference.meta

/.claude/
/.vs/
/Library/
/Logs/
/Temp/
/Obj/
/obj/
/MemoryCaptures/
/UserSettings/
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
*.apk
*.aab
*.unitypackage
*.app
*.exe
*.log
hs_err_pid*.log
"@

try {
    # ============================================================
    # PUSH TO PRIVATE REPO (current state: minimal .gitignore)
    # ============================================================
    if (-not $SkipPrivate) {
        Write-Host "`n=== Pushing to PRIVATE repo (${privateRemote}) [${Branch}] ===" -ForegroundColor Cyan
        & git push $privateRemote "${Branch}" --follow-tags
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to push to private repo (${privateRemote})."
        }
        Write-Host "Private push complete." -ForegroundColor Green
    }

    # ============================================================
    # PUSH TO PUBLIC REPO (filtered snapshot)
    # ============================================================
    if (-not $SkipPublic) {
        Write-Host "`n=== Pushing to PUBLIC repo (${publicRemote}) [${Branch}] ===" -ForegroundColor Cyan

        Write-Host "Creating filtered snapshot..."

        $restrictiveIgnore | Set-Content .gitignore -Encoding UTF8
        & git add .gitignore

        $ignoredFiles = & git -c core.quotepath=false ls-files --cached --ignored --exclude-standard 2>$null
        if ($LASTEXITCODE -eq 0 -and $ignoredFiles) {
            foreach ($f in $ignoredFiles) {
                $file = $f.Trim()
                if ($file) {
                    & git rm --cached --quiet -- $file 2>$null
                }
            }
        }

        & git commit -m "Public snapshot (filtered)" --allow-empty
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to create public commit."
        }

        & git push $publicRemote "${Branch}" --force --follow-tags
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to push to public repo (${publicRemote})."
        }

        Write-Host "Public push complete." -ForegroundColor Green
    }
}
finally {
    Write-Host "Restoring working directory..."
    & git reset --hard $origCommit 2>$null

    Pop-Location -ErrorAction SilentlyContinue
}

Write-Host "`nDone - pushed to both repos [${Branch}]." -ForegroundColor Green
