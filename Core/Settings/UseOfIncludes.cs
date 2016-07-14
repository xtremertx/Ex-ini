using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace eX_INI
{
    //[Flags]
    public enum UseOfIncludes : byte
    {
        Read = 1,               // Parser will read included files
        Ignore = 0,             // Parser will ignore included files
        EditAndRestore = 2,     // Includes are stored into ini object, but it's not read by the parser (its possible to edit them and restore)
        ReadAndRestore = 3      // Includes are stored into .ini object and also read by the parser (so they are interpreted and restored back, you can also edit them)
    }
}
