using ArcGIS.Core.Data.Mapping;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using IGN.Inca.ArcGISPro.SDK.BLL;
using IGN.Inca.ArcGISPro.SDK.BLL.Desktop;
using IGN.Inca.ArcGISPro.SDK.Commons;
using IGN.Inca.ArcGISPro.SDK.Internationalization;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using Button = ArcGIS.Desktop.Framework.Contracts.Button;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace IncaLabeling._UpdateSymbols
{
    internal class UpdateSymbolsViewModel : DockPane
    {
        // Add-in's dockPane ID
        private const string _dockPaneID = "IL_UpdateSymbols";

        protected UpdateSymbolsViewModel()
        {
            // Subscribe to the ProjectOpenedEvent and pass in OnProjectOpened as the delegate.
            ProjectOpenedEvent.Subscribe(OnProjectOpened);

            // initialize "inca_logger_sdk" logger
            LoggerUtils.GetInstance().InitializeLogger();

            // get the dictionary of the current UI language ("CurrentUICulture") using the Culture Resources Manager
            Translator = CultureResourcesManager.GetInstance().dictLanguage;
        }

        #region Properties
        /// <summary>
        /// Culture translator
        /// </summary>
        private ResourceSet Translator { get; set; }
        //private readonly ResourceSet translator;

        /// <summary>
        /// Annotation class path property
        /// </summary>
        private string _annotationClassFullPath = @"";
        public string AnnotationClassFullPath
        {
            get { return _annotationClassFullPath; }
            set
            {
                SetProperty(ref _annotationClassFullPath, value, () => AnnotationClassFullPath);
            }
        }

        /// <summary>
        /// List of styles on the project
        /// </summary> 
        private ObservableCollection<StyleProjectItem> _styleProjectItems = new ObservableCollection<StyleProjectItem>();
        public ObservableCollection<StyleProjectItem> StyleProjectItems
        {
            get { return _styleProjectItems; }
        }

        /// <summary>
        /// Selected style from which we will search for symbols.
        /// </summary>
        private StyleProjectItem _selectedStyle;
        public StyleProjectItem SelectedStyle
        {
            get { return _selectedStyle; }
            set { SetProperty(ref _selectedStyle, value, () => SelectedStyle); }
        }

        /// <summary>
        /// Messages shown in UI
        /// </summary>
        public ObservableCollection<string> StackMessages { get; } = new ObservableCollection<string>();


        private string _logsFileFullPath = null;
        public string LogsFileFullPath
        {
            get
            {
                if (_logsFileFullPath == null)
                {
                    var getEnv = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    _logsFileFullPath = getEnv + @"\ArcGIS\Logs\inca_logger_sdk.log";
                }

                return _logsFileFullPath;
            }
        }

        /// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ START ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        ///     One-Way Data Binding Properties, used specially for internationalization of the UI
        /// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        public string MECHeadingText { get { return Translator.GetString("MECHeading"); } } // Text shown near the top of the DockPane.
        public string SelectAnnotationFeatureClassText { get { return Translator.GetString("SelectAnnotationFeatureClass"); } }
        public string SelectStylxWithTextSymbolsText { get { return Translator.GetString("SelectStylxWithTextSymbols"); } }
        public string LoadColorTextSymbolAndLabelPlacementText { get { return new StringBuilder(Translator.GetString("LoadColorTextSymbolAndLabelPlacement")).ToString(); } }
        public string LoadColorsText { get { return Translator.GetString("LoadColors"); } }
        public string LoadColorsToolTipText { get { return Translator.GetString("LoadColorsToolTip"); } }
        public string LoadTextSymbolsText { get { return Translator.GetString("LoadTextSymbols"); } }
        public string LoadTextSymbolsToolTipText { get { return Translator.GetString("LoadTextSymbolsToolTip"); } }
        public string LoadLabelPlacementsText { get { return Translator.GetString("LoadLabelPlacements"); } }
        public string LoadLabelPlacementsToolTipText { get { return Translator.GetString("LoadLabelPlacementsToolTip"); } }
        public string OverwriteItemsToolTipText { get { return Translator.GetString("OverwriteItemsToolTip"); } }
        public string OverwriteItemsText { get { return Translator.GetString("OverwriteItems"); } }
        public string LabelClassesText { get { return Translator.GetString("LabelClasses"); } }
        public string CreateLabelClassAndBindToLayerToolTipText { get { return Translator.GetString("CreateLabelClassAndBindToLayerToolTip"); } }
        public string CreateLabelClassAndBindToLayerText { get { return Translator.GetString("CreateLabelClassAndBindToLayer"); } }
        public string ReplaceSymbolsText { get { return Translator.GetString("ReplaceSymbols"); } }
        public string MessagesText { get { return Translator.GetString("Messages"); } }
        public string OpenLogsText { get { return Translator.GetString("OpenLogs"); } }
        public string ConfigFilePathIsNullOrEmptyMessage { get { return Translator.GetString("ConfigFilePathIsNullOrEmptyMessage"); } }
        public string StylxNotSelectedMessage { get { return Translator.GetString("StylxNotSelectedMessage"); } }
        public string ReplaceSymbolsOKMessage { get { return Translator.GetString("ReplaceSymbolsOKMessage"); } }
        public string ReplaceSymbolsFailedMessage { get { return Translator.GetString("ReplaceSymbolsFailedMessage"); } }
        public string LoadTextSymbolsOKMessage { get { return Translator.GetString("LoadTextSymbolsOKMessage"); } }
        public string LoadTextSymbolsFailedMessage { get { return Translator.GetString("LoadTextSymbolsFailedMessage"); } }
        public string LoadLabelPlacementsOKMessage { get { return Translator.GetString("LoadLabelPlacementsOKMessage"); } }
        public string LoadLabelPlacementsFailedMessage { get { return Translator.GetString("LoadLabelPlacementsFailedMessage"); } }
        public string CreateLabelClassAndBindToLayerOKMessage { get { return Translator.GetString("CreateLabelClassAndBindToLayerOKMessage"); } }
        public string CreateLabelClassAndBindToLayerFailedMessage { get { return Translator.GetString("CreateLabelClassAndBindToLayerFailedMessage"); } }
        /// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ END ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        #endregion

        #region Commands
        /// <summary>
        /// Command to browse Excel config file with Windows file explorer
        /// </summary>
        private ICommand _browseAnnotationFeatureClassCommand;
        public ICommand BrowseAnnotationFeatureClassCommand
        {
            get { return _browseAnnotationFeatureClassCommand ?? (_browseAnnotationFeatureClassCommand = new RelayCommand(() => BrowseFileName())); }
        }

        /// <summary>
        /// Command to update .stylx items in UI control
        /// </summary>
        private ICommand _updateStyleProjectItemsCommand;
        public ICommand UpdateStyleProjectItemsCommand
        {
            get { return _updateStyleProjectItemsCommand ?? (_updateStyleProjectItemsCommand = new RelayCommand(() => UpdateStyleList())); }
        }


        /// <summary>
        /// Command to start reading and creating Label Classes and bind them to the Layers
        /// </summary>
        private ICommand _replaceSymbolsCommand;
        public ICommand ReplaceSymbolsCommand
        {
            get { return _replaceSymbolsCommand ?? (_replaceSymbolsCommand = new RelayCommand(() => ReplaceSymbols())); }
        }

        /// <summary>
        /// Command to open logs file
        /// </summary>
        private ICommand _openLogsFileCommand;
        public ICommand OpenLogsFileCommand
        {
            get { return _openLogsFileCommand ?? (_openLogsFileCommand = new RelayCommand(() => OpenLogsFile())); }
        }
        #endregion


        /// <summary>
        /// Call ReplaceAnnotationSymbols() method to change the symbols of an annotation class
        /// </summary>
        public async void ReplaceSymbols()
        {
            bool result = await AnnotationSymbols.GetInstance().ReplaceAnnotationSymbols(AnnotationClassFullPath, SelectedStyle);

            if (result)
                AddNewMessageLogInStack(ReplaceSymbolsOKMessage);
            else
                AddNewMessageLogInStack(ReplaceSymbolsFailedMessage);
        }

        #region Manage other controls in the DockPane
        /// <summary>
        /// Show the DockPane
        /// </summary>
        internal static void Show()
        {
            DockPane pane = FrameworkApplication.DockPaneManager.Find(_dockPaneID);
            if (pane == null)
                return;

            pane.Activate();
        }

        /// <summary>
        /// Method to help select the Annotation feature class
        /// </summary>
        public void BrowseFileName()
        {
            var browseFilter = new BrowseProjectFilter("Annotion Classes");
            // Autorise la navigation dans les gdbs fichier et d'entreprise
            browseFilter.AddDoBrowseIntoTypeId("fgdb");
            browseFilter.AddDoBrowseIntoTypeId("egdb");
            // Autorise seulement la sélection des classes d'annotation
            browseFilter.AddCanBeTypeId("fgdb_fc_annotation");
            browseFilter.AddCanBeTypeId("egdb_fc_annotation");

            // Create OpenItemDialog 
            OpenItemDialog dlg = new OpenItemDialog
            {
                MultiSelect = false,
                BrowseFilter = browseFilter
            };

            // Display OpenItemDialog by calling ShowDialog method
            //dlg.Filter = ItemFilters.FeatureClasses_All;
            bool? result = dlg.ShowDialog();

            // Get the selected annotation feature class and show it in the TextBox
            if (result != true) return;
            Item item = dlg.Items.First();

            // Set the config file
            string filename = item.Path;
            AnnotationClassFullPath = filename;
        }

        /// <summary>
        /// Delegate method called when the OnProjectOpened event is published
        /// </summary>
        private void OnProjectOpened(ProjectEventArgs obj)
        {
            // Fill the style files in the current project
            UpdateStyleList();
        }

        /// <summary>
        /// Update the list of styles for the current project
        /// </summary>
        private void UpdateStyleList()
        {
            // Update the combo box with current project styles
            if (Project.Current != null)
            {
                _styleProjectItems.Clear();

                // Update list using currently referenced styles in project
                var projectStyleContainer = Project.Current.GetItems<StyleProjectItem>();
                foreach (var style in projectStyleContainer)
                {
                    _styleProjectItems.Add(style);
                }

                NotifyPropertyChanged(() => StyleProjectItems);
            }
        }

        /// <summary>
        /// Adds a message to the property bound with the listbox control in UI, adds the date and time to the message
        /// </summary>
        /// <param name="message">A message to show in the UI messages listbox</param>
        public void AddNewMessageLogInStack(string message)
        {
            var messWithDate = string.Format("{0} - {1}", DateTime.Now.ToString("HH:mm:ss"), message);
            StackMessages.Add(messWithDate);
            NotifyPropertyChanged(() => StackMessages);
        }

        /// <summary>
        /// Opens the log file in a notepad instance
        /// </summary>
        public void OpenLogsFile()
        {
            // try to open the log file in a notepad++ instance first
            var localDevFOANotepadppPathx86 = @"C:\Program Files (x86)\Notepad++\notepad++.exe";
            var localDevFOANotepadppPath = @"C:\Program Files\Notepad++\notepad++.exe";

            if (File.Exists(localDevFOANotepadppPathx86))
                Process.Start(localDevFOANotepadppPathx86, LogsFileFullPath);
            else if (File.Exists(localDevFOANotepadppPath))
                Process.Start(localDevFOANotepadppPath, LogsFileFullPath);
            else
                // if notepad++ wasn't found then open the log file file default installed notepad
                Process.Start(@"notepad.exe", LogsFileFullPath);
        }
        #endregion
    }

    /// <summary>
    /// Button implementation to show the "IL_UpdateSymbols" DockPane
    /// </summary>
    internal class UpdateSymbols_ShowButton : Button
    {
        protected override void OnClick()
        {
            UpdateSymbolsViewModel.Show();
        }
    }
}
