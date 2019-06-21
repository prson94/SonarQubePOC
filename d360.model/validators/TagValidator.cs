using d360.core.entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.validators
{
    public class TagValidator
    {
        public static void ValidateForPost(TagApiModel model)
        {
            if (model == null)
            {
                throw new Exception("Invalid tag specified [null model].");
            }

            if (string.IsNullOrEmpty(model.Value))
            {
                throw new Exception("Invalid tag specified [no value].");
            }

            if (model.Value.Length > 250)
            {
                throw new Exception("Invalid tag specified [too long].");
            }
        }

        public static void ValidateForPut(Guid uid, TagApiModel model)
        {
            if (model == null)
            {
                throw new Exception("Invalid tag specified [null model].");
            }

            if (string.IsNullOrEmpty(model.Value))
            {
                throw new Exception("Invalid tag specified [no value].");
            }

            if (model.Value.Length > 250)
            {
                throw new Exception("Invalid tag specified [too long].");
            }

            if (uid == Guid.Empty)
            {
                throw new Exception("Invalid uid specified.");
            }

            if (uid != model.uid)
            {
                throw new Exception("Invalid update tag request specified uid doesnt match model uid.");
            }
        }

    }
}
