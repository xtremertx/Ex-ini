#if !(NETFX_45 || NETFX_451)
using eX_INI.Wrapper;
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;

namespace eX_INI
{
    // Represents INI parser, u can parse INI files and strings, parsed output is created as INI object in memory
    // Parser can parse whole input at time or just sections u need (so u will only work with usefull output - not implemented fully)
    // Parser have ability to make a stream with binary value (tagged by: ~ß) (not implemented feature)
    public class IniParser
    {
        #region Ctors

        public IniParser()
        {
            Settings = new IniParserSettings();
            _ht = new Dictionary<string, Section>();
            _ht_includes = new Dictionary<string, List<Include>>();
        }

        #endregion

        #region Events

        // After file is saved or loaded succesfully
        public event Action FileLoaded, FileSaved;
        public event Action<string, ErrorLevelType> OnError;

        #endregion

        #region Properties

        private Dictionary<string, Section> _ht;
        private Dictionary<string, List<Include>> _ht_includes;

        private IniParserSettings _settings;
        public IniParserSettings Settings
        {
            get 
            {
                return _settings; 
            }
            set 
            { 
                _settings = value;
            }
        }

        #endregion

        #region Fields

        // Current section parsed
        private Section cur_section = null;
        // Current notes recorded
        private List<string> cur_notes = null;

        // Ignoruj všechny páry a poznámky na které narazíš dokud nenajdeš novou sekci!
        private bool dont_ignore_pairs = true;
        // Oznamuje že poslední entita byla pár
        private bool last_entity_was_pair = false;
        // Oznamuje ParseInclude() metodě cestu k hlavnímu INI (obsahujícímu includy)
        private string main_ini_path = "";

        // Speed-up properties (allocated coz called all the time)
        private CultureInfo _cInfo = CultureInfo.InvariantCulture;
        private string[] _inheritanceSplit, _pairSplit;
        private int _includeCount, _sectionCount;

        #endregion

        #region Parsing Core
        public void Reset()
        {
            _ht = new Dictionary<string, Section>();
            _ht_includes = new Dictionary<string, List<Include>>();
            cur_notes = new List<string>();
            cur_section = null;
            _cInfo = CultureInfo.InvariantCulture;  // Reset culture for current thread (maybe it changed this time)
        }

        private void Parse(string line)
        {
#if (NETFX_40 || NETFX_45 || NETFX_451)
            if (string.IsNullOrWhiteSpace(line))
#else
            if (line.IsNullOrWhiteSpace())
#endif
            {
                // Save blank lines as special notes
                if (Settings.BlankLinesAsNotes)
                    ParseNote(string.Empty);

                return; // Always jump out
            }

            // We get there only when string is text
            // Get just text without "spaces" (TAB = \u0009)
            // https://msdn.microsoft.com/cs-cz/library/system.char.iswhitespace%28v=vs.110%29.aspx
            line = line.Trim('\u0009','\u2028', '\u2029', '\u000A', '\u000B', '\u000C', '\u000D', '\u0085', '\u00A0', '\u1680', '\u180E', '\u2000', '\u2001', '\u2002', '\u2003', '\u2004', '\u2005', '\u2006', '\u2007', '\u2008', '\u2009', '\u200A', '\u202F', '\u205F', '\u3000', '\u0020');

            if (Settings.ReadNotes && IsNote(line))
                ParseNote(line);

            else if ((Settings.Includes != UseOfIncludes.Ignore) && /*!last_entity_was_pair &&*/ IsInclude(line))
                ParseInclude(line);

            else if (IsSection(line))
                ParseSection(line);

            else if (IsPair(line) && dont_ignore_pairs)
                ParsePair(line);

            // Remove any notes that should be ignored, coz they are associated with ignored pair
            if (last_entity_was_pair && !dont_ignore_pairs)
            {
                cur_notes = new List<string>();
                last_entity_was_pair = false;
            }
        }

