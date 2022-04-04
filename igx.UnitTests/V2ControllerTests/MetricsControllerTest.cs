using d360.core.entities;
using d360.core.entities.Metric;
using d360.web.Controllers.V2;
using d360.web.Models;
using igx.UnitTests.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using Xunit;

namespace igx.UnitTests.V2ControllerTests
{
    [Trait("Unit tests", "Metrics controller")]
    public class MetricsControllerTest : BaseTest
    {
        internal MetricsController metricsController;

        public MetricsControllerTest()
        {
            this.metricsController = new MetricsController(GetCoreComponentSet(), GetScoringRepository(), GetMetricsRepository(), GetAssetRepository())
            {
                Request = new HttpRequestMessage(),
                Configuration = new HttpConfiguration()
            };
        }

        [Fact]
        public async void GetAssetByUid()
        {
            var actionResult = metricsController.GetAssetById(Guid.Parse(DataConstants.ValidGUID)).ExecuteAsync(new System.Threading.CancellationToken()).Result;
            
            var str = await actionResult.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<JObject>(str);

            Assert.True(actionResult.StatusCode == System.Net.HttpStatusCode.OK, XMsg.BadResponseCode);
            Assert.True(Helpers.IsTypeOf(typeof(MetricAssetViewDetailModel), data), XMsg.InvalidJSON);
        }

        [Fact]
        public async void Err_GetAssetByUid_InvalidUid()
        {
            var actionResult = metricsController.GetAssetById(Guid.Parse(DataConstants.InvalidGUID)).ExecuteAsync(new System.Threading.CancellationToken()).Result;

            var str = await actionResult.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<JObject>(str);

            Assert.True(actionResult.StatusCode == System.Net.HttpStatusCode.NotFound, XMsg.BadResponseCode);
            Assert.True(Helpers.IsTypeOf(typeof(ErrorResponse), data), XMsg.InvalidJSON);
        }

        [Fact]
        public async void UpsertMetrics()
        {
            var model = new MetricAssetEditModel();
            model.Name = "test model";
            model.Weight = 1;
            model.IsGroup = false;
            model.AllocationUid = Guid.NewGuid();
            model.ConditionGroups = new List<MetricAssetVersionConditionViewModel>() { 
                new MetricAssetVersionConditionViewModel() { 
                    ConditionItems = new List<MetricAssetVersionConditionItemViewModel>() { 
                        new MetricAssetVersionConditionItemViewModel { 
                            ConditionFieldTypeName = "Name" 
                        } 
                    } 
                }
            };
            model.Definition = new MetricAssetDefinitionViewModel
            {
                Governance = new MetricAssetDefinitionGovernanceViewModel
                {
                    Check = d360.core.enums.MetricGovernanceCheckType.External,
                    External = new MetricAssetDefinitionGovernanceExternalViewModel
                    {
                        UpdateFrequency = d360.core.enums.MetricUpdateFrequency.None
                    }
                }
            };

            var actionResult = metricsController.UpsertAsset(model).ExecuteAsync(new System.Threading.CancellationToken()).Result;

            var str = await actionResult.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<JObject>(str);

            Assert.True(actionResult.StatusCode == System.Net.HttpStatusCode.BadRequest, XMsg.BadResponseCode);
            Assert.True(Helpers.IsTypeOf(typeof(ErrorResponse), data), XMsg.InvalidJSON);

        }

        [Fact]
        public async void Err_UpsertMetrics_NoName()
        {
            var model = new MetricAssetEditModel();
            model.Name = "";
            model.AllocationUid = Guid.NewGuid();
            model.ConditionGroups = new List<MetricAssetVersionConditionViewModel>() { 
                new MetricAssetVersionConditionViewModel() { 
                    ConditionItems = new List<MetricAssetVersionConditionItemViewModel>() {
                        new MetricAssetVersionConditionItemViewModel {
                            ConditionFieldTypeName = "Name"
                        }
                    }  
                } 
            };
            model.Definition = new MetricAssetDefinitionViewModel
            {
                Governance = new MetricAssetDefinitionGovernanceViewModel
                {
                    Check = d360.core.enums.MetricGovernanceCheckType.External,
                    External = new MetricAssetDefinitionGovernanceExternalViewModel
                    {
                        UpdateFrequency = d360.core.enums.MetricUpdateFrequency.None
                    }
                }
            };

            var actionResult = metricsController.UpsertAsset(model).ExecuteAsync(new System.Threading.CancellationToken()).Result;

            var str = await actionResult.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<JObject>(str);

            Assert.True(actionResult.StatusCode == System.Net.HttpStatusCode.BadRequest, XMsg.BadResponseCode);
            Assert.True(Helpers.IsTypeOf(typeof(ErrorResponse), data), XMsg.InvalidJSON);

        }

