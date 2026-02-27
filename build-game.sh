#!/bin/bash

echo "Building and exporting project..."

# Extract version from project.godot
VERSION=$(grep 'config/version=' godot/project.godot | sed 's/config\/version="\(.*\)"/\1/')

# Create version directory (use absolute path so Godot resolves it correctly with --path flag)
VERSION_DIR="$(pwd)/dist/$VERSION"

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
/Applications/Godot_mono.app/Contents/MacOS/Godot --path godot --headless --export-debug "macOS" "$VERSION_DIR/piratesquest.app"
# Create zip from the app
cd "$VERSION_DIR" && zip -r "piratesquest-macos.zip" "piratesquest.app" && cd ../..

echo "==== Building macOS Server ===="
/Applications/Godot_mono.app/Contents/MacOS/Godot --path godot --headless --export-debug "macOS-server" "$VERSION_DIR/piratesquest-server.app"
# Create zip from the app
cd "$VERSION_DIR" && zip -r "piratesquest-server-macos.zip" "piratesquest-server.app" && cd ../..

echo "==== Building Windows Client (x64) ===="
mkdir -p "$VERSION_DIR/piratesquest-windows-x64"
/Applications/Godot_mono.app/Contents/MacOS/Godot --path godot --headless --export-debug "Windows Desktop" "$VERSION_DIR/piratesquest-windows-x64/piratesquest.exe"
# Create zip from the windows folder
cd "$VERSION_DIR" && zip -r "piratesquest-windows-x64.zip" "piratesquest-windows-x64" && cd ../..

echo "==== Building Linux Server (x64) ===="
mkdir -p "$VERSION_DIR/piratesquest-server-linux-x64"
/Applications/Godot_mono.app/Contents/MacOS/Godot --path godot --headless --export-debug "Linux-server" "$VERSION_DIR/piratesquest-server-linux-x64/piratesquest-server"
# Create zip from the linux server folder
cd "$VERSION_DIR" && zip -r "piratesquest-server-linux-x64.zip" "piratesquest-server-linux-x64" && cd ../..

echo "==== Cleaning up .command files ===="
find "$VERSION_DIR" -name "*.command" -type f -delete

echo ""
echo "Build complete! Files are in: $VERSION_DIR"
echo "  - piratesquest.app (macOS client app)"
echo "  - piratesquest-macos.zip (macOS client zip)"
echo "  - piratesquest-server.app (macOS server app)"
echo "  - piratesquest-server-macos.zip (macOS server zip)"
echo "  - piratesquest-windows-x64/ (Windows x64 client folder)"
echo "  - piratesquest-windows-x64.zip (Windows x64 client zip)"
echo "  - piratesquest-server-linux-x64/ (Linux x64 server folder)"
echo "  - piratesquest-server-linux-x64.zip (Linux x64 server zip)"
