using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using IGN.Inca.ArcGISPro.SDK.Commons;
using IGN.Inca.ArcGISPro.SDK.DAL.MaplexEngineConfig;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Namespace qui contient l'ensemble d'objets définissant la logique et comportement 
/// du projet INCA - ArcGIS Pro SDK orientés à une application de type Add-In, c'est-à-dire
/// qu'on peut utiliser toutes les librairies d'ArcGIS Pro SDK
/// </summary>
namespace IGN.Inca.ArcGISPro.SDK.BLL.Desktop
{
    /// <summary>
    /// Classe basée sur l'ensemble de librairies ArcGIS PRO SDK et qui a pour but 
    /// de piloter les demandes de l'Add-In "MaplexEngineConfigAddIn".
    /// </summary>
    public sealed class MaplexEngineConfiguration
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private MaplexEngineConfiguration() { }

        private static MaplexEngineConfiguration _instance;

        public static MaplexEngineConfiguration GetInstance()
        {
            if (_instance == null)
            {
                _instance = new MaplexEngineConfiguration();
            }
            return _instance;
        }

        /// <summary>
        /// Method in charge of reading the config file the "Color", "Text Symbol" or
        /// "Maplex Label Placement" tab and loads them into the given style file
        /// </summary>
        /// <param name="excelConfigFileURI">URI of config file</param>
        /// <param name="style">Represents a style projet item</param>
        /// <param name="itemType">Item type to manage: Text Symbol or Maplex Label Placement</param>
        /// <param name="overwrite">Overwrite style item if it already exists?</param>
        /// <returns></returns>
        public async Task<bool> LoadConfigurationsIntoStyleFile(string excelConfigFileURI, StyleProjectItem style, StyleItemType itemType, bool overwrite = true)
        {
            var result = false;

            if (style == null)
            {
                log.Fatal("The style file is null");
                return result;
            }

            try
            {
                // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                //  COLORS
                // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                if (itemType == StyleItemType.Color)
                {
                    log.Debug("Reading the Colors from config file");
                    var colorsFromExcel = ExcelUtils.GetInstance().ReadExcelWorksheet(existingFileURI: excelConfigFileURI, worksheetName: "color", headRowNumber: 1);

                    if (colorsFromExcel != null)
                    {
                        log.Debug("Converting color rows from Excel to MECColorBO objects");
                        var MECColorBOList = new List<MECColorBO>();
                        foreach (var color in colorsFromExcel)
                        {
                            var aMECColorBO = await MECColorBO.CreateMECColorBO(color, style);

                            if (aMECColorBO != null)
                                MECColorBOList.Add(aMECColorBO);
                        }
                        log.Info(string.Format("{0} Colors converted out of {1} entries found.", MECColorBOList.Count, colorsFromExcel.Count));

                        log.Debug("Creating the Colors from MECColorBO objects into the specified .stylx file");
                        var newColorsCreatedOK = 0;
                        var colorsNotOverwritten = 0;
                        var colorsOverwritten = 0;
                        foreach (var item in MECColorBOList)
                        {
                            var resultCode = await LoadStyleItemIntoStyleFile(item, item.ColorName, StyleItemType.Color, style, overwrite);

                            if (resultCode == 0)
                                colorsNotOverwritten++;
                            else if (resultCode == 1)
                                newColorsCreatedOK++;
                            else if (resultCode == 2)
                                colorsOverwritten++;
                        }

                        log.Info(string.Format("{0} new Colors created. {1} overwritten. Total Colors processed: {2}",
                            newColorsCreatedOK,
                            colorsOverwritten,
                            colorsFromExcel.Count));

                        // checking if all entries (rows) were managed successfully
                        if (colorsFromExcel.Count == (colorsNotOverwritten + newColorsCreatedOK + colorsOverwritten))
                            result = true;
                    }
                }
                // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                //  TEXT SYMBOLS
                // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                else if (itemType == StyleItemType.TextSymbol)
                {
                    log.Debug("Reading the Text Symbols from config file");
                    var textSymbolsFromExcel = ExcelUtils.GetInstance().ReadExcelWorksheet(existingFileURI: excelConfigFileURI, worksheetName: "text_symbol", headRowNumber: 2);

                    if (textSymbolsFromExcel != null)
                    {
                        log.Debug("Converting text symbol rows from Excel to MECTextSymbolsBO objects");
                        var MECTextSymbolBOList = new List<MECTextSymbolBO>();
                        foreach (var textSymbol in textSymbolsFromExcel)
                        {
                            var aMECTextSymbolBO = await MECTextSymbolBO.CreateMECTextSymbolBO(textSymbol, style);

                            if (aMECTextSymbolBO != null)
                                MECTextSymbolBOList.Add(aMECTextSymbolBO);
                        }
                        log.Info(string.Format("{0} TextSymbols converted out of {1} entries found.", MECTextSymbolBOList.Count, textSymbolsFromExcel.Count));

                        log.Debug("Creating the Text Symbols from MECTextSymbolsBO objects into the specified .stylx file");
                        var newTextSymbolsCreatedOK = 0;
                        var textSymbolsNotOverwritten = 0;
                        var textSymbolsOverwritten = 0;
                        foreach (var item in MECTextSymbolBOList)
                        {
                            var resultCode = await LoadStyleItemIntoStyleFile(item, item.TextSymbolName, StyleItemType.TextSymbol, style, overwrite);

                            if (resultCode == 0)
                                textSymbolsNotOverwritten++;
                            else if (resultCode == 1)
                                newTextSymbolsCreatedOK++;
                            else if (resultCode == 2)
                                textSymbolsOverwritten++;
                        }

                        log.Info(string.Format("{0} new Text Symbols created. {1} overwritten. Total Text Symbols processed: {2}",
                            newTextSymbolsCreatedOK,
                            textSymbolsOverwritten,
                            textSymbolsFromExcel.Count));

                        // checking if all entries (rows) were managed successfully
                        if (textSymbolsFromExcel.Count == (textSymbolsNotOverwritten + newTextSymbolsCreatedOK + textSymbolsOverwritten))
                            result = true;
                    }
                }
                // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                //  MAPLEX LABEL PLACEMENTS
                // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                else if (itemType == StyleItemType.MaplexLabelPlacement)
                {
                    log.Debug("Reading the Label Placements from config file");
                    var labelPlacementsFromExcel = ExcelUtils.GetInstance().ReadExcelWorksheet(existingFileURI: excelConfigFileURI, worksheetName: "label_placement", headRowNumber: 4);

                    if (labelPlacementsFromExcel != null)
                    {
                        log.Debug("Converting label placement rows from Excel to MECLabelPlacementBO objects");
                        var MECLabelPlacementBOList = new List<MECMaplexLabelPlacementBO>();
                        foreach (var labelPlacement in labelPlacementsFromExcel)
                        {
                            var aMECLabelPlacementBO = await MECMaplexLabelPlacementBO.CreateMECLabelPlacementBO(labelPlacement, style);

                            if (aMECLabelPlacementBO != null)
                                MECLabelPlacementBOList.Add(aMECLabelPlacementBO);
                        }
                        log.Info(string.Format("{0} Label Placements converted out of {1} entries found.", MECLabelPlacementBOList.Count, labelPlacementsFromExcel.Count));

                        log.Debug("Creating the Label Placements from MECLabelPlacementBO objects into the specified .stylx file");
                        var newLabelPlacementsCreatedOK = 0;
                        var labelPlacementsNotOverwritten = 0;
                        var labelPlacementsOverwritten = 0;

                        foreach (var item in MECLabelPlacementBOList)
                        {
                            var resultCode = await LoadStyleItemIntoStyleFile(item, item.LabelPlacementName, StyleItemType.MaplexLabelPlacement, style, overwrite);

                            if (resultCode == 0)
                                labelPlacementsNotOverwritten++;
                            else if (resultCode == 1)
                                newLabelPlacementsCreatedOK++;
                            else if (resultCode == 2)
                                labelPlacementsOverwritten++;
                        }

                        log.Info(string.Format("{0} new Label Placements created. {1} overwritten. Total Label Placements processed: {2}",
                            newLabelPlacementsCreatedOK,
                            labelPlacementsOverwritten,
                            labelPlacementsFromExcel.Count));

                        // checking if all entries (rows) were managed successfully
                        if (labelPlacementsFromExcel.Count == (labelPlacementsNotOverwritten + newLabelPlacementsCreatedOK + labelPlacementsOverwritten))
                            result = true;
                    }
                }
                else
                {
                    // not implemented cases: i.e. North Arrow, Legend, ScaleBar...
                }
            }
            catch (Exception ex)
            {
                log.Fatal(ex.Message, ex);
            }

            return result;
        }

