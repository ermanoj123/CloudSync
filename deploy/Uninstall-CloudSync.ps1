# Uninstall-CloudSync.ps1
# Requires Run as Administrator

$ServiceName = "CloudSync Service"

Write-Host "Stopping service $ServiceName..."
Stop-Service -Name $ServiceName -ErrorAction SilentlyContinue

Write-Host "Uninstalling service $ServiceName..."
sc.exe delete "$ServiceName"

Write-Host "Service uninstalled successfully."
