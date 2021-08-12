using System;
using Xunit;
using System.Net.Http;
using System.Text;

namespace igx.UnitTests.ExtensionsTests
{
    [Trait("Unit tests", "HttpRequestMessageExtensionTests - Tests logic and methods for http request extension class")]
    public class HttpRequestMessageExtensionsTests : BaseTest
    {

        public HttpRequestMessageExtensionsTests()
        {

        }

        [Fact]
        public void GetQueryStringsBasic()
        {            
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://www.precisely.com/bananas?sweet=1&color=yellow");
                         
            httpRequestMessage.Content = new StringContent("this will be JSON", Encoding.UTF8, "application/json");
            
            var query = httpRequestMessage.GetQueryStrings();

            Assert.True(query.ContainsKey("sweet"));

            Assert.True(query.ContainsKey("color"));

            Assert.True(query["sweet"] == "1");

            Assert.True(query["color"] == "yellow");
        }

        [Fact]
        public void GetQueryStringsEmpty()
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://www.precisely.com/bananas");

            httpRequestMessage.Content = new StringContent("this will be JSON", Encoding.UTF8, "application/json");

            var query = httpRequestMessage.GetQueryStrings();

            Assert.Empty(query);            
        }

        [Fact]
        public void GetQueryStringBasic()
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://www.precisely.com/bananas?sweet=1&color=yellow&empty");

            httpRequestMessage.Content = new StringContent("this will be JSON", Encoding.UTF8, "application/json");

            Assert.Equal("1", httpRequestMessage.GetQueryString("sweet"));

            Assert.Equal("yellow", httpRequestMessage.GetQueryString("color"));

            Assert.Null(httpRequestMessage.GetQueryString("empty"));
        }

        [Fact]
        public void GetQueryStringNull()
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "");

            httpRequestMessage.Content = new StringContent("this will be JSON", Encoding.UTF8, "application/json");

            Assert.Null(httpRequestMessage.GetQueryString("empty"));
        }
    }
}
