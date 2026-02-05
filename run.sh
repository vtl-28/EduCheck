#!/bin/bash
# Run the EduCheck API

set -e

echo "🚀 Starting EduCheck API..."
dotnet run --project EduCheck.API --launch-profile http