        /// <summary>
        /// Method in charge of writing the "Color", "TextSymbol" or "MaplexLabelPlacement" into the specified style file
        /// </summary>
        /// <param name="mecObject">Instance of "MECColorBO", "MECTextSymbolBO" or "MECLabelPlacementBO" ready to be inserted into style file</param>
        /// <param name="itemName">Style item name</param>
        /// <param name="type">Style item type, only "Color", "TextSymbol" and "MaplexLabelPlacement" are managed in this method</param>
        /// <param name="style">Represents a style projet item</param>
        /// <param name="overwrite">Overwrite style item if it already exists?</param>
        /// <returns>Returns "0" if style item was found but not overwritten, "1" if the item doesn't exist and was created, "2" if it was overwritten and "-1" if an error occurred</returns>
        public Task<int> LoadStyleItemIntoStyleFile(object mecObject, string itemName, StyleItemType type, StyleProjectItem style, bool overwrite = false)
        {
            return QueuedTask.Run<int>(() =>
            {
                var result = -1;

                try
                {
                    switch (type)
                    {
                        case StyleItemType.Color:
                            var color = (MECColorBO)mecObject;
                            ColorStyleItem colorStyleItem = new ColorStyleItem()
                            {
                                Color = color.CIMColor,
                                ItemType = type,
                                Category = "INCA25_color",
                                Name = itemName,
                                Key = color.ColorKey,
                                Tags = "INCA, SDK, MEC, " + itemName
                            };

                            var searchedColors = style.SearchColors(itemName);

                            if (searchedColors.Count == 0)
                            {
                                style.AddItem(colorStyleItem);
                                log.Debug(string.Format("Color '{0}' created", itemName));
                                result = 1;
                            }
                            else
                            {
                                if (overwrite)
                                {
                                    // delete item first
                                    StyleHelper.RemoveItem(style, searchedColors.FirstOrDefault());
                                    // and then create it again
                                    style.AddItem(colorStyleItem);
                                    log.Debug(string.Format("Color '{0}' was overwritten", itemName));
                                    result = 2;
                                }
                                else
                                {
                                    // do nothing
                                    log.Debug(string.Format("Color '{0}' already exists, it wasn't overwritten", itemName));
                                    result = 0;
                                }
                            }

                            break;

                        case StyleItemType.TextSymbol:
                            var textSymbol = (MECTextSymbolBO)mecObject;
                            SymbolStyleItem symbolStyleItem = new SymbolStyleItem()
                            {
                                Symbol = textSymbol.CIMTextSymbol,
                                ItemType = type,
                                Category = "INCA25_text_symbol",
                                Name = itemName,
                                Key = "key___" + itemName,
                                Tags = "INCA, SDK, MEC, " + itemName
                            };

                            var searchedSymbols = style.SearchSymbols(StyleItemType.TextSymbol, itemName);

                            if (searchedSymbols.Count == 0)
                            {
                                style.AddItem(symbolStyleItem);
                                log.Debug(string.Format("Text Symbol '{0}' created", itemName));
                                result = 1;
                            }
                            else
                            {
                                if (overwrite)
                                {
                                    // delete item first
                                    StyleHelper.RemoveItem(style, searchedSymbols.FirstOrDefault());
                                    // and then create it again
                                    style.AddItem(symbolStyleItem);
                                    log.Debug(string.Format("Text Symbol '{0}' was overwritten", itemName));
                                    result = 2;
                                }
                                else
                                {
                                    // do nothing
                                    log.Debug(string.Format("Text Symbol '{0}' already exists, it wasn't overwritten", itemName));
                                    result = 0;
                                }
                            }

                            break;

                        case StyleItemType.MaplexLabelPlacement:

                            var labelPlacement = (MECMaplexLabelPlacementBO)mecObject;
                            LabelPlacementStyleItem labelPlacementStyleItem = new LabelPlacementStyleItem()
                            {
                                LabelPlacement = labelPlacement.CIMMaplexLabelPlacementProps,
                                ItemType = StyleItemType.MaplexLabelPlacement,
                                Category = "INCA25_label_placement",
                                Name = itemName,
                                Key = "key___" + itemName,
                                Tags = "INCA, SDK, MEC, " + itemName
                            };

                            var searchedLabelPlacements = style.SearchLabelPlacements(StyleItemType.MaplexLabelPlacement, itemName);

                            if (searchedLabelPlacements.Count == 0)
                            {
                                style.AddItem(labelPlacementStyleItem);
                                log.Debug(string.Format("Label placement '{0}' created", itemName));
                                result = 1;
                            }
                            else
                            {
                                if (overwrite)
                                {
                                    // delete item first
                                    StyleHelper.RemoveItem(style, searchedLabelPlacements.FirstOrDefault());
                                    // and then create it again
                                    style.AddItem(labelPlacementStyleItem);
                                    log.Debug(string.Format("Label Placement '{0}' was overwritten", itemName));
                                    result = 2;
                                }
                                else
                                {
                                    // do nothing
                                    log.Debug(string.Format("Label Placement '{0}' already exists, it wasn't overwritten", itemName));
                                    result = 0;
                                }
                            }

                            break;
                    }
                }
                catch (Exception ex)
                {
                    log.Error(string.Format("Problem trying to write the {0} '{1}' in the stylx file '{2}'.", type.GetType(), itemName, style.Path), ex);
                }

                return result;
            });
        }

