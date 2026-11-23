using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Field = ArcGIS.Core.Data.Field;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

namespace IncaEditing._FeatureEdit
{
    /// <summary>
    /// Outil permettant de changer le côté du décalage d'une ou plusieurs entités linéaires.
    /// Deux modes de fonctionnement sont possibles:
    ///    1. Changement du côté de décalage d'une sélection d'entités par rapport à leurs propres géométries (précision
    ///       géométrique approximative, car on ne repart pas de la géométrie de référence d'origine)
    ///    2. Changement du côté de décalage d'une seule entité en désignant la géométrie de référence qui a servi
    ///       initialement à décaler l'entité (par exemple la route suivie par l'itinéraire).
    /// </summary>
    internal class SpotHeightTool : MapTool
    {
        public SpotHeightTool()
        {
            IsSketchTool = false;
            UseSnapping = false;
            SketchType = SketchGeometryType.Rectangle;

            ControlID = "SpotHeight_options";
            // Dictionnaire de la langue utilisée dans l'IHM ("CurrentUICulture")
            Translator = CultureResourcesManager.GetInstance().dictLanguage;
            Cursor = Cursors.Cross;  // Définir le curseur lors du clic
        }

        /// <summary>
        /// Traducteur de langue
        /// </summary>
        private ResourceSet Translator { get; set; }

        /// <summary>
        /// Retourne une chaîne de caractère incluse dans le dictionnaire multi-langues
        /// </summary>
        /// <returns>La chaîne de caractères dans la langue courante d'ArGIS Pro</returns>
        public string GetString(string name)
        {
            return Translator.GetString(name);
        }

