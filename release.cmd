dotnet build ^
 Assembly.ChangeDetection.slnx ^
 --configuration Release ^
 -property:Version=%1 ^
 -property:ContinuousIntegration=true
