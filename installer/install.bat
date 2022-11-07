@echo off
@setlocal enableextensions
@cd /d "%~dp0"

:: set /p "id=Do you have a mailgun account? y/[N] "

:: Create program files directory and move content into it
mkdir "C:\Program Files\IPFetch"
ROBOCOPY "./" "C:\Program Files\IPFetch" /mir

:: Create logs directory and allow local system access
mkdir "C:\Program Files\IPFetch\logs"
icacls "C:\Program Files\IPFetch\logs" /grant system:f /inheritance:e

sc create "IPFetch" binpath="C:\Program Files\IPFetch\IPFetch.exe" start=auto
sc start "IPFetch"

@pause
