@echo off
setlocal enabledelayedexpansion

echo Creating BrowserConnect release packages...
echo.

cd /d "%~dp0.."

REM Read version from plugin.json
for /f "tokens=*" %%v in ('powershell -Command "(Get-Content 'Community.PowerToys.Run.Plugin.BrowserConnect\plugin.json' | ConvertFrom-Json).Version"') do (
    set VERSION=%%v
)

if "%VERSION%"=="" (
    echo ERROR: Could not read version from plugin.json
    pause
    exit /b 1
)

echo Version: %VERSION%
echo.

set RELEASE_DIR=release

if exist "%RELEASE_DIR%" (
    rd /s /q "%RELEASE_DIR%"
)

mkdir "%RELEASE_DIR%"

for %%A in (x64 ARM64) do (

    echo Creating %%A package...

    set SOURCE=Community.PowerToys.Run.Plugin.BrowserConnect\bin\%%A\Release\net9.0-windows
    set TEMP_DIR=%RELEASE_DIR%\BrowserConnect

    mkdir "!TEMP_DIR!"

    copy /Y "!SOURCE!\Community.PowerToys.Run.Plugin.BrowserConnect.dll" "!TEMP_DIR!\" >nul
    copy /Y "!SOURCE!\Community.PowerToys.Run.Plugin.BrowserConnect.runtimeconfig.json" "!TEMP_DIR!\" >nul
    copy /Y "!SOURCE!\Community.PowerToys.Run.Plugin.BrowserConnect.deps.json" "!TEMP_DIR!\" >nul
    copy /Y "Community.PowerToys.Run.Plugin.BrowserConnect\plugin.json" "!TEMP_DIR!\" >nul

    if exist "!SOURCE!\Images" (
        xcopy "!SOURCE!\Images" "!TEMP_DIR!\Images\" /E /I /Y >nul
    )

    powershell -Command "Compress-Archive -Path '!TEMP_DIR!' -DestinationPath '%RELEASE_DIR%\BrowserConnect-%VERSION%-%%A.zip'"

    if !ERRORLEVEL! NEQ 0 (
        echo Failed creating %%A package.
        pause
        exit /b 1
    )

    rd /s /q "!TEMP_DIR!"
)

echo.
echo Computing SHA256 hashes...

(
    echo SHA256 Checksums
    echo ============================
    echo.

    echo BrowserConnect-%VERSION%-x64.zip:
    certutil -hashfile "%RELEASE_DIR%\BrowserConnect-%VERSION%-x64.zip" SHA256

    echo.
    echo BrowserConnect-%VERSION%-ARM64.zip:
    certutil -hashfile "%RELEASE_DIR%\BrowserConnect-%VERSION%-ARM64.zip" SHA256

) > "%RELEASE_DIR%\checksums.txt"

echo.
echo Release files:
dir "%RELEASE_DIR%"

echo.
echo Done.