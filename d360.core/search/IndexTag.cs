using System;
using System.Collections.Generic;

namespace d360.core.search
{
	public class IndexTag : IEqualityComparer<IndexTag>, IEquatable<IndexTag>
    {
        public Guid? Uid { get; set; }
        
        public string Value { get; set; }
        
        private string _highlight = null;
        
        public string Highlight
        {
            get
            {
                return _highlight ?? Value;
            }
            set
            {
                _highlight = value;
            }
        }

        public bool Equals(IndexTag other)
        {
            return other?.Uid == Uid;
        }

        public bool Equals(IndexTag x, IndexTag y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(IndexTag obj)
        {
            return obj.Uid.GetHashCode();
        }
        
        public override bool Equals(object obj) => Equals(obj as IndexTag);
        public override int GetHashCode()
        {
            return GetHashCode(this);
        }
    }
}
