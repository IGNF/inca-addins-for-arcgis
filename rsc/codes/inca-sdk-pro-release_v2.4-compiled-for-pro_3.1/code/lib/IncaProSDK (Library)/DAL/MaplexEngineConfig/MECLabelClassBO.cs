using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using IGN.Inca.ArcGISPro.SDK.Commons;
using IGN.Inca.ArcGISPro.SDK.DAL.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Namespace qui contient le modèle de données dont l'Add-In "MaplexEngineConfigAddIn" a besoin
/// </summary>
namespace IGN.Inca.ArcGISPro.SDK.DAL.MaplexEngineConfig
{
    /// <summary>
    /// Classe qui aide à la lecture des paramètres provenant du fichier de config et
    /// qui initialise un objet de type CIMLabelClass.
    /// 
    /// Cette classe gère également les processus de binding entre Layers et Label Classes.
    /// 
    /// Code source d'inspiration : https://community.esri.com/thread/222949-how-do-you-create-a-new-label-class
    /// </summary>
    public class MECLabelClassBO
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        readonly static string DEFAULT_LABEL_CLASS_NAME_STARTS_WITH = "Class";

        public MECLabelClassBO() { }

        public CIMLabelClass CIMLabelClass { get; set; }
        public FeatureLayer Layer { get; set; }
        public bool RemoveDefaultLabelClass { get; set; }

