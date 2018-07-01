@echo off
for /d /r %%a in (bin\*) do if /i "%%~nxa" == "Debug" set "folderpath=%%a"
cd %folderpath%
echo Folder path found: %folderpath%
for %%p in (*.exe) do (
	for /l %%j in (1, 1, 11) do (
		for /l %%i in (1, 1, 10) do (
			start "Running..." /b /w Sudoku_solver.exe 4 "%%j"
			timeout 1
		)
	)
)