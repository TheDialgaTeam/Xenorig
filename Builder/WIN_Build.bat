@echo off

SETLOCAL 

SET project=../src/TheDialgaTeam.Xiropht.Xirorig.Builder.csproj
SET configuration=Release

SET framework_core=netcoreapp3.0
SET framework_mono=net462

SET output=../bin/Release

dotnet publish %project% -c %configuration% -f %framework_mono% -o %output%/mono-portable
dotnet publish %project% /p:PublishSingleFile=true -c %configuration% -f %framework_core% -o %output%/net-core-linux-arm -r linux-arm --self-contained
dotnet publish %project% /p:PublishSingleFile=true -c %configuration% -f %framework_core% -o %output%/net-core-linux-x64 -r linux-x64 --self-contained
dotnet publish %project% /p:PublishSingleFile=true -c %configuration% -f %framework_core% -o %output%/net-core-osx-x64 -r osx-x64 --self-contained
dotnet publish %project% /p:PublishSingleFile=true -c %configuration% -f %framework_core% -o %output%/net-core-win-arm -r win-arm --self-contained
dotnet publish %project% /p:PublishSingleFile=true -c %configuration% -f %framework_core% -o %output%/net-core-win-x64 -r win-x64 --self-contained
dotnet publish %project% /p:PublishSingleFile=true -c %configuration% -f %framework_core% -o %output%/net-core-win-x86 -r win-x86 --self-contained

"%~dp0/7z/7za.exe" a %output%/mono-portable.zip %output%/mono-portable
"%~dp0/7z/7za.exe" a %output%/net-core-linux-arm.zip %output%/net-core-linux-arm
"%~dp0/7z/7za.exe" a %output%/net-core-linux-x64.zip %output%/net-core-linux-x64
"%~dp0/7z/7za.exe" a %output%/net-core-osx-x64.zip %output%/net-core-osx-x64
"%~dp0/7z/7za.exe" a %output%/net-core-win-arm.zip %output%/net-core-win-arm
"%~dp0/7z/7za.exe" a %output%/net-core-win-x64.zip %output%/net-core-win-x64
"%~dp0/7z/7za.exe" a %output%/net-core-win-x86.zip %output%/net-core-win-x86

ENDLOCAL