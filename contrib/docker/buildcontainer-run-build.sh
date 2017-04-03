#!/bin/sh
# installs prerequisites into the running Docker container and then builds a SharpIrcBot Docker image
# (yup, "docker build" inside a Docker container)

# install Docker
apt-get update
apt-get install apt-transport-https lsb-release software-properties-common
curl -fsSL https://download.docker.com/linux/debian/gpg | apt-key add -
echo "deb [arch=amd64] https://download.docker.com/linux/debian $(lsb_release -cs) stable" >> "/etc/apt/sources.list.d/docker.list"
apt-get update
apt-get install docker-ce

# clone the repo
git clone --recurse-submodules https://github.com/RavuAlHemio/SharpIrcBot.git /src

# build
cd /src
./contrib/docker/builder-build-image.sh
