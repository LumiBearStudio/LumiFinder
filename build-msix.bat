@echo off
setlocal EnableDelayedExpansion

echo =========================================
echo   LumiFinder MSIX Store Upload Build (x64/x86/ARM64)
echo =========================================
echo.

set REPO_ROOT=D:\11.AI\LumiFiles
set MANIFEST=%REPO_ROOT%\src\LumiFiles\LumiFiles\Package.appxmanifest
set CSPROJ=%REPO_ROOT%\src\LumiFiles\LumiFiles\LumiFiles.csproj
set MSBUILD="C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"

:: -- Extract version from Package.appxmanifest --
for /f "usebackq delims=" %%V in (`powershell -NoProfile -Command "([xml](Get-Content '%MANIFEST%')).Package.Identity.Version"`) do set VER=%%V

if "%VER%"=="" (
    echo ERROR: Version not found in Package.appxmanifest
    pause
    exit /b 1
)

set OUTDIR=%REPO_ROOT%\AppPackages\VER_%VER%

:: -- SAFETY GUARD: validate OUTDIR is well-formed before any cleanup --
:: This prevents catastrophic Remove-Item recursion against repo root if
:: variable expansion goes wrong (e.g. encoding error scrambles 'set' lines).
echo %OUTDIR% | findstr /C:"AppPackages\VER_" >nul
if errorlevel 1 (
    echo ERROR: OUTDIR safety check failed: %OUTDIR%
    echo OUTDIR must contain the literal substring "AppPackages\VER_".
    pause
    exit /b 1
)

echo Version: %VER%
echo Output:  %OUTDIR%
echo.

if not exist "%OUTDIR%" mkdir "%OUTDIR%"

set FAILED=0

:: -- x64 Build --
echo [1/3] Building x64...
%MSBUILD% "%CSPROJ%" /restore /v:minimal ^
    /p:Configuration=Release /p:Platform=x64 ^
    /p:GenerateAppxPackageOnBuild=true /p:AppxBundle=Never ^
    /p:PublishTrimmed=false /p:SelfContained=true ^
    /p:UapAppxPackageBuildMode=StoreUpload ^
    /p:AppxPackageDir="%OUTDIR%\\"
if %ERRORLEVEL% NEQ 0 (
    echo [FAILED] x64 build failed
    set FAILED=1
) else (
    echo [OK] x64
)
echo.

:: -- x86 Build --
echo [2/3] Building x86...
%MSBUILD% "%CSPROJ%" /restore /v:minimal ^
    /p:Configuration=Release /p:Platform=x86 ^
    /p:GenerateAppxPackageOnBuild=true /p:AppxBundle=Never ^
    /p:PublishTrimmed=false /p:SelfContained=true ^
    /p:UapAppxPackageBuildMode=StoreUpload ^
    /p:AppxPackageDir="%OUTDIR%\\"
if %ERRORLEVEL% NEQ 0 (
    echo [FAILED] x86 build failed
    set FAILED=1
) else (
    echo [OK] x86
)
echo.

:: -- ARM64 Build --
echo [3/3] Building ARM64...
%MSBUILD% "%CSPROJ%" /restore /v:minimal ^
    /p:Configuration=Release /p:Platform=ARM64 ^
    /p:GenerateAppxPackageOnBuild=true /p:AppxBundle=Never ^
    /p:PublishTrimmed=false /p:SelfContained=true ^
    /p:UapAppxPackageBuildMode=StoreUpload ^
    /p:AppxPackageDir="%OUTDIR%\\"
if %ERRORLEVEL% NEQ 0 (
    echo [FAILED] ARM64 build failed
    set FAILED=1
) else (
    echo [OK] ARM64
)
echo.

:: -- Create ZIP packages for GitHub Release --
:: AssemblyName is 'LumiFiles', so MSBuild creates LumiFiles_*_Test folders.
:: We rename ZIP output to LumiFinder_v* prefix for consistency.
echo Creating ZIP packages for GitHub Release...
for %%P in (x64 x86 ARM64) do (
    if exist "%OUTDIR%\LumiFiles_%VER%_%%P_Test" (
        powershell -NoProfile -Command "Compress-Archive -Path '%OUTDIR%\LumiFiles_%VER%_%%P_Test\*' -DestinationPath '%OUTDIR%\LumiFinder_v%VER%_%%P.zip' -Force"
        echo [OK] LumiFinder_v%VER%_%%P.zip
    ) else (
        echo [SKIP] %%P test folder not found
    )
)
echo.

:: -- Cleanup: keep only .msixupload and .zip files inside OUTDIR --
:: SAFETY: pass OUTDIR via env var (LF_OUTDIR) instead of -param, because cmd
:: parameter quoting with PowerShell -Command can lose value across long lines.
:: PowerShell-side: triple validation (IsNullOrWhiteSpace + Test-Path + match
:: 'AppPackages\VER_') before any Remove-Item.
echo Cleaning up build artifacts...
set "LF_OUTDIR=%OUTDIR%"
powershell -NoProfile -Command ^
    "$d = $env:LF_OUTDIR; if ([string]::IsNullOrWhiteSpace($d) -or -not (Test-Path -LiteralPath $d -PathType Container)) { Write-Host '[ERROR] Invalid OutDir, skipping cleanup'; exit 1 }; if ($d -notmatch 'AppPackages\\VER_') { Write-Host '[ERROR] OutDir does not match AppPackages\\VER_*, skipping cleanup'; exit 1 }; Get-ChildItem -LiteralPath $d -Recurse -Force | Where-Object { -not $_.PSIsContainer } | Where-Object { $_.Extension -notin '.msixupload','.zip' } | Remove-Item -Force -ErrorAction SilentlyContinue; Get-ChildItem -LiteralPath $d -Directory -Force | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue; Write-Host ('[OK] Cleanup done in ' + $d)"
set "LF_OUTDIR="
echo.

:: -- Results --
if %FAILED%==0 (
    echo =========================================
    echo   ALL BUILDS SUCCESS - v%VER%
    echo =========================================
) else (
    echo =========================================
    echo   SOME BUILDS FAILED
    echo =========================================
)
echo.
echo Output: %OUTDIR%
echo.
echo --- MS Store uploads (.msixupload) ---
dir /b "%OUTDIR%\*.msixupload" 2>nul
echo.
echo --- GitHub Release (.zip) ---
dir /b "%OUTDIR%\*.zip" 2>nul
echo.
pause
