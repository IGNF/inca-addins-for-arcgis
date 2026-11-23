//   Copyright 2019 Esri
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at


using ArcGIS.Core.Data;
using ArcGIS.Core.Events;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Editing.Attributes;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using IGN.Inca.ArcGISPro.SDK.Commons;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
namespace IncaDisplay._AttributeSets
{
    internal class AttributeSetsViewModel : DockPane, INotifyPropertyChanged
    {
        #region Parameters
        // public AttributeSetsViewModel dockPane;
        private const string DockPaneId = "ID_AttributeSets";
        public static List<AttributeSetsRule> AttributeSetsRules = new List<AttributeSetsRule>();
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private static string _origXML = Properties.Settings.Default.AttributeSetsConfigFile;

        public ObservableCollection<AttributeSetsRule> Rules { get; } = new();
        public ICommand ApplyRuleCommand { get; }

        public ICommand ShowInfoPaneCommand { get; }
        

        /// <summary>
        /// Liens entre le nom et l'action du menu déroulant
        /// </summary>
        public ObservableCollection<ActionItem> Actions { get; } =
            new()
            {
                new ActionItem(RuleAction.Delete, AS_Delete),
                new ActionItem(RuleAction.Import, AS_Import),
                new ActionItem(RuleAction.Export, AS_Export),
            };


        private ActionItem _selectedAction;
        public ActionItem SelectedAction
        {
            get => _selectedAction;
            set
            {
                _selectedAction = value;
                OnPropertyChanged(nameof(SelectedAction));

                if (value != null)
                {
                    ExecuteAction(value.Action);
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(
                        new Action(() =>
                        {
                            _selectedAction = null;
                            OnPropertyChanged(nameof(SelectedAction));
                        }),
                        System.Windows.Threading.DispatcherPriority.Background);
                }
            }
        }

        private string _comboText = "";
        public string ComboText
        {
            get => _comboText;
            set
            {
                _comboText = value;
                OnPropertyChanged(nameof(ComboText));
            }
        }
        #endregion Parameters

