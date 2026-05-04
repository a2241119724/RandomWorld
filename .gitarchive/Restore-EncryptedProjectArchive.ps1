[CmdletBinding()]
param(
    [string]$InputPath,
    [string]$OutputZip,
    [string]$Password,
    [string]$ExtractTo,
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-GitRoot {
    $gitRoot = (& git rev-parse --show-toplevel 2>$null)
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($gitRoot)) {
        return (Get-Location).Path
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

function Get-Password {
    param([string]$ProvidedPassword)

    if (-not [string]::IsNullOrEmpty($ProvidedPassword)) {
        return $ProvidedPassword
    }

    if (-not [string]::IsNullOrEmpty($env:GIT_ARCHIVE_PASSWORD)) {
        return $env:GIT_ARCHIVE_PASSWORD
    }

    $securePassword = Read-Host 'Archive password' -AsSecureString
    return ConvertTo-PlainText $securePassword
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

function Test-BytesEqual {
    param(
        [byte[]]$Left,
        [byte[]]$Right
    )

    if ($Left.Length -ne $Right.Length) {
        return $false
    }

    $diff = 0
    for ($index = 0; $index -lt $Left.Length; $index += 1) {
        $diff = $diff -bor ($Left[$index] -bxor $Right[$index])
    }

    return $diff -eq 0
}

function Get-LimitedHmac {
    param(
        [string]$Path,
        [byte[]]$Key,
        [Int64]$ByteCount
    )

    $hmac = [System.Security.Cryptography.HMACSHA256]::new($Key)
    $stream = $null
    try {
        $stream = [IO.File]::OpenRead($Path)
        $buffer = New-Object byte[] 1048576
        $remaining = $ByteCount
        while ($remaining -gt 0) {
            $requested = [Math]::Min($buffer.Length, $remaining)
            $read = $stream.Read($buffer, 0, [int]$requested)
            if ($read -le 0) {
                throw 'Unexpected end of encrypted archive while verifying.'
            }

            [void]$hmac.TransformBlock($buffer, 0, $read, $null, 0)
            $remaining -= $read
        }

        [void]$hmac.TransformFinalBlock((New-Object byte[] 0), 0, 0)
        return $hmac.Hash
    }
    finally {
        if ($stream) { $stream.Dispose() }
        $hmac.Dispose()
    }
}

function Unprotect-ZipFile {
    param(
        [string]$SourcePath,
        [string]$DestinationZip,
        [string]$PlainPassword
    )

    $magic = [Text.Encoding]::ASCII.GetBytes('RWENCZIP1')
    $saltLength = 16
    $ivLength = 16
    $tagLength = 32
    $headerLength = $magic.Length + 4 + $saltLength + $ivLength

    $sourceInfo = Get-Item -LiteralPath $SourcePath
    if ($sourceInfo.Length -le ($headerLength + $tagLength)) {
        throw 'Encrypted archive is too small or invalid.'
    }

    $header = New-Object byte[] $headerLength
    $storedTag = New-Object byte[] $tagLength
    $source = $null
    try {
        $source = [IO.File]::OpenRead($SourcePath)
        if ($source.Read($header, 0, $header.Length) -ne $header.Length) {
            throw 'Could not read encrypted archive header.'
        }

        $source.Seek(-$tagLength, [IO.SeekOrigin]::End) | Out-Null
        if ($source.Read($storedTag, 0, $storedTag.Length) -ne $storedTag.Length) {
            throw 'Could not read encrypted archive authentication tag.'
        }
    }
    finally {
        if ($source) { $source.Dispose() }
    }

    for ($index = 0; $index -lt $magic.Length; $index += 1) {
        if ($header[$index] -ne $magic[$index]) {
            throw 'Encrypted archive format is not supported by this script.'
        }
    }

    $iterationBytes = New-Object byte[] 4
    [Array]::Copy($header, $magic.Length, $iterationBytes, 0, 4)
    if (-not [BitConverter]::IsLittleEndian) {
        [Array]::Reverse($iterationBytes)
    }
    $roundCount = [BitConverter]::ToInt32($iterationBytes, 0)

    $salt = New-Object byte[] $saltLength
    $iv = New-Object byte[] $ivLength
    [Array]::Copy($header, $magic.Length + 4, $salt, 0, $saltLength)
    [Array]::Copy($header, $magic.Length + 4 + $saltLength, $iv, 0, $ivLength)

    $keyMaterial = Get-KeyMaterial -PlainPassword $PlainPassword -Salt $salt -RoundCount $roundCount
    $aesKey = New-Object byte[] 32
    $hmacKey = New-Object byte[] 32
    [Array]::Copy($keyMaterial, 0, $aesKey, 0, 32)
    [Array]::Copy($keyMaterial, 32, $hmacKey, 0, 32)

    $authenticatedLength = $sourceInfo.Length - $tagLength
    $computedTag = Get-LimitedHmac -Path $SourcePath -Key $hmacKey -ByteCount $authenticatedLength
    if (-not (Test-BytesEqual -Left $computedTag -Right $storedTag)) {
        throw 'Password is wrong, or the encrypted archive has been changed.'
    }

    $destinationDirectory = Split-Path -Parent $DestinationZip
    if (-not [string]::IsNullOrWhiteSpace($destinationDirectory)) {
        New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
    }

    $input = $null
    $output = $null
    $aes = $null
    $crypto = $null
    try {
        $input = [IO.File]::OpenRead($SourcePath)
        $input.Seek($headerLength, [IO.SeekOrigin]::Begin) | Out-Null
        $output = [IO.File]::Open($DestinationZip, [IO.FileMode]::Create, [IO.FileAccess]::Write, [IO.FileShare]::None)

        $aes = [System.Security.Cryptography.Aes]::Create()
        $aes.KeySize = 256
        $aes.Mode = [System.Security.Cryptography.CipherMode]::CBC
        $aes.Padding = [System.Security.Cryptography.PaddingMode]::PKCS7
        $aes.Key = $aesKey
        $aes.IV = $iv

        $decryptor = $aes.CreateDecryptor()
        $crypto = [System.Security.Cryptography.CryptoStream]::new(
            $output,
            $decryptor,
            [System.Security.Cryptography.CryptoStreamMode]::Write
        )

        $remaining = $sourceInfo.Length - $headerLength - $tagLength
        $buffer = New-Object byte[] 1048576
        while ($remaining -gt 0) {
            $requested = [Math]::Min($buffer.Length, $remaining)
            $read = $input.Read($buffer, 0, [int]$requested)
            if ($read -le 0) {
                throw 'Unexpected end of encrypted archive while decrypting.'
            }

            $crypto.Write($buffer, 0, $read)
            $remaining -= $read
        }

        $crypto.FlushFinalBlock()
    }
    finally {
        if ($crypto) { $crypto.Dispose() }
        if ($aes) { $aes.Dispose() }
        if ($input) { $input.Dispose() }
        if ($output) { $output.Dispose() }
    }
}

$repoRoot = Get-GitRoot
if ([string]::IsNullOrWhiteSpace($InputPath)) {
    $InputPath = Join-Path $repoRoot '.commit-archive\ProjectSnapshot.zip.aes'
}

if ([string]::IsNullOrWhiteSpace($OutputZip)) {
    $OutputZip = Join-Path $repoRoot '.commit-archive\ProjectSnapshot.zip'
}

$inputFullPath = [IO.Path]::GetFullPath($InputPath)
$outputZipFullPath = [IO.Path]::GetFullPath($OutputZip)
$plainPassword = Get-Password -ProvidedPassword $Password
if ([string]::IsNullOrEmpty($plainPassword)) {
    throw 'Archive password cannot be empty.'
}

if ((Test-Path -LiteralPath $outputZipFullPath) -and -not $Force) {
    throw "Output zip already exists: $outputZipFullPath. Use -Force to overwrite it."
}

Unprotect-ZipFile -SourcePath $inputFullPath -DestinationZip $outputZipFullPath -PlainPassword $plainPassword
Write-Host "Decrypted zip written: $outputZipFullPath"

if (-not [string]::IsNullOrWhiteSpace($ExtractTo)) {
    if ((Test-Path -LiteralPath $ExtractTo) -and -not $Force) {
        throw "Extract destination already exists: $ExtractTo. Use -Force to overwrite files."
    }

    Expand-Archive -LiteralPath $outputZipFullPath -DestinationPath $ExtractTo -Force:$Force
    Write-Host "Archive extracted to: $ExtractTo"
}
