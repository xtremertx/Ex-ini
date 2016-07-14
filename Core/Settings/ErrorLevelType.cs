
namespace eX_INI
{
    public enum ErrorLevelType : byte
    {
        None = 0x0,
        Info,           // On auto repair
        Warning,        // When something's not right, but not a big deal
        Error,          // Real error, but parser can survive this - ignored element probably
        Critical        // Parser cant survive - something FUCKED UP rly hard ;)
    }
}
