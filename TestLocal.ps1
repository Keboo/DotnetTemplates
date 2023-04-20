dotnet new uninstall Keboo.Dotnet.Templates
Remove-Item -Path "Keboo.Dotnet.Templates.*.nupkg"

dotnet pack -o .

dotnet new install $(Get-ChildItem -Path "Keboo.Dotnet.Templates.*.nupkg").Name
