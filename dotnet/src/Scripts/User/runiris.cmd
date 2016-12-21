@ECHO OFF
SETLOCAL
SET allargs=%*
SET subcommand=%1
CALL SET restargs=%%allargs:*%1=%%
Iris\iris.exe %subcommand% --http=Iris\assets %restargs%
