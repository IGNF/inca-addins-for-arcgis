using ArcGIS.Core.Data.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Namespace qui contient l'ensemble d'objets définissant le modèle de données du projet INCA - ArcGIS Pro SDK
/// </summary>
namespace IGN.Inca.ArcGISPro.SDK.DAL
{
    /// <summary>
    /// Classe qui gère un modèle de données customisé pour l'export d'annotations
    /// 
    /// Cette classe hérite de la classe AnnotationBO
    /// </summary>
    public class CustomAnnoBO : StandardAnnoBO
    {
        /// <summary>
        /// Call AnnotationBO constructor before
        /// </summary>
        /// <param name="annoFeature">The Annotation Feature to export</param>
        /// <param name="columnFilter">The fields to export</param>
        public CustomAnnoBO(AnnotationFeature annoFeature, List<string> columnFilter) : base(annoFeature)
        {
            FilteredFields = new Dictionary<string, object>();
            this.SetValuesFromColumnFilter(annoFeature, columnFilter);
        }

        // all the fields to export
        public Dictionary<String, object> FilteredFields { get; set; }

        /// <summary>
        /// Method that reads all the specified columns and stocks them in a dictionary
        /// </summary>
        /// <param name="annoFeature">An annotation feature to export</param>
        /// <param name="columnFilter">The columns/fields to export</param>
        void SetValuesFromColumnFilter(AnnotationFeature annoFeature, List<string> columnFilter)
        {
            foreach (var field in columnFilter)
            {
                var newValueField = annoFeature[field];
                this.FilteredFields.Add(field, newValueField);
            }
        }
    }
}
