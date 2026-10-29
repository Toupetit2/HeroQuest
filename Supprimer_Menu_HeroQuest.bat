@echo off
setlocal
cd /d "%~dp0"
echo Nettoyage du menu HeroQuest de l'editeur Unity...
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Masquer_Menu_HeroQuest.ps1"
set "CODE=%ERRORLEVEL%"
echo.
if not "%CODE%"=="0" (
  echo Le nettoyage a rencontre une erreur. Consultez le message ci-dessus.
) else (
  echo Vous pouvez maintenant revenir dans Unity.
)
pause
exit /b %CODE%
