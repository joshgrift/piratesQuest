#!/bin/bash

echo "Building and exporting project..."

# Extract version from project.godot
VERSION=$(grep 'config/version=' project.godot | sed 's/config\/version="\(.*\)"/\1/')

# Create version directory
VERSION_DIR="dist/$VERSION"

# Check if version directory already exists
if [ -d "$VERSION_DIR" ]; then
    echo "Warning: Version directory '$VERSION_DIR' already exists."
    read -p "Do you want to overwrite it? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo "Build cancelled."
        exit 1
    fi
    echo "Removing existing directory..."
    rm -rf "$VERSION_DIR"
fi

mkdir -p "$VERSION_DIR"

echo "==== Building macOS Client ===="
# Export directly to version directory
/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --export-debug "macOS" "$VERSION_DIR/piratesquest.app"
# Create zip from the app
cd "$VERSION_DIR" && zip -r "piratesquest-macos.zip" "piratesquest.app" && cd ../..

echo "==== Building macOS Server ===="
# Export directly to version directory
/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --export-debug "macOS-server" "$VERSION_DIR/piratesquest-server.app"
# Create zip from the app
cd "$VERSION_DIR" && zip -r "piratesquest-server-macos.zip" "piratesquest-server.app" && cd ../..

echo "==== Building Windows Client (x64) ===="
# Export directly to version directory
mkdir -p "$VERSION_DIR/piratesquest-windows-x64"
/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --export-debug "Windows Desktop" "$VERSION_DIR/piratesquest-windows-x64/piratesquest.exe"
# Create zip from the windows folder
cd "$VERSION_DIR" && zip -r "piratesquest-windows-x64.zip" "piratesquest-windows-x64" && cd ../..

echo "==== Cleaning up .command files ===="
# Remove any .command files from the version directory
find "$VERSION_DIR" -name "*.command" -type f -delete

echo ""
echo "Build complete! Files are in: $VERSION_DIR"
echo "  - piratesquest.app (macOS client app)"
echo "  - piratesquest-macos.zip (macOS client zip)"
echo "  - piratesquest-server.app (macOS server app)"
echo "  - piratesquest-server-macos.zip (macOS server zip)"
echo "  - piratesquest-windows-x64/ (Windows x64 client folder)"
echo "  - piratesquest-windows-x64.zip (Windows x64 client zip)"