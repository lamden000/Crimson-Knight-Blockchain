@echo off
echo ========================================
echo Starting HTTP Server for Withdraw Web
echo ========================================
echo.
echo Server will start at: http://localhost:8000
echo Press Ctrl+C to stop the server
echo.

cd /d "%~dp0"

REM Try Python first
python --version >nul 2>&1
if %errorlevel% == 0 (
    echo Using Python HTTP Server...
    python -m http.server 8000
    goto :end
)

REM Try Python 3
python3 --version >nul 2>&1
if %errorlevel% == 0 (
    echo Using Python3 HTTP Server...
    python3 -m http.server 8000
    goto :end
)

REM Try Node.js http-server
where npx >nul 2>&1
if %errorlevel% == 0 (
    echo Using Node.js http-server...
    npx http-server -p 8000
    goto :end
)

echo.
echo ERROR: No HTTP server found!
echo.
echo Please install one of the following:
echo 1. Python (https://www.python.org/)
echo 2. Node.js (https://nodejs.org/) with npx
echo.
echo Or manually start a server in this directory.
echo.
pause

:end

