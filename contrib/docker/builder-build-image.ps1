# builds SharpIrcBot and packs it into a Docker image

Set-Location "$PSScriptRoot\..\.."
& dotnet restore
Set-Location "SharpIrcBotCLI"
& dotnet publish -f "netcoreapp2.0" -r "debian.8-x64" -o "..\out"
Set-Location ".."
& docker build -t sharpircbot .
