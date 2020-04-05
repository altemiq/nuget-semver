#!/bin/sh

# dotnet tool
dotnet pack \
  src/SemanticVersioning/SemanticVersioning.csproj \
  --output nupkg \
  -property:Version=$1 \
  -property:ContinuousIntegration=true
  