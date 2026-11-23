//   Copyright 2019 Esri
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at

using ArcGIS.Core.Data;
using ArcGIS.Core.Internal.Geometry;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Internal.Catalog;
using ArcGIS.Desktop.Mapping;
using IGN.Inca.ArcGISPro.SDK.Commons;
using IncaDisplay._QueryManager;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Resources;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Button = ArcGIS.Desktop.Framework.Contracts.Button;
using QueryFilter = ArcGIS.Core.Data.QueryFilter;
using Table = ArcGIS.Core.Data.Table;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

namespace IncaDisplay._EditQueue

{
    internal class QueueEditionViewModel : DockPane, INotifyPropertyChanged
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private const string DockPaneId = "ID_QueueEditionDockpane";

        // méthodes pour les raccourcis clavier.
        public static RoutedCommand CmdWorkNext { get; } = new RoutedCommand();
        public static RoutedCommand CmdWorkBack { get; } = new RoutedCommand();
        public static RoutedCommand CmdWorkStart { get; } = new RoutedCommand();
        public static RoutedCommand CmdWorkEnd { get; } = new RoutedCommand();
        public static RoutedCommand CmdWorkDeleteObj { get; } = new RoutedCommand();
        public static RoutedCommand CmdWorkSortingList { get; } = new RoutedCommand();
        public static RoutedCommand CmdWorkOnObject { get; } = new RoutedCommand();
        public static RoutedCommand CmdWorkZoomOnObject { get; } = new RoutedCommand();
        public static RoutedCommand CmdWorkOrderBy { get; } = new RoutedCommand();
        public static RoutedCommand CmdWorkOrderByDescending { get; } = new RoutedCommand();

        public QueueEditionViewModel()
        {
            // get the dictionary of the current UI language ("CurrentUICulture") using the Culture Resources Manager
            Translator = CultureResourcesManager.GetInstance().dictLanguage;

            // initialize "inca_logger_sdk" logger
            LoggerUtils.GetInstance().InitializeLogger();

            // liste des commandes pour les raccourcis clavier
            CommandManager.RegisterClassCommandBinding(typeof(QueueEditionView), new CommandBinding(CmdWorkNext, ExecuteNext, CanExecuteNext));
            CommandManager.RegisterClassCommandBinding(typeof(QueueEditionView), new CommandBinding(CmdWorkBack, ExecuteBack, CanExecuteBack));
            CommandManager.RegisterClassCommandBinding(typeof(QueueEditionView), new CommandBinding(CmdWorkStart, ExecuteStart, CanExecuteStart));
            CommandManager.RegisterClassCommandBinding(typeof(QueueEditionView), new CommandBinding(CmdWorkEnd, ExecuteEnd, CanExecuteEnd));
            CommandManager.RegisterClassCommandBinding(typeof(QueueEditionView), new CommandBinding(CmdWorkDeleteObj, ExecuteDeleteObj, CanExecuteDeleteObj));
            CommandManager.RegisterClassCommandBinding(typeof(QueueEditionView), new CommandBinding(CmdWorkOnObject, ExecuteOnobject, CanExecuteOnobject));
            CommandManager.RegisterClassCommandBinding(typeof(QueueEditionView), new CommandBinding(CmdWorkZoomOnObject, ExecuteZoomOnObject, CanExecuteZoomOnObject));
            CommandManager.RegisterClassCommandBinding(typeof(QueueEditionView), new CommandBinding(CmdWorkOrderBy, ExecuteOrderBy, CanExecuteOrderBy));
            CommandManager.RegisterClassCommandBinding(typeof(QueueEditionView), new CommandBinding(CmdWorkOrderByDescending, ExecuteOrderByDescending, CanExecuteOrderByDescending));
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
            var pane = FrameworkApplication.DockPaneManager.Find(DockPaneId);
            pane?.Activate();
        }

        /// <summary>
        /// Text shown near the top of the DockPane.
        /// </summary>
        private string _heading = "Queue d'édition";

        RowCursor TableCursor;
        Table _table;
        string oid = "";
        string featureName = "";
        // liste qui contient l'ensemble des couples (OID , classe) de la Table à parcourir
        List<KeyValuePair<object, string>> List_INPUTID_Feature = new List<KeyValuePair<object, string>>();
        public static string nom_map = "";
        public static string selected_sort_choice = "";
        public string Heading
        {
            get { return _heading; }
            set { SetProperty(ref _heading, value, () => Heading); }
        }

        private int _ItemID = 0;
        public int ItemID
        {
            get { return _ItemID; }
            set
            {
                _ItemID = value;
                NotifyPropertyChanged(new PropertyChangedEventArgs("ItemID"));
            }
        }

        private int _ItemALL = 0;
        public int ItemALL
        {
            get { return _ItemALL; }
            set
            {
                SetProperty(ref _ItemALL, value, () => ItemALL);
            }
        }

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

        private string _selectedTable = string.Empty;
        public string SelectedTable
        {
            get { return _selectedTable; }
            set
            {
                SetProperty(ref _selectedTable, value, () => SelectedTable);
                AccesTable();
            }
        }

        private string _gdbPath = string.Empty;
        public string GdbPath
        {
            get { return _gdbPath; }
            set { SetProperty(ref _gdbPath, value, () => GdbPath); }
        }

        private ObservableCollection<string> _choises;
        public ObservableCollection<string> Choises
        {
            get { return _choises; }
            set
            {
                _choises = value;
                NotifyPropertyChanged(new PropertyChangedEventArgs("Choises"));
            }
        }
        public FeatureClass featureclass_choose = null;

        private string _selectedChoise = string.Empty;
        public string SelectedChoise
        {
            get { return _selectedChoise; }
            set { SetProperty(ref _selectedChoise, value, () => _selectedChoise); }
        }

        private string _FillWithSortResult = string.Empty;
        public string FillWithSortResult
        {
            get => _FillWithSortResult; 
            set
            {
                _FillWithSortResult = value;
                NotifyPropertyChanged(new PropertyChangedEventArgs("FillWithSortResult")); 
            }
        }
        #endregion

        #region Dictionnaire 
        /// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ START ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        ///     One-Way Data Binding Properties, used specially for internationalization of the UI
        /// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        public string FillWithSelection { get { return Translator.GetString("FillWithSelection"); } }
        public string FillWithTable { get { return Translator.GetString("FillWithTable"); } }
        public string RemoveOne { get { return Translator.GetString("RemoveOne"); } }
        public string RemoveAll { get { return Translator.GetString("RemoveAll"); } }
        public string DeleteObj { get { return Translator.GetString("DeleteObj"); } }
        public string SortingList { get { return Translator.GetString("SortingList"); } }
        public string OnObject { get { return Translator.GetString("OnObject"); } }
        public string OrderBy { get { return Translator.GetString("OrderBy"); } }
        public string OrderByDescending { get { return Translator.GetString("OrderByDescending"); } }
        public string ZoomOnObject { get { return Translator.GetString("ZoomOnObject"); } }
        public string TT_MoveNext { get { return Translator.GetString("TT_MoveNext"); } }
        public string TT_MoveBack { get { return Translator.GetString("TT_MoveBack"); } }
        public string TT_MoveStart { get { return Translator.GetString("TT_MoveStart"); } }
        public string TT_MoveEnd { get { return Translator.GetString("TT_MoveEnd"); } }
        public string TT_RemoveOne { get { return Translator.GetString("TT_RemoveOne"); } }
        public string TT_RemoveAll { get { return Translator.GetString("TT_RemoveAll"); } }
        public string TT_DeleteObj { get { return Translator.GetString("TT_DeleteObj"); } }
        #endregion

