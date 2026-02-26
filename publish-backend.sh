#!/bin/bash
set -e

IMAGE_NAME="piratesquest-api"
TAG="${1:-latest}"

echo "=== Building Docker image ${IMAGE_NAME}:${TAG} ==="
echo "This builds the webview, the .NET API, and packages them together."
docker build -t "${IMAGE_NAME}:${TAG}" .

echo ""
echo "=== Done! ==="
echo "Image: ${IMAGE_NAME}:${TAG}"
echo ""
echo "Run it with:"
echo "  docker run -p 5236:8080 \\"
echo "    -e ConnectionStrings__Default='Host=...;Database=piratesquest;Username=...;Password=...' \\"
echo "    -e Jwt__Key='your-secret-key-at-least-32-bytes' \\"
echo "    -e ServerApiKey='your-server-api-key' \\"
echo "    ${IMAGE_NAME}:${TAG}"
