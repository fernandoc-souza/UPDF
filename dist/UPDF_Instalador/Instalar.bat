@echo off
setlocal EnableExtensions

REM ---- Auto-elevacao para Administrador ----
net session >nul 2>&1
if %errorlevel% neq 0 (
    powershell -WindowStyle Hidden -Command "Start-Process -Verb RunAs -FilePath '%~f0'"
    exit /b
)

set "DEST=%ProgramFiles%\UPDF"
set "SRC=%~dp0"

echo ============================================
echo   Instalador - Uniao PDF FCS (UPDF)
echo ============================================
echo.
echo Instalando em: "%DEST%"
echo.

REM Encerra instancia aberta, se houver
taskkill /IM UPDF.exe /F >nul 2>&1

if not exist "%DEST%" mkdir "%DEST%"

copy /Y "%SRC%UPDF.exe" "%DEST%\UPDF.exe" >nul
if errorlevel 1 (
    echo ERRO ao copiar o executavel. Instalacao cancelada.
    pause
    exit /b 1
)

REM Copia o desinstalador para a pasta de instalacao
copy /Y "%SRC%Desinstalar.bat" "%DEST%\Desinstalar.bat" >nul

REM ---- Atalho na Area de Trabalho (todos os usuarios) ----
powershell -NoProfile -Command "$w=New-Object -ComObject WScript.Shell; $p=[Environment]::GetFolderPath('CommonDesktopDirectory'); $s=$w.CreateShortcut($p+'\UPDF.lnk'); $s.TargetPath='%DEST%\UPDF.exe'; $s.WorkingDirectory='%DEST%'; $s.IconLocation='%DEST%\UPDF.exe,0'; $s.Description='Uniao PDF FCS'; $s.Save()"

REM ---- Atalho no Menu Iniciar (todos os usuarios) ----
powershell -NoProfile -Command "$w=New-Object -ComObject WScript.Shell; $p=[Environment]::GetFolderPath('CommonPrograms'); $s=$w.CreateShortcut($p+'\UPDF.lnk'); $s.TargetPath='%DEST%\UPDF.exe'; $s.WorkingDirectory='%DEST%'; $s.IconLocation='%DEST%\UPDF.exe,0'; $s.Description='Uniao PDF FCS'; $s.Save()"

REM ---- Registro em Aplicativos e Recursos (Adicionar/Remover) ----
set "UKEY=HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\UPDF"
reg add "%UKEY%" /v DisplayName     /t REG_SZ    /d "Uniao PDF FCS (UPDF)"        /f >nul
reg add "%UKEY%" /v DisplayVersion  /t REG_SZ    /d "1.1.0"                        /f >nul
reg add "%UKEY%" /v Publisher       /t REG_SZ    /d "Fernando CS"                  /f >nul
reg add "%UKEY%" /v DisplayIcon     /t REG_SZ    /d "%DEST%\UPDF.exe"              /f >nul
reg add "%UKEY%" /v InstallLocation /t REG_SZ    /d "%DEST%"                       /f >nul
reg add "%UKEY%" /v UninstallString /t REG_SZ    /d "\"%DEST%\Desinstalar.bat\""   /f >nul
reg add "%UKEY%" /v NoModify        /t REG_DWORD /d 1                              /f >nul
reg add "%UKEY%" /v NoRepair        /t REG_DWORD /d 1                              /f >nul

echo.
echo ============================================
echo   UPDF instalado com sucesso!
echo.
echo   - Atalho criado na Area de Trabalho
echo   - Atalho criado no Menu Iniciar
echo ============================================
echo.
pause