        #region Fonctions
        // Valide l'accès à la table et l'existence de l'objet, retourne la carte sur laquelle s'applique la recherche
        public Map AccesTable()
        {
            if (SelectedTable != string.Empty)
            {
                Map map = null;
                GDBProjectItem gdbProjectItem = Project.Current.GetItems<GDBProjectItem>().FirstOrDefault(l => l.Name.Contains(QueryUtil.nom_query));
                QueuedTask.Run(() =>
                {
                    _table = (gdbProjectItem.GetDatastore() as Geodatabase).OpenDataset<Table>(SelectedTable.ToString().Insert(0, "result_"));
                    //création de la liste 
                    if (List_INPUTID_Feature is null || List_INPUTID_Feature.Count() == 0) creerListe(_table);
                    if (nom_map == "") nom_map = MapView.Active.Map.Name;
                    MapProjectItem mapProjItem = Project.Current.GetItems<MapProjectItem>().First(item => item.Name.Equals(nom_map));
                    // Récupérer la map stockee dans la table.
                    map = mapProjItem.GetMap();

                    /* *  TestOIDExist -> Remplacé par un bloc de test par liste pour être plus réactif 
                     * *  et gagner du temps de parcours sur des listes très longues
                    List<KeyValuePair<object, string>> list_temp = List_INPUTID_Feature.ToList();
                    // validation de l'existence des objets
                    foreach (var t in list_temp) TestOIDExist(t.Value, t.Key.ToString(), map);
                    * */

                    // validation de l'existence des objets
                    CheckOIDsInFeatures(map);

                    // affectation du compte total d'objets
                    ItemALL = (int)_table.GetCount();
                    if (ItemID > ItemALL) ItemID = 0;
                });
                return map;
            }
            else
            {
                ItemALL = ItemID = 0;
                List_INPUTID_Feature.Clear();
                return null;
            }
        }

