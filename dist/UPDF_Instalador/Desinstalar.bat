@echo off
setlocal EnableExtensions

REM ---- Auto-elevacao para Administrador ----
net session >nul 2>&1
if %errorlevel% neq 0 (
    powershell -WindowStyle Hidden -Command "Start-Process -Verb RunAs -FilePath '%~f0'"
    exit /b
)

set "DEST=%ProgramFiles%\UPDF"

echo ============================================
echo   Desinstalador - Uniao PDF FCS (UPDF)
echo ============================================
echo.

REM Encerra o programa se estiver aberto
taskkill /IM UPDF.exe /F >nul 2>&1

REM Remove atalhos
del "%PUBLIC%\Desktop\UPDF.lnk" >nul 2>&1
del "%USERPROFILE%\Desktop\UPDF.lnk" >nul 2>&1
del "%ProgramData%\Microsoft\Windows\Start Menu\Programs\UPDF.lnk" >nul 2>&1

REM Remove registro de Adicionar/Remover Programas
reg delete "HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\UPDF" /f >nul 2>&1

REM Remove o executavel
del /F /Q "%DEST%\UPDF.exe" >nul 2>&1

echo UPDF foi desinstalado.
echo.
pause

REM Remove a pasta (inclusive este .bat) apos a saida, em processo separado
start "" /min cmd /c "timeout /t 2 >nul & rmdir /S /Q "%DEST%""
exit /b
