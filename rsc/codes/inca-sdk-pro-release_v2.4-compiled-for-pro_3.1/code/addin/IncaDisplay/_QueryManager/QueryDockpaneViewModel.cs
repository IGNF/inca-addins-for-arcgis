//   Copyright 2019 Esri
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at


using ArcGIS.Core.Data;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using IGN.Inca.ArcGISPro.SDK.Commons;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Resources;
using System.Threading.Tasks;
using System.Windows.Input;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

namespace IncaDisplay._QueryManager
{
    internal class QueryDockpaneViewModel : DockPane
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private const string DockPaneId = "ID_QueryDockpane";

        protected QueryDockpaneViewModel()
        {
            // get the dictionary of the current UI language ("CurrentUICulture") using the Culture Resources Manager
            Translator = CultureResourcesManager.GetInstance().dictLanguage;

            // initialize "inca_logger_sdk" logger
            LoggerUtils.GetInstance().InitializeLogger();
        }

        #region Properties
        /// <summary>
        /// Culture translator
        /// </summary>
        private ResourceSet Translator { get; set; }

        /// <summary>
        /// Show the DockPane.
        /// </summary>
        internal static void Show()
        {
            DockPane pane = FrameworkApplication.DockPaneManager.Find(DockPaneId);
            pane?.Activate();
        }

        /// <summary>
        /// Text shown near the top of the DockPane.
        /// </summary>
        private string _heading = "Gestionnaire de requêtes";
        private static string DataPath;
        private string input_Features;
        private GDBProjectItem gdbProjectItem;

        // Liste des requêtes disponibles dans la gdb
        private ObservableCollection<string> _queries;
        public ObservableCollection<string> Queries
        {
            get { return _queries; }
            set
            {
                _queries = value;
                NotifyPropertyChanged(new PropertyChangedEventArgs("Queries"));
            }
        }

        // La requete selectionnee
        private string _selectedQuery;
        public string SelectedQuery
        {
            get { return _selectedQuery; }
            set { SetProperty(ref _selectedQuery, value, () => SelectedQuery); }
        }

        private string _SelectedName;
        public string SelectedName
        {
            get { return _SelectedName; }
            set { SetProperty(ref _SelectedName, value, () => SelectedName); }
        }

        // le nombre d'éléments sélectionnés en fin de requête
        private string _ResultMessage;
        public string ResultMessage
        {
            get { return _ResultMessage; }
            set
            {
                if (SelectedTable != "" || SelectedTable is not null)
                    SetProperty(ref _ResultMessage, value, () => ResultMessage);
                else SetProperty(ref _ResultMessage, "", () => ResultMessage);
            }
        }

        // Liste des types de requetes
        private ObservableCollection<string> _tables;
        public ObservableCollection<string> Tables
        {
            get { return _tables; }
            set
            {
                _tables = value;
                NotifyPropertyChanged(new PropertyChangedEventArgs("Tables"));
            }
        }

        // Le type de requete selectionnee      
        private string _selectedTable;
        public string SelectedTable
        {
            get { return _selectedTable; }
            set
            {
                SetProperty(ref _selectedTable, value, () => SelectedTable);
                if (SelectedTable == "" || SelectedTable is null)
                { ResultMessage = ""; TextBoxBackground = "WhiteSmoke"; TextBoxFontWeight = "Normal"; }
            }
        }

        private string _NameGDB = "";
        public string NameGDB
        {
            get { return _NameGDB; }
            set { SetProperty(ref _NameGDB, value, () => NameGDB); }
        }

        public string Heading
        {
            get { return _heading; }
            set { SetProperty(ref _heading, value, () => Heading); }
        }

        // Excel config file path property
        private string _excelConfigFileFullPath = @"";
        public string ExcelConfigFileFullPath
        {
            get { return _excelConfigFileFullPath; }
            set
            {
                SetProperty(ref _excelConfigFileFullPath, value, () => ExcelConfigFileFullPath);
            }
        }

        // fond de la fenêtre de résultat des geoprocessings
        private String _TextBoxBackground = "WhiteSmoke";
        public String TextBoxBackground
        {
            get => _TextBoxBackground;
            set => SetProperty(ref _TextBoxBackground, value, () => TextBoxBackground);
        }

