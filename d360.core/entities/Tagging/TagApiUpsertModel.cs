using System;

namespace d360.core.entities
{
	public class TagApiUpsertModel
	{
		private string _value;

		public string Value
		{
			get => _value;
			set
			{
				string trimmedValue = value?.Trim();
			}
		}

		public Guid? TagTypeUid { get; set; }
	}
}