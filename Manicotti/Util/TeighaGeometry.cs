#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
#endregion

namespace Manicotti.Util
{
    public static class TeighaGeometry
    {
        /// <summary>
        /// Pick a DWG import/linked instance (ver.2010 or below), extract all visible elements or 
        /// ones within specific Layer(LineType) if the type(GeometryObjectType) is assigned.
        /// </summary>
        public static List<GeometryObject> ExtractElement(UIDocument uidoc, ImportInstance import, string layer = "*", string type = "*")
        {
            Document doc = uidoc.Document;
            View active_view = doc.ActiveView;

            List<GeometryObject> visible_dwg_geo = new List<GeometryObject>();
            
            // Get Geometry
            var geoElem = import.get_Geometry(new Options());
            Debug.Print("Found elements altogether: " + geoElem.Count().ToString());
            foreach (var geoObj in geoElem)
            {
                if (geoObj is GeometryInstance)
                {
                    var geoIns = geoObj as GeometryInstance;
                    
                    // This may contain child GeometryInstance, so...
                    // If fully explosion is need, recrusive function is needed here
                    var ge2 = geoIns.GetInstanceGeometry();
                    if (ge2 != null)
                    {
                        foreach (var obj in ge2)
                        {
                            // Use the GraphicsStyle to get the DWG layer linked to the Category for visibility.
                            var gStyle = doc.GetElement(obj.GraphicsStyleId) as GraphicsStyle;

                            // If an object does not have a GraphicsStyle just skip it
                            if (gStyle == null)
                            {
                                continue;
                            }
                            //Debug.Print(obj.GetType().Name);

                            // Check if the layer is visible in the view.
                            if (!active_view.GetCategoryHidden(gStyle.GraphicsStyleCategory.Id))
                            {
                                if (layer == "*")
                                {
                                    if (type == "*")
                                    {
                                        visible_dwg_geo.Add(obj);
                                    }
                                    else if (obj.GetType().Name == type)
                                    {
                                        visible_dwg_geo.Add(obj);
                                    }
                                }
                                // Select a certain Linetype(Layername/StyleCategory)
                                else if (gStyle.GraphicsStyleCategory.Name == layer)
                                {
                                    if (type == "*")
                                    {
                                        visible_dwg_geo.Add(obj);
                                    }
                                    else if (obj.GetType().Name == type)
                                    {
                                        visible_dwg_geo.Add(obj);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            Debug.Print("Geometry collected: " + visible_dwg_geo.Count().ToString());
            return visible_dwg_geo;
        }


        /// <summary>
        /// Shatter geometry objects to curves
        /// </summary>
        /// <param name="uidoc"></param>
        /// <param name="import"></param>
        /// <param name="layer"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public static List<Curve> ShatterCADGeometry(UIDocument uidoc, ImportInstance import, string layer, double tolerance)
        {
            List<Curve> shatteredCrvs = new List<Curve>();
            List<GeometryObject> dwg_geos = TeighaGeometry.ExtractElement(uidoc, import, layer);
            if (dwg_geos.Count > 0)
            {
                foreach (var obj in dwg_geos)
                {
                    Curve crv = obj as Curve;
                    PolyLine poly = obj as PolyLine;
                    if (obj.GetType().ToString() == "Autodesk.Revit.DB.Arc")
                    {
                        Debug.Print("An arc detected");
                        Line flattenCrv = Line.CreateBound(crv.GetEndPoint(0), crv.GetEndPoint(1));
                        shatteredCrvs.Add(flattenCrv as Curve);
                        continue;
                    }
                    if (null != crv)
                    {
                        shatteredCrvs.Add(crv);
                    }
                    if (null != poly)
                    {
                        var vertices = poly.GetCoordinates();
                        for (int i = 0; i < vertices.Count() - 1; i++)
                        {
                            if ((vertices[i + 1] - vertices[i]).GetLength() >= tolerance)
                            {
                                shatteredCrvs.Add(Line.CreateBound(vertices[i], vertices[i + 1]) as Curve);
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }
                }
            }
            return shatteredCrvs;
        }
    }
}
