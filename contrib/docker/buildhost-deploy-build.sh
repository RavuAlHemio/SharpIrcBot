#!/bin/sh
# constructs a build image and performs the build

docker build \
    -f Dockerfile.build \
    --tag "sharpircbot-build" \
    .
docker run \
    --interactive \
    --tty \
    --volume /run/docker.sock:/run/docker.sock \
    "sharpircbot-build"
docker rmi "sharpircbot-build"