        [Fact]
        public async void Err_UpsertMetrics_BadWeight()
        {
            var model = new MetricAssetEditModel();
            model.Name = "good name";
            model.Weight = 0;
            model.AllocationUid = Guid.NewGuid();
            model.ConditionGroups = new List<MetricAssetVersionConditionViewModel>() { 
                new MetricAssetVersionConditionViewModel() {
                    ConditionItems = new List<MetricAssetVersionConditionItemViewModel>() {
                        new MetricAssetVersionConditionItemViewModel {
                            ConditionFieldTypeName = "Name"
                        }
                    }
                } 
            };
            model.Definition = new MetricAssetDefinitionViewModel
            {
                Governance = new MetricAssetDefinitionGovernanceViewModel
                {
                    Check = d360.core.enums.MetricGovernanceCheckType.External,
                    External = new MetricAssetDefinitionGovernanceExternalViewModel
                    {
                        UpdateFrequency = d360.core.enums.MetricUpdateFrequency.None
                    }
                }
            };

            var actionResult = metricsController.UpsertAsset(model).ExecuteAsync(new System.Threading.CancellationToken()).Result;

            var str = await actionResult.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<JObject>(str);

            Assert.True(actionResult.StatusCode == System.Net.HttpStatusCode.BadRequest, XMsg.BadResponseCode);
            Assert.True(Helpers.IsTypeOf(typeof(ErrorResponse), data), XMsg.InvalidJSON);

        }

        [Fact]
        public async void Err_UpsertMetrics_BadGrouping()
        {
            var model = new MetricAssetEditModel();
            model.Name = "good name";
            model.Weight = 1;
            model.IsGroup = true;
            model.AllocationUid = Guid.NewGuid();
            model.ConditionGroups = new List<MetricAssetVersionConditionViewModel>() { 
                new MetricAssetVersionConditionViewModel() {
                    ConditionItems = new List<MetricAssetVersionConditionItemViewModel>() {
                        new MetricAssetVersionConditionItemViewModel {
                            ConditionFieldTypeName = "Name"
                        }
                    }
                } 
            };

            var actionResult = metricsController.UpsertAsset(model).ExecuteAsync(new System.Threading.CancellationToken()).Result;

            var str = await actionResult.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<JObject>(str);

            Assert.True(actionResult.StatusCode == System.Net.HttpStatusCode.BadRequest, XMsg.BadResponseCode);
            Assert.True(Helpers.IsTypeOf(typeof(ErrorResponse), data), XMsg.InvalidJSON);

        }

        [Fact]
        public async void Err_UpsertMetrics_BadCondition()
        {
            var model = new MetricAssetEditModel();
            model.Name = "good name";
            model.Weight = 1;
            model.IsGroup = false;
            model.AllocationUid = Guid.NewGuid();
            model.ConditionGroups = new List<MetricAssetVersionConditionViewModel>() { 
                new MetricAssetVersionConditionViewModel() {
                    ConditionItems = new List<MetricAssetVersionConditionItemViewModel>() {
                        new MetricAssetVersionConditionItemViewModel {
                        }
                    }
                } 
            };
            model.Definition = new MetricAssetDefinitionViewModel
            {
                Governance = new MetricAssetDefinitionGovernanceViewModel
                {
                    Check = d360.core.enums.MetricGovernanceCheckType.External,
                    External = new MetricAssetDefinitionGovernanceExternalViewModel
                    {
                        UpdateFrequency = d360.core.enums.MetricUpdateFrequency.None
                    }
                }
            };

            var actionResult = metricsController.UpsertAsset(model).ExecuteAsync(new System.Threading.CancellationToken()).Result;

            var str = await actionResult.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<JObject>(str);

            Assert.True(actionResult.StatusCode == System.Net.HttpStatusCode.BadRequest, XMsg.BadResponseCode);
            Assert.True(Helpers.IsTypeOf(typeof(ErrorResponse), data), XMsg.InvalidJSON);

        }

