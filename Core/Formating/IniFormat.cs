using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace eX_INI
{
    // Implementation of IIniFomatable
    public class IniFormat : IIniFormatable
    {
        //private StringBuilder _strBuilder = new StringBuilder();
        private IniParserSettings settings = null;

        public IniFormat(IniParserSettings settings)
        {
            this.settings = settings;
        }

        public string WriteSection(Section section)
        {
            // Write header + inheritance
            return string.Format("{0}{1}{2}", settings.SectionStartChar, (section.Base != null) ? section.ToString() + settings.InheritanceChar + section.Base.ToString() : section.ToString(), settings.SectionEndChar);
        }

        public string WriteSectionEnd()
        {
            return Environment.NewLine;
        }

        public string WriteNote(string note)
        {
            // Write note with prefix or return user defined note with prefix or blank line (empty notes have space instead!)
            return (string.IsNullOrEmpty(note) ? string.Empty : note.StartsWith(settings.NoteSymbol, StringComparison.OrdinalIgnoreCase) ? note : settings.NoteSymbol + note);
        }

        public string WritePair(KeyValuePair<string, KeyValue> pair)
        {
            return string.Format("{0}{1}{2}", pair.Key, settings.PairOperator, pair.Value.Value);
        }

        public string WriteInclude(string file)
        {
            return string.Format("{0}{1}{2}", settings.IncludeStartChar, file, settings.IncludeEndChar);
        }
    }
}
