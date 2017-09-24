#!/bin/sh
# launches SharpIrcBot within a Docker container

# the admin might want to mount /app/config as a Docker volume
mkdir -p /app/config
for configfile in "Config.json" "CountryCodes.json" "LogFilter.json" "tlds-alpha-by-domain.txt" "tzdb.nzd"
do
    # /app/$configfile -> /app/config/$configfile
    ln -sf "config/$configfile" /app/
done

if [ -f /emergency ]
then
    cd /
    exec /bin/bash
else
    cd /app
    exec dotnet exec SharpIrcBotCLI.dll
fi
