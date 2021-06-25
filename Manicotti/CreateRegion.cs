using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Manicotti
{
    [Transaction(TransactionMode.Manual)]
    public static class CreateRegion
    {
        // Main thread
        public static CurveArray Execute(UIApplication uiapp, List<Curve> wallCrvs, List<Curve> columnCrvs, 
            List<Curve> windowCrvs, List<Curve> doorCrvs)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;
            View view = doc.ActiveView;
            
            
            List<Line> columnLines = Misc.CrvsToLines(columnCrvs);

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
            List<Curve> mergeLines = MeshPatch.CloseGapAtBreakpoint(wallCrvs);

            // 
            List<Curve> fixedLines = MeshPatch.CloseGapAtCorner(mergeLines);

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
                                    if (null != MeshPatch.ExtendLine(centerLine, columnGroup[j]))
                                    {
                                        nestLines.Add(MeshPatch.ExtendLine(centerLine, columnGroup[j]));
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
                var merged = Algorithm.FuseLines(tempStray);
                strays.Add(merged);
            }
            
            // The RegionCluster method should be applied to each cluster of the strays
            // It only works on a bunch of intersected line segments
            List<CurveArray> loops = RegionDetect.RegionCluster(strays);
            // The boolean union method of the loops needs to fix
            var perimeter = RegionDetect.GetBoundary(loops);

            var recPerimeter = MeshPatch.CloseGapAtBreakpoint(perimeter);

            #endregion
            // OUTPUT List<CurveArray> loops
            Debug.Print("REGION COMPLETE!");
            

            return RegionDetect.AlignCrv(recPerimeter);
        }
    }
}
