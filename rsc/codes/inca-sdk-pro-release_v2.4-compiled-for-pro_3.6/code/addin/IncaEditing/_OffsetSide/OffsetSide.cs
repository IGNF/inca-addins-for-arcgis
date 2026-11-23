using System;
using System.Windows;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System.Collections.Generic;
using System.Resources;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Mapping.Events;

namespace IncaEditing._OffsetSide
{
    /// <summary>
    /// Outil permettant de changer le côté du décalage d'une ou plusieurs entités linéaires.
    /// Deux modes de fonctionnement sont possibles:
    ///    1. Changement du côté de décalage d'une sélection d'entités par rapport à leurs propres géométries (précision
    ///       géométrique approximative, car on ne repart pas de la géométrie de référence d'origine)
    ///    2. Changement du côté de décalage d'une seule entité en désignant la géométrie de référence qui a servi
    ///       initialement à décaler l'entité (par exemple la route suivie par l'itinéraire).
    /// </summary>
    internal class OffsetSideTool : MapTool
    {
        #region Parameters

        private bool _isFirstPointSelected = false;
        private bool _isProcessing = false;
        private KeyValuePair<FeatureLayer, long> _sourceSel;
        private KeyValuePair<FeatureLayer, long> _targetSel;
        #endregion

        #region Constructeur
        public OffsetSideTool() : base()
        {
            // Propriétés de l'outil
            ControlID = "OffsetSide_OffsetSide"; 
            IsSketchTool = true;
            SketchType = SketchGeometryType.Point;
            SketchOutputMode = SketchOutputMode.Map;
            UseSnapping = true;

            // Dictionnaire de la langue utilisée dans l'IHM ("CurrentUICulture")
            Translator = CultureResourcesManager.GetInstance().dictLanguage;
        }
        #endregion

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

        /// <summary>
        /// Initialisation de l'outil lors de son activation dans l'IHM
        /// </summary>
        protected override Task OnToolActivateAsync(bool active)
        {
            // Mise à jour de la zone info du panneau
            var viewModel = this.EmbeddableControl as OffsetSideViewModel;
            viewModel.StatusInfo = "Info";
            viewModel.InstructionInfo = string.Format(GetString("ClickShiftedLineMessage")); 

            // Souscription à des événements
            viewModel.MethodChanged += ChangeMethod;
            viewModel.BySelectionCalled += ExecuteMethod;
            MapSelectionChangedEvent.Subscribe(OnSelectionChanged);

            // Activation du mode sélection par défaut
            ActivateSelectionTool();

            return base.OnToolActivateAsync(active);
        }

        /// <summary>
        /// Modification du mode de fonctionnement de l'outil suite à une sollicitation de l'IHM
        /// </summary>
        private void ChangeMethod(object sender, EventArgs e)
        {
            // Récupération du embedded control
            var viewModel = this.EmbeddableControl as OffsetSideViewModel;
            if (viewModel == null)
                return;

            // Changement du type d'outil: saisie (sketch) => sélection
            if (viewModel.BySelectionActive == true)
            {
                // Passage en mode sélection
                ActivateSelectionTool();
            }
            // Changement du type d'outil: sélection => saisie (sketch)
            else if (viewModel.BySelectionActive != true)
            {
                // Passage en mode saisie
                ActivateSketchTool();
            }

            // Désélection des données dans la carte
            QueuedTask.Run(() => MapView.Active.Map.ClearSelection());
        }

        /// <summary>
        /// Activation du mode sélection (traitement de plusieurs entités à la fois)
        /// </summary>
        private void ActivateSelectionTool()
        {
            // Passage du mode saisie => sélection
            ActivateSelectAsync(true);

            // Mise à jour de la zone info du panneau
            var viewModel = this.EmbeddableControl as OffsetSideViewModel;
            viewModel.StatusInfo = "Info";
            viewModel.InstructionInfo = string.Format(GetString("SelectShiftedLinesMessage"));
        }

        /// <summary>
        /// Activation du mode saisie (traitement d'une seule entité à la fois)
        /// </summary>
        private void ActivateSketchTool()
        {
            // Propriétés de l'outil
            Cursor = System.Windows.Input.Cursors.Cross;

            // Passage du mode sélection => saisie
            ActivateSelectAsync(false);
            base.StartSketchAsync();

            // Mise à jour de la zone info du panneau
            var viewModel = this.EmbeddableControl as OffsetSideViewModel;
            viewModel.StatusInfo = "Info";
            viewModel.InstructionInfo = string.Format(GetString("ClickShiftedLineMessage"));
        }

