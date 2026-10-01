@echo off
setlocal

echo ==============================================
echo   BEM-VINDO AO INSTALADOR DA UNIAO PDF FCS
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

echo [1/5] Criando pasta de destino...
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

echo [2/5] Copiando executavel, icones e scripts...
taskkill /F /IM UPDF.exe /T >nul 2>&1
copy /Y "%~dp0UPDF.exe" "%INSTALL_DIR%\UPDF.exe" >nul
copy /Y "%~dp0app_icon.png" "%INSTALL_DIR%\app_icon.png" >nul
copy /Y "%~dp0Desinstalador.bat" "%INSTALL_DIR%\Desinstalador.bat" >nul

echo [3/5] Criando atalhos no sistema...
set "VBS_FILE=%TEMP%\CreateShortcut.vbs"

echo Set oWS = WScript.CreateObject^("WScript.Shell"^) > "%VBS_FILE%"
echo sLinkFile = oWS.ExpandEnvironmentStrings^("%USERPROFILE%\Desktop\UPDF.lnk"^) >> "%VBS_FILE%"
echo Set oLink = oWS.CreateShortcut^(sLinkFile^) >> "%VBS_FILE%"
echo oLink.TargetPath = "%INSTALL_DIR%\UPDF.exe" >> "%VBS_FILE%"
echo oLink.WorkingDirectory = "%INSTALL_DIR%" >> "%VBS_FILE%"
echo oLink.Save >> "%VBS_FILE%"

echo sLinkFile2 = oWS.ExpandEnvironmentStrings^("%ProgramData%\Microsoft\Windows\Start Menu\Programs\UPDF.lnk"^) >> "%VBS_FILE%"
echo Set oLink2 = oWS.CreateShortcut^(sLinkFile2^) >> "%VBS_FILE%"
echo oLink2.TargetPath = "%INSTALL_DIR%\UPDF.exe" >> "%VBS_FILE%"
echo oLink2.WorkingDirectory = "%INSTALL_DIR%" >> "%VBS_FILE%"
echo oLink2.Save >> "%VBS_FILE%"

cscript /nologo "%VBS_FILE%"
del "%VBS_FILE%"

echo [4/5] Registrando o UPDF para abrir arquivos PDF...

:: ---- ProgID: descreve o tipo "documento PDF aberto pelo UPDF" ----
reg add "%CLASSES%\UPDF.Document" /ve /d "Documento PDF" /f >nul
reg add "%CLASSES%\UPDF.Document" /v FriendlyTypeName /t REG_SZ /d "Documento PDF" /f >nul
reg add "%CLASSES%\UPDF.Document\DefaultIcon" /ve /d "\"%INSTALL_DIR%\UPDF.exe\",0" /f >nul
reg add "%CLASSES%\UPDF.Document\shell\open\command" /ve /d "\"%INSTALL_DIR%\UPDF.exe\" \"%%1\"" /f >nul

:: ---- Applications: e ESTA chave que coloca o UPDF no menu "Abrir com" ----
:: Sem ela o programa so aparece pra quem ja escolheu o UPDF manualmente uma vez.
reg add "%CLASSES%\Applications\UPDF.exe" /v FriendlyAppName /t REG_SZ /d "Uniao PDF FCS (UPDF)" /f >nul
reg add "%CLASSES%\Applications\UPDF.exe\shell\open\command" /ve /d "\"%INSTALL_DIR%\UPDF.exe\" \"%%1\"" /f >nul
reg add "%CLASSES%\Applications\UPDF.exe\SupportedTypes" /v ".pdf" /t REG_SZ /f >nul

:: ---- Oferece o UPDF para .pdf sem roubar o leitor padrao ja configurado ----
reg add "%CLASSES%\.pdf\OpenWithProgids" /v "UPDF.Document" /t REG_NONE /f >nul

:: ---- Faz o UPDF aparecer em Configuracoes > Aplicativos padrao ----
reg add "HKLM\Software\UPDF\Capabilities" /v ApplicationName /t REG_SZ /d "Uniao PDF FCS (UPDF)" /f >nul
reg add "HKLM\Software\UPDF\Capabilities" /v ApplicationDescription /t REG_SZ /d "Visualizador, editor e assinador de documentos PDF." /f >nul
reg add "HKLM\Software\UPDF\Capabilities\FileAssociations" /v ".pdf" /t REG_SZ /d "UPDF.Document" /f >nul
reg add "HKLM\Software\RegisteredApplications" /v "UPDF" /t REG_SZ /d "Software\UPDF\Capabilities" /f >nul

:: Versoes antigas do instalador sobrescreviam o leitor padrao do sistema aqui.
:: Isso nao funciona no Windows 10/11 (quem manda e a escolha do usuario) e so
:: atrapalhava, entao foi removido. O padrao agora se define pelo proprio Windows.

echo [5/5] Atualizando o Explorador de Arquivos...
ie4uinit.exe -show >nul 2>&1

echo.
echo ==============================================
echo       INSTALACAO CONCLUIDA COM SUCESSO!
echo ==============================================
echo O atalho 'UPDF' foi criado na Area de Trabalho e no Menu Iniciar.
echo.
echo O UPDF ja aparece ao clicar com o botao direito num PDF, em "Abrir com".
echo Para deixa-lo como leitor padrao: clique com o botao direito num PDF,
echo "Abrir com" ^> "Escolher outro aplicativo" ^> UPDF ^> "Sempre".
echo.
echo Pressione qualquer tecla para sair...
pause >nul
