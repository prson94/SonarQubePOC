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
using d360.core.enums;
using System.Threading;
using System.Net;
using d360.core;

namespace igx.UnitTests.V2ControllerTests
{
    [Trait("Unit tests", "Relationship controller")]
    public class RelationshipControllerTest : BaseTest
    {
        internal RelationshipsController relationshipsController;

        public RelationshipControllerTest()
        {
            this.relationshipsController = new RelationshipsController(GetCommunity(), GetCompany(), GetQueue(), GetStorage(), GetRelationshipRepository(), GetFieldsRepository(), GetAssetRepository())
            {
                Request = new HttpRequestMessage() {
                    RequestUri = new Uri("http://unit-tests.eng.data3sixty.local/home")
                },
                Configuration = new HttpConfiguration()
            };
        }

        [Fact]
        public async void GetPredicatesAsync()
        {

            var actionResult = await relationshipsController.GetPredicatesAsync();

            var str = actionResult.Content.ReadAsStringAsync().Result;
            var data = JsonConvert.DeserializeObject<List<PredicateApiViewModel>>(str);

            Assert.True(actionResult.IsSuccessStatusCode);
            Assert.True(data.Count == DataConstants.GetPredicates().Count());

        }

        [Fact]
        public async void GetPredicatesAsyncByGuid()
        {

            var actionResult = await relationshipsController.GetPredicatesAsync(DataConstants.GetPredicates().First().Uid);

            var str = actionResult.Content.ReadAsStringAsync().Result;
            var data = JsonConvert.DeserializeObject<List<PredicateApiViewModel>>(str);

            Assert.True(actionResult.IsSuccessStatusCode);
            Assert.True(data.Count == 1);

        }

        [Fact]
        public async void GetPredicatesAsyncByType()
        {

            var actionResult = await relationshipsController.GetPredicatesAsync(null, DataConstants.GetPredicates().First().Type);

            var str = actionResult.Content.ReadAsStringAsync().Result;
            var data = JsonConvert.DeserializeObject<List<PredicateApiViewModel>>(str);

            Assert.True(actionResult.IsSuccessStatusCode);
            Assert.True(data.Count == 1);

        }
        [Fact]
        public async void GetPredicatesAsyncByName()
        {

            var actionResult = await relationshipsController.GetPredicatesAsync(null, null, DataConstants.GetPredicates().First().Name);

            var str = actionResult.Content.ReadAsStringAsync().Result;
            var data = JsonConvert.DeserializeObject<List<PredicateApiViewModel>>(str);

            Assert.True(actionResult.IsSuccessStatusCode);
            Assert.True(data.Count == 1);

        }

        [Fact]
        public async void GetPredicatesAsyncByInverse()
        {

            var actionResult = await relationshipsController.GetPredicatesAsync(null, null, null, DataConstants.GetPredicates().First().Inverse);

            var str = actionResult.Content.ReadAsStringAsync().Result;
            var data = JsonConvert.DeserializeObject<List<PredicateApiViewModel>>(str);

            Assert.True(actionResult.IsSuccessStatusCode);
            Assert.True(data.Count == 1);

        }

        [Fact]
        public void GetPredicatesTypesAsync()
        {

            var actionResult = relationshipsController.GetPredicatesTypesAsync();

            var str = actionResult.Content.ReadAsStringAsync().Result;
            var data = JsonConvert.DeserializeObject<List<PredicateTypeApiViewModel>>(str);

            Assert.True(actionResult.IsSuccessStatusCode);
            Assert.True(data.Count > 0); // Count is variable, depending on lineage version.

        }

        [Fact]
        public async void ExportToExcel()
        {

            var actionResult = relationshipsController.ExportToExcel(DataConstants.ValidGUID).ExecuteAsync(new CancellationToken()).Result;

            var str = await actionResult.Content.ReadAsStringAsync();

            Assert.True(actionResult.IsSuccessStatusCode);
            Assert.True(actionResult.Content.Headers.ContentType.ToString() == "application/vnd.ms-excel");

        }