        /// <summary>
        /// Mise à jour de la zone info du panneau lorsque le contenu de la sélection a changé
        /// </summary>
        private void OnSelectionChanged(MapSelectionChangedEventArgs obj)
        {
            if (obj.Map != MapView.Active.Map) return;

            var viewModel = this.EmbeddableControl as OffsetSideViewModel;
            if (viewModel == null) return;

            if (viewModel.BySelectionActive != true || _isProcessing == true) return;

            if (MapView.Active.Map.SelectionCount > 0)
            {
                // Mise à jour de la zone info du panneau
                viewModel.StatusInfo = "Info";
                viewModel.InstructionInfo = string.Format(GetString("ClickButtonSelectionMessage")); 
            }
            else
            {
                // Mise à jour de la zone info du panneau
                viewModel.StatusInfo = "Info";
                viewModel.InstructionInfo = string.Format(GetString("SelectShiftedLinesMessage"));
            }
        }

        /// <summary>
        /// Déclenchement d'un changement de côté de décalage sur la sélection active suite à une sollicitation
        /// depuis l'IHM
        /// </summary>
        private void ExecuteMethod(object sender, EventArgs e)
        {
            // Récupération du embedded control
            var viewModel = this.EmbeddableControl as OffsetSideViewModel;
            if (viewModel == null)
                return;

            if (viewModel.BySelectionActive == true)
                QueuedTask.Run(async () => await OperateBySelection(viewModel));
        }

        /// <summary>
        /// Vérification de la présence d'une seule entité à proximité d'une position géographique donnée
        /// </summary>
        /// <param name="geometry">Géométrie autour de laquelle s'effectue la recherche de l'entité unique</param>
        /// <returns>paire couche/entité permettant d'identifier l'entité unique en sortie</returns>

        private KeyValuePair<FeatureLayer, long> GetUniqueFeature(Geometry geometry)
        {
            // Extraction de la sélection
            MapPoint point = geometry as MapPoint;
            SelectionSet selection = MapView.Active.GetFeatures(point);

            // Vérification de la présence d'entités sélectionnées
            if (selection.IsEmpty)
                return new KeyValuePair<FeatureLayer, long>(null, -1);

            // On extrait la 1ère couche de type linéaire
            FeatureLayer linearLayer = null;
            foreach (Layer layer in selection.ToDictionary().Keys)
            {
                // Vérification du type géométrique de la couche
                if (layer is FeatureLayer featureLayer && featureLayer.ShapeType == esriGeometryType.esriGeometryPolyline)
                {
                    linearLayer = featureLayer;
                    break;
                }
            }

            // Vérification de l'existence de la couche linéaire
            if (linearLayer == null)
                return new KeyValuePair<FeatureLayer, long>(null, -1);

            // Une seule entité doit être sélectionnée
            if (selection[linearLayer].Count != 1)
                return new KeyValuePair<FeatureLayer, long>(null, -1);

            // On retourne la paire couche/entité
            return new KeyValuePair<FeatureLayer, long>(linearLayer, selection[linearLayer][0]);
        }

        /// <summary>
        /// Vérification de la présence des champs de décalage dans une couche d'entités
        /// </summary>
        /// <param name="layer">Couche d'entités à tester</param>
        /// <param name="offsetSideField">Nom du champ indiquant le côté du décalage</param>
        /// <param name="offsetDistField">Nom du champ indiquant la distance de décalage</param>
        private string checkOffsetFields(FeatureLayer layer, string offsetSideField, string offsetDistField)
        {
            // Parcours des champs de la couche
            Field offsetdist_field = null, offsetside_field = null;
            List<Field> field_list = layer.GetFeatureClass().GetDefinition().GetFields().ToList();
            foreach (var field in field_list)
            {
                if (field.Name.ToLowerInvariant() == offsetDistField.ToLowerInvariant()) offsetdist_field = field;
                if (field.Name.ToLowerInvariant() == offsetSideField.ToLowerInvariant()) offsetside_field = field;
            }

            // Envoi des messages d'erreur
            if (offsetdist_field == null)
                return string.Format(GetString("NoFieldMessage"), offsetDistField);

            if (offsetside_field == null)
                return string.Format(GetString("NoFieldMessage"), offsetSideField);

            return null;
        }