        // si on doit écrire en gras , italique, normal.
        private String _TextBoxFontWeight = "Normal";
        public String TextBoxFontWeight
        {
            get => _TextBoxFontWeight;
            set => SetProperty(ref _TextBoxFontWeight, value, () => TextBoxFontWeight);
        }

        #endregion

        #region dictionnaire
        /// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ START ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        ///     One-Way Data Binding Properties, used specially for internationalization of the UI
        /// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        public string QueryType { get { return Translator.GetString("QueryType"); } }
        public string Query { get { return Translator.GetString("Query"); } }
        public string ExecuteQuery { get { return Translator.GetString("ExecuteQuery"); } }
        public string ShowQuery { get { return Translator.GetString("ShowQuery"); } }
        public string CreateTableSelection { get { return Translator.GetString("CreateTableSelection"); } }
        public string WithName { get { return Translator.GetString("WithName"); } }
        public string CreateGDB { get { return Translator.GetString("CreateGDB"); } }
        public string ImportGDB { get { return Translator.GetString("ImportGDB"); } }

        public string TT_ExecuteQuery { get { return Translator.GetString("TT_ExecuteQuery"); } }
        public string TT_ShowQuery { get { return Translator.GetString("TT_ShowQuery"); } }
        public string TT_CreateTableSelection { get { return Translator.GetString("TT_CreateTableSelection"); } }
        public string TT_CreateGDB { get { return Translator.GetString("TT_CreateGDB"); } }
        public string TT_ImportGDB { get { return Translator.GetString("TT_ImportGDB"); } }
        #endregion

        #region Fonctions
        // structure pour stocker les infos sur les tables.
        internal class TableInfo
        {
            public string Name { get; set; }
            public TableDefinition Definition { get; set; }
        }

        // Teste et affecte les valeurs de la table dans le géotraitement
        protected void AttributSelection(Table laTable)
        {
            string selection_type = "NEW_SELECTION", where_clause = string.Empty, invert_where_clause = "NON_INVERT";
            //lit propriétés de la requête
            var def = laTable.GetDefinition();
            int idxTableFieldNOM = def.FindField(QueryDico.Name_Field);
            using (var row = laTable.Search())
            {
                while (row.MoveNext())
                {
                    using (var record = row.Current)
                    {
                        if (record[idxTableFieldNOM].Equals(SelectedQuery))
                        {
                            if (record[def.FindField(QueryDico.Type_Field)] != null)
                                selection_type = record[def.FindField(QueryDico.Type_Field)].ToString();

                            if (record[def.FindField(QueryDico.Query_Field)] != null)
                                where_clause = record[def.FindField(QueryDico.Query_Field)].ToString();

                            if (record[def.FindField(QueryDico.Classe_Field)] != null)
                                input_Features = record[def.FindField(QueryDico.Classe_Field)].ToString();
                            else
                                MessageBox.Show(QueryDico.ProblemLayer);

                            if (record[def.FindField(QueryDico.Invert_Field)] != null)
                                invert_where_clause = record[def.FindField(QueryDico.Invert_Field)].ToString();
                        }
                    }
                }
            }
            // affecte les propriétés dans un groupe d'argument Arcgis
            var arguments = Geoprocessing.MakeValueArray(input_Features, selection_type, where_clause, invert_where_clause);
            Geoprocessing.OpenToolDialog("SelectLayerByAttribute_management", arguments);
            //SelectedProcess = true;           
        }

