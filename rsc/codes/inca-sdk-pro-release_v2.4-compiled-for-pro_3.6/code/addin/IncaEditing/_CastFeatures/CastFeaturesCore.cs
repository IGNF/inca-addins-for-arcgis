using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.Mapping;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using IGN.Inca.ArcGISPro.SDK.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Threading.Tasks;
using System.Windows;

namespace IncaEditing._CastFeatures
{
    /// <summary>
    /// Classe prenant en charge les opérations de transtypage d'entités.
    /// Le transtypage consiste à déplacer d'une classe à une autre des entités selon des règles
    /// définies par l'utilisateur.
    /// </summary>
    public sealed class CastFeaturesCore
    {
        #region Parameters

        /// <summary>
        /// Instance de la classe CastFeaturesCore
        /// /// </summary>
        private static CastFeaturesCore _instance;

        /// <summary>
        /// Traducteur de langue
        /// </summary>
        private ResourceSet Translator { get; set; }

        /// <summary>
        /// Nom du champ géométrique d'ArcGIS Pro (Shape)
        /// /// </summary>
        private static List<string> _geometricFields = null;

        /// <summary>
        /// Liste des règles de transtypage
        /// </summary>
        private static List<CastFeaturesRule> _castFeaturesRules = null;

        /// <summary>
        /// Recherche des classes d'entités cibles seulement au sein de la géodatabase par défaut du projet
        /// </summary>
        public bool SearchInDefaultGdb { get; set; }

        /// <summary>
        /// Autorise les transtypages au sein de la même géodatabase du projet
        /// </summary>
        public bool AllowIntrabase { get; set; }

        #endregion

        #region Constructors

        private CastFeaturesCore()
        {
            _geometricFields = new List<string>() { "shape_length", "shape_area" };

            // Dictionnaire de la langue utilisée dans l'IHM ("CurrentUICulture")
            Translator = CultureResourcesManager.GetInstance().dictLanguage;

            // Recherche des classes cibles limitée à la géodatabase par défaut
            SearchInDefaultGdb = true;

            // Autorise les transtypages au sein de la même géodatabase du projet
            AllowIntrabase = false;
        }

        #endregion

        /// <summary>
        /// Création ou appel à l'instance commune de la classe CastFeaturesCore
        /// </summary>
        /// <returns>L'instance de la classe</returns>
        public static CastFeaturesCore GetInstance()
        {
            if (_instance == null)
            {
                _instance = new CastFeaturesCore();
            }
            return _instance;
        }

        /// <summary>
        /// Retourne une chaîne de caractère incluse dans le dictionnaire multi-langues
        /// </summary>
        /// <returns>La chaîne de caractères dans la langue courante d'ArGIS Pro</returns>
        public string GetString(string name)
        {
            return Translator.GetString(name);
        }

        /// <summary>
        /// Retourne la classe d'entités associée à une couche d'entités ou d'annotations
        /// </summary>
        /// <param name="featureLayer">Couche d'entités ou d'annotations à tester</param>
        /// <returns>La classe d'entités</returns>
        public FeatureClass GetFeatureClass(BasicFeatureLayer featureLayer)
        {
            // Classe d'entités associée
            FeatureClass featureClass = null;
            if (featureLayer is FeatureLayer)
                featureClass = ((FeatureLayer)featureLayer).GetFeatureClass();
            else if (featureLayer is AnnotationLayer)
                featureClass = ((AnnotationLayer)featureLayer).GetFeatureClass();
            return featureClass;
        }

        /// <summary>
        /// Teste le chargement des règles de transtypage
        /// </summary>
        /// <returns>Le résultat du test</returns>
        public bool AreCastFeaturesRulesLoaded()
        {
            if (_castFeaturesRules == null)
            {
                return false;
            }
            else return true;
        }

