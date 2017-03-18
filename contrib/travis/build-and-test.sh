#!/bin/sh
set -e
topdir="`pwd`"

cd "$topdir/Libraries/HtmlAgilityPack"
dotnet build -f "netstandard1.6"
cd "$topdir/Libraries/SmartIrc4net"
dotnet build -f "netstandard1.3"

cd "$topdir/SharpIrcBot"
dotnet build -f "netstandard1.6"

for plugindir in "$topdir/Plugins"/*
do
    cd "$plugindir"
    dotnet build -f "netstandard1.6"
done

cd "$topdir/SharpIrcBotCLI"
dotnet build -f "netcoreapp1.1"

for testdir in "$topdir/Tests"/*Tests
do
    cd "$testdir"
    dotnet build -f "netcoreapp1.1"
    dotnet test -f "netcoreapp1.1"
done
