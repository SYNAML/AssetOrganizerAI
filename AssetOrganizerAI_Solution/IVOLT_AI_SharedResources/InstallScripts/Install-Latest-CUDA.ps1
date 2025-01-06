# Check if running as Administrator
if (-not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "You must run this script as Administrator!"
    exit 1
}

#Updating Execution Piolicy
Set-ExecutionPolicy Unrestricted -Scope Process

Write-Output "Checking for NVIDIA driver..."
$driver = Get-WmiObject Win32_VideoController | Where-Object { $_.Name -like "*NVIDIA*" }
if (-not $driver) {
    Write-Output "NVIDIA driver not found. Please install it first."
    exit 1
}

Write-Output "Fetching the latest CUDA version..."
$latestCudaPage = Invoke-WebRequest -Uri "https://developer.nvidia.com/cuda-downloads" -UseBasicParsing
$latestCudaLink = $latestCudaPage.Links | Where-Object { $_.href -like "*exe" -and $_.href -match "cuda_" } | Select-Object -First 1

if (-not $latestCudaLink) {
    Write-Error "Failed to fetch the latest CUDA version."
    exit 1
}

$installerUrl = $latestCudaLink.href
$installerFile = "$env:USERPROFILE\Downloads\cuda_installer.exe"

Write-Output "Downloading CUDA installer from $installerUrl..."
Invoke-WebRequest -Uri $installerUrl -OutFile $installerFile

Write-Output "Running CUDA Toolkit installer..."
Start-Process -FilePath $installerFile -ArgumentList "/silent", "/noreboot" -Wait

Write-Output "Adding CUDA to PATH..."
[Environment]::SetEnvironmentVariable("PATH", $env:PATH + ";C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v*", [EnvironmentVariableTarget]::Machine)

Write-Output "CUDA Toolkit installation complete. Verifying..."
& "nvcc" --version
