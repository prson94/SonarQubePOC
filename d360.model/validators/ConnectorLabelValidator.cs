using System;
using System.Text.RegularExpressions;

using d360.core.entities;

namespace d360.model.validators
{
    public class ConnectorLabelValidator
    {
        public static void ValidateForPost(ConnectorLabelPostModel model)
        {
            BaseValidation(model);
        }

        public static void ValidateForPut(Guid uid, ConnectorLabelPostModel model)
        {
            BaseValidation(model);

            if (uid == Guid.Empty)
            {
                throw new Exception("Invalid uid specified.");
            }
        }

        public static void BaseValidation(ConnectorLabelPostModel model)
        {
            string isConnectorLabelBlank = "";

            if (model == null)
            {
                throw new Exception("Invalid connector label specified [null model].");
            }
            
            if (string.IsNullOrEmpty(model.Value))
            {
                throw new Exception("Invalid connector label specified [no value].");
            }

            if (!string.IsNullOrEmpty(model.Value))
            {
                isConnectorLabelBlank = Regex.Replace(model.Value, @"\s+", "");
            }

            if (isConnectorLabelBlank.Length < 1)
            {
                throw new Exception("Connector label must be as least 1 character long in length.");
            }

            if (model.Value.Length > 40)
            {
                throw new Exception("Invalid connector label specified [too long].");
            }
        }
    }
}
