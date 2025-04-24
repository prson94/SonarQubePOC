using System;

namespace d360.core.entities
{
	public class TagApiUpsertModel
	{
		public Guid? TagTypeUid { get; set; }
		public string Value { get; set; }
	}
}