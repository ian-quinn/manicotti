using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Manicotti
{
    public class UtilButtonController : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication uiapp, CategorySet selectedCategories)
        {
            try
            {
                Document doc = uiapp.ActiveUIDocument.Document;
                View view = doc.ActiveView;
                if (view is View3D)
                {
                    return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
