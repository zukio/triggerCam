#!/bin/bash

set -e

# --- Install .NET SDK if missing ---
if ! command -v dotnet &> /dev/null; then
  echo "🔧 Installing .NET SDK (apt-based)..."
  wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
  sudo dpkg -i packages-microsoft-prod.deb
  sudo apt-get update
  sudo apt-get install -y dotnet-sdk-8.0
fi


echo "📦 Checking dotnet CLI..."
if ! command -v dotnet &> /dev/null; then
    echo "❌ dotnet CLI is not installed. Please install .NET SDK."
    exit 1
fi

PROJECT="triggerCam.csproj"
if [ ! -f "$PROJECT" ]; then
    echo "❌ $PROJECT not found. Please run this from the project root."
    exit 1
fi

# NuGet dependencies
declare -A packages=(
  ["OpenCvSharp4"]="4.9.0"
  ["OpenCvSharp4.runtime.win"]="4.9.0"
  ["System.IO.Ports"]="8.0.0"
  ["DirectShowLib"]="1.0.0"
)

echo "🔍 Checking required NuGet packages..."
for pkg in "${!packages[@]}"; do
    version="${packages[$pkg]}"
    if ! grep -q "<PackageReference Include=\"$pkg\" Version=\"$version\"" "$PROJECT"; then
        echo "📦 Installing $pkg@$version..."
        dotnet add package "$pkg" -v "$version"
    else
        echo "✅ $pkg@$version already present"
    fi
done

# Create needed folders
mkdir -p bin/Debug/net6.0-windows/Videos
mkdir -p bin/Debug/net6.0-windows/ConsoleLogs

# Build project
echo "🛠 Building project..."
dotnet build -c Debug

if [ $? -eq 0 ]; then
    echo "✅ Build successful! Run with:"
    echo "   dotnet run"
else
    echo "❌ Build failed"
    exit 1
fi
