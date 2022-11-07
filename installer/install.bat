@echo off
@setlocal enableextensions
@cd /d "%~dp0"

:: set /p "id=Do you have a mailgun account? y/[N] "

mkdir "C:\Program Files\IPFetch"

ROBOCOPY "./" "C:\Program Files\IPFetch" /mir

sc create "IPFetch" binpath="C:\Program Files\IPFetch\IPFetch.exe" start=auto
sc start "IPFetch"

@pause
