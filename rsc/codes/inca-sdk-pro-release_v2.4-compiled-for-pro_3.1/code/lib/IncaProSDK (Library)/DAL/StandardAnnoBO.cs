using ArcGIS.Core.CIM;
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
    /// Classe basée sur le modèle de données d'une Annotation Feature Class mais adaptée 
    /// aux besoins du projet pour lire et exporter les annotations dans une Feature Class
    /// normale. Cette classe contient les champs restants du modèle de données classique
    /// d'une Annotation Feature Class
    /// 
    /// Cette classe hérite de la classe AnnotationBO
    /// 
    /// Note : Le character questionmark (?) signifie que l'attribut est Nullable.
    /// </summary>
    public class StandardAnnoBO : AnnotationBO
    {
        public StandardAnnoBO()
        { }

        /// <summary>
        /// Call AnnotationBO constructor before
        /// </summary>
        /// <param name="annoFeature">The Annotation Feature to export</param>
        public StandardAnnoBO(AnnotationFeature annoFeature) : base(annoFeature)
        {
            this.SetValuesFromAnnotationFeature(annoFeature);
        }

        public long? FeatureID { get; set; }
        public long? ZOrder { get; set; }
        public long? AnnotationClassID { get; set; }
        public byte[] Element { get; set; }
        public long? SymbolID { get; set; }
        public short? Status { get; set; }

        public string FontName { get; set; }
        public string FontStyle { get; set; }
        public double? FontSize { get; set; }
        public bool? Underline { get; set; }
        public VerticalAlignment? VerticalAlignment { get; set; }
        public HorizontalAlignment? HorizontalAlignment { get; set; }
        public double? XOffset { get; set; }
        public double? YOffset { get; set; }
        
        public double? FontLeading { get; set; }
        public double? WordSpacing { get; set; }
        public double? CharacterWidth { get; set; }
        public double? CharacterSpacing { get; set; }
        public double? FlipAngle { get; set; }
        public long? Override { get; set; }

        public void SetValuesFromAnnotationFeature(AnnotationFeature annoFeature)
        {
            var featureID = annoFeature["FEATUREID"];
            this.FeatureID = (int?)featureID;

            var zOrder = annoFeature["ZORDER"];
            this.ZOrder = (int?)zOrder;

            var annotationClassID = annoFeature["ANNOTATIONCLASSID"];
            this.AnnotationClassID = (int?)annotationClassID;

            var element = annoFeature["ELEMENT"];
            this.Element = ((System.IO.MemoryStream)element).ToArray();

            var symbolID = annoFeature["SYMBOLID"];
            this.SymbolID = (int?)symbolID;

            var status = annoFeature["STATUS"];
            this.Status = (short?)status;

            var fontName = annoFeature["FONTNAME"];
            this.FontName = (string)fontName;

            var fontStyle = annoFeature["FONTSTYLE"];
            this.FontStyle = (string)fontStyle;

            var fontSize = annoFeature["FONTSIZE"];
            this.FontSize = (double?)fontSize;

            var underline = annoFeature["UNDERLINE"];
            this.Underline = Convert.ToBoolean(underline);

            var verticalAlignment = annoFeature["VERTICALALIGNMENT"];
            this.VerticalAlignment = (ArcGIS.Core.CIM.VerticalAlignment?)(short?)verticalAlignment;

            var horizontalAlignment = annoFeature["HORIZONTALALIGNMENT"];
            this.HorizontalAlignment = (ArcGIS.Core.CIM.HorizontalAlignment?)(short?)horizontalAlignment;

            var xOffset = annoFeature["XOFFSET"];
            this.XOffset = (double?)xOffset;

            var yOffset = annoFeature["YOFFSET"];
            this.YOffset = (double?)yOffset;

            var fontLeading = annoFeature["FONTLEADING"];
            this.FontLeading = (double?)fontLeading;

            var wordSpacing = annoFeature["WORDSPACING"];
            this.WordSpacing = (double?)wordSpacing;

            var characterWidth = annoFeature["CHARACTERWIDTH"];
            this.CharacterWidth = (double?)characterWidth;

            var characterSpacing = annoFeature["CHARACTERSPACING"];
            this.CharacterSpacing = (double?)characterSpacing;

            var flipAngle = annoFeature["FLIPANGLE"];
            this.FlipAngle = (double?)flipAngle;

            var override_ = annoFeature["OVERRIDE"];
            this.Override = (int?)override_;
        }
    }
}
