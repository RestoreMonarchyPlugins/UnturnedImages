@echo off
REM Unturned Icon Generator Launcher
REM This script runs the PowerShell icon generator with default settings.
REM
REM Usage: Just double-click this file to start!
REM
REM Before running:
REM 1. Make sure UnturnedImages module is installed
REM 2. Edit config.json in your Unturned folder to enable AutoStart

cd /d "%~dp0"

echo.
echo ========================================
echo   Unturned Icon Generator
echo ========================================
echo.
echo Starting PowerShell script...
echo.

powershell.exe -ExecutionPolicy Bypass -NoProfile -File "%~dp0RunIconGenerator.ps1"

echo.
pause
