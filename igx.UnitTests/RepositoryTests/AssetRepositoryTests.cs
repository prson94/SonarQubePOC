using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.model;
using d360.model.DataAccessLayer;
using Moq;
using System;
using System.Collections.Generic;
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


    }
}