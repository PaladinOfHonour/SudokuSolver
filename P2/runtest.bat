@echo off
for /d /r %%a in (bin\*) do if /i "%%~nxa" == "Release" set "folderpath=%%a"
cd %folderpath%
echo Folder path found: %folderpath%
for %%p in (*.exe) do (
	for /l %%j in (1, 1, 11) do (
		for /l %%i in (1, 1, 10) do (
			start "Running..." /b /w "%%j" 4
			timeout 1
		)
	)
)