#!/bin/sh

# dotnet tool
dotnet pack \
  src/SemanticVersioning.TeamCity/SemanticVersioning.TeamCity.csproj \
  --output nupkg \
  /p:Version=$1
  