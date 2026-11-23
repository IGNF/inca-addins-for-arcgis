using System;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

namespace IncaEditing._FeatureEdit
{
  internal class AddParapetControls : Button
  {
        protected override void OnClick()
        {
            QueuedTask.Run(() =>
                {
                    try
                    {
                        CheckEverythingIsOk();
                    }
                    catch (Exception exc)
                    {
                        // Catch any exception found and display in a message box
                        MessageBox.Show("Exception caught while trying to run Python tool: " + exc.Message);
                        return;
                    }
                });
        }

        /// <summary>
        ///  Fonction asynchrone qui appelle la fonction d'ajout des parapets
        /// </summary>
        private async static void CheckEverythingIsOk()
        {
            // Une carte doit �tre active
            var mapView = MapView.Active;
            if (mapView == null) return;
            try
            {
                //verifie qu'il n'y a pas de session d'édition en cours
                //variable qui stocke l'état d'édition du projet
                var bedit = false;
                if (await AddParapetCore.IsBeingEdited(out bedit) == false)
                {
                    // Message d'alerte
                    string message = CultureResourcesManager.GetInstance().dictLanguage.GetString("CancelOperation");
                    MessageBox.Show(message, "Info");
                    return;
                }

                //lance le calcul des parapets
                try
                {
                    // Controle des entités de la sélection sur la sélection
                    if (MapView.Active.Map.SelectionCount != 0)
                    {
                        await AddParapetCore.GetInstance().AddParapetFeatureControl();
                    }
                    else
                    {
                        string message = CultureResourcesManager.GetInstance().dictLanguage.GetString("SelectOneObject");
                        MessageBox.Show(message, "Error");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($@"Error: {ex}");
                }
                await Project.Current.SetIsEditingEnabledAsync(bedit);
            }
            catch (Exception ex)
            {
                MessageBox.Show($@"Error: {ex}");
            }
        }
    }
}
