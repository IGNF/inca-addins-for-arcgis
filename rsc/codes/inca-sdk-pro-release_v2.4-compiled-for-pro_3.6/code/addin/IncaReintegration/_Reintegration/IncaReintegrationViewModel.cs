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
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using IGN.Inca.ArcGISPro.SDK.BLL.Desktop;
using IGN.Inca.ArcGISPro.SDK.Commons;
using IGN.Inca.ArcGISPro.SDK.Internationalization;
using Microsoft.VisualBasic;
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
using System.Windows.Threading;


using Button = ArcGIS.Desktop.Framework.Contracts.Button;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;



namespace IncaReintegration._Reintegration
{
    internal class IncaReintegrationViewModel : DockPane
    {
        private const string _dockPaneID = "IncaReintegration_Dockpane1";

        //variables et fonctions lancées à l'initialisation du dockpane de réintégration.
        protected IncaReintegrationViewModel()
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
            ///chemin du .gdb contenant le replica
            GdbFileFullPath = getDefaultGdb().Item1;
            ///nom du .gdb contenant le replica
            GdbFileName = getDefaultGdb().Item2;
            ///mise à jour de la liste des versions de la géodatabase partagée
            Task tache = UpdateGeodatabaseVersionsList();
            ///récupération de la version liée au réplica (data_editor.nom_gdb_fichier)
            Task tache2 = getDefaultVersion();
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
        /// chemin du .gdb contenant le replica
        /// </summary>
        private string _gdbFileFullPath;
        public string GdbFileFullPath
        {
            get { return _gdbFileFullPath; }
            set
            {
                SetProperty(ref _gdbFileFullPath, value, () => GdbFileFullPath);
            }
        }

        /// <summary>
        /// nom du .gdb contenant le replica
        /// </summary>

        private string _gdbFileName;
        public string GdbFileName
        {
            get { return _gdbFileName; }
            set
            {
                SetProperty(ref _gdbFileName, value, () => GdbFileName);
            }
        }

        /// <summary>
        /// liste des versions de la géodatabase partagée
        /// </summary>        
        private List<string> _geodatabaseVersionsList = new List<string>();
        public ObservableCollection<string> GeodatabaseVersionsList
        {
            get { return ListToObservableCollection(_geodatabaseVersionsList); }


        }

        /// <summary>
        /// Version sélectionnée.
        /// </summary>
        private string _selectedVersion;

