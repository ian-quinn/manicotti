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
        public static void Execute(UIApplication uiapp, List<Curve> wallCrvs, Level level, bool IsSilent)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;


            // Bundle double lines and generate their axes
            List<Curve> axes = new List<Curve>();
            double bias = Misc.MmToFoot(20);

            var doubleLines = Misc.CrvsToLines(wallCrvs);
            for (int i = 0; i < doubleLines.Count; i++)
            {
                for (int j = 0; j < doubleLines.Count - i; j++)
                {
                    if (Algorithm.IsParallel(doubleLines[i], doubleLines[i + j])
                        && !Algorithm.IsIntersected(doubleLines[i], doubleLines[i + j]))
                    {
                        // Imperical Units within Revit API
                        if (Algorithm.LineSpacing(doubleLines[i], doubleLines[i + j]) < Misc.MmToFoot(200) + bias
                        && Algorithm.LineSpacing(doubleLines[i], doubleLines[i + j]) > Misc.MmToFoot(200) - bias
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
            List<Curve> mergedAxes = Algorithm.MergeAxes(axes);
            Debug.Print("The merged axes number " + mergedAxes.Count.ToString());

            string task = "Creating Walls...";
            string caption = "Extrude Walls";
            int n = mergedAxes.Count;

            /*using (Transaction tx = new Transaction(doc, "Generate walls"))
            {
                tx.Start();

                // Wall generation
                foreach (Curve axis in mergedAxes)
                {
                    Wall.Create(doc, axis, level.Id, true);
                }

                tx.Commit();
            }*/

            if (IsSilent == true)
            {
                using (Transaction tx = new Transaction(doc, "Generate a wall"))
                {
                    FailureHandlingOptions options = tx.GetFailureHandlingOptions();
                    options.SetFailuresPreprocessor(new Util.FailureSwallower(false, false));
                    tx.SetFailureHandlingOptions(options);

                    tx.Start();
                    foreach (Curve axis in mergedAxes)
                    {
                        Wall.Create(doc, axis, level.Id, true);
                    }
                    tx.Commit();
                }
            }

            else
            {
                Views.ProgressBar pb = new Views.ProgressBar(caption, task, n);
                foreach (Curve axis in mergedAxes)
                {
                    using (Transaction tx = new Transaction(doc, "Generate a wall"))
                    {
                        tx.Start();
                        Wall.Create(doc, axis, level.Id, true);
                        tx.Commit();
                    }
                    pb.Increment();
                    if (pb.ProcessCancelled) { break; }
                }
                pb.JobCompleted();
            }
            
        }
    }
}
