@echo off
for /d /r %%a in (bin\*) do if /i "%%~nxa" == "Release" set "folderpath=%%a"
cd %folderpath%
echo "Folder path found: %folderpath%"
for %%i in (*.exe) do for %%s in (1, 1, 400) do (
	start "Running..." /b "%%i" "%%s" 600000 50
	timeout 1
)