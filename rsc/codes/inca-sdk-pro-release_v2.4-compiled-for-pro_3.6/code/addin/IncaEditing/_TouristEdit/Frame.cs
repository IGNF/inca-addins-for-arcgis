using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Text.RegularExpressions;
using System.Threading;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.Mapping;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using Button = ArcGIS.Desktop.Framework.Contracts.Button;
using Geometry = ArcGIS.Core.Geometry.Geometry;

namespace IncaEditing._TouristEdit
{
    /// <summary>
    /// Bouton permettant d'encadrer un toponyme
    /// </summary>
    internal class Frame_Button : Button
    {
        protected override void OnClick()
        {
            if (MapView.Active != null && MapView.Active.Map != null)
                if (MapView.Active.IsReady)
                    // crée les encadrés sur les toponymes sélectionnés
                    Frame.Go();
        }
    }

    internal class Frame
    {
        private ResourceSet Translator { get; set; }
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected Frame()
        {
            // get the dictionary of the current UI language ("CurrentUICulture") using the Culture Resources Manager
            Translator = CultureResourcesManager.GetInstance().dictLanguage;
        }

        static Polygon SetFrame(string text, Geometry geom, int delta=0)
        {
            int deltaY_min = 4; int deltaY_max = 0; int deltaX = 8;

            List<Double> geom_X_list = new List<double>();
            List<Double> geom_Y_list = new List<double>();
            IReadOnlyCollection<Coordinate2D> geom_coord_list = null;
            geom_coord_list = (geom as Polygon).Copy2DCoordinatesToList();
            foreach (var c in geom_coord_list) { geom_X_list.Add(c.X); geom_Y_list.Add(c.Y); }

            var x_min = geom_X_list.Min() - deltaX - delta;
            var x_max = geom_X_list.Max() + deltaX;
            var y_max = geom_Y_list.Max();
            var y_min = geom_Y_list.Min();

            // augmentation / diminution  du rectangle en fonction des lettres accentuées
            if (Regex.IsMatch(text, "[pqgyjçÇ]")) deltaY_min = deltaY_min + 4;
            if (Regex.IsMatch(text, "[ÂÊÎÔÛÄËÏÖÜÀÉÈÙ]")) deltaY_max = deltaY_max + 4;

            List<Coordinate2D> plCoords = new List<Coordinate2D>()
            {
                new Coordinate2D(x_max + deltaX, y_min - deltaY_min),
                new Coordinate2D(x_min - deltaX, y_min - deltaY_min),
                new Coordinate2D(x_min - deltaX, y_max + deltaY_max),
                new Coordinate2D(x_max + deltaX, y_max + deltaY_max),
            };

            Polygon frame = PolygonBuilderEx.CreatePolygon(plCoords);
            return frame;
        }

