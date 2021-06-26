#region Namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.DB;
#endregion

namespace Manicotti.Util
{
    public class FailureSwallower : IFailuresPreprocessor
    {

        private List<FailureDefinitionId> _failureIdList = new List<FailureDefinitionId>();
        private bool deleteErrors = false;
        private bool deleteWarnings = false;

        public FailureSwallower(bool deleteWarings, bool deleteErrors)
        {
            this.deleteWarnings = deleteWarings;
            this.deleteErrors = deleteErrors;
        }
        public FailureSwallower(FailureDefinitionId id)
        {
            _failureIdList.Add(id);
        }
        public FailureSwallower(List<FailureDefinitionId> idList)
        {
            this._failureIdList = idList;
        }

        FailureProcessingResult IFailuresPreprocessor.PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            IList<FailureMessageAccessor> failList = failuresAccessor.GetFailureMessages();

            if (_failureIdList.Count == 0)
            {
                failuresAccessor.DeleteAllWarnings();
                if (deleteErrors)
                {
                    foreach (FailureMessageAccessor accessor in failList)
                    {
                        if (accessor.GetSeverity() == FailureSeverity.Error)
                        {
                            var ids = accessor.GetFailingElementIds();
                            failuresAccessor.DeleteElements((IList<ElementId>)ids.GetEnumerator());
                        }
                    }
                }
            }
            else
            {
                foreach (FailureMessageAccessor failure in failList)
                {
                    FailureDefinitionId failId = failure.GetFailureDefinitionId();
                    if (_failureIdList.Exists(p => p == failId))
                    {
                        failuresAccessor.DeleteWarning(failure);
                    }
                }
            }
            return FailureProcessingResult.Continue;
        }

    }
}
