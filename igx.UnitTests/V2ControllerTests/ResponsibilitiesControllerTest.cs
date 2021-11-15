using d360.core.entities.Workflow;
using d360.web.Controllers.V2;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using Xunit;
using igx.UnitTests.Core;
using System.Net;
using System.Threading;
using d360.core.entities;
using d360.extensions;

namespace igx.UnitTests.V2ControllerTests
{
    [Trait("Unit tests", "Responsibilities controller")]
    public class ResponsibilitiesControllerTest : BaseTest
    {
        internal ResponsibilitiesController responsibilitiesController;

        public ResponsibilitiesControllerTest()
        {
            this.responsibilitiesController = new ResponsibilitiesController(GetCommunity(), GetCompany(), GetResponsibilityRepository(), GetAssetRepository(),
                GetSettingsRepository(), GetMediator())
            {
                Request = new HttpRequestMessage(),
                Configuration = new HttpConfiguration()
            };
        }

        [Fact]
        public async void GetResponsibilityTypes()
        {
            var result = await responsibilitiesController.GetResponsibilityTypesAsync();
            var str = await result.Content.ReadAsStringAsync();

            Assert.True(result.StatusCode == HttpStatusCode.OK, XMsg.InvalidJSON);
            AssertJSON.True<IEnumerable<ResponsibilityTypeViewModel>>(str);
        }

        [Fact]
        public async void GetResponsibilityTypesByAssetId()
        {
            var result = await responsibilitiesController.GetResponsibilityTypesByAssetTypeAsync(Guid.Parse(DataConstants.ValidGUID));
            var str = await result.Content.ReadAsStringAsync();

            Assert.True(result.StatusCode == HttpStatusCode.OK, XMsg.InvalidJSON);
            AssertJSON.True<IEnumerable<ResponsibilityTypeViewModel>>(str);
        }

        [Fact]
        public async void GetResponsibilityTypeAllocationsAsync()
        {
            var result = await responsibilitiesController.GetResponsibilityTypeAllocationsAsync(Guid.NewGuid());
            var str = await result.Content.ReadAsStringAsync();

            Assert.True(result.StatusCode == HttpStatusCode.OK, XMsg.InvalidJSON);
            AssertJSON.True<IEnumerable<ResponsibilityTypeAllocationViewModel>>(str);
        }

        [Fact]
        public async void GetResponsibilityTypeAllocationsByAssetAsync()
        {
            var result = await responsibilitiesController.GetResponsibilityTypeAllocationsByAssetAsync(Guid.NewGuid());
            var str = await result.Content.ReadAsStringAsync();

            Assert.True(result.StatusCode == HttpStatusCode.OK, XMsg.InvalidJSON);
            AssertJSON.True<IEnumerable<ResponsibilityTypeAllocationViewModel>>(str);
        }

        [Fact]
        public async void GetResponsibilityRulesForTypeAsync()
        {
            var result = await responsibilitiesController.GetResponsibilityRulesForTypeAsync(Guid.NewGuid());
            var str = await result.Content.ReadAsStringAsync();

            Assert.True(result.StatusCode == HttpStatusCode.OK, XMsg.InvalidJSON);
            AssertJSON.True<IEnumerable<ResponsibilityTypeRuleViewModel>>(str);
        }

        [Fact]
        public async void GetResponsibilityRulesStats()
        {
            var result = await responsibilitiesController.GetResponsibilityRulesStats(Guid.NewGuid());
            var str = await result.Content.ReadAsStringAsync();

            Assert.True(result.StatusCode == HttpStatusCode.OK, XMsg.InvalidJSON);
            AssertJSON.True<ResponsibilityTypeRuleStatsViewModel>(str);
        }

        [Fact]
        public async void GetResponsibilities()
        {
            var result = await responsibilitiesController.GetResponsibilities();
            var str = await result.Content.ReadAsStringAsync();

            Assert.True(result.StatusCode == HttpStatusCode.OK, XMsg.InvalidJSON);
            AssertJSON.True<AssetResponsibilitiesApiModel>(str);
        }
    }
}