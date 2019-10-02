using Newtonsoft.Json.Linq;
using System;

namespace igx.IntegrationTests.TestData
{
    public sealed class AssetTypeTestData
    {
        private static JObject _assetTypeInsert = null;
        private static string _executionUrl = null;


        private static readonly object padlock = new object();
        public static JObject AssetTypeInsert
        {
            get
            {
                lock (padlock)
                {
                    if (_assetTypeInsert == null)
                    {
                        JObject jObject = new JObject();
                        jObject.Add(new JProperty("Name", "AssetTypeIntegrationTest-" + Guid.NewGuid().ToString()));
                        jObject.Add(new JProperty("Class", "Business"));
                        jObject.Add(new JProperty("DisplayFormat", "{Name}"));
                        jObject.Add(new JProperty("Description", "Integration test description!"));

                        JObject iconStyle = new JObject();
                        iconStyle.Add(new JProperty("BackColor", "#FFF"));
                        iconStyle.Add(new JProperty("ForeColor", "#000"));

                        jObject.Add(new JProperty("IconStyle", iconStyle));

                        _assetTypeInsert = jObject;
                    }
                    return _assetTypeInsert;
                }
            }
        }


        public static string ExecutionUrl
        {
            get
            {
                lock (padlock)
                {
                    if (_executionUrl == null)
                    {
                        return string.Empty;
                    }
                    return _executionUrl;
                }
            }
            set
            {
                _executionUrl = value;
            }
        }

        public static JArray GetDeleteAssetTypeJSON(Guid uid)
        {
            JArray forDeletes = new JArray();

            JObject jObject = new JObject();
            jObject.Add(new JProperty("Uid", uid.ToString()));
            jObject.Add(new JProperty("Cascade", true));
            forDeletes.Add(jObject);

            return forDeletes;
        }
    }
}
