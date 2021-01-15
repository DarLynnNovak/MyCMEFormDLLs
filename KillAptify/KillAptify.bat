@ECHO OFF
cd C:\Program Files\KillAptify
SET /A ProcessId=-1                                                                          
FOR /F %%T IN ('Wmic process where^(Name^="Aptify Shell.exe"^)get ProcessId^|more +1') DO (
SET /A ProcessId=%%T)                                                 
IF /I %ProcessId% GEQ 0 (
echo %ProcessId%
taskkill /F /PID %ProcessId%
)
@EXIT 0