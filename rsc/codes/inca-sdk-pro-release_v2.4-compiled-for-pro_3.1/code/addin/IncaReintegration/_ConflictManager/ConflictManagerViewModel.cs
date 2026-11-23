using ActiproSoftware.Windows.Controls;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.Mapping;
using ArcGIS.Core.Data.UtilityNetwork.Trace;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Internal.CIM;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.DataReviewer;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Extensions;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Internal.Mapping;
using ArcGIS.Desktop.Internal.Mapping.Ribbon;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ArcGIS.Desktop.Workflow.Models.ConfigItems;
using IGN.Inca.ArcGISPro.SDK.BLL.Desktop;
using IGN.Inca.ArcGISPro.SDK.Commons;
using IGN.Inca.ArcGISPro.SDK.Internationalization;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.Logging;
using OfficeOpenXml.Style.Dxf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Threading;


using Button = ArcGIS.Desktop.Framework.Contracts.Button;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using QueryFilter = ArcGIS.Core.Data.QueryFilter;



namespace IncaReintegration._ConflictManager
{
    internal class ConflictManagerViewModel : DockPane, INotifyPropertyChanged
    {
        private const string _dockPaneID = "ConflictManager_Dockpane";

        //variables et fonctions lancées à l'initialisation du dockpane d'update.
        protected ConflictManagerViewModel()
        {
            // initialize "inca_logger_sdk" logger
            LoggerUtils.GetInstance().InitializeLogger();

            // get the dictionary of the current UI language ("CurrentUICulture") using the Culture Resources Manager
            Translator = CultureResourcesManager.GetInstance().dictLanguage;

            Resolutions = new ObservableCollection<string>
            {
                "Version initiale",
                "Version SDE",
                "Version GDB locale"
            };
        }   

        protected override void OnShow(bool isVisible)
        {
            base.OnShow(isVisible);

            if (isVisible && MapView.Active != null)
            {
                _ = GetTotalConflict(false);
            }
        }



        #region Properties
        /// <summary>
        /// Culture translator
        /// </summary>
        private ResourceSet Translator { get; set; }

        /// <summary>
        /// chemin du projet ArcGIS Pro
        /// </summary>
        public static string ProjectPath;

        /// <summary>
        /// nom du projet ArcGIS Pro
        /// </summary>
        public static string ProjectName;

        private ObservableCollection<TableItem> _tables;
        public ObservableCollection<TableItem> Tables
        {
            get { return _tables; }
            set
            {
                _tables = value;
                NotifyPropertyChanged(new PropertyChangedEventArgs("Tables"));
            }
        }

        public class TableItem
        {
            public string Name { get; set; }
            public int Count { get; set; }
            public bool IsLowCount => Count < 2;
            public bool ToResolve { get; set; }
            public string Display => $"{Name} ({Count})";
        }

        private TableItem _selectedTable;
        public TableItem SelectedTable
        {
            get { return _selectedTable; }
            set
            {
                Objectids = null;
                SelectedObjectid = null;
                IsTableVisible = false;
                
                SetProperty(ref _selectedTable, value, () => SelectedTable);
            }
        }

        private ObservableCollection<ObjectidItem> _objectids;
        public ObservableCollection<ObjectidItem> Objectids
        {
            get { return _objectids; }
            set
            {
                _objectids = value;
                NotifyPropertyChanged(new PropertyChangedEventArgs("Objectids"));
            }
        }

        public class ObjectidItem
        {
            public string Oid { get; set; }
            public bool ToResolve { get; set; }
            public string Display => $"{Oid}";
        }

        private ObjectidItem _selectedObjectid;
        public ObjectidItem SelectedObjectid
        {
            get { return _selectedObjectid; }
            set
            {
                //SetProperty(ref _selectedObjectid, value, () => SelectedObjectid);
                
                if (SetProperty(ref _selectedObjectid, value) && value != null)
                {                   
                    if (long.TryParse(value.Oid, out long oid))
                        _ = CompareObjectId(oid);
                        Debug.WriteLine($"oid = {oid}");
                    IsTableVisible = true;
                    Task task = GetSelectedResolution(SelectedTable.Name,SelectedObjectid.Oid);
                }
                
            }
        }

        private ObservableCollection<string> _resolutions;
        public ObservableCollection<string> Resolutions
        {
            get { return _resolutions; }
            set
            {
                _resolutions = value;
                NotifyPropertyChanged(new PropertyChangedEventArgs("Resolutions"));
            }
        }

        private string _selectedResolution;
        public string SelectedResolution
        {
            get { return _selectedResolution; }
            set
            {
                SetProperty(ref _selectedResolution, value, () => SelectedResolution);
            }
        }

        public class ComparisonRow
        {
            public string FieldName { get; set; }
            public string Value1 { get; set; }
            public string Value2 { get; set; }
            public string Value3 { get; set; }
        }

        private ObservableCollection<ComparisonRow> _comparisonRows =
        new ObservableCollection<ComparisonRow>();

        public ObservableCollection<ComparisonRow> ComparisonRows
        {
            get => _comparisonRows;
            set => SetProperty(ref _comparisonRows, value);
        }

        private bool _isTableVisible;
        public bool IsTableVisible
        {
            get => _isTableVisible;
            set => SetProperty(ref _isTableVisible, value);
        }

        private string _totalConflict;
        public string TotalConflict
        {
            get { return _totalConflict; }
            set
            {
                SetProperty(ref _totalConflict, value, () => TotalConflict);
            }
        }

        private string _conflictType;
        public string ConflictType
        {
            get { return _conflictType; }
            set
            {
                SetProperty(ref _conflictType, value, () => ConflictType);
            }
        }

        private string _progressionText;
        public string ProgressionText
        {
            get { return _progressionText; }
            set
            {
                SetProperty(ref _progressionText, value, () => ProgressionText);
            }
        }


