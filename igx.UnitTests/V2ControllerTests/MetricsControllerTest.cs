using d360.core.entities.Workflow;
using d360.web.Controllers.V2;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Xunit;
using igx.UnitTests.Core;
using d360.core.entities;
using Newtonsoft.Json.Linq;
using d360.core.entities.Metric;
using d360.web.Models;

namespace igx.UnitTests.V2ControllerTests
{
    [Trait("Unit tests", "Metrics controller")]
    public class MetricsControllerTest : BaseTest
    {
        internal MetricsController metricsController;

        public MetricsControllerTest()
        {
            this.metricsController = new MetricsController(GetCommunity(), GetCompany(), GetQueue(), GetMetricsRepository(), GetAssetRepository())
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

            Assert.True(actionResult.StatusCode == System.Net.HttpStatusCode.OK);
            Assert.True(Helpers.IsTypeOf(typeof(MetricAsset), data));

        }

        [Fact]
        public async void Err_GetAssetByUid_InvalidUid()
        {

            var actionResult = metricsController.GetAssetById(Guid.Parse(DataConstants.InvalidGUID)).ExecuteAsync(new System.Threading.CancellationToken()).Result;

            var str = await actionResult.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<JObject>(str);

            Assert.True(actionResult.StatusCode == System.Net.HttpStatusCode.NotFound);
            Assert.True(Helpers.IsTypeOf(typeof(ErrorResponse), data));

        }

        [Fact]
        public async void UpsertMetrics()
        {
            var model = new MetricAssetViewModel();
            model.Name = "test model";
            model.Weight = 1;
            model.IsGroup = false;
            model.Conditions = new List<MetricAssetVersionConditionViewModel>() { new MetricAssetVersionConditionViewModel() { FieldTypeID = 1 } };

            var actionResult = metricsController.UpsertAsset(model).ExecuteAsync(new System.Threading.CancellationToken()).Result;

            var str = await actionResult.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<JObject>(str);

            Assert.True(actionResult.StatusCode == System.Net.HttpStatusCode.OK);
            Assert.True(Helpers.IsTypeOf(typeof(ConfirmResponse), data));

        }

        [Fact]
        public async void Err_UpsertMetrics_NoName()
        {
            var model = new MetricAssetViewModel();
            model.Name = "";
            model.Conditions = new List<MetricAssetVersionConditionViewModel>() { new MetricAssetVersionConditionViewModel() { FieldTypeID = 1 } };

            var actionResult = metricsController.UpsertAsset(model).ExecuteAsync(new System.Threading.CancellationToken()).Result;

            var str = await actionResult.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<JObject>(str);

            Assert.True(actionResult.StatusCode == System.Net.HttpStatusCode.BadRequest);
            Assert.True(Helpers.IsTypeOf(typeof(ErrorResponse), data));

        }

        [Fact]
        public async void Err_UpsertMetrics_BadWeight()
        {
            var model = new MetricAssetViewModel();
            model.Name = "good name";
            model.Weight = 0;
            model.Conditions = new List<MetricAssetVersionConditionViewModel>() { new MetricAssetVersionConditionViewModel() { FieldTypeID = 1 } };

            var actionResult = metricsController.UpsertAsset(model).ExecuteAsync(new System.Threading.CancellationToken()).Result;

            var str = await actionResult.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<JObject>(str);

            Assert.True(actionResult.StatusCode == System.Net.HttpStatusCode.BadRequest);
            Assert.True(Helpers.IsTypeOf(typeof(ErrorResponse), data));

        }

        [Fact]
        public async void Err_UpsertMetrics_BadGrouping()
        {
            var model = new MetricAssetViewModel();
            model.Name = "good name";
            model.Weight = 1;
            model.IsGroup = true;
            model.Conditions = new List<MetricAssetVersionConditionViewModel>() { new MetricAssetVersionConditionViewModel() { FieldTypeID = 1 } };

            var actionResult = metricsController.UpsertAsset(model).ExecuteAsync(new System.Threading.CancellationToken()).Result;

            var str = await actionResult.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<JObject>(str);

            Assert.True(actionResult.StatusCode == System.Net.HttpStatusCode.BadRequest);
            Assert.True(Helpers.IsTypeOf(typeof(ErrorResponse), data));

        }

        [Fact]
        public async void Err_UpsertMetrics_BadCondition()
        {
            var model = new MetricAssetViewModel();
            model.Name = "good name";
            model.Weight = 1;
            model.IsGroup = false;
            model.Conditions = new List<MetricAssetVersionConditionViewModel>() { new MetricAssetVersionConditionViewModel() { FieldTypeID = 0 } };

            var actionResult = metricsController.UpsertAsset(model).ExecuteAsync(new System.Threading.CancellationToken()).Result;

            var str = await actionResult.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<JObject>(str);

            Assert.True(actionResult.StatusCode == System.Net.HttpStatusCode.BadRequest);
            Assert.True(Helpers.IsTypeOf(typeof(ErrorResponse), data));

        }

