[CmdletBinding()]
param(
    [string]$Root,
    [string]$OutputPath,
    [string]$Password,
    [int]$Iterations = 250000
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

function Get-ArchivePassword {
    param(
        [string]$RepoRoot,
        [string]$ProvidedPassword
    )

    if (-not [string]::IsNullOrEmpty($ProvidedPassword)) {
        return $ProvidedPassword
    }

    if (-not [string]::IsNullOrEmpty($env:GIT_ARCHIVE_PASSWORD)) {
        return $env:GIT_ARCHIVE_PASSWORD
    }

    $passwordFile = Join-Path $RepoRoot '.git\commit-archive-password.securestring'
    if (Test-Path -LiteralPath $passwordFile) {
        $secureText = (Get-Content -LiteralPath $passwordFile -Raw).Trim()
        $securePassword = ConvertTo-SecureString $secureText
        return ConvertTo-PlainText $securePassword
    }

    $promptedPassword = Read-Host 'Archive password' -AsSecureString
    return ConvertTo-PlainText $promptedPassword
}

function New-RandomBytes {
    param([int]$Length)

    $bytes = New-Object byte[] $Length
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $rng.GetBytes($bytes)
    }
    finally {
        $rng.Dispose()
    }

    return $bytes
}

function Get-KeyMaterial {
    param(
        [string]$PlainPassword,
        [byte[]]$Salt,
        [int]$RoundCount
    )

    $kdf = [System.Security.Cryptography.Rfc2898DeriveBytes]::new(
        $PlainPassword,
        $Salt,
        $RoundCount,
        [System.Security.Cryptography.HashAlgorithmName]::SHA256
    )

    try {
        return $kdf.GetBytes(64)
    }
    finally {
        $kdf.Dispose()
    }
}

function Protect-ZipFile {
    param(
        [string]$SourceZip,
        [string]$DestinationPath,
        [string]$PlainPassword,
        [int]$RoundCount
    )

    $magic = [Text.Encoding]::ASCII.GetBytes('RWENCZIP1')
    $salt = New-RandomBytes 16
    $iv = New-RandomBytes 16
    $keyMaterial = Get-KeyMaterial -PlainPassword $PlainPassword -Salt $salt -RoundCount $RoundCount
    $aesKey = New-Object byte[] 32
    $hmacKey = New-Object byte[] 32
    [Array]::Copy($keyMaterial, 0, $aesKey, 0, 32)
    [Array]::Copy($keyMaterial, 32, $hmacKey, 0, 32)

    $iterationBytes = [BitConverter]::GetBytes([int]$RoundCount)
    if (-not [BitConverter]::IsLittleEndian) {
        [Array]::Reverse($iterationBytes)
    }

    $header = New-Object byte[] ($magic.Length + $iterationBytes.Length + $salt.Length + $iv.Length)
    $offset = 0
    [Array]::Copy($magic, 0, $header, $offset, $magic.Length)
    $offset += $magic.Length
    [Array]::Copy($iterationBytes, 0, $header, $offset, $iterationBytes.Length)
    $offset += $iterationBytes.Length
    [Array]::Copy($salt, 0, $header, $offset, $salt.Length)
    $offset += $salt.Length
    [Array]::Copy($iv, 0, $header, $offset, $iv.Length)

    $destinationDirectory = Split-Path -Parent $DestinationPath
    if (-not [string]::IsNullOrWhiteSpace($destinationDirectory)) {
        New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
    }

    $temporaryOutput = "$DestinationPath.tmp"
    if (Test-Path -LiteralPath $temporaryOutput) {
        Remove-Item -LiteralPath $temporaryOutput -Force
    }

    $input = $null
    $output = $null
    $aes = $null
    $crypto = $null

    try {
        $input = [IO.File]::OpenRead($SourceZip)
        $output = [IO.File]::Open($temporaryOutput, [IO.FileMode]::Create, [IO.FileAccess]::Write, [IO.FileShare]::None)
        $output.Write($header, 0, $header.Length)

        $aes = [System.Security.Cryptography.Aes]::Create()
        $aes.KeySize = 256
        $aes.Mode = [System.Security.Cryptography.CipherMode]::CBC
        $aes.Padding = [System.Security.Cryptography.PaddingMode]::PKCS7
        $aes.Key = $aesKey
        $aes.IV = $iv

        $encryptor = $aes.CreateEncryptor()
        $crypto = [System.Security.Cryptography.CryptoStream]::new(
            $output,
            $encryptor,
            [System.Security.Cryptography.CryptoStreamMode]::Write
        )

        $buffer = New-Object byte[] 1048576
        while (($read = $input.Read($buffer, 0, $buffer.Length)) -gt 0) {
            $crypto.Write($buffer, 0, $read)
        }
        $crypto.FlushFinalBlock()
    }
    finally {
        if ($crypto) { $crypto.Dispose() }
        if ($aes) { $aes.Dispose() }
        if ($input) { $input.Dispose() }
        if ($output) { $output.Dispose() }
    }

    $hmac = [System.Security.Cryptography.HMACSHA256]::new($hmacKey)
    $hmacInput = $null
    try {
        $hmacInput = [IO.File]::OpenRead($temporaryOutput)
        $tag = $hmac.ComputeHash($hmacInput)
    }
    finally {
        if ($hmacInput) { $hmacInput.Dispose() }
        $hmac.Dispose()
    }

    $append = $null
    try {
        $append = [IO.File]::Open($temporaryOutput, [IO.FileMode]::Append, [IO.FileAccess]::Write, [IO.FileShare]::None)
        $append.Write($tag, 0, $tag.Length)
    }
    finally {
        if ($append) { $append.Dispose() }
    }

    Move-Item -LiteralPath $temporaryOutput -Destination $DestinationPath -Force
}

