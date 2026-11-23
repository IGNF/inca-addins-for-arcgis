using System;
using System.Collections.Generic;
using IGN.Inca.ArcGISPro.SDK.Commons;
using IGN.Inca.ArcGISPro.SDK.DAL.Enum;

namespace IncaEditing._CastFeatures
{
    /// <summary>
    /// Classe décrivant une règle de transtypage d'entités
    /// </summary>
    public class CastFeaturesRule
    {
        public CastFeaturesRule() { }

        #region Parameters

        /// <summary>
        /// Nom de la classe source contenant les entités à transtyper
        /// </summary>
        public string SrcFeatureClass { get; set; }

        /// <summary>
        /// Nom de la classe cible dans laquelle seront enregistrées les entités transtypées
        /// </summary>
        public string TargetFeatureClass { get; set; }

        /// <summary>
        /// Clause SQL utilisée pour filtrer les entités en entrée
        /// </summary>
        public string WhereClause { get; set; }

        /// <summary>
        /// Paramètre indiquant si les entités à transférer doivent être supprimées de la classe source
        /// </summary>
        public bool DeleteFeatures { get; set; }

        /// <summary>
        /// Dictionnaire décrivant les correspondances de transfert entre les champs source et les champs cible 
        /// </summary>
        public Dictionary<string, string> MapFieldsDict { get; set; }

        /// <summary>
        /// Dictionnaire décrivant l'affectation de valeurs prédéfinies à certains champs cible
        /// </summary>
        public Dictionary<string, string> SetFieldValuesDict { get; set; }

        #endregion

        /// <summary>
        /// Initialisation d'une règle de transtypage à partir d'un dictionnaire extrait d'un fichier Excel
        /// </summary>
        /// /// <param name="valuesFromExcel">Dictionnaire de paramètres extraits d'un fichier Excel</param>
        public void CreateRule(Dictionary<string, string> valuesFromExcel)
        {
            ConfigFileUtils dictTools = new ConfigFileUtils(valuesFromExcel);

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //  Colonne SOURCE_FEATURE_CLASS
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            this.SrcFeatureClass = dictTools.GetArgumentValue("SOURCE_FEATURE_CLASS");
                
            // this field is mandatory
            if (string.IsNullOrWhiteSpace(this.SrcFeatureClass)) throw new ArgumentNullException("The mandatory field 'SOURCE_FEATURE_CLASS' is empty");

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //  Colonne TARGET_FEATURE_CLASS
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            this.TargetFeatureClass = dictTools.GetArgumentValue("TARGET_FEATURE_CLASS");

            // this field is mandatory
            if (string.IsNullOrWhiteSpace(this.TargetFeatureClass)) throw new ArgumentNullException("The mandatory field 'TARGET_FEATURE_CLASS' is empty");

            // ~~~~~~~~~~~~~~~~~~~~~~
            //  Colonne WHERE_CLAUSE
            // ~~~~~~~~~~~~~~~~~~~~~~
            this.WhereClause = dictTools.GetArgumentValue("WHERE_CLAUSE");

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //  Colonne DELETE_FEATURES
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            var deleteFeatures = dictTools.GetArgumentValue("DELETE_FEATURES");

            // this field is mandatory
            if (string.IsNullOrWhiteSpace(deleteFeatures))
                throw new ArgumentNullException("The mandatory field 'DELETE_FEATURES' field is empty");

            this.DeleteFeatures = (bool)dictTools.GetConvertedArgument("DELETE_FEATURES", ConvertingUtilTypes.BOOL);

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //  Colonne JSON_MAP_FIELDS
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~
            this.MapFieldsDict = (Dictionary<string, string>)dictTools.GetConvertedArgument("JSON_MAP_FIELDS", ConvertingUtilTypes.DICTIONARY);

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //  Colonne JSON_SET_FIELD_VALUES
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            this.SetFieldValuesDict = (Dictionary<string, string>)dictTools.GetConvertedArgument("JSON_SET_FIELD_VALUES", ConvertingUtilTypes.DICTIONARY);

        }
    }
}