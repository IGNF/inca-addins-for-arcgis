using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using IGN.Inca.ArcGISPro.SDK.Commons;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Button = ArcGIS.Desktop.Framework.Contracts.Button;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace IncaEditing._CastFeatures
{
    /// <summary>
    /// Classe dédiée à la gestion de l'IHM pour le transtypage des entités 
    /// </summary>
    internal class CastFeaturesControls
    {
        #region Parameters

        /// <summary>
        /// Instance partagée de la classe CastFeaturesControls
        /// /// </summary>
        private static CastFeaturesControls _instance;

        /// <summary>
        /// Nom du fichier Excel de configuration des règles de transtypage (chemin absolu)
        /// </summary>
        private string _excelConfigFileFullPath = "";
        public string ExcelConfigFileFullPath
        {
            get { return _excelConfigFileFullPath; }
            set
            {
                _excelConfigFileFullPath = value;
                FileNameChangedEvent.Publish(new FileNameChangedEventArgs(ExcelConfigFileFullPath));
            }
        }

        /// <summary>
        /// Nom du fichier Excel de configuration au lancement de l'Addin
        /// </summary>
        private string _origExcelConfigFileFullPath = "";

        #endregion

        #region Settings

        /// <summary>
        /// Chargement des paramètres enregistrés dans les préférences d'ArcGIS Pro
        /// </summary>
        public void LoadSettings()
        {
            // get the default settings
            IncaEditing.Properties.Settings settings = IncaEditing.Properties.Settings.Default;

            // assign to the values binding to the controls
            ExcelConfigFileFullPath = settings.castFeaturesConfigFile;

            // check existing file path
            if (!string.IsNullOrEmpty(ExcelConfigFileFullPath) && !File.Exists(ExcelConfigFileFullPath))
            {
                ExcelConfigFileFullPath = "";
                settings.castFeaturesConfigFile = "";
                settings.Save();
            }

            // keep track of the original values (used for comparison when saving)
            _origExcelConfigFileFullPath = ExcelConfigFileFullPath;

        }

        /// <summary>
        /// Enregistrement des paramètres dans les préférences d'ArcGIS Pro
        /// </summary>
        public void SaveSettings()
        {
            // save the new settings
            IncaEditing.Properties.Settings settings = IncaEditing.Properties.Settings.Default;

            if (ExcelConfigFileFullPath != _origExcelConfigFileFullPath)
            {
                settings.castFeaturesConfigFile = ExcelConfigFileFullPath;
                settings.Save();
                _origExcelConfigFileFullPath = ExcelConfigFileFullPath;
            }            
        }

        #endregion

        #region Constructors

        protected CastFeaturesControls()
        {
            // initialize "inca_logger_sdk" logger
            LoggerUtils.GetInstance().InitializeLogger();

            // Load settings
            LoadSettings();
        }

        #endregion

        /// <summary>
        /// Retourne l'instance de la classe CastFeaturesControls
        /// </summary>
        /// <returns>L'instance de la classe</returns>
        public static CastFeaturesControls GetInstance()
        {
            if (_instance == null)
            {
                _instance = new CastFeaturesControls();
            }
            return _instance;
        }

        /// <summary>
        /// Affichage de la fenêtre de sélection du fichier Excel de configuration des règles 
        /// </summary>
        /// <returns>Le nom du fichier Excel sélectionné par l'utilisateur</returns>
        public string BrowseFileName()
        {
            // Création de la fenêtre de dialogue 
            OpenFileDialog dlg = new OpenFileDialog
            {
                // Set filter for file extension and default file extension
                DefaultExt = ".xlsx",
                Filter = "Excel Files|*.xls;*.xlsx"
            };

            // Affichage de la fenêtre de dialogue 
            Nullable<bool> result = dlg.ShowDialog();

            // Retourne le nom du fichier sélectionné
            if (result != true) return null;
            return dlg.FileName;
        }

        /// <summary>
        ///Chargement et vérification du fichier Excel de configuration des règles 
        /// </summary>
        /// <returns>Le résultat de l'opération</returns>
        public Task<bool> BrowseAndCheckFileName()
        {
            return QueuedTask.Run<bool>(() =>
            {
                // Sélection du fichier Excel
                string filename = BrowseFileName();
                if (filename == null) return false;

                // Chargement et vérification du fichier
                var result = CastFeaturesCore.GetInstance().LoadCastFeaturesRules(filename);
                if (!result.Result) return false;

                // Conservation du nom de fichier dans les paramètres d'ArcGIS Pro
                ExcelConfigFileFullPath = filename;
                CastFeaturesControls.GetInstance().SaveSettings();

                return true;
            });
        }
    }

    /// <summary>
    /// Bouton permettant d'exécuter le transtypage d'entités
    /// </summary>
    internal class CastFeatures_Button : Button
    {
        public CastFeatures_Button()
        {
            ActiveMapViewChangedEvent.Subscribe(OnActiveMapViewChanged);
            MapSelectionChangedEvent.Subscribe(OnMapSelectionChanged);
            UpdateEnabled();
        }

        private void OnActiveMapViewChanged(ActiveMapViewChangedEventArgs args)
        {
            UpdateEnabled();
        }

        private void OnMapSelectionChanged(MapSelectionChangedEventArgs args)
        {
            UpdateEnabled();
        }

        protected void UpdateEnabled()
        {
            // Bouton actif par défaut
            this.Enabled = true;

            // Désactivation
            var mapView = MapView.Active;
            if (mapView == null || string.IsNullOrEmpty(CastFeaturesControls.GetInstance().ExcelConfigFileFullPath))
            {
                this.Enabled = false;
            }
            else if (MapView.Active.Map.SelectionCount == 0)
            {
                this.Enabled = false;
            }
        }

        protected override void OnClick()
        {
            // Une carte doit être active
            var mapView = MapView.Active;
            if (mapView == null) return;

            // Vérification du fichier contenant les règles de transtypage
            string configFile = CastFeaturesControls.GetInstance().ExcelConfigFileFullPath;
            if (string.IsNullOrEmpty(configFile))
            {
                // Message d'alerte
                string message = CultureResourcesManager.GetInstance().dictLanguage.GetString("ImportCastFeaturesRules");
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(message, "", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Vérification de la validité du fichier
            if (!File.Exists(configFile))
            {
                // Réinitialisation du nom de fichier s'il n'est pas valide
                CastFeaturesControls.GetInstance().ExcelConfigFileFullPath = "";
                CastFeaturesControls.GetInstance().SaveSettings();
                // Message d'alerte
                string message = CultureResourcesManager.GetInstance().dictLanguage.GetString("ImportCastFeaturesRules");
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(message, "", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Chargement des règles de transtypage
            if (!CastFeaturesCore.GetInstance().AreCastFeaturesRulesLoaded())
            {
                var result = CastFeaturesCore.GetInstance().LoadCastFeaturesRules(configFile);
                if (!result.Result) return;
            }

            // Transtypage sur la sélection 
            if (MapView.Active.Map.SelectionCount != 0)
            {
                CastFeaturesCore.GetInstance().CastSelectedFeatures();
            }
        }
    }

    /// <summary>
    /// Bouton permettant de charger le fichier de configuration Excel des règles
    /// </summary>
    internal class CastFeaturesSettings_Button : Button
    {

        public CastFeaturesSettings_Button()
        {
            FileNameChangedEvent.Subscribe(OnUpdateTooltip);
            // Initialisation du tooltip
            UpdateTooltip(CastFeaturesControls.GetInstance().ExcelConfigFileFullPath);
        }

        ~CastFeaturesSettings_Button()
        {
            FileNameChangedEvent.Unsubscribe(OnUpdateTooltip);
        }

        protected override void OnClick()
        {
            // Sélection et chargement du fichier excel
            CastFeaturesControls.GetInstance().BrowseAndCheckFileName();
        }

        protected void OnUpdateTooltip(FileNameChangedEventArgs args)
        {
            UpdateTooltip(args._value);
        }

        protected void UpdateTooltip(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                this.Tooltip = CastFeaturesCore.GetInstance().GetString("ImportCastFeaturesRules");
            else
                this.Tooltip = string.Format(CastFeaturesCore.GetInstance().GetString("CastFeaturesRulesFileName"), fileName);
        }
    }

    /// <summary>
    /// Bouton à cocher pour rechercher les classes d'entités cibles seulement au sein de la géodatabase par défaut du projet
    /// </summary>
    class CastFeatures_default_gdb_CheckBox : ArcGIS.Desktop.Framework.Contracts.CheckBox
    {
        public CastFeatures_default_gdb_CheckBox()
        {
            IsChecked = true;
        }

        protected override void OnClick()
        {
            CastFeaturesCore.GetInstance().SearchInDefaultGdb = (bool)IsChecked;
        }

    }

    /// <summary>
    /// Bouton à cocher pour autoriser les transtypages au sein de la même géodatabase
    /// </summary>
    class CastFeatures_intrabase_CheckBox : ArcGIS.Desktop.Framework.Contracts.CheckBox
    {
        public CastFeatures_intrabase_CheckBox()
        {
            IsChecked = false;
        }

        protected override void OnClick()
        {
            CastFeaturesCore.GetInstance().AllowIntrabase = (bool)IsChecked;
        }

    }
}