        [Fact]
        public async void DeleteMetric()
        {
            var actionResult = metricsController.DeleteById(Guid.Parse(DataConstants.ValidGUID)).ExecuteAsync(new System.Threading.CancellationToken()).Result;

            var str = await actionResult.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<JObject>(str);

            Assert.True(actionResult.StatusCode == System.Net.HttpStatusCode.OK);
            Assert.True(Helpers.IsTypeOf(typeof(ConfirmResponse), data));

        }

        [Fact]
        public async void Err_DeleteMetric_BadUid()
        {
            var actionResult = metricsController.DeleteById(Guid.Parse(DataConstants.InvalidGUID)).ExecuteAsync(new System.Threading.CancellationToken()).Result;

            var str = await actionResult.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<JObject>(str);

            Assert.True(actionResult.StatusCode == System.Net.HttpStatusCode.NotFound);
            Assert.True(Helpers.IsTypeOf(typeof(ErrorResponse), data));

        }

        [Fact]
        public async void GetMetricHierarchyByAssetTypeUid()
        {
            var actionResult = metricsController.GetMetricHierarchyByAssetTypeAsync(Guid.Parse(DataConstants.ValidGUID)).Result.ExecuteAsync(new System.Threading.CancellationToken()).Result;

            var str = await actionResult.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<JArray>(str);

            Assert.True(actionResult.StatusCode == System.Net.HttpStatusCode.OK);
            Assert.True(Helpers.IsTypeOf(typeof(MetricAssetTypeHierarchyModel), data));

        }

        [Fact]
        public async void Err_GetMetricHierarchyByAssetTypeUid_InvalidUid()
        {
            var actionResult = metricsController.GetMetricHierarchyByAssetTypeAsync(Guid.Parse(DataConstants.InvalidGUID)).Result.ExecuteAsync(new System.Threading.CancellationToken()).Result;

            var str = await actionResult.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<JObject>(str);

            Assert.True(actionResult.StatusCode == System.Net.HttpStatusCode.NotFound);
            Assert.True(Helpers.IsTypeOf(typeof(ErrorResponse), data));

        }

        [Fact]
        public async void GetMetricHierarchyByAssetUidAsync()
        {
            var actionResult = metricsController.GetMetricHierarchyByAssetAsync(Guid.Parse(DataConstants.ValidGUID)).Result.ExecuteAsync(new System.Threading.CancellationToken()).Result;

            var str = await actionResult.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<JArray>(str);

            Assert.True(actionResult.StatusCode == System.Net.HttpStatusCode.OK);
            Assert.True(Helpers.IsTypeOf(typeof(MetricAssetHierarchyModel), data));

        }

        [Fact]
        public async void Err_GetMetricHierarchyByAssetUidAsync_BadUid()
        {
            var actionResult = metricsController.GetMetricHierarchyByAssetAsync(Guid.Parse(DataConstants.InvalidGUID)).Result.ExecuteAsync(new System.Threading.CancellationToken()).Result;

            var str = await actionResult.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<JObject>(str);

            Assert.True(actionResult.StatusCode == System.Net.HttpStatusCode.NotFound);
            Assert.True(Helpers.IsTypeOf(typeof(ErrorResponse), data));

        }

        [Fact]
        public async void GetMetricStructureByAssetType()
        {
            var actionResult = metricsController.GetMetricStructureByAssetType(Guid.Parse(DataConstants.ValidGUID)).ExecuteAsync(new System.Threading.CancellationToken()).Result;

            var str = await actionResult.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<JArray>(str);

            Assert.True(actionResult.StatusCode == System.Net.HttpStatusCode.OK);
            Assert.True(Helpers.IsTypeOf(typeof(MetricAssetViewModel), data));

        }

        [Fact]
        public async void GetMetricFieldsByAssetType()
        {
            var actionResult = metricsController.GetMetricFieldsByAssetType(Guid.Parse(DataConstants.ValidGUID)).ExecuteAsync(new System.Threading.CancellationToken()).Result;

            var str = await actionResult.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<JArray>(str);

            Assert.True(actionResult.StatusCode == System.Net.HttpStatusCode.OK);
            Assert.True(Helpers.IsTypeOf(typeof(MetricFieldTypeViewModel), data));

        }

        [Fact]
        public async void PostBulkMetricsToStagingAsync()
        {
            var actionResult = metricsController.PostBulkMetricsToStagingAsync(new BulkMetricsImport()).ExecuteAsync(new System.Threading.CancellationToken()).Result;

            var str = await actionResult.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<JArray>(str);

            Assert.True(actionResult.StatusCode == System.Net.HttpStatusCode.OK);
            Assert.True(Helpers.IsTypeOf(typeof(BulkMetricTemporaryTableModel), data));

        }

    }
}
