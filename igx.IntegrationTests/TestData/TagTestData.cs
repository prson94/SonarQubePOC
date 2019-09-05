using Newtonsoft.Json.Linq;
using System;

namespace igx.IntegrationTests.TestData
{
    public class TagTestData
    {
        public static int TagsCount = 0;

        public static JArray AllItems = null;

        private static JObject _testJSON = null;
        public static JObject TagJSON
        {
            get
            {

                if (_testJSON == null)
                {
                    JObject @object = new JObject();
                    @object.Add(new JProperty("Value", "int_test_tag" + Guid.NewGuid()));
                    _testJSON = @object;
                }

                return _testJSON;
            }
            set { _testJSON = value; }
        }

        private static JObject _testJSON2 = null;
        public static JObject TagJSON2
        {
            get
            {

                if (_testJSON2 == null)
                {
                    JObject @object = new JObject();
                    @object.Add(new JProperty("Value", "int_test_tag" + Guid.NewGuid()));
                    _testJSON2 = @object;
                }

                return _testJSON2;
            }
            set { _testJSON2 = value; }
        }

        private static JObject _testJSON3 = null;
        public static JObject TagJSON3
        {
            get
            {

                if (_testJSON3 == null)
                {
                    JObject @object = new JObject();
                    @object.Add(new JProperty("Value", "int_test_tag" + Guid.NewGuid()));
                    _testJSON3 = @object;
                }

                return _testJSON3;
            }
            set { _testJSON3 = value; }
        }
    }
}
