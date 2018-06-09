#!/bin/sh
set -e
set -x
topdir="`pwd`"

cd "$topdir/SharpIrcBotCLI"
dotnet build "SharpIrcBotCLI.csproj" -f "netcoreapp2.1"

for testdir in "$topdir/Tests"/*Tests
do
    cd "$testdir"
    dotnet test -f "netcoreapp2.1"
done
