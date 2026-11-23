using ArcGIS.Core.Data;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Editing.Attributes;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using Field = ArcGIS.Core.Data.Field;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

namespace IncaEditing._FeatureEdit
{
    /// <summary>
    /// Bouton permettant 
    /// </summary>
    internal class Attribute25K_Button : Button
    {
        protected override void OnClick()
        {
            if (MapView.Active == null)
                return;

            // pour une sélection, sur une carte, switch la valeur (True/False) de l'attribut VISU_25k
            if (MapView.Active.Map != null)
                Attribute.Go("VISU_25");
        }
    }
    internal class Attribute100K_Button : Button
    {
        protected override void OnClick()
        {
            if (MapView.Active == null)
                return;

            // pour une sélection, sur une carte, switch la valeur (True/False) de l'attribut VISU_100k
            if (MapView.Active.Map != null)
                Attribute.Go("VISU_100"); // valeur de l'attribut à modifier en fonction de la chaine
        }
    }

    internal class Attribute
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected Attribute() { }

        #region MESSAGES
        public static string STOPEdition
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Une session d'édition est en cours.\nVoulez vous enregistrer les modifications ? ";
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Se está llevando a cabo una sesión de edición.\n¿Quieres guardar los cambios? ";
                else return "An editing session is underway.\nDo you want to save the changes? ";
            }
        }
        public static string SAVEEdition
        {
            get
            {
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("fr")) return "Enregistrer les modifications : ";
                if (Thread.CurrentThread.CurrentUICulture.Name.ToString().Contains("es")) return "Guardar ediciones : ";
                else return "Save edits : ";
            }
        }
        #endregion

        #region Fonctions utiles
        /// <summary>
        /// inverse la valeur du champ définit par l'indice
        /// met à jour la table et la carte.
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="indice"></param>
        /// <param name="selec"></param>
        private async static void FillAttributeChoise(Layer layer, int indice, KeyValuePair<MapMember, List<long>> selec)
        {
            // liste des oid à modiifer
            List<long> oids = selec.Value;
            // récupère la table associée à la couche
            Table table = null;
            if (layer is FeatureLayer FL) table = FL.GetTable();
            else if (layer is AnnotationLayer AL) table = AL.GetTable();
            else return;

            if (table == null) return;

            // on modifie les attributs :                                    
            foreach (var oid in oids)
            {
                // Créer une nouvelle instance d'édition (pour que la modification soit géréelors de la réintégration)
                var editOperation = new EditOperation();
                // message dans les annulations
                editOperation.Name = "VISU25K " + layer + " : " + oid;
                // Créer un Inspector
                var inspector = new Inspector();
                // Charger la ligne dans l'Inspector
                await QueuedTask.Run(() =>
                {
                    // Remplacez objectID par l'ID de l'objet à mettre à jour
                    inspector.Load(table, oid);
                    if (indice != -1)
                    {
                        // Lire la valeur actuelle du champ booléen
                        short value = SafeToShort(inspector[indice]);
                        // inverse la valeur : 0->1 , 1->0
                        inspector[indice] = Math.Abs(value - 1);

                        // Appliquer les modifications                                                       
                        editOperation.Modify(inspector);
                    }
                });

                // Exécuter l'opération
                await editOperation.ExecuteAsync();
            }

            var tv = TableView.Active;
            // met à jour  la table
            if (tv != null)
                if (tv.CanRefresh)
                    tv.Refresh();
            // met à jour la carte active
            await QueuedTask.Run(() => { MapView.Active.Redraw(true); });
        }        


        public static short SafeToShort(object value, short defaultValue = 1)
        {
            return value == DBNull.Value || value == null ? defaultValue : Convert.ToInt16(value);
        }
        #endregion

        internal async static void Go(string NomAttribut)
        {
            await QueuedTask.Run(async () =>
            {
                // bloquer l'outil s'il y a une session d'édition en cours.
                if (Project.Current.HasEdits)
                {
                    var res = MessageBox.Show(STOPEdition, SAVEEdition, MessageBoxButton.YesNoCancel);
                    if (res == MessageBoxResult.Cancel) return;
                    else if (res == MessageBoxResult.No) await Project.Current.DiscardEditsAsync();
                    else await Project.Current.SaveEditsAsync();
                }

                Map map = MapView.Active.Map;
                Dictionary<MapMember, List<long>> selection = new Dictionary<MapMember, List<long>>();

                // la selection de la carte en cours
                await QueuedTask.Run(() => { selection = map.GetSelection().ToDictionary(); });

                // pour chaque sélection, on trouve la couche, les champs de la classe associée
                // et on valide que le champ NomAttribut est bien présent
                foreach (var selec in selection)
                {
                    Layer layer = map.GetLayersAsFlattenedList().OfType<Layer>().First(f => f.Name.ToString() == selec.Key.Name.Split('/').Last());
                    if (layer is FeatureLayer fl)
                    {
                        List<Field> fields_list = fl.GetFeatureClass().GetDefinition().GetFields().ToList();
                        int idxVisu = fl.GetTable().GetDefinition().FindField(NomAttribut);
                        if (idxVisu != -1) FillAttributeChoise(fl, idxVisu, selec);
                    }
                    else if (layer is AnnotationLayer al)
                    {
                        // vérifie qu'il n'y a pas de champ NomAttribut
                        List<Field> fields_list = al.GetFeatureClass().GetDefinition().GetFields().ToList();
                        int idxVisu = al.GetTable().GetDefinition().FindField(NomAttribut);
                        if (idxVisu != -1) FillAttributeChoise(al, idxVisu, selec);
                    }
                }
                map.ClearSelection();
            });
        }
    }
}

