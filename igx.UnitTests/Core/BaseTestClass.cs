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

            mockRepo.Setup(x=> x.GetPredicateByUID(It.IsAny<Guid>()))
                .Returns((Guid uid) => uid == Guid.Parse(DataConstants.ValidGUID) ? new Predicate() { UID = uid, Type = PredicateType.InterTypeHierarchy } :null);

            mockRepo.Setup(x => x.PostAssets(It.IsAny<List<AssetInsert>>(), It.IsAny<AssetType>(), It.IsAny<ApiExecution>(), It.IsAny<bool>()))
                .Returns((List<AssetInsert> assetInsertList, object o2, object o3, object o4) =>
                 {
                     if (assetInsertList.Count == 0) return null;
                     else return new List<DatabaseBulkAssetResult>() { };
                 }
                );

            mockRepo.Setup(x => x.PutAssets(It.IsAny<List<AssetUpdate>>(), It.IsAny<AssetType>(), It.IsAny<ApiExecution>(), true))
                .Returns((List<AssetUpdate> assetUpdateList, object o2, object o3, object o4tdu) =>
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
        #endregion
    }

}
