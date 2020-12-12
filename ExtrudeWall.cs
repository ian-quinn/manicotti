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
    public class ExtrudeWall : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            // Access current selection
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
            var doubleCurves = importCurves.Where(x => x.LineStyle.Name == "WALL").ToList();
            List<Line> doubleLines = new List<Line>();
            foreach (CurveElement ce in doubleCurves)
            {
                doubleLines.Add(ce.GeometryCurve as Line);
            }


            // Grab the current building level
            FilteredElementCollector colLevels = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.INVALID)
                .OfClass(typeof(Level));
            Level firstLevel = colLevels.FirstElement() as Level;


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
                        if (Algorithm.IsParallel(doubleLines[i], doubleLines[i + j])
                            && !Algorithm.IsIntersected(doubleLines[i], doubleLines[i + j]))
                        {
                            // Imperical Units within Revit API
                            if (Algorithm.LineSpacing(doubleLines[i], doubleLines[i + j]) < 0.65617 + bias
                            && Algorithm.LineSpacing(doubleLines[i], doubleLines[i + j]) > 0.65617 - bias
                            && Algorithm.IsShadowing(doubleLines[i], doubleLines[i + j]))
                            {
                                if (Algorithm.GenerateAxis(doubleLines[i], doubleLines[i + j]) != null)
                                {
                                    axes.Add(Algorithm.GenerateAxis(doubleLines[i], doubleLines[i + j]));
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
                            if (Algorithm.IsOverlapping(element, sublist))
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
                    Line merged = Algorithm.MergeLine(axisBundle);
                    Wall.Create(doc, merged, firstLevel.Id, true);
                }

                tx.Commit();
            }

            return Result.Succeeded;
        }
    }
}