        // Po konci parsování. (Rozhodneme dle nastavení co udělat s post poznámkami)
        private void FinalParse()
        {
            // Zaznamenali jsme nějaké a máme sekci kam je dát?
            if (cur_notes == null || cur_section == null)
                return;

            // Má sekce nějaké poznámky?
            if (cur_section.Notes == null)
                return;

            // Vyžaduje uživatel tyto poznámky associovat s poslední sekcí?
            if (Settings.PostNotesToLastSection)
            {
                // Remove any empty strings (blank lines)
                if (Settings.BlankLinesAsNotes)
                    cur_notes.RemoveAll((note) => note == string.Empty);

                // Přidej poznámky do poznámek sekce
                cur_section.Notes.AddRange(cur_notes);
            }
        }

        #endregion

        #region Parse Methods

        private void ParseSection(string line)
        {
            StringBuilder strBuilder = new StringBuilder(line, _settings.SectionStartChar.Length, line.Length - _sectionCount, 0);

            // Section using inheritance?
            string[] parms = strBuilder.ToString().Split(_inheritanceSplit, 2, StringSplitOptions.None);

            // Set section name
            string name = Settings.CaseSensitiveSections ? parms[0] : parms[0].ToLower(_cInfo);

            // ('\:' = escape sequence to ':')
            //name = name.ReplaceEscapeSequence("\\" + _settings.InheritanceChar, _settings.InheritanceChar);

            // If section is already in main table jump to new one!
            if (_ht.ContainsKey(name))
            {
                // Nastav příznak ignoringu
                dont_ignore_pairs = false;

                // Vymaž poznámky které byli associované k této sekci
                cur_notes = new List<string>();

                RaiseOnErrorEvent(string.Format("Duplicated section {0}, ignoring it!", name), ErrorLevelType.Warning);

                return;
            }

            // We're in totaly new section we can read pairs now
            dont_ignore_pairs = true;

            // Inherited from?
            Section temp = null;

            if (parms.Length > 1)
            {
                string base_name = Settings.CaseSensitiveSections ? parms[1] : parms[1].ToLower(_cInfo);

                // Get base section if already loaded!
                _ht.TryGetValue(base_name, out temp);
            }
            cur_section = new Section(name, temp, null) { Notes = cur_notes };

            // Add new section to collection
            _ht.Add(name, cur_section);

            // Clear notes for new section or pair
            cur_notes = new List<string>();
        }

        private void ParsePair(string line)
        {
            // No section registered, we are in global section then
            if (cur_section == null)
            {
                // If user want to create global section, associate notes with global section
                cur_section = new Section("", null, new Dictionary<string, KeyValue>()) { Notes = (!Settings.CreateGlobalSection) ? null : cur_notes };

                // Add global section to _ht
                _ht.Add("", cur_section);

                #region FeatureUserFlagHereWhereToAssociateNotes?
                // Notes were associated with global section, we dont want to associate them with current pair ;)
                //cur_notes = new List<string>();
                #endregion

                // If user dont want to create global section, associate notes with first pair of global section
                cur_notes = (!Settings.CreateGlobalSection) ? cur_notes : new List<string>();
            }

            // Get global k=v
            string[] binds = line.Split(_pairSplit, 2, StringSplitOptions.None);

            // We have some key?
            if (binds[0].Length > 0)
            {
                // Section dont have any pairs?
                if (cur_section.Pairs == null)
                    cur_section.Pairs = new Dictionary<string, KeyValue>();

                // Section already contains key?
                if (cur_section.Pairs.ContainsKey(binds[0]))
                {
                    RaiseOnErrorEvent(string.Format("Section {0} already contains key {1} - duplicated pair, ignoring!", cur_section.Name, binds[0]), ErrorLevelType.Warning);
                    return;
                }

                // Add new pair into section
                KeyValue kvp = new KeyValue(binds[1]) { Notes = cur_notes };
                cur_section.Pairs.Add(binds[0], kvp);
            }

            // Notes were associated with pair, clear notes for new pair
            cur_notes = new List<string>();
        }

        private void ParseNote(string line)
        {
            // Always initialize notes
            if (cur_notes == null)
                cur_notes = new List<string>();

            // if (NOTE_PREFIX_SYMBOL) only there -> (we know IsNote() & TrimEnd() were used on each "note" or it's string.Empty)
            // string.Empty have LENGTH == 0!
            if (line.Length == 1)
                line = " "; // Its blank note
            else if (line.Length > 1)
                line = new StringBuilder(line, _settings.NoteSymbol.Length, line.Length - _settings.NoteSymbol.Length, 0).ToString(); // Its TEXT or TABS

            // Add new note to current notes
            cur_notes.Add(line);
        }