        // Teste et affecte les valeurs de la table dans le géotraitement "Sélectionner des entités à l’aide d’attributs"
        protected void LocationSelection(Table laTable)
        {
            string selection_type = string.Empty, where_clause = string.Empty, Relationship = string.Empty,
                                    invert_where_clause = "NOT_INVERT", Search_distance = string.Empty,
                                    Selecting_features = string.Empty;
            //lit les propriétés de la requête
            var def = laTable.GetDefinition();
            int idxTableFieldNOM = def.FindField(QueryDico.Name_Field);
            var row = laTable.Search();
            // lit les champs s'ils existent
            while (row.MoveNext())
            {
                using (var record = row.Current)
                    if (record[idxTableFieldNOM].Equals(SelectedQuery))
                    {
                        if (record[def.FindField(QueryDico.Type_Field)] != null)
                            input_Features = record[def.FindField(QueryDico.input_features_Field)].ToString();
                        if (record[def.FindField(QueryDico.Type_Field)] != null)
                            selection_type = record[def.FindField(QueryDico.Type_Field)].ToString();
                        if (record[def.FindField(QueryDico.relation_Field)] != null)
                            Relationship = record[def.FindField(QueryDico.relation_Field)].ToString();
                        if (record[def.FindField(QueryDico.Invert_Field)] != null)
                            invert_where_clause = record[def.FindField(QueryDico.Invert_Field)].ToString();
                        if (record[def.FindField(QueryDico.distance_Field)] != null)
                            Search_distance = record[def.FindField(QueryDico.distance_Field)].ToString();
                        if (record[def.FindField(QueryDico.Selecting_features_Field)] != null)
                            Selecting_features = record[def.FindField(QueryDico.Selecting_features_Field)].ToString();
                    }
            }
            // affecte les propriétés dans un groupe d'argument Arcgis
            var arguments = Geoprocessing.MakeValueArray(input_Features, Relationship, Selecting_features,
                Search_distance, selection_type, invert_where_clause);
            Geoprocessing.OpenToolDialog("SelectLayerByLocation_management", arguments);
            //SelectedProcess = true;
        }

