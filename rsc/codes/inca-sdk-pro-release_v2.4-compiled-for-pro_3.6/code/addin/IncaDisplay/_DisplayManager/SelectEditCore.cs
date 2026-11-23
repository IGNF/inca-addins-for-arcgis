using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using IGN.Inca.ArcGISPro.SDK.Commons;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

namespace IncaDisplay._DisplayManager
{
    /// <summary>
    /// Classe prenant en charge les opérations de transtypage d'entités.
    /// Le transtypage consiste à déplacer d'une classe à une autre des entités selon des règles
    /// définies par l'utilisateur.
    /// </summary>
    public sealed class SelectEditCore
    {
        #region Parameters

        /// <summary>
        /// Instance de la classe SelectEditCore
        /// /// </summary>
        private static SelectEditCore _instance;


        #endregion

        #region Dictionnaire
        public static string AlreadyExist
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "\" est déjà ouvert;" +
                        " Impossible de mettre à jour le fichier.\"";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "ya está abierto." +
                        "No se puede actualizar el archivo.";
                else return "is already open." +
                        "Unable to update the file.";
            }
        }
        public static string VisiblityExist
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "La visibilité existe déjà." +
                        "Voulez-vous la modifier ?";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "La visibilidad ya existe." +
                        " ¿Quiere cambiarla?";
                else return "Visibility already exists." +
                        "Do you want to change it?";
            }
        }
        public static string NameMissing
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Il faut préciser un nom pour enregistrer la visibilité";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Es necesario especificar un nombre para guardar la visibilidad";
                else return "need to specify a name to save the visibility";
            }
        }
        #endregion Dictionnaire

        # region Propriété
        /// <summary>
        /// Création ou appel à l'instance commune de la classe SelectEditCore
        /// </summary>
        /// <returns>L'instance de la classe</returns>
        public static SelectEditCore GetInstance()
        {
            if (_instance == null)
            {
                _instance = new SelectEditCore();
            }
            return _instance;
        }
        #endregion Propriété

        #region fonctions
        /// <summary>
        /// Loading display rules from an excel file
        /// </summary>
        /// <param name="excelConfigFileURI"> Name of Excel file describing display rules</param>
        /// <returns>loading result</returns>
        public Task<bool> LoadSelectEditRules(string excelConfigFileURI)
        {
            return QueuedTask.Run(() =>
            {
                try
                {
                    // liste des choix possibles pour l'onglet gestionnaire => à afficher dans le menu déroulant
                    // Lecture du fichier Excel
                    List<Dictionary<string, string>> rulesFromExcel;
                    try 
                    {
                        rulesFromExcel = ExcelUtils.GetInstance().ReadExcelWorksheet(existingFileURI: excelConfigFileURI, worksheetName: "gestionnaire", headRowNumber: 1);
                    }
                    catch
                    {
                        rulesFromExcel = ExcelUtils.GetInstance().ReadExcelWorksheet(existingFileURI: excelConfigFileURI, worksheetName: "manager", headRowNumber: 1);
                    }
                    // liste de gestion => à afficher dans le menu déroulant
                    SelectEditViewModel.List_Choice = new List<string>();

                    // parcours de l'onglet "Gestionnaire" du fichier sélectionné pour récupérer les noms des onglets à parcourir
                    foreach (Dictionary<string, string> line in rulesFromExcel)
                    {
                        ConfigFileUtils dictTools = new ConfigFileUtils(line);

                        //  on copie la Colonne NOM dans la liste si la colonne existe                     
                        string nom = dictTools.GetArgumentValue(SelectEditRule.name_ALIAS);
                        if (string.IsNullOrWhiteSpace(nom)) throw new ArgumentNullException("The mandatory field" + SelectEditRule.name_ALIAS+ " is empty");
                        else SelectEditViewModel.List_Choice.Add(nom);
                    }

                    // une fois la liste de nom de choix créée, on récupère les règles à appliquer pour chaque choix de visibilité.
                    List<SelectEditRule> SE_Rules = new List<SelectEditRule>();
                    foreach (var name in SelectEditViewModel.List_Choice)
                    {
                        //Lecture du fichier Excel
                        rulesFromExcel = ExcelUtils.GetInstance().ReadExcelWorksheet(excelConfigFileURI, name, headRowNumber: 1);

                        // Restructuration du contenu sous forme de règles d'affichage
                        foreach (Dictionary<string, string> line in rulesFromExcel)
                        {
                            SelectEditRule rule = new SelectEditRule();
                            rule.CreateRule(line);
                            SE_Rules.Add(rule);
                        }
                        SelectEditViewModel.SelectEditRules = SE_Rules;
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Warning", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            });
        }

        /// <summary>
        /// save display rules in excel file
        /// </summary>
        /// <param name="map">Map to assign</param>
        /// <param name="excelConfigFileURI">Name of Excel file describing display rules</param>
        /// <param name="worksheetName">visibility name -> name of the tab in the Excel file</param>
        /// <returns>saving result </returns>
        public Task<bool> SaveSelectEditRules(Map map, string excelConfigFileURI, string worksheetName)
        {
            return QueuedTask.Run(() =>
            {
                try
                {
                    List<SelectEditRule> List_Rules = new List<SelectEditRule>();

                    // listes des couches et annotations de la carte
                    List<FeatureLayer> listLayers = map.GetLayersAsFlattenedList().OfType<FeatureLayer>().ToList();
                    List<AnnotationLayer> listAnnotations = map.GetLayersAsFlattenedList().OfType<AnnotationLayer>().ToList();                    
                    List<ImageMosaicSubLayer> mozaicLayers = map.GetLayersAsFlattenedList().OfType<ImageMosaicSubLayer>().ToList();

                    // listes des groupes et groupes de rasters
                    List<MosaicLayer>  MosaicsLayers = MapView.Active.Map.GetLayersAsFlattenedList().OfType<MosaicLayer>().ToList();
                    List<GroupLayer> groupLayers = map.GetLayersAsFlattenedList().OfType<GroupLayer>().ToList();

                    // pour toutes les couches on vérifie s'il faut, l'afficher, la rendre sélectionnable et/ou éditable
                    // cas des couches carto
                    foreach (var fl in listLayers)
                    {
                        SelectEditRule r = new SelectEditRule();
                        // le parent de la couche est un groupe
                        if (groupLayers.Contains(fl.Parent as GroupLayer))
                        {
                            var pgl = fl.Parent as GroupLayer;
                            // le parent du groupe est un groupe
                            if (groupLayers.Contains(pgl.Parent as GroupLayer))
                            {
                                if ((pgl.Parent as GroupLayer).IsVisible is true)
                                    if ((fl.Parent as GroupLayer).IsVisible is true)
                                        r.SrcDisplay = fl.IsVisible;
                                    else
                                        r.SrcDisplay = (fl.Parent as GroupLayer).IsVisible;
                                else
                                    r.SrcDisplay = (pgl.Parent as GroupLayer).IsVisible;
                            }
                            // groupe à la racine de la carte
                            else
                            {
                                if ((fl.Parent as GroupLayer).IsVisible is true)
                                    r.SrcDisplay = fl.IsVisible;
                                else
                                    r.SrcDisplay = (fl.Parent as GroupLayer).IsVisible;
                            }
                            r.SrcGroupLayer = (fl.Parent as GroupLayer).ToString(); //.Name;
                        }
                        // le parent de la couche est un groupe Raster
                        else if (MosaicsLayers.Contains(fl.Parent as MosaicLayer))
                        {
                            var pgl = fl.Parent as MosaicLayer;
                            // le parent du groupe est un groupe
                            if (MosaicsLayers.Contains(pgl.Parent as MosaicLayer))
                            {
                                if ((pgl.Parent as MosaicLayer).IsVisible is true)
                                    if ((fl.Parent as MosaicLayer).IsVisible is true)
                                        r.SrcDisplay = fl.IsVisible;
                                    else
                                        r.SrcDisplay = (fl.Parent as MosaicLayer).IsVisible;
                                else
                                    r.SrcDisplay = (pgl.Parent as MosaicLayer).IsVisible;
                            }
                            // groupe à la racine de la carte
                            else
                            {
                                if ((fl.Parent as MosaicLayer).IsVisible is true)
                                    r.SrcDisplay = fl.IsVisible;
                                else
                                    r.SrcDisplay = (fl.Parent as MosaicLayer).IsVisible;
                            }
                            r.SrcMosaicLayer = (fl.Parent as MosaicLayer).ToString();//.Name;
                        }
                        // couche simple à la racine de la carte
                        else
                        {
                            r.SrcDisplay = fl.IsVisible;
                        }
                        r.SrcEditable = fl.IsEditable;
                        r.SrcSelectable = fl.IsSelectable;
                        r.SrcLayer = fl.Name;
                        r.SrcMapName = map.Name;
                        r.SrcRuleName = worksheetName;
                        List_Rules.Add(r);
                    }
                    //
                    foreach (var fl in mozaicLayers)
                    {
                        SelectEditRule r = new SelectEditRule();
                        // le parent de la couche est un groupe
                        if (groupLayers.Contains(fl.Parent as GroupLayer))
                        {
                            var pgl = fl.Parent as GroupLayer;
                            // le parent du groupe est un groupe
                            if (groupLayers.Contains(pgl.Parent as GroupLayer))
                            {
                                if ((pgl.Parent as GroupLayer).IsVisible is true)
                                    if ((fl.Parent as GroupLayer).IsVisible is true)
                                        r.SrcDisplay = fl.IsVisible;
                                    else
                                        r.SrcDisplay = (fl.Parent as GroupLayer).IsVisible;
                                else
                                    r.SrcDisplay = (pgl.Parent as GroupLayer).IsVisible;
                            }
                            // groupe à la racine de la carte
                            else
                            {
                                if ((fl.Parent as GroupLayer).IsVisible is true)
                                    r.SrcDisplay = fl.IsVisible;
                                else
                                    r.SrcDisplay = (fl.Parent as GroupLayer).IsVisible;
                            }
                            r.SrcGroupLayer = (fl.Parent as GroupLayer).ToString(); //.Name;
                        }
                        // le parent de la couche est un groupe Raster
                        else if (MosaicsLayers.Contains(fl.Parent as MosaicLayer))
                        {
                            var pgl = fl.Parent as MosaicLayer;
                            // le parent du groupe est un groupe
                            if (MosaicsLayers.Contains(pgl.Parent as MosaicLayer))
                            {
                                if ((pgl.Parent as MosaicLayer).IsVisible is true)
                                    if ((fl.Parent as MosaicLayer).IsVisible is true)
                                        r.SrcDisplay = fl.IsVisible;
                                    else
                                        r.SrcDisplay = (fl.Parent as MosaicLayer).IsVisible;
                                else
                                    r.SrcDisplay = (pgl.Parent as MosaicLayer).IsVisible;
                            }
                            // groupe à la racine de la carte
                            else
                            {
                                if ((fl.Parent as MosaicLayer).IsVisible is true)
                                    r.SrcDisplay = fl.IsVisible;
                                else
                                    r.SrcDisplay = (fl.Parent as MosaicLayer).IsVisible;
                            }
                            r.SrcMosaicLayer = (fl.Parent as MosaicLayer).ToString();//.Name;
                        }
                        // couche simple à la racine de la carte
                        else
                        {
                            r.SrcDisplay = fl.IsVisible;
                        }
                        r.SrcLayer = fl.Name;
                        r.SrcMapName = map.Name;
                        r.SrcRuleName = worksheetName;
                        List_Rules.Add(r);
                    }

                   
                    // idem pour les annotations
                    foreach (var al in listAnnotations)
                    {
                        SelectEditRule r = new SelectEditRule();

                        // le parent de la couche est un groupe
                        if (groupLayers.Contains(al.Parent as GroupLayer))
                        {
                            var pgl = al.Parent as GroupLayer;
                            // le parent du groupe est un groupe
                            if (groupLayers.Contains(pgl.Parent as GroupLayer))
                            {
                                if ((pgl.Parent as GroupLayer).IsVisible is true)
                                    if ((al.Parent as GroupLayer).IsVisible is true)
                                        r.SrcDisplay = al.IsVisible;
                                    else
                                        r.SrcDisplay = (al.Parent as GroupLayer).IsVisible;
                                else
                                    r.SrcDisplay = (pgl.Parent as GroupLayer).IsVisible;
                            }
                            // groupe à la racine de la carte
                            else
                            {
                                if ((al.Parent as GroupLayer).IsVisible is true)
                                    r.SrcDisplay = al.IsVisible;
                                else
                                    r.SrcDisplay = (al.Parent as GroupLayer).IsVisible;
                            }
                            r.SrcGroupLayer = (al.Parent as GroupLayer).Name;
                        }
                        // couche simple à la racine de la carte
                        else
                        {
                            r.SrcDisplay = al.IsVisible;
                        }

                        r.SrcEditable = al.IsEditable;
                        r.SrcSelectable = al.IsSelectable;
                        r.SrcLayer = al.Name;
                        r.SrcMapName = map.Name;
                        r.SrcRuleName = worksheetName;
                        List_Rules.Add(r);
                    }

                    // Enregistrement du fichier
                    return WriteExcelWorksheet(map, excelConfigFileURI, worksheetName, List_Rules);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Warning", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            });
        }

        /// <summary>
        /// Excel file for visibility and map selected
        /// </summary>
        /// <param name="map"> Map to assign</param>
        /// <param name="existingFileURI">Name of Excel file to be modified</param>
        /// <param name="worksheetName"> name of tab to be created/modified</param>
        /// <param name="headRowNumber">the header row number for column names </param>
        /// <returns>result </returns>
        public bool WriteExcelWorksheet(Map map, string existingFileURI, string worksheetName, List<SelectEditRule> selectEditRules, int headRowNumber = 1)
        {
            // tests d'existence
            if (selectEditRules is null)
                throw new ArgumentNullException(nameof(selectEditRules));

            if (!File.Exists(existingFileURI))
                throw new FileNotFoundException(string.Format("The file <{0}> doesn't exist", existingFileURI));

            try
            {
                var existingFile = new FileInfo(existingFileURI);

                // If you use EPPlus in a noncommercial context
                // according to the Polyform Noncommercial license:
                ExcelPackage.License.SetNonCommercialOrganization("IGN");

                using (ExcelPackage xlPackage = new ExcelPackage(existingFile))
                {
                    // Si le fichier est déjà ouvert, alerte l'opérateur
                    if (fileIsOpen(xlPackage.File.FullName))
                    {
                        MessageBox.Show(existingFile + AlreadyExist, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }

                    // Vérifie l'existence de l'onglet pour le modifier
                    ExcelWorksheet selectedWorkSheet = xlPackage.Workbook.Worksheets.FirstOrDefault(x => x.Name.Equals(worksheetName));
                    if (selectedWorkSheet != null)
                    {
                        // alerte l'opérateur de l'existence de l'onglet
                        var resultu = MessageBox.Show(VisiblityExist, "Warning", MessageBoxButton.YesNo);

                        // on écrase l'onglet existant.
                        if (resultu == MessageBoxResult.Yes)
                            selectedWorkSheet.DeleteRow(1, selectedWorkSheet.Dimension.End.Row);

                        // on sort
                        else return false;
                    }
                    // ou le créer
                    else
                    {
                        selectedWorkSheet = xlPackage.Workbook.Worksheets.Add(worksheetName);
                        // ajoute l'option dans le fichier gestionnaire
                        ExcelWorksheet headerWorkSheet = xlPackage.Workbook.Worksheets.FirstOrDefault(x => x.Name.Equals("gestionnaire") || x.Name.Equals("manager"));
                        var row_nb = headerWorkSheet.Dimension.End.Row;
                        headerWorkSheet.Cells.SetCellValue(row_nb, 0, map.Name);
                        headerWorkSheet.Cells.SetCellValue(row_nb, 1, worksheetName);
                    }

                    // écrit tous les choix de couches dans l'onglet "worksheetName"
                    selectedWorkSheet.Cells.SetCellValue(0, 0, SelectEditRule.map_ALIAS);
                    selectedWorkSheet.Cells.SetCellValue(0, 1, SelectEditRule.name_ALIAS);
                    selectedWorkSheet.Cells.SetCellValue(0, 2, SelectEditRule.folder_ALIAS);
                    selectedWorkSheet.Cells.SetCellValue(0, 3, SelectEditRule.layer_ALIAS);
                    selectedWorkSheet.Cells.SetCellValue(0, 4, SelectEditRule.display_ALIAS);
                    selectedWorkSheet.Cells.SetCellValue(0, 5, SelectEditRule.selection_ALIAS);
                    selectedWorkSheet.Cells.SetCellValue(0, 6, SelectEditRule.edition_ALIAS);
                    selectedWorkSheet.Rows[1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    selectedWorkSheet.Rows[1].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#FFD3D3D3"));

                    // Initiate a conditional formatting object                    
                    foreach (var selectEditRule in selectEditRules)
                    {
                        int i = 1;
                        foreach (var r in selectEditRules)
                        {
                            if (r.SrcDisplay || r.SrcSelectable || r.SrcEditable)
                            {
                                selectedWorkSheet.Cells.SetCellValue(i, 0, r.SrcMapName);
                                selectedWorkSheet.Cells.SetCellValue(i, 1, r.SrcRuleName);
                                if (r.SrcGroupLayer !="" && r.SrcGroupLayer is not null)
                                    selectedWorkSheet.Cells.SetCellValue(i, 2, r.SrcGroupLayer);
                                if (r.SrcMosaicLayer != "" && r.SrcMosaicLayer is not null)
                                    selectedWorkSheet.Cells.SetCellValue(i, 2, r.SrcMosaicLayer);
                                selectedWorkSheet.Cells.SetCellValue(i, 3, r.SrcLayer);
                                selectedWorkSheet.Cells.SetCellValue(i, 4, r.SrcDisplay);
                                selectedWorkSheet.Cells.SetCellValue(i, 5, r.SrcSelectable);
                                selectedWorkSheet.Cells.SetCellValue(i, 6, r.SrcEditable);
                                i++;
                            }
                        }
                    }
                    xlPackage.Save();
                }
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Problem reading Excel config file. Message error: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// test if a file is already open
        /// </summary>
        /// <param name="file"> file to test </param> 
        /// <returns>test result</returns>

        private bool fileIsOpen(string file)
        {
            try
            {
                FileStream fs;
                fs = File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                if (fs != null)
                {
                    fs.Close();
                    return false;
                }
                else
                    return true;
            }
            catch { return true; }
        }

        #endregion Fonctions
    }
}
