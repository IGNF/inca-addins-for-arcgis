using ActiproSoftware.Windows.Controls;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.UtilityNetwork.Trace;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Internal.CIM;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.DataReviewer;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Extensions;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Internal.Mapping.Ribbon;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Workflow.Models.ConfigItems;
using IGN.Inca.ArcGISPro.SDK.BLL.Desktop;
using IGN.Inca.ArcGISPro.SDK.Commons;
using IGN.Inca.ArcGISPro.SDK.Internationalization;
using IncaReintegration._ConflictManager;
using Microsoft.VisualBasic.Logging;
using OfficeOpenXml.Style.Dxf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Threading;


using Button = ArcGIS.Desktop.Framework.Contracts.Button;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;



namespace IncaReintegration._Update
{
    internal class IncaUpdateViewModel : DockPane
    {
        private const string _dockPaneID = "IncaUpdate_Dockpane";

        //variables et fonctions lancées à l'initialisation du dockpane d'update.
        protected IncaUpdateViewModel()
        {


            // initialize "inca_logger_sdk" logger
            LoggerUtils.GetInstance().InitializeLogger();

            // get the dictionary of the current UI language ("CurrentUICulture") using the Culture Resources Manager
            Translator = CultureResourcesManager.GetInstance().dictLanguage;
            ///chemin du projet ArcGIS Pro
            ProjectPath = getProjectPath();
            ///nom du projet ArcGIS Pro
            ProjectName = getProjectName();
            ///chemin du fichier .sde de connexion (data editor)
            DataEditorSdeFileFullPath = getSDEPath();
            ///initialisation du texte de progression
            ProgressionText = "";

        }


        #region Properties
        /// <summary>
        /// Culture translator
        /// </summary>
        private ResourceSet Translator { get; set; }

        /// <summary>
        /// chemin du projet ArcGIS Pro
        /// </summary>
        public static string ProjectPath;

        /// <summary>
        /// nom du projet ArcGIS Pro
        /// </summary>
        public static string ProjectName;

        /// <summary>
        /// chemin du fichier .sde de connexion (data editor)
        /// </summary>
        private string _dataEditorSdeFileFullPath;
        public string DataEditorSdeFileFullPath
        {
            get { return _dataEditorSdeFileFullPath; }
            set
            {
                SetProperty(ref _dataEditorSdeFileFullPath, value, () => DataEditorSdeFileFullPath);
            }
        }

        /// <summary>
        /// chemin de la gdb fichier de référence
        /// </summary>
        private string _gdbOriginFullPath;
        public string GdbOriginFullPath
        {
            get { return _gdbOriginFullPath; }
            set
            {
                SetProperty(ref _gdbOriginFullPath, value, () => GdbOriginFullPath);
            }
        }

        /// <summary>
        /// nom de la gdb fichier de référence
        /// </summary>

        private string _gdbOriginName;
        public string GdbOriginName
        {
            get { return _gdbOriginName; }
            set
            {
                SetProperty(ref _gdbOriginName, value, () => GdbOriginName);
            }
        }

        /// <summary>
        /// chemin de la gdb éditée n°1
        /// </summary>
        private string _gdbEditedFullPath1;
        public string GdbEditedFullPath1
        {
            get { return _gdbEditedFullPath1; }
            set
            {
                SetProperty(ref _gdbEditedFullPath1, value, () => GdbEditedFullPath1);
            }
        }

        /// <summary>
        /// nom de la gdb éditée n°1
        /// </summary>

        private string _gdbEditedName1;
        public string GdbEditedName1
        {
            get { return _gdbEditedName1; }
            set
            {
                SetProperty(ref _gdbEditedName1, value, () => GdbEditedName1);
            }
        }


        /// <summary>
        /// chemin du dossier contenant les fichiers de log et la gdb Conflit.gdb
        /// </summary>
        private string _logFolderPath;
        public string LogFolderPath
        {
            get { return _logFolderPath; }
            set
            {
                SetProperty(ref _logFolderPath, value, () => LogFolderPath);
            }
        }

        /// <summary>
        /// Texte affichant la progression du traitement dans le dockPane
        /// </summary>
        private string _progressionText;

        public string ProgressionText
        {
            get { return _progressionText; }
            set { SetProperty(ref _progressionText, value, () => ProgressionText); }
        }

