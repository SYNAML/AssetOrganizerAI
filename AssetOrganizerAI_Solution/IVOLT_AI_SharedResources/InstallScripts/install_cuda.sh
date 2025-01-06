#!/bin/bash

# Check if the script is run as root
if [ "$EUID" -ne 0 ]; then
  echo "Please run as root"
  exit
fi

echo "Updating package lists..."
apt-get update

echo "Installing dependencies..."
apt-get install -y build-essential dkms curl

# Detect the NVIDIA driver
echo "Detecting NVIDIA driver..."
if ! command -v nvidia-smi &> /dev/null; then
  echo "NVIDIA driver is not installed. Installing the recommended driver..."
  apt-get install -y nvidia-driver-525
else
  echo "NVIDIA driver already installed. Version: $(nvidia-smi --query-gpu=driver_version --format=csv,noheader)"
fi

# Get the latest CUDA version and download URL
echo "Fetching the latest CUDA version..."
LATEST_CUDA_URL=$(curl -s https://developer.download.nvidia.com/compute/cuda/repos/ubuntu2004/x86_64/ | grep -Eo 'cuda-repo-ubuntu2004-[0-9]+\.[0-9]+-[0-9]+.*?_amd64.deb' | sort -r | head -n 1)
LATEST_CUDA_VERSION=$(echo $LATEST_CUDA_URL | grep -Eo '[0-9]+\.[0-9]+')

if [ -z "$LATEST_CUDA_URL" ]; then
  echo "Failed to fetch the latest CUDA version."
  exit 1
fi

echo "Latest CUDA version: $LATEST_CUDA_VERSION"
echo "Downloading CUDA package..."
wget -q "https://developer.download.nvidia.com/compute/cuda/repos/ubuntu2004/x86_64/$LATEST_CUDA_URL"

# Install the CUDA package
echo "Installing CUDA package..."
dpkg -i "$LATEST_CUDA_URL"
cp /var/cuda-repo-ubuntu2004-$LATEST_CUDA_VERSION/cuda-*-keyring.gpg /usr/share/keyrings/

echo "Updating package lists..."
apt-get update

echo "Installing CUDA toolkit..."
apt-get install -y cuda

# Add CUDA to PATH and LD_LIBRARY_PATH
echo "Adding CUDA paths to environment variables..."
echo "export PATH=/usr/local/cuda/bin:\$PATH" >> ~/.bashrc
echo "export LD_LIBRARY_PATH=/usr/local/cuda/lib64:\$LD_LIBRARY_PATH" >> ~/.bashrc
source ~/.bashrc

echo "CUDA Toolkit installation complete. Verifying..."
nvcc --version
