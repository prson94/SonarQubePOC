using d360.core.entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace igx.IntegrationTests.TestData
{
    public sealed class XRefTestData
    {
        private static AssetCrossReference _model = null;
        private static List<AssetCrossReference> _modelsList = null;


        private static readonly object padlock = new object();
        public static AssetCrossReference XRefModel
        {
            get
            {
                lock (padlock)
                {
                    if (_model == null)
                    {
                        _model = new AssetCrossReference()
                        {
                           uid = Guid.NewGuid(),
                           DataSource = "testDataSource",
                           ExternalID = "testExternalId",
                           Type = "testType_"+Guid.NewGuid(),
                           FieldHash = "testFieldHash"
                        };
                    }
                    return _model;
                }
            }
        }
        public static List<AssetCrossReference> XRefModelList
        {
            get
            {
                lock (padlock)
                {
                    if (_modelsList == null)
                    {

                        //Using same data source as final delete in tests will be over this unique datasource
                        var sameDataSourceName = "testSameDatasource" + Guid.NewGuid();
                        _modelsList = new List<AssetCrossReference>();
                        _modelsList.Add(new AssetCrossReference()
                        {
                            uid = Guid.NewGuid(),
                            DataSource = sameDataSourceName,
                            ExternalID = "testExternalId2",
                            Type = "testType_" + Guid.NewGuid(),
                            FieldHash = "testFieldHash2"
                        });
                        _modelsList.Add(new AssetCrossReference()
                        {
                            uid = Guid.NewGuid(),
                            DataSource = sameDataSourceName,
                            ExternalID = "testExternalId2",
                            Type = "testType2_" + Guid.NewGuid(),
                            FieldHash = "testFieldHash2"
                        });
                    }
                    return _modelsList;
                }
            }
        }

    }
}
