@echo off
setlocal enabledelayedexpansion

:: Self-elevation code
>nul 2>&1 "%SYSTEMROOT%\system32\cacls.exe" "%SYSTEMROOT%\system32\config\system"
if '%errorlevel%' NEQ '0' (
    echo Set UAC = CreateObject^("Shell.Application"^) > "%temp%\getadmin.vbs"
    echo UAC.ShellExecute "%~s0", "", "", "runas", 1 >> "%temp%\getadmin.vbs"
    "%temp%\getadmin.vbs"
    del "%temp%\getadmin.vbs"
    exit /B
)


REM Define variables
set "download_url=https://github.com/GyanD/codexffmpeg/releases/download/7.1/ffmpeg-7.1-full_build.zip"
set "temp_file=%TEMP%\ffmpeg.zip"
set "extract_dir=%LOCALAPPDATA%\ffmpeg"
set "full_build_dir=%extract_dir%\ffmpeg-7.1-full_build"
set "bin_dir=%full_build_dir%\bin"

REM Create the directory if it doesn't exist
if not exist "%extract_dir%" (
    mkdir "%extract_dir%"
)

REM Download the ffmpeg binary
echo Downloading ffmpeg...
powershell -Command "Invoke-WebRequest -Uri '%download_url%' -OutFile '%temp_file%'"

REM Extract the files
echo Extracting files...
powershell -Command "Expand-Archive -Path '%temp_file%' -DestinationPath '%extract_dir%' -Force"

REM Add to System Path
set "path=%PATH%;%bin_dir%"
setx PATH "%path%" /M

REM Clean up
del "%temp_file%"

echo ffmpeg has been installed and added to the system path.
pause