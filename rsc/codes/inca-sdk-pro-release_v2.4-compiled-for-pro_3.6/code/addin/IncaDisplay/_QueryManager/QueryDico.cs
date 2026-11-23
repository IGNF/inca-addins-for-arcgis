using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;


namespace IncaDisplay._QueryManager
{
    public class QueryDico
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #region GDB NAME
        public static string query_default_name = "REQ";
        #endregion GDB NAME

        #region TABLE NAME
        public static string selection_default_name = "Table_Selection";

        public static string attribute_query_default_name = "config_T_attribut";

        public static string location_query_default_name = "config_T_spatial";

        public static string template_default_name = "config_Template";
        #endregion TABLE NAME

        #region FIELDS & ALIAS ATTRIBUTAIRE
        #region FIELD ATTRIBUTAIRE
        ///   Champs de la table Requete attributaire
        public static string Classe_Field = "Layer";

        public static string Query_Field = "Query";

        public static string Type_Field = "Selection_type";

        public static string Name_Field = "Query_name";

        public static string Invert_Field = "Invert_where_clause";

        #endregion FIELD ATTRIBUTAIRE
        #region ALIAS ATTRIBUTAIRE
        ///   Champs de la table Requete attributaire
        public static string Classe_ALIAS
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "COUCHE";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "CAPA";
                else return "LAYER";
            }
        }
        public static string Query_ALIAS
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "REQUETE";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "CONSULTA";
                else return "QUERY";
            }
        }
        public static string Type_ALIAS
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "TYPE_SELECTION";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "TIPO_SELECCION";
                else return "SELECTION_TYPE";
            }
        }
        public static string Name_ALIAS
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "NOM_REQUETE";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "NOMBRE_CONSULTA";
                else return "QUERY_NAME";
            }
        }
        public static string Invert_ALIAS
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "CLAUSE_INVERSE";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "INVERTIR_CLAUSULA";
                else return "INVERT_CLAUSE";
            }
        }
        #endregion ALIAS ATTRIBUTAIRE
        #endregion FIELDS & ALIAS ATTRIBUTAIRE

        #region  FIELDS & ALIAS SPATIALE
        #region FIELDS SPATIALE
        // ALIAS de la requête spatiale
        public static string distance_Field = "Search_distance";
        public static string relation_Field = "Specifies_the_spatial_relationship";
        public static string input_features_Field = "Input_features";
        public static string Selecting_features_Field = "Selecting_features";

        #endregion FIELDS SPATIALE
        #region ALIAS SPATIALE
        // ALIAS de la requête spatiale
        public static string distance_ALIAS = "DISTANCE";
        public static string relation_ALIAS
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "RELATION";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "RELACION";
                else return "RELATION";
            }
        }
        public static string Selecting_features_ALIAS
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "COUCHE DE SELECTION";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "SELECCIONAR ENTIDADES";
                else return "SELECTING FEATURES";
            }
        }
        public static string input_features_ALIAS
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "ENTITES EN ENTREE";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "ENTIDADES DE ENTRADA ";
                else return "INPUT FEATURES";
            }
        }
        #endregion ALIAS SPATIALE
        #endregion FIELDS & ALIAS

        #region TEMPLATE
        public static string Carte_Field = "Map";
        public static string Carte_ALIAS
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "CARTE";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "MAPA ";
                else return "MAP";
            }
        }
        public static string inputID_Field = "Input_object_ID";
        public static string inputID_ALIAS = "INPUT_ID";
        public static string symbo_Field = "symbolization";
        public static string symbo_ALIAS = "SYMBO";
        #endregion TEMPLATE

        #region VALEUR PAR DEFAUT
        public static string invert_default_value
        { get { return "NOT_INVERT"; } }
        public static string query_default_value
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Nouvelle sélection";
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Nueva selección";
                else return "New selection";
            }
        }
        public static string distance_default_value = "0 Meters";

        #endregion VALEUR PAR DEFAUT

        #region MESSAGES
        public static string ActivateMap
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Activer la carte : ";
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Activar el mapa : ";
                else return "Activate the map : ";
            }
        }

        public static string MessageToActivateMap
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Aucune carte n'est active, cliquer sur une carte pour exécuter la requête.";
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "No hay ningún mapa activo, haga clic en un mapa para ejecutar la solicitud";
                else return "No map is active, click on a map to execute the request";
            }
        }

        public static string DeleteObjectwithID
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Suppression de l'objet ";
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Eliminar objeto ";
                else return "Delete object ";
            }
        }
        public static string MapNOExist
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return " introuvable.";
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return " no encontrado.";
                else return " not found.";
            }
        }
        public static string messageCreateGDB
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Créer ou importer la database 'REQ.gdb'." +
                        " Boutons dans le gestionnaire de requêtes";
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Crear o importar la base de datos 'REQ.gdb'. Botones en el administrador de solicitudes";
                else return "Create or import the 'REQ.gdb' database. Buttons in the query manager";
            }
        }
        public static string WarningGDB
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "La database '";
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "La base de datos '";
                else return "The database '";
            }
        }
        public static string WarningNullSelection
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "La sélection est vide. Impossible de créer une table.";
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "La selección está vacía. No se puede crear una tabla.";
                else return "The selection is empty. Unable to create a table.";
            }
        }
        public static string MessageTable
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return " La table : '";
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "La tabla.";
                else return "The table.";
            }
        }
        public static string ErrorRemoveTable
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return " ne peut pas être enlevée.";
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return " no se puede quitar.";
                else return " cannot be removed.";
            }
        }

        public static string MessageUpdate
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "'  n'est pas à jour.";
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "' no esta actualizada.";
                else return "' is not up to date.";
            }
        }
        public static string ProblemParcours
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Problème lors du parcours.";
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Problema al navegar por la lista.";
                else return "Problem browsing the list.";
            }
        }
        public static string ProgressorMessage
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Construction de la gdb.";
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Construcción del BGF.";
                else return "Construction of the GDB.";
            }
        }
        public static string ProblemConstructGDB
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Problème, contacter l'équipe support : erreur à la Construction de la gdb.";
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Problema, contacte al equipo de soporte: error en la Construcción de la gdb.";
                else return "Problem, contact the support team: error in the Construction of the gdb.";
            }
        }
        public static string ProblemLayer
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Problème, avec la classe ou la couche ...";
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Problema, de problema con clase o capa...";
                else return "Problem, with class or layer...";
            }
        }
        public static string ProblemTable
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Problème à la création de la table ...";
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Problema al crear la tabla...";
                else return "Problem creating table...";
            }
        }
        public static string ProblemField
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Problème à l'ajout du champ ...";
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Problema al agregar el campo...";
                else return "Problem when adding the field...";
            }
        }
        public static string ProblemDefaultValue
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Problème à l'ajout de la valeur par défaut ...";
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Problema con agregar el valor por defecto...";
                else return "Problem with adding the default value...";
            }
        }
        public static string ProblemValidGdb
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Problème lors de la validation de la GDB...";
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Problema al validar la GDB...";
                else return "Problem while validating the GDB...";
            }
        }
        public static string WarningEmptyTable
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Aucune requête à afficher.";
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "No hay consulta para mostrar.";
                else return "No query to display.";
            }
        }
        public static string ProblemEditTable
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Problème pour afficher la liste des tables.";
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Problema para mostrar la lista de tablas.";
                else return "Problem to display the list of tables.";
            }
        }
        public static string ProblemShowQuery
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Problème pour afficher le geotraitement.";
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Problema al mostrar el geoprocesamiento.";
                else return "Problem displaying geoprocessing.";
            }
        }
        public static string ProblemDeleteTable
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Problème lors de la suppression de la table.";
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Problema al borrar la tabla.";
                else return "Problem while dropping table.";
            }
        }

        public static string ProblemEditQuerie
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Problème chargement des requêtes.";
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Problema al cargar consultas.";
                else return "Problem loading requests.";
            }
        }

        public static string MessageEndSortedList_des 
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Tri par ordre décroissant terminé ";
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Ordenar en orden descendente completado.";
                else return "Sort in descending order completed.";
            }
        }
        public static string MessageEndSortedList_asc 
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Tri par ordre croissant terminé";
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Ordenar en orden ascendente completado.";
                else return "Sort in ascending order completed.";
            }
        }

        public static string MessageError_features
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Tri impossible, plusieurs classes sont à parcourir";
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Clasificación imposible, varias clases necesitan ser cubiertas.";
                else return "Impossible sorting, several classes need to be covered.";
            }
        }
        public static string MessageEnd_Selection_nul
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Aucun objet sélectionné sur la couche";
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Ningún objeto seleccionado en la capa";
                else return "No object selected on the layer";
            }
        }

        public static string MessageEnd_Selection_one
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "objet sélectionné sur la couche";
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "elemento seleccionado en la capa";
                else return "selected feature on the layer";
            }
        }
        public static string MessageEnd_Selection_several
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "objets sélectionnés sur la couche";
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "elementos seleccionados en la capa";
                else return "selected features on the layer";
            }
        }
        public static string MessageEnd_Selection_error
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Erreur lors de l'exécution de la requête. Pour vérifier la requête, cliquez sur";
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Error al ejecutar la consulta. Para comprobar la consulta, haga clic en";
                else return "Error executing the query. To check the query, click on ";
            }
        }

        public static string MessageLayer_error
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "La couche questionnée n'existe pas dans la carte.";
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "La capa consultada no existe en el mapa.";
                else return "The queried layer does not exist on the map.";
            }
        }


        #endregion MESSAGES

        #region CREATION GDB, TABLE, CHAMP, VALEUR PAR DEFAUT
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileGdbPath"></param>
        /// <param name="fileGdbName"></param>
        /// <returns></returns>
        public async static Task<bool> ExecuteAddFileGDB(string fileGdbPath, string fileGdbName)
        {
            try
            {
                string fullPath = Path.Combine(fileGdbPath, fileGdbName);
                // si la gdb est déjà dans le dossier, on l'importe
                if (Directory.Exists(fullPath))
                {
                    await QueuedTask.Run(() =>
                    {
                        IProjectItem item = ItemFactory.Instance.Create(fileGdbPath + "\\" + QueryUtil.nom_query + ".gdb") as IProjectItem;
                        Project.Current.AddItem(item);
                    });
                }
                //sinon on la crée
                else
                {
                    var fGdbVersion = "Current";  // create the 'latest' version of file Geodatabase
                    System.Diagnostics.Debug.WriteLine($@"create {fileGdbPath} {fileGdbName}");
                    var parameters = Geoprocessing.MakeValueArray(fileGdbPath, fileGdbName, fGdbVersion);
                    await Geoprocessing.ExecuteToolAsync("management.CreateFileGDB", parameters, null);
                }
                return true;
            }
            catch
            {
                MessageBox.Show(ProblemConstructGDB);
                return false;
            }
        }

        public async static Task ExecuteAddFileTable(string fileGdbPath, string fileTableName)
        {
            try
            {
                var parameters = Geoprocessing.MakeValueArray(fileGdbPath, fileTableName, "", "DEFAULTS", fileTableName);
                await Geoprocessing.ExecuteToolAsync("management.CreateTable", parameters);
            }
            catch
            {
                MessageBox.Show(MessageTable + template_default_name + ErrorRemoveTable);
            }
        }

        public async static Task<bool> ExecuteAddFieldTool(string the_table, KeyValuePair<string, string> field, string fieldType, int? fieldLength = null, bool isNullable = true)
        {
            try
            {
                var parameters = Geoprocessing.MakeValueArray(the_table, field.Key, fieldType.ToUpper(), null, null, fieldLength, field.Value, isNullable ? "NULLABLE" : "NON_NULLABLE");
                var result = await Geoprocessing.ExecuteToolAsync("management.AddField", parameters, null);
                if (result.IsFailed) return false;
                else return true;
            }
            catch
            {
                MessageBox.Show(ProblemField + field);
                return false;
            }
        }

        public async static Task<bool> ExecuteAddDefaultValueTool(string the_table, string field, string fieldDefaultValue = "")
        {
            try
            {
                var parameters = Geoprocessing.MakeValueArray(the_table, field, fieldDefaultValue);
                await Geoprocessing.ExecuteToolAsync("management.AssignDefaultToField", parameters);
                return true;
            }
            catch
            {
                MessageBox.Show(ProblemDefaultValue);
                return false;
            }
        }

        public async static Task<bool> ExecuteAddConstructGDB(string homeDir, string NomGDB)
        {
            bool RETOUR = false;
            try
            {
                if (await ExecuteAddFileGDB(homeDir, NomGDB))
                {
                    IProjectItem item = ItemFactory.Instance.Create(homeDir + "\\" + NomGDB + ".gdb") as IProjectItem;
                    // la gdb est créée vide, il faut la configurer
                    if (item != null)
                    {
                        Project.Current.AddItem(item);
                        RETOUR = await ExecuteTestGDB(homeDir, NomGDB);
                    }
                }
                else MessageBox.Show(ProblemTable + homeDir + NomGDB);
            }
            catch { MessageBox.Show(ProblemConstructGDB); }
            return RETOUR;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="homeDir"></param>
        /// <param name="NomGDB"></param>
        /// <returns></returns>
        public async static Task<bool> ExecuteAddConstructGDB_TEMPLATE(string homeDir, string NomGDB)
        {
            //MessageBox.Show("ExecuteAddConstructGDB_TEMPLATE");
            bool RETOUR = false;
            try
            {
                if (await ExecuteAddFileGDB(homeDir, NomGDB))
                {
                    IProjectItem item = ItemFactory.Instance.Create(homeDir + "\\" + NomGDB + ".gdb") as IProjectItem;
                    //if it is succesfully added to the project, return it otherwise null
                    await QueuedTask.Run(() => { Project.Current.AddItem(item); });

                    try
                    {
                        await ExecuteAddFileTable(homeDir + "\\" + NomGDB + ".gdb", template_default_name);
                        GDBProjectItem gdbProjectItem = Project.Current.GetItems<GDBProjectItem>().FirstOrDefault(l => l.Name.Contains(NomGDB));
                        await QueuedTask.Run(async () =>
                        {
                            Table MAtable = (gdbProjectItem.GetDatastore() as Geodatabase).OpenDataset<Table>(template_default_name);
                            await ExecuteAddFieldTool(MAtable.GetName(), new KeyValuePair<string, string>(Classe_Field, Classe_ALIAS), "text");
                            await ExecuteAddFieldTool(MAtable.GetName(), new KeyValuePair<string, string>(Carte_Field, Carte_ALIAS), "text");
                            await ExecuteAddFieldTool(MAtable.GetName(), new KeyValuePair<string, string>(symbo_Field, symbo_ALIAS), "text");
                            await ExecuteAddFieldTool(MAtable.GetName(), new KeyValuePair<string, string>(inputID_Field, inputID_ALIAS), "text");
                            RETOUR = true;
                        });
                    }
                    catch
                    {
                        MessageBox.Show(ProblemTable + homeDir + "\\" + NomGDB + ".gdb");
                        RETOUR = false;
                    }
                }
                return RETOUR;
            }
            catch
            {
                MessageBox.Show(ProblemConstructGDB);
                return false;
            }
        }
        #endregion CREATION GDB, TABLE, CHAMP, VALEUR PAR DEFAUT

        #region TEST VALIDE GDB
        public async static Task<bool> ExecuteTestGDB(string homeDir, string NomGDB)
        {
            bool RETOUR = false, RETOUR_att = false, RETOUR_loc = false, RETOUR_temp = false;

            // la gdb
            GDBProjectItem gdbProjectItem = Project.Current.GetItems<GDBProjectItem>().FirstOrDefault(l => l.Name.Equals(NomGDB + ".gdb"));
            // test existence gdb
            if (gdbProjectItem is null)
            {
                // si la gdb est déjà dans le dossier, on l'importe
                string fullPath = Path.Combine(homeDir, NomGDB + ".gdb");
                // si la gdb est déjà dans le dossier, on l'importe
                if (Directory.Exists(fullPath))
                {
                    IProjectItem item = ItemFactory.Instance.Create(homeDir + "\\" + NomGDB + ".gdb") as IProjectItem;
                    await QueuedTask.Run(() => { Project.Current.AddItem(item); });
                    gdbProjectItem = Project.Current.GetItems<GDBProjectItem>().FirstOrDefault(l => l.Name.Equals(NomGDB + ".gdb"));
                }
                else
                {
                    //MessageBox.Show(messageCreateGDB);
                    return false;
                }
            }
            if (gdbProjectItem is not null)
            {
                RETOUR_loc = await TestLocationTable(gdbProjectItem, homeDir, NomGDB);
                RETOUR_att = await TestAttributeTable(gdbProjectItem, homeDir, NomGDB);
                RETOUR_temp = await TestTemplateTable(gdbProjectItem, homeDir, NomGDB);

                if (RETOUR_att && RETOUR_loc && RETOUR_temp) RETOUR = true;
                else MessageBox.Show("Probleme test GDB");
            }
            return RETOUR;
        }

        public static async Task<bool> TestAttributeTable(GDBProjectItem gdbProjectItem, string homeDir, string NomGDB)
        {
            bool RETOUR = false;
            // on part du principe qu'on doit créer la table
            bool creation = true;
            Table MAtable = null;
            List<TableDefinition> definitions = null;
            await QueuedTask.Run(() => { definitions = (gdbProjectItem.GetDatastore() as Geodatabase).GetDefinitions<TableDefinition>().ToList(); });
            if (definitions.Count() != 0)
                foreach (var n in definitions)
                {
                    if (await QueuedTask.Run(() => n.GetName() == attribute_query_default_name))
                    {
                        creation = false; break;
                    }
                }

            if (creation)
                await ExecuteAddFileTable(homeDir + "\\" + NomGDB + ".gdb", attribute_query_default_name);

            await QueuedTask.Run(() => { MAtable = (gdbProjectItem.GetDatastore() as Geodatabase).OpenDataset<Table>(attribute_query_default_name); });

            if (MAtable != null)
            {
                await QueuedTask.Run(async () =>
                {
                    //si ok, vérifie les champs
                    if (MAtable.GetDefinition().FindField(Name_Field) == -1)
                        await ExecuteAddFieldTool(MAtable.GetPath().LocalPath.ToString(), new KeyValuePair<string, string>(Name_Field, Name_ALIAS), "text");

                    if (MAtable.GetDefinition().FindField(Type_Field) == -1)
                        await ExecuteAddFieldTool(MAtable.GetPath().LocalPath.ToString(), new KeyValuePair<string, string>(Type_Field, Type_ALIAS), "text");

                    if (MAtable.GetDefinition().FindField(Query_Field) == -1)
                    {
                        await ExecuteAddFieldTool(MAtable.GetPath().LocalPath.ToString(), new KeyValuePair<string, string>(Query_Field, Query_ALIAS), "text");
                        await ExecuteAddDefaultValueTool(MAtable.GetPath().LocalPath.ToString(), Type_Field, query_default_value);
                    }

                    if (MAtable.GetDefinition().FindField(Classe_Field) == -1)
                        await ExecuteAddFieldTool(MAtable.GetPath().LocalPath.ToString(), new KeyValuePair<string, string>(Classe_Field, Classe_ALIAS), "text");

                    if (MAtable.GetDefinition().FindField(Invert_Field) == -1)
                    {
                        await ExecuteAddFieldTool(MAtable.GetPath().LocalPath.ToString(), new KeyValuePair<string, string>(Invert_Field, Invert_ALIAS), "text");
                        await ExecuteAddDefaultValueTool(MAtable.GetPath().LocalPath.ToString(), Invert_Field, invert_default_value);
                    }
                    RETOUR = true;
                });
            }
            MAtable.Dispose();
            return RETOUR;
        }


        public static async Task<bool> TestLocationTable(GDBProjectItem gdbProjectItem, string homeDir, string NomGDB)
        {
            bool RETOUR = false;
            // on part du principe qu'on doit créer la table
            bool creation = true;
            Table MAtable = null;
            List<TableDefinition> definitions = null;
            await QueuedTask.Run(() => { definitions = (gdbProjectItem.GetDatastore() as Geodatabase).GetDefinitions<TableDefinition>().ToList(); });

            if (definitions.Count() != 0)
                foreach (var n in definitions)
                {
                    if (await QueuedTask.Run(() => n.GetName() == location_query_default_name))
                    {
                        creation = false; break;
                    }
                }

            if (creation)
                await ExecuteAddFileTable(homeDir + "\\" + NomGDB + ".gdb", location_query_default_name);

            await QueuedTask.Run(() => { MAtable = (gdbProjectItem.GetDatastore() as Geodatabase).OpenDataset<Table>(location_query_default_name); });

            if (MAtable != null)
            {
                await QueuedTask.Run(async () =>
                {
                    //vérifie les champs
                    if (MAtable.GetDefinition().FindField(Name_Field) == -1)
                        await ExecuteAddFieldTool(MAtable.GetPath().LocalPath.ToString(), new KeyValuePair<string, string>(Name_Field, Name_ALIAS), "text");

                    if (MAtable.GetDefinition().FindField(Type_Field) == -1)
                    {
                        await ExecuteAddFieldTool(MAtable.GetPath().LocalPath.ToString(), new KeyValuePair<string, string>(Type_Field, Type_ALIAS), "text");
                        await ExecuteAddDefaultValueTool(MAtable.GetName(), Type_Field, query_default_value);
                    }
                    if (MAtable.GetDefinition().FindField(input_features_Field) == -1)
                        await ExecuteAddFieldTool(MAtable.GetPath().LocalPath.ToString(), new KeyValuePair<string, string>(input_features_Field, input_features_ALIAS), "text");

                    if (MAtable.GetDefinition().FindField(distance_Field) == -1)
                    {
                        await ExecuteAddFieldTool(MAtable.GetPath().LocalPath.ToString(), new KeyValuePair<string, string>(distance_Field, distance_ALIAS), "text");
                        await ExecuteAddDefaultValueTool(MAtable.GetPath().LocalPath.ToString(), distance_Field, distance_default_value);
                    }

                    if (MAtable.GetDefinition().FindField(relation_Field) == -1)
                        await ExecuteAddFieldTool(MAtable.GetPath().LocalPath.ToString(), new KeyValuePair<string, string>(relation_Field, relation_ALIAS), "text");


                    if (MAtable.GetDefinition().FindField(Selecting_features_Field) == -1)
                        await ExecuteAddFieldTool(MAtable.GetPath().LocalPath.ToString(), new KeyValuePair<string, string>(Selecting_features_Field, Selecting_features_ALIAS), "text");

                    if (MAtable.GetDefinition().FindField(Invert_Field) == -1)
                    {
                        await ExecuteAddFieldTool(MAtable.GetPath().LocalPath.ToString(), new KeyValuePair<string, string>(Invert_Field, Invert_ALIAS), "text");
                        await ExecuteAddDefaultValueTool(MAtable.GetPath().LocalPath.ToString(), Invert_Field, invert_default_value);
                    }
                    RETOUR = true;
                });
            }
            MAtable.Dispose();
            return RETOUR;
        }
        #endregion TEST VALIDE GDB

        #region TEST VALIDE GDB_TEMPLATE
        /// <summary>
        ///  Test l'existence de la gdb, 
        ///  sinon la créée
        ///  si elle existe, vérifie que les tables nécessaire existent 
        ///  et sont conformes
        /// </summary>
        /// <param name="homeDir"></param>
        /// <param name="NomGDB"></param>
        /// <returns></returns>
        public async static Task<bool> ExecuteTestGDB_Template(string homeDir, string NomGDB)
        {
            bool RETOUR = false;
            try
            {

                // test existence gdb, sinon on la créée
                // test si la gdb est déjà liée au projet.
                if (Project.Current.GetItems<GDBProjectItem>().FirstOrDefault(l => l.Name.Equals(NomGDB + ".gdb")) is null)
                    // on va voir si la gdb existe et la lier
                    if (await ExecuteTestGDB(homeDir, NomGDB))
                        RETOUR = await TestTemplateTable(Project.Current.GetItems<GDBProjectItem>().FirstOrDefault(l => l.Name.Equals(NomGDB + ".gdb")), homeDir, NomGDB);
                    else
                        // sinon, on la créee
                        RETOUR = await ExecuteAddConstructGDB_TEMPLATE(homeDir, NomGDB);

                // si elle existe, teste l'existence des tables
                else
                    RETOUR = await TestTemplateTable(Project.Current.GetItems<GDBProjectItem>().FirstOrDefault(l => l.Name.Equals(NomGDB + ".gdb")), homeDir, NomGDB);
                return RETOUR;
            }
            catch
            {
                MessageBox.Show(ProblemValidGdb);
                return false;
            }
        }

        public static async Task<bool> TestTemplateTable(GDBProjectItem gdbProjectItem, string homeDir, string NomGDB)
        {
            bool RETOUR = false;
            // on part du principe qu'on doit créer la table
            bool creation = true;
            Table MAtable = null;
            List<TableDefinition> definitions = null;
            await QueuedTask.Run(() => { definitions = (gdbProjectItem.GetDatastore() as Geodatabase).GetDefinitions<TableDefinition>().ToList(); });
            if (definitions.Count() != 0)
                foreach (var n in definitions)
                    if (await QueuedTask.Run(() => n.GetName() == template_default_name))

                    {
                        creation = false; break;
                    }

            if (creation) await ExecuteAddFileTable(homeDir + "\\" + NomGDB + ".gdb", template_default_name);

            await QueuedTask.Run(() => { MAtable = (gdbProjectItem.GetDatastore() as Geodatabase).OpenDataset<Table>(template_default_name); });
            if (MAtable != null)
            {
                await QueuedTask.Run(async () =>
                {
                    //vérifie les champs
                    if (MAtable.GetDefinition().FindField(Classe_Field) == -1)
                        await ExecuteAddFieldTool(MAtable.GetPath().LocalPath.ToString(), new KeyValuePair<string, string>(Classe_Field, Classe_ALIAS), "text");

                    if (MAtable.GetDefinition().FindField(Carte_Field) == -1)
                        await ExecuteAddFieldTool(MAtable.GetPath().LocalPath.ToString(), new KeyValuePair<string, string>(Carte_Field, Carte_ALIAS), "text");

                    if (MAtable.GetDefinition().FindField(symbo_Field) == -1)
                        await ExecuteAddFieldTool(MAtable.GetPath().LocalPath.ToString(), new KeyValuePair<string, string>(symbo_Field, symbo_ALIAS), "text");

                    if (MAtable.GetDefinition().FindField(inputID_Field) == -1)
                        await ExecuteAddFieldTool(MAtable.GetPath().LocalPath.ToString(), new KeyValuePair<string, string>(inputID_Field, inputID_ALIAS), "text");

                    RETOUR = true;
                });
            }
            return RETOUR;
        }
        #endregion TEST VALIDE GDB+TEMPLATE
    }
}