        /// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ START ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        ///     One-Way Data Binding Properties, used specially for internationalization of the UI
        /// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        /// 
        public string ChooseClass { get { return Translator.GetString("ChooseClass"); } }      
        public string ChooseObjectid { get { return Translator.GetString("ChooseObjectid"); } }
        public string ChooseResolution { get { return Translator.GetString("ChooseResolution"); } }
        public string SaveResolutionTxt { get { return Translator.GetString("SaveResolutionTxt"); } }
        public string FinalSaveTxt { get { return Translator.GetString("FinalSaveTxt"); } }
        public string ZoomButtonTxt { get { return Translator.GetString("ZoomButtonTxt"); } }
        public string BrightButtonTxt { get { return Translator.GetString("BrightButtonTxt"); } }
        public string SavingTxt { get { return Translator.GetString("SavingTxt"); } }
        /// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ END ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~


        #endregion

        #region Commands

        // La région Commands fait le lien entre l'IHM et les fonctions du script.
        // Les commandes sont liées à des boutons ArcGIS Pro dans IncaUpdateView.xaml et lancent des fonctions quand on clique sur ces boutons.
        /// <summary>      

        /// Command to browse Origin gdb file
        /// </summary>
        private ICommand _finalSaveCommand;
        public ICommand FinalSaveCommand
        {
            get { return _finalSaveCommand ?? (_finalSaveCommand = new RelayCommand(() => FinalSave())); }
        }

        /// Command to save conflict resolution
        /// </summary>
        private ICommand _saveCommand;
        public ICommand SaveCommand
        {
            get { return _saveCommand ?? (_saveCommand = new RelayCommand(() => SaveResolution())); }
        }

        /// Command to zoom to a conflict
        /// </summary>
        private ICommand _zoomCommand;
        public ICommand ZoomCommand
        {
            get { return _zoomCommand ?? (_zoomCommand = new RelayCommand(() => Zoom())); }
        }

        /// Command to flash a conflict
        /// </summary>
        private ICommand _flashCommand;
        public ICommand FlashCommand
        {
            get { return _flashCommand ?? (_flashCommand = new RelayCommand(() => Flash())); }
        }

        /// <summary>
        /// Button implementation to Fill the edit queue with a table
        /// commande Remplir la queue d'édition avec une table
        /// </summary>
        private ICommand _CmdFillWithTable;
        public ICommand CmdFillWithTable
        {
            get { return _CmdFillWithTable ?? (_CmdFillWithTable = new RelayCommand(() => FillWithTable())); }
        }

        /// <summary>
        /// Button implementation to Fill the edit queue with a table
        /// commande Remplir la queue d'édition avec une table
        /// </summary>
        private ICommand _CmdFillWithObjectid;
        public ICommand CmdFillWithObjectid
        {
            get { return _CmdFillWithObjectid ?? (_CmdFillWithObjectid = new RelayCommand(() => FillWithObjectid())); }
        }

        /// <summary>
        /// Button implementation to Fill the edit queue with a table
        /// commande Remplir la queue d'édition avec une table
        /// </summary>      
        private ICommand _CmdFillWithResolution;
        public ICommand CmdFillWithResolution
        {           
          get { return _CmdFillWithResolution ?? (_CmdFillWithResolution = new RelayCommand(() => FillWithResolution())); }           
        }

        #endregion

        #region Interaction with the "IncaProSDK" Library

        #endregion

        #region Fonctions appelees par la fonction Update
        private async Task FillWithTable()
        {
            await QueuedTask.Run(() =>
            {
                Dictionary<string, int> classesCount = new Dictionary<string, int>();
                Dictionary<string, bool> classesToResolve = new Dictionary<string, bool>();

                var conflictLayer = GetLayer("conflict_point");
                if (conflictLayer == null)
                    return;

                using (Table conflictTable = conflictLayer.GetTable())
                {
                    var qf = new QueryFilter
                    {
                        WhereClause = "resolution IS NULL OR resolution IN ('SDE', 'INI', 'GDB')",
                        SubFields = "classe"
                    };

                    using (RowCursor cursor = conflictTable.Search(qf, false))
                    {
                        while (cursor.MoveNext())
                        {
                            using (Row row = cursor.Current)
                            {
                                var value = row["classe"]?.ToString();
                                if (string.IsNullOrEmpty(value))
                                    continue;

                                if (classesCount.ContainsKey(value))
                                    classesCount[value]++;
                                else
                                    classesCount[value] = 1;
                            }
                        }
                    }

                    var qf2 = new QueryFilter
                    {
                        WhereClause = "resolution IS NULL",
                        SubFields = "classe"
                    };

                    using (RowCursor cursor = conflictTable.Search(qf2, false))
                    {
                        while (cursor.MoveNext())
                        {
                            using (Row row = cursor.Current)
                            {
                                var value = row["classe"]?.ToString();
                                if (string.IsNullOrEmpty(value))
                                    continue;
                                Debug.WriteLine("Cette classe doit etre traitée :" + value);
                                classesToResolve[value] = true;
                            }
                        }
                    }
                }

                // Remplissage de la liste
                Tables = new ObservableCollection<TableItem>(
                    classesCount
                        .OrderByDescending(kvp => kvp.Value)
                        .Select(kvp => new TableItem
                        {
                            Name = kvp.Key,
                            Count = kvp.Value,
                            ToResolve = classesToResolve.ContainsKey(kvp.Key)
                        })
                );
            });
        }