        protected void CheckOIDsInFeatures(Map map)
        {
            try
            {
                // validation de l'existence des objets
                // List_INPUTID_Feature contient les couples (OID, classe).
                // On souhaite tester l'existence de tous les OID présents dans cette liste pour chaque classe.
                // On reclasse le dictionnaire pour avoir les infos dans le sens (classe : OIDs)
                Dictionary<string, List<object>> dictionary = List_INPUTID_Feature
                .GroupBy(pair => pair.Value) // Grouper par la valeur de la deuxième colonne (string)
                                             //.Where(g => g.Count() > 1) // Garder les groupes où il y a des valeurs identiques
                .ToDictionary(
                    g => g.Key, // Clé : la classe
                    g => g.Select(pair => pair.Key).ToList() // Valeur : liste des oid correspondants
                );

                // pour chaque classe de la table, on vérifie que les OIDs de la table sont toujours présents dans la classe 
                foreach (var entry in dictionary)
                {
                    // la classe
                    string featureClass = entry.Key;
                    //la liste des OIDs à tester
                    List<object> list_OID_table_feature = entry.Value;
                    // la même liste mais avec des longs pour pouvoir la comparer avec la liste des OIDs de la classe
                    List<long> list_OID_table_feature_long = list_OID_table_feature.Select(e => long.Parse(e.ToString())).ToList();

                    // récupère la liste des OID de cette classe :
                    List<long> list_OID_this_feature = GetObjectIDs(map, featureClass);

                    // les élements présents dans les 2 listes
                    var intersect_long = list_OID_table_feature_long.Intersect(list_OID_this_feature).ToList();

                    // on copie la liste pour ne garder que l'écart etntre les 2 listes
                    List<object> diff_oid_feature_table = list_OID_table_feature;
                    // on enlève les OID qui sont seulement présents dans la table et pas dans la classe
                    diff_oid_feature_table.RemoveAll(e => intersect_long.Contains(long.Parse(e.ToString())));

                    // s'il y a un écart entre les 2 listes, on enlève les élement en trop dans la table, pour ne pas passer dessus lors du parcous
                    if (diff_oid_feature_table.Count > 0)
                    {
                        List<long> diff_oid_long = diff_oid_feature_table.Select(e => long.Parse(e.ToString())).ToList();
                        // enlève l'élément de la liste, enlève l'élément de la table
                        // requete pour enlever les élements d'une table
                        QueryFilter qft = new QueryFilter();
                        string filter = "";
                        if (diff_oid_long.Count == 1) filter = " = '" + diff_oid_long[0].ToString() + "'";
                        else
                        {
                            filter = string.Join(", ", diff_oid_long.Select(x => "'" + x.ToString() + "'"));
                            filter = " IN (" + filter + ")";
                        }

                        // supprime de la table
                        qft.WhereClause = QueryDico.inputID_Field + filter;
                        _table.DeleteRows(qft);

                        // supprime de la liste de parcours
                        foreach (var x in diff_oid_feature_table)
                        {
                            KeyValuePair<object, string> obj = new KeyValuePair<object, string>(x, featureClass);
                            if (List_INPUTID_Feature.Any(k => k.Equals(obj)))
                                List_INPUTID_Feature.Remove(obj);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Gérer les exceptions
                Console.WriteLine("Erreur : " + ex.Message);
            }
        }

        private static List<long> GetObjectIDs(Map map, string featureClassPath)
        {
            // Liste pour stocker les ObjectIDs
            List<long> objectIds = new List<long>();

            QueuedTask.Run(() =>
            {
                // Recherchce le nom de la classe
                if (featureClassPath.Contains("/"))
                    featureClassPath = featureClassPath.Split('/')[featureClassPath.Split('/').Count() - 1];

                // Et la couche associée dans la carte
                Layer fl = map.GetLayersAsFlattenedList().OfType<Layer>().
                    FirstOrDefault(l => l.Name == featureClassPath);

                if (fl != null)
                {
                    Selection temp = null;
                    if (fl is FeatureLayer)
                        temp = (fl as FeatureLayer).Select();
                    if (fl is AnnotationLayer)
                        temp = (fl as AnnotationLayer).Select();

                    if (temp != null)
                    {
                        objectIds = temp.GetObjectIDs().ToList();
                        map.ClearSelection();
                    }
                }
            });
            return objectIds;
        }

        /// <summary>
        /// Crée une liste pour la navigation au sein de la table
        /// oid / classe
        /// </summary>
        /// <param name="the_table"></param>
        void creerListe(Table the_table = null)
        {
            if (SelectedTable != string.Empty)
            {
                if (the_table is null)
                // la database et la table
                {
                    GDBProjectItem gdbProjectItem = Project.Current.GetItems<GDBProjectItem>().FirstOrDefault(l => l.Name.Contains(QueryUtil.nom_query));
                    the_table = (gdbProjectItem.GetDatastore() as Geodatabase).OpenDataset<Table>(SelectedTable.ToString().Insert(0, "result_"));
                }
                List<KeyValuePair<object, string>> List_temp = new List<KeyValuePair<object, string>>();

                // parcours de la table pour remplir la liste avec l'id et la classe de chaque entrée
                TableCursor = the_table.Search();
                while (TableCursor.MoveNext())
                {
                    using (var row = TableCursor.Current)
                    {
                        nom_map = row[_table.GetDefinition().FindField(QueryDico.Carte_Field)].ToString();
                        List_temp.Add(new KeyValuePair<object, string>(row[the_table.GetDefinition().FindField(QueryDico.inputID_Field)],
                            row[the_table.GetDefinition().FindField(QueryDico.Classe_Field)].ToString()));
                    }
                }
                // affecte à la liste générique
                List_INPUTID_Feature = List_temp;
            }
        }

        // Button implementation to 
        internal class TableInfo
        {
            /// <summary>
            /// Name
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// table definition
            /// </summary>
            public TableDefinition Definition { get; set; }
        }

        // Test l'existence de l'objet d'OID oid dans la couche "featureName" de la vue active
        bool TestOIDExist(string featureName, string oid, Map map)
        {
            bool Existe = false;
            if (featureName.Contains("/"))
                featureName = featureName.Split('/')[featureName.Split('/').Count() - 1];
            FeatureClass _feature = null;
            if (map.GetLayersAsFlattenedList().OfType<FeatureLayer>().FirstOrDefault(l => l.Name == featureName) != null)
            {
                // la couche avec le nom "featureName"
                FeatureLayer fl = map.GetLayersAsFlattenedList().OfType<FeatureLayer>().FirstOrDefault(l => l.Name == featureName);
                // la classe associée
                _feature = fl.GetFeatureClass();
            }
            else if (map.GetLayersAsFlattenedList().OfType<AnnotationLayer>().FirstOrDefault(l => l.Name == featureName) != null)
            {
                // la couche avec le nom "featureName"
                AnnotationLayer al = map.GetLayersAsFlattenedList().OfType<AnnotationLayer>().FirstOrDefault(l => l.Name == featureName);
                // la classe associée
                _feature = al.GetFeatureClass();
            }

            // l'index du champ "OBJECTID"
            var OidField = _feature.GetDefinition().GetFields().Where(x => x.FieldType.ToString() == "OID").First().Name;
            var idxfeaturefieldoid = _feature.GetDefinition().FindField(OidField);
            // Parcours de la classe pour trouver l'objet avec l'objectid demandé
            RowCursor featureCursor = _feature.Search();
            featureCursor.MoveNext();
            Row row = featureCursor.Current;
            QueryFilter qf = new QueryFilter();
            qf.WhereClause = OidField + " = " + oid;//qf.WhereClause = "OBJECTID = " + oid;            
            RowCursor cursorRow = _feature.Search(qf);
            cursorRow.MoveNext();

            if (cursorRow.Current is null) Existe = false;
            // s'il existe, renvoie vrai
            else
            {
                row = cursorRow.Current;
                if (row[idxfeaturefieldoid].ToString().Equals(oid)) { Existe = true; }
            }
            // sinon, enlève l'élément de la liste, enlève l'élément de la table et renvoie faux
            if (Existe is false)
            {
                int i = List_INPUTID_Feature.IndexOf(List_INPUTID_Feature.Find(x => x.Key.ToString() == oid));
                QueryFilter qft = new QueryFilter();
                qft.WhereClause = QueryDico.inputID_Field + " = '" + oid + "'";

                var cursortableRow = _table.Search(qft);
                cursortableRow.MoveNext();
                Row rowT = cursortableRow.Current;
                List_INPUTID_Feature.Remove(List_INPUTID_Feature[i]);
                // supprime de la table 
                rowT.Delete();
                cursortableRow.Dispose();
            }
            cursorRow.Dispose();
            return Existe;
        }
        #endregion Fonctions

        #region boutons
        /// <summary>
        /// Button implementation to redirect on the selected object
        /// Commande pour recentrer sur l'objet courant
        /// </summary>
        private void CanExecuteOnobject(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = SelectedTable != null;
        }
        public void ExecuteOnobject(object sender, ExecutedRoutedEventArgs e)
        {
            QueuedTask.Run(() =>
            {
                // si la table est sélectionnée et que la sélection est active
                if (SelectedTable != string.Empty)// || MapView.Active.Map.SelectionCount != 0)
                {
                    // si le curseur n'est pas à 0
                    if (ItemID != 0 && ItemID < ItemALL)
                    {
                        // si le nom de la carte est renseigné
                        if (nom_map != "")
                        {
                            // accède à la table
                            List<object> temp = new List<object>();
                            Map map = AccesTable();
                            TableCursor = _table.Search();
                            TableCursor.MoveNext();
                            // sinon, recherche l'objet de la classe correspondant au rang de la liste
                            // et se recentre dessus.
                            oid = List_INPUTID_Feature[ItemID - 1].Key.ToString();
                            featureName = List_INPUTID_Feature[ItemID - 1].Value.ToString();

                            QueryUtil.Selection_by_OID(featureName, oid, map);
                            var mp = map.GetMapPanes().First().MapView;
                            mp.PanToSelected();

                        }
                    }
                }
            });
        }
        /// <summary>
        /// Button implementation to zoom on the selected object
        /// Commande pour recentrer sur l'objet courant
        /// </summary>
        private void CanExecuteZoomOnObject(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = SelectedTable != null && SelectedTable != string.Empty && ItemID != 0;
        }
        public void ExecuteZoomOnObject(object sender, ExecutedRoutedEventArgs e)
        {
            QueuedTask.Run(() =>
            {
                // si le nom de la carte est renseigné
                if (nom_map != "")
                {
                    // accède à la table
                    List<object> temp = new List<object>();
                    Map map = AccesTable();
                    TableCursor = _table.Search();
                    TableCursor.MoveNext();
                    // sinon, recherche l'objet de la classe correspondant au rang de la liste
                    // et se recentre dessus.
                    oid = List_INPUTID_Feature[ItemID - 1].Key.ToString();
                    featureName = List_INPUTID_Feature[ItemID - 1].Value.ToString();

                    QueryUtil.Selection_by_OID(featureName, oid, map);
                    MapView mv = map.GetMapPanes().First().MapView;
                    var selection = mv.Map.GetSelection().ToDictionary();
                    foreach (var kvp in selection)
                    {
                        var layer = kvp.Key;
                        var oids = kvp.Value;
                        FeatureClass ft = null;
                        if (layer is FeatureLayer fl) ft = fl.GetFeatureClass();
                        else if (layer is AnnotationLayer al) ft = al.GetFeatureClass();

                        var OidField = ft.GetDefinition().GetFields().Where(x => x.FieldType.ToString() == "OID").First().Name;
                        var idxfeaturefieldoid = ft.GetDefinition().FindField(OidField);
                        QueryFilter qf = new QueryFilter();
                        if (oids.Count > 0)
                        {
                            qf.WhereClause = $"{OidField} = {oid}";

                            RowCursor cursorRow = ft.Search(qf);
                            cursorRow.MoveNext();
                            Row row = cursorRow.Current;

                            var shape = ((Feature)row).GetShape();
                            var extent = shape.Extent;

                            // Définir une marge (par exemple 10 % de l'étendue)
                            double marginFactor = .8;
                            double expandX = extent.Width * marginFactor;
                            double expandY = extent.Height * marginFactor;

                            var expandedExtent = EnvelopeBuilder.CreateEnvelope(
                            extent.XMin - expandX,
                            extent.YMin - expandY,
                            extent.XMax + expandX,
                            extent.YMax + expandY,
                            extent.SpatialReference
                            );
                            mv.ZoomToAsync(expandedExtent, new TimeSpan(0, 0, 1));
                            break; // Zoom sur le premier objet trouvé
                        }
                    }
                }
            });
        }


        /// <summary>
        /// button implementation one at the back  
        /// commande de retour d'un pas
        /// </summary>
        private void CanExecuteBack(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = SelectedTable != null && (ItemID != 1 ||ItemID !=0);
        }
        private void ExecuteBack(object sender, ExecutedRoutedEventArgs e)
        {
            QueuedTask.Run(() =>
            {
                if (SelectedTable != string.Empty)
                {
                    try
                    {
                        // déplacement seulement si l'on n'est pas sur le premier objet
                        if (ItemID > 1)
                        {
                            Map map = null;
                            // si le nom de la carte est renseigné
                            if (nom_map != "")
                            {
                                // parcours la table pour chercher l'élément suivant s'il existe
                                TableCursor = _table.Search();
                                TableCursor.MoveNext();
                                Row row = TableCursor.Current;

                                var oid_temp = List_INPUTID_Feature[ItemID - 1].Key;
                                var featureName_temp = List_INPUTID_Feature[ItemID - 1].Value.ToString();
                                List<KeyValuePair<object, string>> List_INPUTID_Feature_temp = new List<KeyValuePair<object, string>>(List_INPUTID_Feature);
                                map = AccesTable();
                                List<KeyValuePair<object, string>> diff = List_INPUTID_Feature_temp.Except(List_INPUTID_Feature).ToList();

                                if (diff.Count() == 0)
                                {
                                    ItemID -= 1;
                                    oid = List_INPUTID_Feature[ItemID - 1].Key.ToString();
                                    featureName = List_INPUTID_Feature[ItemID - 1].Value.ToString();
                                }
                                else
                                {
                                    bool test = false;
                                    while (ItemID > 0 && !test)
                                    {
                                        ItemID -= 1;
                                        if (List_INPUTID_Feature[ItemID - 1].Equals(List_INPUTID_Feature_temp[ItemID - 1]))
                                        {
                                            //affectation, selection, et reinitialisation des indices
                                            oid = List_INPUTID_Feature[ItemID - 1].Key.ToString();
                                            featureName = List_INPUTID_Feature[ItemID - 1].Value.ToString();
                                            test = true;
                                        }
                                    }
                                }
                            }
                            QueryUtil.Selection_by_OID(featureName, oid, map);
                            var mp = map.GetMapPanes().First().MapView;
                            mp.PanToSelected();
                        }
                    }
                    catch
                    {
                        MessageBox.Show(QueryDico.ProblemParcours, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }

                }
            });
        }

        /// <summary>
        /// Button implementation to one in front
        /// Commande avance d'un pas
        /// </summary>
        private void CanExecuteNext(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = SelectedTable != null && ItemID != ItemALL;
        }
        private void ExecuteNext(object sender, ExecutedRoutedEventArgs e)
        {
            QueuedTask.Run(() =>
            {
                if (SelectedTable != string.Empty)
                {
                    try
                    {
                        // déplacement seulement si l'on n'est pas sur le dernier objet
                        if (ItemID < _table.GetCount())
                        {
                            Map map = null;
                            // si le nom de la carte est renseigné
                            if (nom_map != "")
                            {
                                // parcours la table pour chercher l'élément suivant s'il existe
                                TableCursor = _table.Search();
                                TableCursor.MoveNext();
                                Row row = TableCursor.Current;
                                if (oid == "" || ItemID == 0)
                                {
                                    oid = List_INPUTID_Feature[ItemID].Key.ToString();
                                    featureName = List_INPUTID_Feature[ItemID].Value.ToString();
                                    ItemID += 1;
                                    map = Project.Current.GetItems<MapProjectItem>().First(item => item.Name.Equals(nom_map)).GetMap();
                                }
                                else
                                {
                                    if (ItemID > 0)
                                    {
                                        var oid_temp = List_INPUTID_Feature[ItemID - 1].Key;
                                        var featureName_temp = List_INPUTID_Feature[ItemID - 1].Value.ToString();
                                        List<KeyValuePair<object, string>> List_INPUTID_Feature_temp = new List<KeyValuePair<object, string>>(List_INPUTID_Feature);
                                        map = AccesTable();
                                        List<KeyValuePair<object, string>> diff = List_INPUTID_Feature_temp.Except(List_INPUTID_Feature).ToList();

                                        if (diff.Count() == 0)
                                        {
                                            oid = List_INPUTID_Feature[ItemID].Key.ToString();
                                            featureName = List_INPUTID_Feature[ItemID].Value.ToString();
                                            ItemID += 1;
                                        }
                                        else
                                        {
                                            bool test = false;
                                            while (ItemID > 1 && !test)
                                            {
                                                ItemID -= 1;
                                                if (List_INPUTID_Feature[ItemID - 1].Equals(List_INPUTID_Feature_temp[ItemID - 1]))
                                                {
                                                    //affectation, selection, et réinitialisation des indices
                                                    ItemID += 1;
                                                    //oid = List_INPUTID_Feature[ItemID - 1].Key.ToString();
                                                    //featureName = List_INPUTID_Feature[ItemID - 1].Value.ToString();
                                                    test = true;
                                                }
                                            }
                                            if (test == false) ItemID = 1;

                                            //affectation, selection, et réinitialisation des indices
                                            oid = List_INPUTID_Feature[ItemID - 1].Key.ToString();
                                            featureName = List_INPUTID_Feature[ItemID - 1].Value.ToString();
                                        }
                                    }
                                }
                            }
                            QueryUtil.Selection_by_OID(featureName, oid, map);
                            var mp = map.GetMapPanes().First().MapView;
                            mp.PanToSelected();
                        }
                    }
                    catch
                    {
                        MessageBox.Show(QueryDico.ProblemParcours, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            });
        }

        /// <summary>
        /// Button implementation to back to start
        /// commande retour au début de la liste
        /// </summary>
        private void CanExecuteStart(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = SelectedTable != null && (ItemID != 1 || ItemID != 0);
        }
        private void ExecuteStart(object sender, ExecutedRoutedEventArgs e)
        {
            QueuedTask.Run(() =>
             {
                 if (SelectedTable != string.Empty)
                 {
                     // accès à la table
                     Map map = AccesTable();
                     // retour au début à partir du rang 2
                     if (ItemID > 1)
                     {
                         // définition du curseur pour parcours la table
                         TableCursor = _table.Search();
                         TableCursor.MoveNext();
                         Row row = TableCursor.Current;

                         // recherche du rang du premier objet existant
                         bool test = false;
                         while (test is false)
                         {
                             // recherche du rang du dernier objet existant
                             var id_first = List_INPUTID_Feature.First().Key;
                             var fc_first = List_INPUTID_Feature.First().Value;
                             QueryFilter qf = new QueryFilter();

                             //requête pour chercher dans la classe, id du dernier rang
                             qf.WhereClause = QueryDico.Classe_Field + "= '" + fc_first + "'  And " + QueryDico.inputID_Field + " = '" + id_first + "'";
                             var cursorRow = _table.Search(qf);
                             cursorRow.MoveNext();
                             row = cursorRow.Current;
                             oid = row[_table.GetDefinition().FindField(QueryDico.inputID_Field)].ToString();

                             var oid_ = row[_table.GetDefinition().FindField(QueryDico.inputID_ALIAS)].ToString();
                             featureName = row[_table.GetDefinition().FindField(QueryDico.Classe_Field)].ToString();
                             // si l'objet avec cet oid existe toujours dans la database, on sort
                             if (TestOIDExist(featureName, oid, map) is true) { test = true; }
                             // sinon test le suivant.
                             else
                             {
                                 // suppression du rang dans la table, mise à jour des compteurs.
                                 int i = List_INPUTID_Feature.IndexOf(List_INPUTID_Feature.Find(x => x.Key.ToString() == oid));
                                 List_INPUTID_Feature.Remove(List_INPUTID_Feature[i]);
                                 ItemALL -= 1;
                             }
                         }
                         //si l'objet est bien présent
                         if (test is true)
                         {
                             // affectation, sélection, et réinitialisation des indices
                             QueryUtil.Selection_by_OID(featureName, oid, map);
                             var mp = map.GetMapPanes().First().MapView;
                             mp.PanToSelected();
                             ItemID = 1;
                         }
                     }
                 }
             });

        }

        /// <summary>
        /// Button implementation to go to the end
        /// Commande pour aller en fin de liste
        /// </summary>
        private void CanExecuteEnd(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = SelectedTable != null && ItemID != ItemALL;
        }
        private void ExecuteEnd(object sender, ExecutedRoutedEventArgs e)
        {
            QueuedTask.Run(() =>
            {
                if (SelectedTable != string.Empty)
                {
                    // accès à la table
                    Map map = AccesTable();
                    // si pas déjà sur le dernier
                    if (ItemID < _table.GetCount())
                    {
                        // si le nom de la carte est renseigné
                        if (nom_map != "")
                        {
                            // Curseur pour trouver le dernier élément de la liste
                            TableCursor = _table.Search();
                            TableCursor.MoveNext();
                            Row row = TableCursor.Current;

                            bool test = false;
                            while (test is false)
                            {
                                // recherche du rang du dernier objet existant
                                var id_last = List_INPUTID_Feature.Last().Key;
                                var fc_last = List_INPUTID_Feature.Last().Value;
                                QueryFilter qf = new QueryFilter();

                                //requête pour chercher dans la classe, id du dernier rang
                                qf.WhereClause = QueryDico.Classe_Field + "= '" + fc_last + "'  And " + QueryDico.inputID_Field + " = '" + id_last + "'";

                                var cursorRow = _table.Search(qf);
                                cursorRow.MoveNext();
                                row = cursorRow.Current;

                                // affectation, sélection, et réinitialisation des indices
                                featureName = row[_table.GetDefinition().FindField(QueryDico.Classe_ALIAS)].ToString();
                                oid = row[_table.GetDefinition().FindField(QueryDico.inputID_ALIAS)].ToString();

                                if (TestOIDExist(featureName, oid, map) is true) { test = true; }
                                else
                                {
                                    // suppression du rang dans la table, mise à jour des compteurs.
                                    int i = List_INPUTID_Feature.IndexOf(List_INPUTID_Feature.Find(x => x.Key == id_last));
                                    List_INPUTID_Feature.Remove(List_INPUTID_Feature[i]);
                                    ItemALL -= 1;
                                    row = cursorRow.Current;
                                }
                            }
                            //si l'objet est bien présent
                            if (test is true)
                            {
                                // affectation, sélection, et réinitialisation des indices
                                oid = row[_table.GetDefinition().FindField(QueryDico.inputID_ALIAS)].ToString();
                                featureName = row[_table.GetDefinition().FindField(QueryDico.Classe_ALIAS)].ToString();
                                // affectation, sélection, et réinitialisation des indices
                                QueryUtil.Selection_by_OID(featureName, oid, map);
                                var mp = map.GetMapPanes().First().MapView;
                                mp.PanToSelected();

                                ItemID = ItemALL;
                            }
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Button implementation delete an item from the list
        /// Commande 
        /// </summary>
        private ICommand _CmdWorkremoveone;
        public ICommand CmdWorkremoveone
        {
            get
            {
                return _CmdWorkremoveone ?? (_CmdWorkremoveone = new RelayCommand(() =>
                {
                    QueuedTask.Run(() =>
                    {
                        if (SelectedTable != string.Empty)
                        {
                            // accès à la table
                            Map map = AccesTable();

                            // on peut en enlever 1 si on a commencé à circuler dans la liste et si la liste compte plus d'un élément.
                            if (ItemID > 0 && ItemALL > 1)
                            {
                                // recherche de l'oid et parcours de la table
                                oid = List_INPUTID_Feature[ItemID - 1].Key.ToString();
                                featureName = List_INPUTID_Feature[ItemID - 1].Value.ToString();

                                TableCursor = _table.Search();
                                TableCursor.MoveNext();
                                Row row = TableCursor.Current;
                                QueryFilter qf = new QueryFilter();
                                qf.WhereClause = QueryDico.inputID_Field + " = '" + oid + "'";
                                var cursorRow = _table.Search(qf);
                                cursorRow.MoveNext();
                                var t = cursorRow.Current;
                                if (TestOIDExist(featureName, oid, map) is true)
                                {
                                    // affectation, sélection et réinitialisation des indices
                                    t.Delete();
                                    int i = List_INPUTID_Feature.IndexOf(List_INPUTID_Feature.Find(x => x.Key.ToString() == oid));
                                    List_INPUTID_Feature.Remove(List_INPUTID_Feature[i]);
                                    MapView.Active.Map.ClearSelection();
                                    if (ItemID > 1) ItemID -= 1;
                                    oid = List_INPUTID_Feature[ItemID - 1].Key.ToString();
                                    ItemALL -= 1;
                                    QueryUtil.Selection_by_OID(featureName, oid, map);
                                    var mp = map.GetMapPanes().First().MapView;
                                    mp.PanToSelected();
                                }
                            }
                            // s'il n'y a qu'un élément dans la liste.
                            else if (ItemID == ItemALL && ItemALL == 1)
                            {
                                // remet les compteurs à zéro et vide la sélection
                                ItemID = 0; ItemALL = 0; oid = "";
                                MapView.Active.Map.ClearSelection();

                                // suppression de la table temporaire
                                if (SelectedTable == QueryDico.selection_default_name)
                                {
                                    Geodatabase geodatabase = Project.Current.GetItems<GDBProjectItem>().FirstOrDefault(l => l.Name.Contains(QueryUtil.nom_query)).GetDatastore() as Geodatabase;
                                    var arguments = Geoprocessing.MakeValueArray(geodatabase.GetPath().LocalPath.ToString() + "/" + SelectedTable);
                                    if (QueryUtil.TableExists(geodatabase, SelectedTable) is true)
                                        Geoprocessing.ExecuteToolAsync("Delete", arguments);
                                }

                                // vider la table active
                                SelectedTable = string.Empty;
                            }
                        }
                    });
                }, () => SelectedTable != null));
            }
        }


        /// <summary> 
        /// Button implementation to empty the list
        /// Commande vider la queue d'édition
        /// </summary>
        private ICommand _CmdWorkremoveall;
        public ICommand CmdWorkremoveall
        {
            get
            {
                return _CmdWorkremoveall ?? (_CmdWorkremoveall = new RelayCommand(async () =>
                {
                    if (SelectedTable != string.Empty)
                    {
                        await QueuedTask.Run(() =>
                        {
                            Map map = AccesTable();

                            // remet les compteurs à zéro et vide la sélection
                            ItemID = 0; ItemALL = 0; oid = "";
                            map.SetSelection(null);

                            // suppression de la table temporaire
                            try
                            {
                                Geodatabase geodatabase = Project.Current.GetItems<GDBProjectItem>().FirstOrDefault(l => l.Name.Contains(QueryUtil.nom_query)).GetDatastore() as Geodatabase;
                                var arguments = Geoprocessing.MakeValueArray(geodatabase.GetPath().LocalPath.ToString() + "/result_" + SelectedTable);
                                if (QueryUtil.TableExists(geodatabase, "result_" + SelectedTable) is true) Geoprocessing.ExecuteToolAsync("Delete", arguments);
                            }
                            catch
                            {
                                MessageBox.Show(QueryDico.ProblemDeleteTable, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }

                            // vider la table active
                            SelectedTable = string.Empty;

                            // vider les choix du tri par champ
                            SelectedChoise = string.Empty;
                            Choises = new();
                            FillWithSortResult = "";
                        });
                    }
                    else
                    {
                        ItemALL = 0;
                        ItemID = 0;
                    }
                }, () => SelectedTable != null));
            }
        }


        /// <summary> 
        /// Button implementation to sort the list by a field
        /// Commande pour trier la liste par un champ
        /// </summary>
        private void CanExecuteOrderBy(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = SelectedTable != null && SelectedTable != "";
        }
        private void ExecuteOrderBy(object sender, ExecutedRoutedEventArgs e)
        {
            FillWithSortResult = "";
            selected_sort_choice = "Croissant";
            QueuedTask.Run(() =>
            {
                if (SelectedChoise != null || SelectedChoise != "")
                {                    
                    if (QueryUtil.nom_query is null) QueryUtil.nom_query = QueryDico.query_default_name;
                    // récupèrer la liste des classes de la table de la REQ.gdb
                    GDBProjectItem gdbProjectItem = Project.Current.GetItems<GDBProjectItem>().FirstOrDefault(l => l.Name.Contains(QueryUtil.nom_query));
                    Table the_table = (gdbProjectItem.GetDatastore() as Geodatabase).OpenDataset<Table>(SelectedTable.ToString().Insert(0, "result_"));
                     
                    if (featureclass_choose != null)
                        if (Sortlist_SelectedTable(List_INPUTID_Feature, featureclass_choose, the_table))
                            FillWithSortResult = QueryDico.MessageEndSortedList_asc;

                    //recharger la table : 
                    SelectedTable = string.Empty;
                    SelectedTable = the_table.GetName().Replace("result_", "");
                }
                else
                    MessageBox.Show("Impossible de trier des éléments de couches différentes.");
            });

        }

        public HashSet<string> GetSingleFeature()
        {
            HashSet<string> classesUniques = new HashSet<string>();
            List<string> parents = new List<string>();            
            string featureName = "";

            // Parcourir chaque couche
            foreach (var kvp in List_INPUTID_Feature)
            {
                if (kvp.Value.ToString().Contains("/"))
                {
                    parents = kvp.Value.Split('/').ToList();
                    featureName = parents[parents.Count() - 1];
                }
                else featureName = kvp.Value;
                
                Map map = MapView.Active.Map;
                // trouver la classe avec le nom
                var layer = map.GetLayersAsFlattenedList().OfType<Layer>().
                    Where(f => f.Name == featureName).First();
                //List<GroupLayer> groupLayers = map.GetLayersAsFlattenedList().OfType<GroupLayer>().ToList();

                FeatureClass featureClass = null;
                if (layer is FeatureLayer fl) featureClass = fl.GetFeatureClass();
                if (layer is AnnotationLayer al) featureClass = al.GetFeatureClass();

                // Ajouter le nom de la FeatureClass au HashSet
                if (featureClass != null)
                {
                    classesUniques.Add(featureClass.GetName());
                    featureclass_choose = featureClass;
                }
            }

            return classesUniques;
        }


        /// <summary> 
        /// Button implementation to sort the list by a field
        /// Commande pour trier la liste par un champ
        /// </summary>
        private void CanExecuteOrderByDescending(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = SelectedTable != null && SelectedTable != "";
        }
        private void ExecuteOrderByDescending(object sender, ExecutedRoutedEventArgs e)
        {
            FillWithSortResult = "";
            QueuedTask.Run(() =>

            { selected_sort_choice = "Décroissant";
                if (SelectedChoise != null || SelectedChoise != "")
                {
                   
                    if (QueryUtil.nom_query is null) QueryUtil.nom_query = QueryDico.query_default_name;
                    // récupèrer la liste des classes de la table de la REQ.gdb
                    GDBProjectItem gdbProjectItem = Project.Current.GetItems<GDBProjectItem>().FirstOrDefault(l => l.Name.Contains(QueryUtil.nom_query));
                    Table the_table = (gdbProjectItem.GetDatastore() as Geodatabase).OpenDataset<Table>(SelectedTable.ToString().Insert(0, "result_"));
  
                    if (featureclass_choose != null)
                        if (Sortlist_SelectedTable(List_INPUTID_Feature, featureclass_choose, the_table))                            
                            FillWithSortResult = QueryDico.MessageEndSortedList_des ;

                    //recharger la table : 
                    SelectedTable = string.Empty;
                    SelectedTable = the_table.GetName().Replace("result_", "");

                }
                else
                    MessageBox.Show("Impossible de trier des éléments de couches différentes.");
            });
        }


        /// <summary>
        /// fait le tri sur le champ sélectionné avec l'option tri choisie
        /// et réenregistre la table.
        /// </summary>
        public bool Sortlist_SelectedTable(List<KeyValuePair<object, string>> ListOIDFeature, FeatureClass feature, Table table)
        {
            try
            {
                QueuedTask.Run(async () =>
                {

                    List<KeyValuePair<object, object>> list_oid_field = new List<KeyValuePair<object, object>>();

                    // récupérer les valeur du champ à trier pour les élements de la classe 
                    List<object> listOID = new();
                    foreach (var oid in ListOIDFeature) listOID.Add(oid.Key);

                    // champ lié à l'OID et indice pour le champ OID
                    var OidField = feature.GetDefinition().GetFields().Where(x => x.FieldType.ToString() == "OID").First().Name;
                    var idxfeaturefieldoid = feature.GetDefinition().FindField(OidField);
                    // indice pour le champ choisi pour le tri
                    var idxfeaturechoosefield = feature.GetDefinition().FindField(SelectedChoise);

                    // Parcours de la classe pour trouver l'objet avec les objectid demandés
                    QueryFilter qf = new QueryFilter();
                    foreach (var oid in listOID)
                    {
                        qf.WhereClause = $"{OidField} = {oid}";
                        qf.SubFields = $"{OidField},{SelectedChoise}";

                        RowCursor cursorRow = feature.Search(qf);
                        cursorRow.MoveNext();
                        Row row = cursorRow.Current;
                        list_oid_field.Add(new KeyValuePair<object, object>(row[idxfeaturefieldoid], row[idxfeaturechoosefield]));
                    }

                    List<KeyValuePair<object, object>> listeTriee = new();
                    // trier ces éléments
                    if (selected_sort_choice == "Croissant")
                        listeTriee = list_oid_field
                            .OrderBy(kv => kv.Value)
                            .ToList();

                    if (selected_sort_choice == "Décroissant")
                        listeTriee = list_oid_field
                          .OrderByDescending(kv => kv.Value)
                          .ToList();

                    // effacer la table et la recréer avec la nouvelle sélection triée.
                    // dans l'ordre de la liste triée, on va copier les infos de la table vers une nouvelle table.
                    GDBProjectItem gdbProjectItem = Project.Current.GetItems<GDBProjectItem>().FirstOrDefault(l => l.Name.Contains(QueryUtil.nom_query));

                    var in_data = gdbProjectItem.Path + "\\" + table.GetName();
                    String tablecopy_name = table.GetName() + "_copy";
                    var out_data = gdbProjectItem.Path + "\\" + tablecopy_name;

                    var arguments = Geoprocessing.MakeValueArray(in_data, out_data);
                    await Geoprocessing.ExecuteToolAsync("management.Copy", arguments);
                    Table table_copy = null;

                    table_copy = (gdbProjectItem.GetDatastore() as Geodatabase).OpenDataset<Table>(tablecopy_name);
                    arguments = Geoprocessing.MakeValueArray(table);
                    await Geoprocessing.ExecuteToolAsync("management.TruncateTable", arguments);

                    if (table_copy != null)
                    {
                        foreach (var kvp in listeTriee)
                            await CopierLigneviaINPUTID(table_copy, table, kvp.Key);

                        arguments = Geoprocessing.MakeValueArray(table_copy);
                        await Geoprocessing.ExecuteToolAsync("management.Delete", arguments);
                    }
                });
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                return false;
            }
        }


        /// <summary>
        /// Méthode async pour copier une ligne d'une table vers une autre
        /// </summary>        
        public async Task CopierLigneviaINPUTID(Table tableSource, Table tableCible, object oidRecherche)
        {
            await QueuedTask.Run(() =>
            {
                var InputID_s = tableSource.GetDefinition().GetFields().Where(x => x.Name.ToLower() == QueryDico.inputID_Field.ToLower()).First().Name;
                var InputID_c = tableCible.GetDefinition().GetFields().Where(x => x.Name.ToLower() == QueryDico.inputID_Field.ToLower()).First().Name;

                QueryFilter qf = new QueryFilter();
                qf.WhereClause = $"{QueryDico.inputID_Field} = '{oidRecherche}'";

                var fieldNames = new List<string>();
                foreach (ArcGIS.Core.Data.Field field in tableSource.GetDefinition().GetFields())
                    fieldNames.Add(field.Name);
                qf.SubFields = string.Join(",", fieldNames);

                using (RowCursor sourceRow = tableSource.Search(qf))
                {
                    if (sourceRow == null)
                        return;

                    sourceRow.MoveNext();
                    Row row = sourceRow.Current;
                    // Créer une nouvelle ligne dans la table cible
                    using (RowBuffer rowBuffer = tableCible.CreateRowBuffer())
                    {
                        // Copier tous les champs, sauf OID et autres champs systèmes
                        foreach (ArcGIS.Core.Data.Field field in tableSource.GetDefinition().GetFields())
                        {
                            var idxfeaturechoosefield = tableSource.GetDefinition().FindField(field.Name);
                            // Copier la valeur
                            if (field.IsEditable && field.Name != tableSource.GetDefinition().GetObjectIDField())
                                rowBuffer[field.Name] = row[idxfeaturechoosefield];
                        }

                        // Créer la ligne dans la table cible
                        using (Row newRow = tableCible.CreateRow(rowBuffer))
                        {
                            // La ligne est maintenant ajoutée dans la table cible
                        }
                    }
                }
            });
        }


        /// <summary> 
        /// Button implementation to empty the list
        /// Commande vider la queue d'édition
        /// </summary>
        private void CanExecuteDeleteObj(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ItemID != 0;
        }
        private void ExecuteDeleteObj(object sender, ExecutedRoutedEventArgs e)
        {
            QueuedTask.Run(() =>
            {
                // accès à la table
                Map map = AccesTable();
                if (map != null)
                {
                    // oid et nom de la couche en cours via ITEMID
                    object oid = List_INPUTID_Feature[ItemID - 1].Key;
                    string featurepath = List_INPUTID_Feature[ItemID - 1].Value.ToString();
                    List<string> parents = new List<string>();
                    string featureName = featurepath;
                    if (featurepath.Contains("/"))
                    {
                        parents = featurepath.Split('/').ToList();
                        featureName = parents[parents.Count() - 1];
                        parents.Remove(featureName);
                    }

                    //on retrouve la couche et la classe 
                    if (map.GetLayersAsFlattenedList().OfType<FeatureLayer>().FirstOrDefault(fl => fl.Name == featureName) != null)
                    {
                        FeatureLayer FL = null;
                        if (parents.Count() > 0)
                            FL = map.GetLayersAsFlattenedList().OfType<FeatureLayer>().FirstOrDefault(fl => fl.Name == featureName &&
                                                             fl.Parent.ToString() == parents[parents.Count() - 1]);

                        else
                            FL = map.GetLayersAsFlattenedList().OfType<FeatureLayer>().FirstOrDefault(fl => fl.Name == featureName);


                        if (FL != null)
                            deleteObject(FL, oid, featurepath);
                    }
                    if (map.GetLayersAsFlattenedList().OfType<AnnotationLayer>().FirstOrDefault(fl => fl.Name == featureName) != null)
                    {
                        AnnotationLayer AL = null;
                        if (parents.Count() > 0)
                            AL = map.GetLayersAsFlattenedList().OfType<AnnotationLayer>().FirstOrDefault(fl => fl.Name == featureName &&
                            fl.Parent.ToString() == parents[parents.Count() - 1]);
                        else
                            AL = map.GetLayersAsFlattenedList().OfType<AnnotationLayer>().FirstOrDefault(fl => fl.Name == featureName);

                        if (AL != null)
                            deleteObject(AL, oid, featurepath);
                    }
                }
            });
        }


        public Task<bool> deleteObject(Layer layer, object oid, string featurepath)
        {
            // Prépare la session d'édition
            EditOperation editOperation = null;
            // on teste la couche, savoir si c'est une couche simple ou une couche d'annotaion
            if (layer == null)
                return Task.FromResult(false);
            // Nouvelle session d'édition
            // Remarque: on passe obligatoirement par une session d'édition (et nom un rowbuffer) pour permettre à l'utilisation 
            // d'annuler l'opération via un Ctrl + z (undo stack).
            editOperation = new EditOperation
            {
                Name = QueryDico.DeleteObjectwithID + oid.ToString(),
                SelectNewFeatures = true
            };
            IEnumerable<long> oids = ConvertStringToLongEnumerable(oid.ToString());

            if (layer is FeatureLayer)
            {
                FeatureLayer FL = layer as FeatureLayer;
                // on  vérifie que la couche soit éditable
                bool iseditable = FL.IsEditable;
                // Mettre la couche accessible en écriture
                if (!FL.IsEditable) FL.SetEditable(true);

                // suppression de l'entrée
                editOperation.Delete(FL, oids);
                // on  remet la couche dans son état édition
                FL.SetEditable(iseditable);
            }
            else if (layer is AnnotationLayer)
            {
                AnnotationLayer AL = layer as AnnotationLayer;
                // on  vérifie que la couche soit éditable
                bool iseditable = AL.IsEditable;
                // Mettre la couche accessible en écriture
                if (!AL.IsEditable) AL.SetEditable(true);

                // suppression de l'entrée
                editOperation.Delete(AL, oids);
                // on  remet la couche dans son état édition
                AL.SetEditable(iseditable);
            }

            // Exécution de l'opération
            bool opResult = editOperation.Execute();
            if (opResult)
            {
                // supprimer de la table en cours
                int i = List_INPUTID_Feature.IndexOf(List_INPUTID_Feature.Find(x => x.Key == oid));
                QueryFilter qf = new QueryFilter();
                qf.WhereClause = QueryDico.Classe_Field + "= '" + featurepath +
                "'  And " + QueryDico.inputID_Field + " = '" + oid + "'";
                RowCursor cursortableRow = _table.Search(qf);
                cursortableRow.MoveNext();
                Row rowT = cursortableRow.Current;
                if (rowT is null)
                {
                    MessageBox.Show("Problème lors de la mise à jour de la table." +
                        "contacter l'équipe support",
                        "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return Task.FromResult(false); ;
                }
                // supprime de la table
                List_INPUTID_Feature.Remove(List_INPUTID_Feature[i]);
                rowT.Delete();
                cursortableRow.Dispose();
                ItemALL -= 1;
                ItemID = 0;
            }
            return Task.FromResult(true); ;
        }


        /// <summary>
        /// Button implementation to Fill the edit queue with a table
        /// commande Remplir la queue d'édition avec une table
        /// </summary>
        private ICommand _CmdFillChoises;
        public ICommand CmdFillChoises
        {
            get
            {
                return _CmdFillChoises ?? (_CmdFillChoises = new RelayCommand(() =>
                {
                    QueuedTask.Run(() =>
                    {


                        if (QueryUtil.nom_query is null) QueryUtil.nom_query = QueryDico.query_default_name;
                        // récupèrer la liste des classes de la table de la REQ.gdb
                        GDBProjectItem gdbProjectItem = Project.Current.GetItems<GDBProjectItem>().FirstOrDefault(l => l.Name.Contains(QueryUtil.nom_query));
                        Table the_table = (gdbProjectItem.GetDatastore() as Geodatabase).OpenDataset<Table>(SelectedTable.ToString().Insert(0, "result_"));
                        creerListe(the_table);


                        HashSet<string> classesUniques = GetSingleFeature();
                        if (classesUniques.Count == 1)
                        {
                            Choises = new ObservableCollection<string>();

                            // Ajouter des éléments à la ComboBox
                            foreach (var fld in featureclass_choose.GetDefinition().GetFields())
                                Choises.Add(fld.Name);
                        }
                        else
                        {
                            FillWithSortResult = QueryDico.MessageError_features;
                        }
                    });
                }));
            }
        }

        public static IEnumerable<long> ConvertStringToLongEnumerable(string input)
        {
            return input.Split(',')
                        .Select(s =>
                        {
                            if (long.TryParse(s, out long number))
                            {
                                return number;
                            }
                            else
                            {
                                throw new FormatException($"Unable to convert '{s}' to long.");
                            }
                        });
        }
        /// <summary>
        /// Button implementation to Fill the edit queue with a table
        /// commande Remplir la queue d'édition avec une table
        /// </summary>
        private ICommand _CmdFillWithTable;
        public ICommand CmdFillWithTable
        {
            get
            {
                return _CmdFillWithTable ?? (_CmdFillWithTable = new RelayCommand(() =>
                {
                    try
                    {
                        //var map = MapView.Active.Map;

                        QueuedTask.Run(async () =>
                        {
                            if (QueryUtil.nom_query is null) QueryUtil.nom_query = QueryDico.query_default_name;
                            if (await QueryDico.ExecuteTestGDB_Template(Project.Current.HomeFolderPath, QueryUtil.nom_query))
                            {
                                var table_temp = new List<TableInfo>();
                                var tables = new List<TableInfo>();

                                //sélection de la gdb
                                GDBProjectItem gdbProjectItem = Project.Current.GetItems<GDBProjectItem>().FirstOrDefault(l => l.Name.Contains(QueryUtil.nom_query));
                                IReadOnlyList<TableDefinition> tableDefinitions = null;

                                // définition de la table
                                tableDefinitions = (gdbProjectItem.GetDatastore() as Geodatabase).GetDefinitions<TableDefinition>();
                                tables.Add(new TableInfo { Definition = null, Name = string.Empty });
                                tables.AddRange(tableDefinitions.Select(definition => new TableInfo { Definition = definition, Name = definition.GetName() }));

                                foreach (var t in tables) if (t.Name.Contains("config_") is true) table_temp.Add(t);
                                foreach (var tp in table_temp) tables.Remove(tp);

                                // remise à zéro
                                ItemID = 0; oid = ""; List_INPUTID_Feature = null;
                                Tables = new ObservableCollection<string>();
                                foreach (var temp in tables) Tables.Add(temp.Name.Replace("result_", ""));

                                // Clear Selection()
                                MapView.Active.Map.ClearSelection();

                                // vider les choix du tri par champ
                                SelectedChoise = string.Empty;
                                Choises = new();
                                FillWithSortResult = "";

                            }
                            else MessageBox.Show(QueryDico.WarningGDB + QueryUtil.nom_query + QueryDico.MessageUpdate, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        });
                    }
                    catch
                    {
                        var mapPane = FrameworkApplication.Panes.OfType<IMapPane>().First() as Pane;
                        mapPane.Activate();
                    }
                }));
            }
        }

        /// <summary>
        /// Button implementation to Fill the edit queue selection
        /// Commande Remplir la table avec la sélection
        /// </summary>
        private ICommand _CmdFillWithSelection;
        public ICommand CmdFillWithSelection
        {
            get
            {
                return _CmdFillWithSelection ?? (_CmdFillWithSelection = new RelayCommand(async () =>
                {
                    try { var map = MapView.Active.Map; }
                    catch
                    {
                        var mapPane = FrameworkApplication.Panes.OfType<IMapPane>().First() as Pane;
                        mapPane.Activate();
                        // MessageBox.Show(QueryDico.ActivateMap + "'" + nom_map + "'");
                    }
                    try
                    {
                        // Recherche la GDB "REQ" pour gérer les enregistrements
                        if (QueryUtil.nom_query is null) QueryUtil.nom_query = QueryDico.query_default_name;
                        var test = await QueryDico.ExecuteTestGDB_Template(Project.Current.HomeFolderPath, QueryUtil.nom_query);
                        if (test)
                        {
                            List<TableInfo> tables = new List<TableInfo>();
                            List<TableInfo> table_temp = new List<TableInfo>();
                            int nb = 0;
                            await QueuedTask.Run(() =>
                            {
                                nb = MapView.Active.Map.GetSelection().Count;
                            });

                            if (nb > 0)
                            {
                                nom_map = MapView.Active.Map.Name;
                                // créer une table temporaire
                                GDBProjectItem gdbProjectItem = Project.Current.GetItems<GDBProjectItem>().FirstOrDefault(l => l.Name.Contains(QueryUtil.nom_query));
                                if (gdbProjectItem == null)
                                {
                                    MessageBox.Show(QueryUtil.nom_query + ".gdb" + QueryDico.MapNOExist);
                                    return;
                                }

                                bool createtable = await QueryUtil.CreerTable("result_" + QueryDico.selection_default_name, gdbProjectItem);
                                if (createtable)
                                {
                                    await QueuedTask.Run(() =>
                                    {
                                        // définition de la table
                                        //IReadOnlyList<TableDefinition> tableDefinitions = (gdbProjectItem.GetDatastore() as Geodatabase).GetDefinitions<TableDefinition>();
                                        //tables.Add(new TableInfo { Definition = null, Name = string.Empty });

                                        //tables.AddRange(tableDefinitions.Select(definition => new TableInfo { Definition = definition, Name = definition.GetName() }));

                                        // foreach (var t in tables) if (t.Name.Contains("config_") is true) table_temp.Add(t);
                                        //foreach (var tp in table_temp) tables.Remove(tp);
                                        _table = (gdbProjectItem.GetDatastore() as Geodatabase).OpenDataset<Table>("result_" + QueryDico.selection_default_name);

                                        // remise à zéro
                                        ItemID = 0; oid = "";
                                        List_INPUTID_Feature = null;
                                        //Tables = new ObservableCollection<string>();
                                        //foreach (var temp in tables) Tables.Add(temp.Name.Replace("result_", ""));
                                        ItemALL = (int)_table.GetCount();
                                        List_INPUTID_Feature = null;
                                        ItemID = 0; oid = "";
                                        SelectedTable = _table.GetName().Replace("result_", "");
                                        MapView.Active.Map.ClearSelection();
                                    });
                                // vider les choix du tri par champ
                                SelectedChoise = string.Empty;
                                Choises = new();
                                FillWithSortResult = "";
 }
                                else MessageBox.Show(QueryDico.ProblemTable + QueryDico.selection_default_name);
                            }
                            else MessageBox.Show(QueryDico.WarningNullSelection);
                        }
                        else MessageBox.Show(QueryDico.messageCreateGDB);
                    }
                    catch
                    {
                        //(Exception e) MessageBox.Show(e.ToString());
                    }
                }));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public ICommand _CmdWorkonreload;
        public ICommand CmdWorkonreload
        {
            get
            {
                return _CmdWorkonreload ?? (_CmdWorkonreload = new RelayCommand(() =>
               {
                   if (SelectedTable != string.Empty)
                   {
                       Map map = AccesTable();
                   }
               }));
            }
        }
        #endregion
    }


    /// <summary>
    /// Button implementation to show the DockPane.
    /// </summary>
    internal class QueueEditionDockpane_ShowButton : Button
    {
        protected override void OnClick()
        {
            QueueEditionViewModel.Show();
        }
    }
}
