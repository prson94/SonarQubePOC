using System;
using System.Collections.Generic;

namespace d360.core.search
{
	public class TypeaheadResult
    {
        public TypeaheadResult()
        {
            Tags = new List<IndexTag>();
        }
        
        public string Name { get; set; }
        
        public string DisplayName { get; set; }
        
        public string Group { get; set; }
        
        public string Type { get; set; }
        
        public string Url { get; set; }

        public string Icon { get; set; }
        
        public string ImageUrl { get; set; }
        
        public List<PathComponent> AssetPath { get; set; }
        
        public Guid? Uid { get; set; }
        
        public Guid? AssetTypeUid { get; set; }
        
        public List<IndexTag> Tags { get; set; }

        public bool MissingIcon()
        {
            return string.IsNullOrEmpty(Icon) && string.IsNullOrEmpty(ImageUrl);
        }
    }
}
