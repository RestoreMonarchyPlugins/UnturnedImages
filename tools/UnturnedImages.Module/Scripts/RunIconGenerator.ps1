<#
.SYNOPSIS
    Automated Unturned icon generator with crash recovery and auto-restart.

.DESCRIPTION
    This script runs Unturned in a loop, automatically restarting it when it crashes.
    Combined with the UnturnedImages module's auto-start feature and crash recovery,
    this enables fully automated icon generation for all assets.

.PARAMETER UnturnedPath
    Path to the Unturned installation folder.
    Default: "C:\Program Files (x86)\Steam\steamapps\common\Unturned"

.PARAMETER MaxRestarts
    Maximum number of restarts before giving up. Set to 0 for unlimited.
    Default: 0 (unlimited)

.PARAMETER RestartDelay
    Delay in seconds between crash and restart.
    Default: 5

.PARAMETER StopOnComplete
    Stop the script when generation_complete.txt is detected.
    Default: $true

.EXAMPLE
    .\RunIconGenerator.ps1

.EXAMPLE
    .\RunIconGenerator.ps1 -UnturnedPath "D:\Games\Unturned" -MaxRestarts 50

.NOTES
    Before running this script:
    1. Edit config.json in your Unturned folder to enable AutoStart:
       {
         "SkipGuids": [],
         "AutoStart": {
           "Enabled": true,
           "Mode": "all",
           "GenerateItems": true,
           "GenerateVehicles": true,
           "QuitWhenDone": true,
           "StartDelaySeconds": 10
         }
       }

    2. Make sure UnturnedImages module is installed in Unturned/Modules/
#>

param(
    [string]$UnturnedPath = "C:\Program Files (x86)\Steam\steamapps\common\Unturned",
    [int]$MaxRestarts = 0,
    [int]$RestartDelay = 5,
    [bool]$StopOnComplete = $true
)

# Paths
$unturnedExe = Join-Path $UnturnedPath "Unturned.exe"
$configPath = Join-Path $UnturnedPath "config.json"
$completionFile = Join-Path $UnturnedPath "generation_complete.txt"
$pendingAssetFile = Join-Path $UnturnedPath "pending_asset.txt"
$logPath = Join-Path $UnturnedPath "Logs\Client.log"

# Validate Unturned path
if (-not (Test-Path $unturnedExe)) {
    Write-Error "Unturned.exe not found at: $unturnedExe"
    Write-Host "Please specify the correct path using -UnturnedPath parameter"
    exit 1
}

# Check if config exists and has AutoStart enabled
if (Test-Path $configPath) {
    $config = Get-Content $configPath -Raw | ConvertFrom-Json
    if (-not $config.AutoStart -or -not $config.AutoStart.Enabled) {
        Write-Warning "AutoStart is not enabled in config.json!"
        Write-Host ""
        Write-Host "Please edit $configPath and add/update the AutoStart section:"
        Write-Host @"
{
  "SkipGuids": [],
  "AutoStart": {
    "Enabled": true,
    "Mode": "all",
    "GenerateItems": true,
    "GenerateVehicles": true,
    "QuitWhenDone": true,
    "StartDelaySeconds": 10
  }
}
"@
        Write-Host ""
        $continue = Read-Host "Continue anyway? (y/n)"
        if ($continue -ne "y") {
            exit 0
        }
    }
} else {
    Write-Warning "config.json not found. It will be created on first run."
}

# Clean up any previous completion marker
if (Test-Path $completionFile) {
    Remove-Item $completionFile -Force
}

# Statistics
$restartCount = 0
$crashedAssets = @()
$startTime = Get-Date

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Unturned Icon Generator" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Unturned Path: $UnturnedPath"
Write-Host "Max Restarts: $(if ($MaxRestarts -eq 0) { 'Unlimited' } else { $MaxRestarts })"
Write-Host "Stop on Complete: $StopOnComplete"
Write-Host ""
Write-Host "Press Ctrl+C to stop the script at any time."
Write-Host ""

function Get-CrashedAsset {
    if (Test-Path $pendingAssetFile) {
        $lines = Get-Content $pendingAssetFile
        if ($lines.Count -ge 2) {
            return @{
                Guid = $lines[0]
                Name = $lines[1]
                Type = if ($lines.Count -ge 3) { $lines[2] } else { "unknown" }
            }
        }
    }
    return $null
}

function Write-Status {
    param([string]$Message, [string]$Color = "White")
    $timestamp = Get-Date -Format "HH:mm:ss"
    Write-Host "[$timestamp] $Message" -ForegroundColor $Color
}

# Main loop
while ($true) {
    # Check if we've hit max restarts
    if ($MaxRestarts -gt 0 -and $restartCount -ge $MaxRestarts) {
        Write-Status "Maximum restarts ($MaxRestarts) reached. Stopping." -Color Yellow
        break
    }

    # Check if generation is complete
    if ($StopOnComplete -and (Test-Path $completionFile)) {
        Write-Status "Generation complete! Stopping." -Color Green
        Remove-Item $completionFile -Force
        break
    }

    # Check for crashed asset from previous run
    $crashedAsset = Get-CrashedAsset
    if ($crashedAsset) {
        Write-Status "Previous crash detected on $($crashedAsset.Type): $($crashedAsset.Name) ($($crashedAsset.Guid))" -Color Red
        $crashedAssets += $crashedAsset
        # The module will handle adding to skip list on next startup
    }

    if ($restartCount -gt 0) {
        Write-Status "Restarting Unturned in $RestartDelay seconds... (Restart #$restartCount)" -Color Yellow
        Start-Sleep -Seconds $RestartDelay
    }

    Write-Status "Starting Unturned..." -Color Cyan

    # Start Unturned
    $process = Start-Process -FilePath $unturnedExe -PassThru -WorkingDirectory $UnturnedPath

    Write-Status "Unturned started (PID: $($process.Id)). Waiting for it to exit..." -Color Gray

    # Wait for the process to exit
    $process.WaitForExit()
    $exitCode = $process.ExitCode

    # Check exit reason
    if ($exitCode -eq 0) {
        # Normal exit - check if generation is complete
        if (Test-Path $completionFile) {
            Write-Status "Unturned exited normally. Generation complete!" -Color Green
            Remove-Item $completionFile -Force
            break
        } else {
            Write-Status "Unturned exited normally (exit code 0)." -Color Green

            # Ask if user wants to continue
            if (-not $StopOnComplete) {
                break
            }
        }
    } else {
        Write-Status "Unturned crashed or exited with code: $exitCode" -Color Red
        $restartCount++
    }
}

# Print summary
$endTime = Get-Date
$duration = $endTime - $startTime

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Total runtime: $($duration.ToString('hh\:mm\:ss'))"
Write-Host "Total restarts: $restartCount"

if ($crashedAssets.Count -gt 0) {
    Write-Host ""
    Write-Host "Assets that caused crashes:" -ForegroundColor Yellow
    foreach ($asset in $crashedAssets) {
        Write-Host "  - [$($asset.Type)] $($asset.Name) ($($asset.Guid))" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "Done!" -ForegroundColor Green
