SET Configuration=Release
SET Version=%1
SET ContinuousIntegration=true

REM dotnet tool
dotnet pack ^
 .\src\SemanticVersioning.CommandLine\SemanticVersioning.CommandLine.csproj ^
 --output .\nupkg
 
REM MSBuild Tasks
dotnet pack ^
 .\src\SemanticVersioning.MSBuild\SemanticVersioning.MSBuild.csproj ^
 --output .\nupkg