        #region dictionnaire
        public static string btnAttributekey_
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Clé";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "clave";
                else return "Key";
            }
        }

        public static string btnAttributeValue_
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Valeurs";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Valores";
                else return "Values";
            }
        }

        public static string AS_Import
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Importer les boutons";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Importar botones";
                else return "Import buttons";
            }
        }

        public static string AS_Export
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Exporter les boutons";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Exportar botones";
                else return "Export buttons";
            }
        }

        public static string AS_Delete
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Supprimer les boutons";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Eliminar botones";
                else return "Delete all buttons";
            }
        }

        public static string UpdateMessage
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Màj";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Actualizar";
                else return "Update";
            }
        }
        public static string StatusMessage
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "L'annulation (Ctrl+Z) est disponible hors de la fenêtre des jeux d'attributs.";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Deshacer (Ctrl+Z) está disponible fuera de la ventana del editor de atributos.";
                else return "Undo (Ctrl+Z) is available outside the attribute editor window.";
            }
        }
        #endregion dictionnaire

        #region constructors
        public AttributeSetsViewModel()
        {
            ApplyRuleCommand = new RelayCommand(ApplyRule);

            // initialize "inca_logger_sdk" logger
            LoggerUtils.GetInstance().InitializeLogger();
        }
        #endregion Constructors

        #region Functions
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hasSelection"></param>
        /// <param name="selection"></param>
        public void UpdateButtonsVisibility(bool hasSelection, SelectionSet selection = null)
        {
            QueuedTask.Run(() =>
            {
                //on affiche en fonction de la classe des objets.
                if (hasSelection && selection != null)
                {
                    List<string> list_ft = new();
                    // Parcourir les couches sélectionnées
                    foreach (var layer in selection.ToDictionary().Keys)
                    {
                        FeatureClass ft = null;
                        // Récupère le nom de la classe associée
                        if (layer is FeatureLayer fl) ft = fl.GetFeatureClass();
                        else if (layer is AnnotationLayer al) ft = al.GetFeatureClass();
                        if (ft != null) list_ft.Add(ft.GetName());
                    }

                    // Parcours des boutons pour voir si on les affiche
                    if (list_ft.Count != 0)
                    {
                        foreach (var rule in Rules)
                        {
                            if (rule.BtnClassName != "")
                            {
                                // Activer ou désactiver le bouton en fonction de la sélection
                                if (list_ft.Contains(rule.BtnClassName.ToString()))
                                    rule.BtnVisibility = Visibility.Visible;
                                else
                                    rule.BtnVisibility = Visibility.Collapsed;
                            }
                        }
                    }
                }

                // sinon on affiche tout 
                else
                    foreach (var rule in Rules)
                        rule.BtnVisibility = Visibility.Visible;
            });
        }

        /// <summary>
        /// Quand la sélection change, on affiche ou masque les boutons pour que seuls ceux nécessaires soient affichés.
        /// </summary>
        /// <param name="args"></param>
        public void OnSelectionChanged(MapSelectionChangedEventArgs args)
        {
            // Récupérer la sélection       
            var selection = args.Selection;
            bool hasSelection = selection != null && !selection.IsEmpty;

            UpdateButtonsVisibility(hasSelection, selection);
        }

        /// <summary>
        /// Gestion des cas d'action du menu déroulant
        /// </summary>
        /// <param name="action"></param>
        private void ExecuteAction(RuleAction action)
        {
            switch (action)
            {
                case RuleAction.Delete:
                    DeleteRules();
                    break;

                case RuleAction.Import:
                    LoadButton();
                    SelectionSet selection = null;
                    // Récupérer la sélection
                    QueuedTask.Run(() => selection = MapView.Active.Map.GetSelection());
                    bool hasSelection = selection != null && !selection.IsEmpty;
                    UpdateButtonsVisibility(hasSelection, selection);

                    break;

                case RuleAction.Export:
                    ExportRules();
                    break;
            }
        }

        /// <summary>
        /// via le menu déroulant, supprime les boutons de l'affichage 
        /// et supprime le chemin du fichier xml dans les paramètres arcgis
        /// </summary>
        public void DeleteRules()
        {
            Rules.Clear();
            AttributeSetsControls.XMLSelectFileFullPath = "";
            AttributeSetsControls.GetInstance().SaveSettings();
        }

        /// <summary>
        /// via le menu déroulant, exporte les règles versun fichier xml
        /// </summary>
        public void ExportRules()
        {
            AttributeSetsControls.GetInstance().BrowseAndSaveFile(Rules);
        }

        /// <summary>
        /// vérifie le chemin du fichier de paramètres, ou demande le chemin du fichier de paramètres
        /// charge les boutons
        /// </summary>
        /// <returns></returns>
        public bool LoadButton(bool ini=false)
        {
            AttributeSetsControls.BrowseAndCheckFileName(ini);

            var rules = AttributeSetsCore.GetInstance()
                            .LoadAttributeSetsRules()
                            .ToList();

            // retour UI thread
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Rules.Clear();
                foreach (var rule in rules)
                    Rules.Add(rule);
            });

            return true;
        }
        

        /// <summary>
        /// Applique les règles du bouton à(aux) (l')objet(s) sélectionné(s)
        /// 
        /// </summary>
        /// <param name="parameter"></param>
        private void ApplyRule(object parameter)
        {
            if (parameter is not AttributeSetsRule rule) return;
            if (rule == null || rule.BtnName==null || rule.BtnName=="") return;            

            // Nom et dictionnaire d'attributs 
            var nomclasse = rule.BtnClassName;
            var dictattributes = rule.BtnFieldsName_Values;

            Map map = MapView.Active.Map;
            if (map != null)
            {
                QueuedTask.Run(() =>
                {
                    // la sélection
                    var selection = map.GetSelection().ToDictionary();

                    // on prend les couches qui ont le même noms que le nom de la couche de sélection
                    foreach (var _selec in selection)
                    {
                        var ListLayer = map.GetLayersAsFlattenedList().OfType<Layer>().
                        Where(f => f.Name == _selec.Key.Name.Split('/').Last());
                        foreach (Layer layer in ListLayer)
                        {
                            FeatureClass featureClass = null;
                            if (layer is FeatureLayer fl && fl.GetSelection().GetCount() != 0)
                                featureClass = fl.GetFeatureClass();

                            if (layer is AnnotationLayer al && al.GetSelection().GetCount() != 0)
                                featureClass = al.GetFeatureClass();

                            // si la couche a bien une sélection et qu'elle est bien rattachée à la classe que l'on souhaite
                            if (featureClass != null && featureClass.GetName().ToString().Equals(nomclasse))
                            {
                                try
                                {
                                    // Récupère la liste des champs pour pouvoir modifier les valeurs 
                                    var listfield = featureClass.GetDefinition().GetFields().ToList();
                                    var OidField = featureClass.GetDefinition().GetFields().Where(
                                        x => x.FieldType.ToString() == "OID").First().Name;
                                    List<long> oids = _selec.Value;

                                    // on modifie les attributs :                                    
                                    foreach (var oid in oids)
                                    {
                                        // Créer un Inspector
                                        var inspector = new Inspector();
                                        // Charger la ligne dans l'Inspector
                                        inspector.Load(featureClass, oid);

                                        // Créer une nouvelle instance d'EditOperation
                                        var editOperation = new EditOperation();
                                        // En modification avec la classe et l'OID
                                        editOperation.Name = UpdateMessage + layer + " : " + oid;
                                        editOperation.SelectModifiedFeatures = true;

                                        foreach (var attr in dictattributes)
                                        {
                                            var field = listfield.FindIndex(f => f.Name.ToLower() == attr.Key.ToLower());
                                            if (field != -1)
                                            {
                                                inspector[field] = attr.Value;
                                            }
                                        }

                                        // Appliquer les modifications
                                        editOperation.Modify(inspector);

                                        // Exécuter l'opération
                                        editOperation.Execute();
                                        // sauvegarde du projet ?
                                        //await Project.Current.SaveEditsAsync();                                         
                                    }

                                    var tv = TableView.Active;
                                    // met à jour  la table
                                    if (tv != null)
                                        if (tv.CanRefresh)
                                            tv.Refresh();
                                    // met à jour la carte active
                                    MapView.Active.Redraw(true);
                                }

                                catch (Exception e) { MessageBox.Show(e.Message); }
                            }
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Recharge les boutons lors de l'ouverture du panneau 
        /// </summary>
        /// <param name="isVisible"></param>
        protected override void OnShow(bool isVisible)
        {
            if (!isVisible) return;
            QueuedTask.Run(() => {LoadButton(true); });

            // Gestion de la sélection pour adapter l'affichage des boutons
            MapSelectionChangedEvent.Subscribe(OnSelectionChanged);
        }
        #endregion Functions

        /// Ouverture du panneau
        internal static void Show()
        {
            DockPane pane = FrameworkApplication.DockPaneManager.Find(DockPaneId);
            pane?.Activate();
        }

        #region CLASS
        internal sealed class FileNameChangedEventArgs : EventArgs
        {
            internal string _value;
            internal FileNameChangedEventArgs(string value)
            {
                _value = value;
            }
        }
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

            public event EventHandler CanExecuteChanged;
        }


        #endregion CLASS
    }

    /// <summary>
    /// Button d'implementation pour voir le panneau.
    /// </summary>
    internal class AttributeSets_ShowButton : ArcGIS.Desktop.Framework.Contracts.Button
    {
        protected override void OnClick()
        {
            AttributeSetsViewModel.Show();
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
    /// Evènement déclenché par le changement de nom du fichier Excel
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
