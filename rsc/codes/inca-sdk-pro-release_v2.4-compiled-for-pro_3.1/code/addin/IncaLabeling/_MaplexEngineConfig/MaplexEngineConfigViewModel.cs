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
using Button = ArcGIS.Desktop.Framework.Contracts.Button;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace IncaLabeling._MaplexEngineConfig
{
    internal class MaplexEngineConfigViewModel : DockPane
    {
        // Add-in's dockPane ID
        private const string _dockPaneID = "IL_MaplexEngineConfig";

        protected MaplexEngineConfigViewModel()
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
        /// Excel config file path property
        /// </summary>
        private string _excelConfigFileFullPath = @"";
        public string ExcelConfigFileFullPath
        {
            get { return _excelConfigFileFullPath; }
            set
            {
                SetProperty(ref _excelConfigFileFullPath, value, () => ExcelConfigFileFullPath);
            }
        }

        /// <summary>
        /// Overwrite style items if checked
        /// </summary>
        private bool _overwriteMaplexStyles = true;
        public bool OverwriteMaplexStyles
        {
            get { return _overwriteMaplexStyles; }
            set { SetProperty(ref _overwriteMaplexStyles, value, () => OverwriteMaplexStyles); }
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
        public string SelectConfigFileText { get { return Translator.GetString("SelectConfigFile"); } }
        public string SelectStylxFileText { get { return Translator.GetString("SelectStylxFile"); } }
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
        public string MessagesText { get { return Translator.GetString("Messages"); } }
        public string OpenLogsText { get { return Translator.GetString("OpenLogs"); } }
        public string ConfigFilePathIsNullOrEmptyMessage { get { return Translator.GetString("ConfigFilePathIsNullOrEmptyMessage"); } }
        public string StylxNotSelectedMessage { get { return Translator.GetString("StylxNotSelectedMessage"); } }
        public string LoadColorsOKMessage { get { return Translator.GetString("LoadColorsOKMessage"); } }
        public string LoadColorsFailedMessage { get { return Translator.GetString("LoadColorsFailedMessage"); } }
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
        private ICommand _browseConfigFileCommand;
        public ICommand BrowseConfigFileCommand
        {
            get { return _browseConfigFileCommand ?? (_browseConfigFileCommand = new RelayCommand(() => this.BrowseFileName())); }
        }

        /// <summary>
        /// Command to update .stylx items in UI control
        /// </summary>
        private ICommand _updateStyleProjectItemsCommand;
        public ICommand UpdateStyleProjectItemsCommand
        {
            get { return _updateStyleProjectItemsCommand ?? (_updateStyleProjectItemsCommand = new RelayCommand(() => this.UpdateStyleList())); }
        }

        /// <summary>
        /// Command to start reading and loading the Colors into the local gallery (.stylx file)
        /// </summary>
        private ICommand _loadColorsCommand;
        public ICommand LoadColorsCommand
        {
            get { return _loadColorsCommand ?? (_loadColorsCommand = new RelayCommand(() => this.LoadColorsIntoGallery())); }
        }

        /// <summary>
        /// Command to start reading and loading the Text Symbols into the local gallery (.stylx file)
        /// </summary>
        private ICommand _loadTextSymbolsCommand;
        public ICommand LoadTextSymbolsCommand
        {
            get { return _loadTextSymbolsCommand ?? (_loadTextSymbolsCommand = new RelayCommand(() => this.LoadTextSymbolsIntoGallery())); }
        }

        /// <summary>
        /// Command to start reading and loading the Label Placements into the local gallery (.stylx file)
        /// </summary>
        private ICommand _loadLabelPlacementsCommand;
        public ICommand LoadLabelPlacementsCommand
        {
            get { return _loadLabelPlacementsCommand ?? (_loadLabelPlacementsCommand = new RelayCommand(() => this.LoadLabelPlacementsIntoGallery())); }
        }

        /// <summary>
        /// Command to start reading and creating Label Classes and bind them to the Layers
        /// </summary>
        private ICommand _createLabelClassAndBindToLayerCommand;
        public ICommand CreateLabelClassAndBindToLayerCommand
        {
            get { return _createLabelClassAndBindToLayerCommand ?? (_createLabelClassAndBindToLayerCommand = new RelayCommand(() => this.CreateLabelClassAndBindToLayer())); }
        }

        /// <summary>
        /// Command to open logs file
        /// </summary>
        private ICommand _openLogsFileCommand;
        public ICommand OpenLogsFileCommand
        {
            get { return _openLogsFileCommand ?? (_openLogsFileCommand = new RelayCommand(() => this.OpenLogsFile())); }
        }
        #endregion

        #region Interaction with the "IncaProSDK" Library
        /// <summary>
        /// Call LoadConfigurationsIntoStyleFile() method to create the Colors
        /// </summary>
        public async void LoadColorsIntoGallery()
        {
            if (string.IsNullOrEmpty(ExcelConfigFileFullPath))
                MessageBox.Show(ConfigFilePathIsNullOrEmptyMessage);
            else if (SelectedStyle == null)
                MessageBox.Show(StylxNotSelectedMessage);
            else
            {
                bool result = await MaplexEngineConfiguration.GetInstance().LoadConfigurationsIntoStyleFile(ExcelConfigFileFullPath, SelectedStyle, StyleItemType.Color, OverwriteMaplexStyles);

                if (result)
                    AddNewMessageLogInStack(LoadColorsOKMessage);
                else
                    AddNewMessageLogInStack(LoadColorsFailedMessage);
            }
        }

        /// <summary>
        /// Call LoadConfigurationsIntoStyleFile() method to create the Text Symbols
        /// </summary>
        public async void LoadTextSymbolsIntoGallery()
        {
            if (string.IsNullOrEmpty(ExcelConfigFileFullPath))
                MessageBox.Show(ConfigFilePathIsNullOrEmptyMessage);
            else if (SelectedStyle == null)
                MessageBox.Show(StylxNotSelectedMessage);
            else
            {
                bool result = await MaplexEngineConfiguration.GetInstance().LoadConfigurationsIntoStyleFile(ExcelConfigFileFullPath, SelectedStyle, StyleItemType.TextSymbol, OverwriteMaplexStyles);

                if (result)
                    AddNewMessageLogInStack(LoadTextSymbolsOKMessage);
                else
                    AddNewMessageLogInStack(LoadTextSymbolsFailedMessage);
            }
        }

        /// <summary>
        /// Call LoadConfigurationsIntoStyleFile() method to create the Label Placements
        /// </summary>
        public async void LoadLabelPlacementsIntoGallery()
        {
            if (string.IsNullOrEmpty(ExcelConfigFileFullPath))
                MessageBox.Show(ConfigFilePathIsNullOrEmptyMessage);
            else if (SelectedStyle == null)
                MessageBox.Show(StylxNotSelectedMessage);
            else
            {
                bool result = await MaplexEngineConfiguration.GetInstance().LoadConfigurationsIntoStyleFile(ExcelConfigFileFullPath, SelectedStyle, StyleItemType.MaplexLabelPlacement, OverwriteMaplexStyles);

                if (result)
                    AddNewMessageLogInStack(LoadLabelPlacementsOKMessage);
                else
                    AddNewMessageLogInStack(LoadLabelPlacementsFailedMessage);
            }
        }

        /// <summary>
        /// Call CreateLabelClassAndBindToLayer() method to create Label Classes and bind them to Layers
        /// </summary>
        public async void CreateLabelClassAndBindToLayer()
        {
            bool result = await MaplexEngineConfiguration.GetInstance().CreateLabelClassAndBindToLayer(ExcelConfigFileFullPath, SelectedStyle);

            if (result)
                AddNewMessageLogInStack(CreateLabelClassAndBindToLayerOKMessage);
            else
                AddNewMessageLogInStack(CreateLabelClassAndBindToLayerFailedMessage);
        }
        #endregion

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
        /// Method to help select the Excel config file
        /// </summary>
        public void BrowseFileName()
        {
            // Create OpenFileDialog 
            OpenFileDialog dlg = new OpenFileDialog
            {
                // Set filter for file extension and default file extension 
                DefaultExt = ".xlsx",
                Filter = "Excel Files|*.xls;*.xlsx"
            };

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected Excel file and show it in the TextBox
            if (result != true) return;
            // Set the config file
            string filename = dlg.FileName;
            ExcelConfigFileFullPath = filename;
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
    /// Button implementation to show the "IL_MaplexEngineConfig" DockPane
    /// </summary>
    internal class MaplexEngineConfig_ShowButton : Button
    {
        protected override void OnClick()
        {
            MaplexEngineConfigViewModel.Show();
        }
    }
}
