using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Manicotti.Util
{
    public static class PublicControl
    {
        public static SketchPlane GetSketchPlaneByPlane(Document doc, XYZ normal, XYZ origin)
        {
            Plane plane = Plane.CreateByNormalAndOrigin(normal, origin);
            return SketchPlane.Create(doc, plane);
        }
    }
}
