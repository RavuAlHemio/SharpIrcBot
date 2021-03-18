#!/bin/sh
set -e
set -x
topdir="`pwd`"

cd "$topdir/SharpIrcBotCLI"
dotnet build "SharpIrcBotCLI.csproj" -f "net5.0"

for testdir in "$topdir/Tests"/*Tests
do
    cd "$testdir"
    dotnet test -f "net5.0"
done
