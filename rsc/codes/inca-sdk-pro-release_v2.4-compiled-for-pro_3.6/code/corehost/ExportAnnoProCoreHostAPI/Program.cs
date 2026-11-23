using ArcGIS.Core.Data;
using ArcGIS.Core.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IGN.Inca.ArcGISPro.SDK.BLL.CoreHost;
using IGN.Inca.ArcGISPro.SDK.DAL;
using IGN.Inca.ArcGISPro.SDK.Commons;
using IGN.Inca.ArcGISPro.SDK.DAL.Enum;
using IGN.Inca.ArcGISPro.SDK.Commons.Exceptions;
using ArcGIS.Core.Data.Exceptions;

namespace ExportAnnoProCoreHostAPI
{
    /// <summary>
    /// Console ou Stand-Alone Application qui sert d'API vers la librairie "IncaProSDK (Library)" et aux fonctionnalités du SDK Pro
    /// </summary>
    class Program
    {
        /// <summary>
        /// Méthode principale qui prend un des annotations générées à partir de ArcGIS PRO
        /// et les exporte dans 2 Feature Classes (une pour les annotations DROITES et une 
        /// autre pour les COURBES) ayant la même structure que les annotations mais toujours 
        /// de type POINT.
        /// 
        /// ATTENTION : les 2 Feature Classes en sortie doivent pre-exister.
        /// </summary>
        /// <param name="args">3 arguments sont obligatoires et 1 optionnel :
        /// 
        /// [0]:string => le 1er argument doit être un array de type JSON 
        /// contenant la liste des annotations à exporter (chemins absolus sont nécessaires)
        /// 
        /// [1]: string => le 2nd argument correspond à la Feature Classe qui va stocker les
        /// annotations DROITES (chemin absolu)
        /// 
        /// [2]: string => le 3ème argument correspond à la Feature Classe qui va stocker les
        /// annotations COURBES (chemin absolu)
        /// 
        /// [3]: string => [OPTIONNEL] Liste de champs séparés par un point-virgule (;) qu'on 
        /// souhaite exporter. Si ce paramètre est présent, il s'agîra d'un export de type
        /// "custom", c'est-à-dire les colonnes à exporter seront basées sur la liste de champs 
        /// renseignés dans ce paramètre, si ce champ est null ou la liste est vide, l'export
        /// sera de type "standard", c'est-à-dire basé sur le modèle classique d'une annotation.
        /// 
        /// NOTE : dans les 2 cas, les champs "OBJECTID", "TEXTSTRING", "SHAPE" et "ANGLE" 
        ///        sont obligatoires.
        /// 
        /// Exemple : 
        /// 
        /// string[] args_ = {
        ///        "[" +
        ///           "'C:/Users/fortega/Documents/ArcGIS/Projects/Projet_SocleCarto_13_tests_after_holidays/Socle_carto_13_toponymes/socle_carto.gdb/HYDROGRAPHIE/h_surf_n0_plan_ign_25k'" +
        ///            ", 'C:/Users/fortega/Documents/ArcGIS/Projects/Projet_SocleCarto_13_tests_after_holidays/Socle_carto_13_toponymes/socle_carto.gdb/ECRITURE/e_ponc_n0_plan_ign_25k'" +
        ///            ", 'C:/Users/fortega/Documents/ArcGIS/Projects/Projet_SocleCarto_13_tests_after_holidays/Socle_carto_13_toponymes/socle_carto.gdb/ECRITURE/e_surf_n0_plan_ign_25k'" +
        ///            ", 'C:/Users/fortega/Documents/ArcGIS/Projects/Projet_SocleCarto_13_tests_after_holidays/Socle_carto_13_toponymes/socle_carto.gdb/ECRITURE/autoroutes_n0_plan_ign_25k'" +
        ///        "]",
        ///        "C:/Users/fortega/Documents/ArcGIS/Projects/Projet_SocleCarto_13_tests_after_holidays/Socle_carto_13_toponymes/socle_carto.gdb/ECRITURE/e_toponyme_droit_plan_ign_25k",
        ///        "C:/Users/fortega/Documents/ArcGIS/Projects/Projet_SocleCarto_13_tests_after_holidays/Socle_carto_13_toponymes/socle_carto.gdb/ECRITURE/e_toponyme_courbe_plan_ign_25k",
        ///        "CLEABS;SYMBO;fontleading;WORDSPACING;FontName"
        ///     };
        ///                     
        /// </param>
        /// <returns>Code de retour, regarder l'Enumerator "ExitCodes"</returns>
        [STAThread]
        static void Main(string[] args)
        {
            var ExitCode = ExitCodes.UnknownError;

            var watch = System.Diagnostics.Stopwatch.StartNew();
            Console.WriteLine(">>> Started >>>");

            // initialize "inca_logger_sdk" logger
            LoggerUtils.GetInstance().InitializeLogger();

            // #########################################################################
            // ########################### Start - DEBUG ONLY ##########################
            // #########################################################################
            //var gdb = "C:/Users/fortega/Documents/ArcGIS/Projects/Sprint_INCA_11/courbes_bezier_nico/Socle_carto_45/Test_Ecla.gdb";

            //string[] args_ = {
            //    "[" +
            //       "' " + gdb + "/a_lin_1000Anno'" +
            //    "]",
            //    gdb + "/out_annotations_straight",
            //    gdb + "/out_annotations_curved",

            // };

            //args = args_;
            // #########################################################################
            // ############################ End - DEBUG ONLY ###########################
            // #########################################################################

            try
            {
                //
                //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                // ######## 0 - Initialiser ArcGIS avant d'utiliser la librairie "ArcGIS.Core"
                // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                try
                {
                    Host.Initialize();
                }
                catch (Exception ex)
                {
                    // Error (missing installation, no license, 64 bit mismatch, etc.)
                    ExitCode = ExitCodes.ArcGISInitializationFailed;
                    throw new Exception("ArcGIS Initialization failed", ex);
                }

                // if we are here, ArcGIS.Core is successfully initialized
                // Now, do the operations using "ArcGIS.Core.Data" assembly
                if (args != null && (args.Length == 3 || args.Length == 4))
                {
                    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    // ######## 1 - check if JSON array parameter is valid
                    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

                    var sourceAnnotationFCList = JsonUtils.ConvertJSONArrayToList(args[0].ToString());
                    if (sourceAnnotationFCList is null)
                    {
                        ExitCode = ExitCodes.InvalidJSONArray;
                        throw new Exception(string.Format("Le JSON string envoyé en paramètre n'est pas valide: {0}", args[0].ToString()));
                    }

                    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    // ######## 2 - check if target Feature Classes that will store straight and curved toponyms are valid
                    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    var targetStraightToponymFeatureClass = args[1];
                    var targetCurvedToponymFeatureClass = args[2];

                    var targetStraightToponymFC = GeodatabaseUtils.CheckGeodatabaseAndDataset(targetStraightToponymFeatureClass, DatasetType.FeatureClass);
                    var targetCurvedToponymFC = GeodatabaseUtils.CheckGeodatabaseAndDataset(targetCurvedToponymFeatureClass, DatasetType.FeatureClass);

                    if (!targetStraightToponymFC.IsValid || !targetCurvedToponymFC.IsValid)
                    {
                        var message = "La Feature Class cible {0} qui doit stocker les toponymes {1} n'est pas valide.";
                        if (!targetStraightToponymFC.IsValid && !targetCurvedToponymFC.IsValid)
                        {
                            message = string.Format(message, targetStraightToponymFeatureClass, "Droits")
                                + "\r\nEt\r\n"
                                + string.Format(message, targetCurvedToponymFeatureClass, "Courbes");
                        }
                        else if (!targetStraightToponymFC.IsValid)
                            message = string.Format(message, targetStraightToponymFeatureClass, "Droits");
                        else if (!targetCurvedToponymFC.IsValid)
                            message = string.Format(message, targetCurvedToponymFeatureClass, "Courbes");

                        ExitCode = ExitCodes.InvalidTargetFeatureClass;
                        throw new ArgumentException(message);
                    }

                    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    // ######## 3 - check filter export fields parameter
                    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    // add this argument => "ID;CLEABS;SYMBO" and convert it into a List<String>
                    var customFieldsParam = args.Length == 4 ? args[3] : null;
                    var customFields = new List<string>();

                    if (!String.IsNullOrWhiteSpace(customFieldsParam))
                        customFields = customFieldsParam.Split(';').ToList();

                    // on parcourt la liste des Annotation Feature Classes envoyés dans le JSON array
                    foreach (var item in sourceAnnotationFCList)
                    {
                        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                        // ######## 4 - check if the current Annotation Feature Class is valid
                        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

                        var sourceAnnotationFC = GeodatabaseUtils.CheckGeodatabaseAndDataset(item, DatasetType.FeatureClass, true);

                        if (sourceAnnotationFC.IsValid)
                        {
                            List<AnnotationBO> annotations = new List<AnnotationBO>();

                            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                            // ######## 5 - Check if filter export fields exist in source and target Feature Classes
                            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                            try
                            {
                                if (customFields.Count > 0)
                                    GeodatabaseUtils.CheckFieldsInDatasets(customFields, new List<DatasetBO>() { sourceAnnotationFC, targetStraightToponymFC, targetCurvedToponymFC });
                            }
                            catch (GeodatabaseFieldException)
                            {
                                ExitCode = ExitCodes.GeodatabaseFieldError;
                                throw;
                            }

                            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                            // ######## 6 - Check if Baseline is null
                            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                            var featuresMissingBaseline = CoreHostAnnoTools.GetInstance().CheckIfAllFeaturesHaveBaseline(sourceAnnotationFC);

                            if (featuresMissingBaseline.Count > 0)
                            {
                                ExitCode = ExitCodes.NullBaselineException;
                                var errorMsg = $"{featuresMissingBaseline.Count} objets n'ont pas de Baseline.\n->>> Annotation Feature Class name: {item} \n\n->>> Features missing the Baseline (OBJECTID's): {String.Join(", ", featuresMissingBaseline)}";
                                throw new NullBaselineException(errorMsg);
                            }

                            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                            // ######## 7 - Read the annotations
                            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                            annotations = CoreHostAnnoTools.GetInstance().GetAnnotationsFromAnnotationFeatureClass(sourceAnnotationFC, customFields);

                            var straightToponyms = from droit in annotations
                                                   where droit.ToponymType == ToponymType.STRAIGHT
                                                   select droit;

                            var curvedToponyms = from courbe in annotations
                                                 where courbe.ToponymType == ToponymType.CURVED
                                                 select courbe;

                            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                            // ######## 8 - Write the annotations into straight toponyms Feature Class
                            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

                            if (straightToponyms.Count() > 0)
                            {
                                // préparer les données
                                var straightToponymsReady = CoreHostAnnoTools.GetInstance().SetMiddlePointAsAnchorForStraightToponyms(straightToponyms);

                                // Ecrire les annotations droites
                                try
                                {
                                    CoreHostAnnoTools.GetInstance().ExportAnnotationsIntoFeatureClass(straightToponymsReady, targetStraightToponymFC);
                                }
                                catch (GeodatabaseException ex)
                                {
                                    ExitCode = ExitCodes.GeodatabaseError;
                                    throw new GeodatabaseException("Un problème est survenu lors de l'export des Annotations Droites. Annotation Feature Class name : " + item, ex);
                                }
                            }

                            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                            // ######## 9 - Write the annotations into curved toponyms Feature Class
                            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                            if (curvedToponyms.Count() > 0)
                            {
                                // préparer les données
                                var curvedToponymsIntoLetters = CoreHostAnnoTools.GetInstance().CollapseCurvedToponymIntoSingleCharacters(curvedToponyms);

                                // Ecrire les annotations courbes
                                try
                                {
                                    CoreHostAnnoTools.GetInstance().ExportAnnotationsIntoFeatureClass(curvedToponymsIntoLetters, targetCurvedToponymFC);
                                }
                                catch (GeodatabaseException ex)
                                {
                                    ExitCode = ExitCodes.GeodatabaseError;
                                    throw new GeodatabaseException("Un problème est survenu lors de l'export des Annotations Courbes. Annotation Feature Class name : " + item, ex);
                                }
                            }
                        }
                        else
                        {
                            ExitCode = ExitCodes.InvalidSourceAnnotationFeatureClass;
                            throw new ArgumentException(string.Format("La Feature Class d'Annotations {0} source n'est pas valide.", item));
                        }
                    }

                    // si cette ligne s'exécute, cela veut dire que tout s'est bien déroulé
                    ExitCode = ExitCodes.Success;
                }
                else
                {
                    ExitCode = ExitCodes.BadNumberOfArguments;
                    throw new ArgumentException(string.Format("Le nombre d'arguments envoyés ne correspond pas à l'attendu. Paramètres envoyés: {0} - Nombre attendu: 3", args.Length));
                }
            }
            catch (Exception ex)
            {
                using (TextWriter errorWriter = Console.Error)
                {
                    var errorMessage = string.Format("[{0}] ExitCode: {1} ({2}) {3}\r\nStack Trace: {4}", ex.GetType().Name, (int)ExitCode, EnumUtils.StringValueOf(ExitCode), ex.Message, ex.StackTrace);
                    errorWriter.WriteLine(errorMessage);
                }
            }
            finally
            {
                watch.Stop();
                var elapsedSeconds = watch.ElapsedMilliseconds / 1000;

                Console.WriteLine(string.Format("Ended in {0} seconds", elapsedSeconds));

                Environment.Exit((int)ExitCode);
            }
        }
    }

    /// <summary>
    /// Enumerator qui indique les différents types de code de retour de l'application
    /// </summary>
    [Flags]
    enum ExitCodes : int
    {
        Success = 6,
        BadNumberOfArguments = 10,
        InvalidArguments = 20,
        InvalidSourceAnnotationFeatureClass = 30,
        InvalidTargetFeatureClass = 40,
        ArcGISInitializationFailed = 50,
        GeodatabaseError = 60,
        GeodatabaseFieldError = 61,
        InvalidJSONArray = 70,
        NullBaselineException = 80,
        UnknownError = -1
    }
}