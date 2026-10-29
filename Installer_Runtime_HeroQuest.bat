@echo off
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Installer_Runtime_HeroQuest.ps1"
pause
