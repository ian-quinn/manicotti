#region Namespaces
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion

namespace Manicotti
{
    class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication a)
        {
            RibbonPanel panel = ribbonPanel(a);
            //string thisAssemblyPath = AssemblyLoadEventArgs.getExecutingAssembly().Location;
            string thisAssemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            
            //var globePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "icon.PNG");
            // need to load image in <filename>/bin/Debug file for windows
            // need to load image to C:\users\<USERNAME>\AppData\Roaming\Autodesk\Revit\Addins\2020
            Uri uriImage = new Uri("pack://application:,,,/Manicotti;component/ico/House_32.ico", UriKind.Absolute);
            BitmapImage defaultImg = new BitmapImage(uriImage);

            // Test button for floorplan split
            PushButton distribute = panel.AddItem(new PushButtonData("distribute", "Build up model\r\non all levels", thisAssemblyPath,
                "Manicotti.Distribute")) as PushButton;
            distribute.ToolTip = "Split geometries and texts in DWG by different floorplans." +
                "Try to extrude walls and columns on each building level. To test this demo, Floor.dwg and Column_demo.rfa must be linked";
            distribute.LargeImage = defaultImg;

            // Button list
            PushButtonData wall = new PushButtonData("extrude_wall", "Create Wall", 
                thisAssemblyPath, "Manicotti.TestWall");
            wall.ToolTip = "Automatically extrude walls based on exploded CAD drawings.";
            BitmapImage wallImg = new BitmapImage(new Uri("pack://application:,,,/Manicotti;component/ico/Wall.ico", UriKind.Absolute));
            wall.Image = wallImg;
            
            PushButtonData column = new PushButtonData("extrude_column", "Create Column", 
                thisAssemblyPath, "Manicotti.TestColumn");
            column.ToolTip = "Automatically extrude columns based on exploded CAD drawings.";
            BitmapImage columnImg = new BitmapImage(new Uri("pack://application:,,,/Manicotti;component/ico/Families.ico", UriKind.Absolute));
            column.Image = columnImg;

            PushButtonData region = new PushButtonData("detect_region", "Detect Region",
                thisAssemblyPath, "Manicotti.RegionDetect");
            region.ToolTip = "Detect enclosed regions based on intersected lines.";
            BitmapImage regionImg = new BitmapImage(new Uri("pack://application:,,,/Manicotti;component/ico/Room.ico", UriKind.Absolute));
            region.Image = regionImg;

            IList<RibbonItem> stackedGeometry = panel.AddStackedItems(wall, column, region);

            // Test button for mesh generation
            PushButtonData mesh = new PushButtonData("mesh", "Fix Axes Mesh", thisAssemblyPath,
                "Manicotti.MeshPatch");
            mesh.ToolTip = "Test button for space mesh generation by Basic & Curtain Wall";
            BitmapImage meshImg = new BitmapImage(new Uri("pack://application:,,,/Manicotti;component/ico/Basics.ico", UriKind.Absolute));
            mesh.Image = meshImg;

            // Test button for CAD info extraction
            PushButtonData channel = new PushButtonData("channel", "Grab DWG Info", thisAssemblyPath,
                "Manicotti.Channel");
            channel.ToolTip = "Extract elements from linked CAD file (Teigha based)";
            BitmapImage channelImg = new BitmapImage(new Uri("pack://application:,,,/Manicotti;component/ico/Parameters.ico", UriKind.Absolute));
            channel.Image = channelImg;

            IList<RibbonItem> stackedAlgorithm = panel.AddStackedItems(mesh, channel);


            a.ApplicationClosing += a_ApplicationClosing;

            return Result.Succeeded;
        }

        void a_Idling(object sender, Autodesk.Revit.UI.Events.IdlingEventArgs e)
        {
        }

        private void a_ApplicationClosing(object sender, Autodesk.Revit.UI.Events.ApplicationClosingEventArgs e)
        {
            throw new NotImplementedException();
        }

        public RibbonPanel ribbonPanel(UIControlledApplication a)
        {
            string tab = "Manicotti";
            RibbonPanel ribbonPanel = null;
            try
            {
                a.CreateRibbonTab(tab);
            }
            catch { }
            try
            {
                RibbonPanel panel = a.CreateRibbonPanel(tab, "Geometry Formation");
            }
            catch { }

            List<RibbonPanel> panels = a.GetRibbonPanels(tab);
            foreach (RibbonPanel p in panels)
            {
                if (p.Name == "Geometry Formation")
                {
                    ribbonPanel = p;
                }
            }

            return ribbonPanel;
        }

        public Result OnShutdown(UIControlledApplication a)
        {
            return Result.Succeeded;
        }
    }
}
