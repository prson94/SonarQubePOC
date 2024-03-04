using System;

namespace d360.core.entities
{
    public class TagApiUpsertModel
    {
        public string Value { get; set; }
		public Guid? TagTypeUid { get; set; }
    }
}
