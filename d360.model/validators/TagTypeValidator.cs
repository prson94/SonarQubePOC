using System;
using System.Net;
using System.Text.RegularExpressions;
using d360.core;
using d360.core.entities;
using d360.core.resources;
using d360.core.validators;

namespace d360.model.validators
{
	public static class TagTypeValidator
	{
		public static WorkHttpStatus ValidateForPost(TagTypeApiUpsertModel model)
		{
			string isTagTypeBlank = "";

			if (model == null)
			{
				return new WorkHttpStatus(HttpStatusCode.BadRequest, TagErrors.InvalidRequestHttpErrorTitle, $"{TagErrors.InvalidTagTypeSpecified}");
			}

			if (string.IsNullOrEmpty(model.Value))
			{
				return new WorkHttpStatus(HttpStatusCode.BadRequest, TagErrors.InvalidRequestHttpErrorTitle, $"{TagErrors.InvalidTagTypeSpecifiedNoValue}");
			}

			if (!string.IsNullOrEmpty(model.Value))
			{
				isTagTypeBlank = Regex.Replace(model.Value, @"\s+", "");
			}

			if (isTagTypeBlank.Length < 1)
			{
				return new WorkHttpStatus(HttpStatusCode.BadRequest, TagErrors.InvalidRequestHttpErrorTitle, $"{TagErrors.InvalidTagTypeShort}");
			}

			if (model.Value.Length > 100)
			{
				return new WorkHttpStatus(HttpStatusCode.BadRequest, TagErrors.InvalidRequestHttpErrorTitle, $"{TagErrors.InvalidTagTypeLong}");
			}

			if(!model.Value.IsValidForTag())
			{
				return new WorkHttpStatus(HttpStatusCode.BadRequest, TagErrors.InvalidRequestHttpErrorTitle, $"{TagErrors.InvalidTagTypeCharacters}");
			}

			return new WorkHttpStatus(HttpStatusCode.OK, "", "");
		}

		public static WorkHttpStatus ValidateForPut(Guid uid, TagTypeApiUpsertModel model)
		{
			var postStatus = ValidateForPost(model);

			if (postStatus.StatusCode != HttpStatusCode.OK)
			{
				return postStatus;
			}

			if (uid == Guid.Empty)
			{
				return new WorkHttpStatus(HttpStatusCode.BadRequest, TagErrors.InvalidRequestHttpErrorTitle, $"{TagErrors.InvalidTagTypeUid}");
			}

			return new WorkHttpStatus(HttpStatusCode.OK, "", "");
		}
	}
}