        // fonction pour la construction de la database "REQ.gdb"
        public async Task<bool> Constructgdb(string folder, string gdbName)
        {
            bool RETOUR = false;
            // nom de la gdb
            QueryUtil.nom_query = gdbName;
            try
            {
                //dossier du projet
                DirectoryInfo dir = new DirectoryInfo(folder + "\\" + QueryUtil.nom_query + ".gdb");
                // si le dossier de la database existe
                if (dir.Exists)
                {
                    // si la database n'est pas dans le projet
                    if (Project.Current.GetItems<GDBProjectItem>().FirstOrDefault(l => l.Name.Equals(QueryUtil.nom_query + ".gdb")) == null)
                    {
                        // import dans le projet
                        IProjectItem item = ItemFactory.Instance.Create(folder + "\\" + QueryUtil.nom_query + ".gdb") as IProjectItem;
                        Project.Current.AddItem(item);
                    }
                    // vérifie si la database est à niveau
                    RETOUR = await QueryDico.ExecuteTestGDB(folder, QueryUtil.nom_query);
                }
                // sinon crée la database                        
                else
                    await QueuedTask.Run(async () =>
                    {
                        RETOUR = await QueryDico.ExecuteAddConstructGDB(folder, QueryUtil.nom_query);
                        try
                        {
                            MapProjectItem mapProjItem = Project.Current.GetItems<MapProjectItem>().First(t => t.Name.Equals(MapView.Active.Map.Name));
                            List<StandaloneTable> List_tables = mapProjItem.GetMap().StandaloneTables.ToList();
                            foreach (var table in List_tables)
                                if (table.Name == QueryDico.attribute_query_default_name || table.Name == QueryDico.location_query_default_name || table.Name == QueryDico.template_default_name)
                                {
                                    await QueuedTask.Run(() => { mapProjItem.GetMap().RemoveStandaloneTable(table); });
                                }
                            mapProjItem.Dispose();
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.ToString());
                        }
                    });
            }
            catch
            {   // problème lors de la construction -> alerte l'opérateur
                MessageBox.Show(QueryDico.ProblemConstructGDB);
                return await new Task<bool>(() => false);
            }
            return RETOUR;
        }


        // Mise à jour des tables permettant le choix des requêtes.
        private async Task UpdateTables(GDBProjectItem GDBProjectItem, List<TableInfo> observedTables)
        {
            var tables = new List<TableInfo>();
            await QueuedTask.Run(() =>
            {
                var tableDefinitions = (GDBProjectItem.GetDatastore() as Geodatabase).GetDefinitions<TableDefinition>();
                tables.AddRange(tableDefinitions.Select(definition => new TableInfo { Definition = definition, Name = definition.GetName() }));
            });
            observedTables.Clear();
            observedTables.AddRange(tables);
        }
        #endregion Fonctions

        #region Les Boutons
        /// <summary>
        /// 
        /// commande d'exécution de la requête sélectionnée.
        /// </summary>
        private ICommand _CmdGO;
        public ICommand CmdGO
        {
            get
            {
                return _CmdGO ?? (_CmdGO = new RelayCommand(async () =>
                {
                    await QueuedTask.Run(() =>
                    {
                        ResultMessage = "";
                        gdbProjectItem = Project.Current.GetItems<GDBProjectItem>().FirstOrDefault(l => l.Name.Contains(QueryUtil.nom_query));
                        if (gdbProjectItem == null) MessageBox.Show(QueryDico.messageCreateGDB);
                        else
                        {
                            var tables = new List<TableInfo>();
                            try
                            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                                UpdateTables(gdbProjectItem, tables);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                                var table = tables.First(matable => matable.Name.Equals(SelectedTable.Insert(0, "config_")));
                                if (table == null) return;

                                Table MAtable = (gdbProjectItem.GetDatastore() as Geodatabase).OpenDataset<Table>(table.Name);

                                //activer la fonction de sélection par attribut.
                                if (MAtable.GetName().Equals(QueryDico.attribute_query_default_name))
                                {
                                    string selection_type = "NEW_SELECTION", where_clause = string.Empty, invert_where_clause = "NON_INVERT";
                                    //lit propriétés de la requête
                                    var def = MAtable.GetDefinition();
                                    int idxTableFieldNOM = def.FindField(QueryDico.Name_Field);
                                    using (var row = MAtable.Search())
                                    {
                                        while (row.MoveNext())
                                        {
                                            using (var record = row.Current)
                                                if (record[idxTableFieldNOM].Equals(SelectedQuery))
                                                {
                                                    if (record[def.FindField(QueryDico.Type_Field)] != null)
                                                        selection_type = record[def.FindField(QueryDico.Type_Field)].ToString();
                                                    if (record[def.FindField(QueryDico.Query_Field)] != null)
                                                        where_clause = record[def.FindField(QueryDico.Query_Field)].ToString();
                                                    if (record[def.FindField(QueryDico.Classe_Field)] != null)
                                                        input_Features = record[def.FindField(QueryDico.Classe_Field)].ToString();
                                                    if (record[def.FindField(QueryDico.Invert_Field)] != null)
                                                        invert_where_clause = record[def.FindField(QueryDico.Invert_Field)].ToString();
                                                }
                                        }
                                    }
                                    // affecte les propriétés dans un groupe d'argument Arcgis
                                    var arguments = Geoprocessing.MakeValueArray(input_Features, selection_type, where_clause, invert_where_clause);
                                    IGPResult result = Geoprocessing.ExecuteToolAsync("SelectLayerByAttribute_management", arguments).Result;
                                    QueryManager_ERROR(result);
                                }
                                //activer la fonction de sélection spatiale.
                                else if (MAtable.GetName().Equals(QueryDico.location_query_default_name))
                                {
                                    string selection_type = string.Empty, where_clause = string.Empty, Relationship = string.Empty,
                                    invert_where_clause = "NOT_INVERT", Search_distance = string.Empty,
                                    Selecting_features = string.Empty;
                                    //lit propriétés de la requête
                                    var def = MAtable.GetDefinition();
                                    int idxTableFieldNOM = def.FindField(QueryDico.Name_Field);
                                    var row = MAtable.Search();
                                    while (row.MoveNext())
                                    {
                                        using (var record = row.Current)
                                            if (record[idxTableFieldNOM].Equals(SelectedQuery))
                                            {
                                                if (record[def.FindField(QueryDico.input_features_Field)] != null)
                                                    input_Features = record[def.FindField(QueryDico.input_features_Field)].ToString();
                                                if (record[def.FindField(QueryDico.Type_Field)] != null)
                                                    selection_type = record[def.FindField(QueryDico.Type_Field)].ToString();
                                                if (record[def.FindField(QueryDico.relation_Field)] != null)
                                                    Relationship = record[def.FindField(QueryDico.relation_Field)].ToString();
                                                if (record[def.FindField(QueryDico.Invert_Field)] != null)
                                                    invert_where_clause = record[def.FindField(QueryDico.Invert_Field)].ToString();
                                                if (record[def.FindField(QueryDico.distance_Field)] != null)
                                                    Search_distance = record[def.FindField(QueryDico.distance_Field)].ToString();
                                                if (record[def.FindField(QueryDico.Selecting_features_Field)] != null)
                                                    Selecting_features = record[def.FindField(QueryDico.Selecting_features_Field)].ToString();
                                            }
                                    }
                                    // affecte les propriétés dans un groupe d'argument Arcgis
                                    var arguments = Geoprocessing.MakeValueArray(input_Features, Relationship, Selecting_features,
                                        Search_distance, selection_type, invert_where_clause);
                                    IGPResult result = Geoprocessing.ExecuteToolAsync("SelectLayerByLocation_management", arguments).Result;
                                    QueryManager_ERROR(result);
                                }
                            }
                            // problème -> alerte l'opérateur
                            catch
                            {
                                TextBoxBackground = "IndianRed";
                                TextBoxFontWeight = "Bold";
                                ResultMessage = $"{QueryDico.MessageToActivateMap}";
                            }
                        }
                    });
                }, () => SelectedQuery != null));
            }
        }

        /// <summary>
        ///  Gestion des erreurs lors de la sortie des geoprocessings
        /// </summary>
        /// <param name="gPResult"></param>
        public void QueryManager_ERROR(IGPResult gPResult)
        {
            if (gPResult.IsFailed)
            {
                TextBoxBackground = "Orange"; TextBoxFontWeight = "Bold";
                ResultMessage = $"{QueryDico.MessageEnd_Selection_error} {ShowQuery}";
            }
            if (!gPResult.IsFailed && !gPResult.IsCanceled)
            {
                // test l'exsitence d'une arborescence dans le chemin de la couche.
                List<string> parents = new List<string>();
                string layer_name;
                if (input_Features.ToString().Contains("/"))
                {
                    parents = input_Features.Split('/').ToList();
                    layer_name = parents[parents.Count - 1];
                }
                else if (input_Features.ToString().Contains("\\"))
                {
                    parents = input_Features.Split('\\').ToList();
                    layer_name = parents[parents.Count - 1];
                    parents.Remove(layer_name);
                }
                else layer_name = input_Features;

                // pour chaque couche trouvée, on vérifie si c'est la bonne, et on exécute la requête si c'est le cas.
                var layerList = MapView.Active.Map.FindLayers(layer_name).ToList();
                if (layerList.Count != 0)
                {
                    foreach (Layer layer in layerList)
                    {
                        //on vérifie que l'on soit bien sur la bonne couche :
                        if (parents.Count == 0 || (parents.Count != 0 && layer.Parent.ToString() == parents[0]))
                        {
                            if (layer is FeatureLayer fl)
                            {
                                TextBoxBackground = "WhiteSmoke"; TextBoxFontWeight = "Normal";
                                if (fl.SelectionCount == 0)
                                    ResultMessage = $"{SelectedQuery} : {QueryDico.MessageEnd_Selection_nul} {input_Features}";
                                else if (fl.SelectionCount == 1)
                                    ResultMessage = $"{SelectedQuery} : {fl.SelectionCount} {QueryDico.MessageEnd_Selection_one} {input_Features}";
                                else
                                    ResultMessage = $"{SelectedQuery} : {fl.SelectionCount} {QueryDico.MessageEnd_Selection_several} {input_Features}";
                            }
                            else if (layer is AnnotationLayer al)
                            {
                                TextBoxBackground = "WhiteSmoke"; TextBoxFontWeight = "Normal";
                                if (al.SelectionCount == 0)
                                    ResultMessage = $"{SelectedQuery} : {QueryDico.MessageEnd_Selection_nul} {input_Features}";
                                else if (al.SelectionCount == 1)
                                    ResultMessage = $"{SelectedQuery} : {al.SelectionCount} {QueryDico.MessageEnd_Selection_one} {input_Features}";
                                else
                                    ResultMessage = $"{SelectedQuery} : {al.SelectionCount} {QueryDico.MessageEnd_Selection_several} {input_Features}";
                            }
                        }
                    }

                }
            }
            // si aucune couche n'est trouvée, on affiche un message d'erreur
            else
            {
                TextBoxBackground = "WhiteSmoke"; TextBoxFontWeight = "Normal";
                ResultMessage = $"{QueryDico.MessageLayer_error} : {input_Features}";
            }
        }

        /// <summary>
        /// Commande pour ouvrir le geoprocessing, les champs sont remplis avec les options de la table sélectionnée
        /// </summary>
        private ICommand _CmdShowQuery;
        public ICommand CmdShowQuery
        {
            get
            {
                return _CmdShowQuery ?? (_CmdShowQuery = new RelayCommand(async () =>
                {
                    await QueuedTask.Run(() =>
                    {
                        gdbProjectItem = Project.Current.GetItems<GDBProjectItem>().FirstOrDefault(l => l.Name.Contains(QueryUtil.nom_query));
                        if (gdbProjectItem == null) MessageBox.Show(QueryDico.messageCreateGDB);
                        else
                        {
                            var tables = new List<TableInfo>();
                            try
                            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                                UpdateTables(gdbProjectItem, tables);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                                var table = tables.First(matable => matable.Name.Equals(SelectedTable.Insert(0, "config_")));
                                if (table == null) return;

                                Table MAtable = (gdbProjectItem.GetDatastore() as Geodatabase).OpenDataset<Table>(table.Name);

                                //activer la fonction de sélection par attribut.
                                if (MAtable.GetName().Contains(QueryDico.attribute_query_default_name)) { AttributSelection(MAtable); }
                                // activer la fonction de sélection par emplacement.
                                else if (MAtable.GetName().Contains(QueryDico.location_query_default_name)) { LocationSelection(MAtable); }
                            }
                            // problème -> alerte l'opérateur
                            catch { MessageBox.Show(QueryDico.ProblemShowQuery); }
                        }
                    });
                }, () => SelectedQuery != null));
            }
        }

        /// <summary>
        /// Commande pour créer une table avec la sélection
        /// </summary>        
        private ICommand _cmdWork;
        public ICommand CmdWork
        {
            get
            {
                return _cmdWork ?? (_cmdWork = new RelayCommand(() =>
                {
                    try
                    {
                        // vérifie si la carte est activitée
                        var map = MapView.Active.Map;
                    }
                    catch
                    {
                        var mapPane = FrameworkApplication.Panes.OfType<IMapPane>().First() as Pane;
                        mapPane.Activate();
                    }

                    if (MapView.Active.Map.SelectionCount != 0)
                    {
                        QueuedTask.Run(async () =>
                        {
                            // Nom de la gdb
                            if (QueryUtil.nom_query is null)
                            {
                                if (NameGDB != "") QueryUtil.nom_query = NameGDB;
                                else QueryUtil.nom_query = QueryDico.query_default_name;
                            }
                            gdbProjectItem = Project.Current.GetItems<GDBProjectItem>().FirstOrDefault(l => l.Name.Equals(QueryUtil.nom_query + ".gdb"));

                            // teste si la table Template de la GDB est correcte pour pouvoir l'utiliser comme modèle
                            if (await QueryDico.ExecuteTestGDB_Template(Project.Current.HomeFolderPath, QueryUtil.nom_query) is true)
                            {
                                // nom de la table en sortie
                                string chaine = "";
                                if (SelectedName != "" && SelectedName != null) chaine = SelectedName;
                                else if (SelectedQuery != "" && SelectedQuery != null) chaine = SelectedQuery.ToString();
                                else chaine = QueryDico.selection_default_name;
                                if (chaine != "")
                                {
                                    chaine = chaine.Replace(" ", "_");
                                    chaine = chaine.Insert(0, "result_");
                                    chaine = chaine.Normalize();
                                }
                                // Crée la Table
                                await QueryUtil.CreerTable(chaine, gdbProjectItem);
                                // on vide le nom de la carte
                                SelectedName = "";
                            }
                            // si la table Template n'est pas à jour -> message pour l'opérateur
                            else MessageBox.Show(QueryDico.messageCreateGDB);
                        });
                    }
                    // s'il n'y a pas de selection -> message pour l'opérateur
                    else MessageBox.Show(QueryDico.WarningNullSelection);
                }));
            }
        }

        /// <summary>
        /// Commande pour remplir et afficher les types de requêtes disponibles depuis les tables de la gdb
        /// </summary>        
        private ICommand _CmdDropDownTables;
        public ICommand CmdDropDownTables
        {
            get
            {
                return _CmdDropDownTables ?? (_CmdDropDownTables = new RelayCommand(async () =>
                {

                    var tables = new List<TableInfo>();
                    var table_temp = new List<TableInfo>();
                    // Nom de la gdb
                    if (QueryUtil.nom_query is null)
                    {
                        if (NameGDB != "") QueryUtil.nom_query = NameGDB;
                        else { QueryUtil.nom_query = QueryDico.query_default_name; NameGDB = QueryDico.query_default_name; }
                    }
                    gdbProjectItem = Project.Current.GetItems<GDBProjectItem>().FirstOrDefault(l => l.Name.Equals(QueryUtil.nom_query + ".gdb"));

                    // teste si la GDB est correcte
                    if (await QueryDico.ExecuteTestGDB(Project.Current.HomeFolderPath, QueryUtil.nom_query))
                    {
                        try
                        {
                            // recherche les tables attributaires nécessaires 
                            await QueuedTask.Run(() =>
                            {
                                IEnumerable<TableDefinition> tableDefinitions = (gdbProjectItem.GetDatastore() as Geodatabase).GetDefinitions<TableDefinition>().Where(t => t.GetName().Equals(QueryDico.attribute_query_default_name));
                                tables.AddRange(tableDefinitions.Select(definition => new TableInfo { Definition = definition, Name = definition.GetName() }));

                                tableDefinitions = (gdbProjectItem.GetDatastore() as Geodatabase).GetDefinitions<TableDefinition>().Where(t => t.GetName().Equals(QueryDico.location_query_default_name));
                                tables.AddRange(tableDefinitions.Select(definition => new TableInfo { Definition = definition, Name = definition.GetName() }));
                            });
                            // affectation de ces tables à la liste "Tables" (liste du bouton)
                            Tables = new ObservableCollection<string>();
                            // en enlevant le préfixe "config_"
                            foreach (var temp in tables) Tables.Add(temp.Name.Replace("config_", ""));
                            // vide la valeur de la Query
                            SelectedQuery = null;
                        }
                        catch
                        {
                            // probleme lors de la recherche des tables -> alerte l'opérateur
                            MessageBox.Show(QueryDico.ProblemEditTable);
                        }
                    }
                    // si la gdb n'est pas à jour -> message pour l'opérateur
                    else MessageBox.Show(QueryDico.messageCreateGDB);
                }));
            }
        }

        /// <summary>
        /// Commande pour lire la table sélectionnée dans "SelectedTable" afin d'afficher toutes les requêtes disponibles dans "Queries"
        /// </summary>
        private ICommand _cmdDropDownQueries;
        public ICommand CmdDropDownQueries
        {
            get
            {
                return _cmdDropDownQueries ?? (_cmdDropDownQueries = new RelayCommand(() =>
                {
                    QueuedTask.Run(() =>
                    {
                        var tables = new List<TableInfo>();
                        gdbProjectItem = Project.Current.GetItems<GDBProjectItem>().FirstOrDefault(l => l.Name.Contains(QueryUtil.nom_query));

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        UpdateTables(gdbProjectItem, tables);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                        // Matable : la table choisi avec le nom dans le menu déroulant "Tables"
                        TableInfo MAtable = tables.First(matable => matable.Name.Equals(SelectedTable.Insert(0, "config_")));
                        if (MAtable == null) return;
                        Table table = (gdbProjectItem.GetDatastore() as Geodatabase).OpenDataset<Table>(MAtable.Name);
                        // s'il n'y a pas de requête, alerte l'opérateur
                        if (table.GetCount() == 0)
                        {
                            MessageBox.Show(QueryDico.WarningEmptyTable);
                            return;
                        }
                        // Recherche les noms des requêtes à ajouter à la liste "QUERIES"
                        TableDefinition def = table.GetDefinition();
                        int idxTableFieldNOM = def.FindField(QueryDico.Name_Field);
                        var rc = table.Search();
                        try
                        {
                            Queries = new ObservableCollection<string>();
                            while (rc.MoveNext())
                            {
                                var record = rc.Current;
                                Queries.Add(record[idxTableFieldNOM].ToString());
                            }
                        }
                        // problème lors du chargement de la requete -> alerte l'opérateur
                        catch { MessageBox.Show(QueryDico.ProblemEditQuerie); }
                    });
                    //   });
                }, () => SelectedTable != null));
            }
        }

        /// <summary>
        /// Commande "Créer la gdb" 
        /// </summary>
        private ICommand _CmdCreateGDB;
        public ICommand CmdCreateGDB => _CmdCreateGDB ??= new RelayCommand(async () =>
        {
            try
            {
                // création de la gdb
                string folder = Path.GetDirectoryName(Project.Current.Path);
                NameGDB = QueryDico.query_default_name;
                // vide les champs "tables" et "requêtes"
                SelectedTable = null;
                SelectedQuery = null;
                ResultMessage = string.Empty;
                bool x = false;
                CancelableProgressorSource cps = new CancelableProgressorSource("création de la gdb", "Canceled");

                int numSecondsDelay = 5;
                //simulate doing some work which can be canceled
                await QueuedTask.Run(async () =>
                {
                    cps.Progressor.Max = (uint)numSecondsDelay;
                    // check every second
                    while (!cps.Progressor.CancellationToken.IsCancellationRequested)
                    {
                        cps.Progressor.Message = QueryDico.ProgressorMessage;
                        log.Debug(string.Format("RunCancelableProgress Loop{0}", cps.Progressor.Value));

                        x = await Constructgdb(folder, QueryDico.query_default_name);
                        if (x) break;
                        else
                        {
                            MessageBox.Show(QueryDico.ProblemConstructGDB);
                            break;
                        }
                    }
                    log.Debug(string.Format("RunCancelableProgress: Canceled {0}",
                        cps.Progressor.CancellationToken.IsCancellationRequested));

                }, cps.Progressor);
                cps.Dispose();
            }
            catch
            {
                MessageBox.Show(QueryDico.ProblemConstructGDB);
            }
        });

        /// <summary>
        /// Commande 'Importer une database'
        /// </summary>
        private ICommand _CmdImportGDB;
        public ICommand CmdImportGDB => _CmdImportGDB ?? (_CmdImportGDB = new RelayCommand(() =>
        {
            // ouverture de la boite de dialogue du choix de la database existante à importer
            OpenItemDialog openDialog = new OpenItemDialog();
            openDialog.InitialLocation = string.IsNullOrEmpty(DataPath) ? @Project.Current.ToString() : DataPath;
            openDialog.BrowseFilter = new BrowseProjectFilter("esri_browseDialogFilters_geodatabases_file");
            var result = openDialog.ShowDialog();
            if (result is true)
            {
                foreach (Item it in openDialog.Items)
                {
                    var errValidation = QueryUtil.ValidateDataPath(it.Path);
                    if (!string.IsNullOrEmpty(errValidation)) MessageBox.Show(errValidation, "Error");
                    else
                    {
                        DataPath = it.Path;
                        QueryUtil.nom_query = Path.GetFileNameWithoutExtension(it.Path);
                    }
                    break;
                }
                // si la gdb à importer est déjà dans le dossier, on l'importe, sinon, il faut copier le dossier gdb à la racine de notre projet.
                var folder = Path.GetDirectoryName(Project.Current.Path);
                if (!DataPath.Contains(folder)) QueryUtil.CopyDir(DataPath, folder);
                //import
                QueuedTask.Run(() =>
                {
                    IProjectItem item = ItemFactory.Instance.Create(folder + "\\" + QueryUtil.nom_query + ".gdb") as IProjectItem;
                    // if it is succesfully added to the project, return it otherwise null
                    Project.Current.AddItem(item);
                });
            }
            // nom de la gdb, et vide les listes des boutons "Tables" et "Requêtes"
            NameGDB = QueryUtil.nom_query;
            SelectedTable = null;
            SelectedQuery = null;
            ResultMessage = string.Empty;
        }));
    }
    #endregion Les Boutons

    /// <summary>
    /// Button implementation to show the DockPane.
    /// </summary>
    internal class QueryDockpane_ShowButton : Button
{
    protected override void OnClick()
    {
        QueryDockpaneViewModel.Show();
    }
}
}
