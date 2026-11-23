using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

namespace IncaEditing._FeatureEdit
{
    public sealed class AddParapetCore
    {
        #region Parameters

        /// <summary>
        /// Instance de la classe AddParapetCore
        /// /// </summary>
        private static AddParapetCore _instance;

        /// <summary>
        /// Retrieve the singleton instance to this module here
        /// </summary>
        private AddParapetCore()
        {

        }

        /// <summary>
        /// Création ou appel à l'instance commune de la classe CastFeaturesCore
        /// </summary>
        /// <returns>L'instance de la classe</returns>
        public static AddParapetCore GetInstance()
        {
            if (_instance == null)
            {
                _instance = new AddParapetCore();
            }
            return _instance;
        }

        /// <summary>
        /// Lance une commande Python dans l'environnement Python d'Arcgis, créant des parapets de manière isolée.
        /// </summary>
        /// <returns>La sortie de la commande</returns>
        /// <param name="param">Dictionnaire contenant les paramétres des entitées sélectionnées</param>
        private string Lauch_Cmd_proc_create_isolated_parapet(Dictionary<string, string> param)
        {

            string geodb_arg = param["gdb"].ToString();
            string fc_arg = param["fc"];
            string oid_arg = param["oid"];

            // Create and format path to Pro
            var pathProExe = System.IO.Path.GetDirectoryName((new Uri(Assembly.GetEntryAssembly().Location)).AbsolutePath);
            if (pathProExe == null) return "Chemin vers pro vide";
            pathProExe = Uri.UnescapeDataString(pathProExe);
            pathProExe = System.IO.Path.Combine(pathProExe, @"Python\envs\arcgispro-py3");

            // Create and format path to Python
            var pathPython = System.IO.Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().Location).AbsolutePath);
            if (pathPython == null) return "Chemin vers python vide";
            pathPython = Uri.UnescapeDataString(pathPython);
            var temp_script_path = System.IO.Path.Combine(pathProExe, @"Scripts", @"run_create_parapets.pyw");
            
            //v�rifie que le chemin vers python existe
            if (System.IO.File.Exists(temp_script_path) == false) {
                MessageBox.Show("Chemin d'acc�s au code python invalide ...\n", "Error");
                return string.Format("Error: {0}", "Chemin d'acc�s au code python invalide ...\n");
            };

            // Create and format process command string.
            var arguments = string.Format(@"""{0}"" -gdb ""{1}"" -fc ""{2}"" -oid ""{3}""",
                System.IO.Path.Combine(pathPython, temp_script_path),
                geodb_arg,
                fc_arg,
                oid_arg);

            // Create process start info, with instruction settings
            var procStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = System.IO.Path.Combine(pathProExe, "pythonw.exe"),
                Arguments = arguments,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Create process and start it
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo = procStartInfo;
            proc.Start();

            proc.WaitForExit();

            // Create and format result string
            string result = proc.StandardOutput.ReadToEnd();
            string error = proc.StandardError.ReadToEnd();

            if (!string.IsNullOrEmpty(error))
            {
                string message = CultureResourcesManager.GetInstance().dictLanguage.GetString("ParapetsFailure");
                message = string.Format("{0}\nError: {1}", message, error);
                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                string message = CultureResourcesManager.GetInstance().dictLanguage.GetString("ParapetsBuild");
                MessageBox.Show(message, "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            return result;
        }

        /// <summary>
        /// Ajoute un ou plusieurs parapet (et ses barbules) en i
        /// </summary>
        /// <returns>Task</bool> vrai si tout s'est bien passé</returns>
        public Task<bool> AddParapetFeatureControl()
        {
            
            // Une carte doit �tre active
            var mapView = MapView.Active;
            if (mapView == null) return Task.FromResult(false);
            try
            {
                string message = CultureResourcesManager.GetInstance().dictLanguage.GetString("WaitParapets");
                var progDialog = new ProgressDialog(message, false);
                progDialog.Show();

                //Entités sélectionnées dans la carte => Collection
                var selection = mapView.Map.GetSelection();

                Dictionary<string, string> mydict = new Dictionary<string, string>();
                List<string> fcList = new List<string>();
                List<string> OID = new List<string>();
                string geodatabase = "";

                // Parcourir le dictionnaire
                foreach (KeyValuePair<MapMember, List<long>> selectedfeature in selection.ToDictionary())
                {
                    MapMember keyMapMember = selectedfeature.Key;
                    List<long> valueLongs = selectedfeature.Value;

                    // Récupération de la classe d'entités & de la couche associées
                    if (selectedfeature.Key is not FeatureLayer) return Task.FromResult(false);
                    var sFeature = selectedfeature.Key as FeatureLayer;

                    // Contrôle de la géométrie de l'objet : linéaire
                    if (sFeature.ShapeType != esriGeometryType.esriGeometryLine && sFeature.ShapeType != esriGeometryType.esriGeometryPolyline)
                    {
                        string message2 = CultureResourcesManager.GetInstance().dictLanguage.GetString("GeomNotLine");
                        MessageBox.Show(message2, "Warning", MessageBoxButton.OK, MessageBoxImage.Error);
                        return Task.FromResult(false);
                    }
                    FeatureClass sFC = sFeature.GetFeatureClass();
                    var dataStore = sFC.GetDatastore();
                    var workspaceNameDef = dataStore.GetConnectionString();
                    var workspaceName = workspaceNameDef.Split('=')[1];
                    geodatabase = workspaceName;

                    // Remplissage du dictionnaire à envoyer au script Python
                    if (fcList.Count() >= 1)
                    {
                        progDialog.Hide();
                        string message2 = CultureResourcesManager.GetInstance().dictLanguage.GetString("OnlyOneClass");
                        throw new Exception(message2);
                    }
                    fcList.Add(sFC.GetName());
                    OID.Add(string.Join(",", valueLongs));
                }

                mydict.Add("gdb", geodatabase);
                mydict.Add("fc", string.Join(";", fcList));
                mydict.Add("oid", string.Join(";", OID));

                // Lancement de la commande de création de parapet(s) isolé(s)
                var result = Lauch_Cmd_proc_create_isolated_parapet(mydict);

                // Fermeture de la barre de progression
                progDialog.Hide();

                // Mise à jour forcée de la carte
                mapView.Redraw(false);
            }
            catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Warning", MessageBoxButton.OK, MessageBoxImage.Error);
                    return Task.FromResult(false);
                }
                return Task.FromResult(true);            
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
                    var res = MessageBox.Show(message4, message3, MessageBoxButton.YesNoCancel);
                    if (res == MessageBoxResult.Cancel)
                        return (false);
                    else if (res == MessageBoxResult.No)
                        await (_ = Project.Current.DiscardEditsAsync());
                    else
                    {
                        await (_ = Project.Current.SaveEditsAsync());
                    }
                }
                await Project.Current.SetIsEditingEnabledAsync(false);
                return (true);
            });
        }
    }
}
#endregion