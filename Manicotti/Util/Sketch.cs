using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
#region Namespaces
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion

namespace Manicotti.Util
{
    /// <summary>
    /// Craphical debugging helper using model lines
    /// </summary>
    public static class Sketch
    {
        #region SketchPlane methods

        /// <summary>
        /// Create a plane perpendicular to the given factor.
        /// </summary>
        public static SketchPlane PlaneNormal(Document doc, XYZ normal, XYZ origin)
        {
            return SketchPlane.Create(doc, Plane.CreateByNormalAndOrigin(normal, origin));
        }

        public static SketchPlane PlaneWorld(Document doc)
        {
            return SketchPlane.Create(doc, Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero));
        }


        // Marker methods

        /// <summary>
        /// Draw an X at the given position.
        /// </summary>
        public static void DrawMarkerX(XYZ p, double size, SketchPlane sketchPlane)
        {
            size *= 0.5;
            XYZ v = new XYZ(size, size, 0);
            Document doc = sketchPlane.Document;
            doc.Create.NewModelCurve(Line.CreateBound(p - v, p + v), sketchPlane);
            v = new XYZ(size, -size, 0);
            doc.Create.NewModelCurve(Line.CreateBound(p - v, p + v), sketchPlane);
        }

        /// <summary>
        /// Draw an O at the given position.
        /// </summary>
        public static void DrawMarkerO(XYZ p, double radius, SketchPlane sketchPlane)
        {
            Document doc = sketchPlane.Document;
            XYZ xAxis = new XYZ(1, 0, 0);
            XYZ yAxis = new XYZ(0, 1, 0);
            doc.Create.NewModelCurve(Arc.Create(p, radius, 0, 2 * Math.PI, xAxis, yAxis), sketchPlane);
        }

        #endregion

        #region Doc geometry methods
        /// <summary>
        /// Return the curve from a Revit database Element 
        /// location curve, if it has one.
        /// </summary>
        public static Curve GetLocationCurve(this Element e)
        {
            Debug.Assert(null != e.Location, "expected an element with a valid Location");

            LocationCurve lc = e.Location as LocationCurve;

            Debug.Assert(null != lc, "expected an element with a valid LocationCurve");

            return lc.Curve;
        }

        /// <summary>
        /// Return the location point of a family instance or null.
        /// This null coalesces the location so you won't get an 
        /// error if the FamilyInstance is an invalid object.  
        /// </summary>
        public static XYZ GetFamilyInstanceLocation(FamilyInstance fi)
        {
            return ((LocationPoint)fi?.Location)?.Point;
        }


        // Detailed line methods
        public static void GetListOfLinestyles(Document doc)
        {
            Category c = doc.Settings.Categories.get_Item(
              BuiltInCategory.OST_Lines);

            CategoryNameMap subcats = c.SubCategories;

            foreach (Category lineStyle in subcats)
            {
                Debug.Print("Line style", string.Format(
                  "Linestyle {0} id {1}", lineStyle.Name, 
                  lineStyle.Id.ToString()));
            }
        }

        #endregion


        #region DetailCurve method
        /// <summary>
        /// Draw detail curves based on List<Curve>
        /// </summary>
        public static void DrawDetailLines(Document doc, List<Curve> crvs, int weight = 2, string color = "red", string pattern = "")
        {
            GetListOfLinestyles(doc);

            View view = doc.ActiveView;
            Color palette = new Color(0, 0, 0);
            switch (color)
            {
                case "red": palette = new Color(200, 50, 80); break;
                case "blue": palette = new Color(100, 149, 237); break;
                case "orange": palette = new Color(255, 140, 0); break;
            }

            FilteredElementCollector fec = new FilteredElementCollector(doc)
                .OfClass(typeof(LinePatternElement));

            LinePatternElement linePatternElem = null;
            if (pattern != "")
            {
                try
                {
                    linePatternElem = fec
                        .Cast<LinePatternElement>()
                        .First<LinePatternElement>(linePattern => linePattern.Name == pattern);
                }
                catch
                {
                    Debug.Print("There's no matching pattern in the document");
                }
            }
            

            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Create Detail Curves");
                foreach (Curve crv in crvs)
                {
                    // Should do style setting here or...?
                    DetailCurve detailCrv = doc.Create.NewDetailCurve(view, crv);
                    GraphicsStyle gs = detailCrv.LineStyle as GraphicsStyle;
                    gs.GraphicsStyleCategory.LineColor = palette;
                    gs.GraphicsStyleCategory.SetLineWeight(weight, gs.GraphicsStyleType);
                    if (linePatternElem != null)
                    {
                        gs.GraphicsStyleCategory.SetLinePatternId(linePatternElem.Id, GraphicsStyleType.Projection);
                    }
                }
                tx.Commit();
            }
        }

        /// <summary>
        /// Draw point marker with detail circles.
        /// Optional colors are "red" "blue" "orange"
        /// </summary>
        public static void DrawDetailMarkers(Document doc, List<XYZ> pts, int weight = 2, string color = "red", string pattern = "")
        {
            GetListOfLinestyles(doc);

            View view = doc.ActiveView;
            Color palette = new Color(0, 0, 0);
            switch (color)
            {
                case "red": palette = new Color(200, 50, 80); break;
                case "blue": palette = new Color(100, 149, 237); break;
                case "orange": palette = new Color(255, 140, 0); break;
            }

            FilteredElementCollector fec = new FilteredElementCollector(doc)
                .OfClass(typeof(LinePatternElement));

            LinePatternElement linePatternElem = null;
            if (pattern != "")
            {
                try
                {
                    linePatternElem = fec
                        .Cast<LinePatternElement>()
                        .First<LinePatternElement>(linePattern => linePattern.Name == pattern);
                }
                catch
                {
                    Debug.Print("There's no matching pattern in the document");
                }
            }

            XYZ xAxis = new XYZ(1, 0, 0);
            XYZ yAxis = new XYZ(0, 1, 0);

            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Create Detail Markers");
                foreach (XYZ pt in pts)
                {
                    double radius = 0.3;
                    Arc marker = Arc.Create(pt, radius, 0, 2 * Math.PI, xAxis, yAxis);
                    DetailCurve detailCrv = doc.Create.NewDetailCurve(view, marker);
                    GraphicsStyle gs = detailCrv.LineStyle as GraphicsStyle;
                    gs.GraphicsStyleCategory.LineColor = palette;
                    gs.GraphicsStyleCategory.SetLineWeight(weight, gs.GraphicsStyleType);
                    if (linePatternElem != null)
                    {
                        gs.GraphicsStyleCategory.SetLinePatternId(linePatternElem.Id, GraphicsStyleType.Projection);
                    }
                }
                tx.Commit();
            }
        }

        #endregion

    }
}
