@echo off
setlocal
cd /d "%~dp0"

set FOUND=0

rem Try the py launcher first (python.org installs register this). Prefer it
rem over the "python" command because Windows also ships a fake python.exe
rem placeholder on PATH for the Microsoft Store, which silently does nothing.
where py >nul 2>nul
if %ERRORLEVEL%==0 (
    py -3 serve.py
    set FOUND=1
    goto :done
)

where python >nul 2>nul
if %ERRORLEVEL%==0 (
    python serve.py
    set FOUND=1
    goto :done
)

where python3 >nul 2>nul
if %ERRORLEVEL%==0 (
    python3 serve.py
    set FOUND=1
    goto :done
)

:done
if %FOUND%==0 (
    echo Could not find Python.
    echo Install it from https://www.python.org/downloads/ and run this file again.
    echo During install, check the box "Add python.exe to PATH".
)

echo.
echo Press any key to close this window.
pause >nul
