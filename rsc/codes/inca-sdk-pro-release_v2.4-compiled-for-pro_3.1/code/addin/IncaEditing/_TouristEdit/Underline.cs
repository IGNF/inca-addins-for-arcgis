using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.Mapping;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Internal.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;

namespace IncaEditing._TouristEdit
{
    /// <summary>
    /// Bouton permettant de souligner un toponyme
    /// </summary>
    internal class Underline_Button : Button
    {
        protected override void OnClick()
        {
            if (MapView.Active != null && MapView.Active.Map != null)
                if (MapView.Active.IsReady)
                    // crée les soulignés sur les toponymes sélectionnés
                    Underline.Go();
                else return;
        }
    }

    /// <summary>
    /// Souligne les toponymes sélectionnés
    /// </summary>
    internal class Underline
    {
        private ResourceSet Translator { get; set; }
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #region dictionnaire        
        public static string MESSAGE_ERROR_T_UNDERLINE
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Attention la classe 't_souligne_n1' n'existe pas ";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Tenga en cuenta que la clase 't_souligne_n1' no existe ";
                else return "Please note that the 't_souligne_n1' class does not exist";
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
        #endregion dictionnaire

        protected Underline()
        {
            // get the dictionary of the current UI language ("CurrentUICulture") using the Culture Resources Manager
            Translator = CultureResourcesManager.GetInstance().dictLanguage;
        }

        /// <summary>
        /// calcul de la polyligne "souligné" depuis une géometrie
        /// </summary>
        /// <param name="text"></param>
        /// <param name="geom"></param>
        /// <returns></returns>
        static Polyline SetUnderline(string text, Geometry geom)
        {
            var deltay = 0;
            List<Double> geom_X_list = new List<double>();
            List<Double> geom_Y_list = new List<double>();
            IReadOnlyCollection<Coordinate2D> geom_coord_list = null;
            // lecture des coordonnées du polygone englobant le texte
            geom_coord_list = geom is Polygon ? (geom as Polygon).Copy2DCoordinatesToList() : (geom as Polyline).Copy2DCoordinatesToList();

            foreach (var c in geom_coord_list) { geom_X_list.Add(c.X); geom_Y_list.Add(c.Y); }
            // xmin, xmax, ymin, ymax.
            var (x_min, y_min) = (geom_X_list.Min(), geom_Y_list.Min());
            var (x_max, y_max) = (geom_X_list.Max(), geom_Y_list.Max());

            // modification du décalage par rapport au texte en fonction des caractères bas
            if (text is not null) if (Regex.IsMatch(text, "[pqgyjçÇ]")) deltay = deltay - 5;
            // construction des coordonnées pour créer la polyligne
            List<Coordinate2D> plCoords = new List<Coordinate2D>
            {
                new Coordinate2D(x_max, y_min + deltay),
                new Coordinate2D(x_min, y_min + deltay)
            };
            Polyline underline = PolylineBuilderEx.CreatePolyline(plCoords);
            return underline;
        }

        /// <summary>
        /// calcul de la polyligne "souligné" avec les coordonnées
        /// </summary>
        /// <param name="text"></param>
        /// <param name="plCoords"></param>
        /// <returns></returns>
        static Polyline SetUnderline(string text, List<Coordinate2D> plCoords)
        {
            var deltay = 0;

            // modification du décalage par rapport au texte en fonction des caractères bas
            if (text is not null) if (Regex.IsMatch(text, "[pqgyjçÇ]")) deltay = deltay - 5;

            // construction des coordonnées pour créer la polyligne
            List<Coordinate2D> Coords = new List<Coordinate2D>
            {
                new Coordinate2D(plCoords.Last().X, plCoords.First().Y + deltay),
                new Coordinate2D(plCoords.First().X, plCoords.First().Y + deltay)
            };

            Polyline underline = PolylineBuilderEx.CreatePolyline(Coords);

            return underline;
        }


