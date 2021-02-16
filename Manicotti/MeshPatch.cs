using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace Manicotti
{
    [Transaction(TransactionMode.Manual)]
    public class MeshPatch : IExternalCommand
    {
        /// <summary>
        /// Extend the line to a boundary line. If the line has already surpassed it, trim the line instead.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="terminal"></param>
        /// <returns></returns>
        public static Line ExtendLine(Line line, Line terminal)
        {
            Line line_unbound = line.Clone() as Line;
            Line terminal_unbound = terminal.Clone() as Line;
            line_unbound.MakeUnbound();
            terminal_unbound.MakeUnbound();
            SetComparisonResult result = line_unbound.Intersect(terminal_unbound, out IntersectionResultArray results);
            if (result == SetComparisonResult.Overlap)
            {
                XYZ sectPt = results.get_Item(0).XYZPoint;
                XYZ extensionVec = (sectPt - line.GetEndPoint(0)).Normalize();
                if (Algorithm.IsPtOnLine(sectPt, line))
                {
                    double distance1 = sectPt.DistanceTo(line.GetEndPoint(0));
                    double distance2 = sectPt.DistanceTo(line.GetEndPoint(1));
                    if (distance1 > distance2)
                    {
                        return Line.CreateBound(line.GetEndPoint(0), sectPt);
                    }
                    else
                    {
                        return Line.CreateBound(line.GetEndPoint(1), sectPt);
                    }
                }
                else
                {
                    if (extensionVec.IsAlmostEqualTo(line.Direction))
                    {
                        return Line.CreateBound(line.GetEndPoint(0), sectPt);
                    }
                    else
                    {
                        return Line.CreateBound(sectPt, line.GetEndPoint(1));
                    }
                }
            }
            else
            {
                Debug.Print("Cannot locate the intersection point.");
                return null;
            }
        }

        /// <summary>
        /// Fuse two collinear segments if they are joined or almost joined.
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public static List<Line> CloseGapAtBreakpoint(List<Line> lines)
        {
            List<List<Line>> mergeGroups = new List<List<Line>>();
            mergeGroups.Add(new List<Line>() { lines[0]});
            lines.RemoveAt(0);

            while (lines.Count != 0)
            {
                foreach (Line element in lines)
                {
                    int iterCounter = 0;
                    foreach (List<Line> sublist in mergeGroups)
                    {
                        iterCounter += 1;
                        if (Algorithm.IsLineAlmostSubsetLines(element, sublist))
                        {
                            sublist.Add(element);
                            lines.Remove(element);
                            goto a;
                        }
                        if (iterCounter == mergeGroups.Count)
                        {
                            mergeGroups.Add(new List<Line>() { element });
                            lines.Remove(element);
                            goto a;
                        }
                    }
                }
            a:;
            }
            Debug.Print("The resulting lines should be " + mergeGroups.Count.ToString());

            List<Line> mergeLines = new List<Line>();
            foreach (List<Line> mergeGroup in mergeGroups)
            {
                if (mergeGroup.Count > 1)
                {
                    Debug.Print("Got lines to be merged " + mergeGroup.Count.ToString());
                    foreach (Line line in mergeGroup)
                    {
                        Debug.Print("Line{0} ({1}, {2}) -> ({3}, {4})", mergeGroup.IndexOf(line), line.GetEndPoint(0).X,
                            line.GetEndPoint(0).Y, line.GetEndPoint(1).X, line.GetEndPoint(1).Y);
                    }
                    Line merged = Algorithm.FuseLines(mergeGroup);
                    mergeLines.Add(merged);
                }
                else
                {
                    mergeLines.Add(mergeGroup[0]);
                }
                
            }
            return mergeLines;
        }

        /// <summary>
        /// Fix the gap when two lines are not met at the corner.
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public static List<Line> CloseGapAtCorner(List<Line> lines)
        {
            List<Line> linePatches = new List<Line>();
            List<int> removeIds = new List<int>();
            for (int i = 0; i < lines.Count; i++)
            {
                for (int j = i + 1; j < lines.Count; j++)
                {

                    if (!Algorithm.IsIntersected(lines[i], lines[j]) &&
                        Algorithm.IsAlmostJoined(lines[i], lines[j]))
                    {
                        removeIds.Add(i);
                        removeIds.Add(j);
                        linePatches.Add(ExtendLine(lines[i], lines[j]));
                        linePatches.Add(ExtendLine(lines[j], lines[i]));
                    }
                }
            }
            removeIds.Sort();
            for (int k = removeIds.Count - 1; k >= 0; k--)
            {
                lines.RemoveAt(removeIds[k]);
            }
            lines.AddRange(linePatches);
            return lines;
        }




        
        // Main thread
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;
            View view = doc.ActiveView;
            Selection sel = uidoc.Selection;

            // Extraction of CurveElements by LineStyle WALL
            CurveElementFilter filter = new CurveElementFilter(CurveElementType.ModelCurve);
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            ICollection<Element> founds = collector.WherePasses(filter).ToElements();
            List<CurveElement> importCurves = new List<CurveElement>();

            foreach (CurveElement ce in founds)
            {
                importCurves.Add(ce);
            }
            var columnCurves = importCurves.Where(x => x.LineStyle.Name == "COLUMN").ToList();
            List<Curve> columnLines = new List<Curve>();  // The door block has one arc at least
            foreach (CurveElement ce in columnCurves)
            {
                columnLines.Add(ce.GeometryCurve as Curve);
            }
            var wallCurves = importCurves.Where(x => x.LineStyle.Name == "WALL").ToList();
            List<Line> wallLines = new List<Line>();  // Algorithm only support walls of line type
            foreach (CurveElement ce in wallCurves)
            {
                wallLines.Add(ce.GeometryCurve as Line);
            }


            // Merge the overlapped wall boundaries
            // Seal the wall boundary by column block
            // INPUT wallLines, columnLines
            // OUTPUT wallLines
            #region PATCH wallLines
            List<Line> patchLines = new List<Line>();
            List<XYZ> sectPts = new List<XYZ>();

            // Seal the wall when encountered with column
            foreach (Line columnLine in columnLines)
            {
                sectPts.Clear();
                foreach (Line wallLine in wallLines)
                {
                    if (!Algorithm.IsParallel(columnLine, wallLine))
                    {
                        SetComparisonResult result = wallLine.Intersect(columnLine, out IntersectionResultArray results);
                        if (result != SetComparisonResult.Disjoint)
                        {
                            XYZ sectPt = results.get_Item(0).XYZPoint;
                            sectPts.Add(sectPt);
                        }
                    }
                    else
                    {
                        continue;
                    }
                }
                if (sectPts.Count() == 2)
                {
                    patchLines.Add(Line.CreateBound(sectPts[0], sectPts[1]));
                }
                if (sectPts.Count() > 2)  // not sure how to solve this 
                {
                    Line minPatch = Line.CreateBound(XYZ.Zero, 10000 * XYZ.BasisX);
                    for (int i = 0; i < sectPts.Count(); i++)
                    {
                        for (int j = i + 1; j < sectPts.Count(); j++)
                        {
                            if (sectPts[i].DistanceTo(sectPts[j]) > 0.001)
                            {
                                Line testPatch = Line.CreateBound(sectPts[i], sectPts[j]);
                                if (testPatch.Length < minPatch.Length)
                                {
                                    minPatch = testPatch;
                                }
                            }
                        }
                    }
                    patchLines.Add(minPatch);
                }
            }

            // Patch for the wall lines
            wallLines.AddRange(patchLines);

            // Merge lines when they are parallel and almost intersected (knob)
            List<Line> mergeLines = CloseGapAtBreakpoint(wallLines);

            // 
            List<Line> fixedLines = CloseGapAtCorner(mergeLines);

            #endregion

            // INPUT wallLines
            // OUTPUT wallClusters TREE data?
            // The clustering process encountered with fatal problem. PAUSED
            #region Cluster the wallLines by hierarchy

            // This fails everytime and I have no clue as to why
            var wallClusters = Algorithm.ClusterByIntersect(Util.LinesToCrvs(fixedLines));
            Debug.Print("{0} clustered wall blocks in total", wallClusters.Count);


            List<List<Curve>> wallBlocks = new List<List<Curve>> { };
            foreach (List<Curve> cluster in wallClusters)
            {
                List<Curve> clusterCrv = cluster;
                if (null != Algorithm.CreateBoundingBox2D(clusterCrv))
                {
                    wallBlocks.Add(Algorithm.CreateBoundingBox2D(clusterCrv));
                }
            }
            Debug.Print("{0} clustered wall bounding boxes in total", wallBlocks.Count);

            #endregion


            #region Iterate the generaion of axis
            #endregion


            // INPUT doorAxis, windowAxis
            #region Merge axis joined/overlapped
            #endregion


            // INPUT columnLines
            #region Extend and trim the axis (include column corner)
            #endregion



            #region Call region detection
            #endregion


            // Get the linestyle of "long-dashed"
            FilteredElementCollector fec = new FilteredElementCollector(doc)
                .OfClass(typeof(LinePatternElement));
            LinePatternElement linePatternElem = fec.FirstElement() as LinePatternElement;

            // Main visualization process
            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Generate Walls");

                // Mark the patch lines
                foreach (Curve patchLine in patchLines)
                {
                    DetailLine axis = doc.Create.NewDetailCurve(view, patchLine) as DetailLine;
                    GraphicsStyle gs = axis.LineStyle as GraphicsStyle;
                    gs.GraphicsStyleCategory.LineColor = new Color(202, 51, 82);
                    gs.GraphicsStyleCategory.SetLineWeight(3, gs.GraphicsStyleType);
                }

                Plane Geomplane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
                SketchPlane sketch = SketchPlane.Create(doc, Geomplane);

                // Mark the bounding box
                foreach (List<Curve> wallBlock in wallBlocks)
                {
                    foreach (Curve edge in wallBlock)
                    {
                        DetailLine axis = doc.Create.NewDetailCurve(view, edge) as DetailLine;
                        GraphicsStyle gs = axis.LineStyle as GraphicsStyle;
                        gs.GraphicsStyleCategory.LineColor = new Color(210, 208, 185);
                        gs.GraphicsStyleCategory.SetLineWeight(1, gs.GraphicsStyleType);
                        gs.GraphicsStyleCategory.SetLinePatternId(linePatternElem.Id, gs.GraphicsStyleType);
                    }
                }
                
                // Mark the fixed lines of walls
                foreach (Line line in mergeLines)
                {
                    ModelCurve modelline = doc.Create.NewModelCurve(line, sketch) as ModelCurve;
                }
                
                tx.Commit();
            }

            return Result.Succeeded;
        }
    }
}
