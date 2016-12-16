#!/bin/sh
if [ -f /emergency ]
then
    cd /
    exec /bin/bash
else
    cd /app
    exec dotnet exec SharpIrcBotCLI.dll
fi