function Test-ExcludedPath {
    param(
        [string]$RelativePath,
        [string]$OutputFullPath,
        [string]$FileFullPath
    )

    if ([StringComparer]::OrdinalIgnoreCase.Equals($OutputFullPath, $FileFullPath)) {
        return $true
    }

    $normalized = ($RelativePath -replace '\\', '/').TrimStart('/')
    if ([string]::IsNullOrWhiteSpace($normalized)) {
        return $true
    }

    $parts = $normalized.Split('/')
    $topLevelExcludes = @(
        '.git',
        '.vs',
        '.claude',
        '.commit-archive',
        'Library',
        'Logs',
        'Temp',
        'Obj',
        'obj',
        'MemoryCaptures',
        'UserSettings'
    )

    if ($topLevelExcludes -contains $parts[0]) {
        return $true
    }

    foreach ($part in $parts) {
        if ($part -eq '__pycache__') {
            return $true
        }
    }

    $fileName = [IO.Path]::GetFileName($normalized)
    $fileExcludes = @(
        '*.csproj',
        '*.sln',
        '*.suo',
        '*.user',
        '*.pidb',
        '*.booproj',
        '*.svd',
        '*.pdb',
        '*.mdb',
        '*.opendb',
        '*.VC.db',
        '*.pyc',
        '*.pyo',
        '*.log',
        'hs_err_pid*.log'
    )

    foreach ($pattern in $fileExcludes) {
        if ($fileName -like $pattern) {
            return $true
        }
    }

    return $false
}

$repoRoot = Get-GitRoot -Candidate $Root
$repoRootFull = [IO.Path]::GetFullPath($repoRoot).TrimEnd('\', '/')

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $repoRootFull '.commit-archive\ProjectSnapshot.zip.aes'
}

$outputFullPath = [IO.Path]::GetFullPath($OutputPath)
$plainPassword = Get-ArchivePassword -RepoRoot $repoRootFull -ProvidedPassword $Password
if ([string]::IsNullOrEmpty($plainPassword)) {
    throw 'Archive password cannot be empty.'
}

$temporaryRoot = Join-Path ([IO.Path]::GetTempPath()) ('rw-commit-archive-' + [Guid]::NewGuid().ToString('N'))
$payloadRoot = Join-Path $temporaryRoot 'payload'
$zipPath = Join-Path $temporaryRoot 'ProjectSnapshot.zip'

New-Item -ItemType Directory -Path $payloadRoot -Force | Out-Null

try {
    $fileCount = 0
    $totalBytes = [Int64]0

    Get-ChildItem -LiteralPath $repoRootFull -Force -Recurse -File | ForEach-Object {
        $fileFullPath = [IO.Path]::GetFullPath($_.FullName)
        $relativePath = $fileFullPath.Substring($repoRootFull.Length).TrimStart('\', '/')

        if (-not (Test-ExcludedPath -RelativePath $relativePath -OutputFullPath $outputFullPath -FileFullPath $fileFullPath)) {
            $targetPath = Join-Path $payloadRoot $relativePath
            $targetDirectory = Split-Path -Parent $targetPath
            New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
            Copy-Item -LiteralPath $fileFullPath -Destination $targetPath -Force
            $script:fileCount += 1
            $script:totalBytes += $_.Length
        }
    }

    if ($fileCount -eq 0) {
        throw 'No files were selected for the archive.'
    }

    $payloadItems = Get-ChildItem -LiteralPath $payloadRoot -Force
    Compress-Archive -LiteralPath ($payloadItems | ForEach-Object { $_.FullName }) -DestinationPath $zipPath -CompressionLevel Optimal -Force
    Protect-ZipFile -SourceZip $zipPath -DestinationPath $outputFullPath -PlainPassword $plainPassword -RoundCount $Iterations

    $sizeMb = [Math]::Round($totalBytes / 1MB, 2)
    Write-Host "Encrypted project archive created: $outputFullPath ($fileCount files, $sizeMb MB source)"
}
finally {
    if (Test-Path -LiteralPath $temporaryRoot) {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
