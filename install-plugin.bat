@echo off
setlocal enabledelayedexpansion

echo Closing PowerToys...
set "RETRY_COUNT=0"
:KILL_LOOP
taskkill /f /im PowerToys.exe >nul 2>&1
tasklist /fi "IMAGENAME eq PowerToys.exe" | find /i "PowerToys.exe" >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    set /a RETRY_COUNT+=1
    if !RETRY_COUNT! LEQ 5 (
        echo PowerToys still running, retrying...
        timeout /t 2 /nobreak >nul
        goto :KILL_LOOP
    ) else (
        echo ERROR: Failed to kill PowerToys.exe. Please close it manually.
        pause
        exit /b 1
    )
)
echo PowerToys closed successfully.

echo Installing BrowserConnect plugin to PowerToys Run...
echo.

set PLUGIN_DIR=%LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins\BrowserConnect
set ENGINES_FILE=searchEngines.txt

REM Delete existing files to ensure a clean install, but PRESERVE history.txt and searchEngines.txt
if exist "%PLUGIN_DIR%" (
    echo Cleaning plugin directory while preserving history and engines...
    pushd "%PLUGIN_DIR%"

    :: Delete all files EXCEPT history.txt and searchEngines.txt
    for /f "delims=" %%f in ('dir /b /a-d ^| findstr /v /i /c:"history.txt" /c:"searchEngines.txt" /c:"google_api.txt"') do (
        del /f /q "%%f" >nul 2>&1
    )

    :: Delete Images folder for a clean image install
    if exist "Images" rd /s /q "Images" >nul 2>&1

    popd
    timeout /t 1 /nobreak >nul 2>&1
)

REM Create fresh plugin directory
if not exist "%PLUGIN_DIR%" mkdir "%PLUGIN_DIR%" 2>nul
echo Plugin directory: %PLUGIN_DIR%

set BUILD_OUTPUT=bin\Release\net9.0-windows

timeout /t 3 /nobreak >nul

REM Copy main plugin DLL
if exist "%BUILD_OUTPUT%\Community.PowerToys.Run.Plugin.BrowserConnect.dll" (
    copy /Y "%BUILD_OUTPUT%\Community.PowerToys.Run.Plugin.BrowserConnect.dll" "%PLUGIN_DIR%\"
    if %ERRORLEVEL% NEQ 0 (
        echo Retrying copying DLL in 3 seconds...
        timeout /t 3 /nobreak >nul
        copy /Y "%BUILD_OUTPUT%\Community.PowerToys.Run.Plugin.BrowserConnect.dll" "%PLUGIN_DIR%\"
        if %ERRORLEVEL% NEQ 0 (
            echo ERROR: Failed to copy DLL. Aborting.
            pause
            exit /b 1
        )
    )
    echo Copied DLL
) else (
    echo ERROR: DLL not found in %BUILD_OUTPUT%. Please run build-plugin.bat first.
    pause
    exit /b 1
)

REM Copy all dependency DLLs from build output (NuGet packages like Google.Apis, Newtonsoft.Json, etc.)
echo Copying dependency DLLs...
for %%f in ("%BUILD_OUTPUT%\*.dll") do (
    if /i not "%%~nxf"=="Community.PowerToys.Run.Plugin.BrowserConnect.dll" (
        copy /Y "%%f" "%PLUGIN_DIR%\" >nul
    )
)
echo Copied dependencies

REM Copy plugin.json
if exist "plugin.json" (
    copy /Y "plugin.json" "%PLUGIN_DIR%\"
    if %ERRORLEVEL% NEQ 0 (
        echo Retrying copying plugin.json in 3 seconds...
        timeout /t 3 /nobreak >nul
        copy /Y "plugin.json" "%PLUGIN_DIR%\"
    )
    echo Copied plugin.json
)

REM Copy Images directory (clean copy since we deleted it above)
if exist "Images" (
    mkdir "%PLUGIN_DIR%\Images"
    copy /Y "Images\*" "%PLUGIN_DIR%\Images\"
    if %ERRORLEVEL% NEQ 0 (
        echo Retrying copying Images in 3 seconds...
        timeout /t 3 /nobreak >nul
        copy /Y "Images\*" "%PLUGIN_DIR%\Images\"
    )
    echo Copied images
)

REM Copy runtimeconfig.json
if exist "%BUILD_OUTPUT%\Community.PowerToys.Run.Plugin.BrowserConnect.runtimeconfig.json" (
    copy /Y "%BUILD_OUTPUT%\Community.PowerToys.Run.Plugin.BrowserConnect.runtimeconfig.json" "%PLUGIN_DIR%\"
    echo Copied runtimeconfig.json
)

REM Copy deps.json
if exist "%BUILD_OUTPUT%\Community.PowerToys.Run.Plugin.BrowserConnect.deps.json" (
    copy /Y "%BUILD_OUTPUT%\Community.PowerToys.Run.Plugin.BrowserConnect.deps.json" "%PLUGIN_DIR%\"
    echo Copied deps.json
)

REM Only copy searchEngines.txt if it doesn't already exist (preserve user customizations)
if not exist "%PLUGIN_DIR%\searchEngines.txt" (
    if exist "%ENGINES_FILE%" (
        copy /Y "%ENGINES_FILE%" "%PLUGIN_DIR%\searchEngines.txt"
        if %ERRORLEVEL% EQU 0 (
            echo Copied searchEngines.txt successfully
        ) else (
            echo WARNING: Failed to copy searchEngines.txt
        )
    )
) else (
    echo Skipping searchEngines.txt - preserving user customizations
)

echo.
echo Plugin installed successfully!

echo Cleaning up build artifacts...
rd /s /q bin >nul 2>&1
rd /s /q obj >nul 2>&1

echo Starting PowerToys...
if exist "%LOCALAPPDATA%\PowerToys\PowerToys.exe" (
    start "" "%LOCALAPPDATA%\PowerToys\PowerToys.exe"
    echo PowerToys started.
) else (
    echo PowerToys.exe not found in standard location. Please start it manually.
)

echo.
exit