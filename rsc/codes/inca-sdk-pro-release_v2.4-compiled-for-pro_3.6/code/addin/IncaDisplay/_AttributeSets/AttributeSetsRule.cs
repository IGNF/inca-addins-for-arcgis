using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Xml;
using static IncaDisplay._AttributeSets.AttributeSetsViewModel;

namespace IncaDisplay._AttributeSets
{
    public class AttributeSetsRule : INotifyPropertyChanged
    {
        #region Constructors
        public AttributeSetsRule() { }
        #endregion Constructors

        #region Parameters
        // Attributs des boutons : 
        // pour gérer l'option d'affichage des boutons dans le panneau en fonction de la sélection
        private Visibility _btnVisibility = Visibility.Visible;
        public Visibility BtnVisibility
        {
            get => _btnVisibility;
            set
            {
                _btnVisibility = value;
                OnPropertyChanged();
            }
        }

        //le nom du bouton
        public string BtnName { get; set; }

        // nom de la classe associée
        public string BtnClassName { get; set; }

        // chemin de l'image référence
        private string _BtnIconPath;
        public string BtnIconPath
        {
            get => _BtnIconPath;
            set { _BtnIconPath = value; OnPropertyChanged(); OnPropertyChanged(nameof(BtnImageText)); }
        }

        // nom de l'image référence
        private string _BtnIconName;
        public string BtnIconName
        {
            get => _BtnIconName;
            set { _BtnIconName = value; OnPropertyChanged(); OnPropertyChanged(nameof(BtnImageText)); }
        }

        // chemin et nom de l'image pour afficher l'icone
        public string BtnImageText
        {
            get
            {
                if (string.IsNullOrWhiteSpace(BtnIconPath) ||
                    string.IsNullOrWhiteSpace(BtnIconName))
                    return null;

                return Path.Combine(BtnIconPath, BtnIconName)
                           .Replace("file:\\", "");
            }
        }

        // sauvegarde des données initiales des icones pour pouvoir afficher les 2 dans le panneau d'édition
        public string InitialIconPath { get; set; }
        public string InitialIconName { get; set; }
        public string InitialImageText =>
            Path.Combine(InitialIconPath, InitialIconName);

        // dictionnaire des attributs et valeurs d'attributs associés au bouton
        public Dictionary<string, string> BtnFieldsName_Values { get; set; }
        #endregion parameters  

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
           => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        #region Fonctions
        /// <summary>
        /// Mise à jour des informations d'un bouton à partir d'une source. 
        /// </summary>
        /// <param name="source"></param>
        public AttributeSetsRule UpdateFrom(AttributeSetsRule source)
        {
            if (source is null || source.BtnClassName == "" || source.BtnClassName is null) return null;

            // copie des attributs
            BtnName = source.BtnName;
            BtnClassName = source.BtnClassName;
            BtnIconName = source.BtnIconName;
            BtnIconPath = source.BtnIconPath;
            BtnFieldsName_Values = source.BtnFieldsName_Values.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            return this;
        }

        /// <summary>
        /// Clone d'un bouton
        /// </summary>
        /// <returns></returns>
        public AttributeSetsRule Clone()
        {
            return UpdateFrom(this);
        }
        #endregion Fonctions


        /// <summary>
        /// Initialisation d'une règle de sélection/édition à partir d'un dictionnaire extrait d'un fichier Excel
        /// </summary>
        /// 
        public void CreateRule(XmlNode button)
        {
            BtnName = button.Attributes["label"]?.Value;
            BtnClassName = button.Attributes["class"]?.Value;

            BtnIconName = button.Attributes["icon"]?.Value;
            BtnIconPath = Path.GetDirectoryName(button.BaseURI);

            var fieldValuesNode = button.SelectSingleNode(".//field-values");
            if (fieldValuesNode != null)
            {
                this.BtnFieldsName_Values = new Dictionary<string, string>();
                // Parcourir tous les éléments <field> dans <field-values>
                foreach (XmlNode fieldNode in fieldValuesNode.SelectNodes(".//field"))
                {
                    // Récupérer les attributs 'name' et 'has-value' des éléments <field>
                    string fieldName = fieldNode.Attributes["name"]?.Value;
                    // Récupérer la valeur de l'élément <field>
                    string fieldValue = fieldNode.InnerText;
                    this.BtnFieldsName_Values.Add(fieldName, fieldValue);
                }
            }
        }
    }

    /// <summary>
    /// Pour la mise à jour du dictionnaire d'attributs du bouton
    /// </summary>
    public class FieldValueItem : INotifyPropertyChanged
    {
        private string _key;
        private string _value;

        public string Key
        {
            get => _key;
            set { _key = value; OnPropertyChanged(); }
        }

        public string Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }


    /// <summary>
    /// Pour la gestion des icones et leur affichage dans la fenêtre d'édition d'un bouton
    /// </summary>
    public class IconItem
    {
        public string ImagePath { get; set; }
    }

    public class ActionItem
    {
        public RuleAction Action { get; }
        public string DisplayName { get; }

        public ActionItem(RuleAction action, string displayName)
        {
            Action = action;
            DisplayName = displayName;
        }
    }

    /// <summary>
    /// liste des possibles dans le menu déroulant du menu principal
    /// </summary>
    public enum RuleAction
    {
        Delete,
        Import,
        Export,
    }
}