        #region
        public static string MESSAGE_ERROR_T_FRAME
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Attention la classe 't_encadre_n1' n'existe pas ";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Tenga en cuenta que la clase 't_encadre_n1' no existe ";
                else return "Please note that the 't_encadre_n1' class does not exist";
            }
        }
        public static string MESSAGE_ERROR_TEXTSTRING
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Manque le champ 'textstring'";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Falta el campo 'textstring' ";
                else return "Missing the 'textstring' field";
            }
        }

        #endregion

        internal async static void Go()
        {
            // récupère la sélection
            var nom_map = MapView.Active.Map.Name;
            MapProjectItem mapProjItem = Project.Current.GetItems<MapProjectItem>().First(item => item.Name.Equals(nom_map));

            await QueuedTask.Run(async () =>
            {
                Map map = mapProjItem.GetMap();
                // classe des encadrés :
                try
                {
                    FeatureLayer FL_frame = map.GetLayersAsFlattenedList().OfType<FeatureLayer>().First(f => f.Name == "t_encadre_n1");
                    if (FL_frame != null)
                    {
                        var selection_ = map.GetSelection().ToDictionary(); ;
                        // liste des géométries et attributs pour créer les encadrés 
                        List<Dictionary<string, object>> frame_list = new List<Dictionary<string, object>>();

                        foreach (var _selec in selection_)
                        {
                            List<AnnotationLayer> List_AL = map.GetLayersAsFlattenedList().OfType<AnnotationLayer>().
                            Where(f => f.Name == _selec.Key.Name.Split('/').Last()).ToList();

                            // vérifie la sélection (on ne traite que les toponymes)                       
                            foreach (AnnotationLayer AL in List_AL)
                            {
                                // vérifie la sélection (seulement des toponymes)
                                if (AL.GetSelection().GetCount() == 0)
                                    continue;
                                else
                                    try
                                    {
                                        Field libelle_field = null, geom_field = null, cleabs_field = null, cleabs25_field = null, symbo_field = null;
                                        List<Field> fields_list = AL.GetFeatureClass().GetDefinition().GetFields().ToList();
                                        foreach (var fl in fields_list)
                                        {
                                            // les attributs nécessaires à passer en paramètres pour la création de l'encadré                                   
                                            if (fl.Name.ToLowerInvariant() == "textstring") libelle_field = fl;
                                            if (fl.FieldType.ToString() == "Geometry") geom_field = fl;

                                            // Les attributs facultatifs à passer en paramètres pour la création de l'encadré
                                            if (fl.Name.ToLowerInvariant() == "cleabs") cleabs_field = fl;
                                            if (fl.Name.ToLowerInvariant() == "cleabs25") cleabs25_field = fl;

                                            // Champ de la classe d'arrivée
                                            if (fl.Name.ToLowerInvariant() == "symbo") symbo_field = fl;

                                        }
                                        if (libelle_field != null && geom_field != null)
                                        {
                                            //parcours de la sélection
                                            using (RowCursor rowCursor = AL.GetSelection().Search())
                                            {
                                                while (rowCursor.MoveNext())
                                                {
                                                    List<string> list_attributs = new List<string>();
                                                    var record = rowCursor.Current;

                                                    string text = record[AL.GetTable().GetDefinition().FindField(libelle_field.Name)].ToString();
                                                    // on le sépare : 
                                                    string[] chaine = Underline.splitText(text);
                                                    Geometry geom = null;
                                                    // Ajout de tous les attibuts jors géométrie au dictionnaire d'attributs
                                                    var atts = new Dictionary<string, object>();
                                                    atts.Add(libelle_field.Name, text);
                                                    if (cleabs_field != null && record[AL.GetTable().GetDefinition().FindField(cleabs_field.Name)] != null)
                                                        atts.Add(cleabs_field.Name, record[AL.GetTable().GetDefinition().FindField(cleabs_field.Name)].ToString());
                                                    if (cleabs25_field != null && record[AL.GetTable().GetDefinition().FindField(cleabs25_field.Name)] != null)
                                                        atts.Add(cleabs25_field.Name, record[AL.GetTable().GetDefinition().FindField(cleabs25_field.Name)].ToString());
                                                    if (symbo_field != null) atts.Add(symbo_field.Name, "ENCADRE");
                                                    
                                                    // gestion de la géométrie en fonction de la chaine de caractère
                                                    if (chaine.Length == 1)
                                                    {
                                                        geom = (record[AL.GetTable().GetDefinition().FindField(geom_field.Name)] as Polygon);                                                                                              
                                                        atts.Add(geom_field.Name, SetFrame(text, geom));
                                                        frame_list.Add(atts);
                                                    }
                                                    else
                                                    {
                                                        // découpage de la chaine en fonction des mots pour récupérer chaque géométrie
                                                        Polygon poly = null;
                                                        List<Geometry> geom_part = new List<Geometry>();
                                                        foreach (var c in chaine)
                                                        {
                                                            using (RowBuffer rowBuffer = AL.GetTable().CreateRowBuffer())
                                                            { // copie de tous les champs
                                                                foreach (var fld in fields_list)
                                                                    if (fld.IsEditable && fld.IsRequired)
                                                                        rowBuffer[fld.Name] = record[fld.Name];
                                                                // Crée un objet temporaire pour mettre à jour le texte et générer la nouvelle géométrie.
                                                                Feature feature = (AL.GetFeatureClass() as AnnotationFeatureClass).CreateRow(rowBuffer);
                                                                AnnotationFeature annoFeat = feature as AnnotationFeature;
                                                                var graphic = annoFeat.GetGraphic();
                                                                var cimTextGraphic = graphic as CIMTextGraphic;

                                                                // et le met à jour
                                                                cimTextGraphic.Text = c;
                                                                annoFeat.SetGraphic(cimTextGraphic);
                                                                poly = record[AL.GetTable().GetDefinition().FindField(geom_field.Name)] as Polygon;

                                                                // suppression de l'objet temporaire
                                                                feature.Delete();
                                                            }
                                                        }

                                                        if (poly.PartCount == 1)
                                                        {
                                                            atts.Add(geom_field.Name, SetFrame(text, poly as Geometry));
                                                            frame_list.Add(atts);
                                                        }
                                                        else if (poly.PartCount > 1)
                                                        {
                                                            // on prend le rectangle englobant
                                                            Polygon rectangle = PolygonBuilderEx.CreatePolygon(poly.Extent);
                                                            atts.Add(geom_field.Name, SetFrame(text, rectangle, 20));
                                                            frame_list.Add(atts);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(MESSAGE_ERROR_TEXTSTRING);
                                    }
                            }
                        }

                        if (frame_list.Count() > 0)
                        {
                            map.ClearSelection();

                            // Session d'édition pour la création des objets "ENCADRES"
                            // on  vérifie que la couche soit éditable
                            bool iseditable = FL_frame.IsEditable;
                            FL_frame.SetEditable(true);
                            var createOperation = new EditOperation();
                            //pour chaque encadré enregistré dans la liste, on l'ajoute à la classe.
                            foreach (var newPolygon_atts in frame_list) createOperation.Create(FL_frame, newPolygon_atts);

                            await createOperation.ExecuteAsync();
                            // on  remet la couche dans son état édition
                            FL_frame.SetEditable(iseditable);
                            // sauvegarde du projet ?
                            //await Project.Current.SaveEditsAsync();
                        }
                    }
                    else ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(MESSAGE_ERROR_T_FRAME);
                }
                catch
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(MESSAGE_ERROR_T_FRAME);
                }
            });
        }
    }
}
