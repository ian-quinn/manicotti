using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace Manicotti
{
    public static class Util
    {
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


        #region DEBUG
        /// <summary>
        /// Return the coorinate of XYZ as a string
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        public static string PrintXYZ(XYZ pt)
        {
            return string.Format(" ({0}, {1}, {2}) ", pt.X, pt.Y, pt.Z);
        }

        /// <summary>
        /// Return the content of List(int) as a string
        /// </summary>
        /// <param name="seq"></param>
        /// <returns></returns>
        public static string PrintSeq(List<int> seq)
        {
            string result = "";
            foreach (int e in seq)
            {
                result = result + e.ToString() + " ";
            }
            return result;
        }
        #endregion

    }
}
