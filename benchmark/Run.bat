@echo off

SETLOCAL 

SET project=../TheDialgaTeam.Xiropht.Xirorig.Benchmark/TheDialgaTeam.Xiropht.Xirorig.Benchmark.csproj
SET configuration=Release
SET framework=netcoreapp5.0

dotnet run --project %project% -c %configuration% --framework %framework%

ENDLOCAL

pause