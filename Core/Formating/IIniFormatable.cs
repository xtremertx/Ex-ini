using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace eX_INI
{
    // Represents option how to format output of INI object to INI file
    // Parser will call this interface on every INI
    public interface IIniFormatable
    {
        string WriteSection(Section section);
        string WriteSectionEnd();   // Can be used for blank line after each section or so...
        string WriteNote(string note);
        string WritePair(KeyValuePair<string, KeyValue> pair);
        string WriteInclude(string file);
    }
}
