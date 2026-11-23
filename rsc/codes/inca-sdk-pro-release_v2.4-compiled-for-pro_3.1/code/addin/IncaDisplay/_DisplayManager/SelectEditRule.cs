using System;
using System.Collections.Generic;
using IGN.Inca.ArcGISPro.SDK.Commons;
using System.Threading;

namespace IncaDisplay._DisplayManager
{
    /// <summary>
    /// Classe décrivant une règle de transtypage d'entités
    /// </summary>
    public class SelectEditRule
    {
        public SelectEditRule() { }

        #region Parameters
        /// <summary>
        /// Nom de la carte pour les paramétrages
        /// </summary>
        public string SrcMapName { get; set; }

        /// <summary>
        /// Nom de la régle pour les paramétrages
        /// </summary>
        public string SrcRuleName { get; set; }

        /// <summary>
        /// Nom du dossier de couche pour les paramétrages
        /// </summary>
        public string SrcGroupLayer { get; set; }

        /// <summary>
        /// Nom du dossier des mosaïques pour les paramétrages
        /// </summary>
        public string SrcMosaicLayer { get; set; }

        /// <summary>
        /// Nom du l'image des rasters pour les paramétrages
        /// </summary>
        public string SrcImageLayer { get; set; }

        /// <summary>
        /// Nom de la couche pour les paramétrages
        /// </summary>
        public string SrcLayer { get; set; }

        /// <summary>
        /// champ pour l'affichage 
        /// </summary>
        public bool SrcDisplay { get; set; }

        /// <summary>
        /// champ pour la sélectionnabilité 
        /// </summary>
        public bool SrcSelectable { get; set; }

        /// <summary>
        /// champ pour l'édition
        /// </summary>
        public bool SrcEditable { get; set; }

        #endregion

        #region Dictionnaire
        public static string map_ALIAS
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "CARTE";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "MAPA ";
                else return "MAP";
            }
        }
        public static string name_ALIAS
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "NOM";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "NOMBRE";
                else return "NAME";
            }
        }
        public static string folder_ALIAS
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "DOSSIER";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "ARCHIVO";
                else return "FOLDER";
            }
        }
        public static string layer_ALIAS
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "COUCHE";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "CAPA";
                else return "LAYER";
            }
        }
        public static string display_ALIAS
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "AFFICHAGE";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "PANTALLA";
                else return "DISPLAY";
            }
        }
        public static string selection_ALIAS
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "SELECTION";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "SELECCION";
                else return "SELECTION";
            }
        }
        public static string edition_ALIAS
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "EDITION";
                else if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "EDICION";
                else return "EDITION";
            }
        }
        
        #endregion

        /// <summary>
        /// Initialisation d'une règle de sélection/édition à partir d'un dictionnaire extrait d'un fichier Excel
        /// </summary>
        /// /// <param name="valuesFromExcel">Dictionnaire de paramètres extraits d'un fichier Excel</param>
        public void CreateRule(Dictionary<string, string> valuesFromExcel)
        {
            ConfigFileUtils dictTools = new ConfigFileUtils(valuesFromExcel);

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //  Colonne CARTE
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            SrcMapName = dictTools.GetArgumentValue(map_ALIAS);
            // this field is mandatory
            if (string.IsNullOrWhiteSpace(SrcMapName)) throw new ArgumentNullException("The mandatory field "+ map_ALIAS  + " is empty");

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //  Colonne DOSSIER
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            SrcGroupLayer = dictTools.GetArgumentValue(folder_ALIAS);
            SrcMosaicLayer = dictTools.GetArgumentValue(folder_ALIAS);

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //  Colonne COUCHE
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            SrcLayer = dictTools.GetArgumentValue(layer_ALIAS);
            // this field is mandatory
            if (string.IsNullOrWhiteSpace(SrcGroupLayer))
                if (string.IsNullOrWhiteSpace(SrcLayer))
                    throw new ArgumentNullException("The mandatory field "+ layer_ALIAS  + " is empty"); 

            // ~~~~~~~~~~~~~~~~~~~~~~
            //  Colonne NOM
            // ~~~~~~~~~~~~~~~~~~~~~~
            SrcRuleName = dictTools.GetArgumentValue(name_ALIAS);
            // this field is mandatory
            if (string.IsNullOrWhiteSpace(SrcRuleName)) throw new ArgumentNullException("The mandatory field " + name_ALIAS + " is empty");

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //  Colonne AFFICHAGE
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~            
            if (dictTools.GetArgumentValue(display_ALIAS) != null)
            {
                if (dictTools.GetArgumentValue(display_ALIAS) == "True" || dictTools.GetArgumentValue(display_ALIAS).ToUpperInvariant() == "OUI" 
                    || dictTools.GetArgumentValue(display_ALIAS).ToUpperInvariant() == "VRAI")
                    SrcDisplay = true;
            }
            else
                SrcDisplay = false;

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //  Colonne SELECTION
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~            
            if (dictTools.GetArgumentValue(selection_ALIAS) != null)
            {
                if (dictTools.GetArgumentValue(selection_ALIAS) == "True" || dictTools.GetArgumentValue(selection_ALIAS).ToUpperInvariant() == "OUI" 
                    || dictTools.GetArgumentValue(selection_ALIAS).ToUpperInvariant() == "VRAI")
                    SrcSelectable = true;
            }
            else
                SrcSelectable = false;

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //  Colonne EDITION
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            if (dictTools.GetArgumentValue(edition_ALIAS) != null)
            {
                if (dictTools.GetArgumentValue(edition_ALIAS) == "True" || dictTools.GetArgumentValue(edition_ALIAS).ToUpperInvariant() == "OUI" 
                    || dictTools.GetArgumentValue(edition_ALIAS).ToUpperInvariant() == "VRAI")
                    SrcEditable = true;
            }
            else
                SrcEditable = false;
        }
    }
}