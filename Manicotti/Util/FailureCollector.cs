#region Namespaces
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
}