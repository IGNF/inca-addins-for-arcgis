using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Xml;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

namespace IncaDisplay._AttributeSets
{
    /// <summary>
    /// Classe prenant en charge les opérations de transtypage d'entités.
    /// Le transtypage consiste à déplacer d'une classe à une autre des entités selon des règles
    /// définies par l'utilisateur.
    /// </summary>
    public sealed class AttributeSetsCore
    {
        #region Parameters
        /// <summary>
        /// Instance de la classe 
        /// </summary>
        private static AttributeSetsCore _instance;

        #endregion Parameters

        #region Constructors
        /// <summary>
        /// Création ou appel à l'instance commune de la classe
        /// </summary>
        /// <returns>L'instance de la classe</returns>
        public static AttributeSetsCore GetInstance()
        {
            if (_instance == null)
            {
                _instance = new AttributeSetsCore();
            }
            return _instance;
        }
        #endregion Constructors

        #region fonctions
        /// <summary>
        /// Chargement des règles des boutons depuis un fichier xml déjà vérifié.
        /// </summary>
        /// <param name="xmlFilePath"> chemin du fichier de paramètres XML décrivant les règles</param>
        /// <returns>liste de règles</returns>
        public List<AttributeSetsRule> LoadAttributeSetsRules()
        {
            // récupération du fichier xml stocké dans les paramètre Arcgis
            string xmlFilePath = Properties.Settings.Default.AttributeSetsConfigFile;
                        
            List<AttributeSetsRule> AS_Rules = new List<AttributeSetsRule>();
            try
            {
                var rules = new List<AttributeSetsRule>();
                // Lecture du fichier Excel en le chargeant dans une instance de XmlDocument 
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(xmlFilePath);
                
                // Récupération des éléments button du fichier XML
                var buttonList = xmlDoc.GetElementsByTagName("button");
                
                // Pour chaque bouton, lecture des paramètres
                foreach (XmlNode button in buttonList)
                {
                    // Accéder aux informations des éléments
                    // on récupère les infos  pour la création du bouton
                    AttributeSetsRule rule = new();
                    rule.CreateRule(button);
                    AS_Rules.Add(rule);
                }
            }

            catch (Exception ex)
            {               
                MessageBox.Show(ex.Message, "Warning", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            } 
            
            return AS_Rules; ;

        }


        /// <summary>
        /// Enregistrement d'un fichier XML avec les boutons et paramètres de la table des jeux d'attributs.
        /// </summary>
        /// <param name="excelConfigFileURI"></param>
        /// <param name="rules"></param>
        /// <returns></returns>
        public bool SaveAttributeSetsRules(string excelConfigFileURI, List<AttributeSetsRule> rules)
        {
            try
            {
                // Enregistrement du fichier
                return WriteExcelWorksheet(excelConfigFileURI, rules);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Warning", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Définition de la méthode d'écriture du xml en focntions des règles
        /// </summary>
        /// <param name="existingFileURI"></param>
        /// <param name="rules"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public bool WriteExcelWorksheet(string existingFileURI, List<AttributeSetsRule> rules)
        {
            // tests d'existence
            if (rules is null)
                throw new ArgumentNullException(nameof(rules));
           
            try
            {
                var existingFile = new FileInfo(existingFileURI);

                // Création d'un XmlWriterSettings pour formater le XML
                XmlWriterSettings settings = new XmlWriterSettings
                {
                    Indent = true,
                    NewLineOnAttributes = true
                };

                // On utilise XmlWriter pour écrire dans le fichier XML
                using (XmlWriter writer = XmlWriter.Create(existingFileURI, settings))
                {
                    writer.WriteStartDocument();

                    #region buttons
                    // ajout de chaque bouton
                    writer.WriteStartElement("buttons");

                    foreach (var kvp in rules)
                    {                        
                        #region button
                        writer.WriteStartElement("button");
                        writer.WriteAttributeString("label", kvp.BtnName);
                        writer.WriteAttributeString("class", kvp.BtnClassName);
                        writer.WriteAttributeString("icon", kvp.BtnIconName);

                        #region field-values
                        writer.WriteStartElement("field-values");
                        if (kvp.BtnFieldsName_Values != null)
                        {    
                            foreach (var rule in kvp.BtnFieldsName_Values)
                            {
                                writer.WriteStartElement("field");
                                writer.WriteAttributeString("name", rule.Key);
                                writer.WriteAttributeString("has-value", "true");
                                writer.WriteString(rule.Value);
                                writer.WriteEndElement();
                            }
                            writer.WriteEndElement(); // "field-values"
                        }
                        #endregion field-values
                        writer.WriteEndElement(); // "button"
                        #endregion button
                    }
                    writer.WriteEndElement();
                    #endregion buttons

                    writer.WriteEndDocument();
                }

                MessageBox.Show("Données enregistrées dans le fichier XML.");
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Problem reading Excel config file. Message error: " + ex.Message, ex);
            }
        }
        #endregion Fonctions
    }

}
