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
				if (trimmedValue != null && trimmedValue.IsValidForTag())
				{
					_value = trimmedValue;
				}
				else
				{
					throw new ArgumentException($"Provided Tag value '{value}' is not valid");
				}
			}
		}

		public Guid? TagTypeUid { get; set; }
	}
}