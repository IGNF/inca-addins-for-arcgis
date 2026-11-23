//   Copyright 2019 Esri
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at

using ArcGIS.Core.Events;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Internal.Catalog;
using ArcGIS.Desktop.Mapping;
using IGN.Inca.ArcGISPro.SDK.Commons;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Resources;
using System.Windows;
using System.Windows.Input;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

namespace IncaDisplay._DisplayManager

{
    internal class SelectEditViewModel : DockPane
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private const string DockPaneId = "ID_SelectEditDockpane";

        protected SelectEditViewModel()
        {
            // get the dictionary of the current UI language ("CurrentUICulture") using the Culture Resources Manager
            Translator = CultureResourcesManager.GetInstance().dictLanguage;

            // initialize "inca_logger_sdk" logger
            LoggerUtils.GetInstance().InitializeLogger();

        }

        /// <summary>
        /// Liste des règles de selection/edition
        /// </summary>
        public static List<SelectEditRule> SelectEditRules = new List<SelectEditRule>();
        public static List<SelectEditRule> SelectEditRules_extrait = null;

        /// <summary>
        /// Liste des noms des régles de selection/edition
        /// </summary>
        public static List<string> List_Choice = null;

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

        private string _SE_Selectable;
        public string SE_Selectable
        {
            get { return _SE_Selectable; }
            set { SetProperty(ref _SE_Selectable, value, () => SE_Selectable); }
        }

        // Liste des Visibilités disponibles dans la gdb
        private ObservableCollection<string> _selecteditlist;
        public ObservableCollection<string> Selecteditlist
        {
            get { return _selecteditlist; }
            set
            {
                _selecteditlist = value;
                NotifyPropertyChanged(new PropertyChangedEventArgs("Selecteditlist"));
            }
        }

        private string _SaveName;
        public string SaveName
        {
            get { return _SaveName; }
            set { SetProperty(ref _SaveName, value, () => SaveName); }
        }
        #endregion

        #region Dictionnaire 
        /// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ START ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        ///     One-Way Data Binding Properties, used specially for internationalization of the UI
        /// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ 
        public string SERefresh { get { return Translator.GetString("SERefresh"); } }
        public string SESave { get { return Translator.GetString("SESave"); } }
        public string SEOpen { get { return Translator.GetString("SEOpen"); } }
        public string SEInfo { get { return Translator.GetString("SEInfo"); } }
        public string SEVisibility { get { return Translator.GetString("SEVisibility"); } }
        public string SESaveName { get { return Translator.GetString("SESaveName"); } }

        #endregion

        #region boutons
        /// <summary>
        /// affiche la liste des visibilités enregistrées depuis le fichier excel
        /// </summary>
        private ICommand _CmdCreerList;
        public ICommand CmdCreateList
        {
            get
            {
                return _CmdCreerList ?? (_CmdCreerList = new RelayCommand(async () =>
                {
                    try
                    {
                        // lecture du fichier excel si aucune liste enregistrée
                        if (List_Choice == null)
                            await SelectEditControls.GetInstance().CheckFileNameAndLoad();

                        // affichage des noms de svisibilités
                        Selecteditlist = new ObservableCollection<string>();
                        foreach (var f in List_Choice) Selecteditlist.Add(f);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Warning", MessageBoxButton.OK, MessageBoxImage.Error);

                    }
                }));
            }
        }

        /// <summary>
        /// bouton pour créer la liste des visibilités enregistrées depuis le fichier excel
        /// </summary>

        private ICommand _CmdSelectable;
        public ICommand CmdSelectable
        {
            get
            {
                return _CmdSelectable ?? (_CmdSelectable = new RelayCommand(async () =>
                {
                    try
                    {
                        // lecture du fichier excel
                        await SelectEditControls.GetInstance().BrowseAndCheckFileName();

                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Problem reading Excel config file. Message error: " + ex.Message, ex);
                    }
                }));
            }
        }

        /// <summary>
        /// bouton pour enregistrer la liste des visibilités enregistrées dans le fichier excel
        /// </summary>
        private ICommand _CmdSave;
        public ICommand CmdSave
        {
            get
            {
                return _CmdSave ?? (_CmdSave = new RelayCommand(async () =>
                {
                    try
                    {
                        // vérifie qu'une carte soit active
                        MapView mapview = MapView.Active;
                        if (mapview == null)
                        {
                            var mapPane = FrameworkApplication.Panes.OfType<IMapPane>().First() as Pane;
                            mapPane.Activate();
                        }
                        Map map = MapView.Active.Map;

                        // parcours et eregistre le fichier
                        if (map != null)
                            if (SaveName == "" || SaveName == null)
                                if (SE_Selectable != "" && SE_Selectable != null)
                                    await SelectEditControls.GetInstance().BrowseAndSaveFile(map, SE_Selectable);
                                else MessageBox.Show(SelectEditCore.NameMissing, "Warning", MessageBoxButton.OK);

                            else await SelectEditControls.GetInstance().BrowseAndSaveFile(map, SaveName);

                        // efface l'affichage
                        SaveName = null;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Problem reading Excel config file. Message error: " + ex.Message, ex);
                    }
                }));
            }
        }

