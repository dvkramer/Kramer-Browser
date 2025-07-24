@echo off
echo =======================================================
echo         KRAMER BROWSER UPDATE AND BUILD SCRIPT
echo =======================================================

:: Navigate to the source directory
cd C:\Chromium\src

:: --- STEP 1: Update Chromium to the latest version ---
echo.
echo [1/4] Updating Chromium source code...
git rebase-update
gclient sync -D

:: Check if the update was successful before proceeding
if errorlevel 1 (
    echo ERROR: Failed to update Chromium. Aborting.
    goto :eof
)

echo.
echo [2/4] Applying Kramer Browser patches...
:: --- STEP 2: Apply all patches from your patches folder ---
:: This loop finds every file ending in .patch in your folder and applies it.
for %%f in ("C:\Chromium\src\patches\*.patch") do (
    echo Applying patch: %%~nxf
    git apply --reject "%%f"
    if errorlevel 1 (
        echo.
        echo ***************************************************************
        echo CRITICAL ERROR: Patch "%%~nxf" failed to apply.
        echo This usually means the underlying Chromium code has changed.
        echo You will need to remake this patch.
        echo See the .rej files to see what failed.
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