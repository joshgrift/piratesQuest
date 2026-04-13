#!/bin/bash
set -euo pipefail

# Exit immediately if any command fails (-e),
# treat unset variables as errors (-u),
# and fail pipelines when any command fails (pipefail).

# DigitalOcean Container Registry settings.
# Example:
#   export DOCR_REGISTRY="registry.digitalocean.com/piratesquest"
#   export DOCR_SERVER_REPOSITORY="piratesquest-server"
DOCR_REGISTRY="${DOCR_REGISTRY:-registry.digitalocean.com/piratesquest}"
DOCR_SERVER_REPOSITORY="${DOCR_SERVER_REPOSITORY:-piratesquest-server}"
DOCR_SERVER_PLATFORM="${DOCR_SERVER_PLATFORM:-linux/amd64}"
BUILD_TIMESTAMP_UTC="$(date -u +"%Y-%m-%dT%H:%M:%SZ")"

echo "Building and exporting project..."

# Extract version from project.godot.
VERSION=$(grep 'config/version=' godot/project.godot | sed 's/config\/version="\(.*\)"/\1/')

echo "==== Building Port WebView ===="
npm --prefix webview install
npm --prefix webview run build -- --base="/fragments/webview/"

# Create version directory (use absolute path so Godot resolves it correctly with --path flag).
VERSION_DIR="$(pwd)/dist/$VERSION"
SERVER_EXPORT_DIR="$VERSION_DIR/piratesquest-server-linux-x64"
SERVER_IMAGE_STAGING_DIR="$(pwd)/godot/.docker/server-image"
SERVER_IMAGE_NAME="${DOCR_REGISTRY}/${DOCR_SERVER_REPOSITORY}"

# Check if version directory already exists.
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
/Applications/Godot_mono.app/Contents/MacOS/Godot --path godot --headless --export-release "macOS" "$VERSION_DIR/piratesquest.app"
# Create zip from the app.
cd "$VERSION_DIR" && zip -r "piratesquest-macos.zip" "piratesquest.app" && cd ../..

echo "==== Building Windows Client x64 ===="
mkdir -p "$VERSION_DIR/piratesquest-windows-x64"
/Applications/Godot_mono.app/Contents/MacOS/Godot --path godot --headless --export-release "Windows Desktop" "$VERSION_DIR/piratesquest-windows-x64/piratesquest.exe"
# Create zip from the windows folder.
cd "$VERSION_DIR" && zip -r "piratesquest-windows-x64.zip" "piratesquest-windows-x64" && cd ../..

echo "==== Building Linux Server x64 ===="
mkdir -p "$SERVER_EXPORT_DIR"
/Applications/Godot_mono.app/Contents/MacOS/Godot --path godot --headless --export-release "Linux-server" "$SERVER_EXPORT_DIR/piratesquest-server"
# Create zip from the linux server folder.
cd "$VERSION_DIR" && zip -r "piratesquest-server-linux-x64.zip" "piratesquest-server-linux-x64" && cd ../..

echo "==== Preparing dedicated server Docker image ===="
rm -rf "$SERVER_IMAGE_STAGING_DIR"
mkdir -p "$SERVER_IMAGE_STAGING_DIR"
cp -R "$SERVER_EXPORT_DIR"/. "$SERVER_IMAGE_STAGING_DIR"/

echo "Building Docker image ${SERVER_IMAGE_NAME}:${VERSION}"
# Add a fresh timestamp label on every build.
# This makes repeated builds of the same game version produce a new image digest,
# so pushing the same version tag updates the registry to the newest upload.
docker build \
  --platform "${DOCR_SERVER_PLATFORM}" \
  --label "org.opencontainers.image.version=${VERSION}" \
  --label "org.opencontainers.image.created=${BUILD_TIMESTAMP_UTC}" \
  -f godot/server-docker/Dockerfile \
  -t "${SERVER_IMAGE_NAME}:${VERSION}" \
  -t "${SERVER_IMAGE_NAME}:latest" \
  .

echo "Pushing Docker image to DigitalOcean Container Registry"
docker push "${SERVER_IMAGE_NAME}:${VERSION}"
docker push "${SERVER_IMAGE_NAME}:latest"

echo "==== Cleaning up .command files ===="
find "$VERSION_DIR" -name "*.command" -type f -delete

echo ""
echo "Build complete! Files are in: $VERSION_DIR"
echo "  - piratesquest.app (macOS client app)"
echo "  - piratesquest-macos.zip (macOS client zip)"
echo "  - piratesquest-windows-x64/ (Windows x64 client folder)"
echo "  - piratesquest-windows-x64.zip (Windows x64 client zip)"
echo "  - piratesquest-server-linux-x64/ (Linux x64 server folder)"
echo "  - piratesquest-server-linux-x64.zip (Linux x64 server zip)"

echo "  - ${SERVER_IMAGE_NAME}:${VERSION} (pushed)"
echo "  - ${SERVER_IMAGE_NAME}:latest (pushed)"