        /// <summary>
        /// bouton pour activer les visibilités enregistrées dans le fichier excel
        /// </summary>
        private ICommand _CmdRefresh;
        public ICommand CmdRefresh
        {
            get
            {
                return _CmdRefresh ?? (_CmdRefresh = new RelayCommand(async () =>
                {
                    try
                    {
                        // carte active
                        MapView mapview = MapView.Active;
                        if (mapview == null)
                        {
                            var mapPane = FrameworkApplication.Panes.OfType<IMapPane>().First() as Pane;
                            mapPane.Activate();
                        }
                        Map map = MapView.Active.Map;
                        if (map != null)
                        {
                            // les règles de l'onglet sélectionné
                            SelectEditRules_extrait = new List<SelectEditRule>();
                            // les dossiers et couches concernés
                            List<string> ListFL = new List<string>();

                            SelectEditRules_extrait.AddRange(SelectEditRules.Where(r => r.SrcRuleName == SE_Selectable));

                            // pour toutes les règles, on sélectionne les règles de l'onglet concerné (SE_Selectable)
                            foreach (var r in SelectEditRules_extrait)
                            {
                                if (!string.IsNullOrWhiteSpace(r.SrcLayer) && r.SrcRuleName.Equals(SE_Selectable))
                                    ListFL.Add(r.SrcLayer);

                                if (!string.IsNullOrWhiteSpace(r.SrcGroupLayer) && r.SrcRuleName.Equals(SE_Selectable))
                                    ListFL.Add(r.SrcGroupLayer);

                                if (!string.IsNullOrWhiteSpace(r.SrcMosaicLayer) && r.SrcRuleName.Equals(SE_Selectable))
                                    ListFL.Add(r.SrcMosaicLayer);
                            }

                            // listes des couches et annotations de la carte
                            List<FeatureLayer> listLayers = map.GetLayersAsFlattenedList().OfType<FeatureLayer>().ToList();
                            List<AnnotationLayer> listAnnotations = map.GetLayersAsFlattenedList().OfType<AnnotationLayer>().ToList();
                            List<ImageMosaicSubLayer> listmozaicLayers = map.GetLayersAsFlattenedList().OfType<ImageMosaicSubLayer>().ToList();

                            List<GroupLayer> listGroupLayers = map.GetLayersAsFlattenedList().OfType<GroupLayer>().ToList();
                            List<MosaicLayer> listGroupMosaics = MapView.Active.Map.GetLayersAsFlattenedList().OfType<MosaicLayer>().ToList();

                            // pour toutes les couches, on vérifie s'il faut, l'afficher, la rendre sélectionnable et/ou éditable  
                            foreach (var fl in listLayers)
                            {
                                if (ListFL.Contains(fl.Name))
                                {
                                    SelectEditRule r = SelectEditRules_extrait.Find(r => r.SrcLayer == fl.Name);
                                    if (r != null)
                                    {
                                        MosaicLayer folder = listGroupMosaics.FirstOrDefault(fd => fl.Parent.Equals(fd));

                                        await QueuedTask.Run(() =>
                                        {
                                            fl.SetVisibility(r.SrcDisplay);
                                            if (folder != null) folder.SetVisibility(r.SrcDisplay);
                                            fl.SetSelectable(r.SrcSelectable);
                                            fl.SetEditable(r.SrcEditable);
                                        });
                                    }
                                }
                                // par défaut on met tout à faux.
                                else
                                {
                                    await QueuedTask.Run(() =>
                                    {
                                        fl.SetVisibility(false);
                                        fl.SetSelectable(false);
                                        fl.SetEditable(false);
                                    });
                                }
                            }

                            // idem pour les annotations
                            foreach (var al in listAnnotations)
                            {
                                if (ListFL.Contains(al.Name))
                                {
                                    SelectEditRule r = SelectEditRules_extrait.Find(r => r.SrcLayer == al.Name);
                                    await QueuedTask.Run(() =>
                                    {
                                        al.SetVisibility(r.SrcDisplay);
                                        al.SetSelectable(r.SrcSelectable);
                                        al.SetEditable(r.SrcEditable);
                                    });
                                }
                                // par défaut on met tout à faux.
                                else
                                {
                                    await QueuedTask.Run(() =>
                                    {
                                        al.SetVisibility(false);
                                        al.SetSelectable(false);
                                        al.SetEditable(false);
                                    });
                                }
                            }

                            // idem pour les images rasters
                            foreach (var fl in listmozaicLayers)
                            {
                                if (ListFL.Contains(fl.Name))
                                {
                                    SelectEditRule r = SelectEditRules_extrait.Find(r => (r.SrcLayer == fl.Name
                                    && r.SrcMosaicLayer == fl.Parent.ToString()));
                                    if (r != null)
                                    {
                                        await QueuedTask.Run(() => fl.SetVisibility(r.SrcDisplay));
                                        // on ouvre aussi le dossier :
                                        MosaicLayer folder = listGroupMosaics.FirstOrDefault(fd => fl.Parent.Equals(fd));
                                        if (folder != null) await QueuedTask.Run(() => folder.SetVisibility(r.SrcDisplay));
                                    }
                                    else await QueuedTask.Run(() => fl.SetVisibility(false));
                                }
                                // par défaut on met tout à faux.
                                else await QueuedTask.Run(() => fl.SetVisibility(false));
                            }

                            // idem pour les groupes
                            foreach (var gl in listGroupLayers)
                            {
                                // le groupe est dans les règles à appliquer
                                if (ListFL.Contains(gl.Name))
                                {
                                    SelectEditRule r = SelectEditRules_extrait.Find(r => r.SrcGroupLayer == gl.Name && r.SrcLayer == "");
                                    if (r != null)
                                    {
                                        gl.SetVisibility(r.SrcDisplay);
                                        if (r.SrcDisplay == true)
                                        {
                                            foreach (var l in gl.GetLayersAsFlattenedList().OfType<FeatureLayer>().ToList())
                                                await QueuedTask.Run(() => l.SetVisibility(true));
                                            foreach (var a in gl.GetLayersAsFlattenedList().OfType<AnnotationLayer>().ToList())
                                                await QueuedTask.Run(() => a.SetVisibility(true));
                                        }
                                    }
                                    else
                                    {
                                        bool IsVisible = false;
                                        foreach (var l in gl.GetLayersAsFlattenedList().OfType<FeatureLayer>().ToList())
                                            if (l.IsVisible == true)
                                                IsVisible = true;
                                        foreach (var a in gl.GetLayersAsFlattenedList().OfType<AnnotationLayer>().ToList())
                                            if (a.IsVisible == true)
                                                IsVisible = true;

                                        await QueuedTask.Run(() => gl.SetVisibility(IsVisible));
                                    }
                                }
                                else
                                {
                                    bool IsVisible = false;
                                    foreach (var l in gl.GetLayersAsFlattenedList().OfType<FeatureLayer>().ToList())
                                        if (l.IsVisible == true) IsVisible = true;
                                    foreach (var a in gl.GetLayersAsFlattenedList().OfType<AnnotationLayer>().ToList())
                                        if (a.IsVisible == true) IsVisible = true;
                                    await QueuedTask.Run(() => gl.SetVisibility(IsVisible));
                                }
                            }

                            //idem pour les mosaïc
                            foreach (var ml in listGroupMosaics)
                            {
                                // le groupe n'est pas dans les règles à appliquer, on le masque
                                if (!ListFL.Contains(ml.Name))
                                    await QueuedTask.Run(() => ml.SetVisibility(false));  
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Problem reading Excel config file. Message error: " + ex.Message, ex);
                    }

                }, () => SE_Selectable != null));
            }
        }

        private ICommand _CmdInfo;
        public ICommand CmdInfo
        {
            get
            {
                return _CmdInfo ?? (_CmdInfo = new RelayCommand(async () =>
                {
                    try
                    {
                        await SelectEditControls.GetInstance().CheckFileName();
                    }

                    catch (Exception ex)
                    {
                        throw new Exception("Problem reading Excel config file. Message error: " + ex.Message, ex);
                    }

                }));
            }
        }
        #endregion
    }


    /// <summary>
    /// Button implementation to show the DockPane.
    /// </summary>
    internal class SelectEditDockpane_ShowButton : Button
    {
        protected override void OnClick()
        {
            SelectEditViewModel.Show();
        }
    }

    #region Events
    /// <summary>
    /// Arguments de l'événement FileNameChangedEvent
    /// </summary>
    internal sealed class FileNameChangedEventArgs : EventArgs
    {
        internal string _value;
        internal FileNameChangedEventArgs(string value)
        {
            _value = value;
        }
    }

    /// <summary>
    /// Evénement déclenché par le changement de nom du fichier Excel
    /// </summary>
    internal class FileNameChangedEvent : CompositePresentationEvent<FileNameChangedEventArgs>
    {
        public static SubscriptionToken Subscribe(Action<FileNameChangedEventArgs> action, bool keepSubscriberAlive = false)
        {
            return FrameworkApplication.EventAggregator.GetEvent<FileNameChangedEvent>().Register(action, keepSubscriberAlive);
        }

        public static void Unsubscribe(Action<FileNameChangedEventArgs> action)
        {
            FrameworkApplication.EventAggregator.GetEvent<FileNameChangedEvent>().Unregister(action);
        }

        public static void Unsubscribe(SubscriptionToken token)
        {
            FrameworkApplication.EventAggregator.GetEvent<FileNameChangedEvent>().Unregister(token);
        }

        internal static void Publish(FileNameChangedEventArgs eventArgs)
        {
            FrameworkApplication.EventAggregator.GetEvent<FileNameChangedEvent>().Broadcast(eventArgs);
        }
    }
    #endregion Events

}