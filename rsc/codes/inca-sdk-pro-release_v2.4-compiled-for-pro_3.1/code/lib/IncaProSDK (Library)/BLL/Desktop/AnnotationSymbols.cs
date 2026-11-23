using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using ArcGIS.Core.Data.Mapping;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

/// <summary>
/// Namespace qui contient l'ensemble d'objets définissant la logique et comportement 
/// du projet INCA - ArcGIS Pro SDK orientés vers une application de type Add-In, c'est-à-dire
/// qu'on peut utiliser toutes les librairies d'ArcGIS Pro SDK
/// </summary>
namespace IGN.Inca.ArcGISPro.SDK.BLL.Desktop
{
    /// <summary>
    /// Classe prenant en charge les opérations de mise à jour des symboles de texte d'une classe
    /// d'annotations
    /// </summary>
    public sealed class AnnotationSymbols
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Instance de la classe AnnotationSymbols
        /// /// </summary>
        private static AnnotationSymbols _instance;

        private AnnotationSymbols() { }

        /// <summary>
        /// Création ou appel à l'instance commune de la classe AnnotationSymbols
        /// </summary>
        /// <returns>L'instance de la classe</returns>
        public static AnnotationSymbols GetInstance()
        {
            if (_instance == null)
            {
                _instance = new AnnotationSymbols();
            }
            return _instance;
        }

        /// <summary>
        /// Mise à jour des symboles de texte d'une classe d'annotations à partir des symboles de texte
        /// inclus dans un fichier de style .stylx.
        /// L'opération se déroule en deux temps:
        ///    1. Substitutions des symboles de texte inclus dans la collection de symboles de la classe d'annotations
        ///    2. Mise à niveau des champs descriptifs des annotations pour qu'ils soient cohérents avec le CIMTextGraphic
        /// Le projet .aprx doit être fermé puis réouvert à la fin de l'opération afin de valider les changements
        /// </summary>
        /// <param name="annotationClassPath">Chemin absolu de la classe d'annotations à modifier</param>
        /// <param name="style">Element de style d'un projet</param>
        /// <returns></returns>
        public Task<bool> ReplaceAnnotationSymbols(string annotationClassPath, StyleProjectItem style)
        {
            return QueuedTask.Run<bool>(() =>
            {
                var result = false;

                // Vérification du style
                if (style == null)
                {
                    log.Fatal("The style file is null");
                    return result;
                }

                try
                {
                    // Extraction du nom de la classe d'annotations
                    string featureClassName = System.IO.Path.GetFileName(annotationClassPath);

                    Geodatabase geodatabase;

                    // Ouverture d'une géodatabase fichier (.gdb)
                    if (annotationClassPath.Contains(".gdb", StringComparison.OrdinalIgnoreCase))
                    {
                        string gdbPath = Regex.Match(annotationClassPath, @".*\.gdb", RegexOptions.IgnoreCase).Value;
                        var fGdbPath = new FileGeodatabaseConnectionPath(new Uri(gdbPath));
                        geodatabase = new Geodatabase(fGdbPath);
                    }
                    // Ouverture d'une géodatabase d'entreprise (.sde)
                    else if (annotationClassPath.Contains(".sde", StringComparison.OrdinalIgnoreCase))
                    {
                        string gdbPath = Regex.Match(annotationClassPath, @".*\.sde", RegexOptions.IgnoreCase).Value;
                        var fGdbPath = new DatabaseConnectionFile(new Uri(gdbPath));
                        geodatabase = new Geodatabase(fGdbPath);
                        
                        // Extraction du nom simple de la classe (débarrassé du préfixe)
                        int pos = featureClassName.LastIndexOf('.');
                        featureClassName = pos >= 0 ? featureClassName[(pos + 1)..] : featureClassName;
                    }
                    // Autre type de base non géré
                    else
                    {
                        log.Fatal("The annotation class must belongs to a geodatabase with a .gdb or .sde extension");
                        return result;
                    }

                    // Chargement de la géodatabase et de la classe d'annotations
                    using (geodatabase)
                    {
                        using (FeatureClass featureClass = geodatabase.OpenDataset<FeatureClass>(featureClassName))
                        {
                            if (featureClass is not AnnotationFeatureClass)
                            {
                                log.Fatal(string.Format("{0} n'est pas une classe d'annotations", featureClassName));
                                return result;
                            }

                            // Classe d'annotations
                            AnnotationFeatureClass annotationFeatureClass = featureClass as AnnotationFeatureClass;

                            // Modification de la collection de symboles de texte de la classe
                            result = UpdateSymbolCollection(annotationFeatureClass, style).Result;

                            // Mise à jour des champs descriptifs des annotations
                            if (result)
                                result = RefreshAnnotationFields(annotationFeatureClass).Result;
                        }
                    }
                }

                catch (Exception ex)
                {
                   log.Fatal(ex.Message, ex);
                }

                return result;
            });
        }

