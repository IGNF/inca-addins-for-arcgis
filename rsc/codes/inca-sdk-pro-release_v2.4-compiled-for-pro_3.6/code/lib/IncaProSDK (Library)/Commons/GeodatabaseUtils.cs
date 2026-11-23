using ArcGIS.Core.Data;
using ArcGIS.Core.Data.Mapping;
using IGN.Inca.ArcGISPro.SDK.DAL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Namespace qui contient l'ensemble d'outils communs et génériques 
/// dans le projet INCA - ArcGIS Pro SDK
/// </summary>
namespace IGN.Inca.ArcGISPro.SDK.Commons
{
    /// <summary>
    /// Classe contenant divers contrôles
    /// </summary>
    public static class GeodatabaseUtils
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        const string GdbExtension = ".gdb";

        /// <summary>
        /// Méthode qui contrôlle à partir d'une URL si le type de Dataset (Unknown, FeatureDataset, FeatureClass,
        /// Table, RelationshipClass, RasterDataset, RasterBand, UtilityNetwork, AttributedRelationshipClass) est 
        /// valide ou pas
        /// </summary>
        /// <param name="gdbAndDatasetURLToCheck">Chemin absolu de la geodatabase et dataset à contrôler</param>
        /// <param name="datasetType">Le type de Dataset, basé sur les types du enumerator ArcGIS SDK ArcGIS.Core.Data.DatasetType</param>
        /// <param name="isAnAnnotationFeatureClass">Paramètre facultatif, true s'il on veut contrôler une Annotation Feature Class</param>
        /// <returns>Retourne une instance de type DatasetBO</returns>
        public static DatasetBO CheckGeodatabaseAndDataset(string gdbAndDatasetURLToCheck, DatasetType datasetType, bool isAnAnnotationFeatureClass = false)
        {
            var datasetToReturn = new DatasetBO
            {
                DatasetType = datasetType
            };

            try
            {
                var splittedPath = Path.GetFullPath(gdbAndDatasetURLToCheck).Split(new[] { GdbExtension }, StringSplitOptions.None);
                datasetToReturn.GeodatabasePath = string.Format("{0}{1}", splittedPath.FirstOrDefault(), GdbExtension);
                datasetToReturn.DatasetName = splittedPath.LastOrDefault().Split(new[] { '\\' }, StringSplitOptions.None).LastOrDefault();

                if (Directory.Exists(datasetToReturn.GeodatabasePath))
                {
                    using (Geodatabase geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(datasetToReturn.GeodatabasePath))))
                    {
                        if (datasetToReturn.DatasetType == DatasetType.Unknown)
                        {
                            datasetToReturn.IsValid = true;
                        }
                        else if (datasetToReturn.DatasetType == DatasetType.Table)
                        {
                            geodatabase.GetDefinition<TableDefinition>(datasetToReturn.DatasetName);
                            datasetToReturn.IsValid = true;
                        }
                        else if (datasetToReturn.DatasetType == DatasetType.FeatureClass)
                        {
                            if (isAnAnnotationFeatureClass)
                            {
                                /*// tests
                                var test = geodatabase.GetDefinition<TableDefinition>(datasetToReturn.DatasetName);
                                var test2 = geodatabase.GetDefinition<FeatureClassDefinition>(datasetToReturn.DatasetName);
                                var test3 = geodatabase.GetDefinition<AnnotationFeatureClassDefinition>(datasetToReturn.DatasetName);
                                // tests*/

                                datasetToReturn.IsAnnotation = geodatabase.GetDefinition<AnnotationFeatureClassDefinition>(datasetToReturn.DatasetName) == null ? false : true;
                            }
                            else
                                geodatabase.GetDefinition<FeatureClassDefinition>(datasetToReturn.DatasetName);

                            datasetToReturn.IsValid = true;
                        }
                        else
                        {
                            // Manage other Dataset type here...
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(string.Format("The Dataset '{0}' type of '{1}' is not valid.\nSee the stacktrace: {2}", gdbAndDatasetURLToCheck, datasetType.GetType(), ex.StackTrace));
                datasetToReturn.IsValid = false;
            }

            return datasetToReturn;
        }

        /// <summary>
        /// Méthode qui vérifie l'existence d'une liste de champs dans un ensemble de datasets (Annotations, Feature Classes, Tables)
        /// </summary>
        /// <param name="fieldsToCheck">Liste de champs</param>
        /// <param name="datasetsToCheck">Liste de Datasets</param>
        public static void CheckFieldsInDatasets(List<string> fieldsToCheck, List<DatasetBO> datasetsToCheck)
        {
            foreach (var checkDataset in datasetsToCheck)
            {
                using (Geodatabase geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(checkDataset.GeodatabasePath))))
                using (FeatureClass checkFC = geodatabase.OpenDataset<FeatureClass>(checkDataset.DatasetName))
                {
                    foreach (var checkField in fieldsToCheck)
                    {
                        if (checkFC.GetDefinition().FindField(checkField) < 0)
                            throw new ArcGIS.Core.Data.Exceptions.GeodatabaseFieldException(string.Format("The field '{0}' doesn't exist in table '{1}'", checkField, checkFC.GetName()));
                    }
                }
            }
        }
    }
}