        private void ParseInclude(string include)
        {
            // Get a path to .ini file
            StringBuilder strBuilder = new StringBuilder(include, _settings.IncludeStartChar.Length, include.Length - _includeCount, 0);
            string path = strBuilder.ToString();
            var _fInfo = new FileInfo(main_ini_path);

            // Fix: DO NOT INCLUDE INCLUDES WITH SAME NAME AS CURRENT FILENAME (coz parser will try to parse their content, but ignoring it coz duplicated content = reading times high)
            // <~\FILENAME.ext> but we're in FILENAME.ext right now!
            // NOTE: This will work even when parsing from STREAM or STRING, in that case user will not specify filename but directory path so it will be ending on: \.x, however we know that whole content of ini is in
            // STRING or STREAM so user cant make cyclic include ;) (its simply possible only in filenames on HDD)
            // Also remove this kind of includes from file and dont make them editable! (auto-repair feature)
            if (path.IndexOf(_fInfo.Name) != -1)
            {
                RaiseOnErrorEvent("Ignoring include refering to the same file we are in!", ErrorLevelType.Error);
                return;
            }

            // Make includes editable?
            if (Settings.Includes == UseOfIncludes.EditAndRestore)
            {
                // Under what section include belong to?
                string s_name = (cur_section == null) ? Include.START_OF_FILE : cur_section.Name;

                // Try to get list
                List<Include> list;
                _ht_includes.TryGetValue(s_name, out list);

                // Add include to list
                if (list == null)
                {
                    list = new List<Include>();

                    // Add list to hashtable
                    _ht_includes.Add(s_name, list);
                }

                list.Add(new Include(path) { Notes = cur_notes });

                // Notes were associated with include, release them now
                cur_notes = new List<string>();

                // We're not parsing include, just making it editable
                return;
            }

            // Is relative or absolute?
            if (path.StartsWith(_settings.RelativePathSymbol, StringComparison.OrdinalIgnoreCase))
            {
                path = strBuilder.ToString(2, strBuilder.Length - 2);
                path = IniParser.GetAbsolutePath(path, _fInfo.DirectoryName);
            }
            else
                path = Path.GetFullPath(path);

            // Extension is not mandatory (it's optional)
            if (!Path.HasExtension(path))                       // Moreover we dont care about certain extension eighter
                path += _settings.DefaultExtension;             // But we assume that it will be .ini extension always (or user specified)

            // File must exists in order to parse him
            if (!File.Exists(path))
            {
                RaiseOnErrorEvent(string.Format("Include references non-existing file: {0}!", path), ErrorLevelType.Error);
                return;
            }

            // Parsing included .ini while ignoring any other includes
            var backup = Settings.Includes;
            Settings.Includes = UseOfIncludes.Ignore;

            using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (BufferedStream bs = new BufferedStream(fs))
            using (StreamReader sr = new StreamReader(bs, true))
            {
                // Empty file?
                if (sr.EndOfStream)
                {
                    RaiseOnErrorEvent(string.Format("Included file was empty: {0}!", path), ErrorLevelType.Error);
                    return;
                }

                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    Parse(line);
                }
                FinalParse();
            }

            // Return user option back!
            Settings.Includes = backup;

            // Dont associate last notes to new: "pair or section" (post notes from include)
            cur_notes = new List<string>();

            // Ignore possible global pairs in main .ini
            dont_ignore_pairs = false;
        }

        #endregion

        #region Is Methods

