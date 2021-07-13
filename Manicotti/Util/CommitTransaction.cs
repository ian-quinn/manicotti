#region Namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.DB;
#endregion

namespace Manicotti
{
    /// <summary>
    /// Abandoned for now.
    /// Correlated functions are within:
    /// FailureCollector / FailureSwallower
    /// </summary>
    static class CommitTransaction
    {
        public static void CommitTrans(this Transaction transaction, bool deleteWarning = true, bool deleteError = false)
        {
            WarningSwallower failuresPreprocessor = new WarningSwallower(deleteWarning, deleteError);
            transaction.commitTransaction(failuresPreprocessor);
        }
        public static void commitTransaction(this Transaction transaction, WarningSwallower failuresPreprocessor)
        {
            FailureHandlingOptions failureOptions = transaction.GetFailureHandlingOptions();
            failureOptions.SetFailuresPreprocessor(failuresPreprocessor);
            failureOptions.SetDelayedMiniWarnings(true);
            failureOptions.SetClearAfterRollback(true);

            transaction.SetFailureHandlingOptions(failureOptions);
            transaction.Commit();
        }

    }
    public class WarningSwallower : IFailuresPreprocessor
    {

        private List<FailureDefinitionId> _failureIdList = new List<FailureDefinitionId>();
        private bool deleteErrors = false;
        private bool deleteWarnings = false;

        public WarningSwallower(bool deleteWarings, bool deleteErrors)
        {
            this.deleteWarnings = deleteWarings;
            this.deleteErrors = deleteErrors;
        }
        public WarningSwallower(FailureDefinitionId id)
        {
            _failureIdList.Add(id);
        }
        public WarningSwallower(List<FailureDefinitionId> idList)
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