        /// <summary>
        /// Fonction permettant de changer le côté du décalage d'une sélection d'entités linéaires par rapport à leurs
        /// propres géométries.
        /// </summary>
        /// <param name="viewModel"></param>
        private Task<bool> OperateBySelection(OffsetSideViewModel viewModel)
        {
            // Sélection active
            if (MapView.Active.Map.SelectionCount != 0)
            {
                _isProcessing = true;
                try
                {
                    // Vérification de l'existence des champs de décalage dans toutes les couches sélectionnées
                    var selection = MapView.Active.Map.GetSelection();

                    foreach (KeyValuePair<MapMember, List<long>> kvp in selection.ToDictionary().Where(kvp => kvp.Key is FeatureLayer))
                    {
                        // Couche d'entités
                        var featureLayer = kvp.Key as FeatureLayer;
                        string errorMessage = checkOffsetFields(featureLayer, "OFFSETSIDE", "OFFSETDIST");

                        if (errorMessage != null)
                            throw new Exception(errorMessage);
                    }

                    // Regroupement des actions en une opération unique
                    MapView.Active.Map.OperationManager.CreateCompositeOperation(async () =>
                    {

                        // Changement du côté du décalage
                        OffsetSide offsetSide = new OffsetSide();
                        var modifiedFeatures = await offsetSide.ChangeOffsetSideFromSelection();

                        // Correction de la topologie aux extrémités du tronçon décalé
                        if (viewModel.RepairTopology == true)
                        {
                            double _searchDistance = 60.0;
                            offsetSide.RepairTopology(modifiedFeatures, _searchDistance);
                        }
                    }, string.Format(GetString("OffsetSideOperation")));

                    // Mise à jour de la zone info du panneau
                    viewModel.StatusInfo = "Complete";
                    viewModel.InstructionInfo = string.Format(GetString("ChangeCompletedMessage"));
                } 
                catch (Exception e)
                {
                    // Mise à jour de la zone info du panneau
                    viewModel.StatusInfo = "Error";
                    viewModel.InstructionInfo = e.Message;
                }
                _isProcessing = false;
            }
            return Task.FromResult(true);
        }

        /// <summary>
        /// Fonction permettant de changer le côté du décalage d'une entité linéaire par rapport à une géométrie linéaire tierce.
        /// Cette fonction est appelée à chaque sollicitation de la souris par l'utilisateur. La sélection des données à traiter 
        /// se fait donc en deux temps: l'utilisateur sélectionne dans un premier temps l'entité linéaire à décaler, puis
        /// dans un second, l'entité portant la géométrie choisie comme référence.
        /// </summary>
        private Task<bool> OperateRelativeToGeometry(OffsetSideViewModel viewModel, Geometry geometry)
        {
            // 1er clic: récupération des entités source à proximité
            if (!_isFirstPointSelected)
            {
                _sourceSel = GetUniqueFeature(geometry);

                if (_sourceSel.Key != null)
                {
                    MapView.Active.FlashFeature(_sourceSel.Key, _sourceSel.Value);

                    // Vérification de l'existence des champs de décalage
                    string errorMessage = checkOffsetFields(_sourceSel.Key, "OFFSETSIDE", "OFFSETDIST");

                    if (errorMessage != null)
                    {
                        // Mise à jour de la zone info du panneau
                        viewModel.StatusInfo = "Error";
                        viewModel.InstructionInfo = errorMessage;
                    }
                    else
                    {
                        // Mise à jour de la zone info du panneau
                        viewModel.StatusInfo = "Info";
                        viewModel.InstructionInfo = string.Format(GetString("ClickReferenceLineMessage"));
                        _isFirstPointSelected = true;
                    }
                }
                else
                {
                    // Mise à jour de la zone info du panneau
                    viewModel.StatusInfo = "Info";
                    viewModel.InstructionInfo = string.Format(GetString("ClickShiftedLineMessage"));
                }

                return Task.FromResult(false);
            }
            // 2ème clic: récupération des entités cible à proximité
            else
            {
                _targetSel = GetUniqueFeature(geometry);

                if (_targetSel.Key != null)
                {
                    MapView.Active.FlashFeature(_targetSel.Key, _targetSel.Value);
                    _isFirstPointSelected = false;
                }
                else return Task.FromResult(false);
            }

            // Activation du curseur patience
            Cursor = System.Windows.Input.Cursors.Wait;
            _isProcessing = true;

            try
            {
                // Extraction des ObjectID
                FeatureLayer sourceLayer = _sourceSel.Key;
                FeatureLayer targetLayer = _targetSel.Key;
                long sourceOID = _sourceSel.Value;
                long targetOID = _targetSel.Value;

                // Regroupement des actions en une opération unique
                MapView.Active.Map.OperationManager.CreateCompositeOperation(async () =>
                {

                    // Changement du côté de décalage de la 1ère entité par rapport à la géométrie de la 2nde entité
                    OffsetSide offsetSide = new OffsetSide();
                    var modifiedFeatures = await offsetSide.ChangeOffsetSideRelativeToGeometry(sourceLayer, sourceOID, targetLayer, targetOID, viewModel.Realign);

                    // Correction de la topologie aux extrémités du tronçon décalé
                    if (viewModel.RepairTopology == true)
                    {
                        double _searchDistance = 60.0;
                        offsetSide.RepairTopology(modifiedFeatures, _searchDistance);
                    }
                }, string.Format(GetString("OffsetSideOperation")));

                Cursor = System.Windows.Input.Cursors.Cross;
                // Mise à jour de la zone info du panneau
                viewModel.StatusInfo = "Complete";
                viewModel.InstructionInfo = string.Format(GetString("ChangeCompletedMessage"));
            }
            catch (Exception e)
            {
                // Mise à jour de la zone info du panneau
                viewModel.StatusInfo = "Error";
                viewModel.InstructionInfo = e.Message;
            }

            _isProcessing = false;
            return Task.FromResult(true);
        }