        /// <summary>
        /// Outil de séparation du texte avec les différents codage de retour à la ligne
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string[] splitText(string text)
        {
            string[] resultat = null;
            string[] chaines = null;

            if (text.Contains("\r\n"))
            {
                // Utilisation de Regex pour diviser en prenant en compte \r\n tout en gardant les \r et \n séparément
                resultat = Regex.Split(text, @"(\r\n)");
                chaines = new string[resultat.Length - 1];

                // Construction du résultat selon le format voulu
                for (int i = 0; i < resultat.Length; i++)
                {
                    if (resultat[i] == "\r\n")
                    {
                        // Sépare le \r et \n sur les nouvelles lignes
                        chaines[i - 1] += "\r";
                        chaines[i] = "\n" + resultat[i + 1];
                        i++;
                    }
                    else
                        chaines[i] = resultat[i];
                }
            }
            else if (text.Contains("\n"))
            {
                int startIndex = 1;

                resultat = text.Split("\n");
                chaines = new string[resultat.Length];
                chaines[0] = resultat[0];

                for (int i = 1; i < resultat.Length; i++)
                {
                    string prefixe = new string('\n', startIndex); // Crée une chaîne de préfixes en fonction de la variable
                    chaines[i] = prefixe + resultat[i];
                    startIndex++;
                }
            }
            else
            {
                chaines = new string[1];
                chaines[0] = text;
            }
            return chaines;
        }


        /// <summary>
        /// Outil de calcul des soulignés avec les segments bas des boites englobantes du toponyme
        /// </summary>
        /// <param name="texte"></param>
        /// <param name="segmentList"></param>
        /// <param name="Fields_List"></param>
        /// <param name="record"></param>
        /// <param name="AL"></param>
        /// <returns></returns>
        public static List<Dictionary<string, object>> underlined_calculation(string texte, List<Polyline> segmentList,
            List<Field> Fields_List, Row record, AnnotationLayer AL)
        {

            // liste des attributs pour créer les soulignés 
            List<Dictionary<string, object>> list_underline = new List<Dictionary<string, object>>();

            // Extraire tous les points
            var allPoints = segmentList.SelectMany(seg => new[] { seg.Points.First(), seg.Points.Last() });

            // Grouper par Y proche (±3)
            var groupesParY = new List<List<MapPoint>>();
            foreach (var pt in allPoints)
            {
                // Cherche un groupe existant dont le Y est proche
                var groupeTrouvé = groupesParY.FirstOrDefault(g => g.Any(p => Math.Abs(p.Y - pt.Y) <= 3));

                if (groupeTrouvé != null) groupeTrouvé.Add(pt);
                else groupesParY.Add(new List<MapPoint> { pt });
            }

            //// s'il y a plus de 4 groupes, le toponyme doit être courbe
            //// on ne crée que le premier et le dernier point.
            List<Coordinate2D> plCoords = new List<Coordinate2D>();
            if (groupesParY.Count > 4)
            {
                // récupère tous les points dans une seule liste
                var tousLesPoints = groupesParY.SelectMany(groupe => groupe).ToList();

                // Trouver les Y min et max
                double minY = tousLesPoints.Min(p => p.Y);
                double maxY = tousLesPoints.Max(p => p.Y);

                // Trouver les X correspondants
                var XpointMinY = tousLesPoints.First(p => p.Y == minY).X;
                var XpointMaxY = tousLesPoints.First(p => p.Y == maxY).X;

                //coordonnées du souligné à créer
                plCoords = new List<Coordinate2D>
                    {
                      new Coordinate2D(XpointMinY, minY),
                      new Coordinate2D(XpointMaxY, maxY)
                    };
                list_underline.Add(atts_create(plCoords, Fields_List, texte, record, AL));
            }
            else
            {
                // Affichage des groupes avec X min / X max            
                foreach (var groupe in groupesParY)
                {
                    double yMoyen = groupe.Average(p => p.Y);
                    double minX = groupe.Min(p => p.X);
                    double maxX = groupe.Max(p => p.X);
                    plCoords = new List<Coordinate2D>
                    {
                      new Coordinate2D(minX, yMoyen),
                      new Coordinate2D(maxX, yMoyen)
                    };
                    list_underline.Add(atts_create(plCoords, Fields_List, texte, record, AL));
                }
            }

            return list_underline;
        }


