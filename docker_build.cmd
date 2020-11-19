SET version=1.0.71

docker run --rm --workdir /build --volume %~dp0:/build --volume %USERPROFILE%\.nuget\packages:/root/.nuget/packages --entrypoint /bin/sh mcr.microsoft.com/dotnet/core/sdk:latest /build/test.sh %version%
docker run --rm --workdir /build --volume %~dp0:/build --volume %USERPROFILE%\.nuget\packages:/root/.nuget/packages --entrypoint /bin/sh mcr.microsoft.com/dotnet/core/sdk:latest /build/pack.sh %version%