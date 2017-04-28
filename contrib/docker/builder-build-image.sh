#!/bin/sh
# builds SharpIrcBot and packs it into a Docker image

cd "$(dirname "$0")/../.."
dotnet restore
cd SharpIrcBotCLI
dotnet publish -f "netcoreapp1.1" -r "debian.8-x64" -o "../out"
cd ..
docker build -t sharpircbot .