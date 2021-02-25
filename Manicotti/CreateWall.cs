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
    public static class CreateWall
    {
        public static void Execute(UIApplication uiapp, List<Line> doubleLines, Level level)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;
            

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
                        if (Algorithm.IsLineOverlapLines(element, sublist))
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

            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Generate walls");

                // Wall generation
                foreach (List<Line> axisBundle in axisGroups)
                {
                    Line merged = Algorithm.MergeLine(axisBundle);
                    Wall.Create(doc, merged, level.Id, true);
                }

                tx.Commit();
            }
        }
    }
}
