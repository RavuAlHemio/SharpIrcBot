#!/bin/sh
set -e
set -x
topdir="`pwd`"

cd "$topdir/Libraries/HtmlAgilityPack"
dotnet build "HtmlAgilityPack.csproj" -f "netstandard1.6"
cd "$topdir/Libraries/SmartIrc4net"
dotnet build "SmartIrc4net.csproj" -f "netstandard1.3"

cd "$topdir/SharpIrcBot"
dotnet build "SharpIrcBot.csproj" -f "netstandard1.6"

for plugindir in "$topdir/Plugins"/*
do
    cd "$plugindir"
    if [ -f *.csproj ]
    then
        dotnet build -f "netstandard1.6"
    fi
done

for plugindir in "$topdir/Plugins/Libraries"/*
do
    cd "$plugindir"
    if [ -f *.csproj ]
    then
        dotnet build -f "netstandard1.6"
    fi
done

cd "$topdir/SharpIrcBotCLI"
dotnet build "SharpIrcBotCLI.csproj" -f "netcoreapp2.0"

for testdir in "$topdir/Tests"/*Tests
do
    cd "$testdir"
    dotnet test -f "netcoreapp2.0"
done
