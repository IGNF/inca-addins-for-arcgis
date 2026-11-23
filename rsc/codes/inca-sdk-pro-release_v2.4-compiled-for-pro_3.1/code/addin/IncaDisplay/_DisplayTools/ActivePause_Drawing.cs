using System.Collections.Generic;
using System.Linq;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace IncaDisplay._DisplayTools
{
    /// <summary>
    /// Bouton permettant d'activer toutes les cartes en même temps
    /// </summary>
    internal class ActiveDrawing_Button : Button
    {
        protected override void OnClick()
        {
            ActiveDrawing.Go();
        }
    }

    internal class ActiveDrawing
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected ActiveDrawing() { }

        internal async static void Go()
        {
            await QueuedTask.Run(() =>
            {
                // récupère toutes les cartes ouvertes
                var mapViews = new List<MapView>();
                foreach (var mp in ArcGIS.Desktop.Framework.FrameworkApplication.Panes.OfType<IMapPane>())
                    mapViews.Add(mp.MapView);

                // modifie pour toutes les cartes le parametrage de recalcul.
                foreach (var mv in mapViews)
                    if (mv != null) mv.DrawingPaused = false;
            });
        }
    }

    /// <summary>
    /// Bouton permettant de stopper la mise à jour sur toutes les cartes
    /// </summary>
    internal class PauseDrawing_Button : Button
    {
        protected override void OnClick()
        {
            PauseDrawing.Go();
        }
    }

    internal class PauseDrawing
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected PauseDrawing() { }

        internal async static void Go()
        {
            await QueuedTask.Run(() =>
            {
                // récupère toutes les cartes ouvertes
                var mapViews = new List<MapView>();
                foreach (var mp in ArcGIS.Desktop.Framework.FrameworkApplication.Panes.OfType<IMapPane>())
                    mapViews.Add(mp.MapView);

                // modifie pour toutes les cartes le parametrage de recalcul.
                foreach (var mv in mapViews)
                    if (mv != null) mv.DrawingPaused = true;
            });
        }
    }
}

