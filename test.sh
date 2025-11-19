#!/bin/sh

dotnet test \
  Assembly.ChangeDetection.slnx \
  -property:ContinuousIntegration=true
