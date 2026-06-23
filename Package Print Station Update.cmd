@echo off
setlocal

cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
  echo .NET SDK is not installed or is not on PATH.
  pause
  exit /b 1
)

set "VPK_EXE=vpk"
where vpk >nul 2>nul
if errorlevel 1 (
  if exist "%USERPROFILE%\.dotnet\tools\vpk.exe" (
    set "VPK_EXE=%USERPROFILE%\.dotnet\tools\vpk.exe"
  ) else (
    echo Velopack CLI was not found. Installing vpk as a .NET global tool...
    dotnet tool install --global vpk --version 1.2.0
    if errorlevel 1 (
      pause
      exit /b 1
    )
    set "VPK_EXE=%USERPROFILE%\.dotnet\tools\vpk.exe"
  )
)

for /f "usebackq delims=" %%v in (`powershell -NoProfile -Command "([xml](Get-Content '.\windows-print-station\CurrentRmsPrintStation.csproj')).Project.PropertyGroup.Version"`) do set VERSION=%%v
if "%VERSION%"=="" (
  echo Could not read version from windows-print-station\CurrentRmsPrintStation.csproj.
  pause
  exit /b 1
)

set "BUILD_DIR=%~dp0windows-print-station\bin\Release\net9.0-windows\win-x64\velopack-build"
set "RELEASE_DIR=%~dp0Releases"

if exist "%BUILD_DIR%" rmdir /s /q "%BUILD_DIR%"

dotnet publish ".\windows-print-station\CurrentRmsPrintStation.csproj" -c Release -r win-x64 --self-contained true -o "%BUILD_DIR%" /p:PublishSingleFile=false
if errorlevel 1 (
  pause
  exit /b 1
)

"%VPK_EXE%" pack -u CurrentRmsPrintStation -v "%VERSION%" -p "%BUILD_DIR%" -e CurrentRmsPrintStation.exe -o "%RELEASE_DIR%" --packTitle "Current-RMS Print Station" --packAuthors "Limelite Lighting"
if errorlevel 1 (
  pause
  exit /b 1
)

echo.
echo Velopack release created in:
echo %RELEASE_DIR%
echo.
echo Upload the files from that folder to a GitHub Release tagged v%VERSION%.
echo.
pause
