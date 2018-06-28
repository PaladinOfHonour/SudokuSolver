@echo off
for /d /r %%a in (bin\*) do if /i "%%~nxa" == "Release" set "folderpath=%%a"
cd %folderpath%
echo Folder path found: %folderpath%
for %%p in (*.exe) do (
	for /l %%j in (1, 1, 10) do (
		for /l %%s in (10, 10, 150) do (
			for /l %%i in (1, 1, 10) do (
				start "Running..." /b /w "%%p" 2 600000 "%%s" "%%j"
				timeout 1
			)
		)
	)
)