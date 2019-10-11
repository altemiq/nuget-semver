REM dotnet tool
dotnet pack .\src\SemanticVersioning.TeamCity\SemanticVersioning.TeamCity.csproj --output .\nupkg

REM TeamCity Tool
MKDIR pack
CD pack
MKDIR win-x64
MKDIR linux-x64

COPY ..\src\SemanticVersioning.TeamCity\bin\Release\netcoreapp3.0\win-x64\publish\* win-x64\
COPY ..\src\SemanticVersioning.TeamCity\bin\Release\netcoreapp3.0\linux-x64\publish\* linux-x64\
COPY ..\src\teamcity-plugin.xml
..\7za.exe a ..\SemanticVersioning.DEFAULT.zip

CD ..

COPY SemanticVersioning.DEFAULT.zip SemanticVersioning.%1.zip

RMDIR pack /S /Q