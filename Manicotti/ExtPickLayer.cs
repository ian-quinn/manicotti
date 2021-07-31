#region Namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Runtime.InteropServices;
#endregion

namespace Manicotti
{
    [Transaction(TransactionMode.Manual)]
    public class ExtPickLayer : IExternalEventHandler
    {
        public string targetValue { get; set; }

        public ExtPickLayer(UIApplication uiapp)
        {
        }

        public void Execute(UIApplication uiapp)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            string layerChain = "";

            //List<Reference> rs = uidoc.Selection.PickObjects(ObjectType.PointOnElement, 
            //    "Pickup elements in the imported DWG file").ToList();
            //using (Transaction tx = new Transaction(doc, "Hide selected layter"))
            //{
            //    tx.Start();
            //    foreach (Reference r in rs)
            //    {
            //        Element elem = doc.GetElement(r);
            //        GeometryObject geoObj = elem.GetGeometryObjectFromReference(r);
            //        GraphicsStyle gs = doc.GetElement(geoObj.GraphicsStyleId) as GraphicsStyle;
            //        if (layerChain == "") { layerChain = gs.GraphicsStyleCategory.Name; }
            //        else { layerChain += ", " + gs.GraphicsStyleCategory.Name; }
            //        Debug.Print("## " + layerChain);

            //        ElementId elementId = gs.GraphicsStyleCategory.Id;
            //        View view = doc.ActiveView;

            //        view.SetCategoryHidden(elementId, true);
            //    }
            //    tx.Commit();
            //}

            bool boTr = true;
            while (boTr)
            {
                try
                {
                    Reference r = uidoc.Selection.PickObject(ObjectType.PointOnElement, "Pickup elements of the target layer within the imported DWG. Press ESC to quit.");
                    Element elem = doc.GetElement(r);
                    //GeometryElement geoElem = elem.get_Geometry(new Options());
                    GeometryObject geoObj = elem.GetGeometryObjectFromReference(r);
                    GraphicsStyle gs = doc.GetElement(geoObj.GraphicsStyleId) as GraphicsStyle;
                    if (layerChain == "") { layerChain = gs.GraphicsStyleCategory.Name; }
                    else { layerChain += ", " + gs.GraphicsStyleCategory.Name; }
                    Properties.Settings.Default[targetValue] = layerChain;

                    ElementId elementId = gs.GraphicsStyleCategory.Id;
                    View view = doc.ActiveView;

                    using (Transaction tx = new Transaction(doc, "Hide selected layter"))
                    {
                        tx.Start();
                        view.SetCategoryHidden(elementId, true);
                        tx.Commit();
                    }
                }
                catch
                {
                    boTr = false;
                }
            }
            //Properties.Settings.Default[targetValue] = layerChain;
        }

        public string GetName()
        {
            return "Pick layers";
        }
    }
}