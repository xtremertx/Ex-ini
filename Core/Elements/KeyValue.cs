using System;
using System.Collections.Generic;
using System.Text;

namespace eX_INI
{
    // Represents value of some key
    public class KeyValue
    {
        #region Ctors

        public KeyValue(string value) : this(value, null) { }

        public KeyValue(string value, params string[] notes)
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

            this.Value = value;
        }

        #endregion

        #region Properties

        public string Value { get; set; }

        private List<string> _notes;
        public List<string> Notes
        {
            get { return _notes; }
            set { _notes = value; }
        }

        #endregion
    }
}
