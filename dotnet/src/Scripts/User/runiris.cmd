REM @ECHO OFF
REM SETLOCAL
REM SET allargs=%*
REM SET subcommand=%1
REM CALL SET restargs=%%allargs:*%1=%%
REM Iris\iris.exe %subcommand% %restargs%
Iris\iris.exe start --project=iris_sample --frontend=Frontend --bind=%1
