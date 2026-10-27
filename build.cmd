@echo off
rem Build the TCP debug assistant without .NET SDK.
rem Usage: build.cmd [-Test] [-Run]
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1" %*
exit /b %ERRORLEVEL%
