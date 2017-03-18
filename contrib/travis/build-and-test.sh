#!/bin/sh
set -e
dotnet build
for testdir in Tests/*Tests
do
    cd "$testdir"
    dotnet test
    cd "../.."
done
