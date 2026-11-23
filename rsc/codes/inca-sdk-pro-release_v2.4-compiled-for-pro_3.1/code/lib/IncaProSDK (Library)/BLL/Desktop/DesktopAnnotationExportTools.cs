using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.Mapping;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Editing.Attributes;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using IGN.Inca.ArcGISPro.SDK.DAL;
using IGN.Inca.ArcGISPro.SDK.Commons;
using IGN.Inca.ArcGISPro.SDK.DAL.Enum;

/// <summary>
/// Namespace qui contient l'ensemble d'objets définissant la logique et comportement 
/// du projet INCA - ArcGIS Pro SDK orientés à une application de type Add-In, c'est-à-dire
/// qu'on peut utiliser toutes les librairies d'ArcGIS Pro SDK
/// 
/// /!\ WARNING /!\ 
///     /!\ ATTENTION /!\ 
///         /!\ ACHTUNG /!\
/// 
/// En cours de développement...
/// 
///         /!\ WARNING /!\
///     /!\ ATTENTION /!\ 
/// /!\ ACHTUNG /!\
/// 
/// </summary>
namespace IGN.Inca.ArcGISPro.SDK.BLL.Desktop
{
    /// <summary>
    /// /!\ WARNING /!\ 
    ///     /!\ ATTENTION /!\ 
    ///         /!\ ACHTUNG /!\
    /// 
    /// En cours de développement...
    /// 
    ///         /!\ WARNING /!\
    ///     /!\ ATTENTION /!\ 
    /// /!\ ACHTUNG /!\
    /// </summary>
    public class DesktopAnnotationExportTools
    {
        public DesktopAnnotationExportTools()
        { }

        private static readonly DesktopAnnotationExportTools _singleton = new DesktopAnnotationExportTools();

        public static DesktopAnnotationExportTools GetInstance()
        {
            return _singleton;
        }

        //public List<AnnotationBO> GetAnnotationsFromTable(Table table)
        //{
        //    // https://github.com/esri/arcgis-pro-sdk/wiki/ProSnippets-Annotation#change-annotation-text-graphic

        //    var listAnnotationsBO = new List<AnnotationBO>();

        //    var problemIds = "";

        //    using (RowCursor rowCursor = table.Search(null, false))
        //    {
        //        while (rowCursor.MoveNext())
        //        {
        //            using (var row = rowCursor.Current as AnnotationFeature)
        //            {
        //                var anAnnotationToExport = new AnnotationBO();

        //                //Get the graphic from the anno feature
        //                var graphic = row.GetGraphic();
        //                var textGraphic = graphic as CIMTextGraphic;

        //                if (graphic is null)
        //                {
        //                    problemIds += "\"" + row.GetObjectID() + "\", ";
        //                    continue;
        //                }

        //                // ############## GET THE BASELINE ###############

        //                // get the cimTextGraphic baseline geometry (Geometry type) and put it into the AnnotationBO SHAPE field instance
        //                anAnnotationToExport.Shape = textGraphic.Shape;

        //                // ############## GET THE ATTRIBUTES ###############

        //                anAnnotationToExport.ObjectID = row.GetObjectID();

        //                var featureID = row["FEATUREID"];
        //                anAnnotationToExport.FeatureID = (int?)featureID;

        //                var zOrder = row["ZORDER"];
        //                anAnnotationToExport.ZOrder = (int?)zOrder;

        //                var annotationClassID = row["ANNOTATIONCLASSID"];
        //                anAnnotationToExport.AnnotationClassID = (int?)annotationClassID;

        //                var element = row["ELEMENT"];
        //                anAnnotationToExport.Element = ((System.IO.MemoryStream)element).ToArray();

        //                var symbolID = row["SYMBOLID"];
        //                anAnnotationToExport.SymbolID = (int?)symbolID;

        //                var status = row["STATUS"];
        //                anAnnotationToExport.SymbolID = (short?)status;

        //                var textString = row["TEXTSTRING"];
        //                anAnnotationToExport.TextString = (string)textString;

        //                var fontName = row["FONTNAME"];
        //                anAnnotationToExport.FontName = (string)fontName;