        /// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ START ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        ///     One-Way Data Binding Properties, used specially for internationalization of the UI
        /// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        /// 
        public string ReintegrationHeading { get { return Translator.GetString("ReintegrationHeading"); } } // Text shown near the top of the DockPane.       
        public string SelectDataEditorSdeFile { get { return Translator.GetString("SelectDataEditorSdeFile"); } }
        public string SelectGdbFileText { get { return Translator.GetString("SelectGdbFile"); } }
        public string SelectGdbOriginFileText { get { return Translator.GetString("SelectGdbOriginFileText"); } }
        public string SelectGdbEditedFileText1 { get { return Translator.GetString("SelectGdbEditedFileText1"); } }
        public string OutputFolderText { get { return Translator.GetString("OutputFolderText"); } }
        public string SelectReplicaNameText { get { return Translator.GetString("SelectReplicaName"); } }
        public string LaunchReintegrationText { get { return Translator.GetString("LaunchReintegration"); } }
        public string DataEditorFilePathIsNullOrEmptyMessage { get { return Translator.GetString("DataEditorFilePathIsNullOrEmptyMessage"); } }
        public string GdbFilePathIsNullOrEmptyMessage { get { return Translator.GetString("GdbFilePathIsNullOrEmptyMessage"); } }
        public string VersionNotSelectedMessage { get { return Translator.GetString("VersionNotSelectedMessage"); } }
        public string VersionNotExistingMessage { get { return Translator.GetString("VersionNotExistingMessage"); } }
        public string SynchronizationKoMessage { get { return Translator.GetString("SynchronizationKoMessage"); } }
        public string ReconciliationKoMessage { get { return Translator.GetString("ReconciliationKoMessage"); } }
        public string ReconciliationAbortMessage { get { return Translator.GetString("ReconciliationAbortMessage"); } }
        public string SimultaneousReconcileMessage { get { return Translator.GetString("SimultaneousReconcileMessage"); } }
        public string ReconciliationOkMessage { get { return Translator.GetString("ReconciliationOkMessage"); } }
        public string GrantKOMessage { get { return Translator.GetString("GrantKOMessage"); } }
        public string StartMessageUpdate { get { return Translator.GetString("StartMessageUpdate"); } }
        public string ConflictMapMessage { get { return Translator.GetString("ConflictMapMessage"); } }
        /// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ END ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~


        #endregion

        #region Commands

        // La région Commands fait le lien entre l'IHM et les fonctions du script.
        // Les commandes sont liées à des boutons ArcGIS Pro dans IncaUpdateView.xaml et lancent des fonctions quand on clique sur ces boutons.
        /// <summary>
        /// Command to browse Origin gdb file
        /// </summary>
        private ICommand _browseGdbOriginFileCommand;
        public ICommand BrowseGdbOriginFileCommand
        {
            get { return _browseGdbOriginFileCommand ?? (_browseGdbOriginFileCommand = new RelayCommand(() => BrowseGdbOriginFileName())); }
        }

        /// <summary>
        /// Command to browse Edited gdb file n°1
        /// </summary>
        private ICommand _browseGdbEditedFileCommand1;
        public ICommand BrowseGdbEditedFileCommand1
        {
            get { return _browseGdbEditedFileCommand1 ?? (_browseGdbEditedFileCommand1 = new RelayCommand(() => BrowseGdbEditedFileName1())); }
        }

        /// <summary>
        /// Command to browse Data Editor .sde file
        /// </summary>
        private ICommand _browseDataEditorSdeCommand;
        public ICommand BrowseDataEditorSdeCommand
        {
            get { return _browseDataEditorSdeCommand ?? (_browseDataEditorSdeCommand = new RelayCommand(() => BrowseDataEditorSdeFileName())); }
        }
        /// <summary>
        /// Command to browse the log folder
        /// </summary>
        private ICommand _browseLogFolderCommand;
        public ICommand BrowseLogFolderCommand
        {
            get { return _browseLogFolderCommand ?? (_browseLogFolderCommand = new RelayCommand(() => BrowseLogFolder())); }
        }