        /// <summary>
        /// Après la réalisation d'une saisie (sketch) en deux points, identification des entités source et cible.
        /// Puis changement de côté de décalage de l'entité source par rapport à la géométrie de l'entité cible. 
        /// </summary>
        /// <param name="geometry">géométrie saisie par l'utilisateur</param>
        /// <returns></returns>
        protected override Task<bool> OnSketchCompleteAsync(Geometry geometry)
        {
            // Vérifications de principe
            if (MapView.Active == null || geometry == null || _isProcessing) return Task.FromResult(false);

            // Récupération du embedded control
            var viewModel = this.EmbeddableControl as OffsetSideViewModel;
            if (viewModel == null)
                return Task.FromResult(false);

            if (viewModel.BySelectionActive == false)
                return QueuedTask.Run(async () => await OperateRelativeToGeometry(viewModel, geometry));
            else
                return Task.FromResult(true);
        }
    }

    /// <summary>
    /// Classe gérant les changements de côté du décalage des itinéraires ou limites
    /// </summary>
    public class OffsetSide
    {
        #region Parameters

        // Valeurs possibles du champ OFFSETSIDE
        const string _rightSide = "R";
        const string _leftSide = "L";

        // Paramètres de décalage des linéaires
        const OffsetType _offsetType = OffsetType.Round;
        const double _bevelRatio = 2.0;

        #endregion

        #region Constructors

        public OffsetSide()
        {
            // Dictionnaire de la langue utilisée dans l'IHM ("CurrentUICulture")
            Translator = CultureResourcesManager.GetInstance().dictLanguage;
        }

        #endregion

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

