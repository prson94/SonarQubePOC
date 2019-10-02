using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace igx.IntegrationTests.TestData
{
    public sealed class AssetTestData
    {
        private static JObject _assetTypeInsert = null;
        private static JArray _assetInserts = null;
        private static JArray _assetUpdates = null;
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
                        jObject.Add(new JProperty("Name", "AssetIntegrationTest-" + Guid.NewGuid().ToString()));
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

        public static JArray AssetInserts
        {
            get
            {
                lock (padlock)
                {
                    if (_assetInserts == null)
                    {
                        JArray inserts = new JArray();

                        JObject asset1 = new JObject();
                        JObject field1 = new JObject();
                        field1.Add(new JProperty("Name", "GUID_NAME_" + Guid.NewGuid().ToString()));
                        asset1.Add("Fields", field1);

                        JObject asset2 = new JObject();
                        JObject field2 = new JObject();
                        field2.Add(new JProperty("Name", "v2GUID_NAME_" + Guid.NewGuid().ToString()));
                        asset2.Add("Fields", field2);

                        inserts.Add(asset1);
                        inserts.Add(asset2);

                        _assetInserts = inserts;
                    }
                    return _assetInserts;
                }
            }
        }
        public static JArray AssetUpdates
        {
            get
            {
                lock (padlock)
                {
                    if (_assetUpdates == null)
                    {
                        JArray updates = new JArray();

                        JObject asset1 = new JObject();
                        JObject field1 = new JObject();
                        field1.Add(new JProperty("Name", "PutEdited/GUID_NAME_" + Guid.NewGuid().ToString()));
                        asset1.Add("Fields", field1);

                        JObject asset2 = new JObject();
                        JObject field2 = new JObject();
                        field2.Add(new JProperty("Name", "PutEdited/v2GUID_NAME_" + Guid.NewGuid().ToString()));
                        asset2.Add("Fields", field2);

                        updates.Add(asset1);
                        updates.Add(asset2);
                        _assetUpdates = updates;
                    }
                    return _assetUpdates;
                }
            }
        }

        public static JArray GetDeleteAssetJSON(List<Guid> uids)
        {
            JArray forDeletes = new JArray();

            foreach (var uid in uids)
            {
                JObject jObject = new JObject();
                jObject.Add(new JProperty("Uid", uid.ToString()));
                jObject.Add(new JProperty("Cascade", true));
                forDeletes.Add(jObject);
            }

            return forDeletes;
        }


        public static JArray GetDeleteJsonForAssetTypeUid(string assetTypeUid, bool isCascade)
        {
            var ret = new JArray();
            var @obj = new JObject();

            obj.Add(new JProperty("Uid", assetTypeUid));
            obj.Add(new JProperty("Cascade", isCascade.ToString()));

            ret.Add(@obj);
            return ret;
        }
    }
}
