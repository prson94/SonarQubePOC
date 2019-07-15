using Newtonsoft.Json.Linq;
using System;

namespace igx.IntegrationTests.TestData
{
    public sealed class XRefTestData
    {
        private static JObject _model = null;
        private static JArray _modelsList = null;


        private static readonly object padlock = new object();
        public static JObject XRefModel
        {
            get
            {
                lock (padlock)
                {
                    if (_model == null)
                    {
                        var obj = new JObject();
                        obj.Add(new JProperty("uid", Guid.NewGuid()));
                        obj.Add(new JProperty("DataSource", "testDataSource"));
                        obj.Add(new JProperty("ExternalID", "testExternalId"));
                        obj.Add(new JProperty("Type", "testType_" + Guid.NewGuid()));
                        obj.Add(new JProperty("FieldHash", "testFieldHash"));

                        _model = obj;
                    }
                    return _model;
                }
            }
        }
        public static JArray XRefModelList
        {
            get
            {
                lock (padlock)
                {
                    if (_modelsList == null)
                    {

                        //Using same data source as final delete in tests will be over this unique datasource
                        var sameDataSourceName = "testSameDatasource" + Guid.NewGuid();

                        var arr = new JArray();

                        var model1 = new JObject();
                        var model2 = new JObject();
                        arr.Add(model1);
                        arr.Add(model2);

                        model1.Add(new JProperty("uid", Guid.NewGuid()));
                        model1.Add(new JProperty("DataSource", sameDataSourceName));
                        model1.Add(new JProperty("ExternalID", "testExternalId2"));
                        model1.Add(new JProperty("Type", "testType_" + Guid.NewGuid()));
                        model1.Add(new JProperty("FieldHash", "testFieldHash2"));

                        model2.Add(new JProperty("uid", Guid.NewGuid()));
                        model2.Add(new JProperty("DataSource", sameDataSourceName));
                        model2.Add(new JProperty("ExternalID", "testExternalId2"));
                        model2.Add(new JProperty("Type", "testType2_" + Guid.NewGuid()));
                        model2.Add(new JProperty("FieldHash", "testFieldHash2"));

                        _modelsList = arr;
                    }
                    return _modelsList;
                }
            }
        }

    }
}
