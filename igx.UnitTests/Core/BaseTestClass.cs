using AutoFixture;
using d360.core;
using d360.core.entities;
using d360.core.entities.Metric;
using d360.core.entities.Workflow;
using d360.core.enums;
using d360.core.queue;
using d360.core.validators;
using d360.extensions;
using d360.model;
using d360.model.DataAccessLayer;
using d360.model.helpers.filters;
using d360.web.Controllers;
using d360.web.Services;
using d360.web.Utilities;
using d360.web.validators;
using FluentAssertions;
using igx.UnitTests.Core;
using Moq;
using Moq.Language;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using repositories;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace igx.UnitTests
{
	public abstract class BaseTest
    {
        protected IFixture Fixture { get; }

        protected BaseTest()
        {
            Fixture = FixtureProvider.Create();
        }

        protected TestException ThrowsTestExceptionAsync<TMock, TResult>(IReturns<TMock, Task<TResult>> mock) where TMock : class
        {
	        var exception = Fixture.Create<TestException>();
	        mock.ThrowsAsync(exception);
	        return exception;
        }

        protected async Task VerifyTestExceptionAsync<TResult>(Task<TResult> task, TestException testException)
        {
	        // act
	        try
	        {
		        await task;
                Assert.False(true, $"Exception not thrown");
	        }
	        catch (Exception exception)
	        {
		        // assert
		        exception.Should().Be(testException);
	        }
        }

        protected Func<Task<TResult>> Act<TResult>(Task<TResult> task)
        {
	        return () => task;
        }

		#region Mock Interfaces

		public ICommunity GetCommunity()
		{
			var mock = new Mock<ICommunity>();
			mock.Setup(x => x.ReadSettingAsync(It.IsAny<int>(), It.IsAny<Setting>()) )
				.ReturnsAsync(Setting.ActionMessage.AsInfoModel());

			mock.Setup(x => x.ReadSettingValueAsync<bool>(It.IsAny<int>(), It.IsAny<Setting>()))
				.ReturnsAsync(false);

			mock.Setup(x => x.ReadSettingsAsDictionaryAsync(It.IsAny<int>()))
				.ReturnsAsync(Setting.ActionMessage.GetAsList().ToDictionary(k => k.ID.ToString(), v => v.Value ?? v.DefaultValue));
			mock.Setup(x => x.ReadSettingsAsync(It.IsAny<int>()))
				.ReturnsAsync(Setting.ActionMessage.GetAsList());

			mock.Setup(x => x.ReadSettingValueAsync<int>(It.IsAny<int>(), It.IsAny<Setting>()));

			mock.Setup(x => x.ReadThemeUidAsync(It.IsAny<int>(), It.IsAny<Guid>()))
							.ReturnsAsync(new Theme());

			mock.Setup(x => x.ReadThemeAsync(It.IsAny<int>(), It.IsAny<string>()))
							.ReturnsAsync(new Theme());

			return mock.Object;
		}
        
		public static ICompanyContext GetCompany()
        {
            var mock = new Mock<ICompanyContext>();
            mock.Setup(x => x.HasAssetTypePermission(It.IsAny<int>(), Permission.ReadAsset)).Returns(
                (int id, Permission p) =>
                {
                    return true;
                }
                );
            mock.Setup(x => x.GetTypeIdentifierInfoModel(It.IsAny<TypeIdentifierInfoModelType>(), It.IsAny<Guid>()))
                 .Returns((TypeIdentifierInfoModelType type, Guid uid) =>
                       {
                           if (uid != Guid.Parse(DataConstants.ValidGUID))
                               return null;
                           else
                           {
							   TypeIdentifierInfoModel result = null;
                               result = new TypeIdentifierInfoModel()
                               {
                                   Object = type.ToString(),
                                   Uid = uid
                               };
							   return Task.FromResult(result);

						   }
                       }

                 );

            var assetTypes = new List<AssetType> { new AssetType { ID = 1, Name = "unit test" } }.AsQueryable();

			var assets = new List<Asset> { new Asset { ID = 1, AssetTypeID=1,Object="Artifact", ObjectID =1} }.AsQueryable();

			var assetMock = CreateDbSetMock<Asset>(assets);
			mock.Setup(x => x.Assets)
				.Returns(assetMock.Object);

			var assetTypeMock = CreateDbSetMock<AssetType>(assetTypes);
            mock.Setup(x => x.Filter<AssetType>(It.IsAny<Expression<Func<AssetType, bool>>>()))
                .Returns(assetTypeMock.Object);

            var fieldTypes = new List<FieldType> { new FieldType { ID = 1, Name = "unit test", Type = "not a tag", AssetTypeID = 1 } }.AsQueryable();
            var fieldTypeMock = CreateDbSetMock<FieldType>(fieldTypes);
            mock.Setup(x => x.FieldTypes).Returns(fieldTypeMock.Object);

            var assetDetails = new List<AssetDetail> { new AssetDetail { uid = Guid.Parse(DataConstants.ValidGUID), AssetTypeUid = Guid.Parse(DataConstants.ValidGUID2) } }.AsQueryable();
            var assetDetailsMock = CreateDbSetMock<AssetDetail>(assetDetails);

            var metricAllocations = new List<MetricAllocation> { new MetricAllocation { ScoreType = ScoreType.Governance, OverrideName = null, AssetTypeUid = Guid.Parse(DataConstants.ValidGUID2) } }.AsQueryable();
            var metricAllocationsMock = CreateDbSetMock<MetricAllocation>(metricAllocations);

            var issues = new List<Issue> { new Issue { ID = 1, AssetID = 1, AssetTypeID = 1 } }.AsQueryable();
            var issuesMock = CreateDbSetMock<Issue>(issues);
            mock.Setup(x => x.Issues).Returns(issuesMock.Object);

			mock.Setup(x => x.ImportRelationships(It.IsAny<ApiExecution>(), It.IsAny<IntersectType>(), It.IsAny<RelationshipInserts>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()))
				.Returns(new List<DatabaseBulkRelationshipResult>() { new DatabaseBulkRelationshipResult() });

            mock.Setup(x => x.HasAssetTypePermission(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Permission>()))
                .Returns(true);

            mock.Setup(x => x.GetObjectDetail(It.IsAny<string>(), It.IsAny<long>()))
                .Returns(new ObjectDetail() { Name = "ObjectName", Description = "ObjectDescription" });

            IList<Field> fields = new List<Field>
              {
                new Field(){ AssetID = 1, FieldTypeID = 1, FormattedValue = "TestStringValue" },
                new Field(){ AssetID = 1, FieldTypeID = 2, FormattedValue = "12.56" },
                new Field(){ AssetID = 1, FieldTypeID = 3, FormattedValue = "10/10/2019", Value="5/2/2019" },
                new Field(){ AssetID = 1, FieldTypeID = 4, Value = "True", FormattedValue = "True" },
                new Field(){ AssetID = 1, FieldTypeID = 5, Value ="1,2", FormattedValue = "Test1,Test2" },
                new Field(){ AssetID = 1, FieldTypeID = 6 }
              };

            var fieldsMock = CreateDbSetMock(fields);
            mock.Setup(x => x.Fields).Returns(fieldsMock.Object);

            mock.Setup(x => x.GetFieldLookupValue(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .Returns((string type, int objId, int ftId, string value) => value == "validlookupvalue" ? 1 : 0);			

            var workflowItemSteps = new List<WorkflowItemStep>() {
 new WorkflowItemStep(){
     StepID = 1,
     ItemID = 1,
     Fields = "<fields TotalResources=\"1\" NumberOfResponses=\"1\">"+
  "<form ResourceID=\"1\">"+
    "<field id=\"boolean1\" label=\"Text\" value=\"True\" displayvalue=\"True\" fieldtype=\"boolean\" />"+
    "<field id=\"integer1\" label=\"Text\" value=\"45\" displayvalue=\"45\" fieldtype=\"integer\" />"+
    "<field id=\"text1\" label=\"Text\" value=\"TestText\" displayvalue=\"TestText\" fieldtype=\"text\" />"+
  "</form>"+
 "</fields>",
     Step = new WorkflowVersionStep(){ Fields = "<fields>"+
  "<form title=\"Form test\">"+
    "<field type=\"boolean\" required=\"true\" label=\"Field 1\" id=\"boolean1\" />"+
    "<field type=\"integer\" required=\"true\" label=\"Field 1\" id=\"integer1\" />"+
    "<field type=\"text\" required=\"true\" label=\"Field 1\" id=\"text1\" />"+
  "</form>"+
"</fields>" ,
     Settings = "<settings>"+
  "<FormResponseType>FirstResponse</FormResponseType>"+
  "<SendFormEmail>false</SendFormEmail>"+
  "<MessageRecipientType>Initiator</MessageRecipientType>"+
  "<IncludePreviousFormResponses>false</IncludePreviousFormResponses>"+
"</settings>"}
 }
            };

            mock.Setup(x => x.WorkflowItemSteps).Returns(CreateDbSetMock(workflowItemSteps).Object);

            return mock.Object;
        }

		public CommunityFeatureFlagService GetCommunityFlags()
		{
			var flagservice = new CommunityFeatureFlagService(GetCache(), GetCommunity(), GetSecurity());
			return flagservice;
		}

        public static Mock<DbSet<T>> CreateDbSetMock<T>(IEnumerable<T> elements) where T : class
        {
            var elementsAsQueryable = elements.AsQueryable();
            var dbSetMock = new Mock<DbSet<T>>();

            dbSetMock.As<IQueryable<T>>().Setup(m => m.Provider).Returns(elementsAsQueryable.Provider);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.Expression).Returns(elementsAsQueryable.Expression);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(elementsAsQueryable.ElementType);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(elementsAsQueryable.GetEnumerator());

            return dbSetMock;
        }

        public ICoreComponentSet GetCoreComponentSet()
        {
            var mock = new Mock<ICoreComponentSet>();
			mock.Setup(s => s.Cache).Returns(GetCache());
			mock.Setup(s => s.Catalogs).Returns(GetCatalogs());
            mock.Setup(s => s.Community).Returns(GetCommunity());
            mock.Setup(s => s.Company).Returns(GetCompany());
			mock.Setup(s => s.CommunityFlags).Returns(GetCommunityFlags());
            mock.Setup(s => s.Workspace).Returns(GetWorkspacesRepository());
			mock.Setup(s => s.RuntimeInfo).Returns(GetRuntimeInfo());
			mock.Setup(s => s.SecurityContext).Returns(GetSecurity());
			return mock.Object;
        }

		private IRuntimeInfo GetRuntimeInfo()
		{
			var mock = new Mock<IRuntimeInfo>();

			return mock.Object;
		}

		public IStorageProvider GetStorage()
        {
            var mock = new Mock<IStorageProvider>();
            mock.Setup(x => x.GetFileContentsAsString(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(() => JsonConvert.SerializeObject(new List<DatabaseBulkAssetResult>()));

            return mock.Object;
        }

        public IQueueSource GetQueue()
        {
            var mock = new Mock<IQueueSource>();

            return mock.Object;
        }

        public ISecurityContextProvider GetSecurity()
        {
            var mock = new Mock<ISecurityContextProvider>();

            return mock.Object;
        }

		public ICachingProvider GetCache()
        {
            var mock = new Mock<ICachingProvider>();

            return mock.Object;
        }

        public IAssetRepository GetAssetRepository()
        {
            var mockRepo = new Mock<IAssetRepository>();
            var realRepo = new AssetRepository(GetCompany(), GetSecurity(), GetQueue(), GetStorage());

            mockRepo.Setup(x => x.GetAssetType(It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<AssetTypeClass?>(), It.IsAny<Guid?>()))
                .Returns(
                Task.FromResult<IEnumerable<AssetTypeApiViewModel>>(new List<AssetTypeApiViewModel>() { new AssetTypeApiViewModel() })
            );

            mockRepo.Setup(x => x.GetAssetByUID(It.IsAny<Guid>()))
                .Returns((Guid uid) => uid == Guid.Parse(DataConstants.ValidGUID) ? new Asset() : null);

            mockRepo.Setup(x => x.GetAssetTypeByUidAndClass(It.IsAny<Guid>(), It.IsAny<AssetTypeClass>()))
                .Returns((Guid uid, AssetTypeClass @class) => uid == Guid.Parse(DataConstants.ValidGUID) ? new AssetType() { Object = "ArtifactType", uid = uid } : null);

            mockRepo.Setup(x => x.GetAssetTypeList()).Returns(AssetTypeClass.BusinessAsset.GetAsList());

            mockRepo.Setup(x => x.GetFieldTypes(It.IsAny<Guid>()))
                .Returns(() => JsonConvert.DeserializeObject<dynamic>(DataConstants.FieldTypesJsonFormat));

            mockRepo.Setup(x => x.GetAssets(It.IsAny<AssetType>(), It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>(), It.IsAny<int>()))
                .Returns(Task.FromResult(new AssetsApiViewModel()));

            mockRepo.Setup(x => x.GetAssetTypeByUID(It.IsAny<Guid>()))
                .Returns((Guid uid) => uid == Guid.Parse(DataConstants.ValidGUID) || uid == Guid.Parse(DataConstants.ValidGUID2) ? new AssetType() { Object = "ArtifactType", uid = uid } : null);

            mockRepo.Setup(x => x.GetPredicateByUID(It.IsAny<Guid>()))
                .Returns((Guid uid) => uid == Guid.Parse(DataConstants.ValidGUID) ? new Predicate() { UID = uid, Type = PredicateType.InterTypeHierarchy } : null);

            mockRepo.Setup(x => x.PostAssets(It.IsAny<List<AssetApiModel>>(), It.IsAny<AssetType>(), It.IsAny<ApiExecution>(), true, false,false))
                .Returns((List<AssetApiModel> assetInsertList, object o2, object o3, object o4, object o5, object o6) =>
                 {
                     if (assetInsertList.Count == 0) return null;
                     else return new List<DatabaseBulkAssetResult>() { };
                 }
                );

            mockRepo.Setup(x => x.PutAssets(It.IsAny<List<AssetApiModel>>(), It.IsAny<AssetType>(), It.IsAny<ApiExecution>(), true, false,false))
                .Returns((List<AssetApiModel> assetUpdateList, object o2, object o3, object o4, object o5, object o6) =>
                {
                    if (assetUpdateList.Count == 0) return null;
                    else return new List<DatabaseBulkAssetResult>() { };
                }
                );

            mockRepo.Setup(x => x.DeleteAssets(It.IsAny<AssetDeletes>(), It.IsAny<AssetType>(), It.IsAny<ApiExecution>(), true))
                .Returns((AssetDeletes assetDeletes, object o2, object o3, object o4) =>
                {
                    if (assetDeletes == null) return null;
                    else return new List<DatabaseBulkAssetResult>() { };
                }
                );

            mockRepo.Setup(x => x.PostBulkAssets(It.IsAny<List<AssetApiModel>>(), It.IsAny<ApiExecution>(), It.IsAny<bool>()))
               .Returns(Task.FromResult(new ApiExecutionInfo()));

            mockRepo.Setup(x => x.PutBulkAssets(It.IsAny<Guid>(), It.IsAny<List<AssetApiModel>>(), It.IsAny<ApiExecution>(), It.IsAny<bool>()))
               .Returns(Task.FromResult(new ApiExecutionInfo()));

            mockRepo.Setup(x => x.DeleteBulkAssets(It.IsAny<Guid>(), It.IsAny<AssetDeletes>(), It.IsAny<ApiExecution>(), false, true))
               .Returns(Task.FromResult(new ApiExecutionInfo()));

            mockRepo.Setup(x => x.DeleteBulkAssets(It.IsAny<Guid>(), It.IsAny<AssetDeletes>(), It.IsAny<ApiExecution>(), true, true))
               .Returns(Task.FromResult(new ApiExecutionInfo()));

            mockRepo.Setup(x => x.GetAssetTypeByModel(It.IsAny<AssetTypeUpsert>()))
                .Returns(new AssetType());

            string outString;
            bool outBool;
            mockRepo.Setup(x => x.AddAssetType(It.IsAny<AssetTypeUpsert>(), It.IsAny<AssetType>(), It.IsAny<AssetType>(), It.IsAny<Predicate>(), 0, out outString, out outBool))
                .Returns(() => new Tuple<HttpStatusCode, string, string>(HttpStatusCode.OK, "", ""));

            mockRepo.Setup(x => x.UpsertAssetStyle(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));

            mockRepo.Setup(x => x.UpdateAssetType(It.IsAny<AssetTypeUpsert>(), It.IsAny<AssetType>(), It.IsAny<AssetType>(), It.IsAny<Predicate>()))
                .Returns(() => new Tuple<HttpStatusCode, string, string>(HttpStatusCode.OK, "", ""));

            mockRepo.Setup(x => x.DoesAssetExists(It.IsAny<Guid>()))
                .Returns((Guid uid) => uid == Guid.Parse(DataConstants.ValidGUID) ? true : false);

            mockRepo.Setup(x => x.GetAssetDescendants(It.IsAny<Guid>(), It.IsAny<IEnumerable<KeyValuePair<string, string>>>()))
                .Returns(Task.FromResult(new AssetDescendantsResults()));            

            return mockRepo.Object;
        }

		public IExecutionsRepository GetExecutionsRepository()
		{
			var mockRepo = new Mock<IExecutionsRepository>();
			mockRepo.Setup(x => x.BulkPatchAssetAndRelations(It.IsAny<PatchBulkCatalogRequestModel>())).Returns(Task.FromResult(new ApiExecutionInfo()));

			mockRepo.Setup(x => x.GetExecutionItemByUid(It.IsAny<Guid>()))
				.Returns((Guid uid) => uid == Guid.Parse(DataConstants.ValidGUID)
				? new ApiExecution()
				{
					Fields = "{}"
				} : null);

			mockRepo.Setup(x => x.GetExecutions(It.IsAny<IEnumerable<KeyValuePair<string, string>>>()))
				.Returns(Task.FromResult(new APIExecutionAPIModelResult { total = 1, pageNum = 1, pageSize = 200, StatusCode = HttpStatusCode.OK }));

			mockRepo.Setup(x => x.GetExecutionStatus(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>()))
				.Returns((Guid uid, bool includeResults, bool includeProcessingDetail) => uid == Guid.Parse(DataConstants.ValidGUID) ?
			   Task.FromResult(new EndpointPayloadResponse<dynamic>
			   {
				   Code = HttpStatusCode.OK,
				   Message = "",
				   Payload = new {
					   Total = 1,
					   Processed = 1,
					   Error = 0,
					   Fields = new { },
					   StartedOn = DateTime.Now,
					   CompletedOn = DateTime.Now,
					   Results = new List<DatabaseBulkAssetResult>()
				   }
			   })
			   : Task.FromResult(new EndpointPayloadResponse<dynamic> { Code = HttpStatusCode.NotFound, Message = "Not found", Payload = null }));

			mockRepo.Setup(x => x.PatchCatalog(It.IsAny<int>(), It.IsAny<PatchBulkCatalogRequestModel>())).Returns(Task.CompletedTask);

			return mockRepo.Object;
		}

		public IFieldsRepository GetFieldsRepository()
        {
            var mockRepo = new Mock<IFieldsRepository>();
            mockRepo.Setup(x => x.GetFieldTypes(It.IsAny<IEnumerable<KeyValuePair<string, string>>>()))
                            .Returns(Task.FromResult(new Tuple<FieldTypesApiViewModel, WorkHttpStatus>(new FieldTypesApiViewModel(), new WorkHttpStatus(HttpStatusCode.OK, "", ""))));

            mockRepo.Setup(x => x.UpdateFields(It.IsAny<FieldTypesApiEditModel>(), It.IsAny<TypeIdentifierInfoModel>()))
                .Returns(new WorkHttpStatus(HttpStatusCode.OK, "", ""));


            mockRepo.Setup(x => x.GetFieldTypes(It.IsAny<TypeIdentifierInfoModel>()))
                .Returns(new List<FieldType>());

            mockRepo.Setup(x => x.GetCustomFields(It.IsAny<SystemObjects>(), It.IsAny<int>()))
                .Returns(new List<string>());

            return mockRepo.Object;
        }

		public IEnumerable<ICatalog> GetCatalogs()
		{
			var mock = new Mock<ICatalog>();

			mock.SetupGet(p => p.Platform).Returns(Platform.Azure);

			mock.Setup(x =>
				x.ConsolidateTagsAsync(It.IsAny<Guid>(), It.IsAny<List<Guid>>())
			).Returns(Task.FromResult(new RepositoryResponse<IEnumerable<TagApiModel>>(new List<TagApiModel>(), 200, true, "")));

			mock.Setup(x =>
				x.CreateAssetTagAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>())
			).Returns(Task.FromResult(new RepositoryResponse<bool>(true, 200, true, "")));

			mock.Setup(x =>
				x.CreateTagAsync(It.IsAny<string>(), It.IsAny<Guid?>())
			).Returns(Task.FromResult(new RepositoryResponse<TagApiModel>(new TagApiModel(), 200, true, "")));

			mock.Setup(x =>
				x.CreateTagTypeAsync(It.IsAny<string>())
			).Returns(Task.FromResult(new RepositoryResponse<TagTypeApiModel>(new TagTypeApiModel(), 200, true, "")));

			mock.Setup(x =>
				x.ReadAssetPaths(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>())
			).Returns(Task.FromResult(new AssetPathResults { items = new List<AssetPathResult> { new AssetPathResult { path = "" } }, total = 1 }));

			mock.Setup(x =>
				x.ReadAncestryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())
			).Returns(Task.FromResult(new List<AssetType> { new AssetType { Name = "test" } }));

			mock.Setup(x =>
				x.ReadTagAsync(It.IsAny<Guid>())
			).Returns(Task.FromResult(new RepositoryResponse<TagApiModel>(new TagApiModel(), 200, true, "")));

			mock.Setup(x =>
				x.ReadTagsAsync(It.IsAny<IEnumerable<KeyValuePair<string, string>>>())
			).Returns(Task.FromResult(
				new RepositoryResponse<PagedApiBaseViewModel<TagApiModel>>(
					new PagedApiBaseViewModel<TagApiModel> { 
						items = new List<TagApiModel> { new TagApiModel { Value = "Test" } }, 
						pageNum = 1, 
						pageSize = 25, 
						total = 1 
					}, 200, true, "")
				)
			);

			mock.Setup(x =>
				x.ReadTagTypeAsync(It.IsAny<Guid>())
			).Returns(Task.FromResult(new RepositoryResponse<TagTypeApiModel>(new TagTypeApiModel(), 200, true, "")));

			mock.Setup(x =>
				x.ReadTagTypesAsync()
			).Returns(Task.FromResult(new List<TagTypeApiModel>().AsEnumerable()));

			mock.Setup(x =>
				x.RemoveAssetTagAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>())
			).Returns(Task.FromResult(new RepositoryResponse<bool>(true, 200, true, "")));

			mock.Setup(x =>
				x.RemoveTagsAsync(It.IsAny<List<Guid>>())
			).Returns(Task.FromResult(new RepositoryResponse<bool>(true, 200, true, "")));

			mock.Setup(x =>
				x.RemoveTagTypesAsync(It.IsAny<List<Guid>>())
			).Returns(Task.FromResult(new RepositoryResponse<bool>(true, 200, true, "")));

			mock.Setup(x =>
				x.UpdateTagAsync(It.IsAny<Guid>(), It.IsAny<string>())
			).Returns(Task.FromResult(new RepositoryResponse<bool>(true, 200, true, "")));

			mock.Setup(x =>
				x.UpdateTagTypeAsync(It.IsAny<Guid>(), It.IsAny<string>())
			).Returns(Task.FromResult(new RepositoryResponse<bool>(true, 200, true, "")));

			return new List<ICatalog> { mock.Object };
		}

		public ICommentRepository GetCommentRepository()
        {
            var mock = new Mock<ICommentRepository>();

            return mock.Object;
        }

        public IWorkflowRepository GetWorkflowRepository()
        {
            var mock = new Mock<IWorkflowRepository>();

            mock.Setup(x => x.GetWorkflowTypes(It.IsAny<IEnumerable<KeyValuePair<string, string>>>()))
                .Returns(Task.FromResult(new List<WorkflowTypeApiViewModel>() { new WorkflowTypeApiViewModel(), new WorkflowTypeApiViewModel() } as IEnumerable<WorkflowTypeApiViewModel>));

            mock.Setup(x => x.GetWorkflowVersionSteps(It.IsAny<Guid>()))
                .Returns((Guid guid) =>
                {
                    var result = new List<WorkflowVersionStepsApiViewModel>() as IEnumerable<WorkflowVersionStepsApiViewModel>;
                    if (guid == Guid.Parse(DataConstants.ValidGUID))
                        return Task.FromResult(result);
                    else
                        return Task.FromResult<IEnumerable<WorkflowVersionStepsApiViewModel>>(null);
                });

            mock.Setup(x => x.GetWorkflowVersions(It.IsAny<IEnumerable<KeyValuePair<string, string>>>()))
                .Returns(Task.FromResult(new WorkflowVersionsApiViewModel()));

            mock.Setup(x => x.GetWorkflowTypeByUID(It.IsAny<Guid>()))
                .Returns(new d360.core.entities.Workflow.Type());

            mock.Setup(x => x.GetWorkflowVersionByUID(It.IsAny<Guid>()))
                 .Returns((Guid uid) => uid == Guid.Parse(DataConstants.ValidGUID) ? new WorkflowVersion() : null);

            mock.Setup(x => x.GetWorkflowItemByUID(It.IsAny<Guid>()))
           .Returns(new WorkflowItem());

            mock.Setup(x => x.GetWorkflows(It.IsAny<IEnumerable<KeyValuePair<string, string>>>()))
                 .Returns(Task.FromResult(new WorkflowsApiViewModel()));

            return mock.Object;
        }

        public IWorkflow GetIssueRepository()
        {
            var mock = new Mock<IWorkflow>();
            mock.Setup(x => x.GetIssueTypeByUIDAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new IssueType());

            mock.Setup(x => x.GetAllocationByAssetTypeAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult(new List<IssueTypeApiModel>() { new IssueTypeApiModel(), new IssueTypeApiModel() } as IEnumerable<IssueTypeApiModel>));


            mock.Setup(x => x.GetIssueByUIDAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid uid) => uid == Guid.Parse(DataConstants.ValidGUID) ? new Issue() : null);

            return mock.Object;
        }
		
        public IRelationshipRepository GetRelationshipRepository()
        {
            var mock = new Mock<IRelationshipRepository>();
            mock.Setup(x => x.GetRelationshipTypeByUID(It.IsAny<Guid>()))
                .Returns(new IntersectType());

            mock.Setup(x => x.GetRelationshipTypeByUID(It.IsAny<Guid>()))
                .Returns((Guid uid) => uid.ToString() == DataConstants.ValidGUID ? new IntersectType() : null);

            mock.Setup(x => x.AnyExists(It.IsAny<Guid>()))
                .Returns((Guid uid) => uid.ToString() == DataConstants.ValidGUID ? true : false);

            mock.Setup(x => x.AnyPredicateExists(It.IsAny<Guid>()))
                .Returns((Guid uid) => uid.ToString() == DataConstants.ValidGUID ? true : false);

            mock.Setup(x => x.BulkPostRelationships(It.IsAny<Guid>(), It.IsAny<RelationshipInserts>(), It.IsAny<ApiExecution>(), It.IsAny<bool>()))
                .Returns(Task.FromResult(new ApiExecutionInfo() { Action = ApiExecutionAction.PostRelationships, CompanyDomainPrefix = "", CompanyID = -1, ExecutionID = Guid.NewGuid(), ResourceID = 56 }));

            mock.Setup(x => x.GetBulkResults(It.IsAny<ApiExecutionInfo>()))
                .Returns(Task.FromResult(new List<DatabaseBulkAssetResult>()));

            mock.Setup(x => x.GetExportModel(It.IsAny<int>()))
                .Returns(DataConstants.GetExcelModel());

            mock.Setup(x => x.GetExportModelWithCustomFields(It.IsAny<int>(), It.IsAny<IEnumerable<string>>()))
                .Returns(DataConstants.GetExcelModel());

            mock.Setup(x => x.GetIntersectTypeById(It.IsAny<int>()))
                .Returns(new List<IntersectType>() { new IntersectType(), new IntersectType() }.AsQueryable());

            mock.Setup(x => x.GetIntersectTypeByUid(It.IsAny<Guid>()))
                .Returns((Guid uid) => uid.ToString() == DataConstants.ValidGUID ? new IntersectType() : null);

            mock.Setup(x => x.GetPredicates(It.IsAny<Guid?>(), It.IsAny<PredicateType?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>()))
                .Returns((Guid? PredicateUid, PredicateType? Type, string Name, string Inverse, bool? IsUsed) =>
                {
                    var predicates = DataConstants.GetPredicates();
                    if (PredicateUid.HasValue)
                    {
                        predicates = predicates.Where(i => i.Uid == PredicateUid.Value);
                    }

                    if (Type.HasValue)
                    {
                        predicates = predicates.Where(i => i.Type == Type.Value);
                    }

                    if (!string.IsNullOrEmpty(Name) && !string.IsNullOrWhiteSpace(Name))
                    {
                        Name = Name.Trim().ToLower();
                        predicates = predicates.Where(i => i.Name.ToLower() == Name);
                    }

                    if (!string.IsNullOrEmpty(Inverse) && !string.IsNullOrWhiteSpace(Inverse))
                    {
                        Inverse = Inverse.Trim().ToLower();
                        predicates = predicates.Where(i => i.Inverse.ToLower() == Inverse);
                    }

                    if (IsUsed.HasValue)
                    {
                        predicates = predicates.Where(x => x.IsInUse == IsUsed);
                    }
                    return Task.FromResult(predicates);
                });

            mock.Setup(x => x.GetRelationshipByUID(It.IsAny<Guid>()))
                .Returns((Guid uid) => uid.ToString() == DataConstants.ValidGUID ? new Intersect() : null);

            mock.Setup(x => x.GetRelationships(It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<string>(), false, new CancellationToken()))
				.Returns(Task.FromResult(JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(new GetRelationshipsApiModel() { items = new List<GetRelationshipApiModel>() { new GetRelationshipApiModel(), new GetRelationshipApiModel() } }))));

            mock.Setup(x => x.GetRelationshipTypes(It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<string>()))
                .Returns(Task.FromResult(new List<IntersectTypeApiViewModel>() { new IntersectTypeApiViewModel(), new IntersectTypeApiViewModel() }));

            mock.Setup(x => x.DeleteRelationships(It.IsAny<ApiExecution>(), It.IsAny<IntersectType>(), It.IsAny<RelationshipDeletes>(), 3600))
                .Returns(new List<DatabaseBulkRelationshipResult>());

            return mock.Object;
        }

        public ITagRepository GetTagRepository()
        {
            var mock = new Mock<ITagRepository>();

            mock.Setup(x => x.DoesTagExists(It.IsAny<string>(), It.IsAny<Guid?>()))
                .Returns((string s, Guid? uid) => s == DataConstants.Tags.ValidName ? false : true);

            mock.Setup(x => x.GetTagByUid(It.IsAny<Guid>()))
                .Returns((Guid uid) => uid.ToString() == DataConstants.ValidGUID ? new Tag() : null);

            mock.Setup(x => x.IsAuthorizedToEditTag(It.IsAny<Guid>()))
                .Returns(true);

            mock.Setup(x => x.DoesAssetTagExists(It.IsAny<int>(), It.IsAny<long>()))
                .Returns(true);
            
			mock.Setup(x => x.GetAssetTag(It.IsAny<int>(), It.IsAny<long>()))
            .Returns(new AssetTag() { UID = Guid.Parse(DataConstants.ValidGUID) });

            mock.Setup(x => x.DoesTagExists(It.IsAny<Guid>()))
                .Returns(true);

            return mock.Object;
        }

        public IThemeRepository GetThemeRepository()
        {
            var mockRepo = new Mock<IThemeRepository>();
            return mockRepo.Object;
        }

		public IResourceSettingRepository GetResourceSettingRepository()
		{
			var mockRepo = new Mock<IResourceSettingRepository>();

			mockRepo.Setup(x => x.GetSettings(It.IsAny<int>(), It.IsAny<Guid>()))
				.ReturnsAsync(new Dictionary<string, string>());

			mockRepo.Setup(x => x.UpsertSetting(It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()));

			mockRepo.Setup(x => x.UpsertGlobalSetting(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()));

			return mockRepo.Object;
		}

		public IDashboardRepository GetDashboardRepository()
		{
			var mockRepo = new Mock<IDashboardRepository>();

			return mockRepo.Object;
		}

		public IAuditRepository GetAuditRepository()
		{
			var mockRepo = new Mock<IAuditRepository>();

			return mockRepo.Object;
		}

		public IMetricsRepository GetMetricsRepository()
        {
            var mock = new Mock<IMetricsRepository>();
            mock.Setup(x => x.AddOrUpdateMetrics(It.IsAny<MetricAssetEditModel>()))
                .Returns(new WorkHttpStatus(HttpStatusCode.OK, "", ""));

            mock.Setup(x => x.DeleteMetric(It.IsAny<MetricAsset>()));

            mock.Setup(x => x.GetActiveMetric(It.IsAny<Guid>()))
                .Returns((Guid uid) => uid == Guid.Parse(DataConstants.ValidGUID) ? new MetricAsset() : null);

            mock.Setup(x => x.GetMetricViewModelByUid(It.IsAny<Guid>(), null))
                .Returns((Guid uid, DateTime? effectiveDate) => uid == Guid.Parse(DataConstants.ValidGUID) ? new MetricAssetViewDetailModel() : null);

            mock.Setup(x => x.GetMetricByUid(It.IsAny<Guid>()))
                .Returns((Guid uid) => uid == Guid.Parse(DataConstants.ValidGUID) ? new MetricAsset() : null);

            mock.Setup(x => x.GetMetricDefinitionHierarchyByAssetType(It.IsAny<Guid>(), It.IsAny<DateTime?>()))
                .Returns(new MetricAssetTypeHierarchyModels() { new MetricAssetTypeHierarchyModel(), new MetricAssetTypeHierarchyModel() });

            mock.Setup(x => x.GetMetricHierarchyByAsset(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),true))
                .Returns(new List<RootMetricAssetHierarchyModel>());

            mock.Setup(x => x.GetMetricStructureByAllocation(It.IsAny<Guid>(), It.IsAny<List<State>>()))
                .Returns(
                new List<MetricAssetViewModel>() {
                    new MetricAssetViewModel { Uid = Guid.Empty, Name = "Name" }
                });

            return mock.Object;
        }

        public Mock<IResponsibilityRepository> GetResponsibilityRepository()
        {
            var mock = new Mock<IResponsibilityRepository>();

            mock.Setup(x => x.GetResponsibilities(It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new AssetResponsibilitiesApiModel() { items = new List<AssetResponsibilityItemModel>() { new AssetResponsibilityItemModel(), new AssetResponsibilityItemModel() } });

            mock.Setup(x => x.GetResponsibilityRules(It.IsAny<Guid>()))
                .ReturnsAsync(new List<ResponsibilityTypeRuleViewModel>() { new ResponsibilityTypeRuleViewModel(), new ResponsibilityTypeRuleViewModel() }.AsEnumerable());

            mock.Setup(x => x.GetResponsibilityRuleStats(It.IsAny<Guid>()))
                .ReturnsAsync(new ResponsibilityTypeRuleStatsViewModel());

            mock.Setup(x => x.GetResponsibilityTypeAllocations(It.IsAny<Guid>()))
                .ReturnsAsync(new List<ResponsibilityTypeAllocationViewModel>() { new ResponsibilityTypeAllocationViewModel(), new ResponsibilityTypeAllocationViewModel() }.AsEnumerable());

            mock.Setup(x => x.GetResponsibilityTypeAllocationsByAsset(It.IsAny<Guid>()))
                .ReturnsAsync(new List<ResponsibilityTypeAllocationViewModel>() { new ResponsibilityTypeAllocationViewModel(), new ResponsibilityTypeAllocationViewModel() }.AsEnumerable());

            mock.Setup(x => x.GetResponsibilityTypes())
                .ReturnsAsync(new List<ResponsibilityTypeViewModel>() { new ResponsibilityTypeViewModel(), new ResponsibilityTypeViewModel() }.AsEnumerable());

			mock.Setup(x => x.GetResponsibilityTypesByAssetUid(It.IsAny<Guid>()))
				 .ReturnsAsync(new List<ResponsibilityTypeViewModel>() { new ResponsibilityTypeViewModel(), new ResponsibilityTypeViewModel() }.AsEnumerable());

			mock.Setup(x => x.GetResponsibilityType(It.IsAny<Guid>()))
				.ReturnsAsync(new ResponsibilityType());

			mock.Setup(x => x.GetResponsibilityTypeByUID(It.IsAny<Guid>()))
				.Returns((Guid uid) => new ResponsibilityType() { UID = uid });

			mock.Setup(x => x.GetResponsibilityRuleTestResults(It.IsAny<ResponsibilityRuleUpsertModel>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<string>()))
				.ReturnsAsync(new ResponsibilityRuleTestResponseModel());

			mock.Setup(x => x.PostBatchResponsibilityOverride(It.IsAny<List<BulkResponsibilityOverridePostModel>>(), It.IsAny<ApiExecution>()))
				.ReturnsAsync(new ApiExecutionInfo());

			mock.Setup(x => x.UpsertResponsibilityRules(It.IsAny<Guid>(), It.IsAny<List<ResponsibilityRuleUpsertModel>>(), It.IsAny<ApiExecution>()))
				.ReturnsAsync(new List<ResponsibilityRuleUpsertResponseModel>() { new ResponsibilityRuleUpsertResponseModel() });

			mock.Setup(x => x.UpsertResponsibilityTypes(It.IsAny<List<ResponsibilityTypeUpsertModel>>(), It.IsAny<ApiExecution>()))
				.Returns(new List<ResponsibilityTypeUpsertResult>() { new ResponsibilityTypeUpsertResult() });

			mock.Setup(x => x.DeleteAllocation(It.IsAny<ResponsibilityType>(), It.IsAny<AssetType>(), It.IsAny<bool>()))
				.ReturnsAsync((ResponsibilityType responsibilityType, AssetType assetType, bool cascade) =>
					new ResponsibilityTypeAllocationResponseModel() { AssetTypeUid = assetType.uid }
				);

			mock.Setup(x => x.GetResponsibilityTypeUsedInOwnershipLookupMessage(It.IsAny<ResponsibilityType>(), It.IsAny<AssetType>()))
				.Returns(string.Empty);

			mock.Setup(x => x.AddAllocation(It.IsAny<ResponsibilityType>(), It.IsAny<AssetType>(), It.IsAny<List<int>>()))
				.Returns((ResponsibilityType responsibilityType, AssetType assetType, List<int> permissionaBitMask) => 
					new ResponsibilityTypeAllocationResponseModel() { AssetTypeUid = assetType.uid }
				);

			mock.Setup(x => x.IsValidResponsibilityForAsset(It.IsAny<Guid>(), It.IsAny<Guid>()))
				.Returns(true);

			return mock;
		}

        public IWorkspaces GetWorkspacesRepository()
        {
            var mock = new Mock<IWorkspaces>();

            return mock.Object;
        }

        public IScoringRepository GetScoringRepository()
        {
            var mock = new Mock<IScoringRepository>();

            mock.Setup(x => x.DeleteAllocation(It.IsAny<MetricAllocation>()));

            return mock.Object;
        }

        public IWorkflowApiModelValidator GetWorkflowApiModelValidator()
        {
            return new WorkflowApiModelValidator(GetAssetRepository(), GetIssueRepository(), GetRelationshipRepository(), GetWorkflowRepository());
        }

        public IFilterDataProvider GetFilterDataProvider()
        {
            var mock = new Mock<IFilterDataProvider>();

            mock.Setup(x => x.GetDataForRelationshipsParsing(
                It.IsAny<List<Guid>>(),
                It.IsAny<List<Guid>>()))
                .Returns((List<Guid> itList, List<Guid> assetList) =>
                {
                    if (itList.Contains(Guid.Parse(DataConstants.ValidGUID))
                    || assetList.Contains(Guid.Parse(DataConstants.ValidGUID2)))
                    {
                        return
                        (new List<IntersectType> {
                        new IntersectType{ uid= Guid.Parse(DataConstants.ValidGUID) },
                        }, new List<Asset> {
                    new Asset{uid=Guid.Parse(DataConstants.ValidGUID2), AssetType = new AssetType()}
                        }, new List<AssetType>());
                    }

                    return (new List<IntersectType>(), new List<Asset>(), new List<AssetType>());
                });

            mock.Setup(x => x.GetFieldLookupValue(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .Returns((string s, int a, int b, string value) =>
                {
                    return value == "validlookupvalue" ? 2 : 0;
                }
                );

            return mock.Object;
        }

        public IResourceRepository GetResourceRepository()
        {
            var mock = new Mock<IResourceRepository>();

            mock.Setup(x => x.GetResouceByUID(It.IsAny<Guid>()))
                .Returns((Guid uid) =>
                uid == Guid.Parse(DataConstants.ValidGUID) ? new GlobalReportingResource() : null);

            return mock.Object;
        }

        #endregion
    }

}
