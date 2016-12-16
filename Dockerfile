FROM microsoft/dotnet:runtime
WORKDIR /
COPY contrib/docker/launch.sh .
WORKDIR /app
COPY out .
ENTRYPOINT ["dotnet", "SharpIrcBotCLI.dll"]
