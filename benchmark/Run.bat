@echo off

SETLOCAL 

SET project=../TheDialgaTeam.Xiropht.Xirorig.Benchmark/TheDialgaTeam.Xiropht.Xirorig.Benchmark.Builder.csproj
SET configuration=Release
SET framework=net462

dotnet run --project %project% -c %configuration% --framework %framework%

ENDLOCAL

pause