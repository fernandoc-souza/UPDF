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

echo [1/3] Criando pasta de destino...
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

echo [2/3] Copiando executavel, icones e scripts...
taskkill /F /IM UPDF.exe /T >nul 2>&1
copy /Y "%~dp0UPDF.exe" "%INSTALL_DIR%\UPDF.exe" >nul
copy /Y "%~dp0app_icon.png" "%INSTALL_DIR%\app_icon.png" >nul
copy /Y "%~dp0Desinstalador.bat" "%INSTALL_DIR%\Desinstalador.bat" >nul

echo [3/3] Criando atalhos no sistema...
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

echo [4/4] Associando arquivos PDF ao UPDF...
reg add "HKCR\.pdf" /ve /d "UPDF.Document" /f >nul
reg add "HKCR\UPDF.Document" /ve /d "Documento PDF" /f >nul
reg add "HKCR\UPDF.Document\DefaultIcon" /ve /d "%INSTALL_DIR%\UPDF.exe,0" /f >nul
reg add "HKCR\UPDF.Document\shell\open\command" /ve /d "\"%INSTALL_DIR%\UPDF.exe\" \"%%1\"" /f >nul


echo.
echo ==============================================
echo       INSTALACAO CONCLUIDA COM SUCESSO!
echo ==============================================
echo O atalho 'UPDF' foi criado na Area de Trabalho e no Menu Iniciar.
echo Pressione qualquer tecla para sair...
pause >nul
