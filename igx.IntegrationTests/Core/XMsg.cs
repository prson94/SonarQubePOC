using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace igx.IntegrationTests
{
    public static class XMsg
    {
        public static string BadResponseCode = "Bad response code from this request!";
        public static string BadContentType = "Bad content type -json expected!";
        public static string NoContent = "No content returned!";

        public static string MissingField(string field) => $"Missing property '{field}' in response!";
        public static string MissingAsset = "Asset missing from response!";

        public static string InvalidFieldValue(string field) => $"Invalid value on field '{field}' in response!";
        public static string InvalidCount = $"Invalid count of items in response!";
        public static string ExecutionStatusErr = "Execution status check failed";


    }
}