        //                var fontStyle = row["FONTSTYLE"];
        //                anAnnotationToExport.FontStyle = (string)fontStyle;

        //                var fontSize = row["FONTSIZE"];
        //                anAnnotationToExport.FontSize = (double?)fontSize;

        //                var underline = row["UNDERLINE"];
        //                anAnnotationToExport.Underline = Convert.ToBoolean(underline);

        //                var verticalAlignment = row["VERTICALALIGNMENT"];
        //                anAnnotationToExport.VerticalAlignment = (VerticalAlignment?)(short?)verticalAlignment;

        //                var horizontalAlignment = row["HORIZONTALALIGNMENT"];
        //                anAnnotationToExport.HorizontalAlignment = (HorizontalAlignment?)(short?)horizontalAlignment;

        //                var xOffset = row["XOFFSET"];
        //                anAnnotationToExport.XOffset = (double?)xOffset;

        //                var yOffset = row["YOFFSET"];
        //                anAnnotationToExport.YOffset = (double?)yOffset;

        //                var angle = row["ANGLE"];
        //                anAnnotationToExport.Angle = (double?)angle;

        //                var fontLeading = row["FONTLEADING"];
        //                anAnnotationToExport.FontLeading = (double?)fontLeading;

        //                var wordSpacing = row["WORDSPACING"];
        //                anAnnotationToExport.WordSpacing = (double?)wordSpacing;

        //                var characterWidth = row["CHARACTERWIDTH"];
        //                anAnnotationToExport.CharacterWidth = (double?)characterWidth;

        //                var characterSpacing = row["CHARACTERSPACING"];
        //                anAnnotationToExport.CharacterSpacing = (double?)characterSpacing;

        //                /*FlipAngle

        //                    Override*/

        //                /*
        //                attAux = insp.FirstOrDefault(a => a.FieldName == "FEATUREID");
        //                if (attAux != null && attAux.CurrentValue != System.DBNull.Value)
        //                    anAnnotationToExport.FeatureID = (int)attAux.CurrentValue;

        //                attAux = insp.FirstOrDefault(a => a.FieldName == "ZORDER");
        //                if (attAux != null && attAux.CurrentValue != System.DBNull.Value)
        //                    anAnnotationToExport.ZOrder = (int)attAux.CurrentValue;

        //                attAux = insp.FirstOrDefault(a => a.FieldName == "ANNOTATIONCLASSID");
        //                if (attAux != null && attAux.CurrentValue != System.DBNull.Value)
        //                    anAnnotationToExport.AnnotationClassID = (int)attAux.CurrentValue;

        //                attAux = insp.FirstOrDefault(a => a.FieldName == "ELEMENT");
        //                if (attAux != null && attAux.CurrentValue != System.DBNull.Value)
        //                    anAnnotationToExport.Element = ((System.IO.MemoryStream)attAux.CurrentValue).ToArray();

        //                anAnnotationToExport.SymbolID = (long)annotationProperties.SymbolID;

        //                attAux = insp.FirstOrDefault(a => a.FieldName == "STATUS");
        //                if (attAux != null && attAux.CurrentValue != System.DBNull.Value)
        //                    anAnnotationToExport.Status = (short)attAux.CurrentValue;

        //                anAnnotationToExport.TextString = annotationProperties.TextString;

        //                anAnnotationToExport.FontName = annotationProperties.FontName;

        //                anAnnotationToExport.FontStyle = annotationProperties.FontStyle;

        //                anAnnotationToExport.FontSize = (double?)annotationProperties.FontSize;

        //                anAnnotationToExport.Underline = (bool?)annotationProperties.Underline;

        //                anAnnotationToExport.VerticalAlignment = (ArcGIS.Core.CIM.VerticalAlignment?)annotationProperties.VerticalAlignment;

        //                anAnnotationToExport.HorizontalAlignment = (ArcGIS.Core.CIM.HorizontalAlignment?)annotationProperties.HorizontalAlignment;

        //                anAnnotationToExport.XOffset = (double?)annotationProperties.OffsetX;

        //                anAnnotationToExport.YOffset = (double?)annotationProperties.OffsetY;

