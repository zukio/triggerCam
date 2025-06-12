#!/bin/bash

set -e

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
  ["NAudio"]="2.2.1"
  ["OpenCvSharp4"]="4.9.0"
  ["OpenCvSharp4.runtime.win"]="4.9.0"
  ["System.IO.Ports"]="8.0.0"
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