        /// <summary>
        /// Mise à jour de la collection de symboles de texte d'une classe d'annotations à partir des
        /// symboles de texte inclus dans un fichier de style .stylex.
        /// Les symboles de texte sont associés si leurs noms sont identiques
        /// </summary>
        /// <param name="annotationFeatureClass">Classe d'annotations à modifier</param>
        /// <param name="style">Element de style d'un projet</param>
        /// <returns></returns>
        public Task<bool> UpdateSymbolCollection(AnnotationFeatureClass annotationFeatureClass, StyleProjectItem style)
        {
            return QueuedTask.Run<bool>(() =>
            {
                var result = false;

                // Propriétés de la classe d'annotations
                AnnotationFeatureClassDefinition fcDefinition = annotationFeatureClass.GetDefinition();
                string annoFeatureClassName = annotationFeatureClass.GetName();
                int pos = annoFeatureClassName.LastIndexOf('.');
                string simpleClassName = pos >= 0 ? annoFeatureClassName[(pos + 1)..] : annoFeatureClassName;

                // Création d'un objet FeatureClassDescription pour modifier les propriétés de la classe
                AnnotationFeatureClassDescription fcDescription = new AnnotationFeatureClassDescription(simpleClassName, fcDefinition);

                if (fcDescription.LinkedFeatureClassDescription == null)
                {
                    fcDescription.IsAutoCreate = true;
                    fcDescription.IsUpdatedOnShapeChange = true;
                }

                // Parcours de la collection de symboles de la classe
                int symbolsUpdated = 0;
                foreach (var symbol in fcDescription.Symbols)
                {
                    // Recherche d'un symbole de texte portant le même nom dans le style
                    string symbolName = symbol.Name;
                    var searchedSymbols = style.SearchSymbols(StyleItemType.TextSymbol, symbolName);

                    if (searchedSymbols.Count > 0)
                    {
                        // Remplacement du symbole de texte
                        CIMSymbol newSymbol = searchedSymbols.First<SymbolStyleItem>().Symbol;
                        symbol.Symbol = newSymbol;
                        symbolsUpdated++;
                    }
                }

                if (symbolsUpdated > 0)
                {
                    // Ajout des modifications à la classe d'annotations via un objet SchemaBuilder
                    var geodatabase = annotationFeatureClass.GetDatastore() as Geodatabase;
                    SchemaBuilder schemaBuilder = new SchemaBuilder(geodatabase);
                    schemaBuilder.Modify(fcDescription);
                    result = schemaBuilder.Build();

                    // Message en cas d'erreur
                    if (!result)
                    {
                        log.Fatal(schemaBuilder.ErrorMessages);
                        symbolsUpdated = 0;
                    }
                }

                // Log du résultat
                log.Info(string.Format("{0} Text symbols updated.", symbolsUpdated));

                return result;
            });
        }

