
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace eX_INI
{
    public static class Extensions
    {
        public static bool HasBitFlag(this UseOfIncludes e, UseOfIncludes bitFlag)
        {
            return ((e & bitFlag) == bitFlag);
        }

        public static bool IsLowerCase(this string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                if (Char.IsLetter(s[i]) && Char.IsUpper(s[i]))
                    return false;
            }
            return true;
        }
    }
}
