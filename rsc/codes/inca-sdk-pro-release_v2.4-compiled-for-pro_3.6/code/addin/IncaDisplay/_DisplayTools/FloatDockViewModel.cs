//   Copyright 2019 Esri
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at


using System.Resources;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using IGN.Inca.ArcGISPro.SDK.Commons;

namespace IncaDisplay._DisplayTools
{
    internal class FloatDockViewModel : DockPane
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private const string DockPaneId = "ID_FloatDock";

        protected FloatDockViewModel()
        {
            // get the dictionary of the current UI language ("CurrentUICulture") using the Culture Resources Manager
            Translator = CultureResourcesManager.GetInstance().dictLanguage;

            // initialize "inca_logger_sdk" logger
            LoggerUtils.GetInstance().InitializeLogger();
        }

        #region dictionnaire
        /// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ START ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        ///     One-Way Data Binding Properties, used specially for internationalization of the UI
        /// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        public string RectangleSelect { get { return Translator.GetString("RectangleSelect"); } }
        public string clearSelection { get { return Translator.GetString("clearSelection"); } }
        public string DeleteObj { get { return Translator.GetString("DeleteObj"); } }
        public string FinishSketch { get { return Translator.GetString("FinishSketch"); } }
        public string EditVerticesMove { get { return Translator.GetString("EditVerticesMove"); } }
        public string EditVerticesModifyFeature { get { return Translator.GetString("EditVerticesModifyFeature"); } }
        public string CastFeatures { get { return Translator.GetString("CastFeatures"); } }
        public string MapsButton { get { return Translator.GetString("MapsButton"); } }
        public string StreetView { get { return Translator.GetString("StreetView"); } }
        #endregion

        /// <summary>
        /// Culture translator
        /// </summary>
        private ResourceSet Translator { get; set; }

        /// <summary>
        /// Show the DockPane.
        /// </summary>
        internal static void Show()
        {
            DockPane pane = FrameworkApplication.DockPaneManager.Find(DockPaneId);
            pane?.Activate();
        }
        // <!--
        //esri_mapping_selectByRectangleTool
        //esri_mapping_clearSelectionButton
        //esri_editing_DelSelFeaturesBtn
        //IE_CastFeatures_SplitButton
        //esri_editing_EditVerticesMove
        //esri_editing_EditVerticesModifyFeature
        //esri_editing_FinishSketch
        //ID_Maps_Button
        //ID_StreetView_Button
        //-->
        #region Les Boutons

        private ICommand _cmd1;
        public ICommand cmd1
        {
            get
            {
                return _cmd1 ?? (_cmd1 = new RelayCommand(async () =>
                {
                    await QueuedTask.Run(() =>
                    {
                        string commandId = "esri_mapping_selectByRectangleTool";
                        var iCommand = FrameworkApplication.GetPlugInWrapper(commandId) as ICommand;
                        if (iCommand != null)
                            iCommand.Execute(null); ;
                    });
                }));
            }
        }

        private ICommand _cmd2;
        public ICommand cmd2
        {
            get
            {
                return _cmd2 ?? (_cmd2 = new RelayCommand(async () =>
                {
                    await QueuedTask.Run(() =>
                    {
                        string commandId = "esri_mapping_clearSelectionButton";
                        var iCommand = FrameworkApplication.GetPlugInWrapper(commandId) as ICommand;
                        if (iCommand != null)
                            iCommand.Execute(null); ;
                    });
                }));
            }
        }

        private ICommand _cmd3;
        public ICommand cmd3
        {
            get
            {
                return _cmd3 ?? (_cmd3 = new RelayCommand(async () =>
                {
                    await QueuedTask.Run(() =>
                    {
                        string commandId = "esri_editing_DelSelFeaturesBtn";
                        var iCommand = FrameworkApplication.GetPlugInWrapper(commandId) as ICommand;
                        if (iCommand != null)
                            iCommand.Execute(null); ;
                    });
                }));
            }
        }

        private ICommand _cmd4;
        public ICommand cmd4
        {
            get
            {
                return _cmd4 ?? (_cmd4 = new RelayCommand(async () =>
                {
                    await QueuedTask.Run(() =>
                    {
                        string commandId = "esri_editing_FinishSketch";
                        var iCommand = FrameworkApplication.GetPlugInWrapper(commandId) as ICommand;
                        if (iCommand != null)
                            iCommand.Execute(null); ;
                    });
                }));
            }
        }

        private ICommand _cmd5;
        public ICommand cmd5
        {
            get
            {
                return _cmd5 ?? (_cmd5 = new RelayCommand(async () =>
                {
                    await QueuedTask.Run(() =>
                    {
                        string commandId = "esri_editing_EditVerticesMove";
                        var iCommand = FrameworkApplication.GetPlugInWrapper(commandId) as ICommand;
                        if (iCommand != null)
                            iCommand.Execute(null); ;
                    });
                }));
            }
        }

        private ICommand _cmd6;
        public ICommand cmd6
        {
            get
            {
                return _cmd6 ?? (_cmd6 = new RelayCommand(async () =>
                {
                    await QueuedTask.Run(() =>
                    {
                        string commandId = "esri_editing_EditVerticesModifyFeature";
                        var iCommand = FrameworkApplication.GetPlugInWrapper(commandId) as ICommand;
                        if (iCommand != null)
                            iCommand.Execute(null); ;
                    });
                }));
            }
        }

        private ICommand _cmd7;
        public ICommand cmd7
        {
            get
            {
                return _cmd7 ?? (_cmd7 = new RelayCommand(async () =>
                {
                    await QueuedTask.Run(() =>
                    {
                        string commandId = "IE_CastFeatures_Button";
                        var iCommand = FrameworkApplication.GetPlugInWrapper(commandId) as ICommand;
                        if (iCommand != null)
                            iCommand.Execute(null); ;
                    });
                }));
            }
        }

        private ICommand _cmd8;
        public ICommand cmd8
        {
            get
            {
                return _cmd8 ?? (_cmd8 = new RelayCommand(async () =>
                {
                    await QueuedTask.Run(() =>
                    {
                        string commandId = "ID_Maps_Button";
                        var iCommand = FrameworkApplication.GetPlugInWrapper(commandId) as ICommand;
                        if (iCommand != null)
                            iCommand.Execute(null); ;

                    });
                }));
            }
        }

        private ICommand _cmd9;
        public ICommand cmd9
        {
            get
            {
                return _cmd9 ?? (_cmd9 = new RelayCommand(async () =>
                {
                    await QueuedTask.Run(() =>
                    {
                        string commandId = "ID_StreetView_Button";
                        var iCommand = FrameworkApplication.GetPlugInWrapper(commandId) as ICommand;
                        if (iCommand != null)
                            iCommand.Execute(null);

                    });
                }));
            }
        }
    }
    #endregion
    /// <summary>
    /// Button implementation to show the DockPane.
    /// </summary>
    internal class FloatDock_ShowButton : Button
    {
        protected override void OnClick()
        {
            FloatDockViewModel.Show();
        }
    }

}