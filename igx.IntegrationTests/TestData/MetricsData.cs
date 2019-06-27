using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace igx.IntegrationTests.TestData
{
    public class MetricTestsData
    {



        public static string AssetTypeGuid = "";
        public static string AssetUid = "";
        public static string NameFieldTypeId = "";
        public static string MetricUid = "";

        private static JObject _testJSONAsset = null;
        public static JObject AssetTypeJSON
        {
            get
            {

                if (_testJSONAsset == null)
                {
                    var @object = new JObject();
                    @object.Add(new JProperty("Name", "MetricsAssetTest" + Guid.NewGuid()));
                    @object.Add(new JProperty("Class", "Glossary"));
                    @object.Add(new JProperty("Description", ""));
                    @object.Add(new JProperty("AutoDisplayDescription", true));
                    @object.Add(new JProperty("DisplayFormat", "{Name}"));
                    @object.Add(new JProperty("Notes", ""));

                    var @iconStyle = new JObject();
                    @iconStyle.Add(new JProperty("ForeColor", "#FFF"));
                    @iconStyle.Add(new JProperty("BackColor", "#000"));

                    @object.Add(new JProperty("IconStyle", @iconStyle));

                    _testJSONAsset = @object;
                }

                return _testJSONAsset;
            }
            set { _testJSONAsset = value; }
        }

        private static JObject _metricsModel = null;
        public static JObject MetricModel
        {
            get
            {

                if (_metricsModel == null)
                {
                    var @object = new JObject();
                    @object.Add(new JProperty("AssetTypeUid", AssetTypeGuid));
                    @object.Add(new JProperty("IsGroup", false));
                    @object.Add(new JProperty("Name", "metric_int_test_" + Guid.NewGuid()));
                    @object.Add(new JProperty("Description", "string"));
                    @object.Add(new JProperty("EffectiveDate", "2019-06-24T11:59:03.874Z"));
                    @object.Add(new JProperty("Weight", 1));
                    @object.Add(new JProperty("ConditionAndOr", "o"));

                    var @conditionArray = new JArray();
                    var @condition = new JObject();
                    condition.Add(new JProperty("FieldTypeID", NameFieldTypeId));
                    condition.Add(new JProperty("Operator", "eq"));
                    condition.Add(new JProperty("Values", "Test name"));

                    @conditionArray.Add(condition);

                    @object.Add(new JProperty("Conditions", @conditionArray));
                    _metricsModel = @object;
                }

                return _metricsModel;
            }
            set { _metricsModel = value; }
        }


        private static JArray _newAssets = null;
        public static JArray NewAssets
        {
            get
            {
                if (_newAssets == null)
                {

                    var arr = new JArray();
                    var @object = new JObject();
                    arr.Add(@object);

                    var @field = new JObject();
                    @object.Add(new JProperty("Fields", field));

                    field.Add(new JProperty("Name", "Metric test asset"));

                    _newAssets = arr;

                }
                return _newAssets;
            }
        }

        private static JArray _metricResults = null;

        public static JArray MetricResultJson
        {
            get
            {
                if (_metricResults == null)
                {
                    var test = new JArray();
                    var @object = new JObject();

                    @object.Add(new JProperty("AssetUid", AssetUid));
                    @object.Add(new JProperty("MetricAssetUid", MetricUid));
                    @object.Add(new JProperty("EffectiveDate", "2019-06-25T09:12:06.127Z"));
                    @object.Add(new JProperty("Result", true));

                    test.Add(@object);
                    _metricResults = test;
                }

                return _metricResults;
            }
        }
    }
}
