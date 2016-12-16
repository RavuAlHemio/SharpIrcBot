FROM microsoft/dotnet:runtime
WORKDIR /
COPY contrib/docker/launch.sh .
WORKDIR /app
COPY out .
ENTRYPOINT ["/bin/sh", "/launch.sh"]
