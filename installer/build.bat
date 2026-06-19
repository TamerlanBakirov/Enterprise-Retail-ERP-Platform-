@echo off
echo === Georgia ERP Installer Build ===
echo.

echo [1/3] Publishing WPF app...
cd /d "%~dp0..\src\GeorgiaERP.Desktop"
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true -p:EnableCompressionInSingleFile=true -o bin\publish\win-x64
if errorlevel 1 (
    echo PUBLISH FAILED
    pause
    exit /b 1
)

echo.
echo [2/3] Building installer...
cd /d "%~dp0"
if exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" (
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" GeorgiaERP.iss
) else (
    echo ERROR: Inno Setup 6 not found. Install from https://jrsoftware.org/isdownload.php
    pause
    exit /b 1
)

echo.
echo [3/3] Done!
echo Installer: %~dp0output\GeorgiaERP_Setup_1.0.0.exe
echo.
pause
