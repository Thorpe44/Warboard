@echo off
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0CLEAN_WARBOARD_PROJECT.ps1"
