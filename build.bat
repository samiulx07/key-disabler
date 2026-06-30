@echo off
setlocal enabledelayedexpansion

echo ===================================================
echo Building Key Disabler Application and Installer
echo ===================================================

echo.
echo [1/2] Publishing portable application...
dotnet publish src/KeyDisabler.App/KeyDisabler.App.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:EnableCompressionInSingleFile=true -o publish

if %ERRORLEVEL% neq 0 (
    echo [ERROR] dotnet publish failed!
    exit /b %ERRORLEVEL%
)

echo.
echo [2/2] Locating Inno Setup compiler...
set "ISCC_PATH="
if exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" set "ISCC_PATH=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if "%ISCC_PATH%"=="" if exist "C:\Program Files\Inno Setup 6\ISCC.exe" set "ISCC_PATH=C:\Program Files\Inno Setup 6\ISCC.exe"

if "%ISCC_PATH%"=="" goto no_iscc

echo Found Inno Setup compiler at: "%ISCC_PATH%"
echo Compiling installer...
"%ISCC_PATH%" "src\KeyDisabler.Installer\KeyDisabler.iss"

if %ERRORLEVEL% neq 0 (
    echo [ERROR] Inno Setup compilation failed!
    exit /b %ERRORLEVEL%
)

goto show_results

:no_iscc
echo [WARNING] Inno Setup 6 compiler (ISCC.exe) not found in standard paths.
echo Please compile src\KeyDisabler.Installer\KeyDisabler.iss manually.

:show_results
echo.
echo ===================================================
echo Build completed successfully!
echo.
echo Outputs:
echo - Portable executable: publish\KeyDisabler.exe
if not "%ISCC_PATH%"=="" echo - Setup installer:    installer-output\KeyDisablerSetup.exe
echo ===================================================