        //                anAnnotationToExport.Angle = (double?)annotationProperties.Angle;

        //                attAux = insp.FirstOrDefault(a => a.FieldName == "FONTLEADING");
        //                if (attAux != null && attAux.CurrentValue != System.DBNull.Value)
        //                    anAnnotationToExport.FontLeading = (double)attAux.CurrentValue;

        //                anAnnotationToExport.WordSpacing = (double?)annotationProperties.WordSpacing;

        //                anAnnotationToExport.CharacterWidth = (double?)annotationProperties.CharacterWidth;

        //                anAnnotationToExport.CharacterSpacing = (double?)annotationProperties.CharacterSpacing;

        //                anAnnotationToExport.FlipAngle = (double?)annotationProperties.FlipAngle;

        //                attAux = insp.FirstOrDefault(a => a.FieldName == "OVERRIDE");
        //                if (attAux != null && attAux.CurrentValue != System.DBNull.Value)
        //                    anAnnotationToExport.Override = (int)attAux.CurrentValue;

        //                // define the toponym type: DROIT or COURBE
        //                anAnnotationToExport.DefineToponymTypeFromGeom();*/

        //                listAnnotationsBO.Add(anAnnotationToExport);
        //            }
        //        }
        //    }

        //    return listAnnotationsBO;
        //}



        //public List<AnnotationBO> GetAnnotationsListFromAnnotationLayer(AnnotationLayer selectedAnnotationLayer)
        //{
        //    var listAnnotationsBO = new List<AnnotationBO>();


        //    // using (AnnotationFeatureClass annoFeatureClass = table as AnnotationFeatureClass)
        //    // {
        //    //Note: this is a non - recycling cursor off the Table, ~not~the layer
        //    using (RowCursor rowCursor = selectedAnnotationLayer.Search())
        //    {
        //        while (rowCursor.MoveNext())
        //        {
        //            var insp = new Inspector();
        //            var anAnnotationToExport = new AnnotationBO();

        //            var currentId = rowCursor.Current.GetObjectID();
        //            insp.Load(selectedAnnotationLayer, currentId);

        //            // get the annotation properties
        //            var annotationProperties = insp.GetAnnotationProperties();

        //            /*if ((int)insp["FEATUREID"] == 203)
        //                Console.Write("dfdf");

        //            //Get the graphic from the anno feature
        //            var graphic = (rowCursor.Current as AnnotationFeature).GetGraphic();
        //            var textGraphic = graphic as CIMTextGraphic;

        //            var geom = textGraphic.Shape;*/

        //            // ############## GET THE BASELINE ###############

        //            // get the cimTextGraphic baseline geometry (Geometry type) and put it into the AnnotationBO SHAPE field instance
        //            anAnnotationToExport.Shape = annotationProperties.Shape;

        //            // ############## GET THE ATTRIBUTES ###############

        //            ArcGIS.Desktop.Editing.Attributes.Attribute attAux;

        //            anAnnotationToExport.ObjectID = currentId;
        //            attAux = insp.FirstOrDefault(a => a.FieldName == "FEATUREID");
        //            if (attAux != null && attAux.CurrentValue != System.DBNull.Value)
        //                anAnnotationToExport.FeatureID = (int)attAux.CurrentValue;

        //            attAux = insp.FirstOrDefault(a => a.FieldName == "ZORDER");
        //            if (attAux != null && attAux.CurrentValue != System.DBNull.Value)
        //                anAnnotationToExport.ZOrder = (int)attAux.CurrentValue;

        //            attAux = insp.FirstOrDefault(a => a.FieldName == "ANNOTATIONCLASSID");
        //            if (attAux != null && attAux.CurrentValue != System.DBNull.Value)
        //                anAnnotationToExport.AnnotationClassID = (int)attAux.CurrentValue;

        //            attAux = insp.FirstOrDefault(a => a.FieldName == "ELEMENT");
        //            if (attAux != null && attAux.CurrentValue != System.DBNull.Value)
        //                anAnnotationToExport.Element = ((System.IO.MemoryStream)attAux.CurrentValue).ToArray();

        //            anAnnotationToExport.SymbolID = (long)annotationProperties.SymbolID;

