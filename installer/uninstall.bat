@echo off
@setlocal enableextensions
@cd /d "%~dp0"

SC STOP "IPFetch"

:: Hack to wait for 3 seconds before deleting the service
ping 192.0.2.2 -n 1 -w 3000 > nul

SC DELETE "IPFetch"

rmdir /S /Q "C:\Program Files\IPFetch"

@pause
