dotnet publish ^
 src\SemanticVersioning\SemanticVersioning.csproj ^
 --configuration Release ^
 --runtime win-x64 ^
 -property:Version=%1 ^
 -property:PublishSingleFile=true ^
 -property:PublishTrimmed=true ^
 -property:ContinuousIntegration=true
dotnet publish ^
 src\SemanticVersioning\SemanticVersioning.csproj ^
 --configuration Release ^
 --runtime linux-x64 ^
 -property:Version=%1 ^
 -property:PublishSingleFile=true ^
 -property:PublishTrimmed=true ^
 -property:ContinuousIntegration=true