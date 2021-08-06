#!/bin/sh

# dotnet tool
dotnet pack \
  src/SemanticVersioning.CommandLine/SemanticVersioning.CommandLine.csproj \
  --configuration Release \
  --output nupkg \
  -property:Version=$1 \
  -property:ContinuousIntegration=true
 
# MSBuild Tasks
dotnet pack ^
  src/SemanticVersioning.CommandLine/SemanticVersioning.MSBuild.csproj \
  --configuration Release \
  --output .\nupkg \
  -property:Version=%1 \
  -property:ContinuousIntegration=true
  