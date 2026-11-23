using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace IncaDisplay._DisplayTools
{
    public class CheckOnMaps : MapTool
    {
        protected override void OnToolMouseDown(MapViewMouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                e.Handled = true; //Handle the event args to get the call to the corresponding async method
        }

        protected override Task HandleMouseDownAsync(MapViewMouseButtonEventArgs e)
        {
            return QueuedTask.Run(() =>
            {
                // Convert the clicked point in client coordinates to the corresponding map coordinates.
                MapPoint mapPoint = MapView.Active.ClientToMap(e.ClientPoint);

                // convert to lon/lat
                MapPoint coords = (MapPoint)GeometryEngine.Instance.Project(mapPoint, SpatialReferences.WGS84);

                // open a web browser
                string xcoord = coords.X.ToString("G", CultureInfo.InvariantCulture);
                string ycoord = coords.Y.ToString("G", CultureInfo.InvariantCulture);
                string url = string.Format("https://www.google.fr/maps/@{0},{1},300m/data=!3m1!1e3?entry=ttu", ycoord, xcoord);
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

                // Remettre l'outil de sélection (Explore Tool)
                FrameworkApplication.SetCurrentToolAsync("esri_mapping_exploreTool");
            });
        }
    }

    public class CheckOnStreetView : MapTool
    {
        protected override void OnToolMouseDown(MapViewMouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                e.Handled = true; //Handle the event args to get the call to the corresponding async method
        }

        protected override Task HandleMouseDownAsync(MapViewMouseButtonEventArgs e)
        {
            return QueuedTask.Run(() =>
            {
                // Convert the clicked point in client coordinates to the corresponding map coordinates.
                MapPoint mapPoint = MapView.Active.ClientToMap(e.ClientPoint);

                // convert to lon/lat
                MapPoint coords = (MapPoint)GeometryEngine.Instance.Project(mapPoint, SpatialReferences.WGS84);

                // open a web browser
                string xcoord = coords.X.ToString("G", CultureInfo.InvariantCulture);
                string ycoord = coords.Y.ToString("G", CultureInfo.InvariantCulture);
                string url = string.Format("https://www.google.com/maps/@?api=1&map_action=pano&viewpoint={0}%2C{1}", ycoord, xcoord);
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                
                // Remettre l'outil de sélection (Explore Tool)
                FrameworkApplication.SetCurrentToolAsync("esri_mapping_exploreTool");
            });
        }
    }
}

