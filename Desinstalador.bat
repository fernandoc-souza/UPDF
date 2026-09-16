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

echo [1/3] Removendo atalhos do sistema...
if exist "%USERPROFILE%\Desktop\UPDF.lnk" del /F /Q "%USERPROFILE%\Desktop\UPDF.lnk"
if exist "%ProgramData%\Microsoft\Windows\Start Menu\Programs\UPDF.lnk" del /F /Q "%ProgramData%\Microsoft\Windows\Start Menu\Programs\UPDF.lnk"

echo [2/3] Removendo arquivos do programa...
if exist "%INSTALL_DIR%" (
    rmdir /S /Q "%INSTALL_DIR%"
)

echo [3/3] Removendo chaves de registro e associacoes...
reg delete "HKCR\UPDF.Document" /f >nul 2>&1
reg delete "HKCR\Applications\UPDF.exe" /f >nul 2>&1

:: Opcional: Remover a associação do .pdf se ainda estiver apontando pro UPDF
for /f "tokens=3*" %%a in ('reg query "HKCR\.pdf" /ve 2^>nul ^| find "REG_SZ"') do set "CURRENT_ASSOC=%%a"
if "%CURRENT_ASSOC%"=="UPDF.Document" (
    reg delete "HKCR\.pdf" /ve /f >nul 2>&1
)

echo.
echo ==============================================
echo    DESINSTALACAO CONCLUIDA COM SUCESSO!
echo ==============================================
echo Todos os arquivos do UPDF foram removidos.
echo Pressione qualquer tecla para sair...
pause >nul
