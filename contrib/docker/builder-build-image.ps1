Set-Location "$PSScriptRoot\..\.."
& dotnet restore
Set-Location "SharpIrcBotCLI"
& dotnet publish -f "netcoreapp1.1" -r "debian.8-x64" -o "..\out"
Set-Location ".."
& docker build -t sharpircbot .
