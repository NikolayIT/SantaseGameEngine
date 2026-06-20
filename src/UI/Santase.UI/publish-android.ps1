<#
.SYNOPSIS
    Builds a Google Play-ready, release-signed Android App Bundle (.aab) for Santase.

.DESCRIPTION
    Signs with YOUR upload keystore (created once via keytool — see PLAYSTORE.md), never
    the shared Android debug key. No secret is committed or passed on the command line:
    the keystore path/alias come from env vars, and the two passwords are read by the
    .NET Android build itself via the "env:VARNAME" indirection.

    Required environment variables:
        SANTASE_KEYSTORE        full path to the .keystore file
        SANTASE_KEY_ALIAS       key alias inside the keystore
        SANTASE_KEYSTORE_PASS   keystore (store) password
        SANTASE_KEY_PASS        key (alias) password

.EXAMPLE
        $env:SANTASE_KEYSTORE      = 'C:\keys\santase-upload.keystore'
        $env:SANTASE_KEY_ALIAS     = 'santase'
        $env:SANTASE_KEYSTORE_PASS = '***'
        $env:SANTASE_KEY_PASS      = '***'
        .\publish-android.ps1
#>
[CmdletBinding()]
param(
    [string] $Keystore = $env:SANTASE_KEYSTORE,
    [string] $Alias    = $env:SANTASE_KEY_ALIAS
)

$ErrorActionPreference = 'Stop'
$projectDir = $PSScriptRoot
$project    = Join-Path $projectDir 'Santase.UI.csproj'

function Require([string] $value, [string] $name) {
    if ([string]::IsNullOrWhiteSpace($value)) { throw "Missing required value: $name. See PLAYSTORE.md." }
}

# Fall back to the git-ignored keystore that lives next to this script, and the default
# alias from PLAYSTORE.md, when the env vars are not set.
if ([string]::IsNullOrWhiteSpace($Keystore)) {
    $localKeystore = Join-Path $projectDir 'santase-upload.keystore'
    if (Test-Path $localKeystore) { $Keystore = $localKeystore }
}
if ([string]::IsNullOrWhiteSpace($Alias)) { $Alias = 'santase' }

Require $Keystore                 'SANTASE_KEYSTORE (or place santase-upload.keystore next to this script)'
Require $Alias                    'SANTASE_KEY_ALIAS'
Require $env:SANTASE_KEYSTORE_PASS 'SANTASE_KEYSTORE_PASS (env var)'
Require $env:SANTASE_KEY_PASS      'SANTASE_KEY_PASS (env var)'
if (-not (Test-Path $Keystore)) { throw "Keystore not found: $Keystore" }

Write-Host ''
Write-Host "Publishing signed Android App Bundle" -ForegroundColor Cyan
Write-Host "  keystore : $Keystore"
Write-Host "  alias    : $Alias"
Write-Host ''

# Passwords are NOT placed on the command line; .NET Android resolves env:VARNAME itself.
dotnet publish $project -f net10.0-android -c Release `
    -p:AndroidKeyStore=true `
    -p:AndroidSigningKeyStore="$Keystore" `
    -p:AndroidSigningKeyAlias="$Alias" `
    -p:AndroidSigningStorePass=env:SANTASE_KEYSTORE_PASS `
    -p:AndroidSigningKeyPass=env:SANTASE_KEY_PASS
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

$aab = Get-ChildItem -Path (Join-Path $projectDir 'bin\Release\net10.0-android') -Filter '*-Signed.aab' -Recurse |
       Sort-Object LastWriteTime -Descending | Select-Object -First 1
Write-Host ''
if ($aab) {
    Write-Host "Done. Upload this to Play Console:" -ForegroundColor Green
    Write-Host "  $($aab.FullName)"
} else {
    Write-Warning "Build finished but no *-Signed.aab was found under bin\Release\net10.0-android."
}
