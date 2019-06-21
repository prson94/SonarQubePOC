using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace igx.IntegrationTests.TestData
{
    public class TagTestData
    {
        private static string _testJSON = string.Empty;
        public static string TagJSON { get {

                if (_testJSON == string.Empty)
                {
                    _testJSON = @"{
                              ""uid"": ""00000000-0000-0000-0000-000000000000"",
                              ""Value"": ""int_test_tag" + Guid.NewGuid() + @""",
                              ""CreatedByUid"": ""00000000-0000-0000-0000-000000000000"",
                              ""CreatedOn"": ""2019-06-21T13:11:58.208Z"",
                              ""UpdatedByUid"": ""00000000-0000-0000-0000-000000000000"",
                              ""UpdatedOn"": ""2019-06-21T13:11:58.208Z""}";
                }

                return _testJSON;
            }
            set { _testJSON = value; }
        }

    }
}
