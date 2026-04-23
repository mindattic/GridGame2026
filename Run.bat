@echo off
cd /d "%~dp0"

set "EXE=Build\Windows\GridGame.exe"
set "UNITY=C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe"

if not exist "%EXE%" (
    echo Standalone build not found. Building...
    if not exist "%UNITY%" (
        echo Unity editor not found at:
        echo   %UNITY%
        pause
        exit /b 1
    )
    "%UNITY%" -batchmode -nographics -projectPath . -executeMethod CliEntryPoints.BuildStandaloneWindows -quit -logFile -
    if errorlevel 1 (
        echo Build failed.
        pause
        exit /b 1
    )
)

start "" "%EXE%"
