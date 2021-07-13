#region Namespaces
using System.IO;
using System.Xml;

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion

namespace Manicotti
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    class CmdPartAtom : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            string GetFamilyName(string path)
            {
                XmlDocument docXml = new XmlDocument();
                docXml.Load(path);
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(docXml.NameTable);
                nsmgr.AddNamespace("ex", "http://www.w3.org/2005/Atom");
                nsmgr.AddNamespace("A", "urn:schemas-autodesk-com:partatom");

                XmlNode node = docXml.SelectSingleNode("/ex:entry/ex:title", nsmgr);
                if (node != null)
                {
                    return node.InnerText;
                }
                else
                {
                    return null;
                }
            }

            string CreatePartAtom(string familyFilePath)
            {
                if (familyFilePath == null)
                {
                    return null;
                }
                else
                {
                    string folder = Path.GetDirectoryName(familyFilePath);
                    string name = Path.GetFileName(familyFilePath);
                    string xmlPath = folder + "\\" + name + ".xml";
                    app.ExtractPartAtomFromFamilyFile(familyFilePath, xmlPath);
                    return xmlPath;
                }
            }


            if (Properties.Settings.Default.url_columnRect == null ||
                Properties.Settings.Default.url_columnRound == null ||
                Properties.Settings.Default.url_door == null ||
                Properties.Settings.Default.url_window == null)
            {
                System.Windows.MessageBox.Show("Family files not defined", "Tips");
                return Result.Cancelled;
            }

            if (!File.Exists(Properties.Settings.Default.url_columnRect) ||
                !File.Exists(Properties.Settings.Default.url_columnRound) ||
                !File.Exists(Properties.Settings.Default.url_door) ||
                !File.Exists(Properties.Settings.Default.url_window))
            {
                System.Windows.MessageBox.Show("Family URL may not be valid", "Tips");
                return Result.Cancelled;
            }

            Transaction trans = new Transaction(doc, "Extract Part Atom");
            trans.Start();
            string name_columnRect = CreatePartAtom(Properties.Settings.Default.url_columnRect);
            string name_columnRound = CreatePartAtom(Properties.Settings.Default.url_columnRound);
            string name_door = CreatePartAtom(Properties.Settings.Default.url_door);
            string name_window = CreatePartAtom(Properties.Settings.Default.url_window);
            trans.Commit();

            if (name_columnRect != null)
            {
                Properties.Settings.Default.name_columnRect = GetFamilyName(name_columnRect);
            }
            if (name_columnRound != null)
            {
                Properties.Settings.Default.name_columnRound = GetFamilyName(name_columnRound);
            }
            if (name_door != null)
            {
                Properties.Settings.Default.name_door = GetFamilyName(name_door);
            }
            if (name_window != null)
            {
                Properties.Settings.Default.name_window = GetFamilyName(name_window);
            }
            Properties.Settings.Default.Save();

            return Result.Succeeded;
        }
    }
}
