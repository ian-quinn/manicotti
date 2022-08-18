#region Namespaces
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Diagnostics;

using Autodesk.Revit.DB;
#endregion

namespace Manicotti
{
    public static class Misc
    {
        #region Resource
        public static Assembly LoadFromSameFolder(object sender, ResolveEventArgs args)
        {
            string folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
            Debug.Print("########" + assemblyPath);
            if (!File.Exists(assemblyPath)) return null;
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            return assembly;
        }
        #endregion


        #region Text Processing
        public static int ExtractIndex(string str)
        {
            int index = -1;
            string numStr = "";
            for (int i = 0; i < str.Length; i++)
            {
                if (Char.IsNumber(str, i))
                {
                    numStr += str[i];
                }
                else if (ChineseToNum(str[i]) != '*')
                {
                    numStr += ChineseToNum(str[i]);
                }
            }
            if (numStr != "")
            {
                index = Convert.ToInt32(numStr);
            }
            return index;
        }

        public static Char ChineseToNum(Char num)
        {
            switch (num)
            {
                case '一': return '1';
                case '二': return '2';
                case '三': return '3';
                case '四': return '4';
                case '五': return '5';
                case '六': return '6';
                case '七': return '7';
                case '八': return '8';
                case '九': return '9';
                case '零': return '0';
                default: return '*';
            }
        }

        public static List<string> GetLayerNames(string layerChain)
        {
            List<string> names = new List<string>();
            string[] split = layerChain.Split(new string[] { ",", "." }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string name in split)
            {
                names.Add(name.Trim());
            }
            Debug.Print("##### " + ListString(names));
            return names;
        }
        #endregion // for Teigha text recognition


        #region Selection
        public static int GetLevel(string label, string key)
        {
            // Consider Regex here, maybe
            int level = -1;
            if (label.Contains(key))
            {
                level = ExtractIndex(label);
            }
            return level;
        }

        /// <summary>
        /// Return the first element of the given type and name.
        /// </summary>
        public static Element GetFirstElementOfTypeNamed(Document doc, Type type, string name)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(type);

        #if EXPLICIT_CODE

              // explicit iteration and manual checking of a property:

              Element ret = null;
              foreach( Element e in collector )
              {
                if( e.Name.Equals( name ) )
                {
                  ret = e;
                  break;
                }
              }
              return ret;
        #endif // EXPLICIT_CODE

        #if USE_LINQ

              // using LINQ:

              IEnumerable<Element> elementsByName =
                from e in collector
                where e.Name.Equals( name )
                select e;

              return elementsByName.First<Element>();
        #endif // USE_LINQ

            // using an anonymous method:

            // if no matching elements exist, First<> throws an exception.

            //return collector.Any<Element>( e => e.Name.Equals( name ) )
            //  ? collector.First<Element>( e => e.Name.Equals( name ) )
            //  : null;

            // using an anonymous method to define a named method:

            Func<Element, bool> nameEquals = e => e.Name.Equals(name);

            return collector.Any<Element>(nameEquals) ? collector.First<Element>(nameEquals) : null;
        }
        #endregion


        #region Conversion
        /// <summary>
        /// Convert a given length in feet to milimeters.
        /// </summary>
        public static double FootToMm(double length) { return length * 304.8; }

        /// <summary>
        /// Convert a given length in milimeters to feet.
        /// </summary>
        public static double MmToFoot(double length) { return length / 304.8; }

        /// <summary>
        /// Convert a given point or vector from milimeters to feet.
        /// </summary>
        public static XYZ MmToFoot(XYZ v) { return v.Divide(304.8); }

        /// <summary>
        /// Convert List of lines to List of curves
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public static List<Curve> LinesToCrvs(List<Line> lines)
        {
            List<Curve> crvs = new List<Curve>();
            foreach (Line line in lines)
            {
                crvs.Add(line as Curve);
            }
            return crvs;
        }

        /// <summary>
        /// Convert List of curves to List of lines
        /// </summary>
        /// <param name="crvs"></param>
        /// <returns></returns>
        public static List<Line> CrvsToLines(List<Curve> crvs)
        {
            List<Line> lines = new List<Line>();
            foreach (Curve crv in crvs)
            {
                lines.Add(crv as Line);
            }
            return lines;
        }

