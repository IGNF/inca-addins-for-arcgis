using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.Exceptions;
using ArcGIS.Core.Data.Mapping;
using ArcGIS.Core.Geometry;
using IGN.Inca.ArcGISPro.SDK.DAL;
using IGN.Inca.ArcGISPro.SDK.Commons;

/// <summary>
/// Namespace qui contient l'ensemble d'objets définissant la logique et comportement 
/// du projet INCA - ArcGIS Pro SDK orientés à une application de type Stand-Alone ou 
/// Console qui utilise uniquement la librairie ArcGIS.Core
/// </summary>
namespace IGN.Inca.ArcGISPro.SDK.BLL.CoreHost
{
    /// <summary>
    /// Classe basée sur la librairie ArcGIS.Core.dll et qui a pour but d'exporter 
    /// une Annotation Feature Class vers une Feature Class normale
    /// </summary>
    public class CoreHostAnnoTools
    {
        public CoreHostAnnoTools()
        { }

        private static readonly CoreHostAnnoTools _singleton = new CoreHostAnnoTools();

        /// <summary>
        /// Pattern Singleton
        /// </summary>
        /// <returns>Une instance de la classe CoreHostAnnotationExportTools</returns>
        public static CoreHostAnnoTools GetInstance()
        {
            return _singleton;
        }

        /// <summary>
        /// Méthode qui récupère les annotations à partir d'une Annotation Feature Class 
        /// portée par la classe DatasetBO et qui retourne une liste d'objets de type
        /// AnnotationBO.
        /// https://github.com/esri/arcgis-pro-sdk/wiki/ProSnippets-Annotation#change-annotation-text-graphic
        /// </summary>
        /// <param name="datasetParam">Une Annotation Feature Class vérifiée et encapsulée dans ce paramètre</param>
        /// <param name="customFields">Liste de champs à prendre en compte lors de l'export. Si la liste est 
        /// vide ou null, l'export sera basé sur le modèle classique d'une annotation</param>
        /// <returns>Une liste d'annotations de type AnnotationBO (soit de type StandardAnnoBO, soit CustomAnnoBO)</returns>
        public List<AnnotationBO> GetAnnotationsFromAnnotationFeatureClass(DatasetBO datasetParam, List<String> customFields)
        {
            var listAnnotationsBO = new List<AnnotationBO>();

            // Filter all the rows that don't have a toponym defined (unplaced on the map)
            QueryFilter queryFilter = new QueryFilter
            {
                WhereClause = "TEXTSTRING IS NOT NULL AND TEXTSTRING <> ''"
            };

            // déterminer si les annotations sont de type standard ou custom
            var isStandardAnno = true;
            if (!(customFields is null) && customFields.Count > 0)
                isStandardAnno = false;

            using (Geodatabase geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(datasetParam.GeodatabasePath))))
            using (FeatureClass annotationFeatureClass = geodatabase.OpenDataset<FeatureClass>(datasetParam.DatasetName))
            using (RowCursor rowCursor = annotationFeatureClass.Search(queryFilter, false))
            {
                while (rowCursor.MoveNext())
                {
                    using (var row = rowCursor.Current as AnnotationFeature)
                    {
                        try
                        {
                            if (isStandardAnno)
                            {
                                // s'il s'agît d'une Annotation de type standard, alors lire tous les champs
                                // typiques d'une Annotation
                                var aStandardAnnotationToExport = new StandardAnnoBO(row);

                                listAnnotationsBO.Add(aStandardAnnotationToExport);
                            }
                            else
                            {
                                // s'il s'agît d'une Annotation de type custom (personnalisée), alors ne lire 
                                // que les champs indiqués dans la liste customFields
                                var aFilteredAnnotationToExport = new CustomAnnoBO(row, customFields);

                                listAnnotationsBO.Add(aFilteredAnnotationToExport);
                            }
                        }
                        catch (GeodatabaseException ex)
                        {
                            throw new GeodatabaseException(string.Format("Error trying to retrieve the annotations from Annotation Feature " +
                                "Class : '{0}' --- Object ID : '{1}'", annotationFeatureClass.GetName(), row.GetObjectID()), ex);
                        }
                    }
                }
            }

