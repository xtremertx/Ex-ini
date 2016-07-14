
namespace eX_INI
{
    // Settings for parser, u can create more settings and just change settings for parser on the fly
    public class IniParserSettings
    {
        public IniParserSettings()
        {
            // Default settings - file will be saved as original (with all blank lines, etc..)
            ReadNotes = true;
            WriteNotes = true;
            BlankLinesAsNotes = true;
            CaseSensitiveSections = true;
            PostNotesToLastSection = true;

            NoteSymbol = ";";
            SectionStartChar = "[";
            SectionEndChar = "]";
            PairOperator = "=";
            IncludeStartChar = "<";
            IncludeEndChar = ">";
            DefaultExtension = ".ini";
            InheritanceChar = ":";
            RelativePathSymbol = @"~\";
            //BinaryStreamTag = "~ß:";
        }

        public bool ReadNotes { get; set; }
        public bool WriteNotes { get; set; }
        public UseOfIncludes Includes { get; set; }
        public bool CaseSensitiveSections { get; set; }
        public bool BlankLinesAsNotes { get; set; }
        public bool PostNotesToLastSection { get; set; }
        public bool CreateGlobalSection { get; set; }
        public bool EndSectionWithBlankLine { get; set; }

        public string DefaultExtension { get; set; }
        public string NoteSymbol { get; set; }
        public string SectionStartChar { get; set; }
        public string SectionEndChar { get; set; }
        public string PairOperator { get; set; }
        public string IncludeStartChar { get; set; }
        public string IncludeEndChar { get; set; }
        public string InheritanceChar { get; set; }
        public string RelativePathSymbol { get; set; }
        //public string BinaryStreamTag { get; set; }
    }
}