        /// <summary>
        /// Synchronisation des champs descriptifs des annotations avec leur CIMTextGraphic modifié.
        /// Le CIMTextGraphic des annotations est bien actualisé quand la collection de symboles est modifiée,
        /// mais curieusement et contrairement à ce qui est avancé dans la documentation, ArcGIS Pro ne 
        /// répercute pas l'information sur les attributs des annotations (fontname, fontstyle, size...).
        /// Une réécritures explicite des valeurs d'attribut est nécessaire.
        /// </summary>
        /// <param name="annotationFeatureClass">Classe d'annotations à modifier</param>
        /// <returns></returns>
        public Task<bool> RefreshAnnotationFields(AnnotationFeatureClass annotationFeatureClass)
        {
            return QueuedTask.Run<bool>(() =>
            {
                var result = false;

                // Il est indispensable d'ouvrir une session d'édition pour modifier des annotations
                var editOp = new EditOperation { Name = "Refresh annotations", SelectModifiedFeatures = false };

                editOp.Callback(context =>
                {
                    // Parcours de toutes les annotations de la classe
                    using (RowCursor cursor = annotationFeatureClass.Search(null, false))
                    {
                        while (cursor.MoveNext())
                        {
                            AnnotationFeature annoFeature = cursor.Current as AnnotationFeature;

                            // Extraction du texte symbol
                            CIMTextGraphic textGraphic = annoFeature.GetGraphic() as CIMTextGraphic;
                            CIMTextSymbol symbol = textGraphic.Symbol.Symbol as CIMTextSymbol;

                            // Famille de police
                            var fieldIndex = annoFeature.FindField("fontname");
                            var value = annoFeature.GetOriginalValue(fieldIndex);
                            value = symbol.FontFamilyName;

                            // Style de police
                            fieldIndex = annoFeature.FindField("fontstyle");
                            value = annoFeature.GetOriginalValue(fieldIndex);
                            value = symbol.FontStyleName;

                            // Taille de police
                            fieldIndex = annoFeature.FindField("fontsize");
                            value = annoFeature.GetOriginalValue(fieldIndex);
                            value = symbol.GetSize();

                            // Souligné
                            fieldIndex = annoFeature.FindField("underline");
                            value = annoFeature.GetOriginalValue(fieldIndex);
                            value = symbol.Underline;

                            // Alignement vertical
                            fieldIndex = annoFeature.FindField("verticalalignment");
                            value = annoFeature.GetOriginalValue(fieldIndex);
                            value = symbol.VerticalAlignment;

                            // Alignement horizontal
                            fieldIndex = annoFeature.FindField("horizontalalignment");
                            value = annoFeature.GetOriginalValue(fieldIndex);
                            value = symbol.HorizontalAlignment;

                            // Offset X
                            fieldIndex = annoFeature.FindField("xoffset");
                            value = annoFeature.GetOriginalValue(fieldIndex);
                            value = symbol.OffsetX;

                            // Offset Y
                            fieldIndex = annoFeature.FindField("yoffset");
                            value = annoFeature.GetOriginalValue(fieldIndex);
                            value = symbol.OffsetY;

                            // Interligne
                            fieldIndex = annoFeature.FindField("fontleading");
                            value = annoFeature.GetOriginalValue(fieldIndex);
                            value = symbol.LineGap;

                            // Intermot
                            fieldIndex = annoFeature.FindField("wordspacing");
                            value = annoFeature.GetOriginalValue(fieldIndex);
                            value = symbol.WordSpacing;

                            // Largeur de caractère
                            fieldIndex = annoFeature.FindField("characterwidth");
                            value = annoFeature.GetOriginalValue(fieldIndex);
                            value = symbol.LetterWidth;

                            // Interlettre
                            fieldIndex = annoFeature.FindField("characterspacing");
                            value = annoFeature.GetOriginalValue(fieldIndex);
                            value = symbol.LetterSpacing;

                            // Angle de basculement
                            fieldIndex = annoFeature.FindField("flipangle");
                            value = annoFeature.GetOriginalValue(fieldIndex);
                            value = symbol.FlipAngle;
                        
                            // Enregistrement des modifications
                            annoFeature.SetGraphic(textGraphic);  // Appel indispensable!
                            annoFeature.Store();
                            context.Invalidate(annoFeature);
                        };
                    }
                }, annotationFeatureClass);

                // Exécution de l'opération
                result = editOp.Execute();

                return result;
            });
        }
    }
}
