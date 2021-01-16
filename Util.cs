using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Manicotti
{
    public class Util
    {
        public static int ExtractIndex(String str)
        {
            int index = -1;
            String numStr = "";
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

        public static int GetLevel(String label, String key)
        {
            // Consider Regex here, maybe
            int level = -1;
            if (label.Contains(key))
            {
                level = ExtractIndex(label);
            }
            return level;
        }
    }
}
