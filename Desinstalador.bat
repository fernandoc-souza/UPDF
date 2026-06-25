@echo off
setlocal

echo ==============================================
echo   DESINSTALADOR DA UNIAO PDF FCS (UPDF)
echo ==============================================

:: Verificar privilégios de Administrador
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo.
    echo Solicitando privilegios de administrador...
    powershell -Command "Start-Process cmd -ArgumentList '/c %~s0' -Verb RunAs"
    exit
)

set "INSTALL_DIR=%ProgramFiles%\UPDF"

echo [1/2] Removendo atalhos do sistema...
if exist "%USERPROFILE%\Desktop\UPDF.lnk" del /F /Q "%USERPROFILE%\Desktop\UPDF.lnk"
if exist "%ProgramData%\Microsoft\Windows\Start Menu\Programs\UPDF.lnk" del /F /Q "%ProgramData%\Microsoft\Windows\Start Menu\Programs\UPDF.lnk"

echo [2/2] Removendo arquivos do programa...
if exist "%INSTALL_DIR%" (
    rmdir /S /Q "%INSTALL_DIR%"
)

echo.
echo ==============================================
echo    DESINSTALACAO CONCLUIDA COM SUCESSO!
echo ==============================================
echo Todos os arquivos do UPDF foram removidos.
echo Pressione qualquer tecla para sair...
pause >nul