        /// <summary>
        /// Method that creates a Label Class and binds it to a Layer
        /// </summary>
        /// <param name="excelConfigFileURI">URI of config file</param>
        /// <param name="style">Represents a style project item</param>
        /// <returns>true if process ended correctly; false if an error occurred during the process</returns>
        public async Task<bool> CreateLabelClassAndBindToLayer(string excelConfigFileURI, StyleProjectItem style)
        {
            var result = false;

            if (style == null)
            {
                log.Fatal("The style file is null");
                return result;
            }

            try
            {
                log.Debug("Reading the Label Classes from config file");
                var labelClassesFromExcel = ExcelUtils.GetInstance().ReadExcelWorksheet(existingFileURI: excelConfigFileURI, worksheetName: "label_class", headRowNumber: 1);

                if (labelClassesFromExcel != null)
                {
                    log.Debug("Converting label class rows from Excel to MECLabelClassBO objects");

                    if (MapView.Active == null)
                    {
                        throw new Exception("There's no any active map. Please open a Map in order to create the label Classes!");
                    }

                    Map activeMap = MapView.Active.Map;

                    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    // 1. Remove all label classes from all layers of the given map
                    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    if (await MECLabelClassBO.RemoveAllLabelClasses(activeMap))
                    {
                        var labelClassesBound = 0;
                        var defaultLabelClassesUpdated = 0;
                        var newDefaultLabelClassesCreated = 0;
                        var layersContextualFieldsUpdated = 0;
                        var defaultLabelClassLayerFilter = new List<string>();

                        var layerToLabelClassesDictionary = new Dictionary<String, List<MECLabelClassBO>>();

                        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                        // 2. Create label class and store it in a dictionary ranged by layers
                        //    => 1 layer = * label classes
                        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                        foreach (var labelClass in labelClassesFromExcel)
                        {
                            // 2.1. Create a label class 
                            var aMECLabelClassBO = await MECLabelClassBO.CreateMECLabelClassBO(labelClass, style);

                            if (aMECLabelClassBO.Layer != null)
                            {
                                if (aMECLabelClassBO.RemoveDefaultLabelClass)
                                {
                                    // 2.2. Store the label class in the dictionary

                                    if (layerToLabelClassesDictionary.ContainsKey(aMECLabelClassBO.Layer.Name))
                                        // the layer already exists in the dictionary, store the label class in the existing List associated to this layer
                                        layerToLabelClassesDictionary.FirstOrDefault(x => x.Key == aMECLabelClassBO.Layer.Name).Value.Add(aMECLabelClassBO);
                                    else
                                    {
                                        // the layer doesn't exist in the dictionary, create a new List of label classes with the current label class, 
                                        // and then add a new item in the dictionary: the layer associated to the list of label classes
                                        var labelClassesList = new List<MECLabelClassBO>() { aMECLabelClassBO };
                                        layerToLabelClassesDictionary.Add(aMECLabelClassBO.Layer.Name, labelClassesList);
                                    }
                                }
                                else
                                    defaultLabelClassesUpdated++;

                                // flag this layer as a layer WITH label classes
                                defaultLabelClassLayerFilter.Add(aMECLabelClassBO.Layer.Name);
                            }
                        }

                        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                        // 3. Bind a range of layer classes to a layer using the preceding Dictionary in step (2)
                        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                        foreach (var layerNameKey in layerToLabelClassesDictionary.Keys)
                        {
                            var featureLayer = layerToLabelClassesDictionary[layerNameKey].FirstOrDefault().Layer;
                            var cimLabelClassesCollection = layerToLabelClassesDictionary[layerNameKey].ConvertAll(x => x.CIMLabelClass);
                            var resultBindingLabelClassToLayer = await MECLabelClassBO.BindLabelClassesToLayer(featureLayer, cimLabelClassesCollection);

                            if (resultBindingLabelClassToLayer)
                                labelClassesBound += layerToLabelClassesDictionary[layerNameKey].Count;
                        }

                        // loop layers
                        var lyrs = activeMap.GetLayersAsFlattenedList().OfType<FeatureLayer>();

                        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                        // 4. Create default label classes and update contextual fields
                        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                        foreach (var lyr in lyrs)
                        {
                            if (!defaultLabelClassLayerFilter.Contains(lyr.Name))
                            {
                                // create default label class for layers that were not in the config file
                                if (await MECLabelClassBO.CreateDefaultLabelClass(lyr))
                                    newDefaultLabelClassesCreated++;
                            }
                            else
                            {
                                // update contextual fields for all label classes in the current layer
                                var resultActivateContextualFields = await MECLabelClassBO.ActivateContextualFields(lyr);
                                if (resultActivateContextualFields)
                                    layersContextualFieldsUpdated++;
                            }
                        }

                        var errors = labelClassesFromExcel.Count - (labelClassesBound + defaultLabelClassesUpdated);

                        log.Info(string.Format("\r************ SUMMARY (Label Class) ************\n" +
                            "{0} Label Class(es) was/were created and bound to a Layer.\n" +
                            "{1} default Label Class(es) was/were updated.\n" +
                            "{2} new default label classes has/have been created.\n" +
                            "{3} layer(s) having at least 1 contextual field has/have been updated.\n" +
                            "{4} Error(s).", labelClassesBound, defaultLabelClassesUpdated, newDefaultLabelClassesCreated, layersContextualFieldsUpdated, errors));

                        if (errors == 0)
                            result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                log.Fatal(ex.Message, ex);
            }

            return result;
        }
    }
}