        /// <summary>
        /// Command to launch the update
        /// </summary>
        private ICommand _launchUpdateCommand;
        public ICommand LaunchUpdateCommand
        {
            get { return _launchUpdateCommand ?? (_launchUpdateCommand = new RelayCommand(() => LaunchUpdate())); }
        }
        #endregion

        #region Interaction with the "IncaProSDK" Library

        // Fonction lancée quand on clique sur le bouton ArcGIS Pro de lancement de l'update.
        // On vérifie que les paramètres sont bien remplis puis on lance la fonction TraitementUpdate.
        public void LaunchUpdate()
        {
            ProgressionText = "";
            TraitementUpdate();

        }
        #endregion

        #region Fonction Update

       //Traitement d'Update
        public void TraitementUpdate()
        {
            QueuedTask.Run(async () =>
            {
                Debug.WriteLine("DEBUT");
                string conflictMapName = "carte_conflits";
                bool conflictMapInProject = false;
                ProgressionText = StartMessageUpdate;
            
                
                if (string.IsNullOrEmpty(GdbOriginFullPath)) {
                    ProgressionText = "KO";
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Il manque le chemin vers la géodatabase fichier d'origine");
                } /// s'il manque la gdb fichier d'origine
                
                else if (!Directory.Exists(GdbOriginFullPath)) {
                    ProgressionText = "KO";
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Le chemin vers la géodatabase fichier d'origine est incorrect");
                }
                    
                else if (string.IsNullOrEmpty(GdbEditedFullPath1)) {
                    ProgressionText = "KO";
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Il manque le chemin vers la géodatabase fichier éditée n°1");
                } /// s'il manque la gdb fichier éditée n°1
                    
                else if (!Directory.Exists(GdbEditedFullPath1)) {
                    ProgressionText = "KO";
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Le chemin vers la géodatabase fichier éditée n°1 est incorrect");
                }

                else if (IsGdbLocked(GdbEditedFullPath1))
                {
                    ProgressionText = "KO";
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"Erreur : la gdb d'édition est verrouillée. " +
                    $"Vérifiez qu'elle n'est pas utilisée dans un projet ArcGIS Pro ouvert. Chemin gdb : {GdbEditedFullPath1}");
                }

                else if (string.IsNullOrEmpty(DataEditorSdeFileFullPath)) {
                    ProgressionText = "KO";
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Il manque le chemin du fichier de connexion sde à la géodatabase partagée");
                }/// s'il manque la connexion sde

                else if (!File.Exists(DataEditorSdeFileFullPath))
                {
                    ProgressionText = "KO";
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Le chemin du le fichier .sde est incorrect");
                }
                   
                else if (string.IsNullOrEmpty(LogFolderPath))
                {
                    ProgressionText = "KO";
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Il manque le chemin vers le répertoire de log");
                }/// s'il manque le répertoire des fichiers de logs

                else if (!Directory.Exists(LogFolderPath))
                {
                    ProgressionText = "KO";
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Le chemin vers le répertoire de log est incorrect");
                }
                
                else
                {
                    foreach (var mapItem in Project.Current.GetItems<MapProjectItem>())
                    {
                        if (mapItem.Name.Equals(conflictMapName, StringComparison.Ordinal))
                        {
                            conflictMapInProject = true;
                        }
                    }
                    if (conflictMapInProject)
                    {
                        Debug.WriteLine("OK la carte carte_conflits est présente");
                        
                        string projectPath = Project.Current.URI;
                        string projectFolder = System.IO.Path.GetDirectoryName(projectPath);
                        // calcul du traitement d'update
                        string result = update(GdbOriginFullPath, GdbEditedFullPath1, DataEditorSdeFileFullPath, LogFolderPath, projectFolder);
                        if (result.Contains("OK"))
                        {
                            ProgressionText = ConflictMapMessage;
                            // Préparation de la carte des conflits
                            // 1. Carte fermée
                            var mapItem = Project.Current.GetItems<MapProjectItem>()
                                .FirstOrDefault(m => m.Name == "carte_conflits");

                            if (mapItem == null)
                                throw new Exception("Carte non trouvée");

                            var map = mapItem.GetMap();
                            Debug.WriteLine("Carte trouvée");

                            // 2. GDB                           
                            var result_content= result.Trim('[', ']');
                            var path = result_content.Split(',')[1].Replace("'", "").Trim();   // enlever quotes
                            var gdbName = $@"{path}";
                            Debug.WriteLine("gdbName = "+gdbName);
                            using var gdb = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(gdbName)));

                            // 3. Création d'un groupe conflits
                            var groupe_conflits = LayerFactory.Instance.CreateGroupLayer(map, 0, "conflits");

                            Debug.WriteLine("Gdb trouvée");
                            // 4. Feature Classes
                            var fcDefs = gdb.GetDefinitions<FeatureClassDefinition>();

                            foreach (var fcDef in fcDefs)
                            {
                                Debug.WriteLine("Couche = " + fcDef.GetName());
                                using var fc = gdb.OpenDataset<FeatureClass>(fcDef.GetName());

                                if (fcDef.GetName() != "conflict_point")
                                {
                                    var groupName = fcDef.GetName().Replace("_sde", "").Replace("_ini", "").Replace("_gdb", "");
                                    var sous_groupe = GetOrCreateSousGroupLayer(groupe_conflits, groupName);
                                    // 4. Création de la couche

                                    var createParams = new FeatureLayerCreationParams(fc)
                                    {
                                        Name = fcDef.GetName()
                                    };
                                    var layer = LayerFactory.Instance.CreateLayer<FeatureLayer>(createParams, sous_groupe);

                                    //changer couleur symbo
                                    if (!fcDef.GetName().Contains("e_toponyme_anno_25000"))
                                    {
                                        var renderer = layer.GetRenderer() as CIMSimpleRenderer;
                                        var geomType = layer.ShapeType;

                                        var source = fcDef.GetName().Substring(fcDef.GetName().Length - 4);

                                        CIMColor color = source switch
                                        {
                                            "_sde" => ColorFactory.Instance.CreateRGBColor(255, 0, 197, 50), // violet
                                            "_gdb" => ColorFactory.Instance.CreateRGBColor(0, 112, 255, 50), //bleu
                                            _ => ColorFactory.Instance.CreateRGBColor(0, 0, 0, 50)
                                        };

                                        CIMSymbol symbol = geomType switch
                                        {
                                            esriGeometryType.esriGeometryPolyline =>
                                                SymbolFactory.Instance.ConstructLineSymbol(color, 1.0),

                                            esriGeometryType.esriGeometryPolygon =>
                                                SymbolFactory.Instance.ConstructPolygonSymbol(
                                                    color,
                                                    SimpleFillStyle.Solid,
                                                    SymbolFactory.Instance.ConstructStroke(color, 1.0, SimpleLineStyle.Solid)),

                                            esriGeometryType.esriGeometryPoint =>
                                                SymbolFactory.Instance.ConstructPointSymbol(color, 3.0),

                                            _ => null
                                        };

                                        if (symbol == null) return;

                                        renderer.Symbol = new CIMSymbolReference
                                        {
                                            Symbol = symbol
                                        };
                                        layer.SetRenderer(renderer);
                                    }

                                }
                                else
                                {
                                    // 4. Création de la couche
                                    var createParams = new FeatureLayerCreationParams(fc)
                                    {
                                        Name = fcDef.GetName()
                                    };

                                    var layer = LayerFactory.Instance.CreateLayer<FeatureLayer>(createParams, groupe_conflits);
                                    string baseDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                                    string lyrxPath = System.IO.Path.Combine(baseDir, "Ressources", "conflict_points.lyrx");
                                    Debug.WriteLine("lyrxPath = " + lyrxPath);
                                    // Charger la couche depuis le .lyrx
                                    var tempLayer = LayerFactory.Instance.CreateLayer(new Uri(lyrxPath), map) as FeatureLayer;
                                    // Copier le renderer
                                    var renderer = tempLayer.GetRenderer();
                                    layer.SetRenderer(renderer);

                                    // Supprimer la couche temporaire
                                    map.RemoveLayer(tempLayer);
                                }
                            }

                            // ouverture de la carte des conflits
                            MapProjectItem mapConflictItem = Project.Current.GetItems<MapProjectItem>().FirstOrDefault(item =>
                                                                item.Name.Equals(conflictMapName));
                            await FrameworkApplication.Panes.CreateMapPaneAsync(mapConflictItem.GetMap());                           
                            // Get the active map view
                            var mapView = MapView.Active;

                            // Find the desired layer by name
                            var featureLayer = mapView.Map.GetLayersAsFlattenedList()
                            .OfType<FeatureLayer>()
                            .FirstOrDefault(layer => layer.Name == "conflict_point");

                            if (featureLayer != null)
                            {
                                // Get the extent of the layer
                                var layerExtent = featureLayer.QueryExtent();

                                // Zoom to the layer's extent
                                mapView.ZoomTo(layerExtent);
                            }
                            string fileName = "chemin_gdb_editee.txt";
                            string filePath = System.IO.Path.Combine(path, fileName);
                            if (!File.Exists(filePath))
                            {
                                File.WriteAllText(filePath, GdbEditedFullPath1);
                            }
                            ProgressionText = "Succès. Traiter les conflits.";
                            ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"Traitement d'update en succès. Retrouvez les logs et la gdb de conflits ici : {LogFolderPath}");
                        }
                        else if (result.Contains("pas de conflits"))
                        {
                            var result_content = result.Trim('[', ']');
                            var gdbName = result_content.Split(',')[1].Replace("'", "").Trim();   // enlever quotes
                            // Remplacement de la gdb d'édition par la gdb d'update
                            Task<(bool Success, string ErrorMessage)> task = ChangeEditionGdb(GdbEditedFullPath1, getUpdateGdbFolder(LogFolderPath, gdbName));
                            ProgressionText = "Succès. Pas de conflits.";
                            ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"Traitement d'update en succès. Pas de conflits détectés. Retrouvez les logs ici : {LogFolderPath}");
                        }
                        else if (result.Contains("pas de modif"))
                        {                           
                            ProgressionText = "Succès. Pas de modifications sur gdb partagée.";
                            ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"Traitement d'update en succès. Pas de modifications sur la gdb partagée. Retrouvez les logs ici : {LogFolderPath}");
                        }
                        else
                        {
                            ProgressionText = "KO";
                            ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"Traitement d'update en échec : {result}");
                            string horodatage = DateTime.Now.ToString("ddMMyyyy_HHmmss")
                                                        .Replace(":", "h").Replace("m", "m");

