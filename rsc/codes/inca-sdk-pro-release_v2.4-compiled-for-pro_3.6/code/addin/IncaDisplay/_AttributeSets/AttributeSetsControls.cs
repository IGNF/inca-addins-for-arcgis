using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Linq;

namespace IncaDisplay._AttributeSets
{
    internal class AttributeSetsControls
    {
        #region Parameters
        /// <summary>
        /// Instance de la classe 
        /// </summary>
        private static AttributeSetsControls _instance;

        /// <summary>
        /// // Nom du fichier Excel de configuration des règles d'affichage (chemin absolu)        
        /// </summary>
        private static string _XMLSelectFileFullPath = "";
        public static string XMLSelectFileFullPath
        {
            get { return _XMLSelectFileFullPath; }
            set
            {
                _XMLSelectFileFullPath = value;
                FileNameChangedEvent.Publish(new FileNameChangedEventArgs(XMLSelectFileFullPath));
            }
        }

        /// <summary>
        /// Nom du fichier Excel de configuration au lancement de l'Addin
        /// </summary>
        private static string _origXMLSelectFileFullPath = "";
        #endregion  Parameters

        #region Constructors
        /// <summary>
        /// Retourne l'instance de la classe SelectEditControls
        /// </summary>
        /// <returns>L'instance de la classe</returns>
        public static AttributeSetsControls GetInstance()
        {
            if (_instance == null)
            {
                _instance = new AttributeSetsControls();
            }
            return _instance;
        }
        #endregion Constructors

        #region Settings
        /// <summary>
        /// Chargement des paramètres enregistrés dans les préférences d'ArcGIS Pro
        /// </summary>
        public static void LoadSettings()
        {
            // get the default settings
            Properties.Settings settings = Properties.Settings.Default;

            // assign to the values binding to the controls
            XMLSelectFileFullPath = settings.AttributeSetsConfigFile;

            // keep track of the original values (used for comparison when saving)
            _origXMLSelectFileFullPath = settings.AttributeSetsConfigFile;

        }

        /// <summary>
        /// Enregistrement des paramètres dans les préférences d'ArcGIS Pro
        /// </summary>
        public void SaveSettings()
        {
            // save the new settings
            Properties.Settings settings = Properties.Settings.Default;

            if (XMLSelectFileFullPath != _origXMLSelectFileFullPath)
            {
                settings.AttributeSetsConfigFile = XMLSelectFileFullPath;
                settings.Save();
                _origXMLSelectFileFullPath = XMLSelectFileFullPath;
            }
        }
        #endregion Settings

        #region Fonctions
        /// <summary>
        /// Choix du fichier XML et conservation du chemin du fichier dans les paramètres arcgis
        /// ou récupération s'il en existe déjà un dans les paramètres
        /// </summary>
        /// <returns>
        /// false si le fichier retourne null
        /// true sinon
        /// </returns>
        public static bool BrowseAndCheckFileName(bool ini = false)// ex ImportXML
        {
            // Lecture du XML
            string xmlFilePath = "";
            if (ini)
            {
                if (Properties.Settings.Default.AttributeSetsConfigFile == "")
                {
                    // Sélection du fichier Excel
                    xmlFilePath = BrowseFileName();
                }
                else xmlFilePath = Properties.Settings.Default.AttributeSetsConfigFile;
            }
            else
            {
                // Sélection du fichier Excel
                xmlFilePath = BrowseFileName();
            }
            if (xmlFilePath == null) return false;

            // Conservation du nom de fichier dans les paramètres d'ArcGIS Pro
            XMLSelectFileFullPath = xmlFilePath;
            GetInstance().SaveSettings();
            return true;
        }


        /// <summary>
        /// Affichage de la fenêtre de sélection du fichier Excel de configuration des règles 
        /// </summary>
        /// <returns> Le nom du fichier Excel sélectionné par l'utilisateur</returns>
        public static string BrowseFileName()
        {
            // Création de la fenêtre de dialogue 
            var dlg = new OpenFileDialog
            {
                Title = "Sélectionner un fichier XML",
                // Set filter for file extension and default file extension
                DefaultExt = ".xml",
                Filter = "Fichiers XML (*.xml)|*.xml|Tous les fichiers (*.*)|*.*"
            };

            // Affichage de la fenêtre de dialogue 
            bool? result = dlg.ShowDialog();

            // Retourne le nom du fichier sélectionné
            if (result != true) return null;
            return dlg.FileName;

        }

        /// <summary>
        /// Ouverture de la fenêtre de choix du dossier du fichier et de son nom
        /// </summary>
        /// <returns> Le nom du fichier </returns>
        public string SaveFileName()
        {
            // Création de la fenêtre de dialogue 
            var dlg = new SaveFileDialog
            {
                // Set filter for file extension and default file extension
                DefaultExt = ".xml",
                Filter = "Fichiers XML (*.xml)|*.xml|Tous les fichiers (*.*)|*.*"
            };

            // Affichage de la fenêtre de dialogue 
            dlg.ShowDialog();

            // Retourne le nom du fichier sélectionné           
            return dlg.FileName;
        }


        /// <summary>
        /// Chargement et vérification du fichier Excel de configuration des règles 
        /// </summary> 
        public void BrowseAndSaveFile(ObservableCollection<AttributeSetsRule> rules)
        {
            // Sélection du fichier Excel
            LoadSettings();
            if (_origXMLSelectFileFullPath == "") BrowseAndCheckFileName();

            //string filename = XMLSelectFileFullPath;
            // Sélection du fichier Excel
            string xmlFilePath = SaveFileName();
            if (xmlFilePath == null) return;
            // sauvegarde dans le fichier
            AttributeSetsCore.GetInstance().SaveAttributeSetsRules(xmlFilePath, rules.ToList());
        }

        #endregion Fonctions
    }
}
