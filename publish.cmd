SET TargetFramework=netcoreapp3.1

dotnet publish ^
 src\SemanticVersioning.CommandLine\SemanticVersioning.CommandLine.csproj ^
 --configuration Release ^
 --framework %TargetFramework% ^
 --runtime win-x64 ^
 -property:Version=%1 ^
 -property:PublishSingleFile=true ^
 -property:PublishTrimmed=true ^
 -property:ContinuousIntegration=true
dotnet publish ^
 src\SemanticVersioning.CommandLine\SemanticVersioning.CommandLine.csproj ^
 --configuration Release ^
 --runtime %TargetFramework% ^
 -property:Version=%1 ^
 -property:PublishSingleFile=true ^
 -property:PublishTrimmed=true ^
 -property:ContinuousIntegration=true