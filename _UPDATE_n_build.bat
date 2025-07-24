@echo off
echo =======================================================
echo         KRAMER BROWSER UPDATE AND BUILD SCRIPT
echo          (Portable - Location Aware Version)
echo =======================================================

:: Place these files in chromium/src/patches
:: %~dp0 is a special variable that means "The Drive and Path of this script".
:: So, we first change the directory to wherever the script is running from.
:: The /d switch handles cases where the script might be on a different drive.
cd /d "%~dp0"

:: Since the script is in the "patches" folder, we go up one level to get to "src".
cd ..

echo Now running from: %cd%

:: --- STEP 1: Update Chromium to the latest version ---
echo.
echo [1/4] Updating Chromium source code...
gclient sync -D --no-history --shallow
git rebase-update

:: Check if the update was successful before proceeding
if errorlevel 1 (
    echo ERROR: Failed to update Chromium. Aborting.
    goto :eof
)

echo.
echo [2/4] Applying Kramer Browser patches...
:: --- STEP 2: Apply all patches from your patches folder ---
:: Now the script just looks for patches in its own folder.
for %%f in ("%~dp0*.patch") do (
    echo Applying patch: %%~nxf
    git apply --reject "%%f"
    if errorlevel 1 (
        echo.
        echo ***************************************************************
        echo CRITICAL ERROR: Patch "%%~nxf" failed to apply.
        echo This usually means the underlying Chromium code has changed.
        echo You will need to remake this patch. See the .rej files.
        echo ***************************************************************
        goto :eof
    )
)

echo.
echo [3/4] Regenerating build files with GN...
:: --- STEP 3: Regenerate the build configuration ---
gn gen out/Default

echo.
echo [4/4] Starting the build...
:: --- STEP 4: Build the browser ---
autoninja -C out/Default chrome

echo.
echo =======================================================
echo                  BUILD PROCESS FINISHED
echo =======================================================