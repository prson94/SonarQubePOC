using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using d360.web.Controllers.V2;
using System.Web.Http;
using System.Net.Http;
using d360.core.enums;
using d360.core.entities;
using igx.UnitTests.Core;
using System.Web.Http.Results;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;

namespace igx.UnitTests
{
    [Trait("Unit tests", "Fields controller")]
    public class FieldsControllerTest : BaseTest
    {
        internal FieldsController fieldsController;
        public FieldsControllerTest()
        {
            this.fieldsController = new FieldsController(GetCommunity(), GetCompany(), GetStorage(), GetQueue(), GetFieldsRepository(), GetAssetRepository())
            {
                Request = new HttpRequestMessage(),
                Configuration = new HttpConfiguration()
            };
        }

        [Fact]
        public async void GetFieldTypes()
        {
            var results = await fieldsController.GetFieldTypesAsync();
            var list = new FieldTypesApiViewModel();
            var res = results.TryGetContentValue(out list);

            Assert.True(results.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(list != null, XMsg.InvalidJSON);

        }

        [Fact]
        public async void PutFields_CheckIfValidatorIncluded()
        {
            FieldTypesApiEditModel model = new FieldTypesApiEditModel();
            var results = await fieldsController.PutFieldTypesAsync(model).Result.ExecuteAsync(new System.Threading.CancellationToken());
            var content = await results.Content.ReadAsStringAsync();

            Assert.True(results.StatusCode == HttpStatusCode.BadRequest, XMsg.BadResponseCode);
            
        }

        [Fact]
        public async void PutFields()
        {
            FieldTypesApiEditModel model = new FieldTypesApiEditModel();
            model.Fields = new List<FieldTypeApiEditModel>();
            model.AssetTypeUid = Guid.Parse(DataConstants.ValidGUID);
            model.Action = FieldTypesApiEditAction.Merge;
            var results = await fieldsController.PutFieldTypesAsync(model).Result.ExecuteAsync(new System.Threading.CancellationToken());
            var content = await results.Content.ReadAsStringAsync();

            var data = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(results.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(data != null, XMsg.InvalidJSON);
            Assert.True(data.GetValue("Uid") != null,"Uid field missing from response");
            Assert.True(data.GetValue("Success") != null, "Success field missing from response");
            Assert.True(data.GetValue("Message") != null, "Message field missing from response");
            Assert.True(Guid.Parse(data.GetValue("Uid").ToString()) == model.AssetTypeUid,"Invalid Uid returned!");
            Assert.True(bool.Parse(data.GetValue("Success").ToString()), "Invalid Success returned!");
        }

        [Fact]
        public async void DeleteFields_CheckIfValidationIncluded()
        {
            FieldTypesApiDeleteModel model = new FieldTypesApiDeleteModel();
            var results = await fieldsController.DeleteFieldTypesAsync(model).Result.ExecuteAsync(new System.Threading.CancellationToken());
            var content = await results.Content.ReadAsStringAsync();

            Assert.True(results.StatusCode == HttpStatusCode.BadRequest, XMsg.BadResponseCode);
        }

        [Fact]
        public async void DeleteFields()
        {
            FieldTypesApiDeleteModel model = new FieldTypesApiDeleteModel();
            model.Fields = new List<FieldTypeApiDeleteModel>();
            model.AssetTypeUid = Guid.Parse(DataConstants.ValidGUID);
            var results = await fieldsController.DeleteFieldTypesAsync(model).Result.ExecuteAsync(new System.Threading.CancellationToken());
            var content = await results.Content.ReadAsStringAsync();


            var data = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(results.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(data != null, XMsg.InvalidJSON);
            Assert.True(data.GetValue("Uid") != null, "Uid field missing from response");
            Assert.True(data.GetValue("Success") != null, "Success field missing from response");
            Assert.True(data.GetValue("Message") != null, "Message field missing from response");
            Assert.True(Guid.Parse(data.GetValue("Uid").ToString()) == model.AssetTypeUid, "Invalid Uid returned!");
            Assert.True(bool.Parse(data.GetValue("Success").ToString()), "Invalid Success returned!");
        }
    }
}
