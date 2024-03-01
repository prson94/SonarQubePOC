using System;
using System.Text.RegularExpressions;
using d360.core;
using d360.core.entities;

namespace d360.model.validators
{
	public class TagTypeValidator
	{
		public static void ValidateForPost(TagTypeApiUpsertModel model)
		{
			string isTagTypeBlank = "";

			if (model == null)
			{
				throw new Exception("Invalid tag type specified [null model].");
			}

			if (string.IsNullOrEmpty(model.Value))
			{
				throw new Exception("Invalid tag type specified [no value].");
			}

			if (!string.IsNullOrEmpty(model.Value))
			{
				isTagTypeBlank = Regex.Replace(model.Value, @"\s+", "");
			}

			if (isTagTypeBlank.Length < 1)
			{
				throw new Exception("Tag type must be as least 1 character long in length.");
			}

			if (model.Value.Length > 100)
			{
				throw new Exception("Invalid tag type specified [too long].");
			}

			if(!model.Value.IsValidForTag())
			{
				throw new Exception("Invalid tag type specified [invalid characters]");
			}
		}

		public static void ValidateForPut(Guid uid, TagTypeApiUpsertModel model)
		{
			ValidateForPost(model);

			if (uid == Guid.Empty)
			{
				throw new ArgumentException("Invalid uid specified.");
			}
		}
	}
}
