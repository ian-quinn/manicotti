#region Namespaces
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
#endregion

namespace Manicotti.Util
{
    class ElementsOfClassSelectionFilter<T> : ISelectionFilter where T : Element
    {
        /// <summary>
        /// Allow selection of elements of type T only.
        /// </summary>
        public bool AllowElement(Element e)
        {
            return e is T;
        }

        public bool AllowReference(Reference r, XYZ p)
        {
            return true;
        }
    }
}