        [Fact]
        public async void GetRelationshipsAsync()
        {

            var actionResult = relationshipsController.GetRelationshipsAsync();

            var str = await actionResult.Result.Content.ReadAsStringAsync();

            Assert.True(actionResult.Result.IsSuccessStatusCode);
            Assert.True(!string.IsNullOrEmpty(str));
            var data = JsonConvert.DeserializeObject<GetRelationshipsApiModel>(str);
            Assert.True(data.items.Count > 0);

        }
        [Fact]
        public async void GetRelationshipsAsyncInvalidRelationshipUid()
        {

            var actionResult = relationshipsController.GetRelationshipsAsync(Guid.Parse(DataConstants.InvalidGUID));

            var str = await actionResult.Result.Content.ReadAsStringAsync();

            Assert.True(!actionResult.Result.IsSuccessStatusCode);
            Assert.True(actionResult.Result.StatusCode == HttpStatusCode.NotFound);

        }

        [Fact]
        public async void GetRelationshipsAsyncInvalidPredicateUid()
        {

            var actionResult = relationshipsController.GetRelationshipsAsync(null,Guid.Parse(DataConstants.InvalidGUID));

            var str = await actionResult.Result.Content.ReadAsStringAsync();

            Assert.True(!actionResult.Result.IsSuccessStatusCode);
            Assert.True(actionResult.Result.StatusCode == HttpStatusCode.NotFound);

        }

        [Fact]
        public async void GetRelationshipsAsyncInvalidSubjectUid()
        {

            var actionResult = relationshipsController.GetRelationshipsAsync(null,null, Guid.Parse(DataConstants.InvalidGUID));

            var str = await actionResult.Result.Content.ReadAsStringAsync();

            Assert.True(!actionResult.Result.IsSuccessStatusCode);
            Assert.True(actionResult.Result.StatusCode == HttpStatusCode.NotFound);

        }

        [Fact]
        public async void GetRelationshipsAsyncInvalidObjectUid()
        {

            var actionResult = relationshipsController.GetRelationshipsAsync(null, null,null, Guid.Parse(DataConstants.InvalidGUID));

            var str = await actionResult.Result.Content.ReadAsStringAsync();

            Assert.True(!actionResult.Result.IsSuccessStatusCode);
            Assert.True(actionResult.Result.StatusCode == HttpStatusCode.NotFound);

        }

        [Fact]
        public async void GetRelationshipsAsyncAllValidUid()
        {

            var actionResult = relationshipsController.GetRelationshipsAsync(Guid.Parse(DataConstants.ValidGUID), Guid.Parse(DataConstants.ValidGUID), Guid.Parse(DataConstants.ValidGUID), Guid.Parse(DataConstants.ValidGUID));

            var str = await actionResult.Result.Content.ReadAsStringAsync();

            Assert.True(actionResult.Result.IsSuccessStatusCode);
            Assert.True(actionResult.Result.StatusCode == HttpStatusCode.OK);

        }

        [Fact]
        public async void GetRelationshipTypesAsync()
        {

            var actionResult = relationshipsController.GetRelationshipTypesAsync();

            var str = await actionResult.Result.Content.ReadAsStringAsync();


            Assert.True(actionResult.Result.IsSuccessStatusCode);
            Assert.True(!string.IsNullOrEmpty(str));
            var data = JsonConvert.DeserializeObject<List<IntersectTypeApiViewModel>>(str);
            Assert.True(data.Count > 0);

        }

        [Fact]
        public async void GetRelationshipTypesAsyncWithParams()
        {

            var actionResult = relationshipsController.GetRelationshipTypesAsync(0, SystemObjects.Artifact.ToString());

            var str = await actionResult.Result.Content.ReadAsStringAsync();


            Assert.True(actionResult.Result.IsSuccessStatusCode);
            Assert.True(!string.IsNullOrEmpty(str));
            var data = JsonConvert.DeserializeObject<List<IntersectTypeApiViewModel>>(str);
            Assert.True(data.Count > 0);

        }


        [Fact]
        public async void GetRelationshipTypesAsyncWithErrParams()
        {

            var actionResult = relationshipsController.GetRelationshipTypesAsync(0, "wrong object type");

            var str = await actionResult.Result.Content.ReadAsStringAsync();


            Assert.True(!actionResult.Result.IsSuccessStatusCode);
            Assert.True(actionResult.Result.StatusCode == HttpStatusCode.BadRequest);

        }

        [Fact]
        public void GetIntersectType()
        {

            var actionResult = relationshipsController.GetIntersectType(1);

            Assert.True(actionResult.Count() > 0);

        }