        #endregion


        #region Formatting
        /// <summary>
        /// Return an English plural suffix for the given
        /// number of items, i.e. 's' for zero or more
        /// than one, and nothing for exactly one.
        /// </summary>
        public static string PluralSuffix(int n)
        {
            return 1 == n ? "" : "s";
        }

        /// <summary>
        /// Return an English plural suffix 'ies' or
        /// 'y' for the given number of items.
        /// </summary>
        public static string PluralSuffixY(int n)
        {
            return 1 == n ? "y" : "ies";
        }

        /// <summary>
        /// Return a dot (full stop) for zero
        /// or a colon for more than zero.
        /// </summary>
        public static string DotOrColon(int n)
        {
            return 0 < n ? ":" : ".";
        }

        /// <summary>
        /// Return a string for a real number
        /// formatted to two decimal places.
        /// </summary>
        public static string RealString(double a)
        {
            return a.ToString("0.##");
        }

        /// <summary>
        /// Return a hash string for a real number
        /// formatted to nine decimal places.
        /// </summary>
        public static string HashString(double a)
        {
            return a.ToString("0.#########");
        }

        /// <summary>
        /// Return a string representation in degrees
        /// for an angle given in radians.
        /// </summary>
        public static string AngleString(double angle)
        {
            return RealString(angle * 180 / Math.PI) + " degrees";
        }

        /// <summary>
        /// Return a string for a length in millimetres
        /// formatted as an integer value.
        /// </summary>
        public static string MmString(double length)
        {
            //return RealString( FootToMm( length ) ) + " mm";
            return Math.Round(FootToMm(length)).ToString() + " mm";
        }

        /// <summary>
        /// Return a string for a UV point
        /// or vector with its coordinates
        /// formatted to two decimal places.
        /// </summary>
        public static string PointString(UV p, bool onlySpaceSeparator = false)
        {
            string format_string = onlySpaceSeparator ? "{0} {1}" : "({0},{1})";
            return string.Format(format_string, RealString(p.U), RealString(p.V));
        }

        /// <summary>
        /// Return a string for an XYZ point
        /// or vector with its coordinates
        /// formatted to two decimal places.
        /// </summary>
        public static string PointString(XYZ p, bool onlySpaceSeparator = false)
        {
            string format_string = onlySpaceSeparator ? "{0} {1} {2}" : "({0},{1},{2})";
            return string.Format(format_string, RealString(p.X), RealString(p.Y), RealString(p.Z));
        }

        /// <summary>
        /// Return a hash string for an XYZ point
        /// or vector with its coordinates
        /// formatted to nine decimal places.
        /// </summary>
        public static string HashString(XYZ p)
        {
            return string.Format("({0},{1},{2})", HashString(p.X), HashString(p.Y), HashString(p.Z));
        }

        /// <summary>
        /// Return a string for this bounding box
        /// with its coordinates formatted to two
        /// decimal places.
        /// </summary>
        public static string BoundingBoxString(BoundingBoxUV bb, bool onlySpaceSeparator = false)
        {
            string format_string = onlySpaceSeparator ? "{0} {1}" : "({0},{1})";

            return string.Format(format_string, PointString(bb.Min, onlySpaceSeparator), PointString(bb.Max, onlySpaceSeparator));
        }

        /// <summary>
        /// Return a string for this bounding box
        /// with its coordinates formatted to two
        /// decimal places.
        /// </summary>
        public static string BoundingBoxString(BoundingBoxXYZ bb, bool onlySpaceSeparator = false)
        {
            string format_string = onlySpaceSeparator ? "{0} {1}" : "({0},{1})";

            return string.Format(format_string, PointString(bb.Min, onlySpaceSeparator), PointString(bb.Max, onlySpaceSeparator));
        }

        /// <summary>
        /// Return a string for this plane
        /// with its coordinates formatted to two
        /// decimal places.
        /// </summary>
        public static string PlaneString(Plane p)
        {
            return string.Format("plane origin {0}, plane normal {1}", PointString(p.Origin), PointString(p.Normal));
        }