                            // Construit le nom complet du fichier de log avec extension
                            string errorFileName = $"error_{horodatage}.txt";
                            string errorFilePath = System.IO.Path.Combine(LogFolderPath, errorFileName);
                            using (StreamWriter writer = new StreamWriter(errorFilePath, false))
                            {
                                writer.WriteLine("Traitement d'update en échec :");
                                writer.WriteLine(result);
                            }
                        }
                    }
                    else
                    {
                        ProgressionText = "KO";
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("KO Il manque la carte carte_conflits");
                    }
                }
            });

            
        }
        #endregion

        #region Fonctions appelees par la fonction Update
        /// <summary>
        /// Methode pour calculer un différentiel entre deux gdb d'éditions et une gdb d'origine + calcul des conflits
        /// </summary>


        public static string update(string fullPathOriginGdb, string fullPathEditedGdb1, string fullPathSde, string fullPathLogFolder, string projectFolder)
        {

            //chemin de l'instance ArcGIS Pro
            var pathProExe = System.IO.Path.GetDirectoryName(new Uri(Assembly.GetEntryAssembly().Location).AbsolutePath);
            if (pathProExe == null) return "ERROR_ArcGIS_Pro_Path";
            pathProExe = Uri.UnescapeDataString(pathProExe);
            pathProExe = System.IO.Path.Combine(pathProExe, @"Python\envs\arcgispro-py3");

            //chemin de l'instance Python
            var pathPython = System.IO.Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().Location).AbsolutePath);
            if (pathPython == null) return "ERROR_Python_Path";
            pathPython = Uri.UnescapeDataString(pathPython);
            pathPython = pathPython + @"\_Update";

            var fullPathProExe = System.IO.Path.Combine(pathProExe, "python.exe");
            var fullPathPython = System.IO.Path.Combine(pathPython, "update.py");
            Debug.WriteLine("fullPathGdb origin = " + fullPathOriginGdb);
            Debug.WriteLine("fullPathGdb1 = " + fullPathEditedGdb1);
            Debug.WriteLine("fullPathSde = " + fullPathSde);
            Debug.WriteLine("fullPathGdb = " + fullPathLogFolder);
            Debug.WriteLine("fullPathProExe = " + fullPathProExe);
            Debug.WriteLine("fullPathPython = " + fullPathPython);
            Debug.WriteLine("projectFolder = " + projectFolder);
            var myArguments = $@"""{fullPathPython}"" ""{fullPathOriginGdb}"" ""{fullPathEditedGdb1}"" ""{fullPathSde}"" ""{fullPathLogFolder}"" ""{projectFolder}""";

            var myCommand = $@"/c """"{fullPathProExe}"" {myArguments}""";

            var procStartInfo = new ProcessStartInfo("cmd", myCommand);

            procStartInfo.RedirectStandardOutput = true;
            procStartInfo.RedirectStandardError = true;
            procStartInfo.UseShellExecute = false;
            procStartInfo.CreateNoWindow = true;

            Process proc = new Process();
            proc.StartInfo = procStartInfo;
            proc.Start();

            string result = proc.StandardOutput.ReadToEnd();
            string error = proc.StandardError.ReadToEnd();
            if (!string.IsNullOrEmpty(error)) result += string.Format("{0} Error: {1}", result, error);

            result = result.TrimEnd('\r', '\n');
            Debug.WriteLine("RESULT = " + result);
                
            return result;
            
            
        }
        #endregion

        #region Manage other controls in the DockPane
        /// <summary>
        /// Applique le style d'une couche à une autre couche.
        /// </summary>
        private static void CopySymbology(FeatureLayer sourceLayer, FeatureLayer targetLayer)
        {
            var renderer = sourceLayer.GetRenderer();
            if (renderer == null)
                return;
            targetLayer.SetRenderer(renderer);
        }

        /// <summary>
        /// Retourne un sous-groupe de l'onglet Contenu ou le crée s'il n'existe pas.
        /// </summary>
        private static GroupLayer GetOrCreateSousGroupLayer(GroupLayer group, string groupName)
        {
            // Recherche existante
            var sous_groupe = group.Layers.OfType<GroupLayer>().FirstOrDefault(g => g.Name == groupName);

            if (sous_groupe != null)
                return sous_groupe;
            // Pour créer groupe tout en bas (en dernier)
            int rang = group.Layers.Count;
            // Création si absent
            return LayerFactory.Instance.CreateGroupLayer(group, rang, groupName);
        }

        /// <summary>
        /// Show the DockPane.
        /// </summary>
        internal static void Show()
        {
            DockPane pane = FrameworkApplication.DockPaneManager.Find(_dockPaneID);
            if (pane == null)
                return;

            pane.Activate();

        }

        /// <summary>
        /// Methode pour afficher le chemin du projet
        /// </summary>
        public static string getProjectPath()
        {
            Debug.WriteLine("ON FAIT APPEL A LA FONCTION getProjectPath");
            var path = @"";
            if (Project.Current != null)
            {
                Debug.WriteLine("Le projet n'est pas nul");
                path = Project.Current.HomeFolderPath;
                Debug.WriteLine("Path vaut : " + path);
                return path;
            }
            else
            {
                Debug.WriteLine("Le projet est nul");
                return path;
            }
        }

        /// <summary>
        /// Methode pour afficher le chemin du fichier sde
        /// </summary>
        public static string getSDEPath()
        {
            Debug.WriteLine("ON FAIT APPEL A LA FONCTION getSDEPath");
            var sdePath = "";
            var folderPath = getProjectPath();
            Debug.WriteLine(folderPath);
            if (folderPath != "")
            {
                string[] files = Directory.GetFiles(folderPath, "*.sde");
                // Update the combo box with current project styles
                int cnt = files.Length;
                Debug.WriteLine(cnt);
                if (cnt > 0)
                {
                    sdePath = files[0];
                }
                foreach (string file in files)
                {
                    Debug.WriteLine(file);
                    if (file.Contains("EDITOR") || file.Contains("editor")) //on récupère en priorité les fichiers SDE du répertoire contenant le projet avec un nom contenant "data_editor"
                    {
                        sdePath = file;
                    }
                }
            }
            return sdePath;
        }

        /// <summary>
        /// Methode pour afficher le nom du projet
        /// </summary>
        public static string getProjectName()
        {
            Debug.WriteLine("ON FAIT APPEL A LA FONCTION getProjectName");
            var name_with_extent = "";
            if (Project.Current != null)
            {
                Debug.WriteLine("Le projet n'est pas nul");
                name_with_extent = Project.Current.Name; // le nom du projet avec l'extension .aprx
            }
            var name = name_with_extent.Split('.').First(); // le nom du projet sans l'extension .aprx
            Debug.WriteLine("Nom du projet vaut : " + name);
            return name;
        }


        /// <summary>
        /// Methode pour afficher le nom et le chemin de la géodatabase fichier
        /// </summary>
        public static (string, string) getDefaultGdb()
        {
            Debug.WriteLine("ON FAIT APPEL A LA FONCTION getDefaultGdb");
            var full_name = "";
            var name_without_extent = "";
            if (Project.Current != null)
            {
                Debug.WriteLine("Le projet n'est pas nul");
                full_name = Project.Current.DefaultGeodatabasePath; // le chemin de la gdb liée au projet avec l'extension .gdb
                name_without_extent = full_name.Split('\\').Last().Split('.').First(); //le nom de la gdb sans extension
            }
            Debug.WriteLine("Nom de geodatabase : " + full_name);
            Debug.WriteLine("Nom complet de geodatabase : " + name_without_extent);
            return (full_name, name_without_extent);
        }

        /// <summary>
        /// Methode pour ouvrir un explorateur de fichier pour sélectionner le fichier sde
        /// </summary>
        public void BrowseDataEditorSdeFileName()
        {
            // Créer un OpenFileDialog 
            OpenFileDialog dlg = new OpenFileDialog
            {
                // filtrer l'extension .sde
                DefaultExt = ".sde",
                Filter = "SDE Files|*.sde",
                InitialDirectory = ProjectPath
            };

            // Afficher OpenFileDialog en appelant la méthode ShowDialog  
            bool? result = dlg.ShowDialog();

            if (result != true) return;
            // dlg.Filename = fichier sélectionné
            string filename = dlg.FileName;
            DataEditorSdeFileFullPath = filename;
        }

        /// <summary>
        /// Methode pour ouvrir un explorateur de fichier pour sélectionner la géodatabase fichier d'origine
        /// </summary>
        public void BrowseGdbOriginFileName()
        {
            // Créer un FolderBrowserDialog  
            FolderBrowserDialog dlg = new FolderBrowserDialog()
            {
                InitialDirectory = ProjectPath
            };
            // Afficher OpenFileDialog en appelant la méthode ShowDialog  
            DialogResult folderResult = dlg.ShowDialog();

            if (folderResult != DialogResult.OK) return;
            // dlg.SelectedPath = fichier sélectionné
            string folderName = dlg.SelectedPath;
            GdbOriginFullPath = folderName; //on récupère le chemin complet de la gdb (chemin + nom)
            GdbOriginName = GdbOriginFullPath.Split('\\').Last().Split('.').First();  //on récupère le nom de la gdb
            Debug.WriteLine($"GdbOriginFullPath = {GdbOriginFullPath}");
            Debug.WriteLine($"GdbFileName = {GdbOriginName}");
        }

        /// <summary>
        /// Methode pour ouvrir un explorateur de fichier pour sélectionner la géodatabase fichier d'origine
        /// </summary>
        public void BrowseGdbEditedFileName1()
        {
            // Créer un FolderBrowserDialog  
            FolderBrowserDialog dlg = new FolderBrowserDialog()
            {
                InitialDirectory = ProjectPath
            };
            // Afficher OpenFileDialog en appelant la méthode ShowDialog  
            DialogResult folderResult = dlg.ShowDialog();

            if (folderResult != DialogResult.OK) return;
            // dlg.SelectedPath = fichier sélectionné
            string folderName = dlg.SelectedPath;
            GdbEditedFullPath1 = folderName; //on récupère le chemin complet de la gdb (chemin + nom)
            GdbEditedName1 = GdbEditedFullPath1.Split('\\').Last().Split('.').First();  //on récupère le nom de la gdb
            Debug.WriteLine($"GdbEditedFullPath = {GdbEditedFullPath1}");
            Debug.WriteLine($"GdbEditedName = {GdbEditedName1}");
        }

        

        /// <summary>
        /// Methode pour ouvrir un explorateur de fichier pour sélectionner la géodatabase fichier d'origine
        /// </summary>
        public void BrowseLogFolder()
        {
            // Créer un FolderBrowserDialog  
            FolderBrowserDialog dlg = new FolderBrowserDialog()
            {
                InitialDirectory = ProjectPath
            };
            // Afficher OpenFileDialog en appelant la méthode ShowDialog  
            DialogResult folderResult = dlg.ShowDialog();

            if (folderResult != DialogResult.OK) return;
            // dlg.SelectedPath = fichier sélectionné
            string folderName = dlg.SelectedPath;
            LogFolderPath = folderName; //on récupère le chemin complet de la gdb (chemin + nom)
            Debug.WriteLine($"LogFolderPath = {LogFolderPath}");
        }

        /// <summary>
        /// Methode pour récupérer le chemin de la gdb d'update
        /// </summary>
        public static string getUpdateGdbFolder(string logFolder, string gdbName)
        {
            var updateGdbPath = Directory.EnumerateDirectories(logFolder, "*", SearchOption.AllDirectories)
                .FirstOrDefault(dirPath => new DirectoryInfo(dirPath).Name
                    .StartsWith(gdbName, StringComparison.OrdinalIgnoreCase));

            return updateGdbPath;
        }

        /// <summary>
        /// Methode pour remplacer la gdb d'édition par la gdb d'update
        /// </summary>
        public static async Task<(bool Success, string ErrorMessage)> ChangeEditionGdb(string gdb1Path, string gdb2Path)
        {
            try
            {
                if (!Directory.Exists(gdb1Path))
                    return (false, $"La gdb1 est introuvable : {gdb1Path}");

                if (!Directory.Exists(gdb2Path))
                    return (false, $"La gdb2 est introuvable : {gdb2Path}");

                string parentDir = System.IO.Path.GetDirectoryName(gdb1Path);
                string gdb1Name = "france25_extraction.gdb";

                string gdb1NameWithoutExt = "france25_extraction";
                string gdb1Extension = ".gdb";

                // Renommage de gdb1 en gdb1_old_yyyyMMdd_HHmmss
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string oldGdbPath = System.IO.Path.Combine(parentDir, $"{gdb1NameWithoutExt}_old_{timestamp}{gdb1Extension}");

                Directory.Move(gdb1Path, oldGdbPath);

                // Copie de gdb2
                string newGdbPath = System.IO.Path.Combine(parentDir, gdb1Name);

                var parameters = Geoprocessing.MakeValueArray(gdb2Path, newGdbPath);

                var gpResult = await Geoprocessing.ExecuteToolAsync(
                    "management.Copy",
                    parameters,
                    environments: null,
                    flags: GPExecuteToolFlags.None);

                if (gpResult.IsFailed)
                {
                    string gpErrors = string.Join(Environment.NewLine, gpResult.Messages);
                    return (false, $"Échec de l'outil management.Copy : {gpErrors}");
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"Erreur lors du remplacement de la gdb d'édition par la gdb d'update." +
                    $"Vérifiez qu'elle n'est pas utilisée dans un projet ArcGIS Pro ouvert. Message d'erreur : {ex.Message}");
                return (false, $"Erreur lors du remplacement de la gdb : {ex.Message}");
            }
        }

        public static bool IsGdbLocked(string gdbPath)
        {
            if (!Directory.Exists(gdbPath))
                return false;

            string testPath = gdbPath + "_test_verrou";

            try
            {
                Directory.Move(gdbPath, testPath);   // ko si verrou
                Directory.Move(testPath, gdbPath);   // on remet le nom de base si pas de verrou
                Debug.WriteLine("PAS VERROU");
                return false;                        
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
            {
                Debug.WriteLine("VERROU");
                return true;                          
            }
        }


        public ObservableCollection<string> ListToObservableCollection(List<string> listString)
        {
            ObservableCollection<string> myCollection = new ObservableCollection<string>(listString as List<string>);
            return myCollection;
        }

        #endregion
    }

    /// <summary>
    /// Button implementation to show the DockPane.
    /// </summary>
    internal class IncaUpdateView_ShowButton : Button
    {
        protected override void OnClick()
        {
            IncaUpdateViewModel.Show();

        }
        protected override void OnUpdate()
        {
            this.Enabled = true;
        }
    }

    
}
