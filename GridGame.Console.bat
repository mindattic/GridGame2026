@echo off
set "ROOT=%~dp0"

:: Run the GridGame menu in this tab
title Main Menu
powershell -NoExit -ExecutionPolicy Bypass -File "%~dp0GridGame.Console.ps1"
