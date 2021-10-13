using d360.core.queue;
using d360.extensions;
using d360.extensions.search;
using Elasticsearch.Net;
using Moq;
using Moq.Protected;
using Nest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace igx.UnitTests.SearchTests
{
    [Trait("Unit tests", "Elastic Search Source tests")]
    public class ElasticSourceTests : BaseTest
    {

        public ElasticSourceTests()
        {
        }

        private readonly int CompanyID = 1;

        private IElasticClient CreateESClientWithResponse(object response, int statusCode = 200)
        {
            var connection = new InMemoryConnection(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response)), statusCode);
            var settings = new ConnectionSettings(new SingleNodeConnectionPool(new System.Uri("http://localhost:9200")), connection).DefaultIndex("d3s");
            return new ElasticClient(settings);
        }

        private IndexObjectModel GetIndexObjectModel(int id)
        {
            return new IndexObjectModel
            {
                ID = id,
                Category = "TestCategory",
                CompanyID = CompanyID,
                AssetType = "TestAsset",
                Uid = new Guid(),
                Fields = new Dictionary<string, string>() {
                    { "Name", "Test name"}
                }
            };
        }


        [Fact]
        public void ElasticVersion()
        {
            string version = "6.5.4";
            Version ver = new Version(version);
            var response = new
            {
                version = new
                {
                    number = version
                }
            };

            var source = new Mock<ElasticSearchSource>(MockBehavior.Strict);
            source.Protected()
                .Setup<IElasticClient>("GetElasticClient", ItExpr.IsAny<int>())
                .Returns(CreateESClientWithResponse(response))
                .Verifiable();

            Version result = source.Object.GetElasticVersion(CompanyID);

            Assert.Equal(ver.ToString(), result.ToString());
        }

        [Fact]
        public void ElasticRecordCount()
        {
            int recordCount = 123;
            var response = new {
                count = recordCount,
                _shards =  new {
                    total = 2,
                    successful = 2,
                    skipped = 0,
                    failed = 0
                } 
            };

            var source = new Mock<ElasticSearchSource>(MockBehavior.Strict);
            source.Protected()
                .Setup<IElasticClient>("GetElasticClient", ItExpr.IsAny<int>())
                .Returns(CreateESClientWithResponse(response))
                .Verifiable();

            int result = source.Object.GetTotalRecordCount(CompanyID);

            Assert.Equal(recordCount, result);
        }

        [Fact]
        public void ElasticGetStatusList()
        {
            int recordCount = 733;
            var response = GetStatusListResponse(recordCount);

            var source = new Mock<ElasticSearchSource>(MockBehavior.Strict);
            source.Protected()
                .Setup<IElasticClient>("GetElasticClient", ItExpr.IsAny<int>())
                .Returns(CreateESClientWithResponse(response))
                .Verifiable();

            List<IndexableCount> result = source.Object.GetStatusList(CompanyID);

            //Counts for categories, and asset types, so double
            Assert.Equal(recordCount * 2, result.Sum(r => r.CurrentCount));
        }

        [Fact]
        public void ElasticGetStatusSearch()
        {
            //IndexResults GetStatusSearch(int companyID, List<IndexTypeList> categories, bool withTypes = false)

            int recordCount = 733;
            var response = GetStatusListResponse(recordCount);

            var source = new Mock<ElasticSearchSource>(MockBehavior.Strict);
            source.Protected()
                .Setup<IElasticClient>("GetElasticClient", ItExpr.IsAny<int>())
                .Returns(CreateESClientWithResponse(response))
                .Verifiable();

            IndexResults result = source.Object.GetStatusSearch(CompanyID, new List<IndexTypeList>(), false);

            Assert.Equal(recordCount, result.Matches);
        }

        [Theory]
        [InlineData()]
        [InlineData("BusinessAssets")]
        [InlineData("BusinessAssets", "TestAssetType")]
        [InlineData(null, null, "294757A6-0994-4716-BDD8-00FF5807880B")]
        public void ElasticClearIndex(string category = null, string assetType = null, string assetTypeGuid = null)
        {
            var source = new Mock<ElasticSearchSource>(MockBehavior.Strict);
            source.Protected()
                .Setup<IElasticClient>("GetElasticClient", ItExpr.IsAny<int>())
                .Returns(CreateESClientWithResponse(new { }))
                .Verifiable();

            try
            {
                if (Guid.TryParse(assetTypeGuid, out Guid atGuid))
                {
                    source.Object.ClearIndex(CompanyID, atGuid);
                }
                else if (!string.IsNullOrEmpty(category))
                {
                    source.Object.ClearIndex(CompanyID, category, assetType);
                }
                else
                {
                    source.Object.ClearIndex(CompanyID);
                }
                return;
            }
            catch (ArgumentException ex)
            {
                Assert.Equal("", ex.Message);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(50)]
        [InlineData(10000)]
        public void ElasticAddToIndex(int noOfModels)
        {
            var response = new {
                took = 30,
                errors = false,
                items = new[] {
                    new { }
                }
            };

            List<IndexObjectModel> models = Enumerable.Range(1, noOfModels).Select(i => GetIndexObjectModel(i)).ToList();

            var source = new Mock<ElasticSearchSource>(MockBehavior.Strict);
            source.Protected()
                .Setup<IElasticClient>("GetElasticClient", ItExpr.IsAny<int>())
                .Returns(CreateESClientWithResponse(response))
                .Verifiable();

            try
            {
                if(noOfModels == 1)
                {
                    source.Object.AddToIndex(models.First());
                } else
                {
                    source.Object.AddToIndex(models);
                }
                return;
            } catch (ArgumentException ex)
            {
                Assert.Equal("", ex.Message);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(50)]
        [InlineData(10000)]
        public void ElasticUpdateInIndex(int noOfModels)
        {
            var response = new
            {
                took = 30,
                errors = false,
                items = new[] {
                    new { }
                }
            };

            List<IndexObjectModel> models = Enumerable.Range(1, noOfModels).Select(i => GetIndexObjectModel(i)).ToList();

            var source = new Mock<ElasticSearchSource>(MockBehavior.Strict);
            source.Protected()
                .Setup<IElasticClient>("GetElasticClient", ItExpr.IsAny<int>())
                .Returns(CreateESClientWithResponse(response))
                .Verifiable();

            try
            {
                if (noOfModels == 1)
                {
                    source.Object.UpdateInIndex(models.First());
                }
                else
                {
                    source.Object.UpdateInIndex(models);
                }
                return;
            }
            catch (ArgumentException ex)
            {
                Assert.Equal("", ex.Message);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(50)]
        [InlineData(10000)]
        public void ElasticRemoveFromIndex(int noOfModels)
        {
            var response = new
            {
                took = 30,
                errors = false,
                items = new[] {
                    new { }
                }
            };

            List<IndexObjectModel> models = Enumerable.Range(1, noOfModels).Select(i => GetIndexObjectModel(i)).ToList();

            var source = new Mock<ElasticSearchSource>(MockBehavior.Strict);
            source.Protected()
                .Setup<IElasticClient>("GetElasticClient", ItExpr.IsAny<int>())
                .Returns(CreateESClientWithResponse(response))
                .Verifiable();

            try
            {
                if (noOfModels == 1)
                {
                    source.Object.RemoveFromIndex(models.First());
                }
                else
                {
                    source.Object.RemoveFromIndex(models);
                }
                return;
            }
            catch (ArgumentException ex)
            {
                Assert.Equal("", ex.Message);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(50)]
        [InlineData(10000)]
        public void ElasticReIndex(int noOfModels)
        {
            var response = new
            {
                took = 30,
                errors = false,
                items = new[] {
                    new { }
                }
            };

            List<IndexObjectModel> models = Enumerable.Range(1, noOfModels).Select(i => GetIndexObjectModel(i)).ToList();

            var source = new Mock<ElasticSearchSource>(MockBehavior.Strict);
            source.Protected()
                .Setup<IElasticClient>("GetElasticClient", ItExpr.IsAny<int>())
                .Returns(CreateESClientWithResponse(response))
                .Verifiable();

            try
            {
                source.Object.ReIndex(CompanyID, models);
                return;
            }
            catch (ArgumentException ex)
            {
                Assert.Equal("", ex.Message);
            }
        }

        [Fact]
        public void ElasticGetSearchResultsWithAggregation()
        {
            QueryRequest queryRequest = new QueryRequest() {
                From = 0,
                Size = 25,
                Term = "more"
            };
            List<IndexTypeList> categories = new List<IndexTypeList>();
            QueryLimitation limits = new QueryLimitation
            {
                ResourceID = 1,
                ResourceGroupIDs = new List<int> { 1, 2, 3},
                ResourceOrgIDs = new List<int>()
            };

            var source = new Mock<ElasticSearchSource>(MockBehavior.Strict);
            source.Protected()
                .Setup<IElasticClient>("GetElasticClient", ItExpr.IsAny<int>())
                .Returns(CreateESClientWithResponse(GetSearchResponse()))
                .Verifiable();

            IndexResults result = source.Object.GetSearchResultsWithAggregation(CompanyID, 1, queryRequest, categories, limits);

            Assert.Equal(2, result.Results.Count);
        }

        [Fact]
        public void SearchResultsModels()
        {
            SearchResultsModel model = GetSearchResponse();

            Assert.True(model != null, "SearchResultsModel is null and should not be.");

            Assert.True(model.hits.total == 2, "Total hits in results is not 2 and should be");
            
            Assert.Equal(typeof(SearchResultsHitsModel), model.hits.GetType());
            Assert.Equal(typeof(SearchResultsShardModel), model._shards.GetType());
            Assert.Equal(typeof(List<SearchResultsHitModel>), model.hits.hits.GetType());
        }


        #region Elastic responses
        private object GetStatusListResponse(int totalcount)
        {
            return new
            {
                took = 1,
                timed_out = false,
                _shards = new
                {
                    total = 2,
                    successful = 2,
                    skipped = 0,
                    failed = 0
                },
                hits = new
                {
                    total = totalcount,
                    max_score = 0.0,
                    hits = new string[0]
                },
                aggregations = new
                {
                    all_types = new
                    {
                        doc_count_error_upper_bound = 0,
                        sum_other_doc_count = 0,
                        buckets = new[] {
                            new {
                                key = "BusinessAsset",
                                doc_count = 702,
                                category = new {
                                    doc_count_error_upper_bound = 0,
                                    sum_other_doc_count = 0,
                                    buckets = new[] {
                                        new {
                                            key = "a9805928-0bdc-4d5f-979c-92db0b52f81b",
                                            doc_count = 686

                                        },
                                        new {
                                            key = "f3a4560b-1f6b-4247-8b1c-fcd7ec80817e",
                                            doc_count = 16

                                        }
                                    }
                                }
                            },
                            new {
                                key = "Model",
                                doc_count = 31,
                                category = new {
                                    doc_count_error_upper_bound = 0,
                                    sum_other_doc_count = 0,
                                    buckets =  new[] {
                                        new {
                                            key = "022bc969-8806-4ffc-937a-8bb84256796f",
                                            doc_count = 20

                                        },
                                        new {
                                            key = "53a693e6-7bdc-4e1b-9681-9ec08036fd60",
                                            doc_count = 11

                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

		private SearchResultsModel GetSearchResponse()
		{
            string jsonresponse = "{\"took\":41,\"timed_out\":false,\"_shards\":{\"total\":2,\"successful\":2,\"skipped\":0,\"failed\":0},\"hits\":{\"total\":2,\"max_score\":16.608347,\"hits\":[{\"_shard\":\"[d3s7][1]\",\"_node\":\"8WUUk5HDSsqNBhS46TEDaQ\",\"_index\":\"d3s7\",\"_type\":\"_doc\",\"_id\":\"Artifact|373492\",\"_score\":16.608347,\"_source\":{\"d3s\": {\"Url\": \"artifact/3/688\",\"AssetType\": \"TwoSide\",\"Category\": \"BusinessAsset\",\"Uid\": \"f539bb36-13ac-424e-88eb-6067c8a0537f\",\"AssetTypeUid\": \"6ac4869e-ac59-4e5c-a7cb-5cc37f0ed441\",\"NoReadResourceID\": [],\"NoReadGroupID\": [],\"NoReadOrgID\": []  },  \"fields\": {\"Name\": \"Select more items\",\"ListsAbound\": \"Root/Level 1.1/Level 2,Root,Root/Level W\"  }},\"highlight\":{\"fields.Name\":[\"Select <em class='search-highlight'>more</em> items\"]},\"_explanation\":{\"value\":16.608347,\"description\":\"sum of:\",\"details\":[{\"value\":16.608347,\"description\":\"sum of:\",\"details\":[{\"value\":16.608347,\"description\":\"sum of:\",\"details\":[{\"value\":9.490484,\"description\":\"weight(fields.Name:more in 355) [PerFieldSimilarity], result of:\",\"details\":[{\"value\":9.490484,\"description\":\"score(doc=355,freq=1.0 = termFreq=1.0\\n), product of:\",\"details\":[{\"value\":2.0,\"description\":\"boost\",\"details\":[]},{\"value\":5.3375382,\"description\":\"idf, computed as log(1 + (docCount - docFreq + 0.5) / (docFreq + 0.5)) from:\",\"details\":[{\"value\":2.0,\"description\":\"docFreq\",\"details\":[]},{\"value\":519.0,\"description\":\"docCount\",\"details\":[]}]},{\"value\":0.88903195,\"description\":\"tfNorm, computed as (freq * (k1 + 1)) / (freq + k1 * (1 - b + b * fieldLength / avgFieldLength)) from:\",\"details\":[{\"value\":1.0,\"description\":\"termFreq=1.0\",\"details\":[]},{\"value\":1.2,\"description\":\"parameter k1\",\"details\":[]},{\"value\":0.75,\"description\":\"parameter b\",\"details\":[]},{\"value\":2.2986512,\"description\":\"avgFieldLength\",\"details\":[]},{\"value\":3.0,\"description\":\"fieldLength\",\"details\":[]}]}]}]},{\"value\":7.117863,\"description\":\"max of:\",\"details\":[{\"value\":7.117863,\"description\":\"weight(fields.Name:more in 355) [PerFieldSimilarity], result of:\",\"details\":[{\"value\":7.117863,\"description\":\"score(doc=355,freq=1.0 = termFreq=1.0\\n), product of:\",\"details\":[{\"value\":1.5,\"description\":\"boost\",\"details\":[]},{\"value\":5.3375382,\"description\":\"idf, computed as log(1 + (docCount - docFreq + 0.5) / (docFreq + 0.5)) from:\",\"details\":[{\"value\":2.0,\"description\":\"docFreq\",\"details\":[]},{\"value\":519.0,\"description\":\"docCount\",\"details\":[]}]},{\"value\":0.88903195,\"description\":\"tfNorm, computed as (freq * (k1 + 1)) / (freq + k1 * (1 - b + b * fieldLength / avgFieldLength)) from:\",\"details\":[{\"value\":1.0,\"description\":\"termFreq=1.0\",\"details\":[]},{\"value\":1.2,\"description\":\"parameter k1\",\"details\":[]},{\"value\":0.75,\"description\":\"parameter b\",\"details\":[]},{\"value\":2.2986512,\"description\":\"avgFieldLength\",\"details\":[]},{\"value\":3.0,\"description\":\"fieldLength\",\"details\":[]}]}]}]}]}]},{\"value\":0.0,\"description\":\"match on required clause, product of:\",\"details\":[{\"value\":0.0,\"description\":\"# clause\",\"details\":[]},{\"value\":1.0,\"description\":\"#ConstantScore(d3s.AssetType:AllTypes d3s.AssetType:TwoSide) #ConstantScore(d3s.Category:BusinessAsset d3s.Category:TechnicalAsset) -d3s.NoReadResourceID:8 -ConstantScore(d3s.NoReadGroupID:1 d3s.NoReadGroupID:10 d3s.NoReadGroupID:2 d3s.NoReadGroupID:7 d3s.NoReadGroupID:8)\",\"details\":[]}]}]},{\"value\":0.0,\"description\":\"match on required clause, product of:\",\"details\":[{\"value\":0.0,\"description\":\"# clause\",\"details\":[]},{\"value\":1.0,\"description\":\"DocValuesFieldExistsQuery [field=_primary_term]\",\"details\":[]}]}]},\"inner_hits\":{\"d3s.Tags\":{\"hits\":{\"total\":0,\"max_score\":null,\"hits\":[]}}}},{\"_shard\":\"[d3s7][0]\",\"_node\":\"8WUUk5HDSsqNBhS46TEDaQ\",\"_index\":\"d3s7\",\"_type\":\"_doc\",\"_id\":\"Artifact|373700\",\"_score\":2.814559,\"_source\":{\"d3s\": {\"Url\": \"artifact/100000030/100000123\",\"AssetType\": \"AllTypes\",\"Category\": \"BusinessAsset\",\"Uid\": \"7ac1dda1-3ef6-4610-9b79-78ca097ab268\",\"AssetTypeUid\": \"9ab02d75-c654-409d-bfb1-0d43b8901a08\",\"NoReadResourceID\": [],\"NoReadGroupID\": [],\"NoReadOrgID\": []  },  \"fields\": {\"Name\": \"All Type One Responsible\",\"Bools\": \"false\",\"Description\": \"More of the all type\",\"Lst\": \"Case Matters\",\"Status\": \"Generic Status\"  }},\"_explanation\":{\"value\":2.814559,\"description\":\"sum of:\",\"details\":[{\"value\":2.814559,\"description\":\"sum of:\",\"details\":[{\"value\":2.814559,\"description\":\"sum of:\",\"details\":[{\"value\":2.814559,\"description\":\"max of:\",\"details\":[{\"value\":2.814559,\"description\":\"weight(fields.Description:more in 1) [PerFieldSimilarity], result of:\",\"details\":[{\"value\":2.814559,\"description\":\"score(doc=1,freq=1.0 = termFreq=1.0\\n), product of:\",\"details\":[{\"value\":1.5,\"description\":\"boost\",\"details\":[]},{\"value\":1.1631508,\"description\":\"idf, computed as log(1 + (docCount - docFreq + 0.5) / (docFreq + 0.5)) from:\",\"details\":[{\"value\":2.0,\"description\":\"docFreq\",\"details\":[]},{\"value\":7.0,\"description\":\"docCount\",\"details\":[]}]},{\"value\":1.6131809,\"description\":\"tfNorm, computed as (freq * (k1 + 1)) / (freq + k1 * (1 - b + b * fieldLength / avgFieldLength)) from:\",\"details\":[{\"value\":1.0,\"description\":\"termFreq=1.0\",\"details\":[]},{\"value\":1.2,\"description\":\"parameter k1\",\"details\":[]},{\"value\":0.75,\"description\":\"parameter b\",\"details\":[]},{\"value\":70.57143,\"description\":\"avgFieldLength\",\"details\":[]},{\"value\":5.0,\"description\":\"fieldLength\",\"details\":[]}]}]}]}]}]},{\"value\":0.0,\"description\":\"match on required clause, product of:\",\"details\":[{\"value\":0.0,\"description\":\"# clause\",\"details\":[]},{\"value\":1.0,\"description\":\"#ConstantScore(d3s.AssetType:AllTypes d3s.AssetType:TwoSide) #ConstantScore(d3s.Category:BusinessAsset d3s.Category:TechnicalAsset) -d3s.NoReadResourceID:8 -ConstantScore(d3s.NoReadGroupID:1 d3s.NoReadGroupID:10 d3s.NoReadGroupID:2 d3s.NoReadGroupID:7 d3s.NoReadGroupID:8)\",\"details\":[]}]}]},{\"value\":0.0,\"description\":\"match on required clause, product of:\",\"details\":[{\"value\":0.0,\"description\":\"# clause\",\"details\":[]},{\"value\":1.0,\"description\":\"DocValuesFieldExistsQuery [field=_primary_term]\",\"details\":[]}]}]},\"inner_hits\":{\"d3s.Tags\":{\"hits\":{\"total\":0,\"max_score\":null,\"hits\":[]}}}}]}}";
            return JsonConvert.DeserializeObject<SearchResultsModel>(jsonresponse);
	    }

        #endregion
    }
}




