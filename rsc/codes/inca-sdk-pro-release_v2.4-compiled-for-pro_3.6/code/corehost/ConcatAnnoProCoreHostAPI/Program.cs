using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IGN.Inca.ArcGISPro.SDK.BLL.CoreHost;
using IGN.Inca.ArcGISPro.SDK.DAL;
using IGN.Inca.ArcGISPro.SDK.Commons;
using IGN.Inca.ArcGISPro.SDK.DAL.Enum;
using IGN.Inca.ArcGISPro.SDK.Commons.Exceptions;
using ArcGIS.Core;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Data.Mapping;
using ArcGIS.Core.Data.Exceptions;
using ArcGIS.Core.Hosting;

namespace ConcatAnnoProCoreHostAPI
{
    /// <summary>
    /// Console ou Stand-Alone Application qui sert d'API vers les fonctionnalités du SDK Pro et la librairie "IncaProSDK (Library)"
    /// </summary>
    class Program
    {
        /// <summary>
        /// Méthode qui gère 2 types de concaténation d'annotations générées à partir de ArcGIS PRO (source) et les insère dans une 
        /// autre Annotation Feature Class (cible) ayant la même structure que les annotations source :
        /// 
        /// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        /// 1. Annotations droites sur plusieurs lignes :
        /// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        /// 
        /// <param name="args">3 arguments sont obligatoires et 1 facultatif :
        /// 
        /// [0]: string => chemin absolu de l'Annotation Feature Class source 
        ///                (celle qui contient les annotations lettre-à-lettre)
        /// 
        /// [1]: string => chemin absolu de l'Annotation Feature Class cible 
        ///                (celle qui contiendra les annotations concaténées)
        /// 
        /// [2]: string => nom du champ qui indique quelles sont les lettres 
        ///                qui appartiennent au même toponyme, c'est-à-dire 
        ///                la clé principale d'origine. "Cleabs" par exemple
        ///                
        /// [3]: string => champs supplémentaires qu'on souhaite conserver séparés
        ///                par une virgule, exemple : "champ_1,champ_2,champ_n"
        /// 
        /// 
        /// Exemple : 
        /// 
        /// string[] args_ = {
        ///        "C:/Users/fortega/Documents/ArcGIS/Projects/SandBoxToponyms/SandBoxToponyms.gdb/src_sample_incomplete",
        ///        "C:/Users/fortega/Documents/ArcGIS/Projects/SandBoxToponyms/SandBoxToponyms.gdb/tgt_sample_incomplete",
        ///        "cleabs",
        ///        "typo"
        ///     };
        ///                     
        /// </param>
        /// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        /// 
        /// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        /// 2. Annotations lettre-à-lettre :
        /// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        /// 
        /// </summary>
        /// <param name="args">6 arguments sont obligatoires et 1 facultatif :
        /// 
        /// [0]: string => chemin absolu de l'Annotation Feature Class source 
        ///                (celle qui contient les annotations lettre-à-lettre)
        /// 
        /// [1]: string => chemin absolu de l'Annotation Feature Class cible 
        ///                (celle qui contiendra les annotations concaténées)
        /// 
        /// [2]: string => nom du champ qui indique le positionnement de la 
        ///                lettre dans la chaîne à reconstruire
        /// 
        /// [3]: string => nom du champ qui indique quelles sont les lettres 
        ///                qui appartiennent au même toponyme, c'est-à-dire 
        ///                la clé principale d'origine. "Cleabs" par exemple
        /// 
        /// [4]: string => nom du champ qui contient le nom complet du toponyme
        /// 
        /// [5]: string => nom du champ supplémentaire qui contient aussi le nom 
        ///                complet du toponyme mais à différence du champ précédent, 
        ///                ce champ est censé contenir tout le nom du toponyme même 
        ///                quand les mots du toponyme ont été découpés et leur clé 
        ///                d'origine est différente. 
        /// [6]: string => champs supplémentaires qu'on souhaite conserver séparés par
        ///                une virgule, exemple : "champ_1,champ_2,champ_n"
        /// 
        /// 
        /// Exemple : 
        /// 
        /// string[] args_ = {
        ///        "C:/Users/fortega/Documents/ArcGIS/Projects/SandBoxToponyms/SandBoxToponyms.gdb/src_sample_incomplete",
        ///        "C:/Users/fortega/Documents/ArcGIS/Projects/SandBoxToponyms/SandBoxToponyms.gdb/tgt_sample_incomplete",
        ///        "ident",
        ///        "cleabs",
        ///        "mot_complet",
        ///        "ecr_compl",
        ///        "typo"
        ///     };
        ///                     
        /// </param>
        /// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        /// 
        /// <returns>Code de retour, regarder l'Enumerator "ExitCodes"</returns>
        [STAThread]
        static void Main(string[] args)
        {
            var ExitCode = ExitCodes.UnknownError;

            var watch = System.Diagnostics.Stopwatch.StartNew();
            Console.WriteLine(">>> Started >>>");

            // initialize "inca_logger_sdk" logger
            LoggerUtils.GetInstance().InitializeLogger();

            /*// #########################################################################
            // ########################### Start - DEBUG ONLY ##########################
            // #########################################################################

            var gdb = "C:/Users/fortega/Documents/ArcGIS/Projects/CONCAT/Test_migration_pour_Fabian/topo_courbes_france_entiere.gdb";

            string[] args_ = {
                
                // droits
                gdb + "/e_toponyme_droit_n0_anno25000", gdb + "/target___france_entiere_echantillon_droits",
                "cleabs", "typo"


                // courbes
                gdb + "/e_toponyme_courbe_n0_anno_25000_eclate_echatillon999", gdb + "/target___france_entiere_echantillon_courbes",
                "ident", "cleabs", "mot_complet", "ecr_compl", "typo"
             };

            args = args_;
            // #########################################################################
            // ############################ End - DEBUG ONLY ###########################
            // #########################################################################*/

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
                if (args != null && args.Length >= 3)
                {
                    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    // ######## 1 - check if parameters are OK
                    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

                    // variables holding the params' values
                    var srcAnnoFeatureClass = args[0];
                    var tgtAnnoFeatureClass = args[1];
                    var origin_key_field_name = args[2]; // CLEABS

                    #region This part of the code is used the concatenation of the toponyms letter-by-letter
                    var index_sequence_field_name = string.Empty;
                    var full_toponym_field_name = string.Empty;
                    var full_words_origin_field_name = string.Empty;
                    var additional_fields = Array.Empty<string>();
                    string add_fields = null;

                    if (args.Length >= 6)
                    {
                        // Case: Concatenation of annotations letter-by-letter
                        index_sequence_field_name = args[2];
                        origin_key_field_name = args[3]; // CLEABS
                        full_toponym_field_name = args[4];
                        full_words_origin_field_name = args[5];
                        if (args.Length == 7)
                        {
                            add_fields = args[6];
                        }
                    } else if(args.Length >= 3)
                    {
                        // Case: Concatenation of straight annotations on multiple lines
                        if (args.Length == 4)
                        {
                            add_fields = args[3];
                        }
                    }

                    if (!string.IsNullOrEmpty(add_fields))
                    {
                        additional_fields = add_fields.Split(',');
                    }
                    #endregion

                    // check if the Annotation Feature Classes are valid 
                    var srcAnnotationFC = GeodatabaseUtils.CheckGeodatabaseAndDataset(srcAnnoFeatureClass, DatasetType.FeatureClass, true);
                    var tgtAnnotationFC = GeodatabaseUtils.CheckGeodatabaseAndDataset(tgtAnnoFeatureClass, DatasetType.FeatureClass, true);

                    if (srcAnnotationFC.IsValid && tgtAnnotationFC.IsValid)
                    {
                        List<AnnotationBO> annotations = new List<AnnotationBO>();
                        HashSet<string> annoCleabsWithErrors = new HashSet<string>();

                        /*
                         *                              PATCH 
                         * 
                         * This control has been deactivated because the toponyms have always a 
                         * baseline of 2 points, so not necessary. And specially to increase the 
                         * performance.
                         * 
                         * 
                        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                        // ######## * - Check if Baseline is null
                        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                        var featuresMissingBaseline = CoreHostAnnoTools.GetInstance().CheckIfAllFeaturesHaveBaseline(srcAnnotationFC);

                        if (featuresMissingBaseline.Count > 0)
                        {
                            ExitCode = ExitCodes.NullBaselineException;
                            var errorMsg = $"{featuresMissingBaseline.Count} objets n'ont pas de Baseline.\n->>> Annotation Feature Class name: {srcAnnotationFC} \n\n->>> Features missing the Baseline (OBJECTID's): {String.Join(", ", featuresMissingBaseline)}";
                            throw new NullBaselineException(errorMsg);
                        }
                        *
                        *
                        */

                        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                        // ######## 2 - Read the annotations
                        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                        var customFieldsToRead = new List<string> { origin_key_field_name };

                        if (args.Length >= 6)
                        {
                            customFieldsToRead = new List<string> { index_sequence_field_name, origin_key_field_name, full_toponym_field_name, full_words_origin_field_name };   
                        }

                        if (additional_fields.Length > 0)
                        {
                            customFieldsToRead.AddRange(additional_fields);
                        }

                        annotations = CoreHostAnnoTools.GetInstance().GetAnnotationsFromAnnotationFeatureClass(srcAnnotationFC, customFieldsToRead);
                        // all the annotations will be straights since they are letters, so the baseline will always have 2 points
                        var straightToponyms = from droit in annotations
                                               where droit.ToponymType == ToponymType.STRAIGHT
                                               select droit;

                        if (straightToponyms.Count() > 0)
                        {
                            try
                            {
                                // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                // ######## 3 - Gather all the toponyms (annotations) having the same
                                //              ORIGIN KEY (Cleabs) and put them in a dictionary
                                // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                var dictAnnosOrderedByCleabs = new Dictionary<string, List<CustomAnnoBO>>();

                                foreach (var aStraightToponym in straightToponyms)
                                {
                                    CustomAnnoBO customToponym = aStraightToponym as CustomAnnoBO;
                                    var cleabs_ori = customToponym.FilteredFields[origin_key_field_name].ToString();
                                    
                                    if (dictAnnosOrderedByCleabs.ContainsKey(cleabs_ori) is false)
                                    {
                                        dictAnnosOrderedByCleabs[cleabs_ori] = new List<CustomAnnoBO>() { customToponym };
                                    }
                                    else
                                    {
                                        dictAnnosOrderedByCleabs[cleabs_ori].Add(customToponym);
                                    }
                                };

                                if (args.Length >= 6)
                                {
                                    // Case: Concatenation of annotations letter-by-letter

                                    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                    // ######## 4 - Control that verifies that "index_sequence_field_name" field values are INTEGERS, if there's an error, log that feature and do the export in a single character anyway
                                    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                    foreach (string cleabs_ori in dictAnnosOrderedByCleabs.Keys)
                                    {
                                        List<CustomAnnoBO> listOfAnnos = dictAnnosOrderedByCleabs[cleabs_ori];

                                        foreach (CustomAnnoBO anAnno in listOfAnnos)
                                        {
                                            int index_seq_int_value;
                                            bool successfullyParsed = int.TryParse(anAnno.FilteredFields[index_sequence_field_name].ToString(), out index_seq_int_value);
                                            if (successfullyParsed)
                                            {
                                                anAnno.FilteredFields[index_sequence_field_name] = index_seq_int_value;
                                            }
                                            else
                                            {
                                                string errorMsg = "Error trying to parse from STRING to INT. CLEABS = " + cleabs_ori;

                                                using (TextWriter errorWriter = Console.Error)
                                                {
                                                    errorWriter.WriteLine(errorMsg);
                                                }

                                                // add the key (cleabs) of this annotation in the list of annotations with errors
                                                annoCleabsWithErrors.Add(cleabs_ori);

                                                break;
                                            }
                                        }
                                    }

                                    foreach (var keyToRemove in annoCleabsWithErrors)
                                    {
                                        dictAnnosOrderedByCleabs.Remove(keyToRemove);
                                    }

                                    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                    // ######## 5 - Concatanate the letters and write them into the target Annotation Feature Class
                                    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                    List<AnnotationBO> annosToInsert = new List<AnnotationBO>();

                                    using (Geodatabase geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(tgtAnnotationFC.GeodatabasePath))))
                                    using (AnnotationFeatureClass targetFC = geodatabase.OpenDataset<AnnotationFeatureClass>(tgtAnnotationFC.DatasetName))
                                    {
                                        // prendre "full_toponym_field_name" strip " ", "#" et compter le nombre de lettres dans ce champ avec le
                                        // nombre de features dans la liste et puis contrôler la séquence du champ "index_sequence_field_name"

                                        foreach (string cleabs_ori in dictAnnosOrderedByCleabs.Keys)
                                        {
                                            List<CustomAnnoBO> listOfAnnos = dictAnnosOrderedByCleabs[cleabs_ori];

                                            var anElementInTheList = listOfAnnos.FirstOrDefault();

                                            // list of points used to compute the average distance between anchor points (in order to compute "CharacterSpacing" value)
                                            var listOfPointsComputeAVG = new List<MapPoint>();
                                            var listOfPolylinesComputeAVG = new List<Polyline>();

                                            // order the letters by "index_sequence_field_name" field
                                            var listOfAnnos_ordered = listOfAnnos.OrderBy(x => x.FilteredFields[index_sequence_field_name]).ToList();

                                            // get the full toponym value
                                            var full_toponym = anElementInTheList.FilteredFields[full_toponym_field_name];

                                            /**
                                             * 
                                             * DEBUG ONLY
                                             * 
                                             */
                                            /*if (cleabs_ori != "25TOPCRB0000000157934275")
                                            {
                                                continue;
                                            }*/
                                            /**
                                             * 
                                             * DEBUG ONLY
                                             * 
                                             */

                                            // manage the "#" values (line break)
                                            var full_toponym_wo_sharps = full_toponym.ToString().Replace("#", " ");
                                            // delete the spaces
                                            var full_toponym_wo_spaces = full_toponym_wo_sharps.Replace(" ", string.Empty);

                                            // split the full toponym value into single words
                                            var words = full_toponym_wo_sharps.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);

                                            // compare if we have the same number of letter annotations than letters in the full toponym value
                                            if (full_toponym_wo_spaces.Count() == listOfAnnos_ordered.Count)
                                            {
                                                var firstElementInTheSortedList = listOfAnnos_ordered.FirstOrDefault();

                                                // setup custom field names and values
                                                var dictKeyValuesCustomFields = new Dictionary<string, object>();
                                                foreach (var itemKey in customFieldsToRead)
                                                {
                                                    var doesKeyExist = firstElementInTheSortedList.FilteredFields.ContainsKey(itemKey);

                                                    if (doesKeyExist)
                                                    {
                                                        var itemValue = firstElementInTheSortedList.FilteredFields[itemKey];
                                                        dictKeyValuesCustomFields[itemKey] = itemValue;
                                                    }
                                                }

                                                // CONTROL : check the index field respects the sequence "n + 2"
                                                var firstIndex = Convert.ToInt32(firstElementInTheSortedList.FilteredFields[index_sequence_field_name]);

                                                int j = 0;
                                                bool indexSequenceControlFailed = false;
                                                // increment +2 the "index_sequence_field_name" since it's a particularity of the data 
                                                for (int i = firstIndex; j < listOfAnnos_ordered.Count; i += 2)
                                                {
                                                    if (i != Convert.ToInt32(listOfAnnos_ordered.ElementAt(j).FilteredFields[index_sequence_field_name]))
                                                    {
                                                        indexSequenceControlFailed = true;
                                                        break;
                                                    }

                                                    j++;
                                                }

                                                if (indexSequenceControlFailed)
                                                {
                                                    // TRY CONTROL INDEX +1
                                                    // new control in case the index sequence increment is 1 by 1
                                                    j = 0;
                                                    indexSequenceControlFailed = false;
                                                    // increment +1 the "index_sequence_field_name" since it's a particularity of the data 
                                                    for (int i = firstIndex; j < listOfAnnos_ordered.Count; i += 1)
                                                    {
                                                        if (i != Convert.ToInt32(listOfAnnos_ordered.ElementAt(j).FilteredFields[index_sequence_field_name]))
                                                        {
                                                            indexSequenceControlFailed = true;
                                                            break;
                                                        }

                                                        j++;
                                                    }

                                                    // if second TRY CONTROL INDEX +1 didn't work, then log an error message
                                                    // and keep the letter-to-letter toponym
                                                    if (indexSequenceControlFailed)
                                                    {
                                                        var srcSplitWordsFromMotCompletField = string.Join(" ", words);
                                                        var errorMsg = "The index sequence field failed for toponym ('" + full_toponym_field_name
                                                            + "' Original toponym field name) : '" + srcSplitWordsFromMotCompletField
                                                            + "' - CLEABS : " + firstElementInTheSortedList.FilteredFields[origin_key_field_name];

                                                        using (TextWriter errorWriter = Console.Error)
                                                        {
                                                            errorWriter.WriteLine(errorMsg);
                                                        }

                                                        // add the key (cleabs) of this annotation in the list of annotations with errors
                                                        annoCleabsWithErrors.Add(cleabs_ori);

                                                        // the process doesn't stop, log and continue with the next toponym
                                                        continue;
                                                    }
                                                }

                                                var geometries1 = new List<Geometry>();
                                                var index = 0;

                                                bool lettersFromFullWordDontMatchWithSingleAnnoLettersControlFailed = false;

                                                // loop all the words in order to build the geometries
                                                foreach (var aWord in words)
                                                {
                                                    List<MapPoint> listOfPoints___ = new List<MapPoint>();

                                                    foreach (var letter in aWord)
                                                    {
                                                        var letterAnno = listOfAnnos_ordered.ElementAt(index);

                                                        // CONTROL : check if the letters from the "full_toponym_field_name" field match with the single annotation letters
                                                        if (letter.ToString() != letterAnno.TextString)
                                                        {
                                                            lettersFromFullWordDontMatchWithSingleAnnoLettersControlFailed = true;
                                                            break;
                                                        }
                                                        else
                                                        {
                                                            var baseline___ = letterAnno.Shape as Polyline;
                                                            listOfPoints___.Add(MapPointBuilderEx.CreateMapPoint(baseline___.Points[0].X, baseline___.Points[0].Y));
                                                            listOfPoints___.Add(MapPointBuilderEx.CreateMapPoint(baseline___.Points[1].X, baseline___.Points[1].Y));

                                                            listOfPointsComputeAVG.Add(MapPointBuilderEx.CreateMapPoint(baseline___.Points[0].X, baseline___.Points[0].Y));
                                                            listOfPolylinesComputeAVG.Add(baseline___);
                                                        }

                                                        index++;
                                                    }

                                                    if (lettersFromFullWordDontMatchWithSingleAnnoLettersControlFailed)
                                                    {
                                                        break;
                                                    }

                                                    Multipoint multiPoint___ = MultipointBuilderEx.CreateMultipoint(listOfPoints___);
                                                    geometries1.Add(multiPoint___);
                                                }

                                                if (lettersFromFullWordDontMatchWithSingleAnnoLettersControlFailed)
                                                {
                                                    var srcToponymJoin = string.Join("", listOfAnnos_ordered.Select(x => x.TextString.ToString()).ToArray());
                                                    var srcSplitWordsFromMotCompletField = string.Join(" ", words);
                                                    var errorMsg = "The letters from the Full Word field don't match with the single annotation letters, the target annotation could not be created. Original toponym ('" + full_toponym_field_name + "' field) : \""
                                                        + srcSplitWordsFromMotCompletField + "\" - Source (from annotation letters) : \"" + srcToponymJoin + "\"";

                                                    using (TextWriter errorWriter = Console.Error)
                                                    {
                                                        errorWriter.WriteLine(errorMsg);
                                                    }

                                                    // add the key (cleabs) of this annotation in the list of annotations with errors
                                                    annoCleabsWithErrors.Add(cleabs_ori);

                                                    continue;
                                                }

                                                GeometryBag bag1 = GeometryBagBuilderEx.CreateGeometryBag(geometries1);
                                                var finalToponymTextString = string.Join(" ", words);

                                                /*if (finalToponymTextString == "Ravine Dundee")
                                                {
                                                    Console.WriteLine("HERE Ravine Dundee");
                                                }*/

                                                /*
                                                 * 
                                                 * NOTA : cette partie il faut l'améliorer absolument pour que l'espace inter-lettres soit plus précis. 
                                                 * 
                                                 * Idée d'implémentation : prendre les points initiaux et finaux de chaque lettre
                                                 * et calculer la largeur de chaque lettre. Ensuite soustraire la somme de ces largeurs 
                                                 * à la longueur totale du mot, et on aura la somme des inter-lettres, à diviser par le nombre d'intervalle
                                                 * 
                                                 *
                                                 */
                                                // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                                //      compute "CharacterSpacing" value
                                                // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                                /*var scale = 25000;
                                                var letterSize = anElementInTheList.FontSize * scale / (2 * 2.835 * 1000);

                                                var avgDistanceBetweenAnchorPoints = 0.0;*/

                                                var listBaselinesbyWords = new List<List<Polyline>>();
                                                int indexBaseline = 0;

                                                foreach (var word in words)
                                                {
                                                    var listBaselineForThisWord = new List<Polyline>();
                                                    char[] charArr = word.ToCharArray();

                                                    foreach (char ch in charArr)
                                                    {
                                                        listBaselineForThisWord.Add(listOfPolylinesComputeAVG[indexBaseline]);
                                                        indexBaseline++;
                                                    }

                                                    listBaselinesbyWords.Add(listBaselineForThisWord);
                                                }

                                                var countOfBaselines = 0;
                                                var totalLengthForAVG = 0.0;
                                                foreach (var baselines in listBaselinesbyWords)
                                                {
                                                    // skip all the one-letter words (i.e. à) from the sum of
                                                    // all the baselines gap and the average computation
                                                    if (baselines.Count > 1)
                                                    {
                                                        for (int i = 1; i < baselines.Count; i++)
                                                        {
                                                            var baselineA = baselines[i - 1];
                                                            var baselineB = baselines[i];

                                                            var pointA = baselineA.Points[1];
                                                            var pointB = baselineB.Points[0];
                                                            var distance = Math.Sqrt(Math.Pow((pointB.X - pointA.X), 2) + Math.Pow((pointB.Y - pointA.Y), 2));
                                                            totalLengthForAVG += distance;
                                                            countOfBaselines++;
                                                        }
                                                    }
                                                }

                                                // on calcule un interlettrage à partir des points intiaux et finaux et en prenant en compte la taille de la police et l'échelle 
                                                // le but c'est d'avoir un character spacing un peu plus précis lorsqu'on édite l'annotation
                                                var avgLetterSpacing = (totalLengthForAVG / countOfBaselines);

                                                var scale = 25000;
                                                var letterSize = (anElementInTheList.FontSize * scale * 0.353) / 1000;
                                                var defaultCharacterSpacing = 0.11 * letterSize;

                                                var spacingRate = ((avgLetterSpacing - defaultCharacterSpacing) / defaultCharacterSpacing) * 100;

                                                if (spacingRate < 0)
                                                {
                                                    spacingRate = 0;
                                                }

                                                /*for (int i = 1; i < listOfPolylinesComputeAVG.Count; i++)
                                                {
                                                    var baselineA = listOfPolylinesComputeAVG[i - 1];
                                                    var baselineB = listOfPolylinesComputeAVG[i];

                                                    var pointA = baselineA.Points[1];
                                                    var pointB = baselineB.Points[1];
                                                    var distance = Math.Sqrt(Math.Pow((pointB.X - pointA.X), 2) + Math.Pow((pointB.Y - pointA.Y), 2));
                                                    Console.WriteLine(distance);
                                                }

                                                if (listOfPointsComputeAVG.Count > 1)
                                                {
                                                    var totalLength = 0.0;
                                                    for (int i = 1; i < listOfPointsComputeAVG.Count; i++)
                                                    {
                                                        var pointA = listOfPointsComputeAVG[i - 1];
                                                        var pointB = listOfPointsComputeAVG[i];

                                                        var distance = Math.Sqrt(Math.Pow((pointB.X - pointA.X), 2) + Math.Pow((pointB.Y - pointA.Y), 2));
                                                        totalLength += distance;
                                                    }
                                                    avgDistanceBetweenAnchorPoints = totalLength / (listOfPointsComputeAVG.Count - 1);
                                                }

                                                var letterSpacing = (avgDistanceBetweenAnchorPoints / letterSize) * 100; // en pourcentage;*/

                                                // update computed values
                                                anElementInTheList.TextString = finalToponymTextString;
                                                anElementInTheList.Shape = bag1;
                                                anElementInTheList.CharacterSpacing = spacingRate;
                                                anElementInTheList.FilteredFields = dictKeyValuesCustomFields;

                                                // add this annotation to the list and do the insertion in one-shot
                                                annosToInsert.Add(anElementInTheList);
                                            }
                                            else
                                            {
                                                var srcToponymJoin = string.Join("", listOfAnnos_ordered.Select(x => x.TextString.ToString()).ToArray());
                                                var srcSplitWordsFromMotCompletField = string.Join(" ", words);
                                                var errorMsg = "Uncomplete source toponym, the target annotation could not be created. Original toponym ('" + full_toponym_field_name + "' field) : \""
                                                    + srcSplitWordsFromMotCompletField + "\" - Source (from annotation letters) : \"" + srcToponymJoin + "\"";

                                                using (TextWriter errorWriter = Console.Error)
                                                {
                                                    errorWriter.WriteLine(errorMsg);
                                                }

                                                // add the key (cleabs) of this annotation in the list of annotations with errors
                                                annoCleabsWithErrors.Add(cleabs_ori);
                                            }
                                        }
                                    }

                                    // one-shot insertion of the annotations
                                    CoreHostAnnoTools.GetInstance().ExportAnnotationsIntoFeatureClass(annosToInsert, tgtAnnotationFC);

                                } else if (args.Length >= 3)
                                {
                                    // Case: Concatenation of straight annotations on multiple lines
                                    List<AnnotationBO> annosToInsert = new List<AnnotationBO>();

                                    foreach (string cleabs_ori in dictAnnosOrderedByCleabs.Keys)
                                    {
                                        try
                                        {
                                            List<CustomAnnoBO> listOfAnnos = dictAnnosOrderedByCleabs[cleabs_ori];

                                            // Sort the list according to the Ymax coordinate of the annotation extent
                                            listOfAnnos.Sort((x, y) => y.Shape.Extent.YMax.CompareTo(x.Shape.Extent.YMax));

                                            var anElementInTheList = listOfAnnos.FirstOrDefault();

                                            string fullTextString = string.Empty;
                                            var geometries1 = new List<Geometry>();

                                            List<Polyline> listOfLines___ = new List<Polyline>();

                                            foreach (CustomAnnoBO anAnno in listOfAnnos)
                                            {
                                                // concat the TextString
                                                if (fullTextString == string.Empty)
                                                {
                                                    fullTextString = anAnno.TextString;
                                                }
                                                else
                                                {
                                                    // add a break-line if toponym is hold on 2 lines or more
                                                    fullTextString += "\n" + anAnno.TextString;
                                                }

                                                // concat the Shape
                                                listOfLines___.Add(anAnno.Shape as Polyline);
                                            }

                                            var polylineCreate = PolylineBuilderEx.CreatePolyline(listOfLines___);

                                            anElementInTheList.TextString = fullTextString;
                                            anElementInTheList.Shape = polylineCreate;

                                            annosToInsert.Add(anElementInTheList);
                                        }
                                        catch (Exception)
                                        {
                                            var errorMsg = "The toponym having the KEY ORIGIN (CLEABS) = '" + cleabs_ori +
                                                "' has failed but it was included in the final export anyway.";

                                            using (TextWriter errorWriter = Console.Error)
                                            {
                                                errorWriter.WriteLine(errorMsg);
                                            }

                                            // add the key (cleabs) of this annotation in the list of annotations with errors
                                            annoCleabsWithErrors.Add(cleabs_ori);

                                            // the process doesn't stop, log and continue with the next toponym
                                            continue;
                                        }
                                    }

                                    // one-shot insertion of the annotations
                                    CoreHostAnnoTools.GetInstance().ExportAnnotationsIntoFeatureClass(annosToInsert, tgtAnnotationFC);
                                }
                            }
                            catch (GeodatabaseException ex)
                            {
                                ExitCode = ExitCodes.GeodatabaseError;
                                throw new GeodatabaseException("Un problème est survenu lors de la concaténation des lettres. ", ex);
                            }
                        }

