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
set "CLASSES=HKLM\Software\Classes"

echo [1/4] Encerrando o programa e removendo atalhos...
taskkill /F /IM UPDF.exe /T >nul 2>&1
if exist "%USERPROFILE%\Desktop\UPDF.lnk" del /F /Q "%USERPROFILE%\Desktop\UPDF.lnk"
if exist "%ProgramData%\Microsoft\Windows\Start Menu\Programs\UPDF.lnk" del /F /Q "%ProgramData%\Microsoft\Windows\Start Menu\Programs\UPDF.lnk"

echo [2/4] Removendo arquivos do programa...
if exist "%INSTALL_DIR%" (
    rmdir /S /Q "%INSTALL_DIR%"
)

echo [3/4] Removendo registro e associacoes de PDF...
reg delete "%CLASSES%\UPDF.Document" /f >nul 2>&1
reg delete "%CLASSES%\Applications\UPDF.exe" /f >nul 2>&1
reg delete "%CLASSES%\.pdf\OpenWithProgids" /v "UPDF.Document" /f >nul 2>&1
reg delete "HKLM\Software\UPDF" /f >nul 2>&1
reg delete "HKLM\Software\RegisteredApplications" /v "UPDF" /f >nul 2>&1

:: Instaladores antigos gravavam o UPDF como leitor padrao aqui. Se ainda estiver
:: apontando pro UPDF, limpa para nao deixar o .pdf orfao de um ProgID inexistente.
for /f "tokens=3*" %%a in ('reg query "%CLASSES%\.pdf" /ve 2^>nul ^| find "REG_SZ"') do set "CURRENT_ASSOC=%%a"
if "%CURRENT_ASSOC%"=="UPDF.Document" (
    reg delete "%CLASSES%\.pdf" /ve /f >nul 2>&1
)

:: Preferencias por usuario (config.json fica em %LOCALAPPDATA% de cada conta)
if exist "%LOCALAPPDATA%\UPDF" rmdir /S /Q "%LOCALAPPDATA%\UPDF" >nul 2>&1

echo [4/4] Atualizando o Explorador de Arquivos...
ie4uinit.exe -show >nul 2>&1

echo.
echo ==============================================
echo    DESINSTALACAO CONCLUIDA COM SUCESSO!
echo ==============================================
echo Todos os arquivos do UPDF foram removidos.
echo Pressione qualquer tecla para sair...
pause >nul
