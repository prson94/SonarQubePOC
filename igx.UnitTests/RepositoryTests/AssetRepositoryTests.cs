using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.model;
using d360.model.DataAccessLayer;
using Dapper;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
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

            mockCompanyContext.Setup(x => x.QueryAsync<UserGetAPIRestrictionModel>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>()))
                .Returns((string sql, object param, int timeout) =>
                {
                    var res = new List<UserGetAPIRestrictionModel>();
                    res.Add(new UserGetAPIRestrictionModel() { HasAssetPermission = true, HasAssetRestriction = true, HasAssetTypeRestriction = true });
                    return Task.FromResult(res as IEnumerable<UserGetAPIRestrictionModel>);
                });

            var fieldTypes = new List<FieldType> { new FieldType { ID = 1, Name = "TextField", FriendlyName = "Text Field", Type = "Text", AssetTypeID = 1 } }.AsQueryable();
            var fieldTypeMock = CreateDbSetMock<FieldType>(fieldTypes);
            mockCompanyContext.Setup(x => x.FieldTypes).Returns(fieldTypeMock.Object);


            var assetRepository = new AssetRepository(mockCompanyContext.Object, GetQueue(), GetStorage(), GetCommunity());

            var assetType = new AssetType() { ID = 1 };

            List<KeyValuePair<string, string>> pars = new List<KeyValuePair<string, string>>();
            var result = assetRepository.GetAssets(assetType, pars);
        }
    }
}