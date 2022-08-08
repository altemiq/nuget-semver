SET version=2.0.1
SET tag=latest

docker pull mcr.microsoft.com/dotnet/sdk:%tag%
docker run --rm --workdir /build --volume %~dp0:/build --volume %USERPROFILE%\.nuget\packages:/root/.nuget/packages --entrypoint /bin/sh mcr.microsoft.com/dotnet/sdk:%tag% /build/test.sh %version%
docker run --rm --workdir /build --volume %~dp0:/build --volume %USERPROFILE%\.nuget\packages:/root/.nuget/packages --entrypoint /bin/sh mcr.microsoft.com/dotnet/sdk:%tag% /build/pack.sh %version%