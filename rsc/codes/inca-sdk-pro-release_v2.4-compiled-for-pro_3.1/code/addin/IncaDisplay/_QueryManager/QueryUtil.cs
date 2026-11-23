using ArcGIS.Core.Data;
using ArcGIS.Core.Data.UtilityNetwork.Trace;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace IncaDisplay._QueryManager
{
    public class QueryUtil
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static string nom_query { get; set; }
        public static bool ClassExists(Geodatabase geodatabase, string ClassName)
        {
            try
            {
                FeatureClassDefinition Definition = geodatabase.GetDefinition<FeatureClassDefinition>(ClassName);
                Definition.Dispose();
                return true;
            }
            catch
            {
                // GetDefinition throws an exception if the definition doesn't exist
                return false;
            }
        }

        public static bool TableExists(Geodatabase geodatabase, string TableName)
        {
            try
            {
                var tableDefinition = geodatabase.GetDefinition<TableDefinition>(TableName);
                tableDefinition.Dispose();
                return true;
            }
            catch
            {
                // GetDefinition throws an exception if the definition doesn't exist
                return false;
            }
        }

        /// fonction d'execution de la sélection par l'attribut OID
        public static void Selection_by_OID(string feature_name, string id, Map map)
        {
            string OidField = "";
            QueuedTask.Run(() =>
            {

                FeatureLayer FL = null;
                AnnotationLayer AL = null;
                // split string 
                string[] result = feature_name.Split('/');
                List<Layer> list_L = map.GetLayersAsFlattenedList().OfType<Layer>().Where(f => f.Name == result.Last()).ToList();
                //List<FeatureLayer> list_FL = map.GetLayersAsFlattenedList().OfType<FeatureLayer>().Where(f => (f.Name == result.Last())).ToList<FeatureLayer>();
                // une seule couche, on la sélectionne
                if (list_L.Count() == 1)
                {
                    if (list_L[0] is FeatureLayer) FL = list_L[0] as FeatureLayer;
                    else if (list_L[0] is AnnotationLayer) AL = list_L[0] as AnnotationLayer;
                    else { return; }
                }

                // s'il y a plusieurs couches avec le même nom, il faut reconstruire l'empilement pour retrouver la bonne couche à associer
                else if (list_L.Count() != 0)
                {
                    foreach (var f in list_L)
                    {
                        Layer k = f;
                        string k_name = k.Name;
                        while (k.Parent.ToString() != map.Name)
                        {
                            k_name = k.Parent.ToString() + "/" + k_name;
                            k = k.Parent as Layer;
                        }
                        if (k_name == feature_name)
                        {
                            if (f is FeatureLayer) FL = f as FeatureLayer;
                            else if (f is AnnotationLayer) AL = f as AnnotationLayer;
                            else { return; }
                            break;
                        }
                    }
                }
                //else
                //{
                //    List<AnnotationLayer> list_AL = map.GetLayersAsFlattenedList().OfType<AnnotationLayer>().Where(f => (f.Name == result.Last())).ToList<AnnotationLayer>();
                //    if (list_AL.Count() == 1) AL = list_AL[0] as AnnotationLayer;
                //    // s'il y a plusieurs couches avec le même nom, il faut reconstruire l'empilement pour retrouver la bonne couche à associer
                //    else if (list_AL.Count() != 0)
                //    {
                //        foreach (var f in list_AL)
                //        {
                //            Layer k = f;
                //            string k_name = k.Name;
                //            while (k.Parent.ToString() != map.Name)
                //            {
                //                k_name = k.Parent.ToString() + "/" + k_name;
                //                k = k.Parent as Layer;
                //            }
                //            if (k_name == feature_name)
                //            {
                //                AL = f;
                //                break;
                //            }
                //        }
                //    }
                //}

                QueryFilter qF = new QueryFilter();
                Table T = null;
                map.ClearSelection();
                if (FL != null)
                {
                    OidField = FL.GetFeatureClass().GetDefinition().GetFields().Where(x => x.FieldType.ToString() == "OID").First().Name;
                    // Requête :
                    qF.WhereClause = OidField + " = " + id;
                    // trouver la table associée
                    T = FL.GetTable();
                }
                else if (AL != null)
                {
                    OidField = AL.GetFeatureClass().GetDefinition().GetFields().Where(x => x.FieldType.ToString() == "OID").First().Name;
                    // Requête :
                    qF.WhereClause = OidField + " = " + id;
                    // trouver la table associée
                    T = AL.GetTable();
                }
                else { return; }

                Selection Table_select = T.Select(qF, SelectionType.ObjectID, SelectionOption.Normal);
                List<long> List_T = Table_select.GetObjectIDs().ToList();
                Dictionary<MapMember, List<long>> DicSelect = new Dictionary<MapMember, List<long>>();
                DicSelect.Add(map.GetLayersAsFlattenedList().OfType<MapMember>().FirstOrDefault(m => m.Name == result.Last()), List_T);
                // sélection sur la carte définie
                map.SetSelection(SelectionSet.FromDictionary(DicSelect));


                //map.ClearSelection();
                //OidField = AL.GetFeatureClass().GetDefinition().GetFields().Where(x => x.FieldType.ToString() == "OID").First().Name;
                //// Requête :
                //QueryFilter qF = new QueryFilter();
                //qF.WhereClause = OidField + " = " + id;
                //// trouver la table associée
                //Table T = AL.GetTable();
                //Selection Table_select = (T.Select(qF, SelectionType.ObjectID, SelectionOption.Normal));
                //List<long> List_T = Table_select.GetObjectIDs().ToList();
                //Dictionary<MapMember, List<long>> DicSelect = new Dictionary<MapMember, List<long>>();
                //DicSelect.Add(map.GetLayersAsFlattenedList().OfType<MapMember>().FirstOrDefault(m => m.Name == result.Last()), List_T);
                //// sélection sur la carte définie
                //map.SetSelection(SelectionSet.FromDictionary(DicSelect));
                //}
            });
        }


        /// Crée une table avec le nom chaine dans la gdb, avec les infos de la couche sélectionnée
        public async static Task<bool> CreerTable(string chaine, GDBProjectItem gdbProjectItem)
        {
            Table table = null;
            //vérifie l'existence du template pour pouvoir créer la table temporaire
            if (await QueryDico.ExecuteTestGDB_Template(Project.Current.HomeFolderPath, nom_query))
            {
                GDBProjectItem gdb = Project.Current.GetItems<GDBProjectItem>().FirstOrDefault(l => l.Name.Equals(nom_query + ".gdb"));
                // Teste l'existence de la table
                var chaine1 = gdbProjectItem.Name.ToString() + "\\" + chaine;
                var arguments = Geoprocessing.MakeValueArray(chaine1);

                await QueuedTask.Run(() =>
                {
                    // si elle existe, on la vide
                    try
                    {
                        FeatureClassDefinition Definition = (gdbProjectItem.GetDatastore() as Geodatabase).GetDefinition<FeatureClassDefinition>(chaine1);
                        Definition.Dispose();
                        Geoprocessing.ExecuteToolAsync("Delete", arguments);
                    }
                    // sinon, on la crée
                    catch
                    {
                        // Création d'une table pour stocker les ID/ CLEABS des objets sélectionnés ainsi que la/les classe(s)  associées
                        var Template = gdbProjectItem.Path + "\\" + QueryDico.template_default_name;
                        arguments = Geoprocessing.MakeValueArray(gdbProjectItem, chaine, Template, "DEFAULTS", chaine);
                        Geoprocessing.ExecuteToolAsync("management.CreateTable", arguments);
                    }

                    // la table à remplir.
                    table = (gdbProjectItem.GetDatastore() as Geodatabase).OpenDataset<Table>(chaine);
                    // si la table a été ajoutée dans l'arborescence de la carte, on l'enlève.
                    StandaloneTable table_content = MapView.Active.Map.StandaloneTables.OfType<StandaloneTable>().FirstOrDefault(n => n.Name == chaine);
                    if (table_content != null) MapView.Active.Map.RemoveStandaloneTable(table_content);

                    // On récupère les index des champs qui vont être remplis, en cas d'absence, on envoie un message
                    int assetNameIndex = table.GetDefinition().FindField(QueryDico.inputID_Field);
                    int assetNameClass = table.GetDefinition().FindField(QueryDico.Classe_Field);
                    int assetNameSymbo = table.GetDefinition().FindField(QueryDico.symbo_Field);
                    int assetNameMap = table.GetDefinition().FindField(QueryDico.Carte_Field);
                    if (assetNameIndex == -1 || assetNameClass == -1 || assetNameSymbo == -1 || assetNameMap == -1)
                    {
                        MessageBox.Show(QueryDico.MessageTable + QueryDico.template_default_name + QueryDico.MessageUpdate);
                        return;
                    }
                    RowBuffer rowBuffer = table.CreateRowBuffer();

                    // Récuperer la liste des couches de la sélection
                    List<Layer> list_Layers = new List<Layer>();
                    var Selection_ = MapView.Active.Map.GetSelection();
                    foreach (var temp_selec in Selection_.ToDictionary())
                    {
                        var t = MapView.Active.Map.GetLayersAsFlattenedList().FirstOrDefault(l => l.Name == temp_selec.Key.Name && l.Parent.ToString() == (temp_selec.Key as Layer).Parent.ToString());
                        list_Layers.Add(t);
                    }

                    List<long> List_ID = new List<long>();
                    // pour chaque couche
                    foreach (var l in list_Layers)
                    {
                        if (l != null)
                        {
                            // on récupère la couche sans son modèle classe ou annotation
                            BasicFeatureLayer lf = null;
                            if (l.GetType().Name == "AnnotationLayer") lf = l as BasicFeatureLayer;
                            else if (l.GetType().Name.Contains("FeatureLayer")) lf = l as BasicFeatureLayer;

                            // On récupère les OIDs des sélections sur la couche
                            List_ID = lf.GetSelection().GetObjectIDs().ToList();
                            int idxTableFieldSymbo = lf.GetTable().GetDefinition().FindField("SYMBO");
                            var OidField = lf.GetTable().GetDefinition().GetFields().Where(x => x.FieldType.ToString() == "OID").First().Name;
                            int idxTableFieldID = lf.GetTable().GetDefinition().FindField(OidField);// "OBJECTID");
                            // construit la requête pour les objets d'oids définis par la sélection
                            QueryFilter queryFilter = new QueryFilter() { ObjectIDs = lf.GetSelection().GetObjectIDs().ToList() };
                            RowCursor rowF = lf.GetTable().Search(queryFilter);
                             
                            foreach (var id in List_ID)
                            {
                                rowF.MoveNext();
                                if (idxTableFieldSymbo != -1)
                                    if (rowF.Current[idxTableFieldSymbo] != null)
                                        rowBuffer[assetNameSymbo] = rowF.Current[idxTableFieldSymbo].ToString();
                                rowBuffer[assetNameIndex] = id;
                                rowBuffer[assetNameMap] = l.Map.ToString();
                                string the_name = l.Name;
                                Layer k = l;
                                while (k.Parent.ToString() != MapView.Active.Map.Name)
                                {
                                    the_name = k.Parent.ToString() + "/" + the_name;
                                    k = k.Parent as Layer;
                                }

                                rowBuffer[assetNameClass] = the_name;
                                var def_symbo = lf.GetFieldDescriptions().FirstOrDefault(f => f.Name == "SYMBO");
                                table.CreateRow(rowBuffer);
                            }
                            rowF.Dispose();
                        }
                    }
                }); return true;
            }
            // pas de template, on sort
            else
            {
                MessageBox.Show(QueryDico.WarningGDB + QueryDico.query_default_name + QueryDico.MessageUpdate);
                return false;
            }
        }


        public static string ValidateDataPath(string DataPath)
        {
            var ext = Path.GetExtension(DataPath).ToLower();
            if (ext != ".gdb"
                || Directory.Exists(DataPath) == false)
            {
                return $@"The path: {DataPath} is not a valid File Geodatabase path";
            }
            else return null;
        }


        public static void CopyDir(string sourceDir, string destDir)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDir);
            if (dir.Exists)
            {
                string realDestDir;
                if (dir.Root.Name != dir.Name)
                {
                    realDestDir = Path.Combine(destDir, dir.Name);
                    if (!Directory.Exists(realDestDir))
                        Directory.CreateDirectory(realDestDir);
                }
                else realDestDir = destDir;
                foreach (string d in Directory.GetDirectories(sourceDir))
                    CopyDir(d, realDestDir);
                foreach (string file in Directory.GetFiles(sourceDir))
                {
                    string fileNameDest = Path.Combine(realDestDir, Path.GetFileName(file));
                    File.Copy(file, fileNameDest, true);
                }
            }
        }
    }
}