        /// <summary>
        /// Exécution d'une opération d'édition, avec levée d'une exception en cas d'échec 
        /// </summary>
        /// <param name="editOperation">opération d'édition à exécuter</param>
        public static void ExecuteEditOperation(EditOperation editOperation)
        {
            try
            {
                bool editResult = editOperation.Execute();
                if (!editResult)
                {
                    throw new Exception(editOperation.ErrorMessage);
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        /// <summary>
        /// Chargement des règles de transtypage à partir d'un fichier excel
        /// </summary>
        /// <param name="excelConfigFileURI">Nom du fichier Excel décrivant les règles de transtypage</param>
        /// <returns>Le résultat du chargement</returns>
        public Task<bool> LoadCastFeaturesRules(string excelConfigFileURI)
        {
            return QueuedTask.Run(() =>
            {
                try
                {
                    // Lecture du fichier Excel
                    var rulesFromExcel = ExcelUtils.GetInstance().ReadExcelWorksheet(existingFileURI: excelConfigFileURI, worksheetName: "CastFeatures", headRowNumber: 1);

                    // Restructuration du contenu sous forme de règles de transtypage
                    List<CastFeaturesRule> castFeaturesRules = new List<CastFeaturesRule>();
                    foreach (Dictionary<string, string> line in rulesFromExcel)
                    {
                        CastFeaturesRule rule = new CastFeaturesRule();
                        rule.CreateRule(line);
                        castFeaturesRules.Add(rule);
                    }

                    _castFeaturesRules = castFeaturesRules;
                    return true;
                }
                catch (Exception ex)
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(ex.Message, "Warning", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            });
        }

        /// <summary>
        /// Transtypage ou suppression couche par couche des entités sélectionnées dans une carte 
        /// </summary>
        public async void CastSelectedFeatures()
        {
            // Une carte doit être active
            var mapView = MapView.Active;
            if (mapView == null) return;

            // Les règles de transtypage doivent être chargées
            if (_castFeaturesRules == null) return;

            await QueuedTask.Run(async () =>
            {
                try
                {
                    //Entités sélectionnées dans la carte
                    var selection = mapView.Map.GetSelection();

                    // Session d'édition unique pour toutes les couches hors annotations
                    EditOperation castOperation = new EditOperation { Name = "Cast features", SelectNewFeatures = true };

                    // Parcours des couches sélectionnées
                    foreach (KeyValuePair<MapMember, List<long>> kvp in selection.ToDictionary().Where(kvp => kvp.Key is BasicFeatureLayer))
                    {
                        // Nom de la classe d'entités associée
                        var srcFeatureLayer = kvp.Key as BasicFeatureLayer;
                        FeatureClass srcFeatureClass = GetFeatureClass(srcFeatureLayer);
                        string srcFeatureClassName = srcFeatureClass.GetName();

                        // Parcours des règles de transtypage
                        foreach (CastFeaturesRule rule in _castFeaturesRules.ToArray().Where(kvp => kvp.SrcFeatureClass == srcFeatureClassName))
                        {
                            bool deleteFeatures = (rule.TargetFeatureClass.ToLower() == "null" || rule.TargetFeatureClass.ToLower() == "none") ? true : false;

                            // Contrôle de cohérence
                            if (deleteFeatures is true && rule.DeleteFeatures is false)
                                throw new Exception("Field DELETE_FEATURES must be filled with TRUE if field TARGET_FEATURE_CLASS has a value of 'null' or 'none'");

                            if (deleteFeatures)
                                // Suppression des entités
                                await DeleteLayerAccordingToRule(castOperation, srcFeatureLayer, rule);
                            else
                                // Transtypage des entités de la couche
                                await CastLayerAccordingToRule(castOperation, srcFeatureLayer, rule);
                        }
                    }

                    // Sauvegarde des opérations
                    if (!castOperation.IsEmpty)
                    {
                        bool result = castOperation.Execute();

                        if (!result)
                        {
                            ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(castOperation.ErrorMessage, "Warning", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(ex.Message, "Warning", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
        }

        /// <summary>
        /// Suppression des entités sélectionnées au sein d'une couche donnée
        /// </summary>
        /// <param name="editOperation">Opération d'édition au sein de laquelle la suppression doit être réalisée</param>
        /// <param name="srcFeatureLayer">Couche d'entités contenant les entités à supprimer</param>
        /// <param name="rule">Règle de transtypage contenant un filtre sélectif à appliquer à la sélection</param>
        /// <returns>Le résultat de la suppression</returns>
        public Task<bool> DeleteLayerAccordingToRule(EditOperation editOperation, BasicFeatureLayer srcFeatureLayer, CastFeaturesRule rule)
        {
            return QueuedTask.Run<bool>(() =>
            {
                // Classe d'entités associée
                FeatureClass srcFeatureClass = GetFeatureClass(srcFeatureLayer);
                string srcFeatureClassName = srcFeatureClass.GetName();

                // La classe associée doit être transtypable
                var result = checkFeatureClassPath(srcFeatureClass, rule.SrcFeatureClass);
                if (!result.Result) return false;

                // Filtre sur les données en entrée
                QueryFilter queryFilter = GetQueryFilter(srcFeatureClass, rule.WhereClause);

                using (RowCursor rowCursor = srcFeatureLayer.GetSelection().Search(queryFilter))
                {
                    while (rowCursor.MoveNext())
                    {
                        using (Feature feature = (Feature)rowCursor.Current)
                        {
                            // Suppression des entités source
                            editOperation.Delete(srcFeatureLayer, feature.GetObjectID());
                        }
                    }
                }
                return true;
            });
        }

        /// <summary>
        /// Vérification qu'un chemin correspond à une classe d'entités donnée. Le chemin peut être réduit
        /// à un simple nom de classe d'entités ou élargi à un chemin complet.
        /// </summary>
        /// <param name="featureClassName">Nom de la classe d'entités</param>
        /// <param name="defaultGeodatabasePath">Géodatabase dans laquelle chercher en premier</param>
        /// <returns>La classe d'entités</returns>
        private Task<bool> checkFeatureClassPath(FeatureClass srcFeatureClass, string pathToCheck)
        {
            return QueuedTask.Run(() =>
            {
                // Décomposition du chemin
                string fileName = System.IO.Path.GetFileName(pathToCheck);
                string directoryName = System.IO.Path.GetDirectoryName(pathToCheck);

                // Vérification du nom de la classe
                if (fileName != srcFeatureClass.GetName())
                    return false;

                // Vérification éventuelle du réperoire
                if (!string.IsNullOrEmpty(directoryName))
                {
                    string srcFcDirectory = System.IO.Path.GetDirectoryName(srcFeatureClass.GetPath().AbsolutePath.ToString());
                    if (directoryName != srcFcDirectory)
                        return false;
                }
                return true;
            });
        }

        /// <summary>
        /// Vérification de la cohérence géométrique de deux classes d'entités: type géométrique, M et Z.
        /// Lance une exception en cas d'incohérence sur le type et le M. Pour le Z, retourne sous forme de
        /// texte le type d'incohérence: sameZ, enableZ ou disableZ
        /// A priori, les différences de projection ne posent pas de problème. Sinon, utiliser la méthode
        /// ReplaceSpatialReference de la classe GeometryBuilderEx.
        /// </summary>
        /// <param name="featureClass1">Nom de la première classe d'entités</param>
        /// <param name="featureClass2">Nom de la seconde classe d'entités</param>
        /// <returns>Le type de différence sur le Z</returns>
        public string checkFeatureClassesConsistency(FeatureClass featureClass1, FeatureClass featureClass2)
        {
            string zConsistency = string.Empty;

            QueuedTask.Run<string>(() =>
            {
                FeatureClassDefinition def1 = featureClass1.GetDefinition();
                FeatureClassDefinition def2 = featureClass2.GetDefinition();

                // Comparaison du type géométrique
                if (def1.GetShapeType() != def2.GetShapeType())
                    throw new Exception(string.Format("{0} and {1} classes have different geometry types", def1.GetName(), def2.GetName()));

                // Comparaison du M
                if (def1.HasM() != def2.HasM())
                    throw new Exception(string.Format("{0} and {1} classes have different M properties", def1.GetName(), def2.GetName()));

                // Comparaison du Z                    
                if (def1.HasZ() && !def2.HasZ())
                    zConsistency = "disableZ";
                else if (!def1.HasZ() && def2.HasZ())
                    zConsistency = "enableZ";
                else
                    zConsistency = "sameZ";

                return zConsistency;
            });
            return zConsistency;
        }

        /// <summary>
        /// Transtypage des entités sélectionnées au sein d'une couche donnée
        /// </summary>
        /// <param name="editOperation">Opération d'édition au sein de laquelle le transtypage doit être réalisé</param>
        /// <param name="srcFeatureLayer">Couche d'entités contenant les entités à transtyper</param>
        /// <param name="rule">Règle de transtypage</param>
        /// <returns>Le résultat du transtypage</returns>
        public Task<bool> CastLayerAccordingToRule(EditOperation editOperation, BasicFeatureLayer srcFeatureLayer, CastFeaturesRule rule)
        {
            return QueuedTask.Run<bool>(async () =>
            {
                // Classe d'entités associée
                FeatureClass srcFeatureClass = GetFeatureClass(srcFeatureLayer);

                // La classe associée doit être transtypable
                var result = checkFeatureClassPath(srcFeatureClass, rule.SrcFeatureClass);
                if (!result.Result) return false;

                // Exclusion éventuelle de la géodatabase source comme géodatabase cible
                string excludedGeodatabasePath = null;
                if (AllowIntrabase == false)
                {
                    // Chemin de la géodatabase source
                    Geodatabase srcGdb = srcFeatureClass.GetDatastore() as Geodatabase;
                    excludedGeodatabasePath = srcGdb.GetPath().LocalPath.ToString();
                }

                // Classe de destination
                var targetFeatureClass = await FindFeatureClass(rule.TargetFeatureClass, SearchInDefaultGdb, null, excludedGeodatabasePath);

                // On sort sans message si le résultat est nul => pas de transtypage à réaliser
                if (targetFeatureClass == null)
                    return true;

                // Contrôle de cohérence géométrique entre les deux classes
                string zConversion = checkFeatureClassesConsistency(srcFeatureClass, targetFeatureClass);
                // Nom du champ portant la géométrie
                string shapeFieldName = targetFeatureClass.GetDefinition().GetShapeField();

                // Filtre sur les données en entrée
                QueryFilter queryFilter = GetQueryFilter(srcFeatureClass, rule.WhereClause);

                // Correspondance des champs à transférer
                var fieldMapping = GetFieldMapping(srcFeatureClass, targetFeatureClass, rule.MapFieldsDict);

                // Vérification des valeurs de champs à affecter 
                CheckSetFieldValues(targetFeatureClass, rule.SetFieldValuesDict);

                using (RowCursor rowCursor = srcFeatureLayer.GetSelection().Search(queryFilter))
                {
                    while (rowCursor.MoveNext())
                    {
                        using (Feature feature = (Feature)rowCursor.Current)
                        {
                            // Valeurs de champ à copier
                            var fieldValues = new Dictionary<string, object>();
                            if (fieldMapping != null)
                            {
                                foreach (KeyValuePair<string, string> kvp in fieldMapping)
                                {
                                    var fieldIndex = feature.FindField(kvp.Key);
                                    if (fieldIndex != -1)
                                    {
                                        // Valeur extraite de la classe source
                                        var value = feature.GetOriginalValue(fieldIndex);
                                        // Conversion géométrique éventuelle
                                        if (kvp.Value == shapeFieldName)
                                        {
                                            switch (zConversion)
                                            {
                                                // Conversion 3D => 2D
                                                case "disableZ":
                                                    var WKBbuffer2D = GeometryEngine.Instance.ExportToWKB(WkbExportFlags.WkbExportStripZs, (Geometry)value);
                                                    value = GeometryEngine.Instance.ImportFromWKB(WkbImportFlags.WkbImportDefaults, WKBbuffer2D, ((Geometry)value).SpatialReference);
                                                    break;
                                                // Conversion 2D => 3D
                                                case "enableZ":
                                                    value = ConvertTo3DGeometry((Geometry)value);
                                                    break;
                                            }
                                        }
                                        fieldValues.Add(kvp.Value, value);
                                    }
                                }
                            }

                            // Valeurs de champ à affecter par défaut
                            if (rule.SetFieldValuesDict != null)
                            {
                                foreach (KeyValuePair<string, string> kvp in rule.SetFieldValuesDict)
                                {
                                    fieldValues.Remove(kvp.Key);
                                    fieldValues.Add(kvp.Key, kvp.Value);
                                }
                            }

                            //Copie de l'entité géométrique vers la cible
                            if (srcFeatureLayer is FeatureLayer)
                                editOperation.Create(targetFeatureClass, fieldValues);

                            // ou copie de l'annotation
                            else if (srcFeatureLayer is AnnotationLayer)
                            {
                                CIMTextGraphic textGraphic = ((AnnotationFeature)feature).GetGraphic() as CIMTextGraphic;
                                CreateAnnotation(targetFeatureClass, fieldValues, textGraphic, rule.SetFieldValuesDict);
                            }

                            // Suppression de l'entité ou de l'annotation source
                            if (rule.DeleteFeatures is true)
                                editOperation.Delete(srcFeatureClass, feature.GetObjectID());
                        }
                    }
                }
                if (srcFeatureLayer is AnnotationLayer)
                    MapView.Active.Redraw(false);
                return true;
            });
        }

        private void CreateAnnotation(FeatureClass featureClass, Dictionary<string, object> fieldValues, CIMTextGraphic textGraphic, Dictionary<string, string> setFieldValuesDict=null)
        /// <summary>
        /// Création d'une annotation à partir d'un dictionnaire de valeurs de champs et d'une description graphique d'annotation. 
        /// </summary>
        /// <param name="featureClass">classe d'annotations dans laquelle enregistrer le résultat</param>
        /// <param name="fieldValues">dictionnaire des valeurs de champ</param>
        /// <param name="textGraphic">description graphique du texte de l'annotation</param>
        /// <param name="setFieldValuesDict">dictionnaire des valeurs de champ modifiées par l'utilisateur</param>
        {

            AnnotationFeatureClass annotationFeatureClass = featureClass as AnnotationFeatureClass;

            // Vérification de la présence du text symbol dans la collection de symboles de la classe
            int symbolID = (int)fieldValues["symbolid"];
            var annoFCDef = annotationFeatureClass.GetDefinition();
            var symCol = annoFCDef.GetSymbolCollection();
            var symbolIdentifier = (from s in symCol where s.ID == symbolID select s).FirstOrDefault();

            if (symbolIdentifier is null)
                throw new Exception("The subtype code does not exist");

            // Sélection des valeurs d'attribut standard
            Dictionary<string, object> values = new Dictionary<string, object>();
            var CIMFields = new List<string> { "override", "symbolid" };
            foreach (KeyValuePair<string, object> kvp in fieldValues)
            {
                if (!CIMFields.Contains(kvp.Key))
                    values[kvp.Key] = kvp.Value;
            }

            // Nouvelle session d'édition spécifique pour les annotations
            // Remarque: on passe obligatoirement par une session d'édition (et nom un rowbuffer sur la classe) pour permettre
            // à l'utilisateur d'annuler l'opération via un Ctrl + z (undo stack).
            EditOperation editAnnoOperation = new EditOperation { Name = "Cast annotations", SelectNewFeatures = true };

            // Création de l'annotation brute avec transfert des valeurs d'attributs (notamment les attributs spécifiques)
            // A ce stade, l'annotation n'est pas exploitable
            RowToken row = editAnnoOperation.Create(featureClass, values);

            // Exécution de l'opération
            bool opResult = editAnnoOperation.Execute();

            // Mise à jour du texte symbole de l'annotation si sa création a réussi
            // On est obligé de procéder en deux étapes car la méthode Create() ne permet pas d'enregistrer une annotation complète
            if (opResult)
            {
                // Utilisation d'une opération de chaînage pour personnaliser le texte symbole.
                // Les deux opérations seront ainsi regroupées en une seule dans la pile de l'historique.
                var chainedOperation = editAnnoOperation.CreateChainedOperation();

                chainedOperation.Callback(context =>
                {
                    // Recherche de l'annotation via son identifiant
                    QueryFilter qf = new QueryFilter();
                    qf.WhereClause = "OBJECTID = " + row.ObjectID.ToString();

                    using (var rowCursor = featureClass.Search(qf, false))
                    {
                        rowCursor.MoveNext();
                        if (rowCursor.Current != null)
                        {
                            AnnotationFeature annoFeature = rowCursor.Current as AnnotationFeature;

                            // Chargement d'autres champs ne pouvant pas être enregistrés via l'opération d'édition et 
                            // nécessitant d'accéder au CIMTextGraphic et CIMTextSymbol de l'objet
                            if (annoFeature != null & textGraphic != null)
                            {
                                // Valeurs personnalisées par l'utilisateur
                                if (setFieldValuesDict != null)
                                {
                                    // Extraction du texte symbol
                                    CIMTextSymbol symbol = textGraphic.Symbol.Symbol as CIMTextSymbol;

                                    // Classe d'annotation
                                    if (setFieldValuesDict.ContainsKey("annotationclassid"))
                                    {
                                        int value = int.Parse(setFieldValuesDict["annotationclassid"]);
                                        annoFeature.SetAnnotationClassID(value);
                                    }

                                    // Statut
                                    if (setFieldValuesDict.ContainsKey("status"))
                                    {
                                        AnnotationStatus value = (AnnotationStatus)int.Parse(setFieldValuesDict["status"]);
                                        annoFeature.SetStatus(value);
                                    }

                                    // Texte
                                    if (setFieldValuesDict.ContainsKey("textstring"))
                                    {
                                        string value = setFieldValuesDict["textstring"];
                                        textGraphic.Text = value;
                                    }

                                    // Famille de police
                                    if (setFieldValuesDict.ContainsKey("fontname"))
                                    {
                                        string value = setFieldValuesDict["fontname"];
                                        symbol.FontFamilyName = value;
                                    }

                                    // Style de police
                                    if (setFieldValuesDict.ContainsKey("fontstyle"))
                                    {
                                        string value = setFieldValuesDict["fontstyle"];
                                        symbol.FontStyleName = value;
                                    }

                                    // Taille de police
                                    if (setFieldValuesDict.ContainsKey("fontsize"))
                                    {
                                        double value = double.Parse(setFieldValuesDict["fontsize"]);
                                        symbol.SetSize(value);
                                    }

                                    // Souligné
                                    if (setFieldValuesDict.ContainsKey("underline"))
                                    {
                                        bool value = setFieldValuesDict["underline"] == "1";
                                        symbol.Underline = value;
                                    }

                                    // Alignement vertical
                                    if (setFieldValuesDict.ContainsKey("verticalalignment"))
                                    {
                                        ArcGIS.Core.CIM.VerticalAlignment value = (ArcGIS.Core.CIM.VerticalAlignment)int.Parse(setFieldValuesDict["verticalalignment"]);
                                        symbol.VerticalAlignment = value;
                                    }

                                    // Alignement horizontal
                                    if (setFieldValuesDict.ContainsKey("horizontalalignment"))
                                    {
                                        ArcGIS.Core.CIM.HorizontalAlignment value = (ArcGIS.Core.CIM.HorizontalAlignment)int.Parse(setFieldValuesDict["horizontalalignment"]);
                                        symbol.HorizontalAlignment = value;
                                    }

                                    // Offset X
                                    if (setFieldValuesDict.ContainsKey("xoffset"))
                                    {
                                        double value = double.Parse(setFieldValuesDict["xoffset"]);
                                        symbol.OffsetX = value;
                                    }

                                    // Offset Y
                                    if (setFieldValuesDict.ContainsKey("yoffset"))
                                    {
                                        double value = double.Parse(setFieldValuesDict["yoffset"]);
                                        symbol.OffsetY = value;
                                    }

                                    // Angle de rotation
                                    // Attention: semble inopérant sur la version 3.6 d'ArcGIS Pro!
                                    if (setFieldValuesDict.ContainsKey("angle"))
                                    {
                                        double value = double.Parse(setFieldValuesDict["angle"]);
                                        symbol.Angle = value;
                                    }

                                    // Interligne
                                    if (setFieldValuesDict.ContainsKey("fontleading"))
                                    {
                                        double value = double.Parse(setFieldValuesDict["fontleading"]);
                                        symbol.LineGap = value;
                                    }

                                    // Intermot
                                    if (setFieldValuesDict.ContainsKey("wordspacing"))
                                    {
                                        double value = double.Parse(setFieldValuesDict["wordspacing"]);
                                        symbol.WordSpacing = value;
                                    }

                                    // Largeur de caractère
                                    if (setFieldValuesDict.ContainsKey("characterwidth"))
                                    {
                                        double value = double.Parse(setFieldValuesDict["characterwidth"]);
                                        symbol.LetterWidth = value;
                                    }

                                    // Interlettre
                                    if (setFieldValuesDict.ContainsKey("characterspacing"))
                                    {
                                        double value = double.Parse(setFieldValuesDict["characterspacing"]);
                                        symbol.LetterSpacing = value;
                                    }

                                    // Angle de basculement
                                    if (setFieldValuesDict.ContainsKey("flipangle"))
                                    {
                                        double value = double.Parse(setFieldValuesDict["flipangle"]);
                                        symbol.FlipAngle = value;
                                    }
                                }

                                // Affectatin du texte symbole (quoi qu'il arrive)
                                annoFeature.SetGraphic(textGraphic);

                                // Enregistrement des modifications et rafraîchissement du cache
                                annoFeature.Store();
                                context.Invalidate(annoFeature);
                            }
                        }
                    }
                }, featureClass);

                // Exécution de l'opération de chaînage
                chainedOperation.Execute();
            }
        }

        /// <summary>
        /// Conversion d'une géométrie 2D en une géométrie 3D (ajout d'un Z). 
        /// </summary>
        /// <param name="geometry">géométrie 2D à convertir</param>
        /// <returns>géométrie 3D avec un Z</returns>
        private Geometry ConvertTo3DGeometry(Geometry geometry)
        {
            Geometry newGeometry = null;
            // Reconstruction d'une géométrie avec Z selon le type géométrique
            switch (geometry.GeometryType)
            {
                case GeometryType.Point:
                    MapPointBuilderEx mapPointBuilder = new MapPointBuilderEx((MapPoint)geometry);
                    mapPointBuilder.HasZ = true;
                    newGeometry = mapPointBuilder.ToGeometry();
                    break;
                case GeometryType.Multipoint:
                    MultipointBuilderEx MultipointBuilder = new MultipointBuilderEx((Multipoint)geometry);
                    MultipointBuilder.HasZ = true;
                    newGeometry = MultipointBuilder.ToGeometry();
                    break;
                case GeometryType.Polyline:
                    PolylineBuilderEx polylineBuilder = new PolylineBuilderEx((Polyline)geometry);
                    polylineBuilder.HasZ = true;
                    newGeometry = polylineBuilder.ToGeometry();
                    break;
                case GeometryType.Polygon:
                    PolygonBuilderEx polygonBuilder = new PolygonBuilderEx((Polygon)geometry);
                    polygonBuilder.HasZ = true;
                    newGeometry = polygonBuilder.ToGeometry();
                    break;
                case GeometryType.Envelope:
                    EnvelopeBuilderEx envelopeBuilder = new EnvelopeBuilderEx((Envelope)geometry);
                    envelopeBuilder.HasZ = true;
                    newGeometry = envelopeBuilder.ToGeometry();
                    break;
                /*case GeometryType.GeometryBag:
                    GeometryBagBuilderEx geometryBagBuilder = new GeometryBagBuilderEx((GeometryBag)geometry);
                    geometryBagBuilder.HasZ = true;
                    newGeometry = geometryBagBuilder.ToGeometry();
                    break;*/
                default:
                    return geometry;
            }
            return newGeometry;
        }

        /// <summary>
        /// Recherche d'une classe d'entités à partir de son nom dans les géodatabases chargées dans le projet courant.
        /// Le chemin d'une géodatabase par défaut peut être fourni pour accélérer la recherche.
        /// Une exception est levée si la classe n'est trouvée dans aucune géodatabase, y compris celle qui serait exclue
        /// </summary>
        /// <param name="featureClassName">Nom de la classe d'entités</param>
        /// <param name="searchInDefaultGeodatabase">Recherche des classes cibles limitée à la géodatabase par défaut</param> 
        /// <param name="priorityGeodatabasePath">Géodatabase dans laquelle chercher en premier</param>
        /// <param name="excludedGeodatabasePath">Géodatabase exclue de la recherche</param>
        /// <returns>La classe d'entités</returns>
        private Task<FeatureClass> FindFeatureClass(string featureClassName, bool searchInDefaultGeodatabase, string priorityGeodatabasePath=null, string excludedGeodatabasePath=null)
        {
            // La géodatabase par défaut devient prioritaire si searchInDefaultGeodatabase est vrai
            if (searchInDefaultGeodatabase == true)
                priorityGeodatabasePath = Project.Current.DefaultGeodatabasePath.ToString();

            // Décomposition du chemin
            string fileName = System.IO.Path.GetFileName(featureClassName);
            string directoryName = System.IO.Path.GetDirectoryName(featureClassName);
            // Suppression éventuelle du nom du dataset
            string gdbExtension = ".gdb";
            int index = directoryName.IndexOf(gdbExtension);
            if (index != -1)
                directoryName = directoryName.Substring(0, index + gdbExtension.Length);

            return QueuedTask.Run(() =>
            {
                FeatureClass targetFeatureClass = null;

                // Si le nom est un chemin complet
                if (!string.IsNullOrEmpty(directoryName))
                {
                    try
                    {
                        // Chargement de la classe à partir de la gdb
                        Geodatabase geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(directoryName, UriKind.Absolute)));
                        targetFeatureClass = geodatabase.OpenDataset<FeatureClass>(fileName);
                        return targetFeatureClass as FeatureClass;
                    }
                    catch
                    {
                        throw new Exception(string.Format(GetString("NoClassMessage"), featureClassName));
                    }
                }

                // Listes des Géodatabases chargées dans le catalogue
                IEnumerable <GDBProjectItem> gdbProjectItems = Project.Current.GetItems<GDBProjectItem>();
                // On réordonne la liste si nécessaire
                if (priorityGeodatabasePath != null)
                    gdbProjectItems = gdbProjectItems.OrderBy(str => str.Path != priorityGeodatabasePath);

                // Parcours des databases à tester
                bool classExists = false;
                foreach (GDBProjectItem gdbProjectItem in gdbProjectItems)
                {
                    // On passe les databases invalides (lien vers la source rompue)
                    if (gdbProjectItem.IsInvalid is true)
                        continue;

                    using (Datastore datastore = gdbProjectItem.GetDatastore())
                    {
                        //On passe les dépôts non supportés (autres que GDB fichier et GDB d'entreprise)
                        if (datastore is not Geodatabase)
                            continue;
                        Geodatabase geodatabase = datastore as Geodatabase;

                        var gdbPath = geodatabase.GetPath().LocalPath.ToString();

                        try
                        {
                            // Chargement de la classe à partir de la gdb
                            targetFeatureClass = geodatabase.OpenDataset<FeatureClass>(featureClassName);

                            // Si la géodatabase est exclue ou qu'on a demandé de se limiter à la géodatabase par défaut, on se contente de noter que la classe existe
                            if (gdbProjectItem.Path == excludedGeodatabasePath || (searchInDefaultGeodatabase == true && gdbProjectItem.Path != priorityGeodatabasePath))
                                classExists = true;
                            // Sinon retour de la classe qui est valide!
                            else
                                return targetFeatureClass as FeatureClass;
                        }
                        catch { }
                    }
                }

                // Levée d'une exception si la classe n'a pas été trouvée
                if (!classExists)
                    throw new Exception(string.Format(GetString("NoClassMessage"), featureClassName));
                return null;
            });            
        }

        /// <summary>
        /// Instanciaton et validation d'un filtre de requête à partir d'une clause SQL
        /// </summary>
        /// <param name="featureClass">Nom de la classe d'entités sur laquelle doit s'appliquer le filtre</param>
        /// <param name="whereClause">Clause SQL</param>
        /// <returns>Le filtre de requête</returns>
        public QueryFilter GetQueryFilter(FeatureClass featureClass, string whereClause)
        {
            try
            {
                // Création d'un filtre
                QueryFilter queryFilter = new QueryFilter { WhereClause = whereClause };
                // Validation de la requête SQL par rapport à la classe d'entités
                var validate = featureClass.Validate(queryFilter);
                return queryFilter;
            }
            catch
            {
                throw new Exception(string.Format(GetString("InvalidSQLClauseMessage"), whereClause));
            }
        }

        /// <summary>
        /// Détermination des correspondances entre les champs en entrée et les champs en sortie du traitement de transtypage.
        /// Par défaut, tous les champs communs aux classes sources et cibles sont recopiés. On ajoute éventuellement
        /// des correspondances supplémentaires pour transférer des champs qui ne porteraient pas le même nom.
        /// </summary>
        /// <param name="srcFeatureClass">Nom de la classe d'entités source</param>
        /// <param name="targetFeatureClass">Nom de la classe d'entités cible</param>
        /// <param name="customFieldMapping">Correspondances particulières définies par l'utilisateur</param>
        /// <returns>Le dictionnaire de correspondances entre champs</returns>
        public Dictionary<string, string> GetFieldMapping(FeatureClass srcFeatureClass, FeatureClass targetFeatureClass, Dictionary<string, string> customFieldMapping)
        {
            // liste des champs communs aux deux classes (hors oid et géométrie)
            var srcFieldList = srcFeatureClass.GetDefinition().GetFields().Where(x => x.FieldType.ToString() != "OID" && x.FieldType.ToString() != "Geometry" && !_geometricFields.Contains(x.Name.ToLower()));
            var targetFieldList = targetFeatureClass.GetDefinition().GetFields().Where(x => x.FieldType.ToString() != "OID" && x.FieldType.ToString() != "Geometry" && !_geometricFields.Contains(x.Name.ToLower()));
            var commonFieldList = targetFieldList.Select(x => x.Name.ToLower()).Intersect((srcFieldList).Select(x => x.Name.ToLower()));

            // Correspondance des champs communs
            Dictionary<string, string> completeFieldMappingDict = commonFieldList.ToDictionary(x => x, x => x);

            // Ajout des champs géométriques
            completeFieldMappingDict.Add(srcFeatureClass.GetDefinition().GetShapeField(), targetFeatureClass.GetDefinition().GetShapeField());

            if (customFieldMapping != null)
            {
                // Vérification des correspondances personnalisées
                foreach (var srcField in customFieldMapping.Keys)
                {
                    if (!srcFieldList.Select(x => x.Name.ToLower()).Contains(srcField.ToLower()))
                        throw new Exception(string.Format(GetString("FieldNotInColumnMessage"), srcField, "JSON_MAP_FIELDS", srcFeatureClass.GetName()));
                }
                foreach (var targetField in customFieldMapping.Values)
                {
                    if (!targetFieldList.Select(x => x.Name.ToLower()).Contains(targetField.ToLower()))
                        throw new Exception(string.Format(GetString("FieldNotInColumnMessage"), targetField, "JSON_MAP_FIELDS", targetFeatureClass.GetName()));
                }

                // Ajout des correspondances personnalisées
                completeFieldMappingDict = completeFieldMappingDict.Union(customFieldMapping).GroupBy(g => g.Key.ToLower()).ToDictionary(pair => pair.Key, pair => pair.Last().Value);
            }

            return completeFieldMappingDict;
        }

        /// <summary>
        /// Vérification du dictionnaire d'affectations de valeurs définies par l'utilisateur
        /// </summary>
        /// <param name="featureClass">Nom de la classe d'entités source</param>
        /// <param name="setFieldValues">Dictionnaire indiquant la valeur à affecter à certains champs</param>
        public void CheckSetFieldValues(FeatureClass featureClass, Dictionary<string, string> setFieldValues)
        {
            if (setFieldValues == null) return;

            // Liste des champs de la classe d'entité
            var fieldList = featureClass.GetDefinition().GetFields().Where(x => x.FieldType.ToString() != "OID" && !_geometricFields.Contains(x.Name));

            // Vérification de la liste des champs
            foreach (var field in setFieldValues.Keys)
            {
                if (!fieldList.Select(x => x.Name).Contains(field))
                    throw new Exception(string.Format(GetString("FieldNotInColumnMessage"), field, "JSON_SET_FIELD_VALUES", featureClass.GetName()));
            }
        }
    }
}
