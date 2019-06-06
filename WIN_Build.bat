SETLOCAL 

SET project=./src/TheDialgaTeam.Xiropht.Xirorig.csproj
SET configuration=Release

SET framework_core=netcoreapp2.2
SET framework_mono=net461

SET output=./bin/Release

dotnet publish %project% -c %configuration% -f %framework_mono% -o %output%/mono-portable
dotnet publish %project% -c %configuration% -f %framework_core% -o %output%/net-core-linux-arm -r linux-arm --self-contained
dotnet publish %project% -c %configuration% -f %framework_core% -o %output%/net-core-linux-x64 -r linux-x64 --self-contained
dotnet publish %project% -c %configuration% -f %framework_core% -o %output%/net-core-osx-x64 -r osx-x64 --self-contained
dotnet publish %project% -c %configuration% -f %framework_core% -o %output%/net-core-win-arm -r win-arm --self-contained
dotnet publish %project% -c %configuration% -f %framework_core% -o %output%/net-core-win-x64 -r win-x64 --self-contained
dotnet publish %project% -c %configuration% -f %framework_core% -o %output%/net-core-win-x86 -r win-x86 --self-contained

ENDLOCAL

pause