        private async Task FillWithObjectid()
        {
            await QueuedTask.Run(() =>
            {
                HashSet<string> objectids = new HashSet<string>();
                HashSet<string> objectidsToResolve = new HashSet<string>();

                // Trouver la couche dans la carte active
                var conflictLayer = GetLayer("conflict_point");

                if (conflictLayer == null || SelectedTable == null)
                    return; // couche absente
                
                // Accéder à la table
                using (Table conflictTable = conflictLayer.GetTable())
                {
                    // Construire le filtre
                    var qf = new QueryFilter
                    {
                        WhereClause = $"classe = '{SelectedTable.Name}' AND (resolution IS NULL OR resolution IN ('SDE', 'INI', 'GDB'))",
                        SubFields = "ORIGIN_OID"
                    };

                    // Parcours des lignes
                    using (RowCursor cursor = conflictTable.Search(qf, false))
                    {
                        while (cursor.MoveNext())
                        {
                            using (Row row = cursor.Current)
                            {
                                var value = row["ORIGIN_OID"];
                                if (value != null)
                                    objectids.Add(value.ToString());
                            }
                        }
                    }

                    // Construire le filtre
                    var qf2 = new QueryFilter
                    {
                        WhereClause = $"classe = '{SelectedTable.Name}' AND resolution IS NULL",
                        SubFields = "ORIGIN_OID"
                    };

                    // Parcours des lignes
                    using (RowCursor cursor = conflictTable.Search(qf2, false))
                    {
                        while (cursor.MoveNext())
                        {
                            using (Row row = cursor.Current)
                            {
                                var value = row["ORIGIN_OID"];
                                if (value != null)
                                    objectidsToResolve.Add(value.ToString());
                            }
                        }
                    }
                }
                Objectids = new ObservableCollection<ObjectidItem>();
                foreach (var temp in objectids)
                {
                    if (objectidsToResolve.Contains(temp))
                    {
                        Objectids.Add(new ObjectidItem
                        {
                            Oid = temp,
                            ToResolve = true
                        });
                    }
                    else
                    {
                        Objectids.Add(new ObjectidItem
                        {
                            Oid = temp,
                            ToResolve = false
                        });
                    }
                } 
                    
            });
        }

