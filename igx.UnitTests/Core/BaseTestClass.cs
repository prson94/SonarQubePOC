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
                               result.Add(new TypeIdentifierInfoModel() {
                                   Object = type.ToString(),
                                   Uid = uid
                               });
                               return Task.FromResult(result as IEnumerable<TypeIdentifierInfoModel>);
                           }
                       }

                 );
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
            var realRepo = new AssetRepository(GetCompany(), GetQueue(), GetStorage());

            mockRepo.Setup(x => x.GetAssetType(It.IsAny<AssetTypeClass?>()))
                .Returns(
                Task.FromResult<IEnumerable<AssetTypeApiViewModel>>(new List<AssetTypeApiViewModel>() { new AssetTypeApiViewModel() })
            );

            mockRepo.Setup(x => x.GetAssetTypeList()).Returns(AssetTypeClass.Glossary.GetAsList());

            mockRepo.Setup(x => x.GetFieldTypes(It.IsAny<Guid>()))
                .Returns(() => JsonConvert.DeserializeObject<dynamic>(DataConstants.FieldTypesJsonFormat));

            mockRepo.Setup(x => x.GetAssets(It.IsAny<Guid>(), It.IsAny<IEnumerable<KeyValuePair<string, string>>>()))
                .Returns(Task.FromResult(new AssetsApiViewModel()));

            mockRepo.Setup(x => x.GetAssetTypeByUID(It.IsAny<Guid>()))
                .Returns((Guid uid) => uid == Guid.Parse(DataConstants.ValidGUID) || uid == Guid.Parse(DataConstants.ValidGUID2) ? new AssetType() { Object = "ArtifactType", uid = uid } : null);

            mockRepo.Setup(x => x.GetPredicateByUID(It.IsAny<Guid>()))
                .Returns((Guid uid) => uid == Guid.Parse(DataConstants.ValidGUID) ? new Predicate() { UID = uid, Type = PredicateType.InterTypeHierarchy } : null);

            mockRepo.Setup(x => x.PostAssets(It.IsAny<List<AssetInsert>>(), It.IsAny<AssetType>(), It.IsAny<ApiExecution>(), true))
                .Returns((List<AssetInsert> assetInsertList, object o2, object o3, object o4) =>
                 {
                     if (assetInsertList.Count == 0) return null;
                     else return new List<DatabaseBulkAssetResult>() { };
                 }
                );

            mockRepo.Setup(x => x.PutAssets(It.IsAny<List<AssetUpdate>>(), It.IsAny<AssetType>(), It.IsAny<ApiExecution>(), true))
                .Returns((List<AssetUpdate> assetUpdateList, object o2, object o3, object o4) =>
                {
                    if (assetUpdateList.Count == 0) return null;
                    else return new List<DatabaseBulkAssetResult>() { };
                }
                );

            mockRepo.Setup(x => x.DeleteAsset(It.IsAny<AssetDeletes>(), It.IsAny<AssetType>(), It.IsAny<ApiExecution>()))
                .Returns((AssetDeletes assetDeletes, object o2, object o3) =>
                {
                    if (assetDeletes == null) return null;
                    else return new List<DatabaseBulkAssetResult>() { };
                }
                );

            mockRepo.Setup(x => x.PostBulkAssets(It.IsAny<List<AssetInsert>>(), It.IsAny<ApiExecution>()))
               .Returns(Task.FromResult(new ApiExecutionInfo()));

            mockRepo.Setup(x => x.PutBulkAssets(It.IsAny<Guid>(), It.IsAny<List<AssetUpdate>>(), It.IsAny<ApiExecution>()))
               .Returns(Task.FromResult(new ApiExecutionInfo()));

            mockRepo.Setup(x => x.BulkDeleteAssets(It.IsAny<Guid>(), It.IsAny<AssetDeletes>(), It.IsAny<ApiExecution>()))
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
            mockRepo.Setup(x => x.AddAssetType(It.IsAny<AssetTypeInsert>(), It.IsAny<AssetType>(), It.IsAny<AssetType>(), It.IsAny<Predicate>(), out outString, out outBool))
                .Returns(() => new Tuple<HttpStatusCode, string, string>(HttpStatusCode.OK, "", ""));

            mockRepo.Setup(x => x.UpsertObjectStyle(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));

            mockRepo.Setup(x => x.UpdateAssetType(It.IsAny<AssetTypeInsert>(), It.IsAny<AssetType>(), It.IsAny<AssetType>(), It.IsAny<Predicate>()))
                .Returns(() => new Tuple<HttpStatusCode, string, string>(HttpStatusCode.OK, "", ""));


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

            return mockRepo.Object;
        }


        #endregion
    }

}
