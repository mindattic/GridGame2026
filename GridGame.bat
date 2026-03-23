@echo off
set "ROOT=%~dp0"

:: Open Claude CLI tabs in the current Windows Terminal window
wt -w 0 new-tab --title "1" --suppressApplicationTitle -d "%ROOT%." -- claude
wt -w 0 new-tab --title "2" --suppressApplicationTitle -d "%ROOT%." -- claude
wt -w 0 new-tab --title "3" --suppressApplicationTitle -d "%ROOT%." -- claude
wt -w 0 new-tab --title "4" --suppressApplicationTitle -d "%ROOT%." -- claude
wt -w 0 new-tab --title "5" --suppressApplicationTitle -d "%ROOT%." -- claude

:: Run the GridGame menu in this tab
title Main Menu
powershell -ExecutionPolicy Bypass -File "%~dp0GridGame.ps1"