        //            attAux = insp.FirstOrDefault(a => a.FieldName == "STATUS");
        //            if (attAux != null && attAux.CurrentValue != System.DBNull.Value)
        //                anAnnotationToExport.Status = (short)attAux.CurrentValue;

        //            anAnnotationToExport.TextString = annotationProperties.TextString;

        //            anAnnotationToExport.FontName = annotationProperties.FontName;

        //            anAnnotationToExport.FontStyle = annotationProperties.FontStyle;

        //            anAnnotationToExport.FontSize = (double?)annotationProperties.FontSize;

        //            anAnnotationToExport.Underline = (bool?)annotationProperties.Underline;

        //            anAnnotationToExport.VerticalAlignment = (VerticalAlignment?)annotationProperties.VerticalAlignment;

        //            anAnnotationToExport.HorizontalAlignment = (HorizontalAlignment?)annotationProperties.HorizontalAlignment;

        //            anAnnotationToExport.XOffset = (double?)annotationProperties.OffsetX;

        //            anAnnotationToExport.YOffset = (double?)annotationProperties.OffsetY;

        //            anAnnotationToExport.Angle = (double?)annotationProperties.Angle;

        //            attAux = insp.FirstOrDefault(a => a.FieldName == "FONTLEADING");
        //            if (attAux != null && attAux.CurrentValue != System.DBNull.Value)
        //                anAnnotationToExport.FontLeading = (double)attAux.CurrentValue;

        //            anAnnotationToExport.WordSpacing = (double?)annotationProperties.WordSpacing;

        //            anAnnotationToExport.CharacterWidth = (double?)annotationProperties.CharacterWidth;

        //            anAnnotationToExport.CharacterSpacing = (double?)annotationProperties.CharacterSpacing;

        //            anAnnotationToExport.FlipAngle = (double?)annotationProperties.FlipAngle;

        //            attAux = insp.FirstOrDefault(a => a.FieldName == "OVERRIDE");
        //            if (attAux != null && attAux.CurrentValue != System.DBNull.Value)
        //                anAnnotationToExport.Override = (int)attAux.CurrentValue;

        //            // define the toponym type: DROIT or COURBE
        //            anAnnotationToExport.DefineToponymTypeBasedOnTheGeometry();

        //            listAnnotationsBO.Add(anAnnotationToExport);
        //        }
        //    }
        //    // }

        //    return listAnnotationsBO;
        //}

        //public async Task<bool> CreateFeatureClassFromTemplate(string outPath, string outName, ArcGISGeometryType geomType, string templatePath, int wkid = 2154)
        //{
        //    var sr = SpatialReferenceBuilder.CreateSpatialReference(wkid);

        //    var has_m = "DISABLED";
        //    var has_z = "DISABLED";

        //    var result = await QueuedTask.Run(async () =>
        //    {
        //        return await Geoprocessing.ExecuteToolAsync("CreateFeatureclass_management",
        //        Geoprocessing.MakeValueArray(outPath, outName, EnumUtils.StringValueOf(geomType), templatePath, has_m, has_z, sr.Wkt));
        //    });

        //    return !result.IsFailed;
        //}

        //public async Task<IGPResult> CreateFeatureClassFromTemplateVoidAsync(string outPath, string outName, ArcGISGeometryType geomType, string templatePath, int wkid = 2154)
        //{
        //    var sr = SpatialReferenceBuilder.CreateSpatialReference(wkid);

        //    var has_m = "DISABLED";
        //    var has_z = "DISABLED";

        //    return await QueuedTask.Run(async () =>
        //    {
        //        return await Geoprocessing.ExecuteToolAsync("CreateFeatureclass_management",
        //        Geoprocessing.MakeValueArray(outPath, outName, EnumUtils.StringValueOf(geomType), templatePath, has_m, has_z, sr.Wkt));
        //    });
        //}



        //public bool ExportAnnotationsIntoFeatureClass(string gdbPath, string featureClassName, List<AnnotationBO> annotationsBO)
        //{
        //    string message = String.Empty;
        //    bool exportResult = false;

