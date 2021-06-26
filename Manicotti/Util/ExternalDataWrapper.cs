#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
#endregion


namespace Manicotti.Util
{
    /// <summary>
    /// 获取Revit常用对象
    /// </summary>
    public class ExternalDataWrapper
    {
        private ExternalCommandData _commandData;
        private UIApplication _uiApp;
        private UIDocument _uiDoc;
        private Application _app;
        private Selection _sel;
        private Document _doc;
        private View _view;

        public ExternalCommandData CommandData
        {
            get { return _commandData; }
        }
        public UIApplication UiApp
        {
            get { return _uiApp; }
        }
        public UIDocument UiDoc
        {
            get { return _uiDoc; }
        }
        public Application App
        {
            get { return _app; }
        }
        public Selection Sel
        {
            get { return _sel; }
        }
        public Document Doc
        {
            get { return _doc; }
        }
        public View ActiveView
        {
            get { return _view; }
        }
        public Autodesk.Revit.Creation.Application AppCreation
        {
            get { return _app.Create; }
        }
        public Autodesk.Revit.Creation.Document DocCreation
        {
            get { return _doc.Create; }
        }

        /// <summary>
        /// 通过ExternalCommandData获取Revit常用对象
        /// </summary>
        /// <param name="commandData"></param>
        public ExternalDataWrapper(ExternalCommandData commandData)
        {
            this._commandData = commandData;
            this._uiApp = commandData.Application;
            this._app = commandData.Application.Application;
            this._uiDoc = commandData.Application.ActiveUIDocument;
            this._doc = commandData.Application.ActiveUIDocument.Document;
            this._sel = commandData.Application.ActiveUIDocument.Selection;
            this._view = commandData.Application.ActiveUIDocument.Document.ActiveView;
        }

        /// <summary>
        /// 通过UIApplication获取Revit常用对象
        /// </summary>
        /// <param name="uiApp"></param>
        public ExternalDataWrapper(UIApplication uiApp)
        {
            this._commandData = null;
            this._uiApp = uiApp;
            this._app = uiApp.Application;
            this._uiDoc = uiApp.ActiveUIDocument;
            this._doc = uiApp.ActiveUIDocument.Document;
            this._sel = uiApp.ActiveUIDocument.Selection;
            this._view = uiApp.ActiveUIDocument.Document.ActiveView;
        }

    }
}
