@echo off
setlocal

cd /d "%~dp0windows-print-station"

where dotnet >nul 2>nul
if errorlevel 1 (
  echo .NET is not installed or is not on PATH.
  echo Install the .NET Desktop Runtime or run Publish Print Station.cmd on a development PC first.
  pause
  exit /b 1
)

dotnet run --configuration Release
if errorlevel 1 pause
