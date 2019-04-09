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
        public ICompanyContext GetCompany()
        {
            var mock = new Mock<ICompanyContext>();
            
            //setup data 
            SetupAssetTypeData(mock);

            return mock.Object;
        }

        public IStorageProvider GetStorage()
        {
            var mock = new Mock<IStorageProvider>();
            
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

        #endregion

        #region ICompanyContext data setup
        private static void SetupAssetTypeData(Mock<ICompanyContext> mock)
        {
            mock.Setup(x => x.QueryAsync<AssetTypeApiViewModel>(It.IsAny<string>(), null, 90)).ReturnsAsync((string s, object o, int time) =>
            {
                //check the string passed to the QueryAsync funciton and return the appropriate data
                if (Helpers.NormalisedComparer(SqlConstants.SQL_FOR_GETASSETTYPESASYNC, s))
                {
                    return new List<AssetTypeApiViewModel>()
                    {
                        new AssetTypeApiViewModel()
                        {
                            Name = "unit test mock name",
                            Notes = "unit test mock notes",
                            uid = Guid.NewGuid(),
                            Class = new d360.core.enums.AssetTypeClassInfo(){ Name = "info class" },
                            Description = "unit test mock description",
                            Path = "unit test path",
                        }
                    };
                }

                //if the SQL doesent match any then return null
                return null;
            });
        }

        #endregion
    }

}
