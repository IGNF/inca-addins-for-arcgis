using ArcGIS.Core.CIM;
using ArcGIS.Core.Data.DDL;
using ArcGIS.Core.Data.Mapping;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;


namespace IncaEditing._ExportSymbols
{
    /// <summary>
    /// Bouton permettant d'exporter les symboles portés par une ou plusieurs couches d'entités
    /// </summary>
    internal class ExportSymbols_Button : Button
    {
        protected override void OnClick()
        {
            // Choix du nom du fichier de styles à créer
            string styleFileName = ExportSymbols.GetInstance().BrowseFileName();

            // Export des symboles vers le nouveau fichier de styles
            if (!string.IsNullOrEmpty(styleFileName))
            {
                ExportSymbols.GetInstance().ExportSymbolsToStyleFile(styleFileName);
            }
        }
    }

    /// <summary>
    /// Export de symboles
    /// </summary>
    class ExportSymbols
    {
        #region Parameters

        /// <summary>
        /// Instance de la classe ExportSymbols
        /// </summary>
        private static ExportSymbols _instance;

        #endregion

        /// <summary>
        /// Création ou appel à l'instance commune de la classe ExportSymbols
        /// </summary>
        /// <returns>L'instance de la classe</returns>
        public static ExportSymbols GetInstance()
        {
            if (_instance == null)
            {
                _instance = new ExportSymbols();
            }
            return _instance;
        }

        /// <summary>
        /// Affichage de la fenêtre de création du fichier de styles dans lequel seront enregistrés les symboles  
        /// </summary>
        /// <returns>Le nom du fichier de styles à créer</returns>
        public string BrowseFileName()
        {
            // Création de la fenêtre de dialogue 
            SaveFileDialog dlg = new SaveFileDialog
            {
                // Set filter for file extension and default file extension
                DefaultExt = ".stylx",
                Filter = "Styles|*.stylx"
            };

            // Affichage de la fenêtre de dialogue 
            Nullable<bool> result = dlg.ShowDialog();

            // Retourne le nom du fichier sélectionné
            if (result != true) return null;
            return dlg.FileName;
        }