        private async Task FillWithResolution()
        {
            await QueuedTask.Run( () =>
            {

                var resolutions = new List<string>();
                resolutions.Add("Version initiale");
                resolutions.Add("Version SDE");
                resolutions.Add("Version GDB locale");
                Resolutions = new ObservableCollection<string>();
                foreach (var temp in resolutions) Resolutions.Add(temp);

            });
        }
        //Fonction qui va permettre de remplir le tableau de comparaison des attributs en conflit
        private async Task CompareObjectId(long objectId)
        {
            await QueuedTask.Run(() =>
            {
                string champs = "";
                // Trouver la couche dans la carte active
                var conflictLayer = GetLayer("conflict_point");

                if (conflictLayer == null || SelectedTable == null || SelectedObjectid == null)
                    return; // couche absente

                // Récupérer les champs en conflit pour l'objectid sélectionné
                using (Table conflictTable = conflictLayer.GetTable())
                {
                    var qf = new QueryFilter
                    {
                        WhereClause = $"classe = '{SelectedTable.Name}' AND ORIGIN_OID = {SelectedObjectid.Oid}",
                        SubFields = "champs_modifies, type_conflit"
                    };

                    using (RowCursor cursor = conflictTable.Search(qf, false))
                    {
                        while (cursor.MoveNext())
                        {
                            using (Row row = cursor.Current)
                            {
                                var value = row["champs_modifies"];
                                if (value != null)
                                    champs = value.ToString();
                                var value_conflict_type = row["type_conflit"];
                                if (value_conflict_type != null)
                                    ConflictType = "Type de conflit : " + value_conflict_type.ToString();
                            }
                        }
                    }
                }

                List<string> fieldsToCompare = champs.Trim('[', ']').Split(',').Select(s => s.Trim(' ', '\'')).ToList();

                                   
                var layer1 = MapView.Active.Map.FindLayers(SelectedTable.Name + "_gdb")
                    .FirstOrDefault() as BasicFeatureLayer;

                var layer2 = MapView.Active.Map.FindLayers(SelectedTable.Name + "_sde")
                    .FirstOrDefault() as BasicFeatureLayer;

                var layer3 = MapView.Active.Map.FindLayers(SelectedTable.Name + "_ini")
                    .FirstOrDefault() as BasicFeatureLayer;

                if (layer1 == null || layer2 == null || layer3 == null)
                {
                    Debug.WriteLine("Couches introuvables");
                    return;
                }

                var tempRows = new List<ComparisonRow>();

                foreach (var field in fieldsToCompare)
                {
                    var value1 = GetFieldValue(layer1, objectId, field);
                    var value2 = GetFieldValue(layer2, objectId, field);
                    var value3 = GetFieldValue(layer3, objectId, field);
                    tempRows.Add(new ComparisonRow
                    {
                        FieldName = field,
                        Value1 = value1,
                        Value2 = value2,
                        Value3 = value3
                    });
                }
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    ComparisonRows.Clear();
                    foreach (var row in tempRows)
                        ComparisonRows.Add(row);
                });
                
                 
            });
        }

        //récupérer la valeur des champ en conflit pour un objet d'un FeatureLayer
        private string GetFieldValue(BasicFeatureLayer layer, long objectId, string fieldName)
        {
            using (var table = layer.GetTable())
            {
                var queryFilter = new QueryFilter
                {
                    WhereClause = $"origin_oid = {objectId}"
                };

                using (var cursor = table.Search(queryFilter, false))
                {
                    if (!cursor.MoveNext())
                        return "—";

                    using (var row = cursor.Current)
                    {
                        return row[fieldName]?.ToString() ?? "NULL";
                    }
                }
            }
        }

        //récupérer la valeur des champ en conflit pour un objet d'un FeatureLayer
        private async Task GetTotalConflict(bool final)
        {
            await QueuedTask.Run(async() =>
            {
                Debug.WriteLine("tentative récupération conflict point");
                // Trouver la couche dans la carte active
                var conflictLayer = GetLayer("conflict_point");
                Debug.WriteLine("récupération conflict point OK");
                if (conflictLayer == null)
                    TotalConflict = "KO Il manque la couche conflict_point"; // couche absente

                var count_total = 0; // nombre de conflits total
                var count_tocheck = 0; // nombre de conflits à traiter
                var count_saved = 0; // nombre de conflits sauvegardés définitivement
                // Accéder à la table
                using (Table conflictTable = conflictLayer.GetTable())
                {
                    // Construire le filtre
                    var qf_total = new QueryFilter
                    {
                        WhereClause = "1 = 1",
                    };

                    // Parcours des lignes
                    using (RowCursor cursor = conflictTable.Search(qf_total, false))
                    {
                        while (cursor.MoveNext())
                        {
                            count_total += 1;
                        }
                    }

                    // Construire le filtre
                    var qf_tocheck = new QueryFilter
                    {
                        WhereClause = "resolution IS NULL",
                    };

                    // Parcours des lignes
                    using (RowCursor cursor = conflictTable.Search(qf_tocheck, false))
                    {
                        while (cursor.MoveNext())
                        {
                            count_tocheck += 1;
                        }
                    }

                    // Construire le filtre
                    var qf_saved = new QueryFilter
                    {
                        WhereClause = "resolution IS NOT NULL AND resolution NOT IN ('SDE', 'INI', 'GDB')",
                    };

                    // Parcours des lignes
                    using (RowCursor cursor = conflictTable.Search(qf_saved, false))
                    {
                        while (cursor.MoveNext())
                        {
                            count_saved += 1;
                        }
                    }
                }
                TotalConflict = $"Nombre Total de Conflits : {count_total.ToString()} / A traiter : {count_tocheck.ToString()} / " +
                $"Traités définitiment: {count_saved.ToString()}";

                if(final && count_total == count_saved)
                {                  
                    string editedGdbPath = getGdbEditedPath(GetConflictGdbFolder());
                    var (succes, errorMessage) = await _Update.IncaUpdateViewModel.ChangeEditionGdb(editedGdbPath, GetGdbFolder());
                    if (succes)
                    {
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"Tous les conflits ont été définitivement traités. Vous pouvez fermer la fenêtre " +
                            $"de résolution des conflits et travailler avec la gdb d'update, accessible ici : {editedGdbPath}");
                    }
                    else
                    {
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"Tous les conflits ont été définitivement traités. ATTENTION " +
                            $"erreur lors du remplacement de l'ancienne gdb d'édition remplacée par la " +
                            $"gdb d'update : {errorMessage} " +
                            $"Si vous avez déplacé l'ancienne gdb d'édition copiez la à son emplacement d'origine, puis relancez l'enregistrement définitif.");
                    }
                    
                }
            });
        }
        private async Task GetSelectedResolution(string table, string objectid)
        {
            string resolution = null;
            
            await QueuedTask.Run(() =>
            {
                Debug.WriteLine("tentative récupération conflict point");
                // Trouver la couche dans la carte active
                var conflictLayer = GetLayer("conflict_point");
                Debug.WriteLine("récupération conflict point OK");
                if (conflictLayer == null)
                    TotalConflict = "KO Il manque la couche conflict_point"; // couche absente
                SelectedResolution = null;

                Dictionary<string, string> resolutions = new Dictionary<string, string>();
                resolutions.Add("INI", "Version initiale");
                resolutions.Add("SDE", "Version SDE");
                resolutions.Add("GDB", "Version GDB locale");
                
                // Accéder à la table
                using (Table conflictTable = conflictLayer.GetTable())
                {                  

                    // Construire le filtre
                    var qf = new QueryFilter
                    {
                        WhereClause = $"classe = '{SelectedTable.Name}' AND ORIGIN_OID = {SelectedObjectid.Oid} AND resolution IS NOT NULL",
                    };

                    // Parcours des lignes
                    using (RowCursor cursor = conflictTable.Search(qf, false))
                    {
                        while (cursor.MoveNext())
                        {
                            using (Row row = cursor.Current)
                            {
                                var value = row["resolution"];
                                if (value != null)
                                    resolution = resolutions[value.ToString()];
                            }
                        }
                    }
                    
                }
            });
            SelectedResolution = resolution;                
            Debug.WriteLine("SelectedResolution = " + SelectedResolution);
        }


        //Sauvegarde de la résolution d'un conflit
        private async Task SaveResolution()
        {
            Debug.WriteLine("Lancement de la sauvegarde");
            if (SelectedTable == null)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Sélectionnez une classe d'entités");
                return;
            }
            else if (SelectedObjectid == null)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Sélectionnez un OBJECTID");
                return;
            }
            else if (SelectedResolution == null)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Sélectionnez une résolution");
                return;
            }
            else
            {
                await QueuedTask.Run(() =>
                {
                    var layer = GetLayer("conflict_point");

                    if(layer == null)
                    {
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Il manque la couche conflict_point");
                        return;
                    }

                    Dictionary<string, string> resolutions = new Dictionary<string, string>();
                    resolutions.Add("Version initiale", "INI");
                    resolutions.Add("Version SDE", "SDE");
                    resolutions.Add("Version GDB locale", "GDB");

                    using (var table = layer.GetTable())
                    {                      
                        var queryFilter = new QueryFilter
                        {
                            WhereClause = $"ORIGIN_OID = {SelectedObjectid.Oid}"
                        };

                        using (var cursor = table.Search(queryFilter, false))
                        {
                            if (!cursor.MoveNext())
                                return; // aucune ligne trouvée

                            using (var row = cursor.Current)
                            {
                                // Opération d'édition
                                var editOp = new EditOperation
                                {
                                    Name = "Mise à jour Résolution"
                                };

                                editOp.Modify(row, new Dictionary<string, object>
                                {
                                    { "resolution", resolutions[SelectedResolution] }
                                });

                                // Exécuter l'opération + enregistrer les changements et fermer la session d'édition
                                if (editOp.Execute())
                                {
                                    Project.Current.SaveEditsAsync();
                                }
                            }
                        }
                    }
                });
                await GetTotalConflict(false);
            }
        }

        //Mise à jour de la résolution d'une liste de conflits après la validation finale de conflits
        private async Task UpdateResolutionAfterFinalSave(string classe, List<int> objectIds, string resolution)
        {
            
            await QueuedTask.Run(() =>
            {
                var layer = GetLayer("conflict_point");

                if (layer == null)
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Il manque la couche conflict_point");
                    return;
                }

                using (var table = layer.GetTable())
                {
                    var queryFilter = new QueryFilter
                    {
                        WhereClause = $"CLASSE = '{classe}' AND ORIGIN_OID IN ({string.Join(",", objectIds)})"
                    };

                    using (var cursor = table.Search(queryFilter, false))
                    {
                        if (!cursor.MoveNext())
                            return; // aucune ligne trouvée

                        using (var row = cursor.Current)
                        {
                            // Opération d'édition
                            var editOp = new EditOperation
                            {
                                Name = "Mise à jour Résolution"
                            };

                            editOp.Modify(row, new Dictionary<string, object>
                            {
                                { "resolution", resolution }
                            });

                            // Exécuter l'opération + enregistrer les changements et fermer la session d'édition
                            if (editOp.Execute())
                            {
                                Project.Current.SaveEditsAsync();
                            }
                        }
                    }
                }
            });
            await GetTotalConflict(false);           
        }

        private string GetGdbFolder()
        {
            var layer = GetLayer("conflict_point");

            if (layer == null)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Il manque la couche conflict_point");
                return null;
            }

            return QueuedTask.Run(() =>
            {
                var table = layer.GetTable();
                var datastore = table.GetDatastore();
                var path = datastore.GetPath();

                if (path == null)
                    return null;

                // Dossier parent de la GDB
                var parentFolder = System.IO.Path.GetDirectoryName(path.LocalPath);

                if (string.IsNullOrEmpty(parentFolder) || !System.IO.Directory.Exists(parentFolder))
                    return null;

                // Recherche des dossiers commençant par "update"
                var updateFolders = System.IO.Directory.GetDirectories(parentFolder, "update*", System.IO.SearchOption.TopDirectoryOnly);

                // Si au moins un trouvé → on prend le premier
                if (updateFolders.Length > 0)
                {
                    return updateFolders[0]; // chemin complet
                }

                return null;
            }).Result;
        }
        private string GetConflictGdbFolder()
        {
            var layer = GetLayer("conflict_point");

            if (layer == null)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Il manque la couche conflict_point");
                return null;
            }

            return QueuedTask.Run(() =>
            {
                var table = layer.GetTable();
                var datastore = table.GetDatastore();
                var path = datastore.GetPath();

                if (path == null)
                    return null;

                return path.LocalPath;
            }).Result;
        }

        //Mise à jour de la résolution des conflits résolus en SDE
        private async Task UpdateSDEResolutionAfterFinalSave()
        {

            await QueuedTask.Run(() =>
            {
                var layer = GetLayer("conflict_point");

                if (layer == null)
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Il manque la couche conflict_point");
                    return;
                }

                using (var table = layer.GetTable())
                {
                    var queryFilter = new QueryFilter
                    {
                        WhereClause = $"RESOLUTION = 'SDE'"
                    };

                    using (var cursor = table.Search(queryFilter, false))
                    {
                        while (cursor.MoveNext())
                        {
                            using (var row = cursor.Current)
                            {
                                // Opération d'édition
                                var editOp = new EditOperation
                                {
                                    Name = "Mise à jour Résolution"
                                };

                                editOp.Modify(row, new Dictionary<string, object>
                                {
                                    { "resolution", "SDE - SAVED" }
                                });

                                // Exécuter l'opération + enregistrer les changements et fermer la session d'édition
                                editOp.Execute();                               
                            }
                        }
                    }
                }
                // sauvegarde
                if (Project.Current.HasEdits)
                    Project.Current.SaveEditsAsync();
            });
            await GetTotalConflict(true);
        }


        private async Task FinalSave()
        {
            Debug.WriteLine("START FINAL SAVE");

            ProgressionText = SavingTxt;

            var gdbPath = GetGdbFolder();

            await QueuedTask.Run(async () =>
            {
                var conflictLayer = GetLayer("conflict_point");

                if (conflictLayer == null || gdbPath == null)
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Il manque la couche conflict_point");
                    return;
                }

                var deleteGdbUpdateSde_Gdb = ConflictRequest(conflictLayer, "Supprimé gdb - Mis à jour SDE", "GDB");
                /*
                foreach (var (cle, liste) in deleteGdbUpdateSde_Gdb)
                {
                    Debug.WriteLine($"Clé : {cle}");

                    foreach (var valeur in liste)
                    {
                        Debug.WriteLine($"  - {valeur}");
                    }
                }
                */
                Debug.WriteLine("DELETE START");

                await Delete(gdbPath, deleteGdbUpdateSde_Gdb);

                Debug.WriteLine("DELETE DONE");

                var deleteGdbUpdateSde_Ini = ConflictRequest(conflictLayer, "Supprimé gdb - Mis à jour SDE", "INI");
                await Update(gdbPath, deleteGdbUpdateSde_Ini, "_ini");

                var updateGdbDeleteSde_Gdb = ConflictRequest(conflictLayer, "Mis à jour gdb - Supprimé SDE", "GDB");
                await Copy(gdbPath, updateGdbDeleteSde_Gdb, "_gdb");

                var updateGdbDeleteSde_Ini = ConflictRequest(conflictLayer, "Mis à jour gdb - Supprimé SDE", "INI");
                await Copy(gdbPath, updateGdbDeleteSde_Ini, "_ini");

                var doubleUpdateGdb = ConflictRequest(conflictLayer, "Double mise à jour", "GDB");
                await Update(gdbPath, doubleUpdateGdb, "_gdb");

                var doubleUpdateIni = ConflictRequest(conflictLayer, "Double mise à jour", "INI");
                await Update(gdbPath, doubleUpdateIni, "_ini");

                await UpdateSDEResolutionAfterFinalSave();
            });

            ProgressionText = null;
            Debug.WriteLine("Dico générés");
        }

        public Dictionary<string, List<string>> ConflictRequest(BasicFeatureLayer conflictLayer, string conflictType, string resolution)
        {
            var dictionnaire = new Dictionary<string, List<string>>();
            using (var table = conflictLayer.GetTable())
            {
                
                var queryFilter = new QueryFilter
                {
                    WhereClause = $"type_conflit = '{conflictType}' AND resolution = '{resolution}'"
                    //WhereClause = "type_conflit = 'Double mise à jour' And resolution = 'INI'"
                };


                // Parcours des lignes
                using (RowCursor cursor = table.Search(queryFilter, false))
                {
                    while (cursor.MoveNext())
                    {
                        using (Row row = cursor.Current)
                        {
                            string classe = row["classe"].ToString();
                            string oid = row["ORIGIN_OID"].ToString();
                            //Debug.WriteLine($"classe : {classe}");
                            //Debug.WriteLine($"oid : {oid}");
                            if (classe != null && oid != null)
                                dictionnaire.TryAdd(classe, new List<string>());
                                dictionnaire[classe].Add(oid);
                        }
                    }
                }
            }
            return dictionnaire;
        }

        //Sauvegarde de la résolution d'un conflit
        private async Task Delete(string gdbPath, Dictionary<string, List<string>> featuresToDelete)
        {
            await QueuedTask.Run(() =>
            {
                using (var geodatabase = new Geodatabase(
                    new FileGeodatabaseConnectionPath(new Uri(gdbPath))))
                {
                    var editOp = new EditOperation
                    {
                        Name = "Delete Features",
                        SelectModifiedFeatures = false,
                        SelectNewFeatures = false
                    };

                    foreach (var kvp in featuresToDelete)
                    {
                        string fcName = kvp.Key;
                        List<int> objectIds = kvp.Value.Select(int.Parse).ToList();

                        if (objectIds == null || objectIds.Count == 0)
                            continue;

                        using (var featureClass = geodatabase.OpenDataset<FeatureClass>(fcName))
                        {
                            using (var table = geodatabase.OpenDataset<Table>(fcName))
                            {
                                var queryFilter = new QueryFilter
                                {
                                    WhereClause = $"OBJECTID IN ({string.Join(",", objectIds)})"
                                };
                                Debug.WriteLine(queryFilter);
                                using (var cursor = table.Search(queryFilter, false))
                                {
                                    while (cursor.MoveNext())
                                    {
                                        using (var row = cursor.Current)
                                        {
                                            editOp.Delete(row);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Exécution de l'opération
                    if (!editOp.IsEmpty)
                    {
                        Debug.WriteLine("editOp Not Empty");
                        bool result = editOp.Execute();

                        if (!result)
                            throw new System.Exception(editOp.ErrorMessage);
                        else Project.Current.SaveEditsAsync();
                        
                    }
                    // Vérification suppression OK + mise à jour resolution
                    foreach (var kvp in featuresToDelete)
                    {
                        string fcName = kvp.Key;
                        List<int> objectIds = kvp.Value.Select(int.Parse).ToList();
                        if (objectIds == null || objectIds.Count == 0)
                            continue;

                        // Récupérer les Objectids encore présents dans la GDB d'update
                        List<int> stillPresent = new List<int>();
                        using (var table = geodatabase.OpenDataset<Table>(fcName))
                        {
                            var checkFilter = new QueryFilter
                            {
                                WhereClause = $"OBJECTID IN ({string.Join(",", objectIds)})"
                            };

                            using (var cursor = table.Search(checkFilter, false))
                            {
                                while (cursor.MoveNext())
                                {
                                    using (var row = cursor.Current)
                                    {
                                        stillPresent.Add((int)row.GetObjectID());
                                    }
                                }
                            }
                        }

                        // Seuls les Objectids bien supprimés passent en SAVED
                        List<int> successfullyDeleted = objectIds
                            .Except(stillPresent)
                            .ToList();

                        if (successfullyDeleted.Any())
                        {
                            Task task = UpdateResolutionAfterFinalSave(fcName, successfullyDeleted, "GDB - SAVED");
                        }
                    }
                }
            });
        }     

        private async Task Copy(
            string gdbPath,
            Dictionary<string, List<string>> featuresToCopy,
            string suffixe)
        {
            await QueuedTask.Run(() =>
            {
                var map = MapView.Active.Map;

                var editOp = new EditOperation
                {
                    Name = "Copy Features",
                    SelectNewFeatures = false
                };

                Dictionary<string, string> resolutions = new Dictionary<string, string>();
                resolutions.Add("_gdb", "GDB - SAVED");
                resolutions.Add("_ini", "INI - SAVED");

                foreach (var kvp in featuresToCopy)
                {
                    string fcName = kvp.Key;

                    // ORIGIN_OID (string → int)
                    var originOids = kvp.Value.Select(int.Parse).ToList();

                    if (!originOids.Any())
                        continue;

                    string sourceLayerName = fcName + suffixe;

                    var sourceLayer = GetLayer(sourceLayerName);

                    if (sourceLayer == null)
                        continue;

                    Debug.WriteLine($"On a bien la couche {sourceLayerName} dans le projet");

                    // récupération table source
                    using (var sourceTable = sourceLayer.GetTable())
                    {
                        // résolution ORIGIN_OID → OBJECTID réel
                        var queryFilter = new QueryFilter
                        {
                            WhereClause = $"ORIGIN_OID IN ({string.Join(",", originOids)})"
                        };

                        var realObjectIds = new List<long>();

                        using (var cursor = sourceTable.Search(queryFilter, false))
                        {
                            while (cursor.MoveNext())
                            {
                                using (var row = cursor.Current)
                                {
                                    realObjectIds.Add(Convert.ToInt64(row.GetObjectID()));
                                }
                            }
                        }

                        if (!realObjectIds.Any())
                            continue;

                        Debug.WriteLine($"{realObjectIds.Count} objets résolus pour {fcName}");

                        // destination layer (GDB cible)
                        string url = $@"{gdbPath}\data\{fcName}";

                        Debug.WriteLine(url);

                        Uri uri = new Uri(url);

                        var layer = LayerFactory.Instance.CreateLayer(uri, map);
                        var destLayer = layer as BasicFeatureLayer;

                        if (destLayer == null)
                            continue;

                        Debug.WriteLine("Nouvelle couche (update.gdb) bien chargée");
                        Debug.WriteLine($"objectids + {realObjectIds}");
                        foreach(var row in realObjectIds)
                        {
                            Debug.WriteLine($"{row}");
                        }
                        try
                        {
                            editOp.Copy(
                                destLayer,
                                sourceLayer,
                                realObjectIds,
                                null);
                            Debug.WriteLine("copy effectué");
                            if (!editOp.IsEmpty)
                            {
                                Debug.WriteLine("il y a des modifs");
                                if (!editOp.Execute())
                                    throw new Exception(editOp.ErrorMessage);
                                else
                                    _ = UpdateResolutionAfterFinalSave(fcName, originOids, resolutions[suffixe]);   
                            }
                        }
                        finally
                        {
                            map.RemoveLayer(destLayer);
                        }
                    }
                }
            });

            // sauvegarde
            if (Project.Current.HasEdits)
                await Project.Current.SaveEditsAsync();
        }

        private async Task Update(
            string gdbPath,
            Dictionary<string, List<string>> featuresToUpdate,
            string suffixe)
        {
            await QueuedTask.Run(() =>
            {
                var map = MapView.Active.Map;
                Dictionary<string, string> resolutions = new Dictionary<string, string>();
                resolutions.Add("_gdb", "GDB - SAVED");
                resolutions.Add("_ini", "INI - SAVED");
                Debug.WriteLine("gdbPath = " + gdbPath);

                foreach (var kvp in featuresToUpdate)
                {
                    string fcName = kvp.Key;
                    var originOids = kvp.Value.Select(int.Parse).ToList();
                    if (!originOids.Any()) continue;

                    string sourceLayerName = fcName + suffixe;
                    var sourceLayer = GetLayer(sourceLayerName);
                    if (sourceLayer == null) continue;

                    bool isAnnotation = sourceLayer is AnnotationLayer;

                    // Chargement du layer cible depuis la GDB fichier
                    string url = $@"{gdbPath}\data\{fcName}";
                    var destLayer = LayerFactory.Instance.CreateLayer(new Uri(url), map) as BasicFeatureLayer;
                    if (destLayer == null) continue;

                    try
                    {
                        var editOp = new EditOperation
                        {
                            Name = "Update Features",
                            SelectModifiedFeatures = false,
                            SelectNewFeatures = false
                        };

                        using (var sourceTable = sourceLayer.GetTable())
                        {
                            var sourceQuery = new QueryFilter
                            {
                                WhereClause = $"ORIGIN_OID IN ({string.Join(",", originOids)})"
                            };

                            var sourceRows = new Dictionary<long, Row>();
                            using (var cursor = sourceTable.Search(sourceQuery, false))
                            {
                                while (cursor.MoveNext())
                                {
                                    var row = cursor.Current;
                                    var originOid = Convert.ToInt64(row["ORIGIN_OID"]);
                                    sourceRows[originOid] = row;
                                }
                            }

                            if (!sourceRows.Any()) continue;

                            var targetQuery = new QueryFilter
                            {
                                WhereClause = $"OBJECTID IN ({string.Join(",", sourceRows.Keys)})"
                            };

                            using (var destTable = destLayer.GetTable())
                            using (var cursor = destTable.Search(targetQuery, false))
                            {
                                while (cursor.MoveNext())
                                {
                                    using (var targetRow = cursor.Current)
                                    {
                                        var oid = targetRow.GetObjectID();
                                        if (!sourceRows.ContainsKey(oid)) continue;

                                        var sourceRow = sourceRows[oid];
                                        var attributes = BuildAttributeDictionary(sourceRow, targetRow);

                                        if (isAnnotation &&
                                            sourceRow is AnnotationFeature srcAnno &&
                                            targetRow is AnnotationFeature tgtAnno)
                                        {
                                            editOp.Modify(targetRow, attributes);
                                            var srcGraphic = srcAnno.GetGraphic() as CIMTextGraphic;
                                            if (srcGraphic != null)
                                            {
                                                tgtAnno.SetGraphic(srcGraphic);
                                                targetRow.Store();
                                            }
                                        }
                                        else if (sourceRow is Feature srcFeat &&
                                                 targetRow is Feature tgtFeat)
                                        {
                                            editOp.Modify(destLayer, oid, srcFeat.GetShape(), attributes);
                                        }
                                    }
                                }
                            }
                        }

                        if (!editOp.IsEmpty)
                        {
                            bool result = editOp.Execute();
                            if (!result)
                                throw new Exception(editOp.ErrorMessage);
                            else
                                _ = UpdateResolutionAfterFinalSave(fcName, originOids, resolutions[suffixe]);
                        }
                    }
                    finally
                    {
                        map.RemoveLayer(destLayer);
                    }
                }
            });

            if (Project.Current.HasEdits)
            {
                Debug.WriteLine("sauvegarde totale");
                await Project.Current.SaveEditsAsync();
            }
        }

        // mise à jour des champs éditables communs
        private void UpdateEditableFields(Row sourceRow, Row targetRow)
        {
            var targetFields = targetRow.GetFields().Select(f => f.Name).ToHashSet();

            foreach (var field in sourceRow.GetFields())
            {
                if (field.FieldType == FieldType.OID) continue;
                if (!field.IsEditable) continue;
                if (!targetFields.Contains(field.Name)) continue;

                // Champs internes annotation gérés via CIMTextGraphic, ignorés ici
                if (IsAnnotationInternalField(field.Name)) continue;

                targetRow[field.Name] = sourceRow[field.Name];
            }
        }

        private Dictionary<string, object> BuildAttributeDictionary(Row sourceRow, Row targetRow)
        {
            var attributes = new Dictionary<string, object>();
            var targetFields = targetRow.GetFields().Select(f => f.Name).ToHashSet();

            foreach (var field in sourceRow.GetFields())
            {
                if (field.FieldType == FieldType.OID) continue;
                if (!field.IsEditable) continue;
                if (!targetFields.Contains(field.Name)) continue;
                if (IsAnnotationInternalField(field.Name)) continue;
                attributes[field.Name] = sourceRow[field.Name];
            }
            return attributes;
        }

        // Champs annotation gérés par le CIMTextGraphic
        private bool IsAnnotationInternalField(string fieldName)
        {
            var annotationManagedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Element", "ZOrder", "AnnotationClassID", "SymbolID",
                "Status", "TextString", "FontName", "FontSize",
                "Bold", "Italic", "Underline", "VerticalAlignment",
                "HorizontalAlignment", "XOffset", "YOffset",
                "Angle", "FontLeading", "WordSpacing", "CharacterWidth",
                "CharacterSpacing", "FlipAngle", "Override"
            };

            return annotationManagedFields.Contains(fieldName);
        }

        private async Task Zoom()
        {
            Debug.WriteLine("Lancement du zoom");
            if (SelectedTable == null)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Sélectionnez une classe d'entités");
                return;
            }
            else if (SelectedObjectid == null)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Sélectionnez un OBJECTID");
                return;
            }
            else
            {
                Debug.WriteLine("On peut tenter le zoom");

                // effacer sélection en cours

                var layer = GetLayer(SelectedTable.Name + "_ini");

                await QueuedTask.Run(() =>
                {
                    // effacer sélection en cours
                    MapView.Active.Map.SetSelection(null);

                    var queryFilter = new QueryFilter
                    {
                        WhereClause = $"ORIGIN_OID = {SelectedObjectid.Oid}"
                    };
                    layer.Select(queryFilter);
                });
                // Zoom côté UI (hors QueuedTask)
                await MapView.Active.ZoomToSelectedAsync();
                await QueuedTask.Run(() =>
                {
                    layer.ClearSelection();
                });

            }
        }

        private async Task Flash()
        {
            Debug.WriteLine("Lancement du flash");
            if (SelectedTable == null)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Sélectionnez une classe d'entités");
                return;
            }
            else if (SelectedObjectid == null)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Sélectionnez un OBJECTID");
                return;
            }

            Debug.WriteLine("On peut tenter le flash");

            var layer = GetLayer(SelectedTable.Name + "_ini");

            if (layer == null)
                return;

            SelectionSet selectionSet = null;

            await QueuedTask.Run(() =>
            {
                // Requête pour récupérer l'ObjectID correspondant à ORIGIN_OID
                var queryFilter = new QueryFilter
                {
                    WhereClause = $"ORIGIN_OID = {SelectedObjectid.Oid}"
                };

                var objectIds = new List<long>();
                using (var cursor = layer.GetTable().Search(queryFilter, false))
                {
                    while (cursor.MoveNext())
                    {
                        using (var row = cursor.Current)
                        {
                            objectIds.Add(row.GetObjectID());
                        }
                    }
                }

                if (objectIds.Count > 0)
                {
                    // Créer le SelectionSet directement à partir des OID
                    selectionSet = SelectionSet.FromDictionary(new Dictionary<MapMember, List<long>>
                    {
                        { layer, objectIds }
                    });
                }
            });

            if (selectionSet != null && selectionSet.Count > 0)
                MapView.Active.FlashFeature(selectionSet);
        }

        private BasicFeatureLayer GetLayer(string name)
        {
            return MapView.Active.Map.GetLayersAsFlattenedList()
                .OfType<BasicFeatureLayer>()
                .FirstOrDefault(l =>
                    l.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public static string getGdbEditedPath(string fullPathConflictGdb)
        {
            string fileName = "chemin_gdb_editee.txt";
            string filePath = System.IO.Path.Combine(fullPathConflictGdb, fileName);
            Debug.WriteLine("fullPathGdbEdited = " + fullPathConflictGdb);           

            using (StreamReader reader = new StreamReader(filePath))
            {
                string firstLine = reader.ReadLine();
                Debug.WriteLine($"La première ligne du fichier est : {firstLine}");
                //System.Windows.MessageBox.Show(firstLine);
                return firstLine;
            }
            
        }
        #endregion

            #region Manage other controls in the DockPane

            /// <summary>
            /// Show the DockPane.
            /// </summary>
        internal static void Show()
        {
            DockPane pane = FrameworkApplication.DockPaneManager.Find(_dockPaneID);
            if (pane == null)
                return;

            pane.Activate();

        }

        public ObservableCollection<string> ListToObservableCollection(List<string> listString)
        {
            ObservableCollection<string> myCollection = new ObservableCollection<string>(listString as List<string>);
            return myCollection;
        }

        #endregion
    }

    /// <summary>
    /// Button implementation to show the DockPane.
    /// </summary>
    internal class ConflictManagerView_ShowButton : Button
    {
        protected override async void OnClick()
        {           
            const string conflictMapName = "carte_conflits";

            MapProjectItem mapItem = Project.Current.GetItems<MapProjectItem>().FirstOrDefault(item =>
                                                                item.Name.Equals(conflictMapName));

            if (mapItem == null)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox
                    .Show("KO Il manque la carte carte_conflits");
                return;
            }
            var activeMapName = MapView.Active?.Map?.Name;

            if (!string.Equals(activeMapName, conflictMapName, StringComparison.OrdinalIgnoreCase))
            {
                // Récupération de la Map SUR LE MCT
                Map map = await QueuedTask.Run(() =>
                {
                    return mapItem.GetMap();
                });

                // Ouverture/activation de la carte (UI thread)
                await FrameworkApplication.Panes.CreateMapPaneAsync(map);
            }
            Debug.WriteLine("OK la carte carte_conflits est présente");           
            ConflictManagerViewModel.Show();

        }
        protected override void OnUpdate()
        {
            this.Enabled = true;
        }
    }

  
}
