using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace Manicotti
{
    public static class CADGeoUtil
    {
        /// <summary>
        /// Pick a DWG import/linked instance (ver.2010 or below), extract all visible elements or 
        /// ones within specific Layer(LineType) if the type is assigned.
        /// </summary>
        public static List<GeometryObject> ExtractElement(UIDocument uidoc, ImportInstance import, string type = "wildcard")
        {
            Document doc = uidoc.Document;
            View active_view = doc.ActiveView;

            List<GeometryObject> visible_dwg_geo = new List<GeometryObject>();
            
            // Get Geometry
            var geoElem = import.get_Geometry(new Options());
            foreach (var geoObj in geoElem)
            {
                if (geoObj is GeometryInstance)
                {
                    var geoIns = geoObj as GeometryInstance;
                    /*
                    foreach (GeometryObject insObj in geoIns.SymbolGeometry)
                    {
                        Debug.Print(insObj.GetType().Name);
                    }
                    */

                    // This may contain child GeometryInstance, so...
                    // If fully explosion is need, recrusive function is needed here
                    var ge2 = geoIns.GetInstanceGeometry();
                    if (ge2 != null)
                    {
                        foreach (var obj in ge2)
                        {
                            // Use the GraphicsStyle to get the DWG layer linked to the Category for visibility.
                            var gStyle = doc.GetElement(obj.GraphicsStyleId) as GraphicsStyle;
                            //Debug.Print("The category is: " + gStyle.GraphicsStyleCategory.Name);
                            // Check if the layer is visible in the view.
                            if (!active_view.GetCategoryHidden(gStyle.GraphicsStyleCategory.Id))
                            {
                                if (type == "wildcard")
                                {
                                    visible_dwg_geo.Add(obj);
                                }
                                // Select a certain Linetype(Layername/StyleCategory)
                                else if (gStyle.GraphicsStyleCategory.Name == type)
                                {
                                    visible_dwg_geo.Add(obj);
                                }
                            }
                        }
                    }
                }
            }
            return visible_dwg_geo;
        }
    }
}
