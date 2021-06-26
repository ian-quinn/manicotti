#region Namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using Manicotti.Util;
#endregion

namespace Manicotti.Views
{
    [Transaction(TransactionMode.Manual)]
    class CmdFindAllFamilyInstance : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //Document Doc = commandData.Application.ActiveUIDocument.Document;
            ExternalDataWrapper wrapper = new ExternalDataWrapper(commandData);
            FindAllFamilyInstanceManager findAllFamilyInstanceManager = new FindAllFamilyInstanceManager(wrapper);
            findAllFamilyInstanceManager.Excetal();
            return Result.Succeeded;
        }
    }
}
