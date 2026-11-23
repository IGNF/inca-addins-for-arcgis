using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data.Mapping;
using ArcGIS.Core.Geometry;
using IGN.Inca.ArcGISPro.SDK.DAL.Enum;

/// <summary>
/// Namespace qui contient l'ensemble d'objets définissant le modèle de données du projet INCA - ArcGIS Pro SDK
/// </summary>
namespace IGN.Inca.ArcGISPro.SDK.DAL
{
    /// <summary>
    /// Classe basée sur le modèle de données d'une Annotation Feature Class mais adaptée 
    /// aux besoins du projet pour lire et exporter les annotations dans une Feature Class
    /// normale. Cette classe contient les champs obligatoires afin de pouvoir exporter une
    /// Annotation, notamment : l'object id, la géométrie, l'écriture du toponyme et l'angle
    /// 
    /// Note : Le character questionmark (?) signifie que l'attribut est Nullable.
    /// </summary>
    public class AnnotationBO : ICloneable
    {
        public AnnotationBO()
        { }

        public AnnotationBO(AnnotationFeature annoFeature)
        {
            //Get the graphic from the anno feature
            var graphic = annoFeature.GetGraphic();
            var textGraphic = graphic as CIMTextGraphic;

            // ############## GET THE BASELINE ###############

            // get the cimTextGraphic baseline geometry (Geometry type)
            // and put it into the AnnotationBO SHAPE field instance
            this.Shape = textGraphic.Shape;

            // ############## GET THE ATTRIBUTES ###############

            this.ObjectID = annoFeature.GetObjectID();

            var textString = annoFeature["TEXTSTRING"];
            this.TextString = (string)textString;

            var angle = annoFeature["ANGLE"];
            this.Angle = (double?)angle;

            // define the toponym type: DROIT or COURBE
            this.DefineToponymTypeBasedOnTheGeometry();
        }

        // Properties :
        public long ObjectID { get; set; }
        public string TextString { get; set; }
        public double? Angle { get; set; }
        public Geometry Shape { get; set; }

        // this property helps to identify if the toponym is "Droit" or "Courbe"
        public ToponymType ToponymType { get; set; }
        
        /// <summary>
        /// Méthode qui détermine si ce toponyme est "droit" ou "courbe" à partir de la géométrie
        /// </summary>
        public void DefineToponymTypeBasedOnTheGeometry()
        {
            // Define the toponym type property
            if (this.Shape is Polyline polyline)
            {
                if (polyline.PartCount == 1 && polyline.PointCount == 2 && !polyline.HasCurves)
                    this.ToponymType = ToponymType.STRAIGHT;
                else
                    this.ToponymType = ToponymType.CURVED;
                //else if (polyline.PartCount > 1 || polyline.PointCount > 2)
                //    this.ToponymType = ToponymType.CURVED;
            }
            else if (this.Shape is GeometryBag geomBag || this.Shape is Multipoint)
            {
                this.ToponymType = ToponymType.CURVED;
            }
            else
                this.ToponymType = ToponymType.UNKNOWN;
        }

        /// <summary>
        /// Méthode qui vérifie si une Annotation possède une Baseline ou pas
        /// </summary>
        /// <param name="annoFeature">Instance de type AnnotationFeature à vérifier la Baseline</param>
        /// <returns>True si la Baseline est NULL, False si l'annotation possède une Baseline</returns>
        public bool IsBaselineNull(AnnotationFeature annoFeature)
        {
            // Get the graphic from the anno feature
            var graphic = annoFeature.GetGraphic();
            var textGraphic = graphic as CIMTextGraphic;

            return textGraphic is null;
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
