@echo off
setlocal

echo Building BrowserConnect PowerToys Run Plugin...
echo.

REM Move to repository root
cd /d "%~dp0.."

echo Cleaning up previous build artifacts...
rd /s /q "Community.PowerToys.Run.Plugin.BrowserConnect\bin" >nul 2>&1
rd /s /q "Community.PowerToys.Run.Plugin.BrowserConnect\obj" >nul 2>&1

REM Check if .NET SDK is installed
dotnet --version >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: .NET SDK not found.
    pause
    exit /b 1
)

echo Building plugin...
dotnet build "Community.PowerToys.Run.Plugin.BrowserConnect\BrowserConnect.csproj" -c Release

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Build failed.
    pause
    exit /b 1
)

echo.
echo Build succeeded.