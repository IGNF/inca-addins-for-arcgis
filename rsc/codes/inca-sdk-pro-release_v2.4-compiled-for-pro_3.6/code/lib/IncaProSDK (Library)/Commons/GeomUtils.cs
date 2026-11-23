using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;

/// <summary>
/// Namespace qui contient l'ensemble d'outils communs et génériques 
/// dans le projet INCA - ArcGIS Pro SDK
/// </summary>
namespace IGN.Inca.ArcGISPro.SDK.Commons
{
    /// <summary>
    /// Classe de support contenant quelques calculs géométriques génériques
    /// </summary>
    class GeomUtils
    {
        const double Rad2Deg = 180.0 / Math.PI;
        const double Deg2Rad = Math.PI / 180.0;

        public static double ComputeAngle(MapPoint start, MapPoint end)
        {
            return Math.Atan2(end.Y - start.Y, end.X - start.X) * Rad2Deg;
        }

        public static MapPoint ComputeMiddlePoint(MapPoint point1, MapPoint point2)
        {
            return new MapPointBuilderEx((point1.X + point2.X) / 2, (point1.Y + point2.Y) / 2, point1.SpatialReference).ToGeometry();
        }
    }
}
