using d360.core.entities;
using d360.core.enums;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace igx.IntegrationTests.TestData
{
    public sealed class AssetTestData
    {
        private static AssetTypeInsert _assetTypeInsert = null;
        private static List<AssetInsert> _assetInserts = null;
        private static List<AssetUpdate> _assetUpdates = null;
        private static string _executionUrl = null;

        private static readonly object padlock = new object();
        public static AssetTypeInsert assetTypeInsert
        {
            get
            {
                lock (padlock)
                {
                    if (_assetTypeInsert == null)
                    {
                        _assetTypeInsert = new AssetTypeInsert()
                        {
                            Name = "AssetIntegrationTest-" + Guid.NewGuid().ToString(),
                            Class = AssetTypeClass.Glossary,
                            DisplayFormat = "{Name}",
                            Description = "Integration test description!",
                            IconStyle = new IconStyleInsert()
                            {
                                BackColor = "#FFF",
                                ForeColor = "#000"
                            }
                        };
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

        public static List<AssetInsert> assetInserts
        {
            get
            {
                lock (padlock)
                {
                    if (_assetInserts == null)
                    {
                        var fields1 = new Dictionary<string, string>();
                        fields1.Add("Name", "GUID_NAME_" + Guid.NewGuid().ToString());
                        var first = new AssetInsert() { Fields = fields1 };
                        var fields2 = new Dictionary<string, string>();
                        fields2.Add("Name", "V2GUID_NAME_" + Guid.NewGuid().ToString());
                        var second = new AssetInsert() { Fields = fields2 };

                        _assetInserts = new List<AssetInsert>();
                        _assetInserts.Add(first);
                        _assetInserts.Add(second);
                    }
                    return _assetInserts;
                }
            }
        }
        public static List<AssetUpdate> assetUpdates
        {
            get
            {
                lock (padlock)
                {
                    if (_assetUpdates == null)
                    {
                        var fields1 = new Dictionary<string, string>();
                        fields1.Add("Name", "PUT/EDITED-GUID_NAME_" + Guid.NewGuid().ToString());
                        var first = new AssetUpdate() { Fields = fields1 };
                        var fields2 = new Dictionary<string, string>();
                        fields2.Add("Name", "PUT/EDITED-V2GUID_NAME_" + Guid.NewGuid().ToString());
                        var second = new AssetUpdate() { Fields = fields2 };

                        _assetUpdates = new List<AssetUpdate>();
                        _assetUpdates.Add(first);
                        _assetUpdates.Add(second);
                    }
                    return _assetUpdates;
                }
            }
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