        public static Dictionary<string, object> atts_create(List<Coordinate2D> plCoords, List<Field> Fields_List, string texte, Row record, AnnotationLayer AL)
        {
            Field libelle_field = Fields_List[0];
            Field geom_field = Fields_List[1];
            Field cleabs_field = Fields_List[2]; ;
            Field cleabs25_field = Fields_List[3];
            Field symbo_field = Fields_List[4];

            Dictionary<string, object> atts = new Dictionary<string, object>
            {
                {libelle_field.Name, texte},
                { geom_field.Name, SetUnderline(texte, plCoords)},
                { cleabs_field.Name, (cleabs_field != null && record[AL.GetTable().GetDefinition().FindField(cleabs_field.Name)] != null)?
                            record[AL.GetTable().GetDefinition().FindField(cleabs_field.Name)].ToString():""},
                { cleabs25_field.Name,(cleabs25_field != null && record[AL.GetTable().GetDefinition().FindField(cleabs25_field.Name)] != null)?
                            record[AL.GetTable().GetDefinition().FindField(cleabs25_field.Name)].ToString():"" },
                { symbo_field.Name, symbo_field != null?  "SOULIGNE":""}
            };

            return atts;
        }

        /// <summary>
        /// 
        /// </summary>
        internal async static void Go()
        {
            // récupère la sélection
            var nom_map = MapView.Active.Map.Name;
            MapProjectItem mapProjItem = Project.Current.GetItems<MapProjectItem>().First(item => item.Name.Equals(nom_map));

            await QueuedTask.Run(async () =>
            {
                Map map = mapProjItem.GetMap();
                //classe des soulignés :
                try
                {
                    // classe pour créer les soulingés.
                    FeatureLayer FL_underline = map.GetLayersAsFlattenedList().OfType<FeatureLayer>().First(f => (f.Name == "t_souligne_n1"));
                    if (FL_underline != null)
                    {
                        var selection_ = map.GetSelection().ToDictionary();
                        // liste des géométries et attributs pour créer les soulignés 
                        List<Dictionary<string, object>> list_underline = new List<Dictionary<string, object>>();

                        // pour toutes les couches sélectionnées, on ne va traiter que les toponymes
                        foreach (var _selec in selection_)
                        {
                            List<AnnotationLayer> List_AL = map.GetLayersAsFlattenedList().OfType<AnnotationLayer>().Where(f => f.Name == _selec.Key.Name.Split('/').Last()).ToList();

                            // vérifie la sélection (on ne traite que les toponymes)                       
                            foreach (AnnotationLayer AL in List_AL)
                            {
                                if (AL.GetSelection().GetCount() == 0)
                                    continue;
                                else
                                    try
                                    {
                                        // recherche les champs à copier
                                        Field libelle_field = null, geom_field = null, cleabs_field = null, cleabs25_field = null, symbo_field = null;

                                        List<Field> allfields_list = AL.GetFeatureClass().GetDefinition().GetFields().ToList();
                                        foreach (var fl in allfields_list)
                                        {
                                            // les attributs nécessaires à passer en paramètres pour la création de l'encadré                                   
                                            if (fl.Name.ToLowerInvariant() == "textstring") libelle_field = fl;
                                            if (fl.FieldType.ToString() == "Geometry") geom_field = fl;

                                            // Les attributs facultatifs à passer en paramètres pour la création de l'encadré
                                            if (fl.Name.ToLowerInvariant() == "cleabs") cleabs_field = fl;
                                            if (fl.Name.ToLowerInvariant() == "cleabs25") cleabs25_field = fl;

                                            // Champ de la classe d'arrivée
                                            if (fl.Name.ToLowerInvariant() == "symbo") symbo_field = fl;
                                            List<Field> Fields_List = new List<Field>
                                            {
                                                libelle_field, geom_field, cleabs_field, cleabs25_field, symbo_field
                                            };
                                        }

                                        // si on a un toponyme à traiter
                                        if (libelle_field != null && geom_field != null)
                                        {
                                            //parcours de la sélection
                                            using (RowCursor rowCursor = AL.GetSelection().Search())
                                            {
                                                while (rowCursor.MoveNext())
                                                {
                                                    var record = rowCursor.Current;
                                                    // Recherche du texte pour déconstruire les toponymes sur plusieurs lignes.
                                                    string text = record[AL.GetTable().GetDefinition().FindField(libelle_field.Name)].ToString();
                                                    // on le sépare : 
                                                    string[] chaine = splitText(text);

                                                    // si le texte est sur une seule ligne, on crée la ligne avec la géométrie par défaut.
                                                    if (chaine.Length == 1)
                                                    {
                                                        Geometry geom = record[AL.GetTable().GetDefinition().FindField(geom_field.Name)] as Geometry;
                                                        if (geom is Polygon polygon)
                                                        {
                                                            // 1 ligne 1 partie, facile :-)
                                                            // on stocke les infos à recopier et la géométrie du souligné.
                                                            if (polygon.PartCount == 1)
                                                            {
                                                                List<string> list_attributs = new List<string>();
                                                                // include the attributes via a dictionary
                                                                var atts = new Dictionary<string, object>();
                                                                //on récupère toutes les valeurs des champs à copier.
                                                                atts.Add(libelle_field.Name, text);
                                                                if (cleabs_field != null && record[AL.GetTable().GetDefinition().FindField(cleabs_field.Name)] != null)
                                                                    atts.Add(cleabs_field.Name, record[AL.GetTable().GetDefinition().FindField(cleabs_field.Name)].ToString());
                                                                if (cleabs25_field != null && record[AL.GetTable().GetDefinition().FindField(cleabs25_field.Name)] != null)
                                                                    atts.Add(cleabs25_field.Name, record[AL.GetTable().GetDefinition().FindField(cleabs25_field.Name)].ToString());
                                                                if (symbo_field != null) atts.Add(symbo_field.Name, "SOULIGNE");

                                                                // calcul de la ligne du souligné
                                                                atts.Add(geom_field.Name, SetUnderline(text, geom));

                                                                //ajout à la liste pour ajout à la base en global
                                                                list_underline.Add(atts);
                                                            }
                                                            // 1 ligne, plusieurs parties
                                                            else if (polygon.PartCount > 1)
                                                            {
                                                                // si l'objet est un toponyme F25 concaténé,
                                                                // le texte est sur une seule ligne, mais la geometrie est multiple.
                                                                // il y a autant de géométrie que de mots                                                 
                                                                List<Field> Fields_List = new()
                                                                {
                                                                    libelle_field, geom_field, cleabs_field, cleabs25_field, symbo_field
                                                                };

                                                                List<Polyline> segmentList = new();
                                                                //on récupère l'ensemble des segments qui "soulignent" les lettres.
                                                                var parts = polygon.Parts;
                                                                foreach (var part in parts)
                                                                {
                                                                    int i = 0;
                                                                    foreach (var seg in part)
                                                                    {
                                                                        // on prend le segment du dessous du polygone.
                                                                        if (i >= part.Count() / 2 && seg != null)
                                                                        {
                                                                            var seg_ = SetUnderline(i.ToString(), PolylineBuilder.CreatePolyline(seg));
                                                                            if (seg_ != null && seg_.Length != 0)
                                                                                segmentList.Add(seg_);
                                                                        }
                                                                        i++;
                                                                    }
                                                                }
                                                                // on reconstruit les soulignés.
                                                                list_underline = underlined_calculation(chaine[0], segmentList, Fields_List, record, AL);
                                                            }
                                                        }
                                                        else MessageBox.Show("Étrange cette géométrie, cas non traité :-(  ");
                                                    }
                                                    else
                                                    {
                                                        // si le texte est sur plusieurs lignes, on traite chaque bloc.
                                                        foreach (var c in chaine)
                                                        {
                                                            using (RowBuffer rowBuffer = AL.GetTable().CreateRowBuffer())
                                                            {
                                                                // copie de tous les champs
                                                                foreach (var fld in allfields_list)
                                                                    if (fld.IsEditable && fld.IsRequired)
                                                                        rowBuffer[fld.Name] = record[fld.Name];

                                                                // Crée un objet temporaire pour mettre à jour le texte et générer la nouvelle géométrie.
                                                                Feature feature = (AL.GetFeatureClass() as AnnotationFeatureClass).CreateRow(rowBuffer);
                                                                AnnotationFeature annoFeat = feature as AnnotationFeature;

                                                                // ajoute le text : CIMTextGraphic
                                                                var graphic = annoFeat.GetGraphic();
                                                                var cimTextGraphic = graphic as CIMTextGraphic;

                                                                //MessageBox.Show(cimTextGraphic.Text);
                                                                // et le met à jour
                                                                cimTextGraphic.Text = c;
                                                                annoFeat.SetGraphic(cimTextGraphic);

                                                                // MessageBox.Show(cimTextGraphic.Text);

                                                                // Ajout des attributs à un dictionnary
                                                                var atts = new Dictionary<string, object>();
                                                                atts.Add(libelle_field.Name, text);
                                                                if (cleabs_field != null && record[AL.GetTable().GetDefinition().FindField(cleabs_field.Name)] != null)
                                                                    atts.Add(cleabs_field.Name, record[AL.GetTable().GetDefinition().FindField(cleabs_field.Name)].ToString());
                                                                if (cleabs25_field != null && record[AL.GetTable().GetDefinition().FindField(cleabs25_field.Name)] != null)
                                                                    atts.Add(cleabs25_field.Name, record[AL.GetTable().GetDefinition().FindField(cleabs25_field.Name)].ToString());
                                                                if (symbo_field != null) atts.Add(symbo_field.Name, "SOULIGNE");

                                                                // calcul de la ligne avec la géométrie du nouvel objet (feature)
                                                                atts.Add(geom_field.Name, SetUnderline(cimTextGraphic.Text, feature.GetShape()));

                                                                //ajout à la liste pour ajout à la base en global
                                                                list_underline.Add(atts);

                                                                // suppression de l'objet temporaire
                                                                feature.Delete();
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    catch { map.GetSelection().ToDictionary().Remove(_selec.Key); }
                            }
                        }

                        //Session d'édition pour la création des objets "SOULIGNES"
                        if (list_underline.Count() > 0)
                        {
                            map.ClearSelection();
                            // on  vérifie que la couche soit éditable
                            bool iseditable = FL_underline.IsEditable;
                            FL_underline.SetEditable(true);
                            var createOperation2 = new EditOperation();
                            // pour chaque souligné enregistré dans la liste, on l'ajoute à la classe.
                            foreach (var newPolyline_atts in list_underline)
                                createOperation2.Create(FL_underline, newPolyline_atts);

                            await createOperation2.ExecuteAsync();

                            // on  remet la couche dans son état édition
                            FL_underline.SetEditable(iseditable);

                            // sauvegarde du projet ?
                            // await Project.Current.SaveEditsAsync();
                        }
                    }
                    else MessageBox.Show(MESSAGE_ERROR_T_UNDERLINE);
                }
                catch { MessageBox.Show(MESSAGE_ERROR_T_UNDERLINE); }
            });
        }
    }
}
