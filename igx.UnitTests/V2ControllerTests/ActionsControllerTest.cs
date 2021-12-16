using d360.web.Controllers.V2;
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
    [Trait("Unit tests", "Asset controller")]
    public class ActionsControllerTest : BaseTest
    {

        internal ActionsController actionsController;
        
        public ActionsControllerTest()
        {
            this.actionsController = new ActionsController(GetCoreComponentSet(), GetCommentRepository(), GetIssueRepository(), GetAssetRepository(), GetResponsibilityRepository())
            {
                Request = new HttpRequestMessage(),
                Configuration = new HttpConfiguration()
            };
        }

        [Fact]
        public async void GetIssueTypesTest()
        {
            var actionResult = await actionsController.GetIssueTypes();
            Assert.True(actionResult.IsSuccessStatusCode, XMsg.BadResponseCode);
        }

        [Fact]
        public async void GetAllocationByAssetTypeAsyncTest()
        {
            var testGuid = Guid.Parse(DataConstants.ValidGUID);
            var actionResult = await actionsController.GetAllocationByAssetTypeAsync(testGuid);
            var res = actionResult.ExecuteAsync(new System.Threading.CancellationToken());

            var str = res.Result.Content.ReadAsStringAsync().Result;
            var data = JsonConvert.DeserializeObject<JArray>(str);

            Assert.True(res.Result.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(data != null, XMsg.InvalidJSON);

        }

        [Fact]
        public void CheckMaxPageSize()
        {
            string pageSize = "500000";
            string pageNum = "1";

            Dictionary<string, string> pageParams = new Dictionary<string, string> { { "_pageSize", pageSize }, { "_pageNum", pageNum } };
            string result = actionsController.isPageSizeAndNumValid(pageParams);

            Assert.Matches("Invalid pageSize value provided. Number is too large",result);

        }

        [Fact]
        public void CheckMaxPageNum()
        {
            string pageSize = "5";
            string pageNum = "150000";
            Dictionary<string, string> pageParams = new Dictionary<string, string> { { "_pageSize", pageSize }, { "_pageNum", pageNum } };

            string result = actionsController.isPageSizeAndNumValid(pageParams);

            Assert.Matches("Invalid pageNum value provided. Number is too large",result);

        }
        [Fact]
        public void CheckNegPageSize()
        {
            string pageSize = "-1";
            string pageNum = "1";

            Dictionary<string, string> pageParams = new Dictionary<string, string> { { "_pageSize", pageSize }, { "_pageNum", pageNum } };
            string result = actionsController.isPageSizeAndNumValid(pageParams);

            Assert.Matches("Invalid pageSize value provided. Value must be greater than 0",result);

        }

        [Fact]
        public void CheckNegPageNum()
        {
            string pageSize = "5";
            string pageNum = "-1";
            Dictionary<string, string> pageParams = new Dictionary<string, string> { { "_pageSize", pageSize }, { "_pageNum", pageNum } };

            string result = actionsController.isPageSizeAndNumValid(pageParams);

            Assert.Matches("Invalid pageNum value provided. Value must be greater than 0",result);

        }

        [Fact]
        public void CheckNonNumericValue()
        {
            string pageSize = "abcdef";
            string pageNum = "-1";
            Dictionary<string, string> pageParams = new Dictionary<string, string> { { "_pageSize", pageSize }, { "_pageNum", pageNum } };

            string result = actionsController.isPageSizeAndNumValid(pageParams);

            Assert.Matches("Invalid pageSize value provided. Must be a numeric value", result);

        }

        [Fact]
        public void CheckMaxLengthOfPageSize()
        {
            string pageSize = "12345678901";
            string pageNum = "1";
            Dictionary<string, string> pageParams = new Dictionary<string, string> { { "_pageSize", pageSize }, { "_pageNum", pageNum } };

            string result = actionsController.isPageSizeAndNumValid(pageParams);

            Assert.Matches("Invalid pageSize value provided.", result);

        }

        [Fact]
        public void CheckMaxLengthOfPageNum()
        {
            string pageNum = "12345678901";
            string pageSize = "1";
            Dictionary<string, string> pageParams = new Dictionary<string, string> { { "_pageSize", pageSize }, { "_pageNum", pageNum } };

            string result = actionsController.isPageSizeAndNumValid(pageParams);

            Assert.Matches("Invalid pageNum value provided.", result);

        }
    }
}
