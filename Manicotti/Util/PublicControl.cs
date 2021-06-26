#region Namespaces
using Autodesk.Revit.DB;
#endregion

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
