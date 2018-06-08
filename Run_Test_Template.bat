@echo off
for /d /r %%a in (bin\*) do if /i "%%~nxa" == "Release" set "folderpath=%%a"
cd %folderpath%
for %%i in (*.exe) do start "Test" /b "%%i" 2 2000 50