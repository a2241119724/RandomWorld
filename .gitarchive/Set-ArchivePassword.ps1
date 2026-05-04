[CmdletBinding()]
param(
    [string]$Root
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-GitRoot {
    param([string]$Candidate)

    if (-not [string]::IsNullOrWhiteSpace($Candidate)) {
        return (Resolve-Path -LiteralPath $Candidate).Path
    }

    $gitRoot = (& git rev-parse --show-toplevel 2>$null)
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($gitRoot)) {
        throw 'This script must run inside a Git repository.'
    }

    return (Resolve-Path -LiteralPath $gitRoot.Trim()).Path
}

function ConvertTo-PlainText {
    param([System.Security.SecureString]$SecureString)

    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureString)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

$repoRoot = Get-GitRoot -Candidate $Root
$passwordFile = Join-Path $repoRoot '.git\commit-archive-password.securestring'

$firstPassword = Read-Host 'Archive password' -AsSecureString
$secondPassword = Read-Host 'Confirm archive password' -AsSecureString

$firstPlain = ConvertTo-PlainText $firstPassword
$secondPlain = ConvertTo-PlainText $secondPassword

if ([string]::IsNullOrEmpty($firstPlain)) {
    throw 'Archive password cannot be empty.'
}

if ($firstPlain -ne $secondPlain) {
    throw 'Passwords did not match.'
}

$firstPassword | ConvertFrom-SecureString | Set-Content -LiteralPath $passwordFile -Encoding ASCII
& git -C $repoRoot config core.hooksPath .githooks

Write-Host "Password saved locally: $passwordFile"
Write-Host 'Git hook path configured: .githooks'
