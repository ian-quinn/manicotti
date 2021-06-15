#region Namespaces
using System;
using System.Diagnostics;
using System.IO;

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion

namespace Manicotti
{
    [Transaction(TransactionMode.Manual)]
    public class Config : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            Application app = uiapp.Application;

            //Properties.Settings.Default.url_install = UtilGetInstallPath.Execute(app);
            string thisAssemblyFolderPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            
            Properties.Settings.Default.url_column = thisAssemblyFolderPath + @"\M_Rectangular Column.rfa";
            Properties.Settings.Default.url_door = thisAssemblyFolderPath + @"\M_Door-Single-Panel.rfa";
            Properties.Settings.Default.url_window = thisAssemblyFolderPath + @"\M_Window-Fixed.rfa";

            Views.Configuration configuration = new Views.Configuration(doc);
            configuration.ShowDialog();

            return Result.Succeeded;
        }
    }
}