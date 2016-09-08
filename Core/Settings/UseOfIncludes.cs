
using System;
using System.ComponentModel;
namespace eX_INI
{
    [Flags]
    public enum UseOfIncludes : byte
    {
        [Description("Alias: Ignore")]
        None = 1 << 0,
        [Description("Parser will ignore included files")]
        Ignore = None,
        [Description("Parser will read included files")]       
        Read = 1 << 2,
        [Description("Includes are stored into .ini object, but it's not read by the parser (its possible to edit them and restore)")]           
        EditAndRestore = 1 << 3,
        //[Description("Includes are stored into .ini object and also read by the parser")]   
        //ReadEditAndRestore = (Read | EditAndRestore)
    }
}
