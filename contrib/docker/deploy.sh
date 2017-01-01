#!/bin/sh
docker \
    create \
    --interactive \
    --tty \
    --name ravusbot \
    --volume /var/lib/ircbot/config:/app/config \
    --volume /etc/localtime:/etc/localtime \
    --restart unless-stopped \
    sharpircbot
