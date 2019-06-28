using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace igx.IntegrationTests.TestData
{
    public class TagTestData
    {
        private static JObject _testJSON = null;
        public static JObject TagJSON { get {

                if (_testJSON == null)
                {
                    JObject @object = new JObject();
                    @object.Add(new JProperty("uid", "00000000-0000-0000-0000-000000000000"));
                    @object.Add(new JProperty("Value", "int_test_tag" + Guid.NewGuid()));
                    @object.Add(new JProperty("CreatedByUid", "00000000-0000-0000-0000-000000000000"));
                    @object.Add(new JProperty("CreatedOn", "2019-06-21T13:11:58.208Z"));
                    @object.Add(new JProperty("UpdatedByUid", "00000000-0000-0000-0000-000000000000"));
                    @object.Add(new JProperty("UpdatedOn", "2019-06-21T13:11:58.208Z"));


                    _testJSON = @object;
                }

                return _testJSON;
            }
            set { _testJSON = value; }
        }

    }
}
