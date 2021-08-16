using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using d360.web.Controllers.V2;
using System.Web.Http;
using System.Net.Http;
using d360.core.enums;
using d360.core.entities;
using igx.UnitTests.Core;
using System.Web.Http.Results;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using d360.core;

namespace igx.UnitTests
{
    [Trait("Unit tests", "JsonTest")]
    public class JsonTest : BaseTest
    {
        public JsonTest()
        {
        }

        [Theory]
        [InlineData(@"[{ p1: '123', o1: {c1:'345',c2:'567'}, a1: [{g1:true,g2:'f'},{g1:true,g2:'f',pp:[{d:true},{d:false}]}] }]")]
        [InlineData(@"{ p1: '123', o1: {c1:'345',c2:'567'}, a1: [{g1:true,g2:'f'},{g1:true,g2:'f',pp:[{d:true},{d:false}]}] }")]
        [InlineData(@"{ p1: '123', o1: {c1:'345',c2:'567'}, a1: [{g1:true,g2:'f'},{g1:true,g2:'f'}] }")]
        public void JObject_CanParseFieldJsonProperties(string json)
        {
            var list = json.ParseJsonIntoJsonPropertiesCollection();
            Assert.True(list.Count > 0,"Json not parsed successfully!");
        }


        [Theory]
        [InlineData(33.2,1)]
        [InlineData(1,0)]
        [InlineData(4322.323232, 6)]
        [InlineData(4322.333333, 6)]
        public void JsonNumberDecimal(decimal num, int result)
        {
            int decimals = num.GetNumberOfDecimalPlaces();

            Assert.Equal(decimals, result);
        }
    }
}
