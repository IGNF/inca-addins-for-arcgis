using IGN.Inca.ArcGISPro.SDK.Commons;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace IncaDisplay._AttributeSets
{
    public partial class InfoButtonWindowViewModel : INotifyPropertyChanged
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #region Parameters
        private readonly Window _window;
        private readonly AttributeSetsRule _originalRule;
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public class RelayCommand : ICommand
        {
            private readonly Action<object> _execute;
            private readonly Func<object, bool> _canExecute;

            public RelayCommand(Action<object> execute,
                                Func<object, bool> canExecute = null)
            {
                _execute = execute;
                _canExecute = canExecute;
            }

            public bool CanExecute(object parameter)
                => _canExecute?.Invoke(parameter) ?? true;

            public void Execute(object parameter)
                => _execute(parameter);

            public event EventHandler CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }
        }
        public ICommand IconClickCommand { get; }

        private ObservableCollection<FieldValueItem> _fields;
        public ObservableCollection<FieldValueItem> Fields
        {
            get => _fields;
            set
            {
                _fields = value;
                OnPropertyChanged();
            }
        }
        public ICommand CmdOK { get; }
        public ICommand CmdCancel { get; }
        public AttributeSetsRule Rule { get; }
        public ObservableCollection<IconItem> Icons { get; } = new();
        #endregion Parameters

        #region dictionnaire
        public static string btnCancel_
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Annuler";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Cancelar";
                else return "Cancel";
            }
        }
        #endregion dictionnaire

        #region Constructors
        protected InfoButtonWindowViewModel()
        {
            // initialize "inca_logger_sdk" logger
            LoggerUtils.GetInstance().InitializeLogger();
        }

        public InfoButtonWindowViewModel(Window window, AttributeSetsRule rule)
        {
            if (rule is null || rule.BtnName == "" || rule.BtnName is null) return;

            // initialisation
            Rule = rule;
            _window = window;
            _originalRule = Rule.Clone();
            Rule.InitialIconPath = rule.BtnIconPath;
            Rule.InitialIconName = rule.BtnIconName;

            // chargement des attributs
            Fields = new ObservableCollection<FieldValueItem>(
                Rule.BtnFieldsName_Values.Select(kvp =>
                new FieldValueItem
                {
                    Key = kvp.Key,
                    Value = kvp.Value
                })
            );

            // contrôles sur les boutons actifs
            CmdOK = new RelayCommand(OnOK);
            CmdCancel = new RelayCommand(OnCancel);
            IconClickCommand = new RelayCommand(OnIconClicked);

            LoadIcons(Rule);
        }
        #endregion Constructors

        #region Functions
        /// <summary>
        /// Chargement et affichage des images du dossier du XML 
        /// dans le panneau d'édition d'un bouton
        /// </summary>
        /// <param name="rule"></param>
        private void LoadIcons(AttributeSetsRule rule)
        {
            // nettoie avant de charger les icones
            Icons.Clear();

            // recupération du chemin 
            var folderPath = rule.BtnIconPath.Replace("file:\\", "");
            if (!Directory.Exists(folderPath)) return;

            // chargement des icones
            // chargement des icones
            foreach (var file in Directory.GetFiles(folderPath, "*.ico")
                     .Where(f => !Path.GetFileName(f)
                                      .StartsWith("ITEM_", StringComparison.OrdinalIgnoreCase)))
                Icons.Add(new IconItem { ImagePath = file });
        }

        /// <summary>
        ///  Action lors du clic sur une icone :
        ///  modification de l'image dans la boite d'édition
        ///  mise à jour de l'image du bouton dans le panneau principale, 
        ///  et modification du nom de l'image
        /// </summary>
        /// <param name="parameter"></param>
        private void OnIconClicked(object parameter)
        {
            if (parameter is not IconItem icon) return;

            // Mise à jour de la règle
            Rule.BtnIconPath = Path.GetDirectoryName(icon.ImagePath);
            Rule.BtnIconName = Path.GetFileName(icon.ImagePath);
        }


        /// <summary>
        /// Action du bouton OK (fenêtre d'édition d'une bouton)
        /// Enregistre les modifications de la fiche dans les attributs du bouton
        /// </summary>
        /// <param name="parameter"></param>
        private void OnOK(object parameter)
        {
            Rule.BtnFieldsName_Values = Fields.ToDictionary(f => f.Key, f => f.Value);

            // si clone :
            _originalRule.UpdateFrom(Rule);

            _window.DialogResult = true;
            _window.Close();
        }


        /// <summary>
        /// Action du bouton CANCEL (fenêtre d'édition d'une bouton)
        /// Annule toutes les modification saisies dans la fiche
        /// </summary>
        /// <param name="parameter"></param>
        private void OnCancel(object parameter)
        {
            Rule.UpdateFrom(_originalRule);
            _window.DialogResult = false;
            _window.Close();
        }
        #endregion functions
    }
}
