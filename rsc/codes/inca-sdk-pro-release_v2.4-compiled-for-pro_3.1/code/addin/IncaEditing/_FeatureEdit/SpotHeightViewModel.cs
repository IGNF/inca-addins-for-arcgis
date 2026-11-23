//   Copyright 2019 Esri
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at


using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Resources;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Controls;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using CodedValueDomain = ArcGIS.Core.Data.CodedValueDomain;
using Field = ArcGIS.Core.Data.Field;
using System.Threading;

namespace IncaEditing._FeatureEdit
{

    /// <summary>
    /// Classe permettant de gérer la classe et la côté du point côté.
    /// </summary>
    internal class SpotHeightViewModel : EmbeddableControl, INotifyPropertyChanged
    {
        public SpotHeightViewModel(XElement options, bool canChangeOptions) : base(options, canChangeOptions)
        {
            // Dictionnaire de la langue utilisée dans l'IHM ("CurrentUICulture")
            Translator = CultureResourcesManager.GetInstance().dictLanguage;
        }

        #region Dictionnaire
        public static string noFC_message
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return " n'existe pas dans la gdb";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return " no existe en la gdb";
                else return " doesn't exist in the gdb";
            }
        }
        #endregion Dictionnaire

        #region Propriétés
        /// <summary>
        /// Traducteur de langue
        /// </summary>
        private ResourceSet Translator { get; set; }

        /// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ START ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        ///     One-Way Data Binding Properties, used specially for internationalization of the UI
        /// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        public string Altitude { get { return Translator.GetString("Altitude"); } }
        /// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ END ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        #endregion

        #region Commande    
        private string _selectSymbo = "POINT_COTE_RESEAU";
        public string SelectSymbo
        {
            get { return _selectSymbo; }
            set
            {
                _selectSymbo = value;
                NotifyPropertyChanged(new PropertyChangedEventArgs("SelectSymbo"));
            }
        }
        private int _SHeight = 100;
        public int SHeight
        {
            get { return _SHeight; }
            set
            {
                _SHeight = value;
                NotifyPropertyChanged(new PropertyChangedEventArgs("SHeight"));
                if (SelectSymbo == "") SelectSymbo = "POINT_COTE_RESEAU";
            }
        }

        private ObservableCollection<String> _shdomain;
        public ObservableCollection<String> SHDomain
        {
            get { return _shdomain; }
            set
            {
                _shdomain = value;
                NotifyPropertyChanged(new PropertyChangedEventArgs("SHDomain"));
            }
        }

        /// <summary>
        /// Remplir le menu déroulant des valeurs de symbo aec les valeurs du domaine.
        /// </summary>
        private ICommand _CmdFillWithSymbo;
        public ICommand CmdFillWithSymbo
        {
            get
            {
                return _CmdFillWithSymbo ?? (_CmdFillWithSymbo = new RelayCommand(() =>
                {
                    Field fld_symbo = null;
                    // Nom de la classe des points côtés.
                    string featureClassName = "o_point_cote_n1";
                    Map map = MapView.Active.Map;
                    if (map != null)
                    {
                        QueuedTask.Run(() =>
                        {
                            try
                            {
                                //Recherche de la couche avec la bonne classe associée.
                                List<FeatureLayer> blockedLayers_list = new List<FeatureLayer>();       
                                foreach (var l in MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>())
                                {
                                    if (l.GetFeatureClass() is null)
                                        blockedLayers_list.Add(l);
                                }
                                List<FeatureLayer> LayersOfMap = map.GetLayersAsFlattenedList().OfType<FeatureLayer>().ToList();
                                List<FeatureLayer> LayersOfMapPossible = LayersOfMap.Except(blockedLayers_list).ToList();
                                FeatureLayer FC = LayersOfMap.Except(blockedLayers_list).ToList().FirstOrDefault(f => f.GetFeatureClass().GetName().ToLower() == featureClassName);

                                // On recherche tous les attributs, et si l'attribut SYMBO existe, on le met de côté.
                                if (FC is null )
                                {
                                    MessageBox.Show(featureClassName + noFC_message);
                                    return;
                                }
                                
                                List<Field> fields_list = FC.GetFeatureClass().GetDefinition().GetFields().ToList();
                                foreach (var fl in fields_list)
                                {
                                    switch (fl.Name.ToLowerInvariant())
                                    {
                                        case "symbo":
                                            fld_symbo = fl;
                                            break;
                                    }
                                }

                                // On récupère le domaine de cet attribut, et on remplit la liste déroulante.
                                if (fld_symbo != null)
                                {
                                    var domain = fld_symbo.GetDomain() as CodedValueDomain;
                                    if (domain != null)
                                    {
                                        SHDomain = new ObservableCollection<string>();
                                        // Ajouter les valeurs de domaine à la liste
                                        foreach (var codedValue in domain.GetCodedValuePairs())
                                            SHDomain.Add(codedValue.Value);
                                    }
                                }

                                // choix du premier élement par défaut.
                                if (SHDomain.Count > 0)
                                    SelectSymbo = SHDomain[0];
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.ToString());
                            }
                        });
                    }
                }));
            }
        }
        #endregion
    }
}
