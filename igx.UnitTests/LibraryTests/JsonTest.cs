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
    public class JsonTest : BaseTest
    {
        public JsonTest()
        {
        }

        [Fact]
        public void JObject_CanParseFieldJsonProperties()
        {
            //var content = @"{ p1: '123', o1: {c1:'345',c2:'567'}, a1: [{g1:true,g2:'f'},{g1:true,g2:'f'}] }";
            //var content = @"{ p1: '123', o1: {c1:'345',c2:'567'}, a1: [{g1:true,g2:'f'},{g1:true,g2:'f',pp:[{d:true},{d:false}]}] }";
            var content = @"[{ p1: '123', o1: {c1:'345',c2:'567'}, a1: [{g1:true,g2:'f'},{g1:true,g2:'f',pp:[{d:true},{d:false}]}] }]";
            var list = content.ParseJsonIntoJsonPropertiesCollection();
            Assert.True(list.Count > 0);
        }

    }
}
