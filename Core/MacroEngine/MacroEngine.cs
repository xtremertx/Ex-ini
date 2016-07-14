using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace eX_INI.Core.MacroEngine
{
    // test feature...
    public class MacroEngine
    {
        HashSet<Macro> _macroSet = new HashSet<Macro>(new MacroComparer());

        /* Accesors */
        public void AddMacro(Macro macro)
        {
            if (macro == null)
                throw new NullReferenceException();

            if (!_macroSet.Contains(macro))
                _macroSet.Add(macro);
            else
                throw new Exception(string.Format("Macro: {0} already exists in the engine!", macro.MacroPrefix));
        }

        public void RemoveMacro(Macro macro)
        {
            // Just try to remove macro
            _macroSet.Remove(macro);
        }

        public Macro this[Macro index]
        {
            get 
            {
                // Look for macro and use equality comparer value if he finds something!
                Macro _find;
                _macroSet.TryGetValue(index, out _find);
                return _find;
            }
        }

        //macro__CONTENT=VALUE (macro can be used only in pairs)
        //lst__= (u dont need to use a special KEY, but u cant use more macros like this in same section then!)
        //lst__=A,B,C,D
        //obj__=MyClass{{}{}}

        /* Call macro engine */
        private Macro _m = new Macro();
        private StringBuilder _str;
        public string CallMacro(string line)
        {
            int i;

            // There is macro prefix? '__' (macro must have at least 1 char)
            if ( (i = line.IndexOf("__", StringComparison.OrdinalIgnoreCase)) > 0)
            {
                // Extract prefix
                _str = new StringBuilder(line);
                _m.MacroPrefix = _str.ToString(0, i);

                // Find registered macro for this prefix
                _m = this[_m];

                // Call macro transformation
                if (_m != null)
                {
                    int ii = i + 2;
                    // Give transform function a clear content of line, return output
                    return _m.TransformFunc(_str.ToString(ii, _str.Length - ii));
                }
            }
            return null;
        }

    }

    public class Macro
    {
        public Func<string, string> TransformFunc { get; set; }
        public string MacroPrefix { get; set; }
    }

    // To speed up things little hopefully ;)
    // Custom comparers are quicker..
    public class MacroComparer : IEqualityComparer<Macro>
    {
        internal Macro Element;

        public bool Equals(Macro x, Macro y)
        {
            if (x.MacroPrefix.Equals(y.MacroPrefix, StringComparison.OrdinalIgnoreCase))
            {
                Element = x;
                return true; 
            }
            return false;
        }

        public int GetHashCode(Macro obj)
        {
            return obj.MacroPrefix.GetHashCode();

            //unchecked // Overflow is fine, just wrap
            //{
            //    int hash = (int)2166136261;
            //    // Suitable nullity checks etc, of course :)
            //    hash = (hash * 16777619) ^ obj.GetHashCode();
            //    hash = (hash * 16777619) ^ obj.MacroPrefix.GetHashCode();
            //    return hash;
            //}
        }

    }

    // Kind of hackier solution to get values from hashset ;p
    public static class HashSetExtension
    {
        public static bool TryGetValue(this HashSet<Macro> hs, Macro value, out Macro valout)
        {
            if (hs.Contains(value))
            {
                valout = (hs.Comparer as MacroComparer).Element;
                return true;
            }
            else
            {
                valout = null;
                return false;
            }
        }
    }
}

