using System.IO;
using System.Linq;
using System.Resources;
using System.Threading;
using System.Windows;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

namespace IncaDisplay._DisplayTools
{
    /// <summary>
    /// Bouton permettant de souligner un toponyme
    /// </summary>
    internal class ClearMaps_Button : Button
    {
        protected override void OnClick()
        {
            // supprime les sélections sur toutes les cartes
            ClearMaps.Go();
        }
    }

    /// <summary>
    /// supprime les sélections sur toutes les cartes
    /// </summary>
    internal class ClearMaps
    {
        private ResourceSet Translator { get; set; }
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #region MESSAGES
        public static string MESSAGE_ERROR
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Impossible d'ouvrir la carte : ";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "No se pueden abrir la tarjeta : ";
                else return "Unable to open the map : ";
            }
        }
        #endregion

        protected ClearMaps()
        {
            // get the dictionary of the current UI language ("CurrentUICulture") using the Culture Resources Manager
            Translator = CultureResourcesManager.GetInstance().dictLanguage;
        }
        public static bool IsValidPath(string path)
        {
            // Vérifie si le chemin est un dossier existant
            if (Directory.Exists(path))
            {
                return true;
            }
            // Vérifie si le chemin est un fichier existant
            else if (File.Exists(path))
            {
                return true;
            }
            // Le chemin n'est ni un fichier ni un dossier existant
            else
            {
                return false;
            }
        }

        internal async static void Go()
        {
            await QueuedTask.Run(() =>
            {
                // récupère la liste des cartes :
                var list_maps = Project.Current.GetItems<MapProjectItem>().ToList();

                // pour chaque carte, on vide la sélection.
                foreach (var map in list_maps)
                {
                    try
                    {
                        map.GetMap().ClearSelection();
                    }
                    catch
                    {
                        MessageBox.Show(MESSAGE_ERROR + map.Name);
                    }
                }
            });
        }
    }
}
