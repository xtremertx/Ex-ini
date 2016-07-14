using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace eX_INI
{
    // Hashtable with includes using key="SectionName", section name is telling us where this include belongs to...
    // Includes are always under section, if there is none, they belongs to "\0" section
    // When saving, parser is always trying to get "\0" section first
    public class Include : IEqualityComparer<Include>, IEquatable<Include>
    {
        #region Ctor's

        public Include(string path) : this(path, null)
        {
        }

        public Include(string path, params string[] notes)
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

            this.Path = path;
        }

        #endregion

        #region Consts

        // Used for signalizing that include belong to start of file! (For user not used internally)
        public const string START_OF_FILE = "\0";

        #endregion

        #region Properties

        public string Path { get; set; }

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

        #region Comparing interface
        public bool Equals(Include x, Include y)
        {
            //// Same path but differenct counts == include, but same count not detected change in notes!
            // <\Path> count: 0
            // <\Path> count: 2
            //bool sameCount = false;
            //if (x.Notes != null && y.Notes != null)
            //{
            //    sameCount = (x.Notes.Count == y.Notes.Count);
            //}

            // But yes there can be include with same path and different notes - but we go for performance now ;)
            return x.Path.Equals(y.Path, StringComparison.OrdinalIgnoreCase); //&& sameCount;
        }

        public int GetHashCode(Include obj)
        {
           return obj.GetHashCode();
        }

        // This one is basically used by List<T> Contains() method ;)
        public bool Equals(Include other)
        {
            return Equals(this, other);
        }
        #endregion
    }
}