        public string SelectedVersion
        {
            get { return _selectedVersion; }
            set { SetProperty(ref _selectedVersion, value, () => SelectedVersion); }
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

        /// <summary>
        /// Indique si un traitement de réintégration est en cours (utilisé pour griser le bouton de lancement)
        /// </summary>
        private bool _isReintegrationRunning;
        public bool IsReintegrationRunning
        {
            get { return _isReintegrationRunning; }
            set
            {
                SetProperty(ref _isReintegrationRunning, value, () => IsReintegrationRunning);
                NotifyPropertyChanged(() => CanLaunchReintegration);
            }
        }

        /// <summary>
        /// True quand le bouton de lancement doit être actif (aucun traitement en cours)
        /// </summary>
        public bool CanLaunchReintegration
        {
            get { return !IsReintegrationRunning; }
        }

        /// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ START ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        ///     One-Way Data Binding Properties, used specially for internationalization of the UI
        /// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        /// 
        public string ReintegrationHeading { get { return Translator.GetString("ReintegrationHeading"); } } // Text shown near the top of the DockPane.       
        public string SelectDataEditorSdeFileText { get { return Translator.GetString("SelectDataEditorSdeFile"); } }
        public string SelectGdbFileText { get { return Translator.GetString("SelectGdbFile"); } }
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
        public string Warning160360Message { get { return Translator.GetString("Warning160360Message"); } }
        public string Warning160370Message { get { return Translator.GetString("Warning160370Message"); } }
        public string Warning003355Message { get { return Translator.GetString("Warning003355Message"); } }
        public string ReconciliationOkMessage { get { return Translator.GetString("ReconciliationOkMessage"); } }
        public string GrantKOMessage { get { return Translator.GetString("GrantKOMessage"); } }
        public string StartMessage { get { return Translator.GetString("StartMessage"); } }
        public string SynchronizeRunningMessage { get { return Translator.GetString("SynchronizeRunningMessage"); } }
        public string ReconcileRunningMessage { get { return Translator.GetString("ReconcileRunningMessage"); } }
        /// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ END ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~


        #endregion

        #region Commands

        // La région Commands fait le lien entre l'IHM et les fonctions du script.
        // Les commandes sont liées à des boutons ArcGIS Pro dans IncaReintegrationView.xaml et lancent des fonctions quand on clique sur ces boutons.
        /// <summary>
        /// Command to browse Data Editor .sde file with Windows file explorer
        /// </summary>
        private ICommand _browseDataEditorSdeFileCommand;
        public ICommand BrowseDataEditorSdeFileCommand
        {
            get { return _browseDataEditorSdeFileCommand ?? (_browseDataEditorSdeFileCommand = new RelayCommand(() => BrowseDataEditorSdeFileName())); }
        }

        /// <summary>
        /// Command to browse the file geodatabase with Windows file explorer
        /// </summary>
        private ICommand _browseGdbFileCommand;
        public ICommand BrowseGdbFileCommand
        {
            get { return _browseGdbFileCommand ?? (_browseGdbFileCommand = new RelayCommand(() => BrowseGdbFileName())); }
        }

        /// <summary>
        /// Command to update versions list
        /// </summary>
        private ICommand _updateGeodatabaseVersionsListCommand;
        public ICommand UpdateGeodatabaseVersionsListCommand
        {
            get { return _updateGeodatabaseVersionsListCommand ?? (_updateGeodatabaseVersionsListCommand = new RelayCommand(() => UpdateGeodatabaseVersionsList())); }
        }

        /// <summary>
        /// Command to launch the reintegration
        /// </summary>
        private ICommand _launchReintegrationtCommand;
        public ICommand LaunchReintegrationCommand
        {
            get { return _launchReintegrationtCommand ?? (_launchReintegrationtCommand = new RelayCommand(() => LaunchReintegration())); }
        }
        #endregion

        #region Interaction with the "IncaProSDK" Library

        // Fonction lancée quand on clique sur le bouton ArcGIS Pro de lancement de la réintégration.
        // On vérifie que les paramètres sont bien remplis puis on lance la fonction TraitementReintegration.
        public void LaunchReintegration()
        {
            ProgressionText = "";
            if (string.IsNullOrEmpty(DataEditorSdeFileFullPath)) /// s'il manque le fichier de connexion sde
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(DataEditorFilePathIsNullOrEmptyMessage);
            else if (!File.Exists(DataEditorSdeFileFullPath))
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(DataEditorFilePathIsNullOrEmptyMessage);
            else if (string.IsNullOrEmpty(GdbFileFullPath)) /// s'il manque la géodatabase fichier contenant le réplica
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(GdbFilePathIsNullOrEmptyMessage);
            else if (!Directory.Exists(GdbFileFullPath))
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(GdbFilePathIsNullOrEmptyMessage);
            else
            {
                IsReintegrationRunning = true;
                TraitementReintegration(DataEditorSdeFileFullPath, GdbFileFullPath);
            }

        }
        #endregion

        #region Fonction Reintegration

        //Traitement de réintégration des données (synchronisation + réconciliation + gestion de conflits)
        public void TraitementReintegration(string sdePath, string gdbPath)
        {

            QueuedTask.Run(async () =>
            {
                try
                {
                //verifie qu'il n'y a pas de session d'édition en cours
                //variable qui stocke l'état d'édition du projet
                var bedit = false;
                if (await IsBeingEdited(out bedit) == false)
                {
                    // Message d'alerte
                    string message = CultureResourcesManager.GetInstance().dictLanguage.GetString("CancelOperation");
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(message, "Info");
                    return;
                }
                ProgressionText = StartMessage;
                Debug.WriteLine("DEBUT");

                Debug.WriteLine(sdePath);
                Debug.WriteLine(gdbPath);
                string state_id_ini = "";
                string state_id_fin = "";
                DateTime date = DateTime.Now;
                //Récupération du nom du réplica et de son identifiant
                string ReplicaString = getReplica(gdbPath);

                if (ReplicaString is null)
                {
                    ProgressionText = "Erreur: le fichier replica.txt est vide";
                    return;
                }
                if (ReplicaString.Contains("ERROR_None"))
                {
                    ProgressionText = "Erreur: pas de replica dans cette gdb";
                    return;
                }
                // Nettoyage du texte : suppression des crochets et des apostrophes
                string cleanedReplica = ReplicaString.Replace("[", "").Replace("]", "").Replace("'", "").Trim();
                // Division du texte en fonction des virgules
                string[] elementsReplica = cleanedReplica.Split(',');

                // Transformation en liste
                List<string> listReplica2 = new List<string>(elementsReplica);
                if (listReplica2.Count != 2)
                {
                    ProgressionText = "Erreur: problème avec la structure de replica.txt";
                    return;
                }
                string versionName = listReplica2[0];
                string replicaID = listReplica2[1].Trim();

                // Récupérer le nom de l'utilisateur connecté à ArcGIS Pro
                var portal = ArcGISPortalManager.Current.GetActivePortal(); //récupération du portaail
                var userName = "";
                if (portal != null && portal.IsSignedOn())  // Vérifier si le portail est actif
                {
                    // Récupérer le nom de l'utilisateur connecté à ArcGIS Pro
                    userName = portal.GetSignOnUsername();

                    if (!string.IsNullOrEmpty(userName))
                    {
                        Debug.WriteLine($"Nom d'utilisateur connecté : {userName}");
                    }
                    else
                    {
                        Debug.WriteLine("Aucun utilisateur connecté.");
                    }
                }
                else
                {
                    userName = "Pas de session connectée";
                    Debug.WriteLine("Aucun portail actif ou pas de session connectée.");
                }




                string listReplicaString = getListReplica(sdePath);
                // Nettoyage du texte : suppression des crochets et des apostrophes
                string cleanedInput = listReplicaString.Replace("[", "").Replace("]", "").Replace("'", "").Trim();
                // Division du texte en fonction des virgules
                string[] elements = cleanedInput.Split(',');
                // Transformation en liste
                List<string> listReplica = new List<string>(elements);
                // Affichage des éléments
                foreach (string element in listReplica)
                {
                    Debug.WriteLine(element.Trim());  // Trim pour supprimer les espaces éventuels
                }


                //----------------------------------------------------------------------------
                // TEST EXISTENCE REPLICA

                // variable qui prend la valeur "oui" si le nom de réplica choisi fait partie de la liste des réplicas de la géodatabase 
                string replicaExists = "grantKO";
                // variable qui prend la valeur "oui" si le nom de version choisi fait partie de la liste des versions de la géodatabase
                string versionExists = "non";

                // Création d'une connexion à la geodatabase d'entreprise
                var sdeCon = new DatabaseConnectionFile(new Uri(sdePath));

                // Connexion à la géodatabase partagée
                using (Geodatabase geodatabase = new Geodatabase(sdeCon))
                {
                    Debug.WriteLine("la connexion est ok");
                    string sdeUser = geodatabase.GetConnectionString().Split(';')[7].Split("=")[1]; //nom du user de la geodatabase partagée, à rajouter avant le nom du replica : USER.NOM_REPLICA
                    string replica_name_operator = (sdeUser + "." + versionName).ToUpper(); //on passe le nom en majuscule car les replicas sont écrits en majuscule dans la table sde.gdb_items
                    Debug.WriteLine("NOM COMPLET DU REPLICA = " + replica_name_operator);


                    replicaExists = "non";

                    // On parcourt la liste pour vérifier si le réplica existe bien                   
                    foreach (string element in listReplica)
                    {
                        Debug.WriteLine($"elementTrimUpper : {element.Trim().ToUpper()}");
                        Debug.WriteLine($"replicaID : {replicaID}");
                        if (element.Trim().ToUpper() == replicaID) // Trim pour supprimer les espaces éventuels
                        {
                            replicaExists = "oui";
                            Debug.WriteLine($"Le replica {element} est validé");
                            break;
                        }
                    }



                    //---------------------------------------------------------------------------------------

                    ///TEST EXISTENCE VERSION
                    VersionManager versionManager = geodatabase.GetVersionManager();
                    IEnumerable<string> versions = versionManager.GetVersionNames(); // Liste contenant toutes les versions de la géodatabase partagée

                    foreach (var version in versions) // on compare chaque version présente à la variable versionName
                    {
                        Debug.WriteLine(version);
                        if (version.ToUpper() == replica_name_operator) // version.GetName nous donne un nom de version en minuscule mais replica_name_operator est en majuscule.
                        {
                            versionExists = "oui";
                            versionName = version; //on met à jour versionName pour qu'il soit écrit de la forme data_editor.nom_replica
                            Debug.WriteLine("La version est validée");
                            break;
                        }
                    }
                }
                ///---------------------------------------------------------------------------------------

                if (versionExists == "oui")
                {
                    var geodatabase1 = new Geodatabase(sdeCon);
                    state_id_ini = get_state_id_default(geodatabase1).Item1; //ste_id récupéré avant l'étape de synchronisation
                    if (replicaExists == "oui") // On lance la Synchronisation
                    {
                        ProgressionText = SynchronizeRunningMessage;
                        // paramètres utilisés par la fonction SynchronizeChanges
                        string geodatabase_1 = sdePath;
                        string in_replica = versionName;
                        string geodatabase_2 = gdbPath;
                        string in_direction = "FROM_GEODATABASE2_TO_1";
                        string conflict_policy = "MANUAL";
                        string conflict_definition = "BY_OBJECT";
                        string reconcile = "DO_NOT_RECONCILE";

                        // Geoprocessing.ExecuteToolAsync prend en argument un nom de géotraitement (tool) et une liste de paramètres (args)
                        var tool = "management.SynchronizeChanges";
                        var args = Geoprocessing.MakeValueArray(geodatabase_1, in_replica, geodatabase_2, in_direction, conflict_policy, conflict_definition, reconcile);
                        Debug.WriteLine("Lancement de la synchronisation");
                        // execution GP Synchronisation du replica avec sa version associée dans la géodatabase
                        // GPThread : on est déjà sur le thread MCT (QueuedTask.Run), il faut forcer l'exécution du GP sur son propre thread
                        IGPResult gp_result = await Geoprocessing.ExecuteToolAsync(tool, args, null, null, null, GPExecuteToolFlags.GPThread);
                        Debug.WriteLine("Messages = " + string.Join(Environment.NewLine, gp_result.Messages.Select(m => $"[{m.Type}] {m.Text}")));
                        Debug.WriteLine("Synchronisation terminée");
                        bool checkReplica = checkReplicaExists(sdePath, replicaID);
                        Debug.WriteLine("CHECKREPLICA : " + checkReplica.ToString());
                        if (!gp_result.IsFailed && !checkReplica)
                        {
                            // Si la synchronisation est réussie
                            Debug.WriteLine("Synchronisation REUSSIE");
                            state_id_fin = get_state_id_default(geodatabase1).Item1; //state_id récupéré à la fin de l'étape de synchronisation
                            date = DateTime.Now;
                            updateLogTable(geodatabase1, userName, versionName, "synchronisation", "succès", date, state_id_ini, state_id_fin);
                            // On enchaine avec la réconciliation
                            Debug.WriteLine("DEBUT RECONCILIATION");
                            ProgressionText = ReconcileRunningMessage;
                            state_id_ini = get_state_id_default(geodatabase1).Item1; //state_id récupéré avant l'étape de réconciliation
                            // paramètres utilisés par la fonction ReconcileVersions
                            string input_database = sdePath;
                            string reconcile_mode = "ALL_VERSIONS";
                            string target_version = "SDE.DEFAULT";
                            string edit_versions = versionName;
                            string acquire_locks = "LOCK_ACQUIRED";
                            string abort_if_conflicts = "ABORT_CONFLICTS";
                            string conflict_definition_reconciliation = "BY_OBJECT";
                            string conflict_resolution = "FAVOR_EDIT_VERSION";
                            string with_post = "POST";
                            string with_delete = "DELETE_VERSION";
                            string out_log = ProjectPath + @"\log_reconciliation_" + versionName + "_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".txt";
                            string reconcile_checkout_versions = "RECONCILE";

                            // Geoprocessing.ExecuteToolAsync prend en argument un nom de géotraitement (tool) et une liste de paramètres (args)
                            var tool_reconciliation = "management.ReconcileVersions";
                            var args_reconciliation = Geoprocessing.MakeValueArray(input_database, reconcile_mode, target_version, edit_versions, acquire_locks, abort_if_conflicts, conflict_definition = conflict_definition_reconciliation, conflict_resolution, with_post, with_delete, out_log, reconcile_checkout_versions);

                            Debug.WriteLine("Lancement de la réconciliation");
                            // execution GP Reconciliation 
                            IGPResult gp_result_reconciliation = await Geoprocessing.ExecuteToolAsync(tool = tool_reconciliation, args = args_reconciliation, null, null, null, GPExecuteToolFlags.GPThread);
                            Debug.WriteLine("Réconciliation terminée");

                            if (!gp_result_reconciliation.IsFailed)
                            {
                                // Si la synchronisation est réussie
                                Debug.WriteLine("La réconciliation sans s'est déroulée sans erreur");
                                var warnings = gp_result_reconciliation.Messages.Where(it => it.Type == GPMessageType.Warning);
                                bool conflictDetected = false;
                                bool simultaneousReconcile = false;
                                bool warning160360 = false;
                                bool warning003355 = false;
                                bool warning160370 = false;
                                foreach (var warning in warnings) //On boucle sur les warning pour vérifier si on obtient le warning 000084 qui indique la présence de conflits
                                {
                                    Debug.WriteLine("warning");
                                    Debug.WriteLine(warning.ToString());
                                    if (warning.ToString().Contains("000084"))
                                    {
                                        conflictDetected = true; //en cas de conflit, la variable conflictDetected passe à true
                                    }
                                    if (warning.ToString().Contains("160260"))
                                    {
                                        simultaneousReconcile = true; //en cas de réconciliation interrompue car une autre réconciliation est déjà en cours, la variable simultaneousReconcile passe à true
                                    }
                                    if (warning.ToString().Contains("160360"))
                                    {
                                        warning160360 = true; //en cas de réconciliation interrompue car la version est déjà en cours de réconciliation, la variable warning160360 passe à true
                                    }
                                    if (warning.ToString().Contains("160370"))
                                    {
                                        warning160370 = true; //en cas de réconciliation interrompue car la version a été redéfinie en référence à une autre BDD, la variable warning160370 passe à true
                                    }
                                    if (warning.ToString().Contains("003355"))
                                    {
                                        warning003355 = true; //en cas d'erreur dans la réconciliation de la version, la variable simultaneousReconcile passe à true
                                    }
                                }
                                if (!conflictDetected && !simultaneousReconcile && !warning160360 && !warning003355 && !warning160370)
                                {
                                    Debug.WriteLine("Réconciliation REUSSIE");
                                    state_id_fin = get_state_id_default(geodatabase1).Item1; //state_id récupéré à la fin de l'étape de réconciliation
                                    date = DateTime.Parse(get_state_id_default(geodatabase1).Item2);
                                    updateLogTable(geodatabase1, userName, versionName, "réconciliation", "succès", date, state_id_ini, state_id_fin);
                                    //suppression fichier replica.txt
                                    string txtFileName = "replica.txt";
                                    string txtFilePath = System.IO.Path.Combine(gdbPath, txtFileName);
                                    try
                                    {
                                        if (File.Exists(txtFilePath))
                                        {
                                            File.Delete(txtFilePath);
                                            Debug.WriteLine("Fichier supprimé avec succès.");
                                        }
                                        else
                                        {
                                            Debug.WriteLine("Le fichier n'existe pas.");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"Erreur : {ex.Message}");
                                    }
                                    ProgressionText = ReconciliationOkMessage;
                                    // Ouverture de pop-up pour annoncer que le traitement est OK
                                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(ReconciliationOkMessage);
                                }
                                else if (simultaneousReconcile)
                                {
                                    Debug.WriteLine("Réconciliation AVORTEE car une autre réconciliation est déjà en cours");
                                    state_id_fin = get_state_id_default(geodatabase1).Item1; //state_id récupéré à la fin de l'étape de réconciliation
                                    date = DateTime.Now;
                                    updateLogTable(geodatabase1, userName, versionName, "réconciliation", "KO (réconciliation déjà en cours)", date, state_id_ini, state_id_fin);
                                    // Ouverture de pop-up pour annoncer que la réconciliation est interrompue car une résolution de conflits est déjà en cours
                                    ProgressionText = SimultaneousReconcileMessage;
                                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(SimultaneousReconcileMessage);
                                }
                                else if (warning160360)
                                {
                                    Debug.WriteLine("Réconciliation AVORTEE car la version est déjà en cours de réconciliation");
                                    state_id_fin = get_state_id_default(geodatabase1).Item1; //state_id récupéré à la fin de l'étape de réconciliation
                                    date = DateTime.Now;
                                    updateLogTable(geodatabase1, userName, versionName, "réconciliation", "KO (version déjà en cours de réconciliation)", date, state_id_ini, state_id_fin);
                                    // Ouverture de pop-up pour annoncer que la réconciliation est interrompue car la version est déjà en cours de réconciliation
                                    ProgressionText = Warning160360Message;
                                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(Warning160360Message);
                                }
                                else if (warning160370)
                                {
                                    Debug.WriteLine("Réconciliation AVORTEE car la version a été redéfinie en référence à un nouvel état de base de données (nouveau StateID).");
                                    state_id_fin = get_state_id_default(geodatabase1).Item1; //state_id récupéré à la fin de l'étape de réconciliation
                                    date = DateTime.Now;
                                    updateLogTable(geodatabase1, userName, versionName, "réconciliation", "KO (redéfinie en référence à un nouvel état de base de données (nouveau StateID))", date, state_id_ini, state_id_fin);
                                    // Ouverture de pop-up pour annoncer que la réconciliation est interrompue car la version est déjà en cours de réconciliation
                                    ProgressionText = Warning160370Message;
                                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(Warning160370Message);
                                }
                                else if (warning003355)
                                {
                                    Debug.WriteLine("Réconciliation en erreur");
                                    state_id_fin = get_state_id_default(geodatabase1).Item1; //state_id récupéré à la fin de l'étape de réconciliation
                                    date = DateTime.Now;
                                    updateLogTable(geodatabase1, userName, versionName, "réconciliation", "KO (Erreur lors de la réconciliation de la version)", date, state_id_ini, state_id_fin);
                                    ProgressionText = Warning003355Message;
                                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(Warning003355Message);
                                }
                                else
                                {
                                    state_id_fin = get_state_id_default(geodatabase1).Item1; //state_id récupéré à la fin de l'étape de réconciliation
                                    date = DateTime.Now;
                                    updateLogTable(geodatabase1, userName, versionName, "réconciliation", "conflit", date, state_id_ini, state_id_fin);
                                    // OUVERTURE DE LA FENETRE DE GESTION DES CONFLITS
                                    Debug.WriteLine("Réconciliation AVORTEE car Conflit");
                                    // Ouverture de pop-up pour annoncer que la réconciliation est interrompue car des conflits ont été détectés
                                    ProgressionText = ReconciliationAbortMessage;
                                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(ReconciliationAbortMessage);
                                    // Pour ouvrir la fenêtre de gestion de conflits il y a 3 étapes:
                                    // 1 - Changer la version de la géodatabase partagée
                                    // 2 - On sélectionne une couche de la géodatabase partagée
                                    // 3 - Lancer le bouton reconcile

                                    // Obtenir le projet en cours
                                    var project = Project.Current;
                                    // Obtenir la couche dont on souhaite changer la version
                                    var map = MapView.Active.Map;
                                    var layers = map.GetLayersAsFlattenedList().Where((l) => l is FeatureLayer); // liste de toutes les couches présentes sur la carte
                                    var sdeUser = "";
                                    var layerName = "";
                                    Geodatabase geodatabase = new Geodatabase(sdeCon);
                                    // Récupérer les classes d'entités (feature classes) de la géodatabase
                                    sdeUser = geodatabase.GetConnectionString().Split(';')[7].Split("=")[1]; //récupération du user de la géodatabase partagée
                                    Debug.WriteLine("GEODATABASE = " + sdeUser);
                                    var selectedLayer = MapView.Active.Map.Layers.FirstOrDefault() as FeatureLayer;
                                    foreach (FeatureLayer layer in layers) //on boucle jusqu'à récupérer une couche qui appartient à la géodatabase partagée
                                    {
                                        Datastore dataStore = layer.GetFeatureClass().GetDatastore();
                                        Debug.WriteLine(dataStore.GetConnectionString());
                                        string connexionString = dataStore.GetConnectionString();
                                        string dataStoreUser = null;
                                        if (connexionString.Split("=")[0] != "DATABASE") // On vérifie que le datastore n'est pas un fichier .gdb
                                        {
                                            dataStoreUser = dataStore.GetConnectionString().Split(';')[7].Split("=")[1];    //nom du user propriétaire du layer. Il doit etre égal au user de la connexion sde
                                        }
                                        Debug.WriteLine("USER NAME = " + dataStoreUser);
                                        if (dataStoreUser == sdeUser)
                                        {
                                            layerName = layer.Name;
                                            selectedLayer = layer;
                                            break;
                                        }

                                    }
                                    Debug.WriteLine("LAYER NAME = " + layerName);

                                    //---------------------------------------------------------------------
                                    // Etape 1 - Changer la version de la géodatabase partagée
                                    VersionManager versionManager = geodatabase.GetVersionManager(); //on appelle le versionManager

                                    await QueuedTask.Run(() =>
                                    {
                                        Geodatabase layerGdb = null;

                                        var layer = selectedLayer;

                                        using (var table = layer.GetTable())
                                        {
                                            layerGdb = table.GetDatastore() as Geodatabase;
                                        }

                                        var vm = layerGdb.GetVersionManager();

                                        var currentVersion = vm.GetCurrentVersion();

                                        var targetVersionName = vm.GetVersionNames()
                                            .FirstOrDefault(v =>
                                                v.Equals(versionName, StringComparison.OrdinalIgnoreCase) && versionManager.GetVersion(v).IsOwner());

                                        if (targetVersionName == null)
                                            return;

                                        MapView.Active.Map.ChangeVersion(currentVersion, versionManager.GetVersion(targetVersionName));
                                    });

                                    // Delais pour attendre la fin de Change Version avant de selectionner une couche pour lancer la réconciliation
                                    // Si la sélection de la couche est faite avant la fin de l'exécution  de ChangeVersion la sélection s'annule quand la table du contenu est rechargée.
                                    await Task.Delay(1000);

                                    //---------------------------------------------------------------------
                                    // Etape 2 - Sélectionner une couche de la géodatabase partagée

                                    await QueuedTask.Run(() =>
                                    {
                                        Debug.WriteLine("DEBUT CHANGEMENT SELECTION");

                                        List<Layer> layersToSelect = new List<Layer>();
                                        layersToSelect.Add(selectedLayer);
                                        MapView.Active.SelectLayers(layersToSelect); //sélection de la couche
                                        Debug.WriteLine("FIN CHANGEMENT SELECTION");
                                        Debug.WriteLine("DEBUT LANCEMENT BOUTON Reconcile");

                                        //---------------------------------------------------------------------
                                        // Etape 3 - Lancement bouton reconcile pour ouvrir la fenetre de gestion de conflits
                                        string commandId = "esri_editor_reconcileVersionButton";
                                        var iCommand = FrameworkApplication.GetPlugInWrapper(commandId) as ICommand;
                                        if (iCommand != null)
                                            iCommand.Execute(null);
                                    });

                                }

                            }
                            else
                            {
                                // Si la réconciliation est KO
                                Debug.WriteLine("Réconciliation KO");
                                state_id_fin = get_state_id_default(geodatabase1).Item1; //state_id récupéré à la fin de l'étape de réconciliation
                                date = DateTime.Now;
                                updateLogTable(geodatabase1, userName, versionName, "réconciliation", "KO", date, state_id_ini, state_id_fin);

                                // Ouverture de pop-up pour annoncer que la réconciliation est KO
                                ProgressionText = ReconciliationKoMessage;
                                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(ReconciliationKoMessage);
                            }

                            Debug.WriteLine("FIN");
                        }
                        else if (gp_result.IsFailed)
                        {
                            // Si la synchronisation est KO
                            Debug.WriteLine("Synchronisation KO");
                            state_id_fin = get_state_id_default(geodatabase1).Item1; //state_id récupéré à la fin de l'étape de synchronisation
                            date = DateTime.Now;
                            updateLogTable(geodatabase1, userName, versionName, "synchronisation", "KO", date, state_id_ini, state_id_fin);
                            // Ouverture de pop-up pour annoncer que la synchronisation est KO
                            ProgressionText = SynchronizationKoMessage;
                            ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(SynchronizationKoMessage);
                        }
                        else
                        {
                            // Si la synchronisation n'a pas eu lieu (bug replica toujours présent)
                            Debug.WriteLine("Synchronisation manquante");
                            ProgressionText = SynchronizationKoMessage;
                            ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Attention la synchronisation ne s'est pas déroulée correctement. Merci de retenter la réintégration");
                        }



                    }
                    else
                    {
                            // D'abord il faut s'assurer qu'on se trouve sur la version DEFAULT pour pouvoir lancer la réconciliation (et notamment la partie supression de la version du replica)
                            // Obtenir le projet en cours
                            var project = Project.Current;
                            // Obtenir la couche dont on souhaite changer la version
                            var map = MapView.Active.Map;
                            var layers = map.GetLayersAsFlattenedList().Where((l) => l is FeatureLayer); // liste de toutes les couches présentes sur la carte
                            var sdeUser = "";
                            var layerName = "";
                            Geodatabase geodatabase = new Geodatabase(sdeCon);
                            // Récupérer les classes d'entités (feature classes) de la géodatabase
                            sdeUser = geodatabase.GetConnectionString().Split(';')[7].Split("=")[1]; //récupération du user de la géodatabase partagée
                            Debug.WriteLine("GEODATABASE = " + sdeUser);
                            var selectedLayer = MapView.Active.Map.Layers.FirstOrDefault() as FeatureLayer;
                            foreach (FeatureLayer layer in layers) //on boucle jusqu'à récupérer une couche qui appartient à la géodatabase partagée
                            {
                                Debug.WriteLine("boucle");
                                Datastore dataStore = layer.GetFeatureClass().GetDatastore();
                                Debug.WriteLine(dataStore.GetConnectionString());
                                string connexionString = dataStore.GetConnectionString();
                                string dataStoreUser = null;
                                if (connexionString.Split("=")[0] != "DATABASE") // On vérifie que le datastore n'est pas un fichier .gdb
                                {
                                    dataStoreUser = dataStore.GetConnectionString().Split(';')[7].Split("=")[1];    //nom du user propriétaire du layer. Il doit etre égal au user de la connexion sde
                                }
                                Debug.WriteLine("USER NAME = " + dataStoreUser);
                                if (dataStoreUser == sdeUser)
                                {
                                    layerName = layer.Name;
                                    selectedLayer = layer;
                                    break;
                                }

                            }
                            Debug.WriteLine("LAYER NAME = " + layerName);

                        // Changer la version de la couche
                        VersionManager versionManager = geodatabase.GetVersionManager();

                            await QueuedTask.Run(() =>
                            {
                                Geodatabase layerGdb = null;

                                var layer = selectedLayer;

                                using (var table = layer.GetTable())
                                {
                                    layerGdb = table.GetDatastore() as Geodatabase;
                                }

                                var vm = layerGdb.GetVersionManager();

                                var currentVersion = vm.GetCurrentVersion();

                                var targetVersion = vm.GetDefaultVersion();

                                if (targetVersion == null)
                                    return;

                                MapView.Active.Map.ChangeVersion(currentVersion, targetVersion);
                            });

                            // Delais pour attendre la fin de Change Version avant de selectionner une couche pour lancer la réconciliation
                            // Si la sélection de la couche est faite avant la fin de l'exécution  de ChangeVersion la sélection s'annule quand la table du contenu est rechargée.
                            await Task.Delay(3000);

                            // On saute la Synchronisation et on lance la Reconciliation directement
                            Debug.WriteLine("Pas besoin de synchro car le réplica n'existe pas");

                            Debug.WriteLine("DEBUT RECONCILIATION");

                            state_id_ini = get_state_id_default(geodatabase).Item1;

                            await QueuedTask.Run(async () =>
                            {
                                ArcGIS.Core.Data.Version newVersion = versionManager.GetCurrentVersion();
                                Debug.WriteLine("currentVersion = " + newVersion.GetName());

                                ProgressionText = ReconcileRunningMessage;
                                // paramètres utilisés par la fonction ReconcileVersions
                                string input_database = sdePath;
                                string reconcile_mode = "ALL_VERSIONS";
                                string target_version = "SDE.DEFAULT";
                                string edit_versions = versionName;
                                string acquire_locks = "LOCK_ACQUIRED";
                                string abort_if_conflicts = "ABORT_CONFLICTS";
                                string conflict_definition = "BY_OBJECT";
                                string conflict_resolution = "FAVOR_EDIT_VERSION";
                                string with_post = "POST";
                                string with_delete = "DELETE_VERSION";
                                string out_log = ProjectPath + @"\log_reconciliation_" + versionName + "_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".txt";
                                string reconcile_checkout_versions = "RECONCILE";

                                // Geoprocessing.ExecuteToolAsync prend en argument un nom de géotraitement (tool) et une liste de paramètres (args)
                                var tool = "management.ReconcileVersions";
                                var args = Geoprocessing.MakeValueArray(input_database, reconcile_mode, target_version, edit_versions, acquire_locks, abort_if_conflicts, conflict_definition, conflict_resolution, with_post, with_delete, out_log, reconcile_checkout_versions);


                                Debug.WriteLine("Lancement de la réconciliation");
                                // execution GP Reconciliation 
                                IGPResult gp_result = await Geoprocessing.ExecuteToolAsync(tool, args, null, null, null, GPExecuteToolFlags.GPThread);
                                Geoprocessing.ShowMessageBox(gp_result.Messages, "GP Messages", gp_result.IsFailed ? GPMessageBoxStyle.Error : GPMessageBoxStyle.Default);
                                Debug.WriteLine("Réconciliation terminée");



                                if (!gp_result.IsFailed)
                                {
                                    // Si la réconciliation est réussie
                                    Debug.WriteLine("Réconciliation REUSSIE");
                                    var warnings = gp_result.Messages.Where(it => it.Type == GPMessageType.Warning);
                                    bool conflictDetected = false;
                                    bool simultaneousReconcile = false;
                                    bool warning160360 = false;
                                    bool warning003355 = false;
                                    foreach (var warning in warnings) //On boucle sur les warning pour vérifier si on obtient le warning 000084 qui indique la présence de conflits
                                    {
                                        Debug.WriteLine(warning);
                                        if (warning.ToString().Contains("WARNING 000084:"))
                                        {
                                            conflictDetected = true; //en cas de conflit, la variable conflictDetected passe à true
                                        }
                                        if (warning.ToString().Contains("WARNING 160260:"))
                                        {
                                            simultaneousReconcile = true; //en cas de réconciliation interrompue car une autre réconciliation est déjà en cours, la variable simultaneousReconcile passe à true
                                        }
                                        if (warning.ToString().Contains("160360"))
                                        {
                                            warning160360 = true; //en cas de réconciliation interrompue car la version est déjà en cours de réconciliation, la variable warning160360 passe à true
                                        }
                                        if (warning.ToString().Contains("003355"))
                                        {
                                            warning003355 = true; //en cas d'erreur dans la réconciliation de la version, la variable simultaneousReconcile passe à true
                                        }
                                    }
                                    if (!conflictDetected && !simultaneousReconcile && !warning160360 && !warning003355)
                                    {
                                        Debug.WriteLine("Réconciliation REUSSIE sans conflit");
                                        // Ouverture de pop-up pour annoncer que le traitement est OK
                                        state_id_fin = get_state_id_default(geodatabase).Item1;
                                        date = DateTime.Parse(get_state_id_default(geodatabase1).Item2);
                                        updateLogTable(geodatabase, userName, versionName, "réconciliation", "succès", date, state_id_ini, state_id_fin);
                                        //suppression fichier replica.txt

                                        string txtFileName = "replica.txt";
                                        string txtFilePath = System.IO.Path.Combine(gdbPath, txtFileName);
                                        try
                                        {
                                            if (File.Exists(txtFilePath))
                                            {
                                                File.Delete(txtFilePath);
                                                Debug.WriteLine("Fichier supprimé avec succès.");
                                            }
                                            else
                                            {
                                                Debug.WriteLine("Le fichier n'existe pas.");
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Debug.WriteLine($"Erreur : {ex.Message}");
                                        }
                                        ProgressionText = ReconciliationOkMessage;
                                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(ReconciliationOkMessage);
                                    }
                                    else if (simultaneousReconcile)
                                    {
                                        Debug.WriteLine("Réconciliation AVORTEE car une autre réconciliation est déjà en cours");
                                        state_id_fin = get_state_id_default(geodatabase1).Item1; //state_id récupéré à la fin de l'étape de réconciliation
                                        date = DateTime.Now;
                                        updateLogTable(geodatabase1, userName, versionName, "réconciliation", "KO (réconciliation déjà en cours", date, state_id_ini, state_id_fin);
                                        // Ouverture de pop-up pour annoncer que la réconciliation est interrompue car une résolution de conflits est déjà en cours
                                        ProgressionText = SimultaneousReconcileMessage;
                                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(SimultaneousReconcileMessage);
                                    }
                                    else if (warning160360)
                                    {
                                        Debug.WriteLine("Réconciliation AVORTEE car la version est déjà en cours de réconciliation");
                                        state_id_fin = get_state_id_default(geodatabase1).Item1; //state_id récupéré à la fin de l'étape de réconciliation
                                        date = DateTime.Now;
                                        updateLogTable(geodatabase1, userName, versionName, "réconciliation", "KO (version déjà en cours de réconciliation)", date, state_id_ini, state_id_fin);
                                        // Ouverture de pop-up pour annoncer que la réconciliation est interrompue car la version est déjà en cours de réconciliation
                                        ProgressionText = Warning160360Message;
                                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(Warning160360Message);
                                    }
                                    else if (warning003355)
                                    {
                                        Debug.WriteLine("Réconciliation en erreur");
                                        state_id_fin = get_state_id_default(geodatabase1).Item1; //state_id récupéré à la fin de l'étape de réconciliation
                                        date = DateTime.Now;
                                        updateLogTable(geodatabase1, userName, versionName, "réconciliation", "KO (Erreur lors de la réconciliation de la version)", date, state_id_ini, state_id_fin);
                                        ProgressionText = Warning003355Message;
                                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(Warning003355Message);
                                    }
                                    else
                                    {
                                        state_id_fin = get_state_id_default(geodatabase).Item1;
                                        // OUVERTURE DE LA FENETRE DE GESTION DES CONFLITS
                                        Debug.WriteLine("Réconciliation AVORTEE car Conflit");
                                        date = DateTime.Now;
                                        updateLogTable(geodatabase, userName, versionName, "réconciliation", "conflit", date, state_id_ini, state_id_fin);

                                        // Ouverture de pop-up pour annoncer que la réconciliation est interrompue car des conflits ont été détectés
                                        ProgressionText = ReconciliationAbortMessage;
                                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(ReconciliationAbortMessage);

                                        // Pour ouvrir la fenêtre de gestion de conflits il y a 3 étapes:
                                        // 1 - Changer la version de la géodatabase partagée pour se mettre sur la version du réplica (version à réconcilier avec DEFAULT)
                                        // 2 - On sélectionne une couche de la géodatabase partagée
                                        // 3 - Lancer le bouton reconcile

                                        // Obtenez le projet en cours
                                        project = Project.Current;
                                        // Obtenez la couche dont vous souhaitez changer la version
                                        map = MapView.Active.Map;
                                        layers = map.GetLayersAsFlattenedList().Where((l) => l is FeatureLayer); // liste de toutes les couches présentes sur la carte
                                        sdeUser = "";
                                        layerName = "";

                                        geodatabase = new Geodatabase(sdeCon);
                                        // Récupérer les classes d'entités (feature classes) de la géodatabase
                                        sdeUser = geodatabase.GetConnectionString().Split(';')[7].Split("=")[1]; //récupération du user de la géodatabase partagée
                                        Debug.WriteLine("GEODATABASE = " + sdeUser);
                                        selectedLayer = MapView.Active.Map.Layers.FirstOrDefault() as FeatureLayer;

                                        foreach (FeatureLayer layer in layers) //on boucle jusqu'à récupérer une couche qui appartient à la géodatabase partagée
                                        {
                                            Datastore dataStore = layer.GetFeatureClass().GetDatastore();
                                            Debug.WriteLine(dataStore.GetConnectionString());
                                            string connexionString = dataStore.GetConnectionString();
                                            string dataStoreUser = null;
                                            if (connexionString.Split("=")[0] != "DATABASE") // On vérifie que le datastore n'est pas un fichier .gdb
                                            {
                                                dataStoreUser = dataStore.GetConnectionString().Split(';')[7].Split("=")[1];    //nom du user propriétaire du layer. Il doit etre égal au user de la connexion sde
                                            }
                                            Debug.WriteLine("USER NAME = " + dataStoreUser);
                                            if (dataStoreUser == sdeUser)
                                            {
                                                layerName = layer.Name;
                                                Debug.WriteLine(" C EST LUI = " + layerName);
                                                selectedLayer = layer;
                                                break;
                                            }

                                        }
                                        Debug.WriteLine("LAYER NAME = " + layerName);


                                        //---------------------------------------------------------------------
                                        // Etape 1 - Changer la version de la géodatabase partagée

                                        await QueuedTask.Run(() =>
                                        {
                                            Geodatabase layerGdb = null;

                                            var layer = selectedLayer;

                                            using (var table = layer.GetTable())
                                            {
                                                layerGdb = table.GetDatastore() as Geodatabase;
                                            }

                                            var vm = layerGdb.GetVersionManager();

                                            var currentVersion = vm.GetCurrentVersion();

                                        var targetVersionName = vm.GetVersionNames()
                                            .FirstOrDefault(v =>
                                                v.Equals(versionName, StringComparison.OrdinalIgnoreCase) && versionManager.GetVersion(v).IsOwner());

                                        if (targetVersionName == null)
                                            return;

                                        MapView.Active.Map.ChangeVersion(currentVersion, versionManager.GetVersion(targetVersionName));
                                    });

                                        // Delais pour attendre la fin de Change Version avant de selectionner une couche pour lancer la réconciliation
                                        // Si la sélection de la couche est faite avant la fin de l'exécution  de ChangeVersion la sélection s'annule quand la table du contenu est rechargée.
                                        await Task.Delay(3000);

                                        //---------------------------------------------------------------------
                                        // Etape 2 - Sélectionner une couche de la géodatabase partagée
                                        await QueuedTask.Run(() =>
                                        {
                                            Debug.WriteLine("DEBUT CHANGEMENT SELECTION");
                                            // select it in the TOC
                                            List<Layer> layersToSelect = new List<Layer>();
                                            layersToSelect.Add(selectedLayer);
                                            MapView.Active.SelectLayers(layersToSelect);
                                            Debug.WriteLine(selectedLayer);//sélection de la couche
                                            Debug.WriteLine("FIN CHANGEMENT SELECTION");
                                            Debug.WriteLine("DEBUT LANCEMENT BOUTON");

                                            //---------------------------------------------------------------------
                                            // Etape 3 - Lancement bouton reconcile pour ouvrir la fenetre de gestion de conflits
                                            string commandId = "esri_editor_reconcileVersionButton";
                                            ICommand iCommand = FrameworkApplication.GetPlugInWrapper(commandId) as ICommand;
                                            if (iCommand != null)
                                            {
                                                iCommand.Execute(null);
                                                Debug.WriteLine("FIN LANCEMENT BOUTON");
                                            }
                                            else
                                            {
                                                Debug.WriteLine("IMPOSSIBLE lancer BOUTON");
                                            }
                                        });

                                    }

                                }
                                else //Réconciliation KO
                                {
                                    state_id_fin = get_state_id_default(geodatabase).Item1;
                                    DateTime date = DateTime.Now;
                                    updateLogTable(geodatabase, userName, versionName, "réconciliation", "KO", date, state_id_ini, state_id_fin);
                                    ProgressionText = ReconciliationKoMessage;
                                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(ReconciliationKoMessage);
                                }
                            });
                        

                    }
                }
                else
                {
                    Debug.WriteLine("La version associée à la géodatabase fichier n'existe pas");
                    ProgressionText = VersionNotExistingMessage;
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(VersionNotExistingMessage);
                }


                Debug.WriteLine("FIN");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Exception non gérée dans TraitementReintegration: " + ex);
                    ProgressionText = "Erreur inattendue: " + ex.Message;
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                        "Erreur inattendue lors de la réintégration :" + Environment.NewLine + ex,
                        "Erreur", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
                finally
                {
                    IsReintegrationRunning = false;
                }
            });
        }
        #endregion

        #region Manage other controls in the DockPane
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
        /// Updates the reconciliation log table after a synchronization or a reconcile operation
        /// </summary>
        public void updateLogTable(Geodatabase geodatabase, string userName, string versionName, string action, string resultat,
            DateTime date, string state_id_ini, string state_id_fin)
        {
            try
            {
                using (Table table = geodatabase.OpenDataset<Table>("data_owner.reconciliation_historique"))
                {
                    Debug.WriteLine("Connexion à la table Ok");
                    WindowsIdentity currentUser = WindowsIdentity.GetCurrent();
                    var userWindows = "";
                    if (currentUser != null)
                    {
                        Debug.WriteLine($"Nom d'utilisateur: {currentUser.Name}");
                        userWindows = currentUser.Name;
                    }
                    else
                    {
                        Debug.WriteLine("Impossible de récupérer l'utilisateur actuel.");
                    }
                    // Créer un nouvel objet RowBuffer pour insérer les données
                    using (RowBuffer rowBuffer = table.CreateRowBuffer())
                    {
                        // Définir les valeurs des colonnes
                        rowBuffer["user_windows"] = userWindows;
                        rowBuffer["user_arcgis"] = userName;
                        rowBuffer["chantier"] = versionName;
                        rowBuffer["action"] = action; // synchronisation ou reconciliation
                        rowBuffer["resultat"] = resultat; // succes ou echec ou conflit
                        //rowBuffer["date"] = DateTime.Now; // Date actuelle
                        rowBuffer["date"] = date;
                        rowBuffer["state_id_ini"] = state_id_ini;
                        rowBuffer["state_id_fin"] = state_id_fin;
                        Debug.WriteLine("Row buffer créé");

                        // Créer et insérer la nouvelle ligne dans la table
                        try
                        {
                            using (Row row = table.CreateRow(rowBuffer))
                            {
                                Debug.WriteLine("Ligne ajoutée");
                                // La ligne est ajoutée, mais il n'est pas nécessaire de faire autre chose
                            }
                        }
                        catch (Exception ex) { Debug.WriteLine("INSERT NOT ALLOWED"); }
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine("Accès à la table impossible"); }
        }

        /// <summary>
        /// Return the geodatabase's state_id and it's creation time
        /// </summary>
        public (string, string) get_state_id_default(Geodatabase geodatabase)
        {
            string state_id = "";
            string creation_time = "";
            using (Table table = geodatabase.OpenDataset<Table>("sde.sde_versions"))
            {
                Debug.WriteLine("la table a bien chargé");
                using (RowCursor rowCursor = table.Search(new ArcGIS.Core.Data.QueryFilter
                {
                    WhereClause = "name='DEFAULT'", // Code du type réplica
                    SubFields = "state_id",
                    PrefixClause = "DISTINCT"
                }))
                {
                    Debug.WriteLine("REQUETE OK");
                    while (rowCursor.MoveNext())
                    {
                        var row = rowCursor.Current;
                        Debug.WriteLine("state_id = " + row["state_id"].ToString());
                        state_id = row["state_id"].ToString();
                    }

                }

            }
            using (Table table = geodatabase.OpenDataset<Table>("sde.sde_states"))
            {
                Debug.WriteLine("la table a bien chargé");
                using (RowCursor rowCursor = table.Search(new ArcGIS.Core.Data.QueryFilter
                {
                    WhereClause = $"state_id={state_id}", // Code du type réplica
                    SubFields = "creation_time",
                    PrefixClause = "DISTINCT"
                }))
                {
                    Debug.WriteLine("REQUETE OK");
                    while (rowCursor.MoveNext())
                    {
                        var row = rowCursor.Current;
                        Debug.WriteLine(row["creation_time"].ToString());
                        creation_time = row["creation_time"].ToString();
                    }

                }

            }
            return (state_id, creation_time);

        }

        /// <summary>
        /// Methode pour afficher le chemin du projet
        /// </summary>
        public static string getProjectPath()
        {
            Debug.WriteLine("ON FAIT APPEL A LA FONCTION getProjectPath");
            var path = @"";
            // Update the combo box with current project styles
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
        /// Methode pour ouvrir un explorateur de fichier pour sélectionner la géodatabase fichier
        /// </summary>
        public void BrowseGdbFileName()
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
            GdbFileFullPath = folderName; //on récupère le chemin complet de la gdb (chemin + nom)
            GdbFileName = GdbFileFullPath.Split('\\').Last().Split('.').First();  //on récupère le nom de la gdb
            Debug.WriteLine($"GdbFileFullPath = {GdbFileFullPath}");
            Debug.WriteLine($"GdbFileName = {GdbFileName}");
            Task tache3 = getDefaultVersion();
        }

        /// <summary>
        /// Mettre à jour la liste des versions de la géodatabase partagée
        /// </summary>    
        public async Task UpdateGeodatabaseVersionsList()
        {
            if (string.IsNullOrEmpty(DataEditorSdeFileFullPath))
            {
                Debug.WriteLine("Veuillez d'abord selectionner une connexion sde");
            }
            else
            {
                await QueuedTask.Run(() =>
                {
                    _geodatabaseVersionsList.Clear();
                    // Chemin de la géodatabase d'entreprise (.sde) qui contient les données
                    string sdePath = DataEditorSdeFileFullPath;
                    // Création d'une connexion à la geodatabase d'entreprise
                    try
                    {
                        var sdeCon = new DatabaseConnectionFile(new Uri(sdePath));

                        Geodatabase geodatabase = new Geodatabase(sdeCon);
                        VersionManager versionManager = geodatabase.GetVersionManager();
                        IReadOnlyList<string> versions = versionManager.GetVersionNames();

                        Debug.WriteLine("Le nom du projet est: " + ProjectName);

                        foreach (var version in versions) // On boucle sur toutes les versions de la géodatabase paratagée (sauf la version default)
                        {
                            if (version != "sde.DEFAULT")
                            {
                                _geodatabaseVersionsList.Add(version);
                                Debug.WriteLine(version);
                            }
                        }
                        NotifyPropertyChanged(() => GeodatabaseVersionsList);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Une erreur s'est produite lors de la connexion à la géodatabase : " + ex.Message);
                    }
                });
            }
        }

        /// <summary>
        /// Récupération du nom du réplica par défaut (version ayant le même nom que la géodatabase fichier)
        /// </summary>
        public async Task getDefaultVersion()
        {
            // Création d'une connexion à la geodatabase d'entreprise
            await QueuedTask.Run(() =>
            {
                try
                {
                    var sdeCon = new DatabaseConnectionFile(new Uri(DataEditorSdeFileFullPath));
                    Geodatabase geodatabase = new Geodatabase(sdeCon);
                    VersionManager versionManager = geodatabase.GetVersionManager();
                    IReadOnlyList<string> versions = versionManager.GetVersionNames();


                    string sdeUser = geodatabase.GetConnectionString().Split(';')[7].Split("=")[1]; // on récupère le user de la géodatabase partagée
                    Debug.WriteLine("CA NOUS  DONNERAIT UNE VERSION: " + sdeUser + "." + GdbFileName);
                    // Copiez les versions dans la liste temporaire
                    foreach (var version in versions) // Pour chaque version de la géodatabase partagée, on vérifie si son nom est égal à sdeUser.GdbFileName si c'est le cas cette version est la version liée au réplica
                    {
                        Debug.WriteLine("Version : " + version);
                        if (version == sdeUser + "." + GdbFileName)
                        {
                            _selectedVersion = version;
                            Debug.WriteLine("VERSION sélectionnée par défaut: " + _selectedVersion);
                            NotifyPropertyChanged(() => SelectedVersion);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Une erreur s'est produite lors de la connexion à la géodatabase : " + ex.Message);
                }

            });
        }


        public static string getReplica(string fullPathGdb)
        {
            string fileName = "replica.txt";
            string filePath = System.IO.Path.Combine(fullPathGdb, fileName);
            bool folderExists = true;
            Debug.WriteLine("fullPathGdb = " + fullPathGdb);
            if (!Directory.Exists(fullPathGdb))
            {
                folderExists = false;
            }
            if (folderExists)
            {
                if (!File.Exists(filePath))
                {
                    var pathProExe = System.IO.Path.GetDirectoryName(new Uri(Assembly.GetEntryAssembly().Location).AbsolutePath);
                    if (pathProExe == null) return "ERROR_ArcGIS_Pro_Path";
                    pathProExe = Uri.UnescapeDataString(pathProExe);
                    pathProExe = System.IO.Path.Combine(pathProExe, @"Python\envs\arcgispro-py3");

                    var pathPython = System.IO.Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().Location).AbsolutePath);
                    if (pathPython == null) return "ERROR_Python_Path";
                    pathPython = Uri.UnescapeDataString(pathPython);
                    pathPython = pathPython + @"\_Reintegration";

                    var fullPathProExe = System.IO.Path.Combine(pathProExe, "python.exe");
                    var fullPathPython = System.IO.Path.Combine(pathPython, "get_replica.py");
                    Debug.WriteLine("fullPathGdb = " + fullPathGdb);
                    Debug.WriteLine("fullPathProExe = " + fullPathProExe);
                    Debug.WriteLine("fullPathPython = " + fullPathPython);
                    var myArguments = $@"""{fullPathPython}"" ""{fullPathGdb}""";

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
                    result = result.TrimEnd('\r', '\n');
                    Debug.WriteLine("RESULT = " + result);

                    if (result == "ERROR_None")
                    {
                        System.Windows.MessageBox.Show("Erreur pas de replica dans cette gdb");
                    }
                    else if (result == "ERROR_Several")
                    {
                        System.Windows.MessageBox.Show("Erreur plusieurs réplicas dans cette gdb");
                    }
                    else if (result == "Exception occurred!")
                    {
                        System.Windows.MessageBox.Show("Erreur lors de la récupération du replica dans la gdb");
                    }
                    else
                    {
                        File.WriteAllText(filePath, result);
                        Debug.WriteLine($"Le fichier {fileName} a été créé et le mot " + result + " a été écrit dedans.");
                        //System.Windows.MessageBox.Show(result);
                    }
                    return result;
                }
                else
                {
                    using (StreamReader reader = new StreamReader(filePath))
                    {
                        string firstLine = reader.ReadLine();
                        Debug.WriteLine($"La première ligne du fichier est : {firstLine}");
                        //System.Windows.MessageBox.Show(firstLine);
                        return firstLine;
                    }
                }
            }
            else
            {
                return "ERROR_gdb_Name";
            }
        }

        public static string getListReplica(string fullPathSde)
        {
            var pathProExe = System.IO.Path.GetDirectoryName(new Uri(Assembly.GetEntryAssembly().Location).AbsolutePath);
            if (pathProExe == null) return "ERROR_ArcGIS_Pro_Path";
            pathProExe = Uri.UnescapeDataString(pathProExe);
            pathProExe = System.IO.Path.Combine(pathProExe, @"Python\envs\arcgispro-py3");

            var pathPython = System.IO.Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().Location).AbsolutePath);
            if (pathPython == null) return "ERROR_Python_Path";
            pathPython = Uri.UnescapeDataString(pathPython);
            pathPython = pathPython + @"\_Reintegration";

            var fullPathProExe = System.IO.Path.Combine(pathProExe, "python.exe");
            var fullPathPython = System.IO.Path.Combine(pathPython, "get_list_replica.py");
            Debug.WriteLine("fullPathSde = " + fullPathSde);
            Debug.WriteLine("fullPathProExe = " + fullPathProExe);
            Debug.WriteLine("fullPathPython = " + fullPathPython);
            var myArguments = $@"""{fullPathPython}"" ""{fullPathSde}""";

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
            result = result.TrimEnd('\r', '\n');
            Debug.WriteLine("RESULT = " + result);
            return result;
        }

        public bool checkReplicaExists(string sdePath, string replicaID)
        {
            bool result = false;
            string listReplicaString = getListReplica(sdePath);
            // Nettoyage du texte : suppression des crochets et des apostrophes
            string cleanedInput = listReplicaString.Replace("[", "").Replace("]", "").Replace("'", "").Trim();
            // Division du texte en fonction des virgules
            string[] elements = cleanedInput.Split(',');
            // Transformation en liste
            List<string> listReplica = new List<string>(elements);
            // Affichage des éléments
            foreach (string element in listReplica)
            {
                Debug.WriteLine(element.Trim());  // Trim pour supprimer les espaces éventuels
            }

            // Création d'une connexion à la geodatabase d'entreprise
            var sdeCon = new DatabaseConnectionFile(new Uri(sdePath));

            // Connexion à la géodatabase partagée
            using (Geodatabase geodatabase = new Geodatabase(sdeCon))
            {

                // On parcourt la liste pour vérifier si le réplica existe bien
                foreach (string element in listReplica)
                {
                    Debug.WriteLine($"elementTrimUpper : {element.Trim().ToUpper()}");
                    Debug.WriteLine($"replicaID : {replicaID}");
                    if (element.Trim().ToUpper() == replicaID) // Trim pour supprimer les espaces éventuels
                    {
                        result = true;
                        Debug.WriteLine($"Le replica {element} est validé");
                        break;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Vérifie qu'il n'y a aucune session d'édition en cours auquel cas, il est proposé la désactivation des outils
        /// et la sauvegarde des données.
        /// Retourne l'état courant de la session d'édition
        /// </summary>
        public static Task<bool> IsBeingEdited(out bool bedit)
        {
            bedit = Project.Current.IsEditingEnabled;
            return QueuedTask.Run<bool>(async () =>
            {
                //vérifie s'il y a des objets en éditions
                if (Project.Current.HasEdits)
                {
                    string message3 = CultureResourcesManager.GetInstance().dictLanguage.GetString("Save");
                    string message4 = CultureResourcesManager.GetInstance().dictLanguage.GetString("SaveEditQuestion");
                    var res = ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(message4, message3, MessageBoxButton.YesNoCancel);
                    if (res == MessageBoxResult.Cancel)
                        return (false);
                    else if (res == MessageBoxResult.No)
                        await (_ = Project.Current.DiscardEditsAsync());
                    else
                    {
                        await (_ = Project.Current.SaveEditsAsync());
                    }
                }
                return (true);
            });
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
    internal class IncaReintegrationView_ShowButton : Button
    {
        protected override void OnClick()
        {
            IncaReintegrationViewModel.Show();

        }
    }
}
