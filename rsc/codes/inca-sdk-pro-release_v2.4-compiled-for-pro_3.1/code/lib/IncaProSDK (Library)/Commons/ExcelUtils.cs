using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
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
    /// Classe qui gère la lecture d'un fichier Excel
    /// </summary>
    public class ExcelUtils
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public ExcelUtils()
        { }

        private static readonly ExcelUtils _singleton = new ExcelUtils();

        /// <summary>
        /// Pattern Singleton
        /// </summary>
        /// <returns>Une instance de la classe ExcelUtils</returns>
        public static ExcelUtils GetInstance()
        {
            return _singleton;
        }

        /// <summary>
        /// Returns a List of the rows as dictionary as Key(ColumnName)/Value from a given Excel file
        /// </summary>
        /// <param name="existingFileURI">the excel file</param>
        /// <param name="worksheetName">the worksheet name to read</param>
        /// <param name="headRowNumber">the head column names row number </param>
        /// <returns>A list of rows (dictionaries)</returns>
        public List<Dictionary<string, string>> ReadExcelWorksheet(string existingFileURI, string worksheetName, int headRowNumber = 1)
        {
            List<Dictionary<string, string>> toReturn = null;

            try
            {
                if (!File.Exists(existingFileURI))
                    throw new FileNotFoundException(string.Format("The file <{0}> doesn't exist", existingFileURI));

                var existingFile = new FileInfo(existingFileURI);

                // If you use EPPlus in a noncommercial context
                // according to the Polyform Noncommercial license:
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (ExcelPackage xlPackage = new ExcelPackage(existingFile))
                {
                    /*DataTable dt = new DataTable();
                    dt = xlPackage.ToDataTable();
                    List<DataRow> listOfRows = new List<DataRow>();
                    listOfRows = dt.AsEnumerable().ToList();*/

                    var selectedWorkSheet = xlPackage.Workbook.Worksheets.Where(x => x.Name.Equals(worksheetName));

                    if (selectedWorkSheet.Count() > 0)
                    {
                        var workSheet = selectedWorkSheet.FirstOrDefault();
                        toReturn = new List<Dictionary<string, string>>();

                        for (int i = headRowNumber + 1; // workSheet.Dimension.Start.Row
                             i <= workSheet.Dimension.End.Row;
                             i++)
                        {
                            var newDict = new Dictionary<string, string>();

                            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                            // ~~~~~~~~~~ Starts Patch ~~~~~~~~~~ 
                            // First column value of a row cannot be NULL
                            //
                            // Note: this check has been added because sometimes there are blank rows
                            // found by this library, so to avoid this the first column is mandatory
                            if (workSheet.Cells[i, 1].Value is null)
                                continue;
                            // ~~~~~~~~~~ Ends Patch ~~~~~~~~~~
                            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

                            /*var keyValue = workSheet.Cells[i, 1].Value.ToString();

                            // Check if key is null
                            if (keyValue is null)
                            {
                                continue;
                                // LOG THIS
                                // throw new Exception("The key in the Excel file is null!");
                            }

                            // Check if key has already been added
                            if (toReturn.ContainsKey(keyValue))
                            {
                                continue;
                                // LOG THIS
                                // throw new Exception("The duplicated key in dictionary!");
                            }*/

                            //var breakFirstLoop = false;

                            for (int j = workSheet.Dimension.Start.Column;
                                     j <= workSheet.Dimension.End.Column;
                                     j++)
                            {
                                var columnName = workSheet.Cells[headRowNumber, j].Value;
                                var cellValue = workSheet.Cells[i, j].Value;

                                if (columnName != null && cellValue != null)
                                    newDict.Add(columnName.ToString(), cellValue.ToString());
                                else if (columnName != null)
                                    newDict.Add(columnName.ToString(), null);
                                /*else
                                {
                                    // LOG
                                    // temp : continue next Excel ROW
                                    breakFirstLoop = true;
                                    break;
                                }*/
                            }

                            //if (!breakFirstLoop)
                            toReturn.Add(newDict);
                        }
                    }
                    else
                    {
                        // PROBLEM : worksheet was not found !
                        throw new ArgumentException(string.Format("La feuille Excel <{0}> n'existe pas dans le fichier <{1}>.", worksheetName, existingFileURI));
                    }
                    
                    // do work here                            
                    // xlPackage.Save();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Problem reading Excel config file. Message error: " + ex.Message, ex);
            }

            return toReturn;
        }
    }
}