        //    QueuedTask.Run(() =>
        //    {
        //        EditOperation editOperation = new EditOperation();

        //        using (Geodatabase geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(gdbPath))))
        //        using (FeatureClass enterpriseTable = geodatabase.OpenDataset<FeatureClass>(featureClassName))
        //        {
        //            geodatabase.ApplyEdits(() =>
        //            {
        //                foreach (var anAnnotation in annotationsBO)
        //                {
        //                    CreatingARow(enterpriseTable, anAnnotation, editOperation);
        //                }

        //                try
        //                {
        //                    exportResult = editOperation.Execute();
        //                    // MessageBox.Show("After editOperation.Execute()");

        //                    if (!exportResult)
        //                    {
        //                        message = editOperation.ErrorMessage;
        //                        // log this as error
        //                        // MessageBox.Show(message);
        //                    }
        //                }
        //                catch (GeodatabaseException exObj)
        //                {
        //                    message = exObj.Message;
        //                    // log this
        //                    // MessageBox.Show(message);
        //                    throw new GeodatabaseException("Problem.");
        //                }
        //            });
        //        }
        //    });

        //    return exportResult;
        //}

        //public void CreatingARow(FeatureClass enterpriseTable, AnnotationBO anAnnotation, EditOperation editOperation)
        //{
        //    //declare the callback here. We are not executing it ~yet~
        //    editOperation.Callback(context =>
        //    {
        //        FeatureClassDefinition tableDefinition = enterpriseTable.GetDefinition();

        //        using (RowBuffer rowBuffer = enterpriseTable.CreateRowBuffer())
        //        {
        //            // Get the attribute fields
        //            // Note : OBJECTID field is not editable.
        //            rowBuffer[nameof(anAnnotation.FeatureID)] = anAnnotation.FeatureID;
        //            rowBuffer[nameof(anAnnotation.ZOrder)] = anAnnotation.ZOrder;
        //            rowBuffer[nameof(anAnnotation.AnnotationClassID)] = anAnnotation.AnnotationClassID;
        //            rowBuffer[nameof(anAnnotation.Element)] = anAnnotation.Element;
        //            rowBuffer[nameof(anAnnotation.SymbolID)] = anAnnotation.SymbolID;
        //            rowBuffer[nameof(anAnnotation.Status)] = anAnnotation.Status;
        //            rowBuffer[nameof(anAnnotation.TextString)] = anAnnotation.TextString;
        //            rowBuffer[nameof(anAnnotation.FontName)] = anAnnotation.FontName;
        //            rowBuffer[nameof(anAnnotation.FontStyle)] = anAnnotation.FontStyle;
        //            rowBuffer[nameof(anAnnotation.FontSize)] = anAnnotation.FontSize;
        //            rowBuffer[nameof(anAnnotation.Underline)] = anAnnotation.Underline;
        //            rowBuffer[nameof(anAnnotation.VerticalAlignment)] = anAnnotation.VerticalAlignment;
        //            rowBuffer[nameof(anAnnotation.HorizontalAlignment)] = anAnnotation.HorizontalAlignment;
        //            rowBuffer[nameof(anAnnotation.XOffset)] = anAnnotation.XOffset;
        //            rowBuffer[nameof(anAnnotation.YOffset)] = anAnnotation.YOffset;
        //            rowBuffer[nameof(anAnnotation.Angle)] = anAnnotation.Angle;
        //            rowBuffer[nameof(anAnnotation.FontLeading)] = anAnnotation.FontLeading;
        //            rowBuffer[nameof(anAnnotation.WordSpacing)] = anAnnotation.WordSpacing;
        //            rowBuffer[nameof(anAnnotation.CharacterWidth)] = anAnnotation.CharacterWidth;
        //            rowBuffer[nameof(anAnnotation.CharacterSpacing)] = anAnnotation.CharacterSpacing;
        //            rowBuffer[nameof(anAnnotation.FlipAngle)] = anAnnotation.FlipAngle;
        //            rowBuffer[nameof(anAnnotation.Override)] = anAnnotation.Override;

        //            rowBuffer[tableDefinition.GetShapeField()] = anAnnotation.Shape;

