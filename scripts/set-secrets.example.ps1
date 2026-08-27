#Requires -Version 5.1
<#
.SYNOPSIS
  Template for loading MarketData secrets into the current PowerShell session.

.DESCRIPTION
  1. Copy this file to set-secrets.local.ps1 (same folder).
  2. Replace the placeholder values with your real settings.
  3. Dot-source the local file so environment variables persist in this session:

       . .\scripts\set-secrets.local.ps1

  set-secrets.local.ps1 is gitignored and must never be committed.

.NOTES
  Environment variables used by eodhd_download:
    EODHD_API_TOKEN     - required EODHD API key
    MARKETDATA_DB_PATH  - optional path to the LiteDB database file
#>

$ErrorActionPreference = 'Stop'

# --- Edit these values in set-secrets.local.ps1 only (not in this example file) ---
$EodhdApiToken = 'YOUR_EODHD_API_TOKEN'
$MarketDataDbPath = 'D:\grokbot\LiteDb\EodHd.db'
# ----------------------------------------------------------------------------------

if ([string]::IsNullOrWhiteSpace($EodhdApiToken) -or $EodhdApiToken -eq 'YOUR_EODHD_API_TOKEN')
{
    throw @"
EODHD API token is not configured.

Copy this file to set-secrets.local.ps1, set `$EodhdApiToken to your real key, then run:
  . .\scripts\set-secrets.local.ps1
"@
}

$env:EODHD_API_TOKEN = $EodhdApiToken

if (-not [string]::IsNullOrWhiteSpace($MarketDataDbPath))
{
    $env:MARKETDATA_DB_PATH = $MarketDataDbPath
}

$tokenPreview = if ($env:EODHD_API_TOKEN.Length -le 4)
{
    '****'
}
else
{
    ('*' * ($env:EODHD_API_TOKEN.Length - 4)) + $env:EODHD_API_TOKEN.Substring($env:EODHD_API_TOKEN.Length - 4)
}

Write-Host "MarketData secrets loaded for this session." -ForegroundColor Green
Write-Host "  EODHD_API_TOKEN    = $tokenPreview"
Write-Host "  MARKETDATA_DB_PATH = $($env:MARKETDATA_DB_PATH)"
Write-Host ""
Write-Host "These variables apply only to this PowerShell session."
Write-Host "Run the app from this same window, for example:"
Write-Host "  dotnet run --project eodhd_download/eodhd_download.csproj"
