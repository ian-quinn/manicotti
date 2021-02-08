#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.DB;
#endregion

namespace Manicotti
{
    public class Algorithm
    {
        // Check the parallel lines
        public static bool IsParallel(Line line1, Line line2)
        {
            XYZ line1_Direction = line1.Direction.Normalize();
            XYZ line2_Direction = line2.Direction.Normalize();
            if (line1_Direction.IsAlmostEqualTo(line2_Direction) ||
                line1_Direction.Negate().IsAlmostEqualTo(line2_Direction))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        // Check the shadowing lines
        public static bool IsShadowing(Line line1, Line line2)
        {
            Line shorter = line1.Clone() as Line;
            Line longer = line2.Clone() as Line;
            if (shorter.Length > longer.Length)
            {
                (shorter, longer) = (longer, shorter);
            }
            XYZ start = shorter.GetEndPoint(0);
            XYZ end = shorter.GetEndPoint(1);
            Line target = longer.Clone() as Line;
            target.MakeUnbound();
            XYZ start_s = target.Project(start).XYZPoint;
            XYZ end_s = target.Project(end).XYZPoint;
            Line start_line = Line.CreateUnbound(start, start_s - start);
            Line end_line = Line.CreateUnbound(end, end_s - end);
            if (IsIntersected(start_line, longer) || IsIntersected(end_line, longer))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        // Check the intersected lines
        public static bool IsIntersected(Line line1, Line line2)
        {
            SetComparisonResult result = line1.Intersect(line2, out IntersectionResultArray results);
            if (result == SetComparisonResult.Overlap
                || result == SetComparisonResult.Subset
                || result == SetComparisonResult.Superset
                || result == SetComparisonResult.Equal)
            {
                return true;
            }
            else
            {
                return false;
            }
            /*
            if (results == null)
            {
                return false;
            }
            */
        }

        // Check a line is overlapping with a group of lines
        public static bool IsOverlapping(Line line, List<Line> list)
        {
            int judgement = 0;
            foreach (Line element in list)
            {
                if (IsParallel(line, element) && IsIntersected(line, element))
                {
                    judgement += 1;
                }
            }
            if (judgement == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        // Check whether a line is intersected with a bunch of lines
        public static bool IsCrossing(Line line, List<Line> list)
        {
            int judgement = 0;
            if (list.Count == 0)
            {
                return true;
            }
            else
            {
                foreach (Line element in list)
                {
                    if (IsIntersected(line, element))
                    {
                        judgement += 1;
                    }
                }
            }

            if (judgement == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        // Calculate the distance of bound lines
        public static double LineSpacing(Line line1, Line line2)
        {
            XYZ midPt = line1.Evaluate(0.5, true);
            Line target = line2.Clone() as Line;
            target.MakeUnbound();
            double spacing = target.Distance(midPt);
            return spacing;
        }

        // Generate axis / offset version
        public static Line GenerateAxis(Line line1, Line line2)
        {
            Curve baseline = line1.Clone();
            Curve targetline = line2.Clone();
            if (line1.Length < line2.Length)
            {
                (baseline, targetline) = (line2.Clone(), line1.Clone());
            }
            targetline.MakeUnbound();
            XYZ midPt = baseline.Evaluate(0.5, true);
            XYZ midPt_proj = targetline.Project(midPt).XYZPoint;
            XYZ vec = (midPt_proj - midPt) / 2;
            double offset = vec.GetLength() / 2.0;
            Debug.Print(offset.ToString());
            if (offset != 0)
            {
                Line axis = Line.CreateBound(baseline.GetEndPoint(0) + vec, baseline.GetEndPoint(1) + vec);
                return axis;
            }
            else
            {
                return null;
            }
            //Line axis = baseline.CreateOffset(offset, vec.Normalize()) as Line;
        }

        // Bubble sort algorithum
        public static List<XYZ> BubbleSort(List<XYZ> pts)
        {
            double threshold = 0.01;
            for (int i = 0; i < pts.Count(); i++)
            {
                for (int j = 0; j < pts.Count() - 1; j++)
                {
                    if (pts[j].X > pts[j + 1].X + threshold)
                    {
                        (pts[j], pts[j + 1]) = (pts[j + 1], pts[j]);
                    }
                }
            }
            for (int i = 0; i < pts.Count(); i++)
            {
                for (int j = 0; j < pts.Count() - 1; j++)
                {
                    if (pts[j].Y > pts[j + 1].Y + threshold)
                    {
                        (pts[j], pts[j + 1]) = (pts[j + 1], pts[j]);
                    }
                }
            }
            return pts;
        }

        // Merge axis
        public static Line MergeLine(List<Line> lines)
        {
            List<XYZ> pts = new List<XYZ>();
            foreach (Line line in lines)
            {
                pts.Add(line.GetEndPoint(0));
                pts.Add(line.GetEndPoint(1));
            }
            List<XYZ> ptsSorted = BubbleSort(pts);
            Line mergedLine = Line.CreateBound(ptsSorted.First(), ptsSorted.Last());
            return mergedLine;
        }

        // Center point of list of lines
        public static XYZ GrabCenterPt(List<Line> lines)
        {
            double ptSum_X = 0;
            double ptSum_Y = 0;
            double ptSum_Z = lines[0].GetEndPoint(0).Z;
            foreach (Line line in lines)
            {
                ptSum_X += line.GetEndPoint(0).X;
                ptSum_X += line.GetEndPoint(1).X;
                ptSum_Y += line.GetEndPoint(0).Y;
                ptSum_Y += line.GetEndPoint(1).Y;
            }
            XYZ centerPt = new XYZ(ptSum_X / lines.Count / 2, ptSum_Y / lines.Count / 2, ptSum_Z);
            return centerPt;
        }

        // Retrieve the width and depth of a rectangle
        // Update will be soon
        public static Tuple<double, double> GrabSizeOfRectangle(List<Line> lines)
        {
            double width = Util.FootToMm(lines[0].Length);
            double depth = Util.FootToMm(lines[1].Length);
            return new Tuple<double, double>(width, depth);
        }
    }
}
