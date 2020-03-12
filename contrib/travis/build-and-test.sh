#!/bin/sh
set -e
set -x
topdir="`pwd`"

cd "$topdir/SharpIrcBotCLI"
dotnet build "SharpIrcBotCLI.csproj" -f "netcoreapp3.1"

for testdir in "$topdir/Tests"/*Tests
do
    cd "$testdir"
    dotnet test -f "netcoreapp3.1"
done