                        // Manage the annotation with errors, copy them in the FC target
                        if (annoCleabsWithErrors.Count > 0)
                        {
                            string delim = "', '";
                            var str = String.Join(delim, annoCleabsWithErrors);

                            QueryFilter queryFilter = new QueryFilter
                            {
                                WhereClause = origin_key_field_name + " IN ('" + str + "')"
                            };

                            using (Geodatabase geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(tgtAnnotationFC.GeodatabasePath))))
                            using (AnnotationFeatureClass sourceFC = geodatabase.OpenDataset<AnnotationFeatureClass>(srcAnnotationFC.DatasetName))
                            using (RowCursor rowCursor = sourceFC.Search(queryFilter, false))
                            {
                                // Read the annotations with errors 
                                List<AnnotationBO> annosToInsert = new List<AnnotationBO>();

                                while (rowCursor.MoveNext())
                                {
                                    using (var row = rowCursor.Current as AnnotationFeature)
                                    {
                                        try
                                        {
                                            var customFields = new List<string>() { index_sequence_field_name, origin_key_field_name,
                                            full_toponym_field_name, full_words_origin_field_name };

                                            var aFilteredAnnotationToExportCustom = new CustomAnnoBO(row, customFields);
                                            annosToInsert.Add(aFilteredAnnotationToExportCustom);
                                        }
                                        catch (GeodatabaseException ex)
                                        {
                                            throw new GeodatabaseException(ex);
                                        }
                                    }
                                }

                                // Insert the annotation with errors in the Annotation FC Target
                                CoreHostAnnoTools.GetInstance().ExportAnnotationsIntoFeatureClass(annosToInsert, tgtAnnotationFC);
                            }
                        }
                    }
                    else
                    {
                        ExitCode = ExitCodes.InvalidSourceOrTargetAnnotationFeatureClass;
                        throw new ArgumentException(string.Format("La Feature Class d'Annotations source {0} ou target {1} n'est pas valide ou n'existe pas.",
                            srcAnnoFeatureClass, tgtAnnoFeatureClass));
                    }

                    // si cette ligne s'exécute, cela veut dire que tout s'est bien déroulé
                    ExitCode = ExitCodes.Success;
                }
                else
                {
                    ExitCode = ExitCodes.BadNumberOfArguments;
                    throw new ArgumentException(string.Format("Le nombre d'arguments envoyés ne correspond pas à l'attendu. Paramètres envoyés: {0} - Nombre attendu: 6", args.Length));
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

                // Console.ReadKey();
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
        InvalidSourceOrTargetAnnotationFeatureClass = 30,
        ArcGISInitializationFailed = 50,
        GeodatabaseError = 60,
        NullBaselineException = 80,
        UnknownError = -1
    }
}