#!/bin/sh

# dotnet tool
dotnet pack \
  SemanticVersioning.CommandLine/SemanticVersioning.CommandLine.csproj \
  --configuration Release \
  --output nupkg \
  -property:Version=$1 \
  -property:ContinuousIntegration=true
 
# MSBuild Tasks
dotnet pack \
  SemanticVersioning.MSBuild/SemanticVersioning.MSBuild.csproj \
  --configuration Release \
  --output nupkg \
  -property:Version=$1 \
  -property:ContinuousIntegration=true
  
