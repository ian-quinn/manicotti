#region Namespaces
using System;
using System.Diagnostics;
using System.IO;

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion

namespace Manicotti.Views
{
    [Transaction(TransactionMode.Manual)]
    public class CmdConfig : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            Application app = uiapp.Application;

            //Properties.Settings.Default.url_install = UtilGetInstallPath.Execute(app);

            Views.Configuration configuration = new Views.Configuration(doc);
            configuration.ShowDialog();

            return Result.Succeeded;
        }
    }
}