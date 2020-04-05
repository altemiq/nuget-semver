#!/bin/sh

dotnet test \
  src/Assembly.ChangeDetection.sln \
  -property:Version=$1 \
  -property:ContinuousIntegration=true