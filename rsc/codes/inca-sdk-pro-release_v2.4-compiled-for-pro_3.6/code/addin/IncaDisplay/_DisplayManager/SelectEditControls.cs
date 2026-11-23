using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace IncaDisplay._DisplayManager
{
    internal class SelectEditControls
    {
        #region Parameters

        /// <summary>
        /// Instance partagée de la classe SelectEditControls
        /// /// </summary>
        private static SelectEditControls _instance;


        /// <summary>
        /// Nom du fichier Excel de configuration des règles d'affichage (chemin absolu)
        /// </summary>
        private string _excelSelectFileFullPath = "";
        public string ExcelSelectFileFullPath
        {
            get { return _excelSelectFileFullPath; }
            set
            {
                _excelSelectFileFullPath = value;
                FileNameChangedEvent.Publish(new FileNameChangedEventArgs(ExcelSelectFileFullPath));
            }
        }

        /// <summary>
        /// Nom du fichier Excel de configuration au lancement de l'Addin
        /// </summary>
        private string _origExcelSelectFileFullPath = "";
        #endregion  Parameters


        #region Settings

        /// <summary>
        /// Chargement des paramètres enregistrés dans les préférences d'ArcGIS Pro
        /// </summary>
        public void LoadSettings()
        {
            // get the default settings
            Properties.Settings settings = Properties.Settings.Default;

            // assign to the values binding to the controls
            ExcelSelectFileFullPath = settings.SelectEditConfigFile;

            // keep track of the original values (used for comparison when saving)
            _origExcelSelectFileFullPath = settings.SelectEditConfigFile;

        }

        /// <summary>
        /// Enregistrement des paramètres dans les préférences d'ArcGIS Pro
        /// </summary>
        public void SaveSettings()
        {
            // save the new settings
            Properties.Settings settings = Properties.Settings.Default;

            if (ExcelSelectFileFullPath != _origExcelSelectFileFullPath)
            {
                settings.SelectEditConfigFile = ExcelSelectFileFullPath;
                settings.Save();
                _origExcelSelectFileFullPath = ExcelSelectFileFullPath;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Retourne l'instance de la classe SelectEditControls
        /// </summary>
        /// <returns>L'instance de la classe</returns>
        public static SelectEditControls GetInstance()
        {
            if (_instance == null)
            {
                _instance = new SelectEditControls();
            }
            return _instance;
        }

        #endregion


        #region Fonctions
        /// <summary>
        ///Chargement et vérification du fichier Excel de configuration des règles 
        /// </summary>
        /// <returns>Le résultat de l'opération</returns>
        public Task<bool> BrowseAndCheckFileName()
        {
            return QueuedTask.Run(() =>
            {
                // Sélection du fichier Excel
                string filename = BrowseFileName();
                if (filename == null) return false;

                // Chargement et vérification du fichier
                var result = SelectEditCore.GetInstance().LoadSelectEditRules(filename);
                if (!result.Result) return false;

                // Conservation du nom de fichier dans les paramètres d'ArcGIS Pro
                ExcelSelectFileFullPath = filename;
                GetInstance().SaveSettings();

                return true;
            });
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
            bool? result = dlg.ShowDialog();

            // Retourne le nom du fichier sélectionné
            if (result != true) return null;
            return dlg.FileName;
        }

        /// <summary>
        ///Chargement et vérification du fichier Excel de configuration des règles 
        /// </summary>
        /// <returns>Le résultat de l'opération</returns>
        public Task<bool> CheckFileNameAndLoad()
        {
            return QueuedTask.Run(async () =>
            {
                Task<bool> result;
                // Sélection du fichier Excel
                LoadSettings();

                if (_origExcelSelectFileFullPath == "")
                {
                    result = Task.FromResult(await BrowseAndCheckFileName());
                    if (!result.Result) return false;
                }

                string filename = ExcelSelectFileFullPath;
                //vérification et Chargement du fichier
                result = SelectEditCore.GetInstance().LoadSelectEditRules(filename);
                if (!result.Result) return false;

                return true;
            });
        }

        /// <summary>
        /// vérification et affichage du nom du fichier Excel de configuration
        /// </summary>
        /// <returns>Le résultat de l'opération</returns>

        public Task<bool> CheckFileName()
        {
            return QueuedTask.Run(async () =>
            {
                Task<bool> result;
                // Sélection du fichier Excel
                LoadSettings();

                if (_origExcelSelectFileFullPath == "")
                {
                    result = Task.FromResult(await BrowseAndCheckFileName());
                    if (!result.Result) return false;
                }

                string filename = ExcelSelectFileFullPath;
                MessageBox.Show(filename, "INFO", MessageBoxButton.OK);
                return true;


            });
        }

        /// <summary>
        ///Chargement et vérification du fichier Excel de configuration des règles 
        /// </summary>
        /// <returns>Le résultat de l'opération</returns>
        public Task<bool> BrowseAndSaveFile(Map map, string worksheetname)
        {
            return QueuedTask.Run(async () =>
            {
                Task<bool> result;
                // Sélection du fichier Excel
                LoadSettings();
                if (_origExcelSelectFileFullPath == "")
                    await GetInstance().BrowseAndCheckFileName();

                string filename = ExcelSelectFileFullPath;
                // sauvegarde dans le fichier
                result = SelectEditCore.GetInstance().SaveSelectEditRules(map, filename, worksheetname);
                if (!result.Result) return false;

                return true;
            });
        }
        #endregion Fonctions
    }
}
