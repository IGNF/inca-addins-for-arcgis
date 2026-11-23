@echo off
chcp 65001 > nul

echo **********************************************************************
echo       SCRIPT D'INSTALLATION DES OUTILS "INCA-SDK POUR ARCGIS PRO"
echo                      - UTILISATEUR -
echo **********************************************************************
echo.
rem Ce script automatise l'installation sur un poste utilisateur du
rem package inca-sdk-pro développé par le projet INCA.

rem Il installe toutes les dépendances nécessaires au bon fonctionnement 
rem des outils inca-sdk-pro (API's, Add-ins).


rem *******************************************************************
rem Début du script d'installation
rem *******************************************************************

rem *******************************************************************
rem Mise à jour de la variable ARCGIS_PRO_ROOT pour accéder au répertoire 
rem où ArcGIS Pro a été installé
rem *******************************************************************

set KEY_NAME="HKEY_LOCAL_MACHINE\SOFTWARE\ESRI\ArcGISPro"
set VALUE_NAME=InstallDir
for /f "tokens=3*" %%a in ('reg query %KEY_NAME% /v %VALUE_NAME%') do (
    set ARCGIS_PRO_ROOT=%%a %%b
)

if not defined ARCGIS_PRO_ROOT (
    echo ERREUR: ArcGIS PRO est introuvable.
    echo Vérifiez votre installation ArcGIS Pro.
    goto:end
)

rem On se positionne dans le répertoire du fichier batch
CD /d "%~dp0"

rem *******************************************************************
rem Préparation du répertoire "bin\bin_inca\sdk" dans ARCGIS PRO ROOT PATH
rem où on va copier les fichier exécutables et DLL's du projet inca-sdk-pro
rem *******************************************************************
set ARCGIS_PRO_BIN_INCA=%ARCGIS_PRO_ROOT%bin\bin_inca\sdk

if not exist "%ARCGIS_PRO_BIN_INCA%\" mkdir "%ARCGIS_PRO_BIN_INCA%\"

echo.
echo *************************************************************************
echo  Copie des ressources du package "inca-sdk-pro" dans "%ARCGIS_PRO_BIN_INCA%"
echo *************************************************************************
echo.

xcopy bin_inca_sdk "%ARCGIS_PRO_BIN_INCA%" /E /Q /Y

rem Si un problème survient, alors on va au flag FAILED
IF NOT %ERRORLEVEL% EQU 0 GOTO :failed

echo.
echo ********************************************************************************
echo  Shared Add-Ins: Partage pour tous les utilisateurs les Add-Ins qui se trouvent 
echo                         dans le répertoire "add-ins"
echo ********************************************************************************
echo.
rem Doc officielle : https://github.com/esri/arcgis-pro-sdk/wiki/ArcGIS-Pro-Registry-Keys

rem répertoire où les AddIns vont être copiés
SET ADDINS_INSTALL_FOLDER=%ARCGIS_PRO_BIN_INCA%\AddIns

if not exist "%ADDINS_INSTALL_FOLDER%" mkdir "%ADDINS_INSTALL_FOLDER%"

rem On se positionne dans le répertoire du fichier batch
CD /d "%~dp0"

xcopy add-ins "%ADDINS_INSTALL_FOLDER%" /E /Q /Y

REG ADD "%KEY_NAME%\Settings\Add-In Folders" /v "%ADDINS_INSTALL_FOLDER%" /t REG_SZ	

echo.
echo Installation réussie!
echo.
GOTO :end

:failed
echo.
echo ###############################
echo /!\ L'installation a échoué /!\
echo Un problème est survenu lors de l'installation du package "inca-sdk-pro"
echo Error code: %errorlevel%
echo.
echo NOTE : Veuillez regarder les messages affichés plus haut
echo.
echo ###############################
echo.

pause

:end
pause