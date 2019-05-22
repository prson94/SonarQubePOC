using d360.core.entities;
using d360.core.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace igx.IntegrationTests.TestData
{
    public sealed class AssetTypeTestData
    {
        private static AssetTypeInsert _assetTypeInsert = null;
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
                            Name = "AssetTypeIntegrationTest-"+ Guid.NewGuid().ToString(),
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
    }
}
