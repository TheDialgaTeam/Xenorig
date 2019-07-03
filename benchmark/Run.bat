@echo off

SETLOCAL 

SET project=../TheDialgaTeam.Xiropht.Xirorig.Benchmark/TheDialgaTeam.Xiropht.Xirorig.Benchmark.Builder.csproj
SET configuration=Release
SET framework=net461

dotnet run --project %project% -c %configuration% --framework %framework%

ENDLOCAL

pause