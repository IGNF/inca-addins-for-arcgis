@echo off
setlocal enabledelayedexpansion
chcp 65001 > nul


echo **********************************************************************
echo       SCRIPT DE PUBLICATION DES OUTILS "INCA-SDK POUR ARCGIS PRO"
echo                      - DEVELOPPEUR -
echo **********************************************************************
echo.
rem Ce script automatise la création d'un fichier d'installation du
rem package inca-sdk-pro développé par le projet INCA.

rem Il copie dans un dossier zippé tous les Add-ins, fichiers exécutables
rem et ressources nécessaires à l'installation des outils inca-sdk-pro.

rem *******************************************************************
rem Récupération du numéro de version
rem *******************************************************************

set DEFAULT_VERSION=x.x.x
set /p NUM_VERSION=Entrez le numéro de version (défaut: %DEFAULT_VERSION%):

if "%NUM_VERSION%"=="" set NUM_VERSION=%DEFAULT_VERSION%

rem *******************************************************************
rem Début du script de publication
rem *******************************************************************

rem On se positionne dans le répertoire du fichier batch
CD /d "%~dp0"

rem Nom du fichier à créer pour contenir la distribution
set OUTPUT_FILE_NAME=inca-sdk-pro-%NUM_VERSION%

rem Chemin (relatif) du dossier contenant la distribution
set OUTPUT_DIR=releases\%OUTPUT_FILE_NAME%
rem Chemin du fichier zippé
set ZIP_FILE=%OUTPUT_DIR%.zip

rem Chemin de la solution Visual Studio
set SOLUTION_DIR=code
set SOLUTION_PATH=%SOLUTION_DIR%\IncaProSDK.sln
rem Configuration de la compilation
set CONFIG=Release
set RUNTIME=win-x64

rem liste des fichiers et dossiers complémentaires à recopier
set RSC_COMP=rsc Changelog.md ReadMe.md user_install.bat

rem *******************************************************************
rem Vérification de l'existence du fichier d'archive de destination
rem *******************************************************************

if exist %ZIP_FILE% (
    echo Le fichier %ZIP_FILE% existe déjà.
    set /p overwrite="Souhaitez-vous le supprimer (Oui ou Non) ? "
    if /i "!overwrite!"=="Oui" (
        rmdir /s /q %ZIP_FILE%
    ) else (goto end)
)

rem suppression du dossier de destination non compressé
if exist %OUTPUT_DIR% (
	rmdir /s /q %OUTPUT_DIR%
)

echo.
rem *******************************************************************
echo Création du dossier d'accueil et de son arborescence
rem *******************************************************************

set ADDIN_TARGET=%OUTPUT_DIR%\add-ins
set BIN_INCA_TARGET=%OUTPUT_DIR%\bin_inca_sdk

mkdir "%OUTPUT_DIR%"
mkdir "%ADDIN_TARGET%"
mkdir "%BIN_INCA_TARGET%"

echo.
rem *******************************************************************
echo Compilation de la solution
rem *******************************************************************
echo.

rem Restauration de la solution
dotnet restore "%SOLUTION_PATH%"

rem Compilation
call "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Tools\VsDevCmd.bat"
MSBuild "%SOLUTION_PATH%" /t:Build /p:Configuration=%CONFIG% /p:Platform="Any CPU" /m /verbosity:quiet

if %errorlevel% neq 0 (
    echo Échec de la compilation!
    goto failed
)

echo.
rem *******************************************************************
echo Publication des exécutables auto-suffisants
rem *******************************************************************

set FOUND=0
for /r "%SOLUTION_DIR%" %%f in (*.csproj) do (
	findstr /i "<OutputType>.*Exe" "%%f" >nul
	if !errorlevel! == 0 (
		set FOUND=1
		echo     Publication de %%~nxf
		dotnet publish "%%f" -c %CONFIG% -r %RUNTIME% --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -v "quiet" -o %OUTPUT_DIR%\temp_publish
		if %errorlevel% neq 0 (
			echo Échec de la publication!
			goto failed
		)
	)
)

if %FOUND%==0 (
	echo    Aucun exécutable publiable trouvé
	goto failed
)

echo.
rem *******************************************************************
echo Copie des fichiers .esriAddInX générés
rem *******************************************************************

rem 
set FOUND=0
for /r "%SOLUTION_DIR%" %%f in (*.esriAddInX) do (
    echo %%f | findstr /i "\\bin\\%CONFIG%\\" >nul
    if !errorlevel! == 0 (
        set FOUND=1
        echo     Copie de %%~nxf
        copy /Y "%%f" "%ADDIN_TARGET%" >nul
    )
) 

if %FOUND%==0 (
	echo    Aucun fichier .esriAddInX trouvé dans un dossier "\bin\%CONFIG%\"
	goto failed
)

echo.
rem *******************************************************************
echo Copie des fichiers exécutables auto-suffisants générés
rem *******************************************************************

rem 
set FOUND=0
for /r "%OUTPUT_DIR%\temp_publish" %%f in (*.exe) do (
	set FOUND=1
	echo     Copie de %%~nxf
	copy /Y "%%f" "%BIN_INCA_TARGET%" >nul
) 

if %FOUND%==0 (
	echo    Aucun fichier .exe trouvé dans un dossier "\%RUNTIME%\publish\"
	goto failed
)

rem Copie du fichier Log4net.config
if exist "%OUTPUT_DIR%\temp_publish\Log4net.config" (
	copy "%OUTPUT_DIR%\temp_publish\Log4net.config" "%BIN_INCA_TARGET%\" >nul
) else (
	echo     Warning: Fichier introuvable: Log4net.config
)

rem Suppression du dossier temporaire de publication
rmdir /s /q %OUTPUT_DIR%\temp_publish

echo.
rem *******************************************************************
echo Copie des ressources complémentaires
rem *******************************************************************

for %%f in (%RSC_COMP%) do (
    rem Cas d'un dossier
	if exist "%~dp0\%%f\" (
		echo     Copie du dossier %%f
		xcopy "%~dp0\%%f" "%OUTPUT_DIR%\%%f\" /E /I /Y >nul
	rem Cas d'un fichier
	) else if exist "%~dp0\%%f" (
		echo     Copie du fichier %%f
		copy "%~dp0\%%f" "%OUTPUT_DIR%\" >nul
	rem Elément introuvable
	) else (
		echo     Warning: Fichier/Dossier introuvable: %%f
	)
)

echo.
rem *******************************************************************
echo Création de l'archive zip
rem *******************************************************************

rem Délai pour s'assurer que la copie soit terminée
timeout /t 2 /nobreak >nul

tar -acf "%ZIP_FILE%" -C "%OUTPUT_DIR%" -- *
if %errorlevel% NEQ 0 (goto:failed)

rem Suppression du dossier temporaire
rmdir /s /q %OUTPUT_DIR%

echo.
echo La publication a réussi!
echo.
goto :end

:failed
echo.
echo ###############################
echo La publication a échoué!
echo Veuillez regarder les messages affichés plus haut
echo ###############################
echo.

:end
endlocal
pause
