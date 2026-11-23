using ArcGIS.Core.Data;
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
    /// Classe qui sert à assembler les différentes informations relatives à un Dataset, 
    /// on utilise cette classe notamment pour encapsuler des informations importantes
    /// sur une Feature Class
    /// </summary>
    public class DatasetBO
    {
        public DatasetBO()
        {
            IsAnnotation = false;
            IsValid = false;
        }

        public string GeodatabasePath { get; set; }
        public string DatasetName { get; set; }
        public DatasetType DatasetType { get; set; }
        public bool IsAnnotation { get; set; }
        public bool IsValid { get; set; }
    }
}
