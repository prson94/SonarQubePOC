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


            mock.Setup(x => x.GetActiveIntersectTypesByObjectType(It.IsAny<int>(), It.IsAny<SystemObjects>()))
                .Returns(Task.FromResult(new List<IntersectTypeApiViewModel>() { new IntersectTypeApiViewModel(), new IntersectTypeApiViewModel() }));

            mock.Setup(x => x.ImportRelationships(It.IsAny<ApiExecution>(), It.IsAny<IntersectType>(), It.IsAny<RelationshipInserts>(), It.IsAny<int>(), It.IsAny<bool>()))
                .Returns(new List<DatabaseBulkRelationshipResult>() { new DatabaseBulkRelationshipResult() });

            mock.Setup(x => x.HasAssetTypePermission(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Permission>()))
                .Returns(true);

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
            var realRepo = new AssetRepository(GetCompany(), GetQueue(), GetStorage(),GetCommunity());

            mockRepo.Setup(x => x.GetAssetType(It.IsAny<AssetTypeClass?>(), It.IsAny<Guid?>()))
                .Returns(
                Task.FromResult<IEnumerable<AssetTypeApiViewModel>>(new List<AssetTypeApiViewModel>() { new AssetTypeApiViewModel() })
            );

            mockRepo.Setup(x => x.GetAssetByUID(It.IsAny<Guid>()))
                .Returns((Guid uid) => uid == Guid.Parse(DataConstants.ValidGUID) ? new Asset() : null);

            mockRepo.Setup(x => x.GetAssetTypeList()).Returns(AssetTypeClass.Glossary.GetAsList());

            mockRepo.Setup(x => x.GetFieldTypes(It.IsAny<Guid>()))
                .Returns(() => JsonConvert.DeserializeObject<dynamic>(DataConstants.FieldTypesJsonFormat));

            mockRepo.Setup(x => x.GetAssets(It.IsAny<Guid>(), It.IsAny<IEnumerable<KeyValuePair<string, string>>>()))
                .Returns(Task.FromResult(new AssetsApiViewModel()));

            mockRepo.Setup(x => x.GetAssetTypeByUID(It.IsAny<Guid>()))
                .Returns((Guid uid) => uid == Guid.Parse(DataConstants.ValidGUID) || uid == Guid.Parse(DataConstants.ValidGUID2) ? new AssetType() { Object = "ArtifactType", uid = uid } : null);

            mockRepo.Setup(x => x.GetPredicateByUID(It.IsAny<Guid>()))
                .Returns((Guid uid) => uid == Guid.Parse(DataConstants.ValidGUID) ? new Predicate() { UID = uid, Type = PredicateType.InterTypeHierarchy } : null);

            mockRepo.Setup(x => x.PostAssets(It.IsAny<List<AssetInsert>>(), It.IsAny<AssetType>(), It.IsAny<ApiExecution>(), true,true,false))
                .Returns((List<AssetInsert> assetInsertList, object o2, object o3, object o4, object o5, object o6) =>
                 {
                     if (assetInsertList.Count == 0) return null;
                     else return new List<DatabaseBulkAssetResult>() { };
                 }
                );

            mockRepo.Setup(x => x.PutAssets(It.IsAny<List<AssetUpdate>>(), It.IsAny<AssetType>(), It.IsAny<ApiExecution>(), true,true,false))
                .Returns((List<AssetUpdate> assetUpdateList, object o2, object o3, object o4, object o5, object o6) =>
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

            mockRepo.Setup(x => x.BulkDeleteAssets(It.IsAny<Guid>(), It.IsAny<AssetDeletes>(), It.IsAny<ApiExecution>(),true))
               .Returns(Task.FromResult(new ApiExecutionInfo()));

            mockRepo.Setup(x => x.GetExecutionItemByUid(It.IsAny<Guid>()))
                .Returns((Guid uid) => uid == Guid.Parse(DataConstants.ValidGUID)
                ? new ApiExecution()
                {
                    Fields = "{}"
                }
                : null);

            mockRepo.Setup(x => x.GetAssetTypeByModel(It.IsAny<AssetTypeInsert>()))
                .Returns(new AssetType());

            string outString;
            bool outBool;
            mockRepo.Setup(x => x.AddAssetType(It.IsAny<AssetTypeInsert>(), It.IsAny<AssetType>(), It.IsAny<AssetType>(), It.IsAny<Predicate>(), 0, out outString, out outBool))
                .Returns(() => new Tuple<HttpStatusCode, string, string>(HttpStatusCode.OK, "", ""));

            mockRepo.Setup(x => x.UpsertObjectStyle(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));

            mockRepo.Setup(x => x.UpdateAssetType(It.IsAny<AssetTypeInsert>(), It.IsAny<AssetType>(), It.IsAny<AssetType>(), It.IsAny<Predicate>()))
                .Returns(() => new Tuple<HttpStatusCode, string, string>(HttpStatusCode.OK, "", ""));

            mockRepo.Setup(x => x.DoesAssetExists(It.IsAny<Guid>()))
                .Returns((Guid uid) => uid == Guid.Parse(DataConstants.ValidGUID) ? true : false);

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

            mock.Setup(x => x.PostBulkCrossReference(It.IsAny<List<AssetCrossReference>>()))
                 .Returns((List<AssetCrossReference> xRefList) => xRefList.Any(x => x.uid == Guid.Parse(DataConstants.InvalidGUID)) ? Task.FromResult(false) : Task.FromResult(true));

            mock.Setup(x => x.PutCrossReference(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AssetCrossReference>())).
                Returns((Guid uid, string s1, string s2, AssetCrossReference xRef) => xRef.uid == Guid.Parse(DataConstants.InvalidGUID) ? Task.FromResult(0) : Task.FromResult(1));

            mock.Setup(x => x.PutCrossReference(It.IsAny<Guid>(), It.IsAny<AssetCrossReference>())).
                Returns((Guid uid, AssetCrossReference xRef) => xRef.uid == Guid.Parse(DataConstants.InvalidGUID) ? Task.FromResult(0) : Task.FromResult(1));

            mock.Setup(x => x.DeleteCrossReferenceByUid(It.IsAny<Guid>()))
                .Returns((Guid guid) => guid == Guid.Parse(DataConstants.InvalidGUID) ? Task.FromResult(0) : Task.FromResult(1));

            mock.Setup(x => x.DeleteCrossReferenceByDataSource(It.IsAny<string>()))
              .Returns((string ds) => ds == DataConstants.ValidDataSource ? Task.FromResult(1) : Task.FromResult(0));

            mock.Setup(x => x.DeleteCrossReferenceByDataSource(It.IsAny<string>(), It.IsAny<string>()))
             .Returns((string ds, string type) => ds == DataConstants.ValidDataSource ? Task.FromResult(1) : Task.FromResult(0));

            mock.Setup(x => x.DeleteCrossReferenceByType(It.IsAny<string>()))
              .Returns((string t) => t == DataConstants.ValidType ? Task.FromResult(1) : Task.FromResult(0));

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

            mock.Setup(x => x.BulkPostRelationships(It.IsAny<Guid>(), It.IsAny<RelationshipInserts>(), It.IsAny<Func<int, object, int, int, ApiExecution>>(), It.IsAny<bool>()))
                .Returns(Task.FromResult(new ApiExecutionInfo() { Action = ApiExecutionAction.PostRelationships, CompanyDomainPrefix = "", CompanyID = -1, ExecutionID = Guid.NewGuid(), ResourceID = 56 }));

            mock.Setup(x => x.GetActiveIntersectTypesByObjectType(It.IsAny<int>(), It.IsAny<SystemObjects>()))
                .Returns(Task.FromResult(new List<IntersectTypeApiViewModel>()));

            mock.Setup(x => x.GetBulkResults(It.IsAny<ApiExecutionInfo>()))
                .Returns(new List<DatabaseBulkAssetResult>());
            mock.Setup(x => x.GetExportModel(It.IsAny<int>()))
                .Returns(DataConstants.GetExcelModel());

            mock.Setup(x => x.GetExportModelWithCustomFields(It.IsAny<int>(), It.IsAny<IEnumerable<string>>()))
                .Returns(DataConstants.GetExcelModel());

            mock.Setup(x => x.GetIntersectTypeById(It.IsAny<int>()))
                .Returns(new List<IntersectType>() { new IntersectType(), new IntersectType() }.AsQueryable());

            mock.Setup(x => x.GetIntersectTypeByUid(It.IsAny<Guid>()))
                .Returns((Guid uid) => uid.ToString() == DataConstants.ValidGUID ? new IntersectType() : null);

            mock.Setup(x => x.GetPredicates())
                .Returns(Task.FromResult(DataConstants.GetPredicates()));

            mock.Setup(x => x.GetRelationshipByUID(It.IsAny<Guid>()))
                .Returns((Guid uid) => uid.ToString() == DataConstants.ValidGUID ? new Intersect() : null);

            mock.Setup(x => x.GetRelationships(It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<string>()))
                .Returns(Task.FromResult(JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(new GetRelationshipsApiModel() { items = new List<GetRelationshipApiModel>() { new GetRelationshipApiModel(), new GetRelationshipApiModel() } }))));

            mock.Setup(x => x.GetRelationshipTypes(It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<string>()))
                .Returns(Task.FromResult(new List<IntersectTypeApiViewModel>() { new IntersectTypeApiViewModel(), new IntersectTypeApiViewModel() }));

            mock.Setup(x => x.DeleteRelationships(It.IsAny<IntersectType>(), It.IsAny<RelationshipDeletes>(), It.IsAny<bool>()))
                .Returns(Task.FromResult(new RelationshipDeleteResult(HttpStatusCode.OK, "", "", new List<RelationshipDeleteApiStatus>() { new RelationshipDeleteApiStatus() })));

            return mock.Object;
        }

        public ITagRepository GetTagRepository()
        {
            var mock = new Mock<ITagRepository>();

            mock.Setup(x => x.CreateTag(It.IsAny<TagApiModel>()))
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

            mock.Setup(x => x.UpdateTag(It.IsAny<Guid>(), It.IsAny<TagApiModel>(), It.IsAny<Tag>()))
                .Returns((Guid uid, TagApiModel tam, Tag tag) => (uid == tam.uid && uid == Guid.Parse(DataConstants.ValidGUID)) ? tam : null);
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

            return mock.Object;
        }

        public IMetricsRepository GetMetricsRepository()
        {
            var mock = new Mock<IMetricsRepository>();
            bool outBool;
            mock.Setup(x => x.AddOrUpdateMetrics(It.IsAny<MetricAssetViewModel>(), out outBool))
                .Returns(new WorkHttpStatus(HttpStatusCode.OK, "", ""));

            mock.Setup(x => x.BulkMetricsImport(It.IsAny<BulkMetricsImport>(), It.IsAny<ApiExecution>()))
                .Returns(new List<BulkMetricTemporaryTableModel>() { new BulkMetricTemporaryTableModel() });

            mock.Setup(x => x.DeleteMetric(It.IsAny<MetricAsset>()));

            mock.Setup(x => x.GetActiveMetric(It.IsAny<Guid>()))
                .Returns((Guid uid) => uid == Guid.Parse(DataConstants.ValidGUID) ? new MetricAsset() : null);

            mock.Setup(x => x.GetMetricByUid(It.IsAny<Guid>()))
                .Returns((Guid uid) => uid == Guid.Parse(DataConstants.ValidGUID) ? new MetricAsset() : null);

            mock.Setup(x => x.GetMetricDefinitionHierarchyByAssetType(It.IsAny<Guid>(), It.IsAny<DateTime?>()))
                .Returns(new MetricAssetTypeHierarchyModels() { new MetricAssetTypeHierarchyModel(), new MetricAssetTypeHierarchyModel() });

            mock.Setup(x => x.GetMetricFieldFragments(It.IsAny<Guid>()))
                .Returns(new List<string>()
                {
                    @"[{""ID"":420,""Name"":""Name"",""Type"":""Text""},{""ID"":421,""Name"":""AssetDate"",""Type"":""Date""}]"
                });

            mock.Setup(x => x.GetMetricHierarchyByAsset(It.IsAny<Guid>(), It.IsAny<DateTime?>()))
                .Returns(new MetricAssetHierarchyModels());

            mock.Setup(x => x.GetMetricStructureFragments(It.IsAny<Guid>()))
                .Returns(new List<string>() {
                    @"[{""ID"":420,""Name"":""Name"",""Type"":""Text""},{""ID"":421,""Name"":""AssetDate"",""Type"":""Date""}]"
                });

            return mock.Object;
        }

        public IResponsibilityRepository GetResponsibilityRepository()
        {
            var mock = new Mock<IResponsibilityRepository>();

            mock.Setup(x => x.GetResponsibilities(It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.FromResult(new AssetResponsibilitiesApiModel() { items = new List<AssetResponsibilityItemModel>() { new AssetResponsibilityItemModel(), new AssetResponsibilityItemModel() } }));

            mock.Setup(x => x.GetResponsibilityRules(It.IsAny<Guid>()))
                .Returns(Task.FromResult(new List<ResponsibilityTypeRuleViewModel>() { new ResponsibilityTypeRuleViewModel(), new ResponsibilityTypeRuleViewModel() }.AsEnumerable()));

            mock.Setup(x => x.GetResponsibilityRuleStats(It.IsAny<Guid>()))
                .Returns(Task.FromResult(new ResponsibilityTypeRuleStatsViewModel()));

            mock.Setup(x => x.GetResponsibilityTypeAllocations(It.IsAny<Guid>()))
                .Returns(Task.FromResult(new List<ResponsibilityTypeAllocationViewModel>() { new ResponsibilityTypeAllocationViewModel(), new ResponsibilityTypeAllocationViewModel() }.AsEnumerable()));

            mock.Setup(x => x.GetResponsibilityTypes())
                .Returns(Task.FromResult(new List<ResponsibilityTypeViewModel>() { new ResponsibilityTypeViewModel(), new ResponsibilityTypeViewModel() }.AsEnumerable()));

            mock.Setup(x => x.GetResponsibilityTypesByAssetUid(It.IsAny<Guid>()))
                .Returns(Task.FromResult(new List<ResponsibilityTypeViewModel>() { new ResponsibilityTypeViewModel(), new ResponsibilityTypeViewModel() }.AsEnumerable()));

            return mock.Object;
        }

        public IWorkflowApiModelValidator GetWorkflowApiModelValidator()
        {
            return new WorkflowApiModelValidator(GetAssetRepository(), GetIssueRepository(), GetRelationshipRepository(), GetWorkflowRepository());
        }
        #endregion
    }

}