        private bool IsSection(string line)
        {
            return (line.StartsWith(Settings.SectionStartChar, StringComparison.OrdinalIgnoreCase) && line.EndsWith(Settings.SectionEndChar, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsPair(string line)
        {
            return (last_entity_was_pair = line.Contains(Settings.PairOperator));
        }

        private bool IsNote(string line)
        {
            return line.StartsWith(Settings.NoteSymbol, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsInclude(string line)
        {
            return (line.StartsWith(Settings.IncludeStartChar, StringComparison.OrdinalIgnoreCase) && line.EndsWith(Settings.IncludeEndChar, StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        // Parser nastaveni je použito pro ukládání a loadování (vyjímkou je: Load(INI)/Save(INI), který využije interní nastavení INI objektu)
        #region Load/Save Methods

        // Adds parsed string data into ini (ini can be uninitialized, will be auto initialized)
        public void AddStringData(INI ini, string data, string lineSeparator, string basePath = null)
        {
            //// Backup user settings
            //var backup = _settings.Includes;
            //var backup2 = _settings.CreateGlobalSection;

            //// Make sure we will not interpret includes, were merging, also create global section
            //_settings.Includes = UseOfIncludes.EditAndRestore;
            //_settings.CreateGlobalSection = true;

            INI.Merge(ini, Load(data, lineSeparator, basePath), mergeIncludes: true);

            //// Restore user settings
            //_settings.Includes = backup;
            //_settings.CreateGlobalSection = backup2;
        }

        // Creates ini object with UTF-8 encoding by default
        public INI Load(string data, string lineSeparator, string basePath = null)
        {
            CacheSettings();

            string[] lines = data.Split(new string[] { lineSeparator }, StringSplitOptions.None);

            if (lines == null)
            {
                RaiseOnErrorEvent("No lines found to be parsed!", ErrorLevelType.Error);
                return null;
            }

            // Base path for relative includes (default is app path)
            main_ini_path = MakeValidBasePath(basePath);

            // Running parser
            foreach (string line in lines)
            {
                Parse(line);
            }
            FinalParse();

            // Create new INI object with parsed collection
            INI ini = new INI();
            ini.Encoding = UTF8Encoding.UTF8;
            ini.Settings = _settings;
            ini.ht_full = _ht;
            ini.ht_incl = _ht_includes;

            // Prepare parser for new work
            Reset();

            // Return new INI
            return ini;
        }

        // (base_path must end with: "..\FILENAME.EXTENSION" or ParseInclude() will look to bad location - its workaround;))
        private string MakeValidBasePath(string basePath)
        {
            // EXE_FILE_PATH as default when NULL
            if (basePath == null)
                return Environment.CurrentDirectory;

            // Check if PATH given is folder or filename
            string s = Path.GetFileName(basePath);

            // string.Empty means its FOLDER so make workaround ;)
            // Or its FILENAME.EXT so return it as it is!
            return ( (s.Length == 0) ? new StringBuilder(basePath).Append(@"\.x").ToString() : basePath);
        }

        // Initialize INI object using INI object informations (file, encoding)
        public void Load(INI ini)
        {
            if (ini != null)
            {
                CacheSettings();

                // Backup parser settings
                IniParserSettings backup = _settings;

                // Use INI settings for parser
                _settings = ini.Settings;

                // Remove informations inside (will be replaced anyway, so make GC life easier)
                ini.Clear();

                // Parse file specified using INI informations
                INI ini2 = Load(ini.Filename, ini.Encoding);

                // Replace information with new one
                ini.ht_full = ini2.ht_full;
                ini.ht_incl = ini2.ht_incl;

                // Return parser settings back
                _settings = backup;
            }
        }

        // Autodetects encoding for file
        public INI Load(string file, Encoding encoding = null)
        {
            INI ini;

            using (FileStream fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                ini = Load(fs, (encoding == null) ? IniParser.GetFileEncoding(file) : encoding, file);
                ini.Filename = file;
            }

            return ini;
        }

        // Encoding in stream must be specified, there is no way how to detect that
        public INI Load(Stream stream, Encoding encoding, string basePath = null)
        {
            CacheSettings();
            // Load whole config variables into memory!
            using (BufferedStream bs = new BufferedStream(stream))
            using (StreamReader sr = new StreamReader(bs, encoding))
            {
                // Empty file?
                if (sr.EndOfStream)
                {
                    RaiseOnErrorEvent(string.Format("Failed to load the file {0}, it was empty!", (basePath == null) ? string.Empty : basePath), ErrorLevelType.Error);
                    return null;
                }

                // Base path for relative includes (default is app path)
                main_ini_path = MakeValidBasePath(basePath);

                // Parse .ini file
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    Parse(line);
                }
                FinalParse();
            }

            // Create new INI object with parsed collection
            INI ini = new INI(null, encoding);
            ini.ht_full = _ht;
            ini.ht_incl = _ht_includes;
            ini.Settings = _settings;

            // Prepare parser for new work
            Reset();

            // Call event
            if (FileLoaded != null)
                FileLoaded();

            // Return new INI
            return ini;
        }


        private void WriteIncludesToFile(StreamWriter sw, INI ini, IIniFormatable format, string includeForSection)
        {
            // Includes that dont belongs to any section
            if (Settings.Includes == UseOfIncludes.EditAndRestore)
            {
                // Get includes for section specified
                List<Include> list;
                ini.ht_incl.TryGetValue(includeForSection, out list);

                // Are there any includes?
                if (list != null)
                {
                    // Write them to file
                    foreach (var include in list)
                    {
                        // Write notes associated with includes
                        if (Settings.WriteNotes && include.Notes != null)
                            foreach (var note in include.Notes)
                                sw.WriteLine(format.WriteNote(note));

                        sw.WriteLine(format.WriteInclude(include.Path));
                    }
                }
            }
        }

        // Using UTF-8 encoding by default when not specified by the user
        // [NOTE] when saving into any stream, underlying stream (stream were ur writing INI content) is not CLOSED, be sure to close it on the end of the day, to prevent LEAKS!
        public void Save(INI ini, Stream stream, Encoding encoding = null, IIniFormatable format = null)
        {
            // Load whole config variables into memory!
#if (NETFX_45 || NETFX_451)
            using (StreamWriter sw = new StreamWriter(stream, encoding, 4096, true))  // .NET 4.5 code solving same problem!
#else
            using (StreamWriterWrapper sw = new StreamWriterWrapper(stream, encoding))
#endif
            {
                CacheSettings();

                if (format == null)
                    format = new IniFormat(_settings);

                // Includes that dont belongs to any section
                WriteIncludesToFile(sw, ini, format, Include.START_OF_FILE);

                // User DONT want global section in file?
                if (!Settings.CreateGlobalSection && ini.Global != null)
                {
                    if (ini.Global.Pairs != null)
                    {
                        // Write pairs without global section creating
                        foreach (var pair in ini.Global.Pairs)
                        {
                            if (Settings.WriteNotes && pair.Value.Notes != null)
                            {
                                foreach (var note in pair.Value.Notes)
                                    sw.WriteLine(format.WriteNote(note));
                            }
                            sw.WriteLine(format.WritePair(pair));
                        }
                    }

                    if (Settings.EndSectionWithBlankLine)
                        sw.Write(format.WriteSectionEnd()); // Make section blank line

                    // Includes that belongs to Global
                    WriteIncludesToFile(sw, ini, format, "");

                    ini.ht_full.Remove(""); // Global backuped our global section for now, we'll return global back to table on the end of saving process!
                }

                // Write all sections to file
                foreach (var section in ini.ht_full.Values)
                {
                    if (Settings.WriteNotes && section.Notes != null)
                    {
                        // Write all notes from section to file
                        foreach (var note in section.Notes)
                            sw.WriteLine(format.WriteNote(note));
                    }

                    // Write section header itself to file
                    sw.WriteLine(format.WriteSection(section));

                    if (section.Pairs != null)
                    {
                        // Write pairs to section
                        foreach (var pair in section.Pairs)
                        {
                            if (Settings.WriteNotes && pair.Value.Notes != null)
                            {
                                // Write notes from each pair before pair itself
                                foreach (var note in pair.Value.Notes)
                                    sw.WriteLine(format.WriteNote(note));
                            }

                            // Write pair itself to file
                            sw.WriteLine(format.WritePair(pair));
                        }
                    }

                    // Write end line after section
                    if (Settings.EndSectionWithBlankLine)
                        sw.Write(format.WriteSectionEnd());

                    // Write all includes under section
                    WriteIncludesToFile(sw, ini, format, section.Name);
                }

                // Flush all data to underlying stream before closing StreamWriterWrapper
                // Buffer is 4K or so, when reading files smaller than 4K, stream was not filled with data! (FIXED)
                sw.Flush();

                // Set position to start again! (most ppls will try to read new data in stream from start OFC!)
                stream.Position = 0L;
            }

            // We're adding global section back to main table!
            if (!Settings.CreateGlobalSection)
                ini.ht_full.Add("", ini.Global);

            // Call event
            if (FileSaved != null)
                FileSaved();
        }

        public void Save(INI ini, string filepath, Encoding encoding, IIniFormatable format = null)
        {
            using (FileStream fs = File.Create(filepath))
            {
                Save(ini, fs, encoding, format);
            }
        }

        public void Save(INI ini, string filepath)
        {
            Save(ini, filepath, ini.Encoding);
        }

        public void Save(INI ini)
        {
            // Backup parser settings
            IniParserSettings backup = _settings;

            // Use INI settings for parser
            _settings = ini.Settings;

            Save(ini, ini.Filename, ini.Encoding);

            // Return parser settings back
            _settings = backup;
        }

        #endregion

        #region Custom Part
        private void RaiseOnErrorEvent(string info, ErrorLevelType infoLevel = ErrorLevelType.Info)
        {
            var handler = OnError;
            if (handler != null)
                handler(info, infoLevel);
        }

        private void CacheSettings()
        {
            // MUST BE CALLED BEFORE LOAD/SAVE - to refresh settings before using it in parser!
            // Caching this can significantly speed up Split() methods - refresh on new user settings
            _inheritanceSplit = new string[] { _settings.InheritanceChar };
            _pairSplit = new string[] { _settings.PairOperator };
            _includeCount = _settings.IncludeStartChar.Length + _settings.IncludeEndChar.Length;
            _sectionCount = _settings.SectionStartChar.Length + _settings.SectionEndChar.Length;
        }
        #endregion

        #region Static Methods
        // Vrátí relativní cestu k souboru na základě adresáře bázového
        // soubor musí obsahovat absolutní cestu a adresář může být obojí (v případě relativní cesty je opraven na absolutní)
        public static string GetRelativePath(string filename, string currentDirectory)
        {
            // Pokud nezačíná: C:Folder, C:\Folder, \\PC\Folder - jedná se o relativní cestu složky
            if (!Path.IsPathRooted(currentDirectory))
                currentDirectory = Path.GetFullPath(currentDirectory); // Změníme na absolutní

            Uri pathUri = new Uri(filename);

            // Folders must end in a slash
            if (!currentDirectory.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                currentDirectory += Path.DirectorySeparatorChar;
            }

            Uri folderUri = new Uri(currentDirectory);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        public static string GetAbsolutePath(string filename, string currentDirectory)
        {
            // Pokud nezačíná: C:Folder, C:\Folder, \\PC\Folder - jedná se o relativní cestu složky
            if (!Path.IsPathRooted(currentDirectory))
                currentDirectory = Path.GetFullPath(currentDirectory); // Změníme na absolutní

            return Path.Combine(currentDirectory, filename);
        }

        public static Encoding GetFileEncoding(string srcFile)
        {
            // *** Use Default of Encoding.Default (Ansi CodePage)
            Encoding enc = Encoding.Default;

            // *** Detect byte order mark if any - otherwise assume default
            byte[] buffer = new byte[5];
            FileStream file = new FileStream(srcFile, FileMode.Open);
            file.Read(buffer, 0, 5);
            file.Close();

            if (buffer[0] == 0xef && buffer[1] == 0xbb && buffer[2] == 0xbf)
                enc = Encoding.UTF8;
            else if (buffer[0] == 0xfe && buffer[1] == 0xff)
                enc = Encoding.Unicode;
            else if (buffer[0] == 0 && buffer[1] == 0 && buffer[2] == 0xfe && buffer[3] == 0xff)
                enc = Encoding.UTF32;
            else if (buffer[0] == 0x2b && buffer[1] == 0x2f && buffer[2] == 0x76)
                enc = Encoding.UTF7;

            return enc;
        }
        #endregion
    }
}
