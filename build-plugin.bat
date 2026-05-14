@echo off
echo Building BrowserConnect PowerToys Run Plugin...
echo.

echo Cleaning up previous build artifacts...
rd /s /q bin >nul 2>&1
rd /s /q obj >nul 2>&1

REM Check if .NET SDK is installed
dotnet --version >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: .NET SDK not found. Please install .NET 9.0 SDK or later.
    pause
    exit /b 1
)

REM Build the plugin
echo Building plugin...
dotnet build BrowserConnect.csproj -c Release
