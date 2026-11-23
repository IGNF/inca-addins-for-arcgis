using ArcGIS.Core.CIM;
using ArcGIS.Core.Data.UtilityNetwork.Trace;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;


namespace IncaDisplay._DisplayContents 
{
    #region Affichage
    /// <summary>
    /// Bouton permettant d'ouvrir ou fermer toutes les couches d'entités et ggroupe de couches en affichage
    /// </summary>
    internal class TurnOnAll_ViewButton : Button
    {
        protected override void OnClick()
        {
            List<GroupLayer> listGroupLayers = MapView.Active.Map.GetLayersAsFlattenedList().OfType<GroupLayer>().ToList();
            List<FeatureLayer> listLayers = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().ToList();
            List<AnnotationLayer> listAnnotations = MapView.Active.Map.GetLayersAsFlattenedList().OfType<AnnotationLayer>().ToList();
            List<MosaicLayer> listGroupMosaics = MapView.Active.Map.GetLayersAsFlattenedList().OfType<MosaicLayer>().ToList();
            List<ImageMosaicSubLayer> listMozaic = MapView.Active.Map.GetLayersAsFlattenedList().OfType<ImageMosaicSubLayer>().ToList();

            var allLayers = listLayers.Cast<Layer>().Concat(listAnnotations).Concat(listGroupLayers).Concat(listGroupMosaics).Concat(listMozaic);

            // Boucle unique pour masquer toutes les couches
            foreach (var ly in allLayers)
            {
                QueuedTask.Run(() => ly.SetVisibility(true));
            }
        }
        protected override void OnUpdate()
        {
            Enabled = true;  
        }
    }

    internal class TurnOffAll_ViewButton : Button
    {
        protected override void OnClick()
        {
            List<GroupLayer> listGroupLayers = MapView.Active.Map.GetLayersAsFlattenedList().OfType<GroupLayer>().ToList();
            List<FeatureLayer> listLayers = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().ToList();
            List<AnnotationLayer> listAnnotations = MapView.Active.Map.GetLayersAsFlattenedList().OfType<AnnotationLayer>().ToList();
            List<MosaicLayer> listGroupMosaics = MapView.Active.Map .GetLayersAsFlattenedList() .OfType<MosaicLayer>() .ToList();
            List<ImageMosaicSubLayer> listMozaic = MapView.Active.Map.GetLayersAsFlattenedList().OfType<ImageMosaicSubLayer>().ToList();

            var allLayers = listLayers.Cast<Layer>().Concat(listAnnotations).Concat(listGroupLayers).Concat(listGroupMosaics).Concat(listMozaic);

            // Boucle unique pour masquer toutes les couches
            foreach (var ly in allLayers)
            {
                QueuedTask.Run(() => ly.SetVisibility(false));
            }
        }
        protected override void OnUpdate()
        {
            Enabled = true;
        }
    }
    #endregion Affichage
    
    # region Sélection
    ///
    internal class TurnOnAll_SelectButton : Button
    {
        protected override void OnClick()
        {
            List<FeatureLayer> listLayers = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().ToList();
            List<AnnotationLayer> listAnnotations = MapView.Active.Map.GetLayersAsFlattenedList().OfType<AnnotationLayer>().ToList(); ;
              
            foreach (var ly in listLayers) QueuedTask.Run(() => ly.SetSelectable(true)); 
            foreach (var ly in listAnnotations) QueuedTask.Run(() => ly.SetSelectable(true));
        }
        protected override void OnUpdate()
        {
            Enabled = true;
        }
    }

    internal class TurnOffAll_SelectButton : Button
    {
        protected override void OnClick()
        {
            List<FeatureLayer> listLayers = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().ToList();
            List<AnnotationLayer> listAnnotations = MapView.Active.Map.GetLayersAsFlattenedList().OfType<AnnotationLayer>().ToList();

            foreach (var ly in listLayers) QueuedTask.Run(() => ly.SetSelectable(false));
            foreach (var ly in listAnnotations) QueuedTask.Run(() => ly.SetSelectable(false));
        }
        protected override void OnUpdate()
        {
            Enabled = true;
        }
    }
    #endregion Sélection

    #region Édition
    internal class TurnOnAll_EditButton : Button
    {
        protected override void OnClick()
        {
            List<FeatureLayer> listLayers = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().ToList();
            List<AnnotationLayer> listAnnotations = MapView.Active.Map.GetLayersAsFlattenedList().OfType<AnnotationLayer>().ToList();
            
            foreach (var ly in listLayers) QueuedTask.Run(() => ly.SetEditable(true));
            foreach (var ly in listAnnotations) QueuedTask.Run(() => ly.SetEditable(true));
        }
        protected override void OnUpdate()
        {
            Enabled = true;
        }
    }

    internal class TurnOffAll_EditButton : Button
    {
        protected override void OnClick()
        {
            List<FeatureLayer> listLayers = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().ToList();
            List<AnnotationLayer> listAnnotations = MapView.Active.Map.GetLayersAsFlattenedList().OfType<AnnotationLayer>().ToList();
           
            foreach (var ly in listLayers) QueuedTask.Run(() => ly.SetEditable(false));
            foreach (var ly in listAnnotations) QueuedTask.Run(() => ly.SetEditable(false));
        }
        protected override void OnUpdate()
        {
            Enabled = true;
        }
    }
    #endregion Édition
}
