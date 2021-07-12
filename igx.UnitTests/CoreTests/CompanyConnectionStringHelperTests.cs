using d360.core;
using System;
using Xunit;

namespace igx.UnitTests.CoreTests
{
    [Trait("Unit tests", "Company Connection String Helper Test Class - Tests logic for generating connection string")]
    public class CompanyConnectionStringHelperTests : BaseTest
    {

        public CompanyConnectionStringHelperTests()
        {

        }

        [Fact]
        public void CheckBasicConnectionString()
        {
            string conString = CompanyConnectionStringHelper.ConnectionString(5, "server.com","user","password");

            string expectedResult = $"server=server.com;Database=D3S_5;User ID=user;Password=password;MultipleActiveResultSets=True;ConnectRetryCount=10;ConnectRetryInterval=10;Connection Timeout=100;";

            Assert.True(conString == expectedResult, "Connection string generated is not expected value");
        }

        [Fact]
        public void CheckInvalidUser()
        {            
            Assert.Throws<Exception>(() => CompanyConnectionStringHelper.ConnectionString(5, "server.com", string.Empty, "password"));
        }

        [Fact]
        public void CheckInvalidServer()
        {
            Assert.Throws<Exception>(() => CompanyConnectionStringHelper.ConnectionString(5, string.Empty, "user", "password"));
        }
    }
}