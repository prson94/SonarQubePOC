using d360.core.entities;
using System;
using System.Text.RegularExpressions;

namespace d360.model.validators
{
    public class TagValidator
    {
        public static void ValidateForPost(TagApiUpsertModel model)
        {
            string isTagBlank = "";
            if (model == null)
            {
                throw new Exception("Invalid tag specified [null model].");
            }
            if (string.IsNullOrEmpty(model.Value))
            {
                throw new Exception("Invalid tag specified [no value].");
            }

            if (!string.IsNullOrEmpty(model.Value))
            {
                isTagBlank = Regex.Replace(model.Value, @"\s+", "");
            }

            if (isTagBlank.Length < 1)
            {
                throw new Exception("Tag must be as least 1 character long in length.");
            }

            if (model.Value.Length > 100)
            {
                throw new Exception("Invalid tag specified [too long].");
            }
        }

        public static void ValidateForPut(Guid uid, TagApiUpsertModel model)
        {
            string isTagBlank = "";
            if (model == null)
            {
                throw new Exception("Invalid tag specified [null model].");
            }

            if (string.IsNullOrEmpty(model.Value))
            {
                throw new Exception("Invalid tag specified [no value].");
            }
            if (!string.IsNullOrEmpty(model.Value))
            {
                isTagBlank = Regex.Replace(model.Value, @"\s+", "");
            }

            if (isTagBlank.Length < 1)
            {
                throw new Exception("Tag must be as least 1 character long in length.");
            }

            if (model.Value.Length > 100)
            {
                throw new Exception("Invalid tag specified [too long].");
            }

            if (uid == Guid.Empty)
            {
                throw new Exception("Invalid uid specified.");
            }

        }

    }
}
