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

        private static string _testJSONAsset = string.Empty;
        public static string AssetTypeJSON
        {
            get
            {

                if (_testJSONAsset == string.Empty)
                {
                    _testJSONAsset = @"{
                                      ""Name"": ""MetricsAssetTest" + Guid.NewGuid() + @""",
                                      ""Class"": ""Glossary"",
                                      ""Description"": """",
                                      ""AutoDisplayDescription"": true,
                                      ""DisplayFormat"": ""{Name}"",
                                      ""IconStyle"": {
                                        ""ForeColor"": ""#FFF"",
                                        ""BackColor"": ""#000""
                                      },
                                      ""Notes"": """"
                                    }";
                }

                return _testJSONAsset;
            }
            set { _testJSONAsset = value; }
        }

        private static string _metricsModel = string.Empty;
        public static string MetricModel
        {
            get
            {

                if (_metricsModel == string.Empty)
                {
                    _metricsModel = @"{
                                      ""AssetTypeUid"": """ + AssetTypeGuid + @""",
                                      ""IsGroup"": false,
                                      ""Name"": ""metric_int_test_" + Guid.NewGuid() + @""",
                                      ""Description"": ""string"",
                                      ""EffectiveDate"": ""2019-06-24T11:59:03.874Z"",
                                      ""Weight"": 1,
                                      ""ConditionAndOr"": ""o"",
                                      ""Conditions"": [
                                        {
                                          ""FieldTypeID"": " + NameFieldTypeId + @",
                                          ""Operator"": ""eq"",
                                          ""Values"": ""Test name""
                                        }
                                      ]
                                    }";
                }

                return _metricsModel;
            }
            set { _metricsModel = value; }
        }


        private static string _newAsset = string.Empty;
        public static string NewAsset
        {
            get
            {
                if (string.IsNullOrEmpty(_newAsset))
                {
                    _newAsset = @"[
                                      {
                                        ""Fields"": {
                                          ""Name"": ""Metric test asset""
                                        }
                                    }
                                    ]";
                }
                return _newAsset;
            }
        }
    }
}