        /// <summary>
        /// Creates a label class from parameters in Excel config file
        /// </summary>
        /// <param name="valuesFromExcel">a dictionary representing an entry row in the config file</param>
        /// <param name="stylx">represents the style project item</param>
        /// <returns>an initialized MECLabelClassBO object</returns>
        public static Task<MECLabelClassBO> CreateMECLabelClassBO(Dictionary<string, string> valuesFromExcel, StyleProjectItem stylx)
        {
            return QueuedTask.Run<MECLabelClassBO>(() =>
            {
                MECLabelClassBO newMECLabelClassBO = null;
                ConfigFileUtils dictTools = new ConfigFileUtils(valuesFromExcel);

                string layerName = string.Empty;
                string labelClassName = string.Empty;

                try
                {
                    // start creating a new instance of "MECLabelClassBO"
                    newMECLabelClassBO = new MECLabelClassBO()
                    {
                        RemoveDefaultLabelClass = false
                    };

                    // ~~~~~~~
                    //  LAYER
                    // ~~~~~~~
                    layerName = dictTools.GetArgumentValue("LAYER");

                    // this field is mandatory
                    if (string.IsNullOrWhiteSpace(layerName))
                        throw new ArgumentNullException("'LAYER' field is empty");

                    var layer = dictTools.GetConvertedFeatureLayer("LAYER");

                    // ~~~~~~~~~~~~~~~~~~
                    //  IS_LABEL_VISIBLE
                    // ~~~~~~~~~~~~~~~~~~
                    var isLabelVisible = dictTools.GetArgumentValue("IS_LABEL_VISIBLE");

                    // this field is mandatory
                    if (string.IsNullOrWhiteSpace(isLabelVisible))
                        throw new ArgumentNullException("'IS_LABEL_VISIBLE' field is empty");

                    layer.SetLabelVisibility((bool)dictTools.GetConvertedArgument("IS_LABEL_VISIBLE", ConvertingUtilTypes.BOOL));

                    newMECLabelClassBO.Layer = layer;

                    // ~~~~~~~~~~~~~~~~~~
                    //  LABEL_CLASS_NAME
                    // ~~~~~~~~~~~~~~~~~~
                    labelClassName = dictTools.GetArgumentValue("LABEL_CLASS_NAME");

                    // enable labels on this layer
                    if (newMECLabelClassBO.Layer.IsLabelVisible)
                    {
                        // this field is mandatory
                        if (string.IsNullOrWhiteSpace(labelClassName))
                            throw new ArgumentNullException("'LABEL_CLASS_NAME' field is empty");

                        // ~~~~~~~~~~~~~
                        //  TEXT_SYMBOL
                        // ~~~~~~~~~~~~~
                        var textSymbolName = dictTools.GetArgumentValue("TEXT_SYMBOL");

                        // this field is mandatory
                        if (string.IsNullOrWhiteSpace(textSymbolName))
                            throw new ArgumentNullException("'TEXT_SYMBOL' field is empty");

                        var textSymbol = dictTools.GetStyleItemFromGallery("TEXT_SYMBOL", stylx, StyleItemType.TextSymbol) as CIMSymbolReference;

                        // ~~~~~~~~~~~~~~~~~
                        //  LABEL_PLACEMENT
                        // ~~~~~~~~~~~~~~~~~
                        var labelPlacementName = dictTools.GetArgumentValue("LABEL_PLACEMENT");

                        // this field is mandatory
                        if (string.IsNullOrWhiteSpace(labelPlacementName))
                            throw new ArgumentNullException("'LABEL_PLACEMENT' field is empty");

                        var labelPlacement = dictTools.GetStyleItemFromGallery("LABEL_PLACEMENT", stylx, StyleItemType.MaplexLabelPlacement) as CIMMaplexLabelPlacementProperties;

                        // create the new label class and initialize props
                        var newLabelClass = new CIMLabelClass
                        {
                            Visibility = true,
                            Name = labelClassName,
                            TextSymbol = textSymbol,
                            MaplexLabelPlacementProperties = labelPlacement
                        };

                        // ~~~~~~~~~~
                        //  PRIORITY
                        // ~~~~~~~~~~
                        var priority = dictTools.GetConvertedArgument("PRIORITY", ConvertingUtilTypes.INT);
                        if (priority != null) newLabelClass.Priority = (int)priority;

                        // ~~~~~~~~~~~~~~~~~
                        //  USE_CODED_VALUE
                        // ~~~~~~~~~~~~~~~~~
                        var useCodedValue = dictTools.GetConvertedArgument("USE_CODED_VALUE", ConvertingUtilTypes.BOOL);
                        if (useCodedValue != null) newLabelClass.UseCodedValue = (bool)useCodedValue;

                        // ~~~~~~~~~~~~~~~~~~~~~
                        //  REMOVE_EXTRA_SPACES
                        // ~~~~~~~~~~~~~~~~~~~~~
                        var removeExtraSpaces = dictTools.GetConvertedArgument("REMOVE_EXTRA_SPACES", ConvertingUtilTypes.BOOL);
                        if (removeExtraSpaces != null) newLabelClass.MaplexLabelPlacementProperties.RemoveExtraWhiteSpace = (bool)removeExtraSpaces;

                        // ~~~~~~~~~~~~~~~~~~~~~~~~~~
                        //  REMOVE_EXTRA_LINE_BREAKS
                        // ~~~~~~~~~~~~~~~~~~~~~~~~~~
                        var removeExtraLineBreaks = dictTools.GetConvertedArgument("REMOVE_EXTRA_LINE_BREAKS", ConvertingUtilTypes.BOOL);
                        if (removeExtraLineBreaks != null) newLabelClass.MaplexLabelPlacementProperties.RemoveExtraLineBreaks = (bool)removeExtraLineBreaks;

                        // ~~~~~~~~~~~~~~~~~~~
                        //  EXPRESSION_ENGINE
                        // ~~~~~~~~~~~~~~~~~~~
                        var expressionEngine = dictTools.GetConvertedEnumArgument("EXPRESSION_ENGINE", LabelExpressionEngine.Arcade);
                        if (expressionEngine != null) newLabelClass.ExpressionEngine = (LabelExpressionEngine)expressionEngine;

                        // ~~~~~~~~~~~~
                        //  EXPRESSION
                        // ~~~~~~~~~~~~
                        var expression = dictTools.GetArgumentValue("EXPRESSION");
                        if (expression != null) newLabelClass.Expression = expression;

                        // ~~~~~~~~~~~~~~
                        //  WHERE_CLAUSE
                        // ~~~~~~~~~~~~~~
                        var whereClause = dictTools.GetArgumentValue("WHERE_CLAUSE");
                        if (whereClause != null) newLabelClass.WhereClause = whereClause;

                        // ~~~~~~~~~~~~~~
                        //  MAXIMUM_SCALE
                        // ~~~~~~~~~~~~~~
                        var maximumScale = dictTools.GetConvertedArgument("MAXIMUM_SCALE", ConvertingUtilTypes.DOUBLE);
                        if (maximumScale != null) newLabelClass.MaximumScale = (double)maximumScale;

                        // ~~~~~~~~~~~~~~
                        //  MINIMUM_SCALE
                        // ~~~~~~~~~~~~~~
                        var minimumScale = dictTools.GetConvertedArgument("MINIMUM_SCALE", ConvertingUtilTypes.DOUBLE);
                        if (minimumScale != null) newLabelClass.MinimumScale = (double)minimumScale;

						// AJOUT PYC
						// ~~~~~~~~~~~~~~~~
                        //  FEATURE_WEIGHT
                        // ~~~~~~~~~~~~~~~~
                        var featureWeight = dictTools.GetConvertedArgument("FEATURE_WEIGHT", ConvertingUtilTypes.INT);
                        if (featureWeight != null) newLabelClass.MaplexLabelPlacementProperties.FeatureWeight = (int)featureWeight;

                        // ~~~~~~~~~~~~~~~~~~~~~~~~~
                        //  POLYGON_BOUNDARY_WEIGHT
                        // ~~~~~~~~~~~~~~~~~~~~~~~~~
                        var polygonBoundaryWeight = dictTools.GetConvertedArgument("POLYGON_BOUNDARY_WEIGHT", ConvertingUtilTypes.INT);
                        if (polygonBoundaryWeight != null) newLabelClass.MaplexLabelPlacementProperties.PolygonBoundaryWeight = (int)polygonBoundaryWeight;

                        // FIN AJOUT PYC
						
						// assign CIMLabelClass to MECLabelClassBO object => this label class will replace the default label class
                        newMECLabelClassBO.CIMLabelClass = newLabelClass;

                        // remove default LabelClass
                        newMECLabelClassBO.RemoveDefaultLabelClass = true;
                    }
                    else
                    {
                        try
                        {
                            // Create default Label Class
                            CreateDefaultLabelClass(newMECLabelClassBO.Layer);

                            // Get default label class and update it
                            var lyrDefn = newMECLabelClassBO.Layer.GetDefinition() as CIMFeatureLayer;
                            var listLabelClasses = lyrDefn.LabelClasses.ToList();

                            var defaultLabelClass = listLabelClasses.FirstOrDefault(x => x.Name.StartsWith(DEFAULT_LABEL_CLASS_NAME_STARTS_WITH));
                            if (defaultLabelClass == null)
                                throw new ArgumentNullException(string.Format("Default label class name starting with '{0}' was not found", DEFAULT_LABEL_CLASS_NAME_STARTS_WITH));

                            var maplexLabelProps = defaultLabelClass.MaplexLabelPlacementProperties;// as CIMMaplexLabelPlacementProperties;

                            // ~~~~~~~~~~~~~~~~
                            //  FEATURE_WEIGHT
                            // ~~~~~~~~~~~~~~~~
                            var featureWeight = dictTools.GetConvertedArgument("FEATURE_WEIGHT", ConvertingUtilTypes.INT);
                            if (featureWeight != null) maplexLabelProps.FeatureWeight = (int)featureWeight;

                            //if (maplexLabelProps.FeatureType == LabelFeatureType.Polygon)
                            //{
                            // ~~~~~~~~~~~~~~~~~~~~~~~~~
                            //  POLYGON_BOUNDARY_WEIGHT
                            // ~~~~~~~~~~~~~~~~~~~~~~~~~
                            var polygonBoundaryWeight = dictTools.GetConvertedArgument("POLYGON_BOUNDARY_WEIGHT", ConvertingUtilTypes.INT);
                            if (polygonBoundaryWeight != null) maplexLabelProps.PolygonBoundaryWeight = (int)polygonBoundaryWeight;
                            //}

                            // Set the labelClasses back
                            lyrDefn.LabelClasses = listLabelClasses.ToArray();
                            // Set the layer's definition;
                            newMECLabelClassBO.Layer.SetDefinition(lyrDefn);

                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Problem trying to modify default label class", ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Error(string.Format("LAYER NAME='{0}' - LABEL CLASS NAME='{1}' ", layerName, labelClassName), ex);
                    newMECLabelClassBO.Layer = null;
                }

                return newMECLabelClassBO;
            });
        }

        /// <summary>
        /// Binds the new label class to a layer
        /// </summary>
        /// <returns>true if binding was executed correctly; false if errors were found</returns>
        public Task<bool> BindLabelClassToLayer()
        {
            return QueuedTask.Run<bool>(() =>
            {
                try
                {
                    // Get the layer's label classes
                    var lyrDefn = this.Layer.GetDefinition() as CIMFeatureLayer;
                    var lyrsLabelsClasses = lyrDefn.LabelClasses; //.ToList();
                    if (lyrsLabelsClasses == null) lyrsLabelsClasses = new List<CIMLabelClass>().ToArray();
                    var lyrsLabelsClassesList = lyrsLabelsClasses.ToList();

                    // replace default label class for the new one:
                    var defaultLabelClass = lyrsLabelsClassesList.FirstOrDefault(x => x.Name.StartsWith(DEFAULT_LABEL_CLASS_NAME_STARTS_WITH));

                    // Remove default label class
                    lyrsLabelsClassesList.Remove(defaultLabelClass);
                    // Add the new label class to the labels label class
                    lyrsLabelsClassesList.Add(this.CIMLabelClass);

                    // Set the labelClasses back
                    lyrDefn.LabelClasses = lyrsLabelsClassesList.ToArray();
                    // Set the layer's definition;
                    this.Layer.SetDefinition(lyrDefn);

                    return true;
                }
                catch (Exception ex)
                {
                    var layerName = Layer != null ? this.Layer.Name : null;
                    var labelClassName = this.CIMLabelClass?.Name;
                    var msg = string.Format("Problem trying to bind Label Class='{0}' to Layer='{1}'", layerName, labelClassName);
                    log.Error(msg, ex);
                    return false;
                }
            });
        }

        /// <summary>
        /// Binds a list of new label classes to a layer
        /// </summary>
        /// <returns>true if binding was executed correctly; false if errors were found</returns>
        public static Task<bool> BindLabelClassesToLayer(FeatureLayer layer, List<CIMLabelClass> labelClassesToAdd)
        {
            return QueuedTask.Run<bool>(() =>
            {
                try
                {
                    // Get the layer's label classes
                    var lyrDefn = layer.GetDefinition() as CIMFeatureLayer;
                    var lyrsLabelsClasses = lyrDefn.LabelClasses; //.ToList();
                    if (lyrsLabelsClasses == null) lyrsLabelsClasses = new List<CIMLabelClass>().ToArray();
                    var lyrsLabelsClassesList = lyrsLabelsClasses.ToList();

                    // replace default label class for the new one:
                    var defaultLabelClass = lyrsLabelsClassesList.FirstOrDefault(x => x.Name.StartsWith(DEFAULT_LABEL_CLASS_NAME_STARTS_WITH));

                    // Remove default label class
                    lyrsLabelsClassesList.Remove(defaultLabelClass);
                    // Add the list of new label classes to the Collection
                    lyrsLabelsClassesList.AddRange(labelClassesToAdd);

                    // Set the labelClasses back
                    lyrDefn.LabelClasses = lyrsLabelsClassesList.ToArray();
                    // Set the layer's definition;
                    layer.SetDefinition(lyrDefn);

                    return true;
                }
                catch (Exception ex)
                {
                    var layerName = layer != null ? layer.Name : null;
                    var labelClassesNames = String.Join(", ", labelClassesToAdd.ConvertAll(x => x.Name).ToArray());
                    var msg = string.Format("Problem trying to bind Label Class='{0}' to Layer='{1}'", layerName, labelClassesNames);
                    log.Error(msg, ex);
                    return false;
                }
            });
        }

        /// <summary>
        /// Updates the contextual fields from a given layer
        /// </summary>
        /// <param name="lyr">layer to update</param>
        /// <returns>true if process ended correctly; false if problems were found</returns>
        public static Task<bool> ActivateContextualFields(FeatureLayer lyr)
        {
            return QueuedTask.Run<bool>(() =>
            {
                try
                {
                    var contextualFieldFound = false;

                    // Get the layer's label classes
                    var lyrDefn = lyr.GetDefinition() as CIMFeatureLayer;
                    var lyrsLabelsClasses = lyrDefn.LabelClasses; //.ToList();
                    if (lyrsLabelsClasses == null) lyrsLabelsClasses = new List<CIMLabelClass>().ToArray();
                    var lyrsLabelsClassesList = lyrsLabelsClasses.ToList();

                    foreach (var item in lyrsLabelsClassesList)
                    {
                        var maplexLabelPlaceProps = item.MaplexLabelPlacementProperties;

                        // check if a ROTATION_FIELD has been defined
                        if (maplexLabelPlaceProps.RotationProperties.RotationField != null)
                        {
                            // by activating this property, the rotation field is applied to the labeling props
                            maplexLabelPlaceProps.RotationProperties.Enable = true;
                            contextualFieldFound = true;
                        }
                    }

                    // update only if at least one contextual field has been found
                    if(contextualFieldFound)
                    {
                        // Set the labelClasses back
                        lyrDefn.LabelClasses = lyrsLabelsClassesList.ToArray();
                        // Set the layer's definition;
                        lyr.SetDefinition(lyrDefn);

                        return true;
                    }

                    // otherwise return false
                    return false;
                }
                catch (Exception ex)
                {
                    var layerName = lyr?.Name;
                    var msg = string.Format("Problem activating contextual fields on layer='{0}'", layerName);
                    log.Error(msg, ex);
                    return false;
                }
            });
        }

        /// <summary>
        /// Removes all label classes from a given map
        /// </summary>
        /// <param name="map">map to remove label classes from</param>
        /// <returns>true if process ended correctly; false if problems were found</returns>
        public static Task<bool> RemoveAllLabelClasses(Map map)
        {
            return QueuedTask.Run<bool>(() =>
            {
                try
                {
                    var lyrs = map.GetLayersAsFlattenedList().OfType<FeatureLayer>();

                    foreach (var lyr in lyrs)
                    {
                        var lyrDefn = lyr.GetDefinition() as CIMFeatureLayer;
                        if (lyrDefn.LabelClasses == null)
                            continue;

                        var listLabelClasses = lyrDefn.LabelClasses.ToList();

                        // remove all
                        listLabelClasses.RemoveAll(x => x != null);

                        // set the labelClasses back
                        lyrDefn.LabelClasses = listLabelClasses.ToArray();
                        // set the layer's definition
                        lyr.SetDefinition(lyrDefn);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    var msg = string.Format("Problem removing all Label Classes from map='{0}'", map.Name);
                    log.Error(msg, ex);
                    return false;
                }
            });
        }

        /// <summary>
        /// Creates a default label class in the given layer 
        /// </summary>
        /// <param name="layer">create label class in the specified layer</param>
        /// <returns>true if process ended correctly; false if problems were found</returns>
        public static Task<bool> CreateDefaultLabelClass(FeatureLayer layer)
        {
            return QueuedTask.Run<bool>(() =>
            {
                try
                {
                    // Default Label Class
                    var lyrDefn = layer.GetDefinition() as CIMFeatureLayer;
                    var lyrsLabelsClasses = lyrDefn.LabelClasses; //.ToList();

                    // case : default label class has been removed
                    if (lyrsLabelsClasses == null)
                    {
                        lyrsLabelsClasses = new List<CIMLabelClass>().ToArray();
                        var lyrsLabelsClassesList = lyrsLabelsClasses.ToList();

                        // get the geometry type from layer in order to define the maplex label placement FeatureType
                        var labelFeatureType = LabelFeatureType.Line;
                        if (layer.ShapeType == esriGeometryType.esriGeometryPoint || layer.ShapeType == esriGeometryType.esriGeometryMultipoint)
                            labelFeatureType = LabelFeatureType.Point;
                        else if (layer.ShapeType == esriGeometryType.esriGeometryPolygon || layer.ShapeType == esriGeometryType.esriGeometryEnvelope)
                            labelFeatureType = LabelFeatureType.Polygon;

                        var maplexLabelClassProps = new CIMMaplexLabelPlacementProperties
                        {
                            FeatureType = labelFeatureType,
                            // reset to 0 the weights 'cuz the default values are FeatureWeight=100 and PolygonBoundaryWeight=200
                            FeatureWeight = 0,
                            PolygonBoundaryWeight = 0
                        };

                        // create the new label class and initialize props
                        var newLabelClass = new CIMLabelClass
                        {
                            Visibility = true,
                            Name = DEFAULT_LABEL_CLASS_NAME_STARTS_WITH + " 1", // "Class 1"
                            TextSymbol = SymbolFactory.Instance.ConstructTextSymbol().MakeSymbolReference(),
                            MaplexLabelPlacementProperties = maplexLabelClassProps
                        };

                        lyrsLabelsClassesList.Add(newLabelClass);
                        lyrDefn.LabelClasses = lyrsLabelsClassesList.ToArray();
                        layer.SetDefinition(lyrDefn); //set the layer's definition

                        return true;
                    }
                    else
                        return false;
                }
                catch (Exception ex)
                {
                    var msg = string.Format("Problem creating Default Label Classes for layer='{0}'", layer.Name);
                    throw new Exception(msg, ex);
                }
            });
        }
    }
}
