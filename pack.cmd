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

SET TargetFramework=netcoreapp3.1

REM TeamCity Tool
MKDIR pack
CD pack
MKDIR win-x64
MKDIR linux-x64

COPY ..\src\SemanticVersioning.Commandline\bin\%Configuration%\%TargetFramework%\win-x64\publish\* win-x64\
COPY ..\src\SemanticVersioning.Commandline\bin\%Configuration%\%TargetFramework%\linux-x64\publish\* linux-x64\
COPY ..\src\teamcity-plugin.xml
..\7za.exe a ..\SemanticVersioning.DEFAULT.zip

CD ..

COPY SemanticVersioning.DEFAULT.zip SemanticVersioning.%1.zip

RMDIR pack /S /Q