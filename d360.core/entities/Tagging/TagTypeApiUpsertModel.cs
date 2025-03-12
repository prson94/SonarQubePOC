using System;

namespace d360.core.entities
{
	public class TagTypeApiUpsertModel
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
					throw new ArgumentException($"Provided TagType value '{value}' is not valid");
				}
			}
		}
	}
}