        /// <summary>
        /// Changement du côté du décalage d'un itinéraire ou d'une limite décalés, en utilisant une géométrie linéaire tierce comme support.
        /// Le traitement s'applique à une entité de type linéaire qui doit comporter les champs OFFSETDIST et OFFSETSIDE
        /// </summary>
        /// <param name="sourceLayer">couche d'entités contenant l'entité dont il faut changer le côté de décalage</param>
        /// <param name="featureOID">objectid de l'entité dont il faut changer le côté de décalage</param>
        /// <param name="refGeometryLayer">couche d'entités contenant la géométrie utilisée comme référence</param>
        /// <param name="refGeometryOID">objectid de l'entité utilisée comme référence géométrique</param>
        /// <param name="keepOffsetSide">indique si le côté du décalage doit être conservé. L'outil sert alors à réaligner une limite décalée sur une nouvelle géométrie</param>
        internal async Task<Dictionary<MapMember, List<long>>> ChangeOffsetSideRelativeToGeometry(FeatureLayer sourceLayer, long featureOID, FeatureLayer refGeometryLayer, long refGeometryOID, bool keepOffsetSide=false)
        {

            Dictionary<MapMember, List<long>> modifiedFeatures = new Dictionary<MapMember, List<long>>();

            await QueuedTask.Run(() =>
            {
                try
                {
                    // Extraction de l'entité de référence à partir de son objectID
                    Feature refFeature = null;
                    QueryFilter queryFilter = new QueryFilter()
                    {
                        ObjectIDs = new List<long>() { refGeometryOID }
                    };

                    using (RowCursor cursor = refGeometryLayer.GetFeatureClass().Search(queryFilter))
                    {
                        if (cursor.MoveNext())
                            refFeature = (Feature)cursor.Current;
                    }

                    // Extraction de la géométrie de référence
                    var refGeometry = refFeature.GetShape() as Polyline;

                    // Recherche des champs OFFSETDIST et OFFSETSIDE dans la couche source
                    Field offsetdist_field = null, offsetside_field = null;
                    List<Field> field_list = sourceLayer.GetFeatureClass().GetDefinition().GetFields().ToList();
                    foreach (var field in field_list)
                    {
                        if (field.Name.ToLowerInvariant() == "offsetdist") offsetdist_field = field;
                        if (field.Name.ToLowerInvariant() == "offsetside") offsetside_field = field;
                    }

                    if (offsetdist_field == null)
                    {
                        throw new Exception(string.Format(GetString("NoFieldMessage"), "OFFSETDIST"));
                    }
                    if (offsetside_field == null)
                    {
                        throw new Exception(string.Format(GetString("NoFieldMessage"), "OFFSETSIDE"));
                    }

                    // Index des champs de décalage
                    int offsetdist_index = sourceLayer.GetTable().GetDefinition().FindField(offsetdist_field.Name);
                    int offsetside_index = sourceLayer.GetTable().GetDefinition().FindField(offsetside_field.Name);

                    // Session d'édition pour la couche source
                    EditOperation offsetSideOperation = new EditOperation { Name = "Offset side", SelectNewFeatures = false };

                    offsetSideOperation.Callback(context =>
                    {
                        // Extraction de l'entité décalée à partir de son objectID
                        Feature feature = null;
                        QueryFilter queryFilter = new QueryFilter()
                        {
                            ObjectIDs = new List<long>() { featureOID }
                        };

                        using (RowCursor cursor = sourceLayer.GetFeatureClass().Search(queryFilter, false))
                        {
                            if (cursor.MoveNext())
                                feature = (Feature)cursor.Current;
                        }

                        // Valeurs des champs de décalage
                        var offsetdist_value = feature.GetOriginalValue(offsetdist_index);
                        var offsetside_value = feature.GetOriginalValue(offsetside_index);

                        // Vérification des valeurs des champs
                        if (offsetdist_value == null || offsetside_value == null) return;

                        // Valeur du décalage
                        double distance = Convert.ToDouble(offsetdist_value);
                        if ((offsetside_value.ToString() == _rightSide && keepOffsetSide is false) ||
                            (offsetside_value.ToString() == _leftSide && keepOffsetSide is true))
                        {
                            distance = -distance;
                        }
                        // Décalage géométrique
                        var offsetGeometry = GeometryEngine.Instance.Offset(refGeometry, distance, _offsetType, _bevelRatio) as Polyline;

                        // Extraction de la géométrie à décaler pour en déterminer la dimension (2D ou 3D)
                        var geometry = feature.GetShape();
                        // Gestion du Z de la géométrie décalée (si cible 3D)
                        if (geometry.HasZ)
                        {
                            // Affectation d'un Z constant pour éviter les valeurs nulles qui font crasher ArcGIS Pro
                            if (offsetGeometry.HasZ)
                            {
                                var offsetGeometryNew = GeometryEngine.Instance.SetConstantZ(offsetGeometry, 0.0);
                                offsetGeometry = offsetGeometryNew as Polyline;
                            }
                            // Ajout d'un Z à la géométrie (Z = 0.0)
                            else
                            {
                                PolylineBuilderEx polylineBuilder = new PolylineBuilderEx(offsetGeometry);
                                polylineBuilder.HasZ = true;
                                offsetGeometry = polylineBuilder.ToGeometry();
                            }
                        }
                        // Debug
                        // var coords_string = string.Join(", ", offsetGeometry.Points.Select(point => $"({point.X}, {point.Y}, {(point.HasZ ? point.Z.ToString() : "N/A")})"));
                        // ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(coords_string, "Warning");

                        // Mise à jour du champ offsetside (inversion des valeurs)
                        if (keepOffsetSide is false)
                        {
                            if (offsetside_value.ToString() == _rightSide)
                            {
                                feature[offsetside_field.Name] = _leftSide;
                            }
                            else if (offsetside_value.ToString() == _leftSide)
                            {
                                feature[offsetside_field.Name] = _rightSide;
                            }
                        }

                        // Enregistrement des modifications et rafraîchissement du cache
                        feature.SetShape(offsetGeometry);
                        feature.Store();
                        context.Invalidate(feature);
                    }, sourceLayer.GetTable());

                    // Sauvegarde des opérations
                    if (!offsetSideOperation.IsEmpty)
                    {
                        bool result = offsetSideOperation.Execute();

                        if (!result)
                        {
                            ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(offsetSideOperation.ErrorMessage, "Warning", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        
                        // Enregistrement des données modifiées à retourner
                        modifiedFeatures[sourceLayer] = new List<long>() { featureOID };
                    }
                }
                catch (Exception ex)
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(ex.Message, "Warning", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

            return modifiedFeatures;
        }

        /// <summary>
        /// Changement du côté du décalage d'un ou plusieurs itinéraires ou limites décalés.
        /// Le traitement s'applique aux entités couramment sélectionnées, qui doivent être de type linéaire et comporter les champs OFFSETDIST et OFFSETSIDE.
        /// </summary>
        internal async Task<Dictionary<MapMember, List<long>>> ChangeOffsetSideFromSelection()
        {
            Dictionary<MapMember, List<long>> modifiedFeatures = new Dictionary<MapMember, List<long>>();

            // Une carte doit être active
            var mapView = MapView.Active;
            if (mapView == null) return modifiedFeatures;

            await QueuedTask.Run(() =>
            {
                //Entités sélectionnées dans la carte
                var selection = mapView.Map.GetSelection();
                if (selection == null) return;

                // Parcours des couches sélectionnées
                foreach (KeyValuePair<MapMember, List<long>> kvp in selection.ToDictionary().Where(kvp => kvp.Key is FeatureLayer))
                {
                    // Couche d'entités
                    var featureLayer = kvp.Key as FeatureLayer;

                    //Vérification du type géométrique qui doit être linéaire
                    if (featureLayer.ShapeType != esriGeometryType.esriGeometryPolyline) return;

                    // Recherche des champs OFFSETDIST et OFFSETSIDE
                    Field offsetdist_field = null, offsetside_field = null;
                    List<Field> field_list = featureLayer.GetFeatureClass().GetDefinition().GetFields().ToList();
                    foreach (var field in field_list)
                    {
                        if (field.Name.ToLowerInvariant() == "offsetdist") offsetdist_field = field;
                        if (field.Name.ToLowerInvariant() == "offsetside") offsetside_field = field;
                    }

                    if (offsetdist_field == null)
                    {
                        throw new Exception(string.Format(GetString("NoFieldMessage"), "OFFSETDIST"));
                    }
                    if (offsetside_field == null)
                    {
                        throw new Exception(string.Format(GetString("NoFieldMessage"), "OFFSETSIDE"));
                    }

                    // Index des champs de décalage
                    int offsetdist_index = featureLayer.GetTable().GetDefinition().FindField(offsetdist_field.Name);
                    int offsetside_index = featureLayer.GetTable().GetDefinition().FindField(offsetside_field.Name);

                    // Session d'édition pour la couche
                    EditOperation offsetSideOperation = new EditOperation { Name = "Offset side", SelectNewFeatures = false };

                    offsetSideOperation.Callback(context =>
                    {
                        // Parcours des entités sélectionnées
                        using (RowCursor rowCursor = featureLayer.GetSelection().Search(null, false))
                        {
                            while (rowCursor.MoveNext())
                            {
                                using (Feature feature = (Feature)rowCursor.Current)
                                {
                                    // Extraction de la géométrie
                                    Polyline geometry = feature.GetShape() as Polyline;

                                    // Valeurs des champs de décalage
                                    var offsetdist_value = feature.GetOriginalValue(offsetdist_index);
                                    var offsetside_value = feature.GetOriginalValue(offsetside_index);

                                    // Vérification des valeurs des champs
                                    if (offsetdist_value == null || offsetside_value == null) continue;

                                    // Valeur du décalage
                                    double distance = Convert.ToDouble(offsetdist_value);
                                    distance *= 2;
                                    if (offsetside_value.ToString() == _rightSide)
                                    {
                                        distance = -distance;
                                    }
                                    // Décalage géométrique
                                    var offsetGeometry = GeometryEngine.Instance.Offset(geometry, distance, _offsetType, _bevelRatio) as Polyline;

                                    // Gestion du Z de la géométrie décalée (si cible en 3D)
                                    if (geometry.HasZ)
                                    {
                                        // Affectation d'un Z constant pour éviter les valeurs nulles qui font crasher ArcGIS Pro
                                        if (offsetGeometry.HasZ)
                                        {
                                            var offsetGeometryNew = GeometryEngine.Instance.SetConstantZ(offsetGeometry, 0.0);
                                            offsetGeometry = offsetGeometryNew as Polyline;
                                        }
                                        // Ajout d'un Z à la géométrie (Z = 0.0)
                                        else
                                        {
                                            PolylineBuilderEx polylineBuilder = new PolylineBuilderEx(offsetGeometry);
                                            polylineBuilder.HasZ = true;
                                            offsetGeometry = polylineBuilder.ToGeometry();
                                        }
                                    }
                                    // Debug
                                    // var coords_string = string.Join(", ", offsetGeometry.Points.Select(point => $"({point.X}, {point.Y}, {(point.HasZ ? point.Z.ToString() : "N/A")})"));
                                    // ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(coords_string, "Warning");

                                    // Mise à jour du champ offsetside (inversion des valeurs)
                                    if (offsetside_value.ToString() == _rightSide)
                                    {
                                        feature[offsetside_field.Name] = _leftSide;
                                    }
                                    else if (offsetside_value.ToString() == _leftSide)
                                    {
                                        feature[offsetside_field.Name] = _rightSide;
                                    }

                                    // Enregistrement des modifications et rafraîchissement du cache
                                    feature.SetShape(offsetGeometry);
                                    feature.Store();
                                    context.Invalidate(feature);
                                }
                            }
                        }
                    }, featureLayer.GetTable());

                    // Sauvegarde des opérations
                    if (!offsetSideOperation.IsEmpty)
                    {
                        bool result = offsetSideOperation.Execute();

                        if (!result)
                            throw new Exception(offsetSideOperation.ErrorMessage);

                        // Enregistrement des données modifiées à retourner
                        modifiedFeatures[featureLayer] = kvp.Value;
                    }
                }
            });

            return modifiedFeatures;
        }

        /// <summary>
        /// Fonction permettant de créer une géométrie de type polyligne à partir d'une liste d'identifiants (objectid) d'entités présentes
        /// dans une couche d'entités.
        /// </summary>
        /// <param name="featureLayer">Couche d'entités contenant les entités sources utilisées pour construire la géométrie</param>
        /// <param name="objectIDsList">Liste des identifiants</param>
        internal Polyline GetGeometryFromObjectIDs(FeatureLayer featureLayer, List<long> objectIDsList)
        {
            // Récupération des géométries des entités de référence
            List<Polyline> referenceGeometries = new List<Polyline>();
            foreach (long objectId in objectIDsList)
            {
                // Récupérer la géométrie de l'entité par son ObjectID
                QueryFilter queryFilter = new QueryFilter { WhereClause = $"OBJECTID = {objectId}" };
                using (RowCursor cursor = featureLayer.Search(queryFilter))
                {
                    while (cursor.MoveNext())
                    {
                        Feature feature = (Feature)cursor.Current;
                        // Récupérer la géométrie de l'entité
                        Polyline geometry = (Polyline)feature.GetShape();
                        referenceGeometries.Add(geometry);
                    }
                }
            }
            // Concaténation des géométries en une géométrie multipart unique
            Polyline polylineGeometry = PolylineBuilderEx.CreatePolyline(referenceGeometries, AttributeFlags.None);
            return polylineGeometry;
        }

        /// <summary>
        /// Fonction permettant d'obtenir la longueur de l'entité linéaire la plus courte incluse dans la sélection courante d'une couche particulière, .
        /// </summary>
        /// <param name="featureLayer">Couche contenant les entités linéaires à évaluer dans sa sélection</param>
        public static double GetShortestSelectedLineLength(FeatureLayer featureLayer)
        {
            if (featureLayer == null) throw new ArgumentNullException(nameof(featureLayer));

            // Récupérer les entités sélectionnées
            using (Selection selection = featureLayer.GetSelection())
            {
                if (selection.GetCount() == 0) throw new InvalidOperationException("Aucune entité sélectionnée dans la couche.");

                double shortestLength = double.MaxValue;
                QueryFilter queryFilter = new QueryFilter { WhereClause = $"OBJECTID IN ({string.Join(",", selection.GetObjectIDs())})" };
                // Parcourir les ObjectIDs sélectionnés
                using (RowCursor cursor = featureLayer.Search(queryFilter))
                {
                    while (cursor.MoveNext())
                    {
                        using (Feature feature = cursor.Current as Feature)
                        {
                            if (feature == null) continue;

                            // Vérifie si la géométrie est une polyline
                            if (feature.GetShape() is Polyline polyline)
                            {
                                // Comparer les longueurs
                                if (polyline.Length < shortestLength)
                                {
                                    shortestLength = polyline.Length;
                                }
                            }
                        }
                    }
                }
                return shortestLength;
            }
        }

        /// <summary>
        /// Fonction permettant de corriger la topologie des entités dont le côté de décalage a été changé au niveau de leurs extrémités.
        /// Des géotraitements de prolongement et de découpage des géométries sont appelés pour assurer une continuité géométrique
        /// avec les autres entités de la classe.
        /// </summary>
        /// <param name="modifiedFeatures">Dictionnaire des entités à traiter identifiées par la paire couche/identifiant</param>
        /// <param name="searchDistance">Distance définissant la zone d'action du traitement</param>
        internal async void RepairTopology(Dictionary<MapMember, List<long>> modifiedFeatures, double searchDistance)
        {
            await QueuedTask.Run(() =>
            {
                // Parcours des couches du dictionnaire
                foreach (KeyValuePair<MapMember, List<long>> kvp in modifiedFeatures.Where(kvp => kvp.Key is FeatureLayer))
                {
                    // Couche d'entités
                    var featureLayer = kvp.Key as FeatureLayer;
                    // Liste d'identifiants
                    var modifiedObjectIDs = kvp.Value;

                    // Création d'une zone tampon autour de la géométrie de ligne
                    Polyline polylineGeometry1 = GetGeometryFromObjectIDs(featureLayer, modifiedObjectIDs);
                    Geometry bufferGeometry = GeometryEngine.Instance.Buffer(polylineGeometry1, searchDistance);

                    // Sélection des entités intersectant les entités déplacées (précaution nécessaire pour éviter de supprimer lors de la troncature
                    // des tronçons courts dont la longueur serait inférieure à la distance de recherche).
                    var spatialQuery1 = new SpatialQueryFilter() { FilterGeometry = polylineGeometry1, SpatialRelationship = SpatialRelationship.Intersects };
                    var selectedFeatures1 = featureLayer.Select(spatialQuery1);

                    // Adaptation de la distance de recherche
                    double shortestLength = GetShortestSelectedLineLength(featureLayer);
                    double minSearchDistance = Math.Round(Math.Min(searchDistance, shortestLength), 1, MidpointRounding.ToNegativeInfinity);

                    // Troncature des lignes
                    var parameters1 = Geoprocessing.MakeValueArray(featureLayer, minSearchDistance, "KEEP_SHORT");
                    var gpResult = Geoprocessing.ExecuteToolAsync("TrimLine_edit", parameters1);

                    // Sélection des entités incluses dans le buffer (les lignes à prolonger ne se coupent pas forcément!)
                    var spatialQuery2 = new SpatialQueryFilter() { FilterGeometry = bufferGeometry, SpatialRelationship = SpatialRelationship.Intersects };
                    var selectedFeatures2 = featureLayer.Select(spatialQuery2);

                    // Prolongation des lignes jusqu'à la première entité d'intersection
                    // Attention! il y a bien un espace en fin du paramètre "EXTENSION " (ArcGIS Pro 3.1)
                    var parameters2 = Geoprocessing.MakeValueArray(featureLayer, searchDistance, "EXTENSION ");
                    gpResult = Geoprocessing.ExecuteToolAsync("ExtendLine_edit", parameters2);
                    // 2ème essai car ExtendLine a parfois des difficultés lorsqu'il faut prolonger sur des prolongements...
                    gpResult = Geoprocessing.ExecuteToolAsync("ExtendLine_edit", parameters2);

                    // Sélection des entités intersectant la géométrie tronquée puis prolongée des entités déplacées
                    Polyline polylineGeometry3 = GetGeometryFromObjectIDs(featureLayer, modifiedObjectIDs);
                    var spatialQuery3 = new SpatialQueryFilter() { FilterGeometry = polylineGeometry3, SpatialRelationship = SpatialRelationship.Intersects };
                    var selectedFeatures3 = featureLayer.Select(spatialQuery3);

                    // Adaptation de la distance de recherche
                    shortestLength = GetShortestSelectedLineLength(featureLayer);
                    minSearchDistance = Math.Round(Math.Min(searchDistance, shortestLength), 1, MidpointRounding.ToNegativeInfinity);

                    // Troncature des lignes
                    var parameters3 = Geoprocessing.MakeValueArray(featureLayer, minSearchDistance, "KEEP_SHORT");
                    gpResult = Geoprocessing.ExecuteToolAsync("TrimLine_edit", parameters3);

                    MapView.Active.Map.ClearSelection();
                }
            });
        }
    }
}
