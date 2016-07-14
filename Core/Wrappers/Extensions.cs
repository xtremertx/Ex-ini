#if !(NETFX_40 || NETFX_45 || NETFX_451)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace eX_INI.Wrapper
{
    // Extension for .NET 3.5 (this function is from .NET 4.5+)
    public static class Extensions
    {
        public static bool IsNullOrWhiteSpace(this string value)
        {
            if (value == null) return true;

            for (int i = 0; i < value.Length; i++)
            {
                if (!Char.IsWhiteSpace(value[i])) return false;
            }

            return true;
        }

        //public static StringBuilder CreateStringBuilder(this string value, int startIndex, int endIndex)
        //{
        //    return new StringBuilder(value, startIndex, value.Length - startIndex - (value.Length - endIndex) - 1, 0);
        //}

        //public static string ReplaceEscapeSequence(this string value, string findValue, string replaceValue)
        //{
        //    return new StringBuilder(value).Replace(findValue, replaceValue).ToString();
        //}

        //// This will replaces only first occurence of string (speed-up)
        //public static string ReplaceFirstOccurenceOnly(this string value, string findValue, string replaceValue)
        //{
        //    int i = value.IndexOf(findValue, StringComparison.Ordinal);

        //    return (i > -1) ? new StringBuilder(value).Replace(findValue, replaceValue, i, findValue.Length).ToString() : value;
        //}
    }
}

#endif
