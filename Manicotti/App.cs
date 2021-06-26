#region Namespaces
using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion

namespace Manicotti
{
    class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication a)
        {
            //string thisAssemblyPath = AssemblyLoadEventArgs.getExecutingAssembly().Location;
            string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;

            // 1st Panel
            RibbonPanel modelBuild = ribbonPanel(a, "Manicotti", "Create Model");
            //var globePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "icon.PNG");
            // need to load image in <filename>/bin/Debug file for windows
            // need to load image to C:\users\<USERNAME>\AppData\Roaming\Autodesk\Revit\Addins\2020
            Uri uriImage = new Uri("pack://application:,,,/Manicotti;component/Resources/ico/Model.ico", UriKind.Absolute);
            BitmapImage modelImg = new BitmapImage(uriImage);

            // Test button for floorplan split
            PushButtonData createAllButtonData = new PushButtonData("create_all", "Build up model\non all levels", 
                thisAssemblyPath, "Manicotti.CmdCreateAll");
            createAllButtonData.AvailabilityClassName = "Manicotti.Util.ButtonControl";
            PushButton createAll = modelBuild.AddItem(createAllButtonData) as PushButton;
            createAll.ToolTip = "Split geometries and texts by their floors." +
                " Try to create walls and columns on all levels. To test this demo, Link_floor.dwg must be linked. (act on Linked DWG)";
            createAll.LargeImage = modelImg;

            // Button list
            PushButtonData wall = new PushButtonData("create_wall", "Walls", 
                thisAssemblyPath, "Manicotti.CmdCreateWall");
            wall.ToolTip = "Extrude walls. To test the demo, Link_demo.dwg must be linked. (act on Linked DWG with WALL layer)";
            BitmapImage wallImg = new BitmapImage(new Uri("pack://application:,,,/Manicotti;component/Resources/ico/Wall.ico", UriKind.Absolute));
            wall.Image = wallImg;
            
            PushButtonData column = new PushButtonData("create_column", "Columns", 
                thisAssemblyPath, "Manicotti.CmdCreateColumn");
            column.ToolTip = "Extrude columns. To test the demo, Link_demo.dwg must be linked. (act on Linked DWG with COLUMN layer)";
            BitmapImage columnImg = new BitmapImage(new Uri("pack://application:,,,/Manicotti;component/Resources/ico/Column.ico", UriKind.Absolute));
            column.Image = columnImg;
            
            PushButtonData opening = new PushButtonData("create_opening", "Openings", 
                thisAssemblyPath, "Manicotti.CmdCreateOpening");
            opening.ToolTip = "Insert openings. To test the demo, Link_demo.dwg must be linked. (need layer DOOR, WINDOW & WALL)";
            BitmapImage openingImg = new BitmapImage(new Uri("pack://application:,,,/Manicotti;component/Resources/ico/Opening.ico", UriKind.Absolute));
            opening.Image = openingImg;

            IList<RibbonItem> stackedGeometry = modelBuild.AddStackedItems(wall, column, opening);

            PushButton config = modelBuild.AddItem(new PushButtonData("config", "Default\nSettings",
                thisAssemblyPath, "Manicotti.Views.CmdConfig")) as PushButton;
            config.ToolTip = "Default and preferance settings. WIP";
            BitmapImage configImg = new BitmapImage(new Uri("pack://application:,,,/Manicotti;component/Resources/ico/Winform.ico", UriKind.Absolute));
            config.LargeImage = configImg;


            // 2nd Panel
            RibbonPanel modelFix = ribbonPanel(a, "Manicotti", "Misc.");
            // Currently we disable this panel that is still under test
            modelFix.Enabled = false;
            
            PushButton mesh = modelFix.AddItem(new PushButtonData("mesh", "Patch\nAxis Grid", 
                thisAssemblyPath, "Manicotti.MeshPatch")) as PushButton;
            mesh.ToolTip = "WIP. Create the axis grid of walls. To test the demo, Link_demo.dwg must be linked. (act on Linked DWG) ";
            BitmapImage meshImg = new BitmapImage(new Uri("pack://application:,,,/Manicotti;component/Resources/ico/Anchor.ico", UriKind.Absolute));
            mesh.LargeImage = meshImg;

            PushButtonData region = new PushButtonData("detect_region", "Detect Region",
                thisAssemblyPath, "Manicotti.RegionDetect");
            region.ToolTip = "Detect enclosed regions. (act on ModelLines with WALL linetype)";
            BitmapImage regionImg = new BitmapImage(new Uri("pack://application:,,,/Manicotti;component/Resources/ico/Room.ico", UriKind.Absolute));
            region.Image = regionImg;

            PushButtonData sketch = new PushButtonData("sketch", "Sketch DWG", 
                thisAssemblyPath, "Manicotti.SketchDWG");
            sketch.ToolTip = "Extract geometries and texts. To test the demo, Link_test.dwg must be linked. (act on Linked DWG)";
            BitmapImage channelImg = new BitmapImage(new Uri("pack://application:,,,/Manicotti;component/Resources/ico/Sketch.ico", UriKind.Absolute));
            sketch.Image = channelImg;

            PushButtonData fusion = new PushButtonData("fusion", "Regen Axis", 
                thisAssemblyPath, "Manicotti.Fusion");
            fusion.ToolTip = "Space mesh regeneration. (act on Walls & Curtains). WIP";
            BitmapImage fusionImg = new BitmapImage(new Uri("pack://application:,,,/Manicotti;component/Resources/ico/Boundary.ico", UriKind.Absolute));
            fusion.Image = fusionImg;

            IList<RibbonItem> stackedAlgorithm = modelFix.AddStackedItems(region, sketch, fusion);

            PushButton test = modelFix.AddItem(new PushButtonData(
                "test", "Test\nButton", thisAssemblyPath, "Manicotti.TestIntersect")) as PushButton;
            test.ToolTip = "Default and preferance settings. WIP";
            BitmapImage testImg = new BitmapImage(new Uri("pack://application:,,,/Manicotti;component/Resources/ico/Error.ico", UriKind.Absolute));
            test.LargeImage = testImg;

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

        public RibbonPanel ribbonPanel(UIControlledApplication a, String tabName, String panelName)
        {
            RibbonPanel ribbonPanel = null;
            try
            {
                a.CreateRibbonTab(tabName);
            }
            catch { }
            try
            {
                RibbonPanel panel = a.CreateRibbonPanel(tabName, panelName);
            }
            catch { }

            List<RibbonPanel> panels = a.GetRibbonPanels(tabName);
            foreach (RibbonPanel p in panels)
            {
                if (p.Name == panelName)
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
