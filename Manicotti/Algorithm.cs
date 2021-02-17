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
        #region XYZ method
        /// <summary>
        /// Calculate the clockwise angle from vec1 to vec2
        /// </summary>
        /// <param name="vec1"></param>
        /// <param name="vec2"></param>
        /// <returns></returns>
        public static double AngleTo2PI(XYZ vec1, XYZ vec2)
        {
            double dot = vec1.X * vec2.X + vec1.Y * vec2.Y;    // dot product between [x1, y1] and [x2, y2]
            double det = vec1.X * vec2.Y - vec1.Y * vec2.X;    // determinant
            double angle = Math.Atan2(det, dot);  // Atan2(y, x) or atan2(sin, cos)
            return angle;
        }
        
        /// <summary>
        /// Return XYZ after axis system rotation
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static XYZ PtAxisRotation2D(XYZ pt, double angle)
        {
            double Xtrans = pt.X * Math.Cos(angle) + pt.Y * Math.Sin(angle);
            double Ytrans = pt.Y * Math.Cos(angle) - pt.X * Math.Sin(angle);
            return new XYZ(Xtrans, Ytrans, pt.Z);
        }
        
        /// <summary>
        /// Check if a point is on a line
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        public static bool IsPtOnLine(XYZ pt, Line line)
        {
            XYZ ptStart = line.GetEndPoint(0);
            XYZ ptEnd = line.GetEndPoint(1);
            XYZ vec1 = (ptStart - pt).Normalize();
            XYZ vec2 = (ptEnd - pt).Normalize();
            if (vec1.IsAlmostEqualTo(vec2)) { return false; }
            else { return true; }
        }
        #endregion
        
        

        #region Curve method
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
            if (IsIntersected(start_line, longer) || IsIntersected(end_line, longer)) { return true; }
            else { return false; }
        }
        
        /// <summary>
        /// Check if two curves are strictly intersected
        /// </summary>
        /// <param name="crv1"></param>
        /// <param name="crv2"></param>
        /// <returns></returns>
        public static bool IsIntersected(Curve crv1, Curve crv2)
        {
            // Can be safely apply to lines
            // Line segment can only have 4 comparison results: Disjoint, subset, overlap, equal
            SetComparisonResult result = crv1.Intersect(crv2, out IntersectionResultArray results);
            if (result == SetComparisonResult.Overlap
                || result == SetComparisonResult.Subset
                || result == SetComparisonResult.Superset
                || result == SetComparisonResult.Equal)
            { return true; }
            else { return false; }
        }
        
        /// <summary>
        /// Check if two lines are almost joined.
        /// </summary>
        /// <param name="crv1"></param>
        /// <param name="crv2"></param>
        /// <returns></returns>
        public static bool IsAlmostJoined(Line line1, Line line2)
        {
            double radius = Util.MmToFoot(50);
            XYZ ptStart = line1.GetEndPoint(0);
            XYZ ptEnd = line1.GetEndPoint(1);
            XYZ xAxis = new XYZ(1, 0, 0);   // The x axis to define the arc plane. Must be normalized
            XYZ yAxis = new XYZ(0, 1, 0);   // The y axis to define the arc plane. Must be normalized
            Curve knob1 = Arc.Create(ptStart, radius, 0, 2 * Math.PI, xAxis, yAxis);
            Curve knob2 = Arc.Create(ptEnd, radius, 0, 2 * Math.PI, xAxis, yAxis);
            SetComparisonResult result1 = knob1.Intersect(line2, out IntersectionResultArray results1);
            SetComparisonResult result2 = knob2.Intersect(line2, out IntersectionResultArray results2);
            if (result1 == SetComparisonResult.Overlap || result2 == SetComparisonResult.Overlap)
            { return true; }
            else { return false; }
        }

        // Check a line is overlapping with a group of lines
        public static bool IsLineIntersectLines(Line line, List<Line> list)
        {
            int judgement = 0;
            foreach (Line element in list)
            {
                if (IsIntersected(line, element))
                {
                    judgement += 1;
                }
            }
            if (judgement == 0) { return false; }
            else { return true; }
        }
        
        // Check a line is overlapping with a group of lines
        public static bool IsLineOverlapLines(Line line, List<Line> list)
        {
            int judgement = 0;
            foreach (Line element in list)
            {
                if (IsParallel(line, element) && IsIntersected(line, element))
                {
                    judgement += 1;
                }
            }
            if (judgement == 0) { return false; }
            else { return true; }
        }
        
        // Check a line is parallel with a group of lines
        public static bool IsLineParallelLines(Line line, List<Line> list)
        {
            int judgement = 0;
            foreach (Line element in list)
            {
                if (IsParallel(line, element))
                {
                    judgement += 1;
                }
            }
            if (judgement == 0) { return false; }
            else { return true; }
        }
        
        // Check a line is almost joined to a group of lines
        public static bool IsLineAlmostJoinedLines(Line line, List<Line> list)
        {
            int judgement = 0;
            if (list.Count == 0) { return true; }
            else
            {
                foreach (Line element in list)
                {
                    if (IsAlmostJoined(line, element))
                    {
                        judgement += 1;
                    }
                }
            }
            if (judgement == 0) { return false; }
            else { return true; }
        }

        // Check a line is almost subset to a group of lines
        public static bool IsLineAlmostSubsetLines(Line line, List<Line> list)
        {
            int judgement = 0;
            if (list.Count == 0) { return true; }
            else
            {
                foreach (Line element in list)
                {
                    if (IsParallel(line, element) && IsAlmostJoined(line, element))
                    {
                        judgement += 1;
                    }
                }
            }
            if (judgement == 0) { return false; }
            else { return true; }
        }


        /// <summary>
        /// Recreate a line in replace of the joining/overlapping lines
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public static Line FuseLines(List<Line> lines)
        {
            double Z = lines[0].GetEndPoint(0).Z;
            List<XYZ> pts = new List<XYZ>();
            foreach (Line line in lines)
            {
                pts.Add(line.GetEndPoint(0));
                pts.Add(line.GetEndPoint(1));
            }
            double Xmin = double.PositiveInfinity;
            double Xmax = double.NegativeInfinity;
            double Ymin = double.PositiveInfinity;
            double Ymax = double.NegativeInfinity;
            foreach (XYZ pt in pts)
            {
                if (pt.X < Xmin) { Xmin = pt.X; }
                if (pt.X > Xmax) { Xmax = pt.X; }
                if (pt.Y < Ymin) { Ymin = pt.Y; }
                if (pt.Y > Ymax) { Ymax = pt.Y; }
            }
            return Line.CreateBound(new XYZ(Xmin, Ymin, Z), new XYZ(Xmax, Ymax, Z));
        }
        
        /// <summary>
        /// Cluster curves if they are almost intersected
        /// </summary>
        /// <param name="crvs"></param>
        /// <returns></returns>
        public static List<List<Curve>> ClusterByIntersect(List<Curve> crvs)
        {
            // Check whether a line is intersected with a bunch of lines
            bool IsCrvIntersectCrvs(Curve crv, List<Curve> list)
            {
                int judgement = 0;
                // shit here
                if (list.Count == 0) { return true; }
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
                if (judgement == 0) { return false; }
                else { return true; }
            }

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
                crv0 = RegionDetect.ExtendCrv(crv0, 0.1);
                Curve crv1 = crv0.CreateTransformed(up);
                Curve crv2 = crv0.CreateTransformed(down);
                Curve crv3 = crv0.CreateTransformed(left);
                Curve crv4 = crv0.CreateTransformed(right);
                int iterCount = -1;
                for (int j = 0; j < clusters.Count; j++)
                {
                    iterCount = j;
                    if (IsCrvIntersectCrvs(crv0, clusters[j]) ||
                        IsCrvIntersectCrvs(crv1, clusters[j]) ||
                        IsCrvIntersectCrvs(crv2, clusters[j]) ||
                        IsCrvIntersectCrvs(crv3, clusters[j]) ||
                        IsCrvIntersectCrvs(crv4, clusters[j]))
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
        
        /// <summary>
        /// Cluster lines if they are almost joined at end point.
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public static List<List<Line>> ClusterByKnob(List<Line> lines)
        {
            List<List<Line>> clusters = new List<List<Line>> { };
            clusters.Add(new List<Line> { lines[0] });
            for (int i = 1; i < lines.Count; i++)
            {
                if (null == lines[i]) { continue; }
                foreach (List<Line> cluster in clusters)
                {
                    if (IsLineAlmostJoinedLines(lines[i], cluster))
                    {
                        cluster.Add(lines[i]);
                        goto a;
                    }
                }
                clusters.Add(new List<Line> { lines[i] });
            a:
                continue;
            }
            return clusters;
        }
        
        /// <summary>
        /// Get points of curves
        /// </summary>
        /// <param name="crvs"></param>
        /// <returns></returns>
        public static List<XYZ> GetPtsOfCrvs(List<Curve> crvs)
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

        
        #endregion



        #region Region method
        /// <summary>
        /// Return the bounding box of curves. 
        /// The box has the minimum area with axis in align with the curve direction.
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
            List<XYZ> pts = GetPtsOfCrvs(crvs);
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
                    double Xtrans = PtAxisRotation2D(pt, angle).X;
                    double Ytrans = PtAxisRotation2D(pt, angle).Y;
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
                XYZ pt1 = PtAxisRotation2D(new XYZ(X0, Y0, ZAxis), -deflection);
                XYZ pt2 = PtAxisRotation2D(new XYZ(X1, Y0, ZAxis), -deflection);
                XYZ pt3 = PtAxisRotation2D(new XYZ(X1, Y1, ZAxis), -deflection);
                XYZ pt4 = PtAxisRotation2D(new XYZ(X0, Y1, ZAxis), -deflection);
                Curve crv1 = Line.CreateBound(pt1, pt2) as Curve;
                Curve crv2 = Line.CreateBound(pt2, pt3) as Curve;
                Curve crv3 = Line.CreateBound(pt3, pt4) as Curve;
                Curve crv4 = Line.CreateBound(pt4, pt1) as Curve;
                List<Curve> boundingBox = new List<Curve> { crv1, crv2, crv3, crv4 };
                return boundingBox;
            }
        }

        // Center point of list of lines
        // Need upgrade to polygon center point method
        public static XYZ GetCenterPt(List<Line> lines)
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
        public static Tuple<double, double, double> GetSizeOfRectangle(List<Line> lines)
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

            return Tuple.Create(Math.Round(width, 2), Math.Round(depth, 2), rotations.Min());
            // clockwise rotation in radian measure
            // x pointing right and y down as is common for computer graphics
            // this will mean you get a positive sign for clockwise angles
        }

        // 
        public static CurveArray RectifyPolygon(List<Line> lines)
        {
            CurveArray boundary = new CurveArray();
            List<XYZ> vertices = new List<XYZ>() { };
            vertices.Add(lines[0].GetEndPoint(0));
            foreach (Line line in lines)
            {
                XYZ ptStart = line.GetEndPoint(0);
                XYZ ptEnd = line.GetEndPoint(1);
                if (vertices.Last().IsAlmostEqualTo(ptStart))
                {
                    vertices.Add(ptEnd);
                    continue;
                }
                if (vertices.Last().IsAlmostEqualTo(ptEnd))
                {
                    vertices.Add(ptStart);
                    continue;
                }
            }
            Debug.Print("number of vertices: " + vertices.Count());
            foreach (XYZ pt in vertices)
            {
                Debug.Print(Util.PrintXYZ(pt));
            }
            for (int i = 0; i < lines.Count; i++)
            {
                boundary.Append(Line.CreateBound(vertices[i], vertices[i + 1]));
            }
            return boundary;
        }

        // 
        public static Tuple<double, double, double> GetSizeOfFootprint(List<Line> lines)
        {
            return null;
        }
        #endregion



        // Here collects obsolete methods
        #region Trashbin
        /// <summary>
        /// Bubble sort algorithm. Need upgrade. Cannot apply to vertical/horizontal points mushed up
        /// </summary>
        /// <param name="pts"></param>
        /// <returns></returns>
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
        #endregion
    }
}
