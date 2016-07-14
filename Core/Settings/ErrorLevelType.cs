using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace eX_INI
{
    public enum ErrorLevelType : byte
    {
        None = 0x0,
        Info,           // On auto repair
        Warning,        // When something is not right, but not a big deal
        Error,          // Real error, but parser can survive still - something ignored probably
        Critical        // Parser cant survive - something FUCKED UP rly hard ;)
    }
}
