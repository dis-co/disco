@ECHO OFF
SETLOCAL
SET allargs=%*
SET subcommand=%1
CALL SET restargs=%%allargs:*%1=%%
src\Iris\bin\Debug\Iris\iris.exe %subcommand% --http=src/Iris/assets/frontend %restargs%
