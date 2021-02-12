#region Namespaces
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.DB;
#endregion

namespace Manicotti
{
    public class Algorithm
    {
        // Calculate the clockwise angle from vec1 to vec2
        public static double AngleTo2PI(XYZ vec1, XYZ vec2)
        {
            double dot = vec1.X * vec2.X + vec1.Y * vec2.Y;    // dot product between [x1, y1] and [x2, y2]
            double det = vec1.X * vec2.Y - vec1.Y * vec2.X;    // determinant
            double angle = Math.Atan2(det, dot);  // Atan2(y, x) or atan2(sin, cos)
            return angle;
        }

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
        public static bool IsIntersected(Curve crv1, Curve crv2)
        {
            SetComparisonResult result = crv1.Intersect(crv2, out IntersectionResultArray results);
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
            // shit here
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

        // Check whether a line is intersected with a bunch of lines
        public static bool IsCrossingCrv(Curve crv, List<Curve> list)
        {
            int judgement = 0;
            // shit here
            if (list.Count == 0)
            {
                return true;
            }
            else
            {
                foreach (Curve element in list)
                {
                    if (IsIntersected(crv, element))
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

        // Cluster lines by intersection
        // target line will move a little bit within the tolerance to expand adjacencies
        public static List<List<Curve>> ClusterByIntersection(List<Curve> crvs)
        {
            double tolerance = 0.01;
            Transform up = Transform.CreateTranslation(tolerance * XYZ.BasisY);
            Transform down = Transform.CreateTranslation(-tolerance * XYZ.BasisY);
            Transform left = Transform.CreateTranslation(-tolerance * XYZ.BasisX);
            Transform right = Transform.CreateTranslation(tolerance * XYZ.BasisX);
            List<List<Curve>> clusters = new List<List<Curve>> { };
            clusters.Add(new List<Curve> { });
            for (int i = 0; i < crvs.Count; i++)
            {
                if (null == crvs[i]) { continue; }
                Curve crv0 = crvs[i].Clone();
                crv0 = RegionDetect.ExtendCrv(crv0, 0.05);
                Curve crv1 = crv0.CreateTransformed(up);
                Curve crv2 = crv0.CreateTransformed(down);
                Curve crv3 = crv0.CreateTransformed(left);
                Curve crv4 = crv0.CreateTransformed(right);
                int iterCount = -1;
                for (int j = 0; j < clusters.Count; j++)
                {
                    iterCount = j;
                    if (IsCrossingCrv(crv0, clusters[j]) ||
                        IsCrossingCrv(crv1, clusters[j]) ||
                        IsCrossingCrv(crv2, clusters[j]) ||
                        IsCrossingCrv(crv3, clusters[j]) ||
                        IsCrossingCrv(crv4, clusters[j]))
                    {
                        clusters[iterCount].Add(crvs[i]);
                        goto a;
                    }
                }
                clusters.Add(new List<Curve> { crvs[i] });
            a:
                continue;
            }
            return clusters;
        }

        // Get all vertices
        public static List<XYZ> FlattenToPts(List<Curve> crvs)
        {
            List<XYZ> pts = new List<XYZ> { };
            foreach (Curve crv in crvs)
            {
                XYZ ptStart = crv.GetEndPoint(0);
                XYZ ptEnd = crv.GetEndPoint(1);
                pts.Add(ptStart);
                pts.Add(ptEnd);
            }
            for (int i = 0; i < pts.Count; i++)
            {
                for (int j = pts.Count - 1; j > i; j--)
                {
                    if (pts[i].IsAlmostEqualTo(pts[j]))
                    {
                        pts.RemoveAt(j);
                    }
                }
            }
            Debug.Print("Vertices in all: " + pts.Count.ToString());
            return pts;
        }

        // Return XYZ under rotated axis system with radian angle
        public static XYZ AxisTransformPt2D(XYZ pt, double angle)
        {
            double Xtrans = pt.X * Math.Cos(angle) + pt.Y * Math.Sin(angle);
            double Ytrans = pt.Y * Math.Cos(angle) - pt.X * Math.Sin(angle);
            return new XYZ(Xtrans, Ytrans, pt.Z);
        }
        
        /// <summary>
        /// Return the bounding box of curves. The box has minimum area with axis in align with the curve direction.
        /// </summary>
        /// <param name="crvs"></param>
        /// <returns></returns>
        public static List<Curve> CreateBoundingBox2D(List<Curve> crvs)
        {
            // There can be a bounding box of an arc
            // but it is ambiguous to define the deflection of an arc-block
            // which is not the case as a door-block or window-block
            if (crvs.Count <= 1) { return null; }

            // Tolerance is to avoid generating boxes too small
            double tolerance = 0.001;
            List<XYZ> pts = FlattenToPts(crvs);
            double ZAxis = pts[0].Z;
            List<double> processions = new List<double> { };
            foreach (Curve crv in crvs)
            {
                // The Arc features no deflection of the door block
                if (crv.GetType().ToString() == "Autodesk.Revit.DB.Line")
                {
                    double angle = XYZ.BasisX.AngleTo(crv.GetEndPoint(1) - crv.GetEndPoint(0));
                    if (angle > Math.PI / 2)
                    {
                        angle = Math.PI - angle;
                    }
                    if (!processions.Contains(angle))
                    {
                        processions.Add(angle);
                    }
                }
                
            }
            //Debug.Print("Deflections in all: " + processions.Count.ToString());

            double area = double.PositiveInfinity;  // Mark the minimum bounding box area
            double deflection = 0;  // Mark the corresponding deflection angle
            double X0 = 0;
            double X1 = 0;
            double Y0 = 0;
            double Y1 = 0;
            foreach (double angle in processions)
            {
                double Xmin = double.PositiveInfinity;
                double Xmax = double.NegativeInfinity;
                double Ymin = double.PositiveInfinity;
                double Ymax = double.NegativeInfinity;
                foreach (XYZ pt in pts)
                {
                    double Xtrans = AxisTransformPt2D(pt, angle).X;
                    double Ytrans = AxisTransformPt2D(pt, angle).Y;
                    if (Xtrans < Xmin) { Xmin = Xtrans; }
                    if (Xtrans > Xmax) { Xmax = Xtrans; }
                    if (Ytrans < Ymin) { Ymin = Ytrans; }
                    if (Ytrans > Ymax) { Ymax = Ytrans; }
                }
                if (((Xmax - Xmin) * (Ymax - Ymin)) < area)
                {
                    area = (Xmax - Xmin) * (Ymax - Ymin);
                    deflection = angle;
                    X0 = Xmin;
                    X1 = Xmax;
                    Y0 = Ymin;
                    Y1 = Ymax;
                }
            }
            
            if (X1 - X0 < tolerance || Y1 - Y0 < tolerance)
            {
                Debug.Print("WARNING! Bounding box too small to be generated! ");
                return null;
            }

            else
            {
                // Inverse transformation
                XYZ pt1 = AxisTransformPt2D(new XYZ(X0, Y0, ZAxis), -deflection);
                XYZ pt2 = AxisTransformPt2D(new XYZ(X1, Y0, ZAxis), -deflection);
                XYZ pt3 = AxisTransformPt2D(new XYZ(X1, Y1, ZAxis), -deflection);
                XYZ pt4 = AxisTransformPt2D(new XYZ(X0, Y1, ZAxis), -deflection);
                Curve crv1 = Line.CreateBound(pt1, pt2) as Curve;
                Curve crv2 = Line.CreateBound(pt2, pt3) as Curve;
                Curve crv3 = Line.CreateBound(pt3, pt4) as Curve;
                Curve crv4 = Line.CreateBound(pt4, pt1) as Curve;
                List<Curve> boundingBox = new List<Curve> { crv1, crv2, crv3, crv4 };
                return boundingBox;
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
        public static Tuple<double, double, double> GrabSizeOfRectangle(List<Line> lines)
        {
            List<double> rotations = new List<double> { };  // in radian
            List<double> lengths = new List<double> { };  // in milimeter
            foreach (Line line in lines)
            {
                XYZ vec = line.GetEndPoint(1) - line.GetEndPoint(0);
                double angle = AngleTo2PI(vec, XYZ.BasisX);
                //Debug.Print("Iteration angle is " + angle.ToString());
                rotations.Add(angle);
                lengths.Add(Util.FootToMm(line.Length));
            }
            int baseEdgeId = rotations.IndexOf(rotations.Min());
            double width = lengths[baseEdgeId];
            double depth = width;
            if (width == lengths.Min()) { depth = lengths.Max(); }
            else { depth = lengths.Min(); }

            return Tuple.Create(Math.Round(width,2), Math.Round(depth,2), rotations.Min());
            // clockwise rotation in radian measure
            // x pointing right and y down as is common for computer graphics
            // this will mean you get a positive sign for clockwise angles
        }
    }
}
