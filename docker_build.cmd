SET version=1.0.48

docker run --rm --workdir /build --volume %~dp0:/build --volume %USERPROFILE%\.nuget\packages:/root/.nuget/packages --entrypoint /bin/sh mcr.microsoft.com/dotnet/core/sdk:3.0 /build/test.sh %version%
docker run --rm --workdir /build --volume %~dp0:/build --volume %USERPROFILE%\.nuget\packages:/root/.nuget/packages --entrypoint /bin/sh mcr.microsoft.com/dotnet/core/sdk:3.0 /build/pack.sh %version%