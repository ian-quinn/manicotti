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
            PushButton wall = panel.AddItem(new PushButtonData("extrude_wall", "Extrude\r\nWall", thisAssemblyPath,
                "Manicotti.ExtrudeWall")) as PushButton; //needs to be the ButtonName.Command

            wall.ToolTip = "Automatically extrude walls based on exploded CAD drawings. " +
                "WIP";

            //var globePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "icon.PNG");
            // need to load image in <filename>/bin/Debug file for windows
            // need to load image to C:\users\<USERNAME>\AppData\Roaming\Autodesk\Revit\Addins\2020
            Uri uriImage = new Uri("pack://application:,,,/Manicotti;component/ico/temp.ico", UriKind.Absolute);
            BitmapImage largeImage = new BitmapImage(uriImage);
            wall.LargeImage = largeImage;

            // Column button
            PushButton column = panel.AddItem(new PushButtonData("extrude_column", "Extrude\r\nColumn", thisAssemblyPath,
                "Manicotti.ExtrudeColumn")) as PushButton;
            column.ToolTip = "Automatically extrude columns based on exploded CAD drawings. " +
                "WIP";
            column.LargeImage = largeImage;

            // Test button
            PushButton test = panel.AddItem(new PushButtonData("trim_stray", "Trim\r\nStray", thisAssemblyPath,
                "Manicotti.Sandbox")) as PushButton;
            test.ToolTip = "This function allows you to delete orphan line segments (one endpoint not connected by others) to form a neat boundary. WIP";
            test.LargeImage = largeImage;

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