        /// <summary>
        /// Enregistrement dans un nouveau fichier de styles des symboles des couches sélectionnées dans la table de contenu.
        /// Pour les annotations, ce sont les symboles de texte de la collection de symboles des couches qui sont exportés.
        /// </summary>
        /// <param name="styleFileName">nom du fichier de styles à créer</param>
        public async void ExportSymbolsToStyleFile(string styleFileName)
        {
            await QueuedTask.Run(() =>
            {
                try
                {
                    // Suppression du fichier de styles s'il existe déjà
                    if (File.Exists(styleFileName))
                        Project.Current.RemoveStyle(styleFileName);
                        File.Delete(styleFileName);

                    // Création du nouveau fichier de styles (.stylx)
                    Project.Current.CreateStyle(styleFileName);
                    StyleProjectItem style = Project.Current.GetItems<StyleProjectItem>().First(x => x.Path == styleFileName);

                    // 1. Export des symboles d'entités de toutes les couches d'entités de la carte active
                    var selectedLayersList = MapView.Active.GetSelectedLayers().OfType<FeatureLayer>();
                    int symbolCount = 0;

                    foreach (var selectedLayer in selectedLayersList)
                    {
                        var featureLayer = selectedLayer as FeatureLayer;

                        // Type géométrique des symboles de la couche
                        StyleItemType symbolType = GetSymbolTypeFromLayer(featureLayer);

                        CIMRenderer currentRenderer = featureLayer.GetRenderer();

                        // Symbologie unique
                        if (currentRenderer is CIMSimpleRenderer)
                        {
                            //Get simple renderer from feature layer
                            CIMSimpleRenderer simpleRenderer = currentRenderer as CIMSimpleRenderer;

                            // Définition du symbole
                            SymbolStyleItem symbolStyleItem = new SymbolStyleItem()
                            {
                                Symbol = simpleRenderer.Symbol.Symbol,
                                ItemType = symbolType,
                                Category = featureLayer.Name,
                                Name = featureLayer.Name,
                                Key = featureLayer.Name,
                                Tags = $"ExportedFromLayer"
                            };

                            // Ajout du symbole au fichier de style
                            style.AddItem(symbolStyleItem);
                            symbolCount++;
                        }

                        // Symbologie à valeurs uniques
                        else if (currentRenderer is CIMUniqueValueRenderer)
                        {
                            CIMUniqueValueRenderer uniqueValueRenderer = currentRenderer as CIMUniqueValueRenderer;
                            foreach (CIMUniqueValueGroup uvrGroup in uniqueValueRenderer.Groups)
                            {
                                foreach (CIMUniqueValueClass uvrClass in uvrGroup.Classes)
                                {
                                    // Définition du symbole
                                    SymbolStyleItem symbolStyleItem = new SymbolStyleItem()
                                    {
                                        Symbol = uvrClass.Symbol.Symbol,
                                        ItemType = symbolType,
                                        Category = featureLayer.Name,
                                        Name = uvrClass.Label,
                                        Key = uvrClass.Label,
                                        Tags = $"ExportedFromLayer"
                                    };

                                    // Ajout du symbole au fichier de style
                                    style.AddItem(symbolStyleItem);
                                    symbolCount++;
                                }
                            }
                        }
                    }

                    // 2. Export des symboles de texte des couches d'annotations de la carte active
                    var selectedAnnoLayersList = MapView.Active.GetSelectedLayers().OfType<AnnotationLayer>();

                    foreach (var annoLayer in selectedAnnoLayersList)
                    {
                        var annoFeatureClass = annoLayer.GetFeatureClass() as AnnotationFeatureClass;
                        AnnotationFeatureClassDefinition fcDefinition = annoFeatureClass.GetDefinition();
                        var fcDescription = new AnnotationFeatureClassDescription(fcDefinition);

                        // Parcours de la collection de symboles de la classe
                        foreach (var symbol in fcDescription.Symbols)
                        {
                            // Définition du symbole de texte
                            SymbolStyleItem symbolStyleItem = new SymbolStyleItem()
                            {
                                Symbol = symbol.Symbol,
                                ItemType = StyleItemType.TextSymbol,
                                Category = annoLayer.Name,
                                Name = symbol.Name,
                                Key = symbol.Name,
                                Tags = $"ExportedFromLayer"
                            };

                            // Ajout du symbole au fichier de style
                            style.AddItem(symbolStyleItem);
                            symbolCount++;
                        }
                    }

                    // Message de confirmation
                    string message = CultureResourcesManager.GetInstance().dictLanguage.GetString("NumberOfExportedSymbolsMessage");
                    message = string.Format(message, symbolCount);
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(message, "", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(ex.Message, "Warning", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
        }

        /// <summary>
        /// Indique le type géométrique des symboles d'une couche d'entités 
        /// </summary>
        /// <param name="featureLayer">couche d'entités à évaluer</param>
        /// <returns>Le type géométrique des symboles de la couche</returns>
        private StyleItemType GetSymbolTypeFromLayer(FeatureLayer featureLayer)
        {
            // Type géométrique de la couche
            var shapeType = featureLayer.ShapeType;
            StyleItemType styleItemType = StyleItemType.PointSymbol;

            // Déduction du type géométrique des symboles
            switch (shapeType)
            {
                case esriGeometryType.esriGeometryPoint:
                case esriGeometryType.esriGeometryMultipoint:
                    styleItemType = StyleItemType.PointSymbol;
                    break;
                case esriGeometryType.esriGeometryEnvelope:
                case esriGeometryType.esriGeometryPolygon:
                    styleItemType = StyleItemType.PolygonSymbol;
                    break;
                case esriGeometryType.esriGeometryLine:
                case esriGeometryType.esriGeometryPolyline:
                case esriGeometryType.esriGeometryPath:
                case esriGeometryType.esriGeometryCircularArc:
                case esriGeometryType.esriGeometryEllipticArc:
                    styleItemType = StyleItemType.LineSymbol;
                    break;
                case ArcGIS.Core.CIM.esriGeometryType.esriGeometryMultiPatch:
                    styleItemType = StyleItemType.MeshSymbol;
                    break;
            }
            return styleItemType;
        }
    }
}
