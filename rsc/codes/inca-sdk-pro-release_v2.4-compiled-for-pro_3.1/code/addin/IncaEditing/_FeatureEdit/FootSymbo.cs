using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using Field = ArcGIS.Core.Data.Field;
using Geometry = ArcGIS.Core.Geometry.Geometry;
using Polygon = ArcGIS.Core.Geometry.Polygon;
using QueryFilter = ArcGIS.Core.Data.QueryFilter;
using Segment = ArcGIS.Core.Geometry.Segment;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

namespace IncaEditing._FeatureEdit
{
    /// <summary>
    /// Bouton permettant d'encadrer un toponyme
    /// </summary>
    internal class FootSymboTool : Button
    {
        protected override void OnClick()
        {
            MapView mapview = MapView.Active;
            if (mapview == null)
                return;

            Map map = MapView.Active.Map;

            // parcours et eregistre le fichier
            if (map != null)
                FootSymbo.Go();
        }
    }

    internal class FootSymbo
    {
        private ResourceSet Translator { get; set; }
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected FootSymbo()
        {
            // get the dictionary of the current UI language ("CurrentUICulture") using the Culture Resources Manager
            Translator = CultureResourcesManager.GetInstance().dictLanguage;
        }

        const string b_symbole = "b_symbole_lin_n1";
        const string b_sport_surf = "b_sport_surf_n3";
        const string symbo_ligne = "LIGNE_FOOT";
        const string symbo_terrain = "FOOT_SURF";

