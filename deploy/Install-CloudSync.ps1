# Install-CloudSync.ps1
# Requires Run as Administrator

$ErrorActionPreference = "Stop"

$ServiceName = "CloudSync Service"
$ExePath = Join-Path $PSScriptRoot "..\src\CloudSync.Worker\bin\Release\net8.0\win-x64\publish\CloudSync.Worker.exe"

if (-Not (Test-Path $ExePath)) {
    Write-Error "Could not find specific executable at $ExePath. Please build/publish the project first."
    exit 1
}

Write-Host "Creating service $ServiceName..."
New-Service -Name $ServiceName `
            -BinaryPathName $ExePath `
            -DisplayName "CloudSync Google Drive Synchronization Service" `
            -Description "Continuously monitors local folders and pushes to Google Drive natively." `
            -StartupType Automatic

# Set recovery options (restart on failure)
sc.exe failure "$ServiceName" reset= 86400 actions= restart/60000/restart/60000/restart/60000
sc.exe config "$ServiceName" start= delayed-auto

Write-Host "Service installed successfully. You can start it via 'Start-Service ""$ServiceName""'."
