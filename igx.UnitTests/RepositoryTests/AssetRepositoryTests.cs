using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.model;
using d360.model.DataAccessLayer;
using Dapper;
using igx.UnitTests.Core;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace igx.UnitTests.RepositoryTests
{
    [Trait("Unit tests", "Asset Repository")]
    public class AssetRepositoryTests : BaseTest
    {

        [Fact]
        public void GetAssetType()
        {
            var mockCompanyContext = new Mock<ICompanyContext>();
            mockCompanyContext.Setup(x => x.CurrentResourceIsAdmin).Returns(true);

            List<string> mustContain = new List<string>();
            mustContain.Add("A.UseAsTransformation=@useAsTransformation");
            mustContain.Add("A.Hierarchical=@hierarchical");
            mustContain.Add("A.AutoDisplayDescription=@autodisplaydescription");
            mustContain.Add("A.AutoDisplayParent=@autoDisplayParent");
            mustContain.Add("A.Object=@obj");
            mustContain.Add("A.ObjectID=@objId");
            mustContain.Add("as HasDashboards");
            mustContain.Add("select Level, Name, Description");
            mustContain.Add("A.uid=@assetTypeUid");

            mockCompanyContext.Setup(x => x.QueryAsync(It.IsAny<string>(),
                It.IsAny<Func<AssetTypeApiViewModel, IconStyleInsert, AssetTypeApiViewModel>>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<int>()))
                .Returns((string sql, Func<AssetTypeApiViewModel, IconStyleInsert, AssetTypeApiViewModel> map, string split, object param, int timeout) =>
                {
                    var ret = new List<AssetTypeApiViewModel>();
                    bool hasMissingSQL = false;
                    foreach (var item in mustContain)
                    {
                        if (!sql.Contains(item))
                        {
                            hasMissingSQL = true;
                        }
                    }

                    //if generated sql is missing some part of query, returning null will fail test
                    if (hasMissingSQL)
                    {
                        return null;
                    }
                    return Task.FromResult(ret as IEnumerable<AssetTypeApiViewModel>);
                }
                );

            var assetRepository = new AssetRepository(mockCompanyContext.Object, GetQueue(), GetStorage(), GetCommunity());

            List<KeyValuePair<string, string>> pars = new List<KeyValuePair<string, string>>();
            pars.Add(new KeyValuePair<string, string>("useastransformation", "true"));
            pars.Add(new KeyValuePair<string, string>("hierarchical", "true"));
            pars.Add(new KeyValuePair<string, string>("autodisplaydescription", "true"));
            pars.Add(new KeyValuePair<string, string>("autodisplayparent", "true"));
            pars.Add(new KeyValuePair<string, string>("includedashboardflag", "true"));
            pars.Add(new KeyValuePair<string, string>("includelevels", "true"));
            pars.Add(new KeyValuePair<string, string>("obj", "Artifact"));
            pars.Add(new KeyValuePair<string, string>("objid", "10"));

            var result = assetRepository.GetAssetType(pars, AssetTypeClass.BusinessAsset, Guid.Parse("cee303f2-9c99-46b4-9ec3-116634049d17"));

            Assert.False(result.Status == TaskStatus.Faulted, "Invalid SQL Generated. Look at mock mockCompanyContext for QueryAsync");

        }
        [Fact]
        public void GetAssetTypeExceptions()
        {
            var mockCompanyContext = new Mock<ICompanyContext>();
            mockCompanyContext.Setup(x => x.CurrentResourceIsAdmin).Returns(true);

            var assetRepository = new AssetRepository(mockCompanyContext.Object, GetQueue(), GetStorage(), GetCommunity());

            List<KeyValuePair<string, string>> pars = new List<KeyValuePair<string, string>>();
            pars.Add(new KeyValuePair<string, string>("useastransformation", "invalid_bool_value"));

            var result = assetRepository.GetAssetType(pars, AssetTypeClass.BusinessAsset, Guid.Parse("cee303f2-9c99-46b4-9ec3-116634049d17"));
            Assert.Contains("Invalid value for parameter [useastransformation]", result.Exception.InnerException.Message);

            pars.Clear();
            pars.Add(new KeyValuePair<string, string>("hierarchical", "invalid_bool_value"));
            result = assetRepository.GetAssetType(pars, AssetTypeClass.BusinessAsset, Guid.Parse("cee303f2-9c99-46b4-9ec3-116634049d17"));
            Assert.Contains("Invalid value for parameter [hierarchical]", result.Exception.InnerException.Message);


            pars.Clear();
            pars.Add(new KeyValuePair<string, string>("autodisplaydescription", "invalid_bool_value"));
            result = assetRepository.GetAssetType(pars, AssetTypeClass.BusinessAsset, Guid.Parse("cee303f2-9c99-46b4-9ec3-116634049d17"));
            Assert.Contains("Invalid value for parameter [autodisplaydescription]".ToLowerInvariant(), result.Exception.InnerException.Message.ToLowerInvariant());

            pars.Clear();
            pars.Add(new KeyValuePair<string, string>("autodisplayparent", "invalid_bool_value"));
            result = assetRepository.GetAssetType(pars, AssetTypeClass.BusinessAsset, Guid.Parse("cee303f2-9c99-46b4-9ec3-116634049d17"));
            Assert.Contains("Invalid value for parameter [autodisplayparent]".ToLowerInvariant(), result.Exception.InnerException.Message.ToLowerInvariant());

            pars.Clear();
            pars.Add(new KeyValuePair<string, string>("includedashboardflag", "invalid_bool_value"));
            result = assetRepository.GetAssetType(pars, AssetTypeClass.BusinessAsset, Guid.Parse("cee303f2-9c99-46b4-9ec3-116634049d17"));
            Assert.Contains("Invalid value for parameter [includedashboardflag]".ToLowerInvariant(), result.Exception.InnerException.Message.ToLowerInvariant());

            pars.Clear();
            pars.Add(new KeyValuePair<string, string>("includelevels", "invalid_bool_value"));
            result = assetRepository.GetAssetType(pars, AssetTypeClass.BusinessAsset, Guid.Parse("cee303f2-9c99-46b4-9ec3-116634049d17"));
            Assert.Contains("Invalid value for parameter [includelevels]".ToLowerInvariant(), result.Exception.InnerException.Message.ToLowerInvariant());
        }

        [Fact]
        public void GetAssets()
        {
            var mockCompanyContext = new Mock<ICompanyContext>();
            mockCompanyContext.Setup(x => x.CurrentResourceIsAdmin).Returns(true);

            mockCompanyContext.Setup(x => x.Query<Guid>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>()))
                .Returns((string sql, object param, int timeout) =>
                {
                    if (sql.ToLowerInvariant().Contains("create table #family"))
                    {
                        var assets = param.GetType().GetProperty("assetUid").GetValue(param, null) as IEnumerable<Guid>;
                        if (assets.FirstOrDefault() == Guid.Parse(DataConstants.ValidGUID2))
                        {
                            return new List<Guid>();
                        }
                        return new List<Guid> { Guid.Parse(DataConstants.ValidGUID2) };
                    }
                    return null;
                });

            mockCompanyContext.Setup(x => x.QueryAsync<UserGetAPIRestrictionModel>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>()))
                .Returns((string sql, object param, int timeout) =>
                {
                    var res = new List<UserGetAPIRestrictionModel>();
                    res.Add(new UserGetAPIRestrictionModel() { HasAssetPermission = true, HasAssetRestriction = true, HasAssetTypeRestriction = true });
                    return Task.FromResult(res as IEnumerable<UserGetAPIRestrictionModel>);
                });

            var fieldTypes = new List<FieldType>();
            fieldTypes.Add(new FieldType { ID = 1, Name = "TextField", FriendlyName = "Text Field", Type = "Text", AssetTypeID = 1, IsListable = true });
            fieldTypes.Add(new FieldType { ID = 2, Name = "Path", FriendlyName = "Path Field", Type = "Path", AssetTypeID = 1, IsListable = true });
            fieldTypes.Add(new FieldType { ID = 3, Name = "OwnershipLookup", FriendlyName = "Ownerhip ", Type = "OwnershipLookup", AssetTypeID = 1, IsListable = true });

            var fieldTypeLookups = new List<FieldTypeLookup>();
            fieldTypeLookups.Add(new FieldTypeLookup { Definition = "{\"DisplayAsList\":true,\"DisplayAssignmentSource\":false,\"ExpandGroupMembership\":true}", FieldTypeID = 3 });

            var fieldTypeMock = CreateDbSetMock<FieldType>(fieldTypes.AsQueryable());
            mockCompanyContext.Setup(x => x.FieldTypes).Returns(fieldTypeMock.Object);

            var fieldTypeLookupMock = CreateDbSetMock<FieldTypeLookup>(fieldTypeLookups.AsQueryable());
            mockCompanyContext.Setup(x => x.FieldTypeLookups).Returns(fieldTypeLookupMock.Object);

            mockCompanyContext.Setup(x => x.TypeHasParent(It.IsAny<SystemObjects>(), It.IsAny<int>(), It.IsAny<PredicateType>()))
                .Returns(true);

            mockCompanyContext.Setup(x => x.Any<Asset>(It.IsAny<Expression<Func<Asset, bool>>>())).Returns(true);

            List<string> mustContain = new List<string>();
            mustContain.Add("create table #PermissiondAssets");
            mustContain.Add("AssetID from #PermissiondAssets");
            mustContain.Add("Profiling.HasProfiling as HasProfiling");
            mustContain.Add("create table #OwnershipLookupAssets");
            mustContain.Add("F3.FormattedValue as [OwnershipLookup]");
            mustContain.Add("from #OwnershipLookupAssets ola3");
            mustContain.Add("A.uid in @assetUids");
            mustContain.Add("AssetDetail Parent on Parent.ID = AAP.ParentAssetID");

            List<string> mustContainWithFilter = new List<string>();
            mustContainWithFilter.Add("F1.FormattedValue like @simpleFilter");
            mustContainWithFilter.Add("Node.DisplayPath like @simpleFilter");
            mustContainWithFilter.Add("rd.SecurityAssetUid in @ownerUids");
            mustContainWithFilter.Add("rd.SecurityAssetUid in @notOwnerUids");
            mustContainWithFilter.Add("HParent.Uid = @parentUid");

            mockCompanyContext
                .Setup(x => x.ExecuteGetAssetsQuery(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<DynamicParameters>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns((string getAllQuery, CancellationToken cancellationToken, DynamicParameters dbArgs, bool includeTotal, bool includeOwnershipData) =>
                {
                    bool hasMissingSQL = false;
                    foreach (var item in mustContain)
                    {
                        if (!getAllQuery.ToLowerInvariant().Contains(item.ToLowerInvariant()))
                        {
                            hasMissingSQL = true;
                        }
                    }

                    if (dbArgs.ParameterNames.Contains("simpleFilter"))
                    {
                        foreach (var item in mustContainWithFilter)
                        {
                            if (!getAllQuery.ToLowerInvariant().Contains(item.ToLowerInvariant()))
                            {
                                hasMissingSQL = true;
                            }
                        }
                    }

                    //if generated sql is missing some part of query, returning null will fail test
                    if (hasMissingSQL)
                    {
                        return null;
                    }

                    var res = new AssetsQueryResults();

                    dynamic asset = new ExpandoObject();
                    asset._rowid = 1;
                    asset.AssetId = 1;
                    //tree grid is getting parents throught recursion call which removes simple filter
                    asset.AssetUid = dbArgs.ParameterNames.Contains("simpleFilter") ? DataConstants.ValidGUID : DataConstants.ValidGUID2;
                    asset.TextField = "Some value";
                    asset.Permissions = "{\"ReadAsset\":true}";
                    asset.Segments = "<path><segment level=\"1\" position=\"1\" assetTypeId=\"2\" assetId=\"3\">asdasdas</segment></path>";
                    res.items = new List<dynamic>() { asset };
                    asset.Remove = new Func<string, bool>((string s) => { return true; });

                    res.total = res.items.Count();

                    dynamic owner = new ExpandoObject();
                    owner.Assets = "1,2,3";
                    owner.OwnershipLookup = "[]";
                    res.ownershipData = new List<dynamic> { owner };

                    return Task.FromResult(res);
                });

            var assetRepository = new AssetRepository(mockCompanyContext.Object, GetQueue(), GetStorage(), GetCommunity());

            var assetType = new AssetType() { ID = 1, Object = SystemObjects.ArtifactType.ToString() };

            List<KeyValuePair<string, string>> pars = new List<KeyValuePair<string, string>>();

            pars.Add(new KeyValuePair<string, string>("_onlylistablefields", "true"));
            pars.Add(new KeyValuePair<string, string>("_includefields", "TextField,Path,OwnershipLookup"));
            pars.Add(new KeyValuePair<string, string>("_listcolorsasjson", "true"));
            pars.Add(new KeyValuePair<string, string>("_includetotal", "true"));
            pars.Add(new KeyValuePair<string, string>("_includecolor", "true"));
            pars.Add(new KeyValuePair<string, string>("_includecreatedmodifiedby", "true"));
            pars.Add(new KeyValuePair<string, string>("_includeownershiplookup", "true"));
            pars.Add(new KeyValuePair<string, string>("_includeprofilingcheck", "true"));
            pars.Add(new KeyValuePair<string, string>("_includeparent", "true"));
            pars.Add(new KeyValuePair<string, string>("usegraphforparent", "true"));
            pars.Add(new KeyValuePair<string, string>("_assetuid", "cee303f2-9c99-46b4-9ec3-116634049d17"));
            pars.Add(new KeyValuePair<string, string>("includesegments", "true"));
            pars.Add(new KeyValuePair<string, string>("_loadpermissiondetails", "true"));
            pars.Add(new KeyValuePair<string, string>("_simplefilter", "test"));
            pars.Add(new KeyValuePair<string, string>("_ownedby", "cee303f2-9c99-46b4-9ec3-116634049d17"));
            pars.Add(new KeyValuePair<string, string>("_notownedby", "cee303f2-9c99-46b4-9ec3-116634049d17"));
            pars.Add(new KeyValuePair<string, string>("_parentuid", "cee303f2-9c99-46b4-9ec3-116634049d17"));
            pars.Add(new KeyValuePair<string, string>("isForTreeGrid", "true"));

            var result = assetRepository.GetAssets(assetType, pars);

            Assert.False(result.Status == TaskStatus.Faulted, "Invalid SQL Generated. Look at mock mockCompanyContext for QueryAsync and mustContain definition");
        }


        [Fact]
        public async void GetAssetsPath()
        {
            var mockCompanyContext = new Mock<ICompanyContext>();
            mockCompanyContext.Setup(x => x.CurrentResourceIsAdmin).Returns(true);

            mockCompanyContext.Setup(x => x.QueryFirstOrDefaultAsync<int>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>()))
                .Returns((string sql, object param, int timeout) =>
                {
                    return Task.FromResult(5);
                });


            mockCompanyContext.Setup(x => x.QueryAsync<AssetPathResult>(It.IsAny<string>(), It.IsAny<DynamicParameters>(), It.IsAny<int>()))
                .Returns((string sql, DynamicParameters param, int timeout) =>
                {
                    var res = new List<AssetPathResult>();
                    var expectedParams = new List<string> { "pagesize", "pagenum", "offset", "assetTypeId" };

                    if (!expectedParams.All(x => param.ParameterNames.Select(y => y.ToLowerInvariant()).Contains(x.ToLowerInvariant())))
                    {
                        return null;
                    }

                    return Task.FromResult(res as IEnumerable<AssetPathResult>);
                });

            List<string> mustContain = new List<string>();



            var assetRepository = new AssetRepository(mockCompanyContext.Object, GetQueue(), GetStorage(), GetCommunity());

            List<KeyValuePair<string, string>> pars = new List<KeyValuePair<string, string>>();
            pars.Add(new KeyValuePair<string, string>("_pagesize", "30"));
            pars.Add(new KeyValuePair<string, string>("_pagenum", "5"));
            pars.Add(new KeyValuePair<string, string>("_includetotal", "true"));

            var assetType = new AssetType { ID = 1, uid = Guid.Parse(DataConstants.ValidGUID) };
            var result = await assetRepository.GetAssetPaths(assetType, pars);
        }

        [Fact]
        public void GetAssetsExcel()
        {
            var mockCompanyContext = new Mock<ICompanyContext>();
            mockCompanyContext.Setup(x => x.CurrentResourceIsAdmin).Returns(true);

            mockCompanyContext.Setup(x => x.Query<Guid>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>()))
                .Returns((string sql, object param, int timeout) =>
                {
                    if (sql.ToLowerInvariant().Contains("create table #family"))
                    {
                        var assets = param.GetType().GetProperty("assetUid").GetValue(param, null) as IEnumerable<Guid>;
                        if (assets.FirstOrDefault() == Guid.Parse(DataConstants.ValidGUID2))
                        {
                            return new List<Guid>();
                        }
                        return new List<Guid> { Guid.Parse(DataConstants.ValidGUID2) };
                    }
                    return null;
                });


            mockCompanyContext.Setup(x => x.QueryAsync<UserGetAPIRestrictionModel>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>()))
                .Returns((string sql, object param, int timeout) =>
                {
                    var res = new List<UserGetAPIRestrictionModel>();
                    res.Add(new UserGetAPIRestrictionModel() { HasAssetPermission = true, HasAssetRestriction = true, HasAssetTypeRestriction = true });
                    return Task.FromResult(res as IEnumerable<UserGetAPIRestrictionModel>);
                });

            var itTypes = new List<IntersectType>();
            var IntersectTypeMock = CreateDbSetMock<IntersectType>(itTypes.AsQueryable());
            mockCompanyContext.Setup(x => x.IntersectTypes).Returns(IntersectTypeMock.Object);

            var assetType = new AssetType() { ID = 1, Object = SystemObjects.ArtifactType.ToString(), uid = Guid.Parse(DataConstants.ValidGUID) };
            var assetTypes = new List<AssetType>();
            assetTypes.Add(assetType);
            var assetTypeMock = CreateDbSetMock<AssetType>(assetTypes.AsQueryable());
            mockCompanyContext.Setup(x => x.AssetTypes).Returns(assetTypeMock.Object);

            var fieldTypes = new List<FieldType>();
            fieldTypes.Add(new FieldType { ID = 1, Name = "TextField", FriendlyName = "Text Field", Type = "Text", AssetTypeID = 1, IsListable = true });
            var fieldTypeMock = CreateDbSetMock<FieldType>(fieldTypes.AsQueryable());
            mockCompanyContext.Setup(x => x.FieldTypes).Returns(fieldTypeMock.Object);

            mockCompanyContext.Setup(x => x.TypeHasParent(It.IsAny<SystemObjects>(), It.IsAny<int>(), It.IsAny<PredicateType>()))
                .Returns(true);

            mockCompanyContext.Setup(x => x.Any<Asset>(It.IsAny<Expression<Func<Asset, bool>>>())).Returns(true);

            mockCompanyContext
                .Setup(x => x.ExecuteGetAssetsQuery(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<DynamicParameters>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns((string getAllQuery, CancellationToken cancellationToken, DynamicParameters dbArgs, bool includeTotal, bool includeOwnershipData) =>
                {

                    var res = new AssetsQueryResults();

                    dynamic asset = new ExpandoObject();
                    asset._rowid = 1;
                    asset.AssetId = 1;
                    asset.AssetUid = DataConstants.ValidGUID;
                    //tree grid is getting parents throught recursion call which removes simple filter
                    asset.TextField = "Some value";
                    res.items = new List<dynamic>() { asset };
                    res.total = res.items.Count();

                    return Task.FromResult(res);
                });

            var assetRepository = new AssetRepository(mockCompanyContext.Object, GetQueue(), GetStorage(), GetCommunity());


            List<KeyValuePair<string, string>> pars = new List<KeyValuePair<string, string>>();
            pars.Add(new KeyValuePair<string, string>("_includeparent", "true"));
            var result = assetRepository.GetAssetsExcel(assetType.uid, pars);

            Assert.False(result.Status == TaskStatus.Faulted, "Invalid SQL Generated. Look at mock mockCompanyContext for QueryAsync and mustContain definition");
        }

        [Fact]
        public async void GetHierarchyExcel()
        {
            var mockCompanyContext = new Mock<ICompanyContext>();
            mockCompanyContext.Setup(x => x.CurrentResourceIsAdmin).Returns(true);

            mockCompanyContext.Setup(x => x.Query<Guid>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>()))
                .Returns((string sql, object param, int timeout) =>
                {
                    if (sql.ToLowerInvariant().Contains("create table #family"))
                    {
                        var assets = param.GetType().GetProperty("assetUid").GetValue(param, null) as IEnumerable<Guid>;
                        if (assets == null || assets.FirstOrDefault() == Guid.Parse(DataConstants.ValidGUID2))
                        {
                            return new List<Guid>();
                        }
                        return new List<Guid> { Guid.Parse(DataConstants.ValidGUID2) };
                    }
                    if (sql.ToLowerInvariant().Contains("From AssetTypeLevel ATL"))
                    {

                    }
                    return null;
                });

            mockCompanyContext.Setup(x => x.Query<dynamic>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>()))
                .Returns((string sql, object param, int timeout) =>
                {
                    if (sql.Contains("From AssetTypeLevel ATL"))
                    {
                        //GetPolicyTypeLevels
                        var res = new List<dynamic>();
                        dynamic item = new ExpandoObject();

                        item.PolicyTypeID = 1;
                        item.Level = 1;
                        item.Name = "Name";
                        item.Description = "Description";

                        res.Add(((dynamic)item));
                        return res;
                    }
                    return null;
                });


            mockCompanyContext.Setup(x => x.QueryAsync<UserGetAPIRestrictionModel>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>()))
                .Returns((string sql, object param, int timeout) =>
                {
                    var res = new List<UserGetAPIRestrictionModel>();
                    res.Add(new UserGetAPIRestrictionModel() { HasAssetPermission = true, HasAssetRestriction = true, HasAssetTypeRestriction = true });
                    return Task.FromResult(res as IEnumerable<UserGetAPIRestrictionModel>);
                });

            var itTypes = new List<IntersectType>();
            var IntersectTypeMock = CreateDbSetMock<IntersectType>(itTypes.AsQueryable());
            mockCompanyContext.Setup(x => x.IntersectTypes).Returns(IntersectTypeMock.Object);

            var assetType = new AssetType() { ID = 1, Object = SystemObjects.Policy.ToString(), Class = AssetTypeClass.Policy, uid = Guid.Parse(DataConstants.ValidGUID) };
            var assetTypes = new List<AssetType>();
            assetTypes.Add(assetType);
            var assetTypeMock = CreateDbSetMock<AssetType>(assetTypes.AsQueryable());
            mockCompanyContext.Setup(x => x.AssetTypes).Returns(assetTypeMock.Object);

            var fieldTypes = new List<FieldType>();
            fieldTypes.Add(new FieldType { ID = 1, Name = "TextField", IsPartOfKey = true, FriendlyName = "Text Field", Type = "Text", AssetTypeID = 1, IsListable = true }); ;
            var fieldTypeMock = CreateDbSetMock<FieldType>(fieldTypes.AsQueryable());
            mockCompanyContext.Setup(x => x.FieldTypes).Returns(fieldTypeMock.Object);

            mockCompanyContext.Setup(x => x.TypeHasParent(It.IsAny<SystemObjects>(), It.IsAny<int>(), It.IsAny<PredicateType>()))
                .Returns(true);

            mockCompanyContext.Setup(x => x.Any<Asset>(It.IsAny<Expression<Func<Asset, bool>>>())).Returns(true);

            mockCompanyContext
                .Setup(x => x.ExecuteGetAssetsQuery(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<DynamicParameters>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns((string getAllQuery, CancellationToken cancellationToken, DynamicParameters dbArgs, bool includeTotal, bool includeOwnershipData) =>
                {

                    var res = new AssetsQueryResults();

                    dynamic asset = new ExpandoObject();
                    asset._rowid = 1;
                    asset.AssetId = 1;
                    asset.AssetUid = Guid.Parse(DataConstants.ValidGUID);
                    asset.ParentUid = Guid.Parse(DataConstants.ValidGUID2);
                    //tree grid is getting parents throught recursion call which removes simple filter
                    asset.TextField = "Some value";

                    dynamic asset2 = new ExpandoObject();
                    asset2._rowid = 2;
                    asset2.AssetId = 2;
                    asset2.ParentUid = null;
                    asset2.AssetUid = Guid.Parse(DataConstants.ValidGUID2);
                    asset2.TextField = "Some value 2";

                    res.items = new List<dynamic>() { asset, asset2 };
                    res.total = res.items.Count();

                    return Task.FromResult(res);
                });

            var assetRepository = new AssetRepository(mockCompanyContext.Object, GetQueue(), GetStorage(), GetCommunity());


            List<KeyValuePair<string, string>> pars = new List<KeyValuePair<string, string>>();
            pars.Add(new KeyValuePair<string, string>("_order", "Field1"));
            pars.Add(new KeyValuePair<string, string>("_simplefilter", "value"));

            var result = await assetRepository.GetHierarchyExcel(assetType.uid, pars);

            Assert.True(result != null && result.GetCells().Count == 3, "Invalid document returned");
        }

        [Fact]
        public async void GetAssetsByPath()
        {
            var mockCompanyContext = new Mock<ICompanyContext>();
            mockCompanyContext.Setup(x => x.CurrentResourceIsAdmin).Returns(true);

            mockCompanyContext.Setup(x => x.QueryAsync<int>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>()))
                      .Returns((string sql, object param, int timeout) =>
                      {
                          var res = new List<int>();
                          res.Add(2);
                          return Task.FromResult(res as IEnumerable<int>);
                      });

            List<string> queryMustContain = new List<string>();

            queryMustContain.Add("inner join IntersectType I on I.Subject"); //filter by subject
            queryMustContain.Add("and P.[Uid] = @puid"); // filter by predicate
            queryMustContain.Add("where T.[Class] = @class"); //class filter
            queryMustContain.Add("T.[Uid] = @uid"); //by uid
            queryMustContain.Add("T.[UseAsTransformation] = @uat"); //filter by is transformation
            queryMustContain.Add("select T.ID from AssetType T"); //is from type filter
            queryMustContain.Add("graph.AssetNode N"); //main select

            mockCompanyContext.Setup(x => x.QueryAsync<AssetsByPathItemApiViewModel>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>()))
                     .Returns((string sql, object param, int timeout) =>
                     {
                         bool hasMissingSQL = false;
                         foreach (var item in queryMustContain)
                         {
                             if (!sql.Contains(item))
                             {
                                 hasMissingSQL = true;
                             }
                         }

                         //if generated sql is missing some part of query, returning null will fail test
                         if (hasMissingSQL)
                         {
                             return null;
                         }


                         var res = new List<AssetsByPathItemApiViewModel>();
                         res.Add(new AssetsByPathItemApiViewModel() { });
                         return Task.FromResult(res as IEnumerable<AssetsByPathItemApiViewModel>);
                     });
            var assetRepository = new AssetRepository(mockCompanyContext.Object, GetQueue(), GetStorage(), GetCommunity());


            List<KeyValuePair<string, string>> pars = new List<KeyValuePair<string, string>>();
            pars.Add(new KeyValuePair<string, string>("_order", "Field1"));
            pars.Add(new KeyValuePair<string, string>("_simplefilter", "value"));
            var request = new AssetsByPathApiRequestModel();

            request.filters = new List<AssetsByPathItemApiFilterRequestModel>
            {
                new AssetsByPathItemApiFilterRequestModel {
                    Class = AssetTypeClass.BusinessAsset,
                    UseAsTransformation = true,
                    Uid = Guid.Parse(DataConstants.ValidGUID),
                    AsSideOfRelationship = new AssetsByPathItemApiFilterSideOfRelationshipRequestModel{
                     PredicateType = PredicateType.InterTypeHierarchy,
                     PredicateUid = Guid.Parse(DataConstants.ValidGUID),
                     Side = AssetsByPathItemApiFilterSideOfRelationshipRequestEnum.Subject
                    }
                }
            };
            request.pageNum = 2;
            request.searchPhrase = "test";
            request.pageSize = 21;

            //if fail take a look on expected query elements in queryMustContain
            var result = await assetRepository.GetAssetsByPath(request);

            Assert.True(result.items.Count() == 1);
            Assert.True(result.total == 2);
            Assert.True(result.pageSize == 21);
            Assert.True(result.pageNum == 2);

        }
    }
}