        [Fact]
        public async void ERR_PostRelationshipAsync_InvalidUid()
        {
            var model = new RelationshipInserts();

            var actionResult = await relationshipsController.PostRelationshipsAsync(Guid.Parse(DataConstants.InvalidGUID),model);
            var result = await actionResult.ExecuteAsync(new CancellationToken());
            var str = result.Content.ReadAsStringAsync();

            Assert.True(!result.IsSuccessStatusCode);
            Assert.True(result.StatusCode == HttpStatusCode.NotFound);
        }

        [Fact]
        public async void ERR_PostRelationshipAsync_InvalidModel()
        {
            var model = new RelationshipInserts();

            var actionResult = await relationshipsController.PostRelationshipsAsync(Guid.Parse(DataConstants.ValidGUID), null);
            var result = await actionResult.ExecuteAsync(new CancellationToken());
            var str = result.Content.ReadAsStringAsync();

            Assert.True(!result.IsSuccessStatusCode);
            Assert.True(result.StatusCode == HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async void ERR_PostRelationshipAsync_MaxLimitReached()
        {
            var model = new RelationshipInserts();
            for(int i =0; i<= 251; i++)
            {
                model.Add(new RelationshipInsert());
            }

            var actionResult = await relationshipsController.PostRelationshipsAsync(Guid.Parse(DataConstants.ValidGUID), model);
            var result = await actionResult.ExecuteAsync(new CancellationToken());
            var str = result.Content.ReadAsStringAsync();

            Assert.True(!result.IsSuccessStatusCode);
            Assert.True(result.StatusCode == HttpStatusCode.BadRequest);
        }
        [Fact]
        public async void PostRelationshipAsync()
        {
            var model = new RelationshipInserts();
            for (int i = 0; i <= 10; i++)
            {
                model.Add(new RelationshipInsert());
            }

            var actionResult = await relationshipsController.PostRelationshipsAsync(Guid.Parse(DataConstants.ValidGUID), model);
            var result = await actionResult.ExecuteAsync(new CancellationToken());
            var str = await result.Content.ReadAsStringAsync();

            Assert.True(result.IsSuccessStatusCode);
            Assert.True(result.StatusCode == HttpStatusCode.OK);
            Assert.True(!string.IsNullOrEmpty(str));
            var data = JsonConvert.DeserializeObject<List<DatabaseBulkRelationshipResult>>(str);
            Assert.True(data.Count > 0);
        }

        [Fact]
        public async void ERR_PostBulkRelationshipAsync_InvalidUid()
        {
            var model = new RelationshipInserts();

            var actionResult = await relationshipsController.PostBulkRelationshipsAsync(Guid.Parse(DataConstants.InvalidGUID), model);
            var result = await actionResult.ExecuteAsync(new CancellationToken());
            var str = result.Content.ReadAsStringAsync();

            Assert.True(!result.IsSuccessStatusCode);
            Assert.True(result.StatusCode == HttpStatusCode.NotFound);
        }

        [Fact]
        public async void ERR_PostBulkRelationshipAsync_InvalidModel()
        {
            var model = new RelationshipInserts();

            var actionResult = await relationshipsController.PostBulkRelationshipsAsync(Guid.Parse(DataConstants.ValidGUID), null);
            var result = await actionResult.ExecuteAsync(new CancellationToken());
            var str = result.Content.ReadAsStringAsync();

            Assert.True(!result.IsSuccessStatusCode);
            Assert.True(result.StatusCode == HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async void PostBulkRelationshipAsync_MaxLimitReached()
        {
            var model = new RelationshipInserts();
            for (int i = 0; i <= 500; i++)
            {
                model.Add(new RelationshipInsert());
            }

            var actionResult = await relationshipsController.PostBulkRelationshipsAsync(Guid.Parse(DataConstants.ValidGUID), model);
            var result = await actionResult.ExecuteAsync(new CancellationToken());
            var str = result.Content.ReadAsStringAsync();

            Assert.True(result.IsSuccessStatusCode);
            Assert.True(result.StatusCode == HttpStatusCode.OK);
        }
        [Fact]
        public async void PostBulkRelationshipAsync()
        {
            var model = new RelationshipInserts();
            for (int i = 0; i <= 10; i++)
            {
                model.Add(new RelationshipInsert());
            }

            var actionResult = await relationshipsController.PostBulkRelationshipsAsync(Guid.Parse(DataConstants.ValidGUID), model);
            var result = await actionResult.ExecuteAsync(new CancellationToken());
            var str = await result.Content.ReadAsStringAsync();

            Assert.True(result.IsSuccessStatusCode);
            Assert.True(result.StatusCode == HttpStatusCode.OK);
            Assert.True(!string.IsNullOrEmpty(str));
            var data = JsonConvert.DeserializeObject<JObject>(str);
            Assert.True(data.GetValue("ExecutionID").ToString() != null);
            Assert.True(data.GetValue("Message").ToString() != null);
            Assert.True(data.GetValue("Uri").ToString() != null);
        }

        [Fact]
        public async void GetExecutionStatus()
        {

            var actionResult = relationshipsController.GetExecutionStatus(Guid.Parse(DataConstants.ValidGUID));

            var result = await actionResult.Result.ExecuteAsync(new CancellationToken());
            var str = await result.Content.ReadAsStringAsync();

            Assert.True(result.IsSuccessStatusCode);
            Assert.True(result.StatusCode == HttpStatusCode.OK);
            Assert.True(!string.IsNullOrEmpty(str));
            var data = JsonConvert.DeserializeObject<JObject>(str);
            Assert.True(data.GetValue("CompletedOn").ToString() != null);
            Assert.True(data.GetValue("Error").ToString() != null);
            Assert.True(data.GetValue("Fields").ToString() != null);
            Assert.True(data.GetValue("Processed").ToString() != null);
            Assert.True(data.GetValue("StartedOn").ToString() != null);
            Assert.True(data.GetValue("Total").ToString() != null);
            Assert.True(data.GetValue("Results").ToString() != null);

        }

        [Fact]
        public async void DeleteRelationships()
        {

            var guid = Guid.Parse(DataConstants.ValidGUID);
            var model = new RelationshipDeletes();
            model.Add(new RelationshipDelete() { Uid = Guid.Parse(DataConstants.ValidGUID) });

            var actionResult = relationshipsController.DeleteRelationships(guid, model);
            var result = actionResult.Result.ExecuteAsync(new CancellationToken()).Result;
            var str = await result.Content.ReadAsStringAsync();


            Assert.True(result.IsSuccessStatusCode);
            Assert.True(!string.IsNullOrEmpty(str));
            var data = JsonConvert.DeserializeObject<List<DatabaseBulkRelationshipResult>>(str);
            Assert.True(data.Count > 0);

        }

        [Fact]
        public async void ERR_DeleteRelationships_InvalidGuid()
        {

            var guid = Guid.Parse(DataConstants.InvalidGUID);
            var model = new RelationshipDeletes();
            model.Add(new RelationshipDelete() { Uid = Guid.Parse(DataConstants.ValidGUID) });

            var actionResult = relationshipsController.DeleteRelationships(guid, model);
            var result = actionResult.Result.ExecuteAsync(new CancellationToken()).Result;
            var str = await result.Content.ReadAsStringAsync();


            Assert.True(!result.IsSuccessStatusCode);
            Assert.True(result.StatusCode == HttpStatusCode.NotFound);

        }

        [Fact]
        public async void ERR_DeleteRelationships_InvalidModel()
        {

            var guid = Guid.Parse(DataConstants.ValidGUID);
            var model = new RelationshipDeletes();

            var actionResult = relationshipsController.DeleteRelationships(guid, model);
            var result = actionResult.Result.ExecuteAsync(new CancellationToken()).Result;
            var str = await result.Content.ReadAsStringAsync();


            Assert.True(!result.IsSuccessStatusCode);
            Assert.True(result.StatusCode == HttpStatusCode.BadRequest);

        }

        [Fact]
        public async void ERR_DeleteRelationships_InvalidModelItemUid()
        {

            var guid = Guid.Parse(DataConstants.ValidGUID);
            var model = new RelationshipDeletes();
            model.Add(new RelationshipDelete() { Uid = Guid.Parse(DataConstants.InvalidGUID) });

            var actionResult = relationshipsController.DeleteRelationships(guid, model);
            var result = actionResult.Result.ExecuteAsync(new CancellationToken()).Result;
            var str = await result.Content.ReadAsStringAsync();


            Assert.True(!result.IsSuccessStatusCode);
            Assert.True(result.StatusCode == HttpStatusCode.BadRequest);

        }
    }
}