        #region MESSAGES
        public static string MESSAGE_ERROR_SPORT
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Attention la classe " + b_sport_surf + " n'existe pas ";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Tenga en cuenta que la clase " + b_sport_surf + " no existe ";
                else return "Please note that the " + b_sport_surf + " class does not exist";
            }
        }
        public static string MESSAGE_ERROR_SYMBOLE
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Attention la classe " + b_symbole + " n'existe pas ";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Tenga en cuenta que la clase " + b_symbole + " no existe ";
                else return "Please note that the " + b_symbole + " class does not exist";
            }
        }

        public static string MESSAGE_ERROR_OBJECT
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Problème avec la clé absolue de l'objet d'ID : ";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Problema con la clave absoluta del objeto con ID: ";
                else return "Problem with the absolute key of the object with ID: ";
            }
        }

        public static string MESSAGE_ERROR_SELECTION
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "La sélection est vide ou ne contient pas d'objets de la classe " + b_sport_surf + ".";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "La selección está vacía o no contiene ningún objeto de la clase " + b_sport_surf + ".";
                else return "The selection is empty or does not contain any objects of " + b_sport_surf + " class.";
            }
        }

        #endregion

        public static List<Segment> SortSegmentsByLength(List<Segment> segments)
        {
            // Trier les segments par longueur (calculée avec la méthode GetLength)
            return segments.OrderBy(s => s.Length).ToList();

        }

        public static Geometry CreateRectangle(Geometry geometry)
        {
            // Ensure the geometry is valid and not empty
            if (geometry == null || geometry.IsEmpty) return null;

            return GeometryEngine.Instance.Generalize(geometry, 10);
        }

        internal async static void Go()
        {

            //Récupérer la vue active de la carte            
            Map map = MapView.Active.Map;

            // Couches à remplir 
            FeatureLayer FL_SYMBO = null, FL_SPORT = null;
            // Dictionnaire d ela sélection en cours
            Dictionary<MapMember, List<long>> selection = new Dictionary<MapMember, List<long>>();
            // Liste des objets à créer
            List<Dictionary<string, object>> sport_list = new List<Dictionary<string, object>>();

            //identifier les couches qui ne sont pas correctement liées à une classe d'entités ou une source de données,
            List<FeatureLayer> blockedLayers_list = new List<FeatureLayer>();
            foreach (var l in map.GetLayersAsFlattenedList().OfType<FeatureLayer>())
            {
                await QueuedTask.Run(() =>
                    {
                        if (l.GetFeatureClass() is null)
                            blockedLayers_list.Add(l);
                    });
            }

            await QueuedTask.Run(() =>
            {
                // Recherche les 2 classes où créer les lignes et ronds des terains de foot.
                try
                {
                    List<FeatureLayer> LayersOfMap = map.GetLayersAsFlattenedList().OfType<FeatureLayer>().ToList();
                    List<FeatureLayer> LayersOfMapPossible = LayersOfMap.Except(blockedLayers_list).ToList();
                    FL_SPORT = LayersOfMap.Except(blockedLayers_list).ToList().FirstOrDefault(f => f.GetFeatureClass().GetName() == b_sport_surf); ;
                    if (FL_SPORT is null) { MessageBox.Show(MESSAGE_ERROR_SPORT); return; }
                    FL_SYMBO = LayersOfMap.Except(blockedLayers_list).ToList().FirstOrDefault(f => f.GetFeatureClass().GetName() == b_symbole); ;
                    if (FL_SYMBO is null) { MessageBox.Show(MESSAGE_ERROR_SYMBOLE); return; }

                    selection = map.GetSelection().ToDictionary();

                    if (selection.Count > 0)
                    {   //on nettoie la sélection de ce qui n'est pas un terrain de sport.
                        foreach (var selec in selection)
                            if (selec.Key.Name.Split('/').Last() != FL_SPORT.Name)
                                selection.Remove(selec.Key);
                    }
                    // si la sélection ne compte pas que des objets de la couche b_sport_surf
                    if (selection.Count > 1 || selection.Count == 0)
                    {
                        MessageBox.Show(MESSAGE_ERROR_SELECTION);
                        return;
                    }

                    if (selection.Count == 1)
                    {
                        // Session d'édition pour la création des objets                     
                        var createOperation = new EditOperation();

                        // les champs à remplir
                        List<Field> fields_list = null;
                        fields_list = FL_SYMBO.GetFeatureClass().GetDefinition().GetFields().ToList();

                        Field geom_field = null, cleabs_field = null, symbo = null, comment = null;
                        foreach (var fld in fields_list)
                        {
                            switch (fld.Name.ToLowerInvariant())
                            {
                                case "cleabs":
                                    cleabs_field = fld;
                                    break;
                                case "comment":
                                    comment = fld;
                                    break;
                                case "symbo":
                                    symbo = fld;
                                    break;
                                default:
                                    break;
                            }
                            switch (fld.FieldType.ToString())
                            {
                                case "Geometry":
                                    geom_field = fld;
                                    break;
                                default:
                                    break;
                            }
                        }

                        bool test = false;
                        // teste s'il est nécessaire de calculer les rectangles engmlobants.
                        // si la géométrie a plus de 5 points, il faut simplifier.
                        foreach (var selec in selection)
                        {
                            for (int i = 0; i < selec.Value.Count(); i++)
                            {
                                if (test == false)
                                {
                                    var liste = FL_SPORT.GetTable().GetDefinition().GetFields();
                                    int idxTableFieldgeom = FL_SPORT.GetTable().GetDefinition().FindField("shape");
                                    // requête pour sélection l'objet avec l'OID concerné.
                                    var queryFilter = new QueryFilter { WhereClause = "OBJECTID =" + selec.Value[i] };
                                    using (var rowCursor = FL_SPORT.GetTable().Search(queryFilter))
                                        while (rowCursor.MoveNext())
                                            using (Row row = rowCursor.Current)
                                            {
                                                // récupère et teste la géométrie
                                                Geometry geom_test = (row[idxTableFieldgeom] as Polygon).Clone();
                                                if (geom_test.PointCount > 5) test = true;
                                            }
                                }
                            }
                        }
                        // parcours les sélections et calcule la géométrie de l'objet enceinte.
                        foreach (var selec in selection)
                        {
                            for (int i = 0; i < selec.Value.Count(); i++)
                            {
                                string cleabs_value = "", cleabs25_value = "", symbo_value = ""; Geometry geom = null;
                                string OidField = FL_SPORT.GetFeatureClass().GetDefinition().GetFields().Where(x => x.FieldType.ToString() == "OID").First().Name;
                                int idxTableFieldcleabs25 = FL_SPORT.GetTable().GetDefinition().FindField("cleabs25");
                                int idxTableFieldcleabs = FL_SPORT.GetTable().GetDefinition().FindField("cleabs");
                                int idxTableFieldsymbo = FL_SPORT.GetTable().GetDefinition().FindField("symbo");

                                // on récupère la cleabs25 de l'objet terrain de sport en cours pour trouver la géométrie simplifiée dans la classe calculée
                                var queryFilter = new QueryFilter { WhereClause = OidField + " = " + selec.Value[i] };
                                using (RowCursor rowCursor = FL_SPORT.GetTable().Search(queryFilter))
                                {
                                    while (rowCursor.MoveNext())
                                    {
                                        using (Row row = rowCursor.Current)
                                        {
                                            if (idxTableFieldcleabs25 != -1)
                                                if (row.GetOriginalValue(idxTableFieldcleabs25) is not null)
                                                    cleabs25_value = row.GetOriginalValue(idxTableFieldcleabs25).ToString();

                                            if (idxTableFieldsymbo != -1)
                                                if (row.GetOriginalValue(idxTableFieldsymbo) is not null)
                                                    symbo_value = row.GetOriginalValue(idxTableFieldsymbo).ToString();

                                            if (idxTableFieldcleabs != -1)
                                                if (row.GetOriginalValue(idxTableFieldcleabs) is not null)
                                                    cleabs_value = row.GetOriginalValue(idxTableFieldcleabs).ToString();
                                        }
                                    }
                                    if (idxTableFieldcleabs25 != -1 && cleabs25_value != "")
                                    {     //parcours de la sélection
                                        queryFilter = new QueryFilter { WhereClause = "cleabs25" + "= '" + cleabs25_value + "'" };
                                    }
                                    else if (idxTableFieldcleabs != -1 && cleabs_value != "")
                                    {     //parcours de la sélection
                                        queryFilter = new QueryFilter { WhereClause = "cleabs" + "= '" + cleabs_value + "'" };
                                    }
                                    else
                                        queryFilter = new QueryFilter();

                                }
                                // on ne calcule les symbo que sur les terrains de foot
                                if (symbo_value == symbo_terrain)
                                {
                                    using (var rowCursor = FL_SPORT.GetTable().Search(queryFilter))
                                        while (rowCursor.MoveNext())
                                            using (Row row = rowCursor.Current)
                                                geom = (row[FL_SPORT.GetTable().GetDefinition().FindField(geom_field.Name)] as Polygon).Clone();

                                    // si la géométrie est complexe : on prend la généralisation
                                    if (test) geom = CreateRectangle(geom);

                                }
                                //on ne traite pas l'objet
                                else return;

                                if (geom != null)
                                {
                                    // attributs pour la création de l'objet
                                    var atts = new Dictionary<string, object>();
                                    // ajout à l'attribut des champs nécessaires
                                    if (cleabs_value != "")
                                        if (cleabs_field != null)
                                            atts.Add(cleabs_field.Name, cleabs_value);
                                        else if (cleabs25_value != "")
                                            if (comment != null)
                                                atts.Add(comment.Name, cleabs25_value);
                                    if (symbo != null)
                                        atts.Add(symbo.Name, symbo_ligne);

                                    // on récupère les segments
                                    List<Segment> segmentList = new List<Segment>(30);
                                    ICollection<Segment> collection = segmentList;
                                    (geom as Polygon).GetAllSegments(ref collection);
                                    segmentList = collection.ToList();

                                    // tri dans l'ordre de grandeur
                                    List<Segment> t = SortSegmentsByLength(segmentList);

                                    // calcul des points sur les deux axes les plus longs.
                                    int n = t.Count - 1;
                                    var x1 = t[n - 1].StartPoint.X + (t[n - 1].EndPoint.X - t[n - 1].StartPoint.X) / 2;
                                    var y1 = t[n - 1].StartPoint.Y + (t[n - 1].EndPoint.Y - t[n - 1].StartPoint.Y) / 2;
                                    var x2 = t[n].StartPoint.X + (t[n].EndPoint.X - t[n].StartPoint.X) / 2;
                                    var y2 = t[n].StartPoint.Y + (t[n].EndPoint.Y - t[n].StartPoint.Y) / 2;
                                    List<Coordinate2D> plCoords = new List<Coordinate2D>
                                {
                                    new Coordinate2D(x1, y1),
                                    new Coordinate2D(x2, y2)
                                };
                                    // ajout de la géométrie à l'attributs
                                    atts.Add(geom_field.Name, PolylineBuilderEx.CreatePolyline(plCoords));
                                    // ajout de l'attribut à la liste des objets à traiter
                                    sport_list.Add(atts);
                                }
                            }
                        }

                        if (sport_list.Count() > 0)
                        {
                            map.ClearSelection();
                            // on  vérifie que la couche soit éditable
                            bool iseditable = FL_SYMBO.IsEditable;
                            FL_SYMBO.SetEditable(true);

                            //pour chaque terrain de foot dans la liste, on l'ajoute la symbo de la ligne à la classe.
                            foreach (var newPolygon_atts in sport_list) createOperation.Create(FL_SYMBO, newPolygon_atts);

                            createOperation.ExecuteAsync();

                            // on  remet la couche dans son état édition
                            FL_SYMBO.SetEditable(iseditable);

                            // sauvegarde du projet ?
                            //await Project.Current.SaveEditsAsync();               
                        }
                        //map.ClearSelection();
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            });
        }
    }
}