        //            /*// Get the geometry
        //            if (anAnnotation.Shape is Polyline polyline)
        //            {
        //                rowBuffer[tableDefinition.GetShapeField()] = polyline;
        //            }
        //            else if (anAnnotation.Shape is GeometryBag geomBag)
        //            {

        //                var objects = JObject.Parse(anAnnotation.Shape.ToJson()); // parse as array  
        //                var jGeometries = objects.GetValue("geometries") as JArray;

        //                foreach (JObject jPoints in jGeometries)
        //                {
        //                    var aPart = jPoints.GetValue("points") as JArray;
        //                    List<Coordinate2D> newPartCoordinates = new List<Coordinate2D>();

        //                    foreach (JArray aPoint in aPart)
        //                    {
        //                        newPartCoordinates.Add(new Coordinate2D((double)aPoint[0], (double)aPoint[1]));
        //                    }

        //                    var newPolyline = PolylineBuilderEx.CreatePolyline(newPartCoordinates);
        //                }

        //                rowBuffer[tableDefinition.GetShapeField()] = newPolyline;
        //            }*/

        //            using (Row row = enterpriseTable.CreateRow(rowBuffer))
        //            {
        //                // To Indicate that the attribute table has to be updated.
        //                context.Invalidate(row);
        //            }
        //        }
        //    }, enterpriseTable);
        //}

        //public List<AnnotationBO> CollapseToponymCourbeIntoSingleWord(IEnumerable<AnnotationBO> annotationsCourbes)
        //{
        //    List<AnnotationBO> annotationsCourbesCollapsed = new List<AnnotationBO>();

        //    foreach (var item in annotationsCourbes)
        //    {
        //        char[] delimiters = new char[] { '\r', '\n', ' ' };
        //        var textstringDecomposed = item.TextString.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

        //        if (item.Shape is GeometryBag shapeGeometryBag)
        //        {
        //            var index = 0;

        //            if (textstringDecomposed.Count() != (item.Shape as GeometryBag).PartCount)
        //            {
        //                // log this
        //                throw new Exception("The number of words in the textstring is not the same than the geometries");
        //            }

        //            var objects = JObject.Parse(item.Shape.ToJson()); // parse as array  
        //            var jGeometries = objects.GetValue("geometries") as JArray;

        //            foreach (JObject jPoints in jGeometries)
        //            {
        //                var aPart = jPoints.GetValue("points") as JArray;
        //                List<Coordinate2D> newPartCoordinates = new List<Coordinate2D>();

        //                foreach (JArray aPoint in aPart)
        //                {
        //                    newPartCoordinates.Add(new Coordinate2D((double)aPoint[0], (double)aPoint[1]));
        //                }

        //                var newPolyline = PolylineBuilderEx.CreatePolyline(newPartCoordinates);

        //                var anAnnotationBO = (AnnotationBO)item.Clone();
        //                anAnnotationBO.Shape = newPolyline;
        //                anAnnotationBO.TextString = textstringDecomposed[index];

        //                annotationsCourbesCollapsed.Add(anAnnotationBO);

        //                index++;
        //            }
        //        }
        //        else if (item.Shape is Multipoint shapeMultipoint)
        //        {
        //            var objects = JObject.Parse(shapeMultipoint.ToJson()); // parse as array  
        //            var jPoints = objects.GetValue("points") as JArray;

        //            List<Coordinate2D> newPartCoordinates = new List<Coordinate2D>();

        //            foreach (JArray aPoint in jPoints)
        //            {
        //                newPartCoordinates.Add(new Coordinate2D((double)aPoint[0], (double)aPoint[1]));
        //            }

        //            var newPolyline = PolylineBuilderEx.CreatePolyline(newPartCoordinates);

        //            var anAnnotationBO = (AnnotationBO)item.Clone();
        //            anAnnotationBO.Shape = newPolyline;
        //            anAnnotationBO.TextString = textstringDecomposed[0];

        //            annotationsCourbesCollapsed.Add(anAnnotationBO);
        //        }
        //        else
        //            annotationsCourbesCollapsed.Add(item);
        //    }

        //    return annotationsCourbesCollapsed;
        //}

    }
}
