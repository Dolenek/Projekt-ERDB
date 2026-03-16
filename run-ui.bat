@echo off
setlocal

cd /d "%~dp0"

where dotnet >nul 2>&1
if errorlevel 1 (
  echo dotnet SDK was not found in PATH.
  echo Install the .NET SDK and try again.
  pause
  exit /b 1
)

echo Launching EpicRPGBot.UI...
dotnet run --project "%~dp0EpicRPGBot.UI\EpicRPGBot.UI.csproj" -c Debug
set "exit_code=%errorlevel%"

if not "%exit_code%"=="0" (
  echo.
  echo EpicRPGBot.UI exited with code %exit_code%.
  pause
)

exit /b %exit_code%
