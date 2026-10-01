@echo off
setlocal

echo ==============================================
echo   UPDF - REPARAR MENU "ABRIR COM"
echo ==============================================
echo.
echo Use este script quando o UPDF ja esta instalado mas nao aparece
echo ao clicar com o botao direito num PDF, em "Abrir com".
echo Nao reinstala nada e nao mexe no seu leitor de PDF padrao.
echo.

:: Verificar privilégios de Administrador
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo Solicitando privilegios de administrador...
    powershell -Command "Start-Process cmd -ArgumentList '/c %~s0' -Verb RunAs"
    exit
)

set "INSTALL_DIR=%ProgramFiles%\UPDF"
set "CLASSES=HKLM\Software\Classes"

if not exist "%INSTALL_DIR%\UPDF.exe" (
    echo ERRO: nao encontrei o UPDF em "%INSTALL_DIR%".
    echo Rode o Instalador.bat primeiro.
    echo.
    pause
    exit /b 1
)

echo Registrando...

reg add "%CLASSES%\UPDF.Document" /ve /d "Documento PDF" /f >nul
reg add "%CLASSES%\UPDF.Document" /v FriendlyTypeName /t REG_SZ /d "Documento PDF" /f >nul
reg add "%CLASSES%\UPDF.Document\DefaultIcon" /ve /d "\"%INSTALL_DIR%\UPDF.exe\",0" /f >nul
reg add "%CLASSES%\UPDF.Document\shell\open\command" /ve /d "\"%INSTALL_DIR%\UPDF.exe\" \"%%1\"" /f >nul

reg add "%CLASSES%\Applications\UPDF.exe" /v FriendlyAppName /t REG_SZ /d "Uniao PDF FCS (UPDF)" /f >nul
reg add "%CLASSES%\Applications\UPDF.exe\shell\open\command" /ve /d "\"%INSTALL_DIR%\UPDF.exe\" \"%%1\"" /f >nul
reg add "%CLASSES%\Applications\UPDF.exe\SupportedTypes" /v ".pdf" /t REG_SZ /f >nul

reg add "%CLASSES%\.pdf\OpenWithProgids" /v "UPDF.Document" /t REG_NONE /f >nul

reg add "HKLM\Software\UPDF\Capabilities" /v ApplicationName /t REG_SZ /d "Uniao PDF FCS (UPDF)" /f >nul
reg add "HKLM\Software\UPDF\Capabilities" /v ApplicationDescription /t REG_SZ /d "Visualizador, editor e assinador de documentos PDF." /f >nul
reg add "HKLM\Software\UPDF\Capabilities\FileAssociations" /v ".pdf" /t REG_SZ /d "UPDF.Document" /f >nul
reg add "HKLM\Software\RegisteredApplications" /v "UPDF" /t REG_SZ /d "Software\UPDF\Capabilities" /f >nul

ie4uinit.exe -show >nul 2>&1

echo.
echo ==============================================
echo   PRONTO!
echo ==============================================
echo Clique com o botao direito em qualquer PDF: o UPDF deve estar
echo na lista de "Abrir com".
echo.
echo Se ainda nao aparecer, feche e abra o Explorador de Arquivos
echo (ou reinicie o computador) e tente de novo.
echo.
pause >nul
