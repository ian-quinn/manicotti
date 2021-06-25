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
        public static Curve ExtendLine(Curve line, Curve terminal)
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
                if (Algorithm.IsPtOnLine(sectPt, line as Line))
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
                    if (extensionVec.IsAlmostEqualTo(line_unbound.Direction))
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

        /* abandoned for now
        public static Curve ExtendLineToBox(Curve line, List<Curve> box)
        {
            Line result = null;
            foreach (Curve edge in box)
            {
                var test = ExtendLine(line, edge);
                if (null == test) { continue; }
                if (test.Length > line.Length)
                {
                    return result;
                }
            }
            Debug.Print("Failure at line extension to box");
            return result;
        }
        */

        /// <summary>
        /// Fuse two collinear segments if they are joined or almost joined.
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public static List<Curve> CloseGapAtBreakpoint(List<Curve> lines)
        {
            List<List<Curve>> mergeGroups = new List<List<Curve>>();
            mergeGroups.Add(new List<Curve>() { lines[0] });
            lines.RemoveAt(0);

            while (lines.Count != 0)
            {
                foreach (Line element in lines)
                {
                    int iterCounter = 0;
                    foreach (List<Curve> sublist in mergeGroups)
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
                            mergeGroups.Add(new List<Curve>() { element });
                            lines.Remove(element);
                            goto a;
                        }
                    }
                }
            a:;
            }
            Debug.Print("The resulting lines should be " + mergeGroups.Count.ToString());

            List<Curve> mergeLines = new List<Curve>();
            foreach (List<Curve> mergeGroup in mergeGroups)
            {
                if (mergeGroup.Count > 1)
                {
                    Debug.Print("Got lines to be merged " + mergeGroup.Count.ToString());
                    foreach (Line line in mergeGroup)
                    {
                        Debug.Print("Line{0} ({1}, {2}) -> ({3}, {4})", mergeGroup.IndexOf(line), line.GetEndPoint(0).X,
                            line.GetEndPoint(0).Y, line.GetEndPoint(1).X, line.GetEndPoint(1).Y);
                    }
                    var merged = Algorithm.FuseLines(mergeGroup);
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
        public static List<Curve> CloseGapAtCorner(List<Curve> lines)
        {
            List<Curve> linePatches = new List<Curve>();
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

            double tolerance = commandData.Application.Application.ShortCurveTolerance;

            // Pick Import Instance
            Reference r = uidoc.Selection.PickObject(ObjectType.Element, new Util.ElementsOfClassSelectionFilter<ImportInstance>());
            var import = doc.GetElement(r) as ImportInstance;
            
            List<Curve> columnCrvs = Util.TeighaGeometry.ShatterCADGeometry(uidoc, import, "COLUMN", tolerance);
            List<Curve> wallCrvs = Util.TeighaGeometry.ShatterCADGeometry(uidoc, import, "WALL", tolerance);
            List<Curve> doorCrvs = Util.TeighaGeometry.ShatterCADGeometry(uidoc, import, "DOOR", tolerance);
            List<Curve> windowCrvs = Util.TeighaGeometry.ShatterCADGeometry(uidoc, import, "WINDOW", tolerance);
            //List<Line> columnLines = Util.CrvsToLines(columnCrvs);
            //List<Line> wallLines = Util.CrvsToLines(wallCrvs);

            // Merge the overlapped wall boundaries
            // Seal the wall boundary by column block
            // INPUT List<Line> wallLines, List<Line> columnLines
            #region PATCH wallLines
            List<Line> patchLines = new List<Line>();
            List<XYZ> sectPts = new List<XYZ>();

            // Seal the wall when encountered with column
            foreach (Curve columnCrv in columnCrvs)
            {
                sectPts.Clear();
                foreach (Line wallCrv in wallCrvs)
                {
                    if (!Algorithm.IsParallel(columnCrv, wallCrv))
                    {
                        SetComparisonResult result = wallCrv.Intersect(columnCrv, out IntersectionResultArray results);
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
            wallCrvs.AddRange(patchLines);

            // Merge lines when they are parallel and almost intersected (knob)
            List<Curve> mergeLines = CloseGapAtBreakpoint(wallCrvs);

            // 
            List<Curve> fixedLines = CloseGapAtCorner(mergeLines);

            #endregion
            // OUTPUT List<Line> fixedLines


            // INPUT List<Line> fixedLines
            #region Cluster the wallLines by hierarchy

            var wallClusters = Algorithm.ClusterByIntersect(fixedLines);
            Debug.Print("{0} clustered wall blocks in total", wallClusters.Count);

            // Generate boundingbox marker for the wall cluster
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
            // INPUT List<List< Curve >> wallClusters
            Debug.Print("WALL LINES PATCH & CLUSTERING COMPLETE!");


            // INPUT List<List<Curve>> wallClusters
            #region Iterate the generaion of axis

            // Wall axes
            List<Curve> axes = new List<Curve>();
            double bias = Misc.MmToFoot(20);
            foreach (List<Curve> wallCluster in wallClusters)
            {
                List<Line> lines = Misc.CrvsToLines(wallCluster);
                for (int i = 0; i < lines.Count; i++)
                {
                    for (int j = 0; j < lines.Count - i; j++)
                    {
                        if (Algorithm.IsParallel(lines[i], lines[i + j])
                            && !Algorithm.IsIntersected(lines[i], lines[i + j]))
                        {
                            if (Algorithm.LineSpacing(lines[i], lines[i + j]) < Misc.MmToFoot(200) + bias
                            && Algorithm.LineSpacing(lines[i], lines[i + j]) > Misc.MmToFoot(200) - bias
                            && Algorithm.IsShadowing(lines[i], lines[i + j]))
                            {
                                if (Algorithm.GenerateAxis(lines[i], lines[i + j]) != null)
                                {
                                    axes.Add(Algorithm.GenerateAxis(lines[i], lines[i + j]));
                                    Debug.Print("got it!");
                                }
                            }
                        }
                    }
                }
            }
            #endregion
            // OUTPUT List<Line> axes
            Debug.Print("WALL AXIS ITERATION COMPLETE!");


            // INPUT List<Curve> doorCrvs
            // INPUT List<Curve> windowCrvs
            // INPUT List<Line> axes
            #region Merge axis joined/overlapped

            // Door axes
            var doorClusters = Algorithm.ClusterByIntersect(doorCrvs);
            List<List<Curve>> doorBlocks = new List<List<Curve>> { };
            foreach (List<Curve> cluster in doorClusters)
            {
                if (null != Algorithm.CreateBoundingBox2D(cluster))
                {
                    doorBlocks.Add(Algorithm.CreateBoundingBox2D(cluster));
                }
            }
            List<Curve> doorAxes = new List<Curve> { };
            foreach (List<Curve> doorBlock in doorBlocks)
            {
                List<Curve> doorFrame = new List<Curve> { };
                for (int i = 0; i < doorBlock.Count; i++)
                {
                    int sectCount = 0;
                    List<Line> fenses = new List<Line>();
                    foreach (Line line in fixedLines)
                    {
                        Curve testCrv = doorBlock[i].Clone();
                        SetComparisonResult result = RegionDetect.ExtendCrv(testCrv, 0.01).Intersect(line,
                                                   out IntersectionResultArray results);
                        if (result == SetComparisonResult.Overlap)
                        {
                            sectCount += 1;
                            fenses.Add(line);
                        }
                    }
                    if (sectCount == 2)
                    {
                        XYZ projecting = fenses[0].Evaluate(0.5, true);
                        XYZ projected = fenses[1].Project(projecting).XYZPoint;
                        if (fenses[0].Length > fenses[1].Length)
                        {
                            projecting = fenses[1].Evaluate(0.5, true);
                            projected = fenses[0].Project(projecting).XYZPoint;
                        }
                        Line doorAxis = Line.CreateBound(projecting, projected);
                        doorAxes.Add(doorAxis);
                        //doorAxes.Add(doorBlock[i]);
                    }
                }
            }

            // Window axes
            var windowClusters = Algorithm.ClusterByIntersect(windowCrvs);

            List<List<Curve>> windowBlocks = new List<List<Curve>> { };
            foreach (List<Curve> cluster in windowClusters)
            {
                if (null != Algorithm.CreateBoundingBox2D(cluster))
                {
                    windowBlocks.Add(Algorithm.CreateBoundingBox2D(cluster));
                }
            }
            List<Curve> windowAxes = new List<Curve> { };
            foreach (List<Curve> windowBlock in windowBlocks)
            {
                Line axis1 = Line.CreateBound((windowBlock[0].GetEndPoint(0) + windowBlock[0].GetEndPoint(1)).Divide(2),
                    (windowBlock[2].GetEndPoint(0) + windowBlock[2].GetEndPoint(1)).Divide(2));
                Line axis2 = Line.CreateBound((windowBlock[1].GetEndPoint(0) + windowBlock[1].GetEndPoint(1)).Divide(2),
                    (windowBlock[3].GetEndPoint(0) + windowBlock[3].GetEndPoint(1)).Divide(2));
                if (axis1.Length > axis2.Length)
                {
                    windowAxes.Add(axis1);
                }
                else
                {
                    windowAxes.Add(axis2);
                }
            }
            

            axes.AddRange(windowAxes);
            axes.AddRange(doorAxes);
            Debug.Print("Checklist for axes: Door-{0}, Window-{1}, All-{2}", doorAxes.Count, windowAxes.Count,
                axes.Count);
            List<Curve> axesExtended = new List<Curve>();
            foreach (Curve axis in axes)
            {
                axesExtended.Add(Algorithm.ExtendLine(axis, 200));
            }
            // Axis merge 
            List<List<Curve>> axisGroups = Algorithm.ClusterByOverlap(axesExtended);
            List<Curve> centerLines = new List<Curve>();
            foreach (List<Curve> axisGroup in axisGroups)
            {
                var merged = Algorithm.FuseLines(axisGroup);
                centerLines.Add(merged);
            }

            #endregion
            // OUTPUT List<Line> centerLines
            Debug.Print("WINDOW / DOOR LINES JOINED!");


            // INPUT List<Curve> columnCrvs
            // INPUT List<Line> centerLines
            #region Extend and trim the axis (include column corner)

            List<List<Curve>> columnGroups = Algorithm.ClusterByIntersect(columnCrvs);
            foreach (List<Curve> columnGroup in columnGroups)
            {
                //List<Line> columnGrouplines = Util.CrvsToLines(columnGroup);
                List<Curve> nestLines = new List<Curve>();
                for (int i = 0; i < columnGroup.Count; i++)
                {
                    foreach (Line centerLine in centerLines)
                    {
                        SetComparisonResult result = columnGroup[i].Intersect(centerLine, out IntersectionResultArray results);
                        if (result == SetComparisonResult.Overlap)
                        {
                            for (int j = 0; j < columnGroup.Count; j++)
                            {
                                if (j != i)
                                {
                                    if (null != ExtendLine(centerLine, columnGroup[j]))
                                    {
                                        nestLines.Add(ExtendLine(centerLine, columnGroup[j]));
                                    }
                                }
                            }
                        }
                    }
                }
                Debug.Print("Got nested lines: " + nestLines.Count.ToString());
                if (nestLines.Count < 2) { continue; }
                else
                {
                    centerLines.AddRange(nestLines);
                    int count = 0;
                    for (int i = 1; i < nestLines.Count; i++)
                    {
                        if (!Algorithm.IsParallel(nestLines[0], nestLines[i]))
                        { count += 1; }
                    }
                    if (count == 0)
                    {
                        var patches = Algorithm.CenterLinesOfBox(columnGroup);
                        foreach (Line patch in patches)
                        {
                            if (Algorithm.IsLineIntersectLines(patch, nestLines)) { centerLines.Add(patch); }
                        }
                    }
                }
            }

            #endregion
            // OUTPUT List<Line> centerLines
            Debug.Print("AXES JOINED AT COLUMN");


            // INPUT List<Line> centerLines
            //#The region detect function has fatal bug during boolean union operation
            #region Call region detection
            // Axis merge 
            List<List<Curve>> tempStrays = Algorithm.ClusterByOverlap(centerLines);
            List<Curve> strays = new List<Curve>();
            foreach (List<Curve> tempStray in tempStrays)
            {
                Curve merged = Algorithm.FuseLines(tempStray);
                strays.Add(merged);
            }

            //var strayClusters = Algorithm.ClusterByIntersect(Util.LinesToCrvs(strays));
            //Debug.Print("Cluster of strays: " + strayClusters.Count.ToString());
            //Debug.Print("Cluster of strays[0]: " + strayClusters[0].Count.ToString());
            //Debug.Print("Cluster of strays[1]: " + strayClusters[1].Count.ToString());
            // The RegionCluster method should be applied to each cluster of the strays
            // It only works on a bunch of intersected line segments
            List<CurveArray> loops = RegionDetect.RegionCluster(strays);
            // The boolean union method of the loops needs to fix
            var perimeter = RegionDetect.GetBoundary(loops);
            var recPerimeter = CloseGapAtBreakpoint(perimeter);
            var arrayPerimeter = RegionDetect.AlignCrv(recPerimeter);
            for (int i = 0; i < arrayPerimeter.Size; i++)
            {
                Debug.Print("Line-{0} {1} {2}", i, Misc.PrintXYZ(arrayPerimeter.get_Item(i).GetEndPoint(0)),
                    Misc.PrintXYZ(arrayPerimeter.get_Item(i).GetEndPoint(1)));
            }
            #endregion
            // OUTPUT List<CurveArray> loops
            Debug.Print("REGION COMPLETE!");

            


            // Get the linestyle of "long-dashed"
            FilteredElementCollector fec = new FilteredElementCollector(doc)
                .OfClass(typeof(LinePatternElement));
            LinePatternElement linePatternElem = fec.FirstElement() as LinePatternElement;

            // Main visualization process
            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Generate Floor");

                // Draw wall patch lines
                /*
                foreach (Curve patchLine in patchLines)
                {
                    DetailLine axis = doc.Create.NewDetailCurve(view, patchLine) as DetailLine;
                    GraphicsStyle gs = axis.LineStyle as GraphicsStyle;
                    gs.GraphicsStyleCategory.LineColor = new Color(202, 51, 82);
                    gs.GraphicsStyleCategory.SetLineWeight(3, gs.GraphicsStyleType);
                }
                */

                Plane Geomplane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
                SketchPlane sketch = SketchPlane.Create(doc, Geomplane);

                /*
                // Draw bounding boxes
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
                */

                /*
                // Draw Axes
                Debug.Print("Axes all together: " + strays.Count.ToString());
                foreach (Line centerLine in strays)
                {
                    ModelCurve modelline = doc.Create.NewModelCurve(centerLine, sketch) as ModelCurve;
                }
                */

                // Draw Regions
                
                foreach (CurveArray loop in loops)
                {
                    foreach (Curve edge in loop)
                    {
                        ModelCurve modelline = doc.Create.NewModelCurve(edge, sketch) as ModelCurve;
                    }
                }
                
                foreach (Curve edge in arrayPerimeter)
                {
                    DetailLine axis = doc.Create.NewDetailCurve(view, edge) as DetailLine;
                    GraphicsStyle gs = axis.LineStyle as GraphicsStyle;
                    gs.GraphicsStyleCategory.LineColor = new Color(202, 51, 82);
                    gs.GraphicsStyleCategory.SetLineWeight(8, gs.GraphicsStyleType);
                }


                tx.Commit();
            }

            return Result.Succeeded;
        }
    }
}
