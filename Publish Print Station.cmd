@echo off
setlocal

cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
  echo .NET SDK is not installed or is not on PATH.
  echo Install the .NET SDK on this PC, or publish from another development PC.
  pause
  exit /b 1
)

for /f %%i in ('powershell -NoProfile -Command "Get-Date -Format yyyyMMdd-HHmmss"') do set STAMP=%%i
set "PUBLISH_DIR=%~dp0windows-print-station\bin\Release\net9.0-windows\win-x64\publish-%STAMP%"

dotnet publish ".\windows-print-station\CurrentRmsPrintStation.csproj" -c Release -r win-x64 --self-contained true -o "%PUBLISH_DIR%" /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
if errorlevel 1 (
  pause
  exit /b 1
)

echo.
echo Published app:
echo %PUBLISH_DIR%\CurrentRmsPrintStation.exe
echo.
pause