        public static string AddComment
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Ajout 25K";
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "25K Añade";
                else return "25K Adds";
            }
        }
        /// <summary>
        /// Initialisation de l'outil lors de son activation dans l'IHM
        /// </summary>
        protected override async Task OnToolActivateAsync(bool hasMapViewChanged)
        {
            await ActivateSelectAsync(true);
        }

        public static string MESSAGE_ERROR_SYMBOLE
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Attention la classe n'existe pas ";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Tenga en cuenta que la clase no existe ";
                else return "Please note that the class does not exist";
            }
        }

        protected override void OnToolMouseDown(MapViewMouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                e.Handled = true; //Handle the event args to get the call to the corresponding async method
        }

        public Task CreateSpotHeight(MapViewMouseButtonEventArgs e, SpotHeightViewModel sp)
        {
            return QueuedTask.Run(() =>
            {
                // identifier les couches qui ne sont pas correctement liées à une classe d'entités ou une source de données,
                // et qui nécessitent une intervention pour "réparer" ces relations dans ArcGIS Pro.
              List<FeatureLayer> blockedLayers_list = new List<FeatureLayer>();
                foreach (var l in MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>())
                {
                    if (l.GetFeatureClass() is null)
                        blockedLayers_list.Add(l);
                }

                //Convert the clicked point in client coordinates to the corresponding map coordinates.
                var mapPoint = MapView.Active.ClientToMap(e.ClientPoint);
                var point = MapPointBuilderEx.CreateMapPoint(mapPoint.X, mapPoint.Y, mapPoint.SpatialReference);

                // Exemple de création d'un point dans une couche de type FeatureLayer
                Map map = MapView.Active.Map;
                var createOperatioTopo = new EditOperation { Name = "création de la cote" };
                var atts = new Dictionary<string, object>();
                var createOperationPtC = new EditOperation { Name = "création du point côté" };
                var attsptc = new Dictionary<string, object>();

                List<FeatureLayer> LayersOfMap = map.GetLayersAsFlattenedList().OfType<FeatureLayer>().ToList();
                List<FeatureLayer> LayersOfMapPossible = LayersOfMap.Except(blockedLayers_list).ToList();
                FeatureLayer PC_Layer = LayersOfMap.Except(blockedLayers_list).ToList().FirstOrDefault(f => f.GetFeatureClass().GetName().ToLower() == "o_point_cote_n1"); ;
                if (PC_Layer is null) { MessageBox.Show(MESSAGE_ERROR_SYMBOLE); return Task.FromResult(true);}



                List<Field> fields_list = PC_Layer.GetFeatureClass().GetDefinition().GetFields().ToList();
                Field symbo = null, geom_field = null, toponyme = null, territoire = null, comment = null;
                foreach (var fl in fields_list)
                {
                    switch (fl.Name.ToLowerInvariant())
                    {
                        case "symbo":
                            symbo = fl;
                            break;
                        case "toponyme":
                            toponyme = fl;
                            break;
                        case "territoire":
                            territoire = fl;
                            break;
                        case "comment":
                            comment = fl;
                            break;
                        default:
                            break;
                    }
                    switch (fl.FieldType.ToString())
                    {
                        case "Geometry":
                            geom_field = fl;
                            break;
                        default:
                            break;
                    }
                }
                if (symbo != null && geom_field != null)
                {
                    // on  vérifie que la couche soit éditable
                    bool iseditable = PC_Layer.IsEditable;
                    PC_Layer.SetEditable(true);
                    PC_Layer.SetVisibility(true);
                    attsptc.Add(symbo.Name, sp.SelectSymbo);
                    attsptc.Add(toponyme.Name, sp.SHeight);
                    attsptc.Add(geom_field.Name, point);
                    if (point.X < 1082800 && point.X > 124000 && point.Y < 7110000 && point.Y > 6137000)
                        attsptc.Add(territoire.Name, "FXX");
                    attsptc.Add(comment.Name, AddComment);
                    createOperationPtC.Create(PC_Layer, attsptc);
                    createOperationPtC.ExecuteAsync();
                    // on  remet la couche dans son état édition
                    PC_Layer.SetEditable(iseditable);
                }

                AnnotationLayer TC_Layer = map.GetLayersAsFlattenedList().OfType<AnnotationLayer>().First(f => f.GetFeatureClass().GetName().ToLower().Contains("e_toponyme_anno"));
                fields_list = TC_Layer.GetFeatureClass().GetDefinition().GetFields().ToList();
                Field annotationclassid = null, symbolid = null, textstring = null, wordspacing = null, geomfield = null;
                Field fontname = null, fontstyle = null, fontsize = null, characterwidth = null, flipangle = null, typo = null;
                foreach (var fl in fields_list)
                {
                    switch (fl.Name.ToLowerInvariant())
                    {
                        case "annotationclassid":
                            annotationclassid = fl;
                            break;
                        case "symbolid":
                            symbolid = fl;
                            break;
                        case "textstring":
                            textstring = fl;
                            break;
                        case "wordspacing":
                            wordspacing = fl;
                            break;
                        case "fontname":
                            fontname = fl;
                            break;
                        case "fontstyle":
                            fontstyle = fl;
                            break;
                        case "fontsize":
                            fontsize = fl;
                            break;
                        case "characterwidth":
                            characterwidth = fl;
                            break;
                        case "flipangle":
                            flipangle = fl;
                            break;
                        case "typo":
                            typo = fl;
                            break;
                        default:
                            break;
                    }
                    switch (fl.FieldType.ToString())
                    {
                        case "Geometry":
                            geomfield = fl;
                            break;
                        default:
                            break;
                    }
                }
                if (symbolid != null && geomfield != null)
                {
                    // on  vérifie que la couche soit éditable
                    bool iseditable = TC_Layer.IsEditable;
                    TC_Layer.SetEditable(true);
                    TC_Layer.SetVisibility(true);
                    //var atts = new Dictionary<string, object>();
                    atts.Add(annotationclassid.Name, 41);//41 = TYPO_E_1
                    atts.Add(symbolid.Name, 45);
                    atts.Add(textstring.Name, sp.SHeight);
                    atts.Add(wordspacing.Name, 100);
                    atts.Add(fontname.Name, "Formata");
                    atts.Add(fontstyle.Name, "Italic");
                    atts.Add(fontsize.Name, 5);
                    atts.Add(characterwidth.Name, 100);
                    atts.Add(flipangle.Name, 90);
                    atts.Add(typo.Name, "TYPO_E_2");
                    atts.Add(geom_field.Name, point);

                    createOperatioTopo.Create(TC_Layer, atts);
                    createOperatioTopo.ExecuteAsync();
                    // on  remet la couche dans son état édition
                    TC_Layer.SetEditable(iseditable);
                }


                string commandId = "esri_editing_EditVerticesMove";
                var iCommand = FrameworkApplication.GetPlugInWrapper(commandId) as ICommand;

                if (iCommand != null) iCommand.Execute(null);

                return Task.FromResult(true);
            });

        }


        protected override Task HandleMouseDownAsync(MapViewMouseButtonEventArgs e)
        {
            // Vérifications de principe
            if (MapView.Active == null)
                return Task.FromResult(false);

            var sp = EmbeddableControl as SpotHeightViewModel;
            if (sp == null)
                // Retourner false permet de garder l'outil actif après la saisie
                return Task.FromResult(false);
            else
            {
                CreateSpotHeight(e, sp);
                return Task.FromResult(true);
            }
        }
    }
}