        /// <summary>
        /// Return a string for this transformation
        /// with its coordinates formatted to two
        /// decimal places.
        /// </summary>
        public static string TransformString(Transform t)
        {
            return string.Format("({0},{1},{2},{3})", PointString(t.Origin),
              PointString(t.BasisX), PointString(t.BasisY), PointString(t.BasisZ));
        }

        /// <summary>
        /// Return a string for a list of doubles 
        /// formatted to two decimal places.
        /// </summary>
        public static string DoubleArrayString(IEnumerable<double> a, bool onlySpaceSeparator = false)
        {
            string separator = onlySpaceSeparator ? " " : ", ";

            return string.Join(separator, a.Select<double, string>(x => RealString(x)));
        }

        /// <summary>
        /// Return a string for this point array
        /// with its coordinates formatted to two
        /// decimal places.
        /// </summary>
        public static string PointArrayString(IEnumerable<UV> pts, bool onlySpaceSeparator = false)
        {
            string separator = onlySpaceSeparator ? " " : ", ";

            return string.Join(separator, pts.Select<UV, string>(p => PointString(p, onlySpaceSeparator)));
        }

        /// <summary>
        /// Return a string for this point array
        /// with its coordinates formatted to two
        /// decimal places.
        /// </summary>
        public static string PointArrayString(IEnumerable<XYZ> pts, bool onlySpaceSeparator = false)
        {
            string separator = onlySpaceSeparator ? " " : ", ";

            return string.Join(separator, pts.Select<XYZ, string>(p => PointString(p, onlySpaceSeparator)));
        }

        /// <summary>
        /// Return a string representing the data of a
        /// curve. Currently includes detailed data of
        /// line and arc elements only.
        /// </summary>
        public static string CurveString(Curve c)
        {
            string s = c.GetType().Name.ToLower();

            XYZ p = c.GetEndPoint(0);
            XYZ q = c.GetEndPoint(1);

            s += string.Format(" {0} --> {1}", PointString(p), PointString(q));

            // To list intermediate points or draw an
            // approximation using straight line segments,
            // we can access the curve tesselation, cf.
            // CurveTessellateString:

            //foreach( XYZ r in lc.Curve.Tessellate() )
            //{
            //}

            // List arc data:

            Arc arc = c as Arc;

            if (null != arc)
            {
                s += string.Format(" center {0} radius {1}", PointString(arc.Center), arc.Radius);
            }

            // Todo: add support for other curve types
            // besides line and arc.

            return s;
        }

        /// <summary>
        /// Return a string for this curve with its
        /// tessellated point coordinates formatted
        /// to two decimal places.
        /// </summary>
        public static string CurveTessellateString(Curve curve)
        {
            return "curve tessellation " + PointArrayString(curve.Tessellate());
        }

        /// <summary>
        /// Convert a UnitSymbolType enumeration value
        /// to a brief human readable abbreviation string.
        /// </summary>
        /*public static string UnitSymbolTypeString(UnitSymbolType u)
        {
            string s = u.ToString();

            Debug.Assert(s.StartsWith("UST_"),
              "expected UnitSymbolType enumeration value to begin with 'UST_'");

            s = s.Substring(4).Replace("_SUP_", "^").ToLower();

            return s;
        }*/


        public static string ListString(List<string> list)
        {
            string fusion = "";
            for (int index = 0; index < list.Count(); index++)
            {
                fusion = fusion + list[index] + " ";
            }
            return fusion;
        }

        public static string ListString(List<bool> list)
        {
            string fusion = "";
            for (int index = 0; index < list.Count(); index++)
            {
                fusion = fusion + list[index].ToString() + " ";
            }
            return fusion;
        }

        public static string ListString(List<int> list)
        {
            string fusion = "";
            for (int index = 0; index < list.Count(); index++)
            {
                fusion = fusion + list[index].ToString() + " ";
            }
            return fusion;
        }

        public static string ListString(List<double> list)
        {
            string fusion = "";
            for (int index = 0; index < list.Count(); index++)
            {
                fusion = fusion + list[index].ToString() + " ";
            }
            return fusion;
        }

        public static string ListString(List<XYZ> list)
        {
            string fusion = "";
            for (int index = 0; index < list.Count(); index++)
            {
                fusion = fusion + PointString(list[index]) + " ";
            }
            return fusion;
        }


        #endregion // Formatting

    }
}
