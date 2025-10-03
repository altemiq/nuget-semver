dotnet build ^
 Assembly.ChangeDetection.sln ^
 --configuration Release ^
 -property:Version=%1 ^
 -property:ContinuousIntegration=true
