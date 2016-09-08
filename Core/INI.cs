using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace eX_INI
{
    /* 
     * 
     * FUTURE:
     * práce s .INI: (explicitní možnost uživatele uložit určitou sekci do .ini souboru a vymazat ji másledně z paměti, explicitní možnost loadovat uživatelem aktuálně potřebnou sekci, bez autoloadu celého souboru)
     *  
     */
    public class INI : IDisposable, IEnumerable<Section>
    {
        #region Globals

        // Full path to .INI or .ex-INI file 
        private string file;
        public string Filename
        {
            get { return file; }
            set { file = value; }
        }

        // Encoding of current INI object
        private Encoding coding;
        public Encoding Encoding
        {
            get { return coding; }
            set { coding = value; }
        }

        // Settings used by this INI object
        public IniParserSettings Settings { get; set; }

        // HashTable containning (sections=pairs)
        internal Dictionary<string, Section> ht_full = null;
        // Hashtable containing includes (position of include is determined by "section_name" or "\0" (same as: Include.START_OF_FILE))
        internal Dictionary<string, List<Include>> ht_incl = null;

        public Dictionary<string, List<Include>> Includes
        {
            get { return ht_incl; }
            set { ht_incl = value; }
        }

        #endregion

        #region Events

        // When new section is added or removed, reports section name and status
        public event Action<string, SectionState> Changed;

        #endregion

        #region Ctors
        // For creating new INI document
        public INI()
        {
            // Common settings for most INI files
            coding = Encoding.Default;
            ht_full = new Dictionary<string, Section>();
            Settings = new IniParserSettings();
        }

        // Creating INI from FILE
        public INI(string filePath)
            : this(filePath, null, null)
        {
        }

        // Creating INI from FILE, with encoding
        public INI(string filePath, Encoding encoding)
            : this(filePath, null, encoding)
        {
        }

        // Specifying settings (used for case-secnsitivity only)
        public INI(string filePath, IniParserSettings settings)
            : this(filePath, settings, null)
        {
        }

        // For creating or loading some file with ability to find out file encoding for us.
        public INI(string filePath, IniParserSettings settings, bool autoEncoding = false)
            : this(filePath, settings, (autoEncoding) ? IniParser.GetFileEncoding(filePath) : null)
        {
        }

        public INI(string filePath, IniParserSettings settings, Encoding encoding) : this()
        {
            file = filePath;
            Settings = settings;
            coding = encoding ?? Encoding.Default;
        }
        #endregion

        #region Enum - Section status report

        public enum SectionState : byte
        {
            Added,      // section added
            Removed,    // section removed    

            // section was changed (removed, added, changed value)
            //PairAdded,
            //PairRemoved,
            //PairChanged
        }

        #endregion

        #region Indexer

        // Possible Exception
        public Section this[string key]
        {
            get { return ht_full[key]; }
            set { ht_full[key] = value; }
        }

        #endregion

        #region Global

        // Returns global section or null
        private Section _global;
        public Section Global
        {
            get
            {
                if (_global == null)
                    _global = TryGetSection("");

                return _global;
            }
            private set
            {
                _global = value;
            }
        }

        #endregion

        // U can specify if u want only original section values or orig. plus inherited together
        public Dictionary<string, KeyValue> GetValues(string section, bool inherited = false)
        {
            // CaseSensitive?
            CaseSensitiveSection(ref section);

            // Get section
            Section sec;
            ht_full.TryGetValue(section, out sec);

            Dictionary<string, KeyValue> ht = null;

            // Exist
            if (sec != null)
            {
                ht = sec.Pairs;

                // Section is child
                if (inherited && sec.Base != null)
                {
                    // Get inherited members only for B
                    foreach (var pair in sec.Base.Pairs)
                    {
                        if (!ht.ContainsKey(pair.Key))
                            ht.Add(pair.Key, pair.Value);
                    }
                }
            }
            return ht;
        }

        // Get whole inheritence hierarchy for section (without virtual pairs)
        // Last child in inheritance will override all pairs (because it was a last override after all)
        // So if [A]0=A,1=A[B:A]0=B,5=B[C:B]0=C,6=C return == 0=C,6=C,5=B,1=A
        public Dictionary<string, KeyValue> GetWholeInheritance(Section section, Dictionary<string, KeyValue> dic = null)
        {
            if (section == null) return null;

            Dictionary<string, KeyValue> ht = (dic == null) ? section.Pairs : dic;

            // If A has B?
            if (section.Base != null)
            {
                KeyValue kvp;

                // Copy B to A table
                foreach (var pair in section.Base.Pairs)
                {
                    if (!ht.TryGetValue(pair.Key, out kvp))
                        ht.Add(pair.Key, pair.Value);
                }

                // If B has C ?
                if (section.Base.Base != null)
                {
                    // Call for B to get C
                    GetWholeInheritance(section.Base, ht);
                }
            }
            return ht;
        }

        public Dictionary<string, KeyValue> GetWholeInheritance(string section)
        {
            // CaseSensitive?
            CaseSensitiveSection(ref section);

            // Get section
            Section sec;
            ht_full.TryGetValue(section, out sec);

            return GetWholeInheritance(sec);
        }

        // U can specify if value is only from original or also inherited values, this can speed up method
        public string GetValue(string section, string key, bool inherited = false)
        {
            KeyValue value = null;

            var temp = GetValues(section, inherited);

            if (temp != null)
                temp.TryGetValue(key, out value);

            return (value == null) ? null : value.Value;
        }

        // If value is null, return defaultValue if its null too, return string.Empty as default
        public string GetValue(string section, string key, string defaulfValue, bool inherited = false)
        {
            return (GetValue(section, key, inherited) ?? defaulfValue ?? string.Empty);
        }

        // Exception will return default value
        public R GetValue<R>(Func<string, R> handler, string section, string key, bool inherited = false)
        {
            string value = GetValue(section, key, inherited);
            try
            {
                return handler(value);
            }
            catch (Exception)
            {
                return default(R);
            }
        }


        // [WARNING] If INI_VALID_VALUE is: (NULL || DEFAULT) it will be replaced with DEFAULT_VALUE!
        public R GetValue<R>(R defaultValue, Func<string, R> handler, string section, string key, bool inherited = false)
        {
            // Possible returned value: (Nullable/Reference types = null, Value types = default(..) || some_valid_value)
            R @return = GetValue<R>(handler, section, key, inherited);

            // Is Nullable/Reference type (conversion failed)?
            // Is ValueType (default value, conversion failed)?
            return (@return == null) ? defaultValue :
                ((@return.Equals(default(R))) ? defaultValue :@return); // [NOTE] Equals value types OK, ref. types FAILS and return original value :P

            // Should be slower coz of reflection
            //if (@return.GetType().IsValueType)
            //{
            //    // enum, struct or primitive type is already: default(R)
            //    return @return;
            //}
            //else
            //{
            //    // Reference type or nullable type can be null, lets check it
            //    // Return: defaultValue if null or same
            //    return (@return == null) ? defaultValue : @return;
            //}
        }

        public bool SectionExists(string section)
        {
            // CaseSensitive?
            CaseSensitiveSection(ref section);
            return ht_full.ContainsKey(section);
        }

        public bool KeyExists(string section, string key)
        {
            // CaseSensitive?
            CaseSensitiveSection(ref section);
            // Get section
            Section sec;
            ht_full.TryGetValue(section, out sec);

            return (sec != null && sec.Pairs.ContainsKey(key)) ? true : false;
        }

        public Section TryGetSection(string section)
        {
            // CaseSensitive?
            CaseSensitiveSection(ref section);
            // Get section
            Section temp;
            ht_full.TryGetValue(section, out temp);

            return temp;
        }

        // Returns: non-CaseSensitive string (in lower) if we are not using CaseSensitivity for "section names".
        // In future this can be applied on keys (caseSensitive keys)
        private void CaseSensitiveSection(ref string section)
        {
            // CaseSensitive false?
            if (!Settings.CaseSensitiveSections && !section.IsLowerCase()) section = section.ToLower();
        }

        public IEnumerable<Section> GetSections()
        {
            return ht_full.Values;
        }

        public IEnumerable<string> GetSectionNames()
        {
            return ht_full.Keys;
        }

        public void AddSection(Section s)
        {
            string name = s.Name;
            CaseSensitiveSection(ref name);

            if (!ht_full.ContainsKey(name))
                ht_full.Add(name, s);

            ChangedEventHandler(name, SectionState.Added);
        }

        // Can be used by inherited classes and inside the library
        internal protected void ChangedEventHandler(string section, SectionState status)
        {
            if (Changed != null)
                Changed(section, status);
        }

        public bool RemoveSection(string section)
        {
            CaseSensitiveSection(ref section);
            bool b = ht_full.Remove(section);
            ChangedEventHandler(section, SectionState.Removed);
            return b;
        }

        public void Clear()
        {
            ht_full.Clear();

            if(ht_incl != null)
                ht_incl.Clear();
        }

        // B is priority INI (will override all)
        // Specify behavior of hierarchy, B overrides A hierarchy or not?
        // [WARNING]: Everything is inside memory - 2 INI objects, eh maybe next time some threading solution, unfortunately library is not thread safe ;)
        public static void Merge(INI firstIni, INI secondIni, /*string saveAs = null, Encoding saveEncoding = null,*/ bool overrideInheritanceByIniB = false, bool mergeIncludes = false)
        {
            // INI A will be merged with INI B
            // INI A get new values from duplicated sections in B
            // INI A get new sections from B
            // INI A get new values from B (override)
            // Load whole config variables into memory!
            if (firstIni == null || secondIni == null)
                throw new ArgumentNullException();

            IniParser parser = new IniParser();
            
            // INI_A awaits to be initialized?
            if (firstIni.ht_full.Count < 1)
            {
                parser.Load(firstIni);
            }
            // INI_B awaits to be initialized?
            if (secondIni.ht_full.Count < 1)
            {
                parser.Load(secondIni);
            }

            // MERGE hashtables
            foreach (var pair in secondIni.ht_full)
            {
                Section currentSection = pair.Value;
                Section originalSection;

                // Override section in A by B
                // Add new pairs & override values of existing ones
                if ((originalSection = firstIni.TryGetSection(pair.Key)) != null)
                {
                    // Override inheritance by B?
                    if (overrideInheritanceByIniB)
                        originalSection.Base = currentSection.Base;

                    foreach (var key in currentSection.Pairs)
                    {
                        if (originalSection.Pairs.ContainsKey(key.Key))
                        {
                            // Override
                            originalSection.Pairs[key.Key] = key.Value;
                        }
                        else
                        {
                            // Add new one
                            originalSection.Pairs.Add(key.Key, key.Value);
                        }
                    }
                    // Save section to A
                    firstIni[pair.Key] = originalSection;
                }
                else
                {
                    // Add new section
                    firstIni.AddSection(currentSection);
                }

                INI.MergeIncludes(pair.Key, firstIni, secondIni);
            }

            // Fix: START_OF_FILE includes
            INI.MergeIncludes(Include.START_OF_FILE, firstIni, secondIni);

            //// Save into file - if requested by the user
            //if(!string.IsNullOrEmpty(saveAs))
            //    parser.Save(firstIni, saveAs, saveEncoding);
        }

        private static void MergeIncludes(string section, INI firstIni, INI secondIni)
        {
            List<Include> includes;
            if (secondIni.ht_incl.TryGetValue(section, out includes))
            {
                List<Include> originalIncludesList;

                // Merge existing includes into iniA
                // Find out if iniA have existing includes for section
                if (firstIni.ht_incl.TryGetValue(section, out originalIncludesList))
                {
                    // Add non-existing ones from iniB into iniA
                    foreach (var include in includes)
                    {
                        // iniA section dont contains this include? Add it! Otherwise merge differencies (notes) [notes are not merged right now]
                        //if (!originalIncludesList.Exists(i => i.Path.Equals(include.Path)))
                        if (!originalIncludesList.Contains(include))
                            originalIncludesList.Add(include);
                    }
                }
                else
                    firstIni.ht_incl.Add(section, includes);    // iniA dont have any includes for this section
            }
        }

        public void Dispose()
        {
            // Set all resources to NULL, hopefully GC release them more quickly!
            if(ht_full != null)
                this.Clear();

            file = null;
            coding = null;
            ht_incl = null;
            ht_full = null;
            GC.SuppressFinalize(this);
        }

        #region IEnumerable<T>
        // Support for foreach cycle
        public IEnumerator<Section> GetEnumerator()
        {
            return ht_full.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
        #endregion
    }
}
