#region Namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
#endregion

namespace Manicotti
{
    class SelectLayer
    {
        public static string strGraphicsStyleName(UIDocument uidoc)
        {
            Reference r = uidoc.Selection.PickObject(ObjectType.PointOnElement);

            Element elm = uidoc.Document.GetElement(r);

            GeometryObject geo = elm.GetGeometryObjectFromReference(r);

            GraphicsStyle gs = uidoc.Document.GetElement(geo.GraphicsStyleId) as GraphicsStyle;
            string strNsme = gs.GraphicsStyleCategory.Name;

            return strNsme;
        }
    }
}