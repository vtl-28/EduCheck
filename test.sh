#!/bin/bash
# Run all tests

set -e

echo "🧪 Running tests..."
dotnet test EduCheck.Tests
echo "✅ Tests completed!"