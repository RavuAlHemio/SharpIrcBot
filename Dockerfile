# runtime Dockerfile
FROM microsoft/dotnet:runtime
WORKDIR /
COPY contrib/docker/container-launch.sh .
WORKDIR /app
COPY out .
ENTRYPOINT ["/bin/sh", "/container-launch.sh"]
