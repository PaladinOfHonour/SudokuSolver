@echo off
for /d /r %%a in (*) do if /i "%%~nxa" == "Release" if /i "%%a\.." != "obj" set "folderpath=%%a"
cd %folderpath%
for %%i in (*.exe) do start "Test" /b "%%i" 2 2000 50
pause