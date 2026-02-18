#!/bin/bash
# Build the entire solution

set -e

echo "🔨 Building EduCheck solution..."
dotnet build EduCheck.sln
echo "✅ Build completed successfully!"