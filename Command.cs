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

namespace Manicotti
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        // Check the parallel lines
        public bool IsParallel(Line line1, Line line2)
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
        public bool IsShadowing(Line line1, Line line2)
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
        public bool IsIntersected(Line line1, Line line2)
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

        // Check a line is overlappint with a group of lines
        public bool IsOverlapping(Line line, List<Line> list)
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
        public bool IsCrossing(Line line, List<Line> list)
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
        public double LineSpacing(Line line1, Line line2)
        {
            XYZ midPt = line1.Evaluate(0.5, true);
            Line target = line2.Clone() as Line;
            target.MakeUnbound();
            double spacing = target.Distance(midPt);
            return spacing;
        }

        // Generate axis / offset version
        public Line GenerateAxis(Line line1, Line line2)
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
        public List<XYZ> BubbleSort(List<XYZ> pts)
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
        public Line MergeLine(List<Line> lines)
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
        public XYZ GrabCenterPt(List<Line> lines)
        {
            XYZ ptSum = new XYZ(0, 0, 0);
            foreach (Line line in lines)
            {
                ptSum += line.GetEndPoint(0);
                ptSum += line.GetEndPoint(1);
            }
            XYZ centerPt = ptSum / lines.Count / 2;
            return centerPt;
        }

        // ###################################################################
        // ###################################################################
        // ###################################################################

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            // Access current selection
            Selection sel = uidoc.Selection;

            // Extraction of CurveElements by LineStyle WALL & COLUMN
            CurveElementFilter filter = new CurveElementFilter(CurveElementType.ModelCurve);
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            ICollection<Element> founds = collector.WherePasses(filter).ToElements();
            List<CurveElement> importCurves = new List<CurveElement>();

            foreach (CurveElement ce in founds)
            {
                importCurves.Add(ce);
            }
            var doubleCurves = importCurves.Where(x => x.LineStyle.Name == "WALL").ToList();
            List<Line> doubleLines = new List<Line>();
            foreach (CurveElement ce in doubleCurves)
            {
                doubleLines.Add(ce.GeometryCurve as Line);
            }
            var columnCurves = importCurves.Where(x => x.LineStyle.Name == "COLUMN").ToList();
            List<Line> columnLines = new List<Line>();
            foreach (CurveElement ce in columnCurves)
            {
                columnLines.Add(ce.GeometryCurve as Line);
            }


            // Grab the current building level
            FilteredElementCollector colLevels = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.INVALID)
                .OfClass(typeof(Level));
            Level firstLevel = colLevels.FirstElement() as Level;


            // Grab the columntype
            FilteredElementCollector colColumns = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol));
            //    .OfCategory(BuiltInCategory.OST_Columns);
            // OST handles the internal family types, maybe?
            FamilySymbol column_demo = colColumns.FirstElement() as FamilySymbol;
            // Use default setting to avoid error handling, which is a lack of the line below
            //FamilySymbol column_demo = columnTypes.Find((FamilySymbol fs) => { return fs.Name == "Column_demo"});
            foreach (FamilySymbol columnType in colColumns)
            {
                //Debug.Print(columnType.Name);
                if (columnType.Name == "Column_demo")
                {
                    column_demo = columnType as FamilySymbol;
                    break;
                }
            }


            // Modify document within a transaction
            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Generate Walls");

                // Bundle double lines and generate their axes
                List<Line> axes = new List<Line>();
                double bias = 0.01;

                for (int i = 0; i < doubleLines.Count; i++)
                {
                    for (int j = 0; j < doubleLines.Count - i; j++)
                    {
                        if (IsParallel(doubleLines[i], doubleLines[i + j])
                            && !IsIntersected(doubleLines[i], doubleLines[i + j]))
                        {
                            // Imperical Units within Revit API
                            if (LineSpacing(doubleLines[i], doubleLines[i + j]) < 0.65617 + bias
                            && LineSpacing(doubleLines[i], doubleLines[i + j]) > 0.65617 - bias
                            && IsShadowing(doubleLines[i], doubleLines[i + j]))
                            {
                                if (GenerateAxis(doubleLines[i], doubleLines[i + j]) != null)
                                {
                                    axes.Add(GenerateAxis(doubleLines[i], doubleLines[i + j]));
                                }
                                Debug.Print(doubleLines[i].Length.ToString() + " | " + doubleLines[i + j].Length.ToString());
                            }
                        }
                    }
                }

                // Axis merge / 
                List<List<Line>> axisGroups = new List<List<Line>>();
                axisGroups.Add(new List<Line>() { axes[0] });

                while (axes.Count != 0)
                {
                    foreach (Line element in axes)
                    {
                        int iterCounter = 0;
                        foreach (List<Line> sublist in axisGroups)
                        {
                            iterCounter += 1;
                            if (IsOverlapping(element, sublist))
                            {
                                sublist.Add(element);
                                axes.Remove(element);
                                goto a;
                            }
                            if (iterCounter == axisGroups.Count)
                            {
                                axisGroups.Add(new List<Line>() { element });
                                axes.Remove(element);
                                goto a;
                            }
                        }
                    }
                a:;
                }
                Debug.Print("The merged axes number " + axisGroups.Count.ToString());


                // Wall generation
                foreach (List<Line> axisBundle in axisGroups)
                {
                    Line merged = MergeLine(axisBundle);
                    Wall.Create(doc, merged, firstLevel.Id, true);
                }


                // Column basepoint
                List<List<Line>> columnGroups = new List<List<Line>>();
                columnGroups.Add(new List<Line>() { });
                while (columnLines.Count != 0)
                {
                    foreach (Line element in columnLines)
                    {
                        int iterCounter = 0;
                        foreach (List<Line> sublist in columnGroups)
                        {
                            iterCounter += 1;
                            if (IsCrossing(element, sublist))
                            {
                                sublist.Add(element);
                                columnLines.Remove(element);
                                goto a;
                            }
                            if (iterCounter == columnGroups.Count)
                            {
                                columnGroups.Add(new List<Line>() { element });
                                columnLines.Remove(element);
                                goto a;
                            }
                        }
                    }
                a:;
                }


                // activate
                if (!column_demo.IsActive)
                {
                    column_demo.Activate();
                }

                // Column generation
                foreach (List<Line> baselines in columnGroups)
                {
                    XYZ columnCenterPt = GrabCenterPt(baselines);
                    doc.Create.NewFamilyInstance(columnCenterPt, column_demo, firstLevel, Autodesk.Revit.DB.Structure.StructuralType.Column);
                }


                tx.Commit();
            }

            return Result.Succeeded;
        }
    }
}
