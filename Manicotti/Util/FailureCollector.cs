﻿#region Namespaces
using System.Collections.Generic;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion

namespace Manicotti.Util
{
    /// <summary>
    /// Collect all failure message description strings.
    /// </summary>
    class FailureCollector : IFailuresPreprocessor
    {
        List<string> FailureList { get; set; }

        public FailureCollector()
        {
            FailureList = new List<string>();
        }

        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            foreach (FailureMessageAccessor fMA in failuresAccessor.GetFailureMessages())
            {
                FailureList.Add(fMA.GetDescriptionText());
                FailureDefinitionId FailDefID
                  = fMA.GetFailureDefinitionId();

                //if (FailDefID == BuiltInFailures
                //  .GeneralFailures.DuplicateValue)
                //    failuresAccessor.DeleteWarning(fMA);
            }
            return FailureProcessingResult.Continue;
        }

        public void ShowDialogue()
        {
            string s = string.Join("\r\n", FailureList);
            TaskDialog.Show("Post Processing Failures:", s);
        }
    }

    //[Transaction(TransactionMode.Manual)]
    //class FailureCollector : IExternalCommand
    //{
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        UIApplication uiApp = commandData.Application;
    //        Document doc = uiApp.ActiveUIDocument.Document;

    //        MessageDescriptionGatheringPreprocessor pp = new MessageDescriptionGatheringPreprocessor();

    //        using (Transaction t = new Transaction(doc))
    //        {
    //            FailureHandlingOptions ops = t.GetFailureHandlingOptions();

    //            ops.SetFailuresPreprocessor(pp);
    //            t.SetFailureHandlingOptions(ops);

    //            t.Start("Marks");

    //            // Generate a 'duplicate mark' warning message:

    //            IList<Element> specEqu = new FilteredElementCollector(doc)
    //                .OfCategory(BuiltInCategory.OST_SpecialityEquipment)
    //                .WhereElementIsNotElementType()
    //                .ToElements();

    //            if (specEqu.Count >= 2)
    //            {
    //                for (int i = 0; i < 2; i++)
    //                    specEqu[i].get_Parameter(
    //                      BuiltInParameter.ALL_MODEL_MARK).Set(
    //                        "Duplicate Mark");
    //            }

    //            // Generate an 'duplicate wall' warning message:

    //            Element level = new FilteredElementCollector(doc)
    //              .OfClass(typeof(Level))
    //              .FirstElement();

    //            Line line = Line.CreateBound(XYZ.Zero, 10 * XYZ.BasisX);
    //            Wall wall1 = Wall.Create(doc, line, level.Id, false);
    //            Wall wall2 = Wall.Create(doc, line, level.Id, false);

    //            t.Commit();
    //        }
    //        pp.ShowDialogue();

    //        return Result.Succeeded;
    //    }
    //}
}