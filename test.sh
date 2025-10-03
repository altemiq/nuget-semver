#!/bin/sh

dotnet test \
  Assembly.ChangeDetection.sln \
  -property:ContinuousIntegration=true
