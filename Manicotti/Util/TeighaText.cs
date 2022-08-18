#region Namespaces
using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using Teigha.Runtime;
using Teigha.DatabaseServices;
using Teigha.Geometry;
#endregion


namespace Manicotti.Util
{
    public static class TeighaText
    {
        // Just for the time being
        // Mixing models and methods sucks
        public class CADTextModel
        {
            private string text;
            private XYZ location;
            private double angel;
            private string layer;

            public string Text
            {
                get { return text; }
                set { text = value; }
            }

            public double Angel
            {
                get { return angel; }
                set { angel = value; }
            }

            public XYZ Location
            {
                get { return location; }
                set { location = value; }
            }

            public string Layer
            {
                get { return layer; }
                set { layer = value; }
            }
        }

        /// <summary>
        /// Get the absolute path of the linked DWG file (ver.2010 or below)
        /// </summary>
        public static string GetCADPath(UIDocument uidoc, ImportInstance import)
        {
            Document doc = uidoc.Document;
            //Element ele = import as ImportInstance;
            CADLinkType cadLinkType = doc.GetElement(import.GetTypeId()) as CADLinkType;
            return ModelPathUtils.ConvertModelPathToUserVisiblePath(cadLinkType.GetExternalFileReference().GetAbsolutePath());
        }


        /// <summary>
        /// Retreive all text data from dwg file and store them in CADModels 
        /// @CreatBridgeForRevit2018
        /// </summary>
        public static List<CADTextModel> GetCADText(string dwgPath)
        {

            List<CADTextModel> listCADModels = new List<CADTextModel>();
            using (new Services())
            {
                using (Database database = new Database(false, false))
                {
                    database.ReadDwgFile(dwgPath, FileShare.Read, true, "");
                    using (var trans = database.TransactionManager.StartTransaction())
                    {
                        using (BlockTable table = (BlockTable)database.BlockTableId.GetObject(OpenMode.ForRead))
                        {
                            using (SymbolTableEnumerator enumerator = table.GetEnumerator())
                            {
                                StringBuilder sb = new StringBuilder();
                                while (enumerator.MoveNext())
                                {
                                    using (BlockTableRecord record = (BlockTableRecord)enumerator.Current.GetObject(OpenMode.ForRead))
                                    {

                                        foreach (ObjectId id in record)
                                        {
                                            Entity entity = (Entity)id.GetObject(OpenMode.ForRead, false, false);
                                            CADTextModel model = new CADTextModel();
                                            switch (entity.GetRXClass().Name)
                                            {
                                                case "AcDbText":
                                                    DBText text = (DBText)entity;
                                                    model.Location = ConverCADPointToRevitPoint(text.Position);
                                                    model.Text = text.TextString;
                                                    Debug.Print(model.Text);
                                                    model.Angel = text.Rotation;
                                                    model.Layer = text.Layer;
                                                    listCADModels.Add(model);
                                                    break;
                                                case "AcDbMText":
                                                    MText mText = (MText)entity;
                                                    model.Location = ConverCADPointToRevitPoint(mText.Location);
                                                    model.Text = mText.Text;
                                                    model.Angel = mText.Rotation;
                                                    model.Layer = mText.Layer;
                                                    listCADModels.Add(model);
                                                    break;
                                                case "AcDbBlockReference":
                                                    BlockReference br = (BlockReference)entity;
                                                    AttributeCollection attcol = br.AttributeCollection;
                                                    foreach (ObjectId attId in attcol)
                                                    {
                                                        AttributeReference attRef = (AttributeReference)trans.GetObject(attId, OpenMode.ForRead);
                                                        if (IsLabel(attRef.TextString))
                                                        {
                                                            model.Text = attRef.TextString;
                                                            model.Location = ConverCADPointToRevitPoint(br.Position);
                                                            model.Angel = br.Rotation;
                                                            model.Layer = br.Layer;
                                                            listCADModels.Add(model);
                                                        }
                                                    }
                                                    break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return listCADModels;
        }
        
        /// <summary>
        /// Convert Point3d(Metric) in DWG to XYZ(Imperal) in Revit
        /// </summary>
        public static XYZ ConverCADPointToRevitPoint(Point3d point)
        {
            double MillimetersToUnits(double value)
            {
                return UnitUtils.ConvertToInternalUnits(value, UnitTypeId.Millimeters);
            }
            return new XYZ(MillimetersToUnits(point.X), MillimetersToUnits(point.Y), MillimetersToUnits(point.Z));
        }

        /// <summary>
        /// //
        /// </summary>
        /// <param name="str"></param>
        /// <param name="digits"></param>
        /// <returns></returns>
        public static bool IsLabel(string str)
        {
            Regex rex = new Regex(@"^[A-Z]+\d{4}$");
            if (rex.IsMatch(str))
            {
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// /////
        /// </summary>
        /// <param name="label"></param>
        /// <param name="axisLength"></param>
        /// <param name="defHeight"></param>
        /// <returns></returns>
        public static Tuple<double, double> DecodeLabel(string label, double axisLength, double defHeight)
        {
            double width = axisLength;
            double height = defHeight;
            if (IsLabel(label))
            {
                width = Convert.ToInt32(label.Substring(label.Length - 4, 2)) * 100.0;
                height = Convert.ToInt32(label.Substring(label.Length - 2)) * 100.0;
            }
            else
            {
                Debug.Print("Processing invalid label for openings. Reset to default value.");
            }
            if (width != axisLength) { width = axisLength; height = defHeight; }

            return Tuple.Create(width, height);
        }
    }
}
