using System;

namespace Validator
{
    internal class KeyTable : IEquatable<KeyTable>
    {
        public string Table { get; set; }
        public string Key { get; set; }

        public bool Equals(KeyTable other)
        {
            if (other == null)
            {
                return false;
            }

            return (other.Key == Key && other.Table == Table);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            
            KeyTable kt = obj as KeyTable;

            if (kt == null)
                return false;
            else
                return Equals(kt);

        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}