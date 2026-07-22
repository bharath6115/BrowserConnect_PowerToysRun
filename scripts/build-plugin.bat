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

echo.
echo Building x64 plugin...
dotnet build "Community.PowerToys.Run.Plugin.BrowserConnect\BrowserConnect.csproj" -c Release -p:Platform=x64

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo x64 build failed.
    pause
    exit /b 1
)

echo.
echo Building ARM64 plugin...
dotnet build "Community.PowerToys.Run.Plugin.BrowserConnect\BrowserConnect.csproj" -c Release -p:Platform=ARM64

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ARM64 build failed.
    pause
    exit /b 1
)

echo.
echo Both x64 and ARM64 builds succeeded.