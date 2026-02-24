#!/bin/bash

echo "Publishing backend..."
dotnet publish server --configuration Release
echo "Done! Output is in: server/bin/Release/net*/publish/"