            return listAnnotationsBO;
        }

        /// <summary>
        /// Méthode qui insère des annotations dans une Feature Class cible
        /// </summary>
        /// <param name="annotationsBO">Liste d'annotations à insérer</param>
        /// <param name="targetDataset">Feature Class cible portée par une instance de la classe DatasetBO</param>
        public void ExportAnnotationsIntoFeatureClass(IEnumerable<AnnotationBO> annotationsBO, DatasetBO targetDataset)
        {
            try
            {
                using (Geodatabase geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(targetDataset.GeodatabasePath))))
                using (FeatureClass targetFC = geodatabase.OpenDataset<FeatureClass>(targetDataset.DatasetName))
                {
                    geodatabase.ApplyEdits(() =>
                    {
                        // tout d'abord déterminer s'il s'agît d'annotations de type 
                        // StandardAnnoBO ou CustomAnnoBO
                        var isStandardAnno = true;
                        if (annotationsBO.FirstOrDefault() is CustomAnnoBO)
                            isStandardAnno = false;

                        // parcourir toutes les annotations et préparer leur insértion 
                        // dans la FC cible
                        foreach (var anAnnotation in annotationsBO)
                        {
                            CreatingARow(anAnnotation, targetFC, isStandardAnno);
                        }
                    });
                }
            }
            catch (GeodatabaseException ex)
            {
                throw new GeodatabaseException(string.Format("Un problème est survenu lors de l'export d'annotations dans la Feature Class {0}", targetDataset.DatasetName), ex);
            }
        }

        public void ModifyAnnotation(Geodatabase geodatabase, AnnotationFeatureClass annoLayer)
        {
            //
            // Méthode jetable utilisée uniquement comme un sandbox pour essayer de régler le problème 
            // de l'override sur les champs classiques de l'annotation parce qu'on passe par le
            // CIMSymbolReference, notamment le champ "CharacterSpacing".
            // 
            // TODO : Régler ce problème.
            //
            QueryFilter qf = new QueryFilter();
            qf.WhereClause = "OBJECTID < 100";

            using (RowCursor rowCursor = annoLayer.Search())
            {
                geodatabase.ApplyEdits(() =>
                {
                    if (rowCursor.MoveNext())
                    {
                        using (var annoFeature = rowCursor.Current as AnnotationFeature)
                        {
                            ////Get the graphic from the anno feature
                            //var graphic = annoFeature.GetGraphic();
                            //var textGraphic = graphic as CIMTextGraphic;

                            //annoFeature[21] = 695;

                            //// change the text and the color
                            //textGraphic.Text = "hello world";
                            //var symbol = textGraphic.Symbol.Symbol;
                            ///*symbol.SetColor(ColorFactory.Instance.RedRGB);
                            //textGraphic.Symbol = symbol.MakeSymbolReference();*/
                            //// update the graphic
                            //annoFeature.SetGraphic(textGraphic);
                            //// store is required
                            //annoFeature.Store();

                            CIMTextGraphic textGraphic = annoFeature.GetGraphic() as CIMTextGraphic;
                            CIMSymbolReference symbolRef = textGraphic.Symbol;
                            // symbolRef.SymbolName = "1";  //change the symbol being referred to by the CIMTextGraphic
                            CIMTextSymbol textSymbol = symbolRef.Symbol as CIMTextSymbol;

                            textSymbol.Height = 6; //change the height of the text.
                            textSymbol.LetterSpacing = 369;

                            annoFeature.SetGraphic(textGraphic);
                            annoFeature.Store();

                        }
                    }
                });
            }

            // #####################################
            // FOA 24/11/2021 : Attention : cet approche fonctionne uniquement avec les Add-ins (Desktop libraries)
            // #####################################

            /*     // load the inspector with the feature
                 var insp = new Inspector();
             insp.Load(annoLayer, oid);

             // get the annotation properties
             var annoProperties = insp.GetAnnotationProperties();
             // get the cimTextGraphic geometry
             Geometry textGraphicGeometry = annoProperties.Shape;

             // increase the font size
             annoProperties.FontSize = 48;

             // rotate the cimTextGraphic - but only if it's a polyline
             Polyline baseLine = textGraphicGeometry as Polyline;
             if (baseLine != null)
             {
                 // rotate the baseline 90 degrees
                 var origin = GeometryEngine.Instance.Centroid(baseLine);
                 Geometry rotatedBaseline = GeometryEngine.Instance.Rotate(baseLine, origin, System.Math.PI / 2);

                 // set the updated geometry back to the annotation properties
                 annoProperties.Shape = rotatedBaseline;
             }

             // assign the annotation properties back to the inspector
             insp.SetAnnotationProperties(annoProperties);

             // create the edit operation
             var op = new EditOperation
             {
                 Name = "Update annotation baseline",
                 SelectModifiedFeatures = true,
                 SelectNewFeatures = false
             };

             // call modify
             op.Modify(insp);*/
        }

        /// <summary>
        /// Méthode qui insère 1 annotation de type AnnotationBO dans une Feature Class
        /// </summary>
        /// <param name="anAnnotation">Instance d'une annotation de type AnnotationBO</param>
        /// <param name="targetFeatureClass">Feature Class cible</param>
        /// <param name="isStandardAnno">Booléen qui indique s'il s'agît d'une Annotation 
        /// de type StandardAnnoBO ou CustomAnnoBO</param>
        protected void CreatingARow(AnnotationBO anAnnotation, FeatureClass targetFeatureClass, bool isStandardAnno)
        {
            FeatureClassDefinition tableDefinition = targetFeatureClass.GetDefinition();

            // Create the Row Buffer in order to load the values
            using (RowBuffer rowBuffer = targetFeatureClass.CreateRowBuffer())
            {
                var standardAnno = (StandardAnnoBO)anAnnotation;

                if (!(targetFeatureClass is AnnotationFeatureClass))
                {
                    // Set the mandatory fields
                    // Note : OBJECTID field is not editable.
                    rowBuffer[tableDefinition.GetShapeField()] = anAnnotation.Shape;
                    rowBuffer[nameof(anAnnotation.TextString)] = anAnnotation.TextString;
                    rowBuffer[nameof(anAnnotation.Angle)] = anAnnotation.Angle;
                }
                else
                {
                    // Set "regular" fields (non-mandatory) of the standard annotation class
                    rowBuffer[nameof(standardAnno.FeatureID)] = standardAnno.FeatureID;
                    rowBuffer[nameof(standardAnno.ZOrder)] = standardAnno.ZOrder;
                    rowBuffer[nameof(standardAnno.AnnotationClassID)] = standardAnno.AnnotationClassID;
                    rowBuffer[nameof(standardAnno.Element)] = standardAnno.Element;
                    rowBuffer[nameof(standardAnno.SymbolID)] = standardAnno.SymbolID;

                    rowBuffer[nameof(standardAnno.Status)] = standardAnno.Status;
                    rowBuffer[nameof(standardAnno.FontName)] = standardAnno.FontName;
                    rowBuffer[nameof(standardAnno.FontStyle)] = standardAnno.FontStyle;
                    rowBuffer[nameof(standardAnno.FontSize)] = standardAnno.FontSize;
                    rowBuffer[nameof(standardAnno.Underline)] = standardAnno.Underline;
                    rowBuffer[nameof(standardAnno.VerticalAlignment)] = standardAnno.VerticalAlignment;
                    rowBuffer[nameof(standardAnno.HorizontalAlignment)] = standardAnno.HorizontalAlignment;
                    rowBuffer[nameof(standardAnno.XOffset)] = standardAnno.XOffset;
                    rowBuffer[nameof(standardAnno.YOffset)] = standardAnno.YOffset;

                    rowBuffer[nameof(standardAnno.FontLeading)] = standardAnno.FontLeading;
                    rowBuffer[nameof(standardAnno.WordSpacing)] = standardAnno.WordSpacing;
                    rowBuffer[nameof(standardAnno.CharacterWidth)] = standardAnno.CharacterWidth;
                    rowBuffer[nameof(standardAnno.CharacterSpacing)] = standardAnno.CharacterSpacing;
                    rowBuffer[nameof(standardAnno.FlipAngle)] = standardAnno.FlipAngle;
                }

                if (isStandardAnno == false)
                {
                    // Set the "custom" fields (added by the user)
                    var filteredAnno = (CustomAnnoBO)anAnnotation;

                    foreach (var field in filteredAnno.FilteredFields)
                    {
                        rowBuffer[field.Key] = field.Value;
                    }
                }

                // Create the feature
                Feature feature = targetFeatureClass.CreateRow(rowBuffer) as Feature;

                // Update some important fields (Mandatory fields) that could not be loaded
                // in the Row Buffer, we need set them through the toCIMTextGraphic (Shape
                // and Text) and the CIMTextSymbol (LetterSpacing and Angle)
                // NB: the OBJECTID field is not editable
                if (targetFeatureClass is AnnotationFeatureClass)
                {
                    AnnotationFeature annoFeat = feature as AnnotationFeature;

                    // get the CIMTextGraphic
                    var graphic = annoFeat.GetGraphic();
                    var cimTextGraphic = graphic as CIMTextGraphic;

                    // update the fields Shape and Text
                    cimTextGraphic.Shape = anAnnotation.Shape;
                    cimTextGraphic.Text = anAnnotation.TextString;

                    //get the CIMTextSymbol
                    var symbol = graphic.Symbol.Symbol;
                    CIMTextSymbol txtSymbol = symbol as CIMTextSymbol;

                    // update the Letter Spacing and the Angle
                    txtSymbol.LetterSpacing = (double)standardAnno.CharacterSpacing;
                    txtSymbol.Angle = (double)anAnnotation.Angle;

                    // These 2 steps are very important in order save the changes above made.
                    // Set the CIMTextGraphic on the graphic, push it back into the feature, and store.
                    annoFeat.SetGraphic(cimTextGraphic);
                    feature.Store();
                }
            }
        }

        /// <summary>
        /// Méthode qui calcule le point milieu d'un segment, notamment le segment qui sert de support aux annotations droites.
        /// Attention : La géométrie segment des annotations en entrée est remplacée par le point milieu
        /// </summary>
        /// <param name="straightAnnotations">Liste d'annotations de type AnnotationBO</param>
        /// <returns>La même liste d'annotations mais la géométrie (champ "Shape") est remplacée par le point milieu</returns>
        public IEnumerable<AnnotationBO> SetMiddlePointAsAnchorForStraightToponyms(IEnumerable<AnnotationBO> straightAnnotations)
        {
            foreach (var toponym in straightAnnotations)
            {
                var polyline = toponym.Shape as Polyline;
                toponym.Shape = GeomUtils.ComputeMiddlePoint(polyline.Points[0], polyline.Points[1]);
            }

            return straightAnnotations;
        }

        /// <summary>
        /// Méthode qui vérifie si toutes les features ont bien une géométrie Baseline
        /// </summary>
        /// <param name="datasetParam">Une Annotation Feature Class vérifiée et encapsulée dans ce paramètre</param>
        /// <returns>Retourne une liste avec les OBJECTID's qui n'ont pas de Baseline. Retourne une liste vide si toutes les features ont une Baseline</returns>
        public List<long> CheckIfAllFeaturesHaveBaseline(DatasetBO datasetParam)
        {
            // Filter all the rows that don't have a toponym defined (unplaced on the map)
            QueryFilter queryFilter = new QueryFilter
            {
                WhereClause = "TEXTSTRING IS NOT NULL AND TEXTSTRING <> ''"
            };

            var anno = new AnnotationBO();
            var objectidsMissingBaseline = new List<long>();

            using (Geodatabase geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(datasetParam.GeodatabasePath))))
            using (FeatureClass annotationFeatureClass = geodatabase.OpenDataset<FeatureClass>(datasetParam.DatasetName))
            using (RowCursor rowCursor = annotationFeatureClass.Search(queryFilter, false))
            {
                while (rowCursor.MoveNext())
                {
                    using (var row = rowCursor.Current as AnnotationFeature)
                    {
                        try
                        {
                            // on vérifie si cette annotation feature a une baseline ou pas
                            if (anno.IsBaselineNull(row))
                            {
                                objectidsMissingBaseline.Add(row.GetObjectID());
                            }
                        }
                        catch (GeodatabaseException ex)
                        {
                            throw new GeodatabaseException(string.Format("Error trying to retrieve the annotations from Annotation Feature " +
                                "Class : '{0}' --- Object ID : '{1}'", annotationFeatureClass.GetName(), row.GetObjectID()), ex);
                        }
                    }
                }
            }

            return objectidsMissingBaseline;
        }

        public double CalculatePointFromBezierCurve(double t, double xy1, double xy2, double xy3, double xy4)
        {
            //var t = 0.5; // given example value
            //var x = (1 - t) * (1 - t) * p[0].x + 2 * (1 - t) * t * p[1].x + t * t * p[2].x;
            //var y = (1 - t) * (1 - t) * p[0].y + 2 * (1 - t) * t * p[1].y + t * t * p[2].y;

            // P = (1−t)^3 * P1 + 3 * (1−t)^2 * t * P2 + 3 * (1−t) * t^2 * P3 + t^3 * P4
            return Math.Pow((1 - t), 3) * xy1 + 3 * Math.Pow((1 - t), 2) * t * xy2 + 3 * (1 - t) * Math.Pow(t, 2) * xy3 + Math.Pow(t, 3) * xy4;

        }

        /// <summary>
        /// Méthode qui éclate des annotations courbes en charactères simples, la méthode gère le
        /// découpage des mots et géométries en s'appuyant sur la méthode "SplitCurvedToponymIntoCharacters"
        /// </summary>
        /// <param name="curvedAnnotations">Liste d'annotations courbes à convertir en charactères simples</param>
        /// <returns>Nouvelle liste d'annotations courbes converties en charactères simples</returns>
        public List<AnnotationBO> CollapseCurvedToponymIntoSingleCharacters(IEnumerable<AnnotationBO> curvedAnnotations)
        {
            List<AnnotationBO> annotationsCourbesCollapsed = new List<AnnotationBO>();

            foreach (var item in curvedAnnotations)
            {
                //Console.WriteLine(item.ObjectID);
                //if (item.ObjectID != 76)
                //{
                //    continue;
                //}

                char[] delimiters = new char[] { '\r', '\n', ' ' };
                var textstringDecomposed = item.TextString.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

                if (item.Shape is GeometryBag shapeGeometryBag)
                {
                    // Multi geometries

                    var indexWordGeometry = 0;

                    if (textstringDecomposed.Count() != (item.Shape as GeometryBag).PartCount)
                    {
                        throw new Exception(string.Format("Le nombre de mots dans le toponyme : {0} ne " +
                            "correspond pas au nombre de géométries dans la baseline : " +
                            "{1} géométries.", item.TextString, (item.Shape as GeometryBag).PartCount));
                    }

                    var objects = JObject.Parse(item.Shape.ToJson()); // parse as array  
                    var jGeometries = objects.GetValue("geometries") as JArray;

                    foreach (JObject jPoints in jGeometries)
                    {
                        var aPart = jPoints.GetValue("points") as JArray;

                        // ****************************************************************************************
                        // STARTS - Patch : Annotations export compatibility with desktop after upgrade of Dataset
                        // possible BEZIER CURVES PROBLEM
                        // ****************************************************************************************
                        if (aPart == null)
                        {
                            // possible solution in case of desktop compatibility, not in case of Bezier curves
                            //aPart = jPoints.GetValue("paths") as JArray;
                            throw new Exception(string.Format("Il est possible qu'il s'agisse de courbes de Bézier, l'outil ne " +
                            "gère pas (pour l'instant) les courbes de Bézier. \nNom du toponyme : {0}", item.TextString));
                            // Info complémentaire: "\n###---###\nItem JSON = {1}", objects.ToString())); 
                        }
                        // **************
                        //  ENDS - Patch
                        // **************

                        List<Coordinate2D> newPartCoordinates = new List<Coordinate2D>();

                        foreach (JArray aPoint in aPart)
                        {
                            newPartCoordinates.Add(new Coordinate2D((double)aPoint[0], (double)aPoint[1]));
                        }

                        var newPolyline = PolylineBuilderEx.CreatePolyline(newPartCoordinates);

                        var annotationsWordIntoCharacters = SplitCurvedToponymIntoCharacters(item, textstringDecomposed[indexWordGeometry], newPolyline);

                        annotationsCourbesCollapsed.AddRange(annotationsWordIntoCharacters);

                        indexWordGeometry++;
                    }
                }
                else if (item.Shape is Multipoint shapeMultipoint)
                {
                    // Single geometry

                    var objects = JObject.Parse(shapeMultipoint.ToJson()); // parse as array  
                    var jPoints = objects.GetValue("points") as JArray;

                    // ****************************************************************************************
                    // STARTS - Patch : Annotations export compatibility with desktop after upgrade of Dataset
                    // ****************************************************************************************
                    //if (jPoints == null)
                    //{
                    //    //jPoints = objects.GetValue("paths") as JArray;
                    //}
                    // **************
                    //  ENDS - Patch
                    // **************

                    List<Coordinate2D> newPartCoordinates = new List<Coordinate2D>();

                    foreach (JArray aPoint in jPoints)
                    {
                        newPartCoordinates.Add(new Coordinate2D((double)aPoint[0], (double)aPoint[1]));
                    }

                    var newPolyline = PolylineBuilderEx.CreatePolyline(newPartCoordinates);

                    var annotationsWordIntoCharacters = SplitCurvedToponymIntoCharacters(item, textstringDecomposed[0], newPolyline);

                    annotationsCourbesCollapsed.AddRange(annotationsWordIntoCharacters);
                }
                else
                {
                    var polyline = item.Shape as Polyline;

                    if (polyline.HasCurves)
                    {
                        var objects = JObject.Parse(item.Shape.ToJson()); // parse as array
                        throw new Exception(string.Format("Il s'agit de courbes de Bézier, l'outil ne " +
                            "gère pas (pour l'instant) les courbes de Bézier. \nNom du toponyme : {0} \n###---###\nItem JSON = {1}", item.TextString, objects.ToString()));

                        /*
                         * 
                         * NOTA: Développement en cours, courbes de Bézier. 
                         *       Le code ci-dessous n'est pas exécuté
                         * 
                         */

                        /* #################################################
                         * Date 24/11/2022 - Avant départ Fabian
                         * 
                         * TODO: Une idée de solution pour contourner ce problème 
                         *       serait d'exporter la courbe de Bézier en forme 
                         *       d'entité linéaire et enregistrer les résultats 
                         *       en tant que Features (Feature Class) normaux et 
                         *       pas en tant que Annotations (Annotation Feature 
                         *       Class).
                         * 
                         */
                        /*
                        var jGeometries = objects.GetValue("curvePaths") as JArray;

                        var fromPoint = jGeometries[0][0] as JArray;

                        var b = ((jGeometries[0][1] as JObject).GetValue("b") as JArray);

                        var toPoint = b[0] as JArray;

                        var controlPoint1 = b[1] as JArray;
                        var controlPoint2 = b[2] as JArray;

                        List<Coordinate2D> newPartCoordinates = new List<Coordinate2D>();

                        var treatedString = item.TextString.Replace("\r\n", "");

                        var totalPointsX2Counter = 1;
                        var totalPointsX2 = treatedString.Count() * 2;

                        double incr = decimal.ToDouble(decimal.Divide(1, (totalPointsX2)));

                        for (double t = 0; totalPointsX2Counter <= totalPointsX2; t += incr)
                        {
                            var xCalculated = CalculatePointFromBezierCurve(t,
                                Double.Parse(fromPoint[0].ToString()),
                                Double.Parse(controlPoint1[0].ToString()),
                                Double.Parse(controlPoint2[0].ToString()),
                                Double.Parse(toPoint[0].ToString()));

                            var yCalculated = CalculatePointFromBezierCurve(t,
                                Double.Parse(fromPoint[1].ToString()),
                                Double.Parse(controlPoint1[1].ToString()),
                                Double.Parse(controlPoint2[1].ToString()),
                                Double.Parse(toPoint[1].ToString()));

                            totalPointsX2Counter += 1;

                            Console.WriteLine(xCalculated);
                            Console.WriteLine(yCalculated);

                            newPartCoordinates.Add(new Coordinate2D(xCalculated, yCalculated));
                        }

                        var newPolyline = PolylineBuilderEx.CreatePolyline(newPartCoordinates);

                        var annotationsWordIntoCharacters = SplitCurvedToponymIntoCharacters(item, treatedString, newPolyline);

                        annotationsCourbesCollapsed.AddRange(annotationsWordIntoCharacters);
                        */
                    }
                    else
                    {
                        // POLYLINE
                        // this case was not tested because curved toponyms are often Multipoint or GeometryBag
                        var annotationsWordIntoCharacters = SplitCurvedToponymIntoCharacters(item, textstringDecomposed[0], item.Shape as Polyline);

                        annotationsCourbesCollapsed.AddRange(annotationsWordIntoCharacters);
                    }
                }
            }

            return annotationsCourbesCollapsed;
        }

        /// <summary>
        /// Méthode qui éclate un mot (toponyme) et sa baseline (polyline) en charactères 
        /// simples en découpant la géométrie et crée de nouvelles instances de la classe 
        /// AnnotationBO à partir du template envoyé en paramètre et retourne une liste de
        /// objets de type AnnotationBO
        /// </summary>
        /// <param name="templateAnnotation">Template de l'annotation sur la quelle les nouvelles annotations de charactères simples vont se calquer</param>
        /// <param name="aWord">Un toponyme composé d'un seul mot ou sans espaces. Ex: Champs-sur-Marne</param>
        /// <param name="aPolyline">La baseline (polyline) du toponyme</param>
        /// <returns>Une liste d'annotations de type AnnotationBO découpées lettre-à-lettre</returns>
        protected List<AnnotationBO> SplitCurvedToponymIntoCharacters(AnnotationBO templateAnnotation, String aWord, Polyline aPolyline)
        {
            var annotationsCourbesCollapsedByCharacter = new List<AnnotationBO>();

            // start cropping into characters
            if (aWord.Count() != (aPolyline.PointCount / 2))
            {
                throw new Exception(string.Format("Le nombre de charactères dans le toponyme composé d'un seul " +
                    "mot ou sans espaces : {0} ne correspond pas au nombre de segments de la géométrie envoyée : " +
                    "{1} segments.", aWord, aPolyline.PointCount / 2));
            }

            var indexCharacter = 0;

            for (var i = 0; i < aPolyline.PointCount; i = i + 2)
            {
                var start = aPolyline.Points.ElementAt(i);
                var end = aPolyline.Points.ElementAt(i + 1);

                var anAnnotationBO = (AnnotationBO)templateAnnotation.Clone();

                anAnnotationBO.Shape = start;
                anAnnotationBO.Angle = GeomUtils.ComputeAngle(start, end);
                anAnnotationBO.TextString = aWord[indexCharacter].ToString();

                annotationsCourbesCollapsedByCharacter.Add(anAnnotationBO);

                indexCharacter++;
            }

            return annotationsCourbesCollapsedByCharacter;
        }

        /// <summary>
        /// Method that inserts an Annotation into a specified Annotation Feature Class 
        /// using the CIMTextGraphic and CIMSymbolReference objects
        /// 
        /// Use this method when you need to create an Annotation from scratch.
        /// 
        /// </summary>
        /// <param name="textString">Annotation text</param>
        /// <param name="geom">Annotation geometry</param>
        /// <param name="symbolID">Annotation's symbol reference in the Annotation Feature Class</param>
        /// <param name="annoFeatureClass">Annotation Feature Class where to insert the given annotation</param>
        /// <param name="customFieldsToInsert">Additional fields we would like to insert</param>
        /// <param name="letterSpacingParam">The space between the letters</param>
        public void InsertAnno(string textString, Geometry geom, int symbolID, AnnotationFeatureClass annoFeatureClass, Dictionary<string, object> customFieldsToInsert, double? letterSpacingParam)
        {
            //Create the row buffer
            RowBuffer rowBuffer = annoFeatureClass.CreateRowBuffer();
            Feature feature = annoFeatureClass.CreateRow(rowBuffer) as Feature;

            var annoFCDef = annoFeatureClass.GetDefinition();
            var symCol = annoFCDef.GetSymbolCollection();

            //get the text symbol from the symbol collection
            var symbolIdentifier = (from s in symCol where s.ID == symbolID select s).FirstOrDefault();
            var symbol = symbolIdentifier.Symbol;

            AnnotationFeature annoFeat = feature as AnnotationFeature;

            annoFeat.SetStatus(AnnotationStatus.Placed);
            annoFeat.SetAnnotationClassID(0);

            //Setup the text graphic
            CIMTextGraphic cimTextGraphic = new CIMTextGraphic
            {
                Text = textString,
                Shape = geom
            };
            CIMTextSymbol txtSymbol = symbol as CIMTextSymbol;

            if (letterSpacingParam > 0)
            {
                txtSymbol.LetterSpacing = (double)letterSpacingParam; //Set spacing on the CIMTextSymbol
            }

            //Setup the symbol reference with the symbol id and the text symbol
            var symbolRef = new CIMSymbolReference
            {
                SymbolName = symbolID.ToString(),
                Symbol = txtSymbol
            };

            // Setup all the custom fields, if there are
            foreach (var itemKey in customFieldsToInsert.Keys)
            {
                var indexField = annoFeat.FindField(itemKey);
                if (indexField != -1)
                {
                    annoFeat[indexField] = customFieldsToInsert[itemKey];
                }

            }

            //Set the symbol reference on the graphic, push it back into the feature, and store.
            cimTextGraphic.Symbol = symbolRef;
            annoFeat.SetGraphic(cimTextGraphic);
            feature.Store();
        }
    }
}
