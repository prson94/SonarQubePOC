using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using d360.model;
using d360.web;
using d360.web.Controllers.V2;
using Xunit;
using Moq;
using d360.extensions;
using System.Data.Entity;
using d360.core.entities;
using System.Text.RegularExpressions;
using igx.UnitTests.Core;
using d360.model.DataAccessLayer;
using System.Linq.Expressions;
using d360.core.enums;
using Newtonsoft.Json;
using d360.core.queue;
using System.Net;
using d360.core.entities.Workflow;
using d360.model.validators;
using d360.core.entities.Metric;
using d360.core;
using System.Dynamic;
using Newtonsoft.Json.Linq;
using System.Threading;
using d360.model.helpers.filters;
using MediatR;
using d360.web.Controllers;
using d360.web.Utilities;
using LaunchDarkly.Sdk.Server;

namespace igx.UnitTests
{
    public class BaseTest
    {
        #region Mock Interfaces
        public ICommunityContext GetCommunity()
        {
            var mock = new Mock<ICommunityContext>();

            return mock.Object;
        }
        public static ICompanyContext GetCompany()
        {
            var mock = new Mock<ICompanyContext>();
            mock.Setup(x => x.CurrentResourceIsAdmin).Returns(true);
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
                               var result = new List<TypeIdentifierInfoModel>();
                               result.Add(new TypeIdentifierInfoModel()
                               {
                                   Object = type.ToString(),
                                   Uid = uid
                               });
                               return Task.FromResult(result as IEnumerable<TypeIdentifierInfoModel>);
                           }
                       }

                 );

            var assetTypes = new List<AssetType> { new AssetType { ID = 1, Name = "unit test" } }.AsQueryable();
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

            var issues = new List<Issue> { new Issue { ID = 1, @Object = "Artifact", ObjectID = 1, ObjectType = "ArtifactType", ObjectTypeID = 1 } }.AsQueryable();
            var issuesMock = CreateDbSetMock<Issue>(issues);
            mock.Setup(x => x.Issues).Returns(issuesMock.Object);

            mock.Setup(x => x.GetSettingValue<int>(Setting.MaxExcelExportRows)).Returns(50000);

            mock.Setup(x => x.GetActiveIntersectTypesByObjectType(It.IsAny<int>(), It.IsAny<SystemObjects>()))
                .Returns(Task.FromResult(new List<IntersectTypeApiViewModel>() { new IntersectTypeApiViewModel(), new IntersectTypeApiViewModel() }));

            mock.Setup(x => x.ImportRelationships(It.IsAny<ApiExecution>(), It.IsAny<IntersectType>(), It.IsAny<RelationshipInserts>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns(new List<DatabaseBulkRelationshipResult>() { new DatabaseBulkRelationshipResult() });

            mock.Setup(x => x.HasAssetTypePermission(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Permission>()))
                .Returns(true);

            mock.Setup(x => x.GetObjectDetail(It.IsAny<string>(), It.IsAny<long>()))
                .Returns(new ObjectDetail() { Name = "ObjectName", Description = "ObjectDescription" });

            IList<Field> fields = new List<Field>
              {
                new Field(){ ObjectID = 1, ObjectType = "ArtifactType", FieldTypeID = 1, FormattedValue = "TestStringValue" },
                new Field(){ ObjectID = 1, ObjectType = "ArtifactType", FieldTypeID = 2, FormattedValue = "12.56" },
                new Field(){ ObjectID = 1, ObjectType = "ArtifactType", FieldTypeID = 3, FormattedValue = "10/10/2019", Value="5/2/2019" },
                new Field(){ ObjectID = 1, ObjectType = "ArtifactType", FieldTypeID = 4, Value = "True", FormattedValue = "True" },
                new Field(){ ObjectID = 1, ObjectType = "ArtifactType", FieldTypeID = 5, Value ="1,2", FormattedValue = "Test1,Test2" },
                new Field(){ ObjectID = 1, ObjectType = "ArtifactType", FieldTypeID = 6 }
              };

            var fieldsMock = CreateDbSetMock(fields);
            mock.Setup(x => x.Fields).Returns(fieldsMock.Object);

            IList<ShoppingCart> shoppingCarts = new List<ShoppingCart>() {
                new ShoppingCart(){ ID=1 }
            };

            var shopCartMock = CreateDbSetMock(shoppingCarts);
            mock.Setup(x => x.ShoppingCarts).Returns(shopCartMock.Object);

            mock.Setup(x => x.GetById<ShoppingCart>(It.IsAny<int>()))
                .Returns((int id) => id > 0 ? new ShoppingCart() { ID = id, RequestedOn = new DateTime(2000, 1, 1) } : null);

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

        public IApplicationUriProvider GetApplicationUriProvider()
        {
            var mock = new Mock<IApplicationUriProvider>();
            return mock.Object;
        }

        public ICoreComponentSet GetCoreComponentSet()
        {
            //var mock = new Mock<CoreComponentSet>(GetCommunity(), GetCompany(), GetSettingsRepository(), new LdClient("sdk-4dbbdcf8-62bd-451b-b78b-8f96b1de2e68"));
            var mock = new Mock<ICoreComponentSet>();
            mock.Setup(s => s.Community).Returns(GetCommunity());
            mock.Setup(s => s.Company).Returns(GetCompany());
            mock.Setup(s => s.Ld).Returns(new LdClient("sdk-4dbbdcf8-62bd-451b-b78b-8f96b1de2e68"));
            mock.Setup(s => s.SettingsRepository).Returns(GetSettingsRepository());
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
            var realRepo = new AssetRepository(GetCompany(), GetQueue(), GetStorage(), GetCommunity());

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

            mockRepo.Setup(x => x.GetAssets(It.IsAny<AssetType>(), It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new AssetsApiViewModel()));

            mockRepo.Setup(x => x.GetAssetTypeByUID(It.IsAny<Guid>()))
                .Returns((Guid uid) => uid == Guid.Parse(DataConstants.ValidGUID) || uid == Guid.Parse(DataConstants.ValidGUID2) ? new AssetType() { Object = "ArtifactType", uid = uid } : null);

            mockRepo.Setup(x => x.GetPredicateByUID(It.IsAny<Guid>()))
                .Returns((Guid uid) => uid == Guid.Parse(DataConstants.ValidGUID) ? new Predicate() { UID = uid, Type = PredicateType.InterTypeHierarchy } : null);

            mockRepo.Setup(x => x.PostAssets(It.IsAny<List<AssetInsert>>(), It.IsAny<AssetType>(), It.IsAny<ApiExecution>(), true, false, false))
                .Returns((List<AssetInsert> assetInsertList, object o2, object o3, object o5, object o6, object o7) =>
                 {
                     if (assetInsertList.Count == 0) return null;
                     else return new List<DatabaseBulkAssetResult>() { };
                 }
                );

            mockRepo.Setup(x => x.PutAssets(It.IsAny<List<AssetUpdate>>(), It.IsAny<AssetType>(), It.IsAny<ApiExecution>(), true, false, false))
                .Returns((List<AssetUpdate> assetUpdateList, object o2, object o3, object o5, object o6, object o7) =>
                {
                    if (assetUpdateList.Count == 0) return null;
                    else return new List<DatabaseBulkAssetResult>() { };
                }
                );

            mockRepo.Setup(x => x.DeleteAsset(It.IsAny<AssetDeletes>(), It.IsAny<AssetType>(), It.IsAny<ApiExecution>(), true))
                .Returns((AssetDeletes assetDeletes, object o2, object o3, object o4) =>
                {
                    if (assetDeletes == null) return null;
                    else return new List<DatabaseBulkAssetResult>() { };
                }
                );

            mockRepo.Setup(x => x.PostBulkAssets(It.IsAny<List<AssetInsert>>(), It.IsAny<ApiExecution>(), It.IsAny<bool>()))
               .Returns(Task.FromResult(new ApiExecutionInfo()));

            mockRepo.Setup(x => x.PutBulkAssets(It.IsAny<Guid>(), It.IsAny<List<AssetUpdate>>(), It.IsAny<ApiExecution>(), It.IsAny<bool>()))
               .Returns(Task.FromResult(new ApiExecutionInfo()));

            mockRepo.Setup(x => x.BulkDeleteAssets(It.IsAny<Guid>(), It.IsAny<AssetDeletes>(), It.IsAny<ApiExecution>(), false, true))
               .Returns(Task.FromResult(new ApiExecutionInfo()));

            mockRepo.Setup(x => x.BulkDeleteAssets(It.IsAny<Guid>(), It.IsAny<AssetDeletes>(), It.IsAny<ApiExecution>(), true, true))
               .Returns(Task.FromResult(new ApiExecutionInfo()));

            mockRepo.Setup(x => x.GetExecutionItemByUid(It.IsAny<Guid>()))
                .Returns((Guid uid) => uid == Guid.Parse(DataConstants.ValidGUID)
                ? new ApiExecution()
                {
                    Fields = "{}"
                }
                : null);
            mockRepo.Setup(x => x.GetExecutionItems(It.IsAny<IEnumerable<KeyValuePair<string, string>>>()))
                .Returns(Task.FromResult(new APIExecutionAPIModelResult { total = 1, pageNum = 1, pageSize = 200, StatusCode = HttpStatusCode.OK }));

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

            mockRepo.Setup(x => x.GetExecutionStatusModel(It.IsAny<Guid>(), It.IsAny<bool>()))
                .Returns((Guid uid, bool includeResults) => uid == Guid.Parse(DataConstants.ValidGUID) ?
               Task.FromResult<dynamic>(new
               {
                   Total = 1,
                   Processed = 1,
                   Error = "",
                   Fields = JObject.Parse("{}"),
                   StartedOn = DateTime.Now,
                   CompletedOn = DateTime.Now,
                   Results = new List<DatabaseBulkAssetResult>()
               })
               : Task.FromResult<dynamic>(null));

            mockRepo.Setup(x => x.GetAssetDescendants(It.IsAny<Guid>(), It.IsAny<IEnumerable<KeyValuePair<string, string>>>()))
                .Returns(Task.FromResult(new AssetDescendantsResults()));            

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

        public ICommentRepository GetCommentRepository()
        {
            var mock = new Mock<ICommentRepository>();

            return mock.Object;
        }

        public ICrossReferencesRepository GetCrossReferencesRepository()
        {
            var mock = new Mock<ICrossReferencesRepository>();

            mock.Setup(x => x.GetCrossReferences(It.IsAny<IEnumerable<KeyValuePair<string, string>>>()))
                .Returns(() => Task.FromResult(new List<AssetCrossReference>() { new AssetCrossReference() { } } as IEnumerable<AssetCrossReference>));

            mock.Setup(x => x.GetByAssetUid(It.IsAny<string>()))
                .Returns(Task.FromResult<IEnumerable<AssetCrossReference>>(new List<AssetCrossReference>() { new AssetCrossReference() }));

            mock.Setup(x => x.GetCrossReferenceByTypeId(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult<IEnumerable<AssetCrossReference>>(new List<AssetCrossReference>() { new AssetCrossReference() }));

            mock.Setup(x => x.GetCrossReferenceByType(It.IsAny<string>()))
                .Returns(Task.FromResult<IEnumerable<AssetCrossReference>>(new List<AssetCrossReference>() { new AssetCrossReference() }));

            mock.Setup(x => x.GetCrossReferenceByDataSource(It.IsAny<string>()))
                .Returns(Task.FromResult<IEnumerable<AssetCrossReference>>(new List<AssetCrossReference>() { new AssetCrossReference() }));

            mock.Setup(x => x.CreateNewCrossReference(It.IsAny<AssetCrossReference>()))
                .Returns(Task.FromResult<int>(1));

            mock.Setup(x => x.XrefExists(It.IsAny<AssetCrossReference>()))
                .Returns((AssetCrossReference xref) => xref.uid == Guid.Parse(DataConstants.InvalidGUID) ? Task.FromResult(true) : Task.FromResult(false));

            mock.Setup(x => x.PostBulkCrossReference(It.IsAny<List<AssetCrossReference>>(), It.IsAny<ApiExecution>()))
                 .Returns((List<AssetCrossReference> xRefList, object o2) =>
                 {
                     if (xRefList.Count == 0) return null;
                     else return new List<AssetCrossReferenceResult>() { };
                 });


            mock.Setup(x => x.PutCrossReference(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AssetCrossReference>())).
                Returns((Guid uid, string s1, string s2, AssetCrossReference xRef) => xRef.uid == Guid.Parse(DataConstants.InvalidGUID) ? Task.FromResult(0) : Task.FromResult(1));

            mock.Setup(x => x.PutCrossReference(It.IsAny<Guid>(), It.IsAny<AssetCrossReference>())).
                Returns((Guid uid, AssetCrossReference xRef) => xRef.uid == Guid.Parse(DataConstants.InvalidGUID) ? Task.FromResult(0) : Task.FromResult(1));

            mock.Setup(x => x.DeleteCrossReferenceByUid(It.IsAny<Guid>()))
                .Returns((Guid guid) => guid == Guid.Parse(DataConstants.InvalidGUID) ? Task.FromResult(0) : Task.FromResult(1));

            mock.Setup(x => x.DeleteCrossReferenceByDataSource(It.IsAny<string>(), It.IsAny<int>()))
              .Returns((string ds, int tout) => ds == DataConstants.ValidDataSource ? Task.FromResult(1) : Task.FromResult(0));

            mock.Setup(x => x.DeleteCrossReferenceByDataSource(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
             .Returns((string ds, string type, int tout) => ds == DataConstants.ValidDataSource ? Task.FromResult(1) : Task.FromResult(0));

            mock.Setup(x => x.DeleteCrossReferenceByType(It.IsAny<string>(), It.IsAny<int>()))
              .Returns((string t, int tout) => t == DataConstants.ValidType ? Task.FromResult(1) : Task.FromResult(0));

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

        public IIssueRepository GetIssueRepository()
        {
            var mock = new Mock<IIssueRepository>();
            mock.Setup(x => x.GetIssueTypeByUID(It.IsAny<Guid>()))
                .Returns(new IssueType());

            mock.Setup(x => x.GetAllocationByAssetType(It.IsAny<Guid>()))
                .Returns(Task.FromResult(new List<IssueTypeApiModel>() { new IssueTypeApiModel(), new IssueTypeApiModel() } as IEnumerable<IssueTypeApiModel>));


            mock.Setup(x => x.GetIssueByUID(It.IsAny<Guid>()))
                .Returns((Guid uid) => uid == Guid.Parse(DataConstants.ValidGUID) ? new Issue() : null);

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

            mock.Setup(x => x.GetActiveIntersectTypesByObjectType(It.IsAny<int>(), It.IsAny<SystemObjects>()))
                .Returns(Task.FromResult(new List<IntersectTypeApiViewModel>()));

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

            mock.Setup(x => x.GetRelationships(It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<string>(), false))
                .Returns(Task.FromResult(JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(new GetRelationshipsApiModel() { items = new List<GetRelationshipApiModel>() { new GetRelationshipApiModel(), new GetRelationshipApiModel() } }))));

            mock.Setup(x => x.GetRelationshipTypes(It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<string>()))
                .Returns(Task.FromResult(new List<IntersectTypeApiViewModel>() { new IntersectTypeApiViewModel(), new IntersectTypeApiViewModel() }));

            mock.Setup(x => x.DeleteRelationships(It.IsAny<ApiExecution>(), It.IsAny<IntersectType>(), It.IsAny<RelationshipDeletes>(), 3600, It.IsAny<bool>()))
                .Returns(new List<DatabaseBulkRelationshipResult>());

            return mock.Object;
        }

        public ITagRepository GetTagRepository()
        {
            var mock = new Mock<ITagRepository>();

            mock.Setup(x => x.CreateTag(It.IsAny<TagApiUpsertModel>()))
                .Returns(new TagApiModel());

            mock.Setup(x => x.DoesTagExists(It.IsAny<string>()))
                .Returns((string s) => s == DataConstants.Tags.ValidName ? false : true);

            mock.Setup(x => x.DeleteTags(It.IsAny<List<TagApiDeleteModel>>()))
                .Returns((List<TagApiDeleteModel> list) => list.Any(x => x.uid.ToString() == DataConstants.InvalidGUID) ? false : true);

            mock.Setup(x => x.GetTagByUid(It.IsAny<Guid>()))
                .Returns((Guid uid) => uid.ToString() == DataConstants.ValidGUID ? new Tag() : null);

            mock.Setup(x => x.IsAuthorizedToEditTag(It.IsAny<Guid>()))
                .Returns(true);

            mock.Setup(x => x.GetTags(It.IsAny<IEnumerable<KeyValuePair<string, string>>>()))
                .Returns(Task.FromResult(new TagApiModelWrapper() { items = new List<TagApiModel>() }));

            mock.Setup(x => x.UpdateTag(It.IsAny<Guid>(), It.IsAny<TagApiUpsertModel>(), It.IsAny<Tag>()))
                .Returns((Guid uid, TagApiUpsertModel tam, Tag tag) => uid == Guid.Parse(DataConstants.ValidGUID) ? new TagApiModel() { Value = tam.Value, uid = uid } : null);
            mock.Setup(x => x.IsAuthorizedToDeleteAssetTag(It.IsAny<int>(), It.IsAny<long>()))
                .Returns(true);
            mock.Setup(x => x.CreateAssetTag(It.IsAny<int>(), It.IsAny<long>()))
                .Returns(new AssetTag());
            mock.Setup(x => x.DoesAssetTagExists(It.IsAny<int>(), It.IsAny<long>()))
                .Returns(true);
            mock.Setup(x => x.GetAssetTag(It.IsAny<int>(), It.IsAny<long>()))
            .Returns(new AssetTag() { UID = Guid.Parse(DataConstants.ValidGUID) });
            mock.Setup(x => x.DeleteAssetTag(It.IsAny<int>(), It.IsAny<long>()))
                .Returns(true);

            mock.Setup(x => x.DoesTagExists(It.IsAny<Guid>()))
                .Returns(true);

            return mock.Object;
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

            mock.Setup(x => x.GetMetricHierarchyByAsset(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime?>()))
                .Returns(new List<RootMetricAssetHierarchyModel>());

            mock.Setup(x => x.GetMetricStructureByAllocation(It.IsAny<Guid>(), It.IsAny<List<State>>()))
                .Returns(
                new List<MetricAssetViewModel>() {
                    new MetricAssetViewModel { Uid = Guid.Empty, Name = "Name" }
                });

            return mock.Object;
        }

        public IResponsibilityRepository GetResponsibilityRepository()
        {
            var mock = new Mock<IResponsibilityRepository>();

            mock.Setup(x => x.GetResponsibilities(It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.FromResult(new AssetResponsibilitiesApiModel() { items = new List<AssetResponsibilityItemModel>() { new AssetResponsibilityItemModel(), new AssetResponsibilityItemModel() } }));

            mock.Setup(x => x.GetResponsibilityRules(It.IsAny<Guid>()))
                .Returns(Task.FromResult(new List<ResponsibilityTypeRuleViewModel>() { new ResponsibilityTypeRuleViewModel(), new ResponsibilityTypeRuleViewModel() }.AsEnumerable()));

            mock.Setup(x => x.GetResponsibilityRuleStats(It.IsAny<Guid>()))
                .Returns(Task.FromResult(new ResponsibilityTypeRuleStatsViewModel()));

            mock.Setup(x => x.GetResponsibilityTypeAllocations(It.IsAny<Guid>()))
                .Returns(Task.FromResult(new List<ResponsibilityTypeAllocationViewModel>() { new ResponsibilityTypeAllocationViewModel(), new ResponsibilityTypeAllocationViewModel() }.AsEnumerable()));

            mock.Setup(x => x.GetResponsibilityTypeAllocationsByAsset(It.IsAny<Guid>()))
                .Returns(Task.FromResult(new List<ResponsibilityTypeAllocationViewModel>() { new ResponsibilityTypeAllocationViewModel(), new ResponsibilityTypeAllocationViewModel() }.AsEnumerable()));

            mock.Setup(x => x.GetResponsibilityTypes())
                .Returns(Task.FromResult(new List<ResponsibilityTypeViewModel>() { new ResponsibilityTypeViewModel(), new ResponsibilityTypeViewModel() }.AsEnumerable()));

            mock.Setup(x => x.GetResponsibilityTypesByAssetUid(It.IsAny<Guid>()))
                .Returns(Task.FromResult(new List<ResponsibilityTypeViewModel>() { new ResponsibilityTypeViewModel(), new ResponsibilityTypeViewModel() }.AsEnumerable()));

            return mock.Object;
        }

        public ISettingsRepository GetSettingsRepository()
        {
            var mock = new Mock<ISettingsRepository>();

            mock.Setup(x => x.GetSetting(Setting.ActionMessage))
                .Returns(Setting.ActionMessage.AsInfoModel());

            mock.Setup(x => x.GetSettingValue<bool>(Setting.DisableCommunityPosting)).Returns(false);

            mock.Setup(x => x.GetSettingsAsDictionary()).Returns(Setting.ActionMessage.GetAsList().ToDictionary(k => k.ID.ToString(), v => v.Value ?? v.DefaultValue));

            mock.Setup(x => x.GetSettings())
                .Returns(Setting.ActionMessage.GetAsList());

            return mock.Object;
        }

        public IMediator GetMediator()
        {
            var mock = new Mock<IMediator>();
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

        public ISurveyRepository GetSurveyRepository()
        {
            var mock = new Mock<ISurveyRepository>();
            mock.Setup(x => x.GetSurveyTypeByUid(It.IsAny<Guid>()))
                .Returns((Guid uid) =>
                uid == Guid.Parse(DataConstants.ValidGUID) ? new SurveyType() : null);

            return mock.Object;
        }

        #endregion
    }

}
