#!/bin/bash

cd "$(dirname "$0")/EduCheck.API"

export OTEL_SERVICE_NAME=""
export OTEL_EXPORTER_OTLP_ENDPOINT=""
export OTEL_EXPORTER_OTLP_PROTOCOL=""
export OTEL_RESOURCE_ATTRIBUTES="""
export OTEL_EXPORTER_OTLP_HEADERS="Authorization=Basic YOUR_TOKEN_HERE"

echo "🚀 Starting EduCheck API with Grafana Cloud telemetry..."


dotnet run