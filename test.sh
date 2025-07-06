#!/bin/sh

dotnet test \
  src/Assembly.ChangeDetection.sln \
  -property:ContinuousIntegration=true