        [Fact]
        public async void DeleteMetric()
        {
            var actionResult = metricsController.DeleteById(Guid.Parse(DataConstants.ValidGUID)).ExecuteAsync(new System.Threading.CancellationToken()).Result;

            var str = await actionResult.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<JObject>(str);

            Assert.True(actionResult.StatusCode == System.Net.HttpStatusCode.OK, XMsg.BadResponseCode);
            Assert.True(Helpers.IsTypeOf(typeof(ConfirmResponse), data), XMsg.InvalidJSON);

        }

        [Fact]
        public async void Err_DeleteMetric_BadUid()
        {
            var actionResult = metricsController.DeleteById(Guid.Parse(DataConstants.InvalidGUID)).ExecuteAsync(new System.Threading.CancellationToken()).Result;

            var str = await actionResult.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<JObject>(str);

            Assert.True(actionResult.StatusCode == System.Net.HttpStatusCode.NotFound, XMsg.BadResponseCode);
            Assert.True(Helpers.IsTypeOf(typeof(ErrorResponse), data), XMsg.InvalidJSON);

        }

        [Fact]
        public async void GetMetricHierarchyByAssetTypeUid()
        {
            var actionResult = metricsController.GetMetricHierarchyByAssetTypeAsync(Guid.Parse(DataConstants.ValidGUID)).Result.ExecuteAsync(new System.Threading.CancellationToken()).Result;

            var str = await actionResult.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<JArray>(str);

            Assert.True(actionResult.StatusCode == System.Net.HttpStatusCode.OK, XMsg.BadResponseCode);
            Assert.True(Helpers.IsTypeOf(typeof(MetricAssetTypeHierarchyModel), data), XMsg.InvalidJSON);

        }

        [Fact]
        public async void Err_GetMetricHierarchyByAssetTypeUid_InvalidUid()
        {
            var actionResult = metricsController.GetMetricHierarchyByAssetTypeAsync(Guid.Parse(DataConstants.InvalidGUID)).Result.ExecuteAsync(new System.Threading.CancellationToken()).Result;

            var str = await actionResult.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<JObject>(str);

            Assert.True(actionResult.StatusCode == System.Net.HttpStatusCode.NotFound, XMsg.BadResponseCode);
            Assert.True(Helpers.IsTypeOf(typeof(ErrorResponse), data), XMsg.InvalidJSON);

        }

        [Fact]
        public async void GetMetricHierarchyByAssetUidAllocationAsync_BadUid()
        {
            var actionResult = metricsController.GetMetricHierarchyByAssetAndAllocationAsync(DataConstants.ValidGUID2, DataConstants.ValidGUID).Result.ExecuteAsync(new System.Threading.CancellationToken()).Result;

            var str = await actionResult.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<JObject>(str);

            Assert.True(actionResult.StatusCode == System.Net.HttpStatusCode.NotFound, XMsg.BadResponseCode);
            Assert.True(Helpers.IsTypeOf(typeof(ErrorResponse), data), XMsg.InvalidJSON);

        }

        [Fact]
        public async void Err_GetMetricHierarchyByAssetUidAsync_BadUid()
        {
            var actionResult = metricsController.GetMetricHierarchyByAssetAndScoreTypeAsync(d360.core.enums.ScoreType.Governance, Guid.Parse(DataConstants.InvalidGUID)).Result.ExecuteAsync(new System.Threading.CancellationToken()).Result;

            var str = await actionResult.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<JObject>(str);

            Assert.True(actionResult.StatusCode == System.Net.HttpStatusCode.NotFound, XMsg.BadResponseCode);
            Assert.True(Helpers.IsTypeOf(typeof(ErrorResponse), data), XMsg.InvalidJSON);

        }

        [Fact]
        public async void GetMetricFieldsByAssetType()
        {
            var actionResult = metricsController.GetMetricFieldsByAssetType(Guid.Parse(DataConstants.ValidGUID));
            var result = actionResult.ExecuteAsync(new System.Threading.CancellationToken()).Result;
            var str = await result.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<JArray>(str);

            Assert.True(result.StatusCode == System.Net.HttpStatusCode.OK, XMsg.BadResponseCode);
            Assert.True(Helpers.IsTypeOf(typeof(MetricFieldTypeViewModel), data), XMsg.InvalidJSON);

        }

    }
}
