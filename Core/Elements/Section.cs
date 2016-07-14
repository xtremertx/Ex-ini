using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace eX_INI
{
    // Represents section in .INI file and also .ex-INI
    public class Section
    {
        #region Ctors

        public Section(string name) : this(name, null, null) { }

        public Section(string name, Dictionary<string, KeyValue> pairs) : this(name, null, pairs) { }

        public Section(string name, Section inheritedFrom, Dictionary<string, KeyValue> pairs) : this(name, inheritedFrom, pairs, null) { }

        public Section(string name, Section inheritedFrom, Dictionary<string, KeyValue> pairs, params string[] notes)
        {
            if (notes != null)
            {
                var list = new List<string>(notes.Length);
                foreach (var note in notes)
                {
                    list.Add(note);
                }

                this._notes = list;
            }

            this.Name = name;
            this._base = inheritedFrom;
            this.Pairs = pairs;
        }

        #endregion

        #region Properties

        public string Name { get; set; }

        public Dictionary<string, KeyValue> Pairs { get; set; }

        // Possible Exception!
        public KeyValue this[string key]
        {
            get { return Pairs[key]; }
            set { Pairs[key] = value; }
        }

        private Section _base;
        public Section Base
        {
            get { return _base; }
            set
            {
                // A cant inherit from A!
                if (!this.Equals(value))
                    _base = value;
            }
        }

        private List<string> _notes;
        public List<string> Notes
        {
            get { return _notes; }
            set
            {
                _notes = value;
            }
        }

        #endregion

        #region Methods

        // Should be error FREE (null key means "" key)
        public string GetValue(string key)
        {
            KeyValue temp;
            Pairs.TryGetValue(key ?? "", out temp);
            return temp.Value;
        }

        public Dictionary<string, KeyValue> GetValues(bool inherited)
        {
            Dictionary<string, KeyValue> tb = this.Pairs;

            // Inherited values too?
            if (inherited && (_base != null))
            {
                // Get inherited members only for B
                foreach (var pair in _base.Pairs)
                {
                    if (!tb.ContainsKey(pair.Key))
                        tb.Add(pair.Key, pair.Value);
                }
            }
            return tb;
        }

        public override string ToString()
        {
            return Name;
        }

        #endregion
    }
}
