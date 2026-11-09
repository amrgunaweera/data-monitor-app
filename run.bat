@echo off
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -Command "Unblock-File '.\DataMonitor.exe', '.\NetPulseMonitor.exe' -ErrorAction SilentlyContinue"
start "" ".\DataMonitor.exe" %*

