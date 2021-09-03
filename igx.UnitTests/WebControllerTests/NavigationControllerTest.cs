using d360.core.entities;
using d360.web.Controllers;
using d360.web.Controllers.V2;
using igx.UnitTests.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Xml.Linq;
using Xunit;

namespace igx.UnitTests.WebControllerTests
{
    [Trait("Unit tests", "Navigation controller")]
    public class NavigationControllerTest : BaseTest
    {

        internal NavigationController navigationController;
        public NavigationControllerTest()
        {
            this.navigationController = new NavigationController(GetCommunity(), GetCompany(), GetStorage(), GetSettingsRepository());
        }

        [Fact]
        public void parseNavigationNullXml()
        {            
            Assert.True(navigationController.parseXmlNavigationDocument(null).Count == 0);
        }

        [Fact]
        public void parseNavigationValidXml()
        {
            XElement xml = XElement.Parse(@"<nav><nav>
                          <name>Suma TA_23rd Jan</name>
                          <url>artifact/100002613</url>
                          <feature>0</feature>
                          <items>
                            <nav>
                              <name>aaaaa</name>
                              <url>artifact/100003081</url>
                              <menuID>Menu_AT100003081</menuID>
                              <feature>0</feature>
                            </nav>
                            <nav>
                              <name>AAAAAAA</name>
                              <url>artifact/100003082</url>
                              <menuID>Menu_AT100003082</menuID>
                              <feature>0</feature>
                            </nav>
                            <nav>
                              <name>zzzz</name>
                              <url>artifact/100003084</url>
                              <menuID>Menu_AT100003084</menuID>
                              <feature>0</feature>
                            </nav>
                          </items>
                        </nav>                        
                        </nav>
                        ");

            var res = navigationController.parseXmlNavigationDocument(xml,false);

            Assert.True(res.Count == 1);

            Assert.True(res[0].Items.Count == 3);
            Assert.True(res[0].Name == "Suma TA_23rd Jan");
            Assert.True(res[0].Url == "artifact/100002613");
            Assert.True(string.IsNullOrEmpty(res[0].MenuID));
            Assert.True(res[0].ShowChildren == false);
            Assert.True(res[0].Items[0].ShowChildren == false);
            Assert.True(res[0].Items[0].Items == null);
            Assert.True(res[0].Items[0].Name == "aaaaa");
            Assert.True(res[0].Items[0].Url == "artifact/100003081");
            Assert.True(string.IsNullOrEmpty(res[0].Items[0].MenuID));
        }


        [Fact] void NoTechAssetsMenu()
        {
            List<TopNavigationItem> nodes = new List<TopNavigationItem>();

            nodes.Add(new TopNavigationItem
            {
                MenuID = "#Technical",
                SortOrder = 3,
                Icon = "fa-book",
                ImageIconUrl = null,
                Title = "Business Assets",
                Items = @"<nav>
                              <name>_3tran</name>
                              <url>artifact/100000172</url>
                              <feature>0</feature>
                            </nav>"
            });

            ;

            Assert.True(navigationController.GenerateSiteMenu(nodes, false, true).Count == 0);
        }

        [Fact]
        void HasTechAssetsMenu()
        {
            List<TopNavigationItem> nodes = new List<TopNavigationItem>();

            nodes.Add(new TopNavigationItem
            {
                MenuID = "#Technical",
                SortOrder = 3,
                Icon = "fa-book",
                ImageIconUrl = null,
                Title = "Business Assets",
                Items = @"<nav>
                              <name>_3tran</name>
                              <url>artifact/100000172</url>
                              <feature>0</feature>
                            </nav>"
            });

            Assert.True(navigationController.GenerateSiteMenu(nodes, true, true).Count == 1);
        }

        [Fact]
        public void GenerateSiteMenu()
        {
            List<TopNavigationItem> nodes = new List<TopNavigationItem>();

            nodes.Add(new TopNavigationItem
            {
                MenuID = "#Business",
                SortOrder = 3,
                Icon = "fa-book",
                ImageIconUrl = null,
                Title = "Business Assets",
                Items = @"<nav>
  <name>_3tran</name>
  <url>artifact/100000172</url>
  <feature>0</feature>
</nav>
<nav>
  <name>_ref test</name>
  <url>artifact/100000211</url>
  <feature>0</feature>
</nav>
<nav>
  <name>A</name>
  <url>artifact/100000318</url>
  <feature>0</feature>
</nav>
<nav>
  <name>AllAny Artifact</name>
  <url>artifact/98</url>
  <feature>0</feature>
</nav>
<nav>
  <name>api 3 test</name>
  <url>artifact/109</url>
  <feature>0</feature>
</nav>
<nav>
  <name>autocomcolortest</name>
  <url>artifact/100000262</url>
  <feature>0</feature>
</nav>
<nav>
  <name>B</name>
  <url>artifact/100000319</url>
  <feature>0</feature>
</nav>
<nav>
  <name>bambam 123</name>
  <url>artifact/133</url>
  <feature>0</feature>
</nav>
<nav>
  <name>banana</name>
  <url>artifact/100000233</url>
  <feature>0</feature>
</nav>
<nav>
  <name>BQ 9390 Asset</name>
  <url>artifact/100000149</url>
  <feature>0</feature>
</nav>
<nav>
  <name>BQ Child asset Type</name>
  <url>artifact/100000257</url>
  <feature>0</feature>
</nav>
<nav>
  <name>BQ test asset Type 12</name>
  <url>artifact/100000227</url>
  <feature>0</feature>
  <items>
    <nav>
      <name>child item</name>
      <url>artifact/100000248</url>
      <menuID>Menu_AT100000248</menuID>
      <feature>0</feature>
    </nav>
  </items>
</nav>
<nav>
  <name>BQ Test Asset type 20</name>
  <url>artifact/100000229</url>
  <feature>0</feature>
</nav>
<nav>
  <name>BQ test BA 1</name>
  <url>artifact/100000256</url>
  <feature>0</feature>
</nav>
<nav>
  <name>BQ test GOV-9390</name>
  <url>artifact/100000148</url>
  <feature>0</feature>
</nav>
<nav>
  <name>BQ Test Type 3</name>
  <url>artifact/100000302</url>
  <feature>0</feature>
</nav>
<nav>
  <name>brian test asset</name>
  <url>artifact/100000135</url>
  <feature>0</feature>
</nav>
<nav>
  <name>bulk load 510</name>
  <url>artifact/100000317</url>
  <feature>0</feature>
</nav>
<nav>
  <name>bulk load test</name>
  <url>artifact/120</url>
  <feature>0</feature>
</nav>
<nav>
  <name>Capital Market</name>
  <url>artifact/57</url>
  <feature>0</feature>
</nav>
<nav>
  <name>Cascade</name>
  <url>artifact/100000261</url>
  <feature>0</feature>
</nav>
<nav>
  <name>Cascading Dropdowns</name>
  <url>artifact/2</url>
  <feature>0</feature>
</nav>
<nav>
  <name>CCCC</name>
  <url>artifact/140</url>
  <feature>0</feature>
</nav>
<nav>
  <name>Color List Test</name>
  <url>artifact/100000264</url>
  <feature>0</feature>
</nav>
<nav>
  <name>Compound Display Value</name>
  <url>artifact/22</url>
  <feature>0</feature>
</nav>
<nav>
  <name>CoolTerms</name>
  <url>artifact/100000292</url>
  <feature>0</feature>
</nav>
<nav>
  <name>Data Mart</name>
  <url>artifact/62</url>
  <feature>0</feature>
</nav>
<nav>
  <name>Datamart</name>
  <url>artifact/69</url>
  <feature>0</feature>
</nav>
<nav>
  <name>Date test</name>
  <url>artifact/100000036</url>
  <feature>0</feature>
</nav>
<nav>
  <name>date time</name>
  <url>artifact/100000273</url>
  <feature>0</feature>
</nav>
<nav>
  <name>Days of the week</name>
  <url>artifact/1</url>
  <feature>0</feature>
</nav>
<nav>
  <name>Default Image</name>
  <url>artifact/100000061</url>
  <feature>0</feature>
</nav>
<nav>
  <name>Default values test</name>
  <url>artifact/26</url>
  <feature>0</feature>
</nav>
<nav>
  <name>defaults</name>
  <url>artifact/123</url>
  <feature>0</feature>
</nav>
<nav>
  <name>DQ Mapped Type</name>
  <url>artifact/100000288</url>
  <feature>0</feature>
</nav>
<nav>
  <name>fast food restraunts</name>
  <url>artifact/107</url>
  <feature>0</feature>
</nav>
<nav>
  <name>GOV-10806 test asset</name>
  <url>artifact/100000228</url>
  <feature>0</feature>
</nav>
<nav>
  <name>GOV-13862-A</name>
  <url>artifact/100000300</url>
  <feature>0</feature>
</nav>
<nav>
  <name>GOV-13862-B</name>
  <url>artifact/100000301</url>
  <feature>0</feature>
</nav>
<nav>
  <name>GOV-7949-Artifact</name>
  <url>artifact/100000012</url>
  <feature>0</feature>
</nav>
<nav>
  <name>GOV-9291 Asset 2</name>
  <url>artifact/100000154</url>
  <feature>0</feature>
</nav>
<nav>
  <name>GOV-9291 New Asset</name>
  <url>artifact/100000153</url>
  <feature>0</feature>
</nav>
<nav>
  <name>GOV-9386 Asset 2</name>
  <url>artifact/100000150</url>
  <feature>0</feature>
</nav>
<nav>
  <name>GOV-9508 Asset 1</name>
  <url>artifact/100000155</url>
  <feature>0</feature>
</nav>
<nav>
  <name>GOV-9585</name>
  <url>artifact/100000156</url>
  <feature>0</feature>
</nav>
<nav>
  <name>GOV-9867</name>
  <url>artifact/100000169</url>
  <feature>0</feature>
</nav>
<nav>
  <name>IconTest2</name>
  <url>artifact/100000123</url>
  <feature>0</feature>
</nav>
<nav>
  <name>Json Asset Testing</name>
  <url>artifact/111</url>
  <feature>0</feature>
</nav>
<nav>
  <name>ken test</name>
  <url>artifact/100000215</url>
  <feature>0</feature>
</nav>
<nav>
  <name>Link test</name>
  <url>artifact/100000037</url>
  <feature>0</feature>
</nav>
<nav>
  <name>list of no color</name>
  <url>artifact/100000265</url>
  <feature>0</feature>
</nav>
<nav>
  <name>Market Data</name>
  <url>artifact/47</url>
  <feature>0</feature>
</nav>
<nav>
  <name>Market Trend</name>
  <url>artifact/60</url>
  <feature>0</feature>
</nav>
<nav>
  <name>MarketDataTest</name>
  <url>artifact/100000040</url>
  <feature>0</feature>
</nav>
<nav>
  <name>MB Business Term</name>
  <url>artifact/121</url>
  <feature>0</feature>
</nav>
<nav>
  <name>MJD API Test</name>
  <url>artifact/100000183</url>
  <feature>0</feature>
</nav>
<nav>
  <name>MJD API Test 3</name>
  <url>artifact/100000166</url>
  <feature>0</feature>
  <items>
    <nav>
      <name>MJD API Test Child</name>
      <url>artifact/100000167</url>
      <menuID>Menu_AT100000167</menuID>
      <feature>0</feature>
    </nav>
  </items>
</nav>
<nav>
  <name>MJD Broken</name>
  <url>artifact/100000310</url>
  <feature>0</feature>
</nav>
<nav>
  <name>MJD Lots of Fields</name>
  <url>artifact/129</url>
  <feature>0</feature>
</nav>
<nav>
  <name>MJD Score Test</name>
  <url>artifact/100000175</url>
  <feature>0</feature>
</nav>
<nav>
  <name>MJD Vendor</name>
  <url>artifact/100000113</url>
  <feature>0</feature>
</nav>
<nav>
  <name>MJP Bulk Asset Load</name>
  <url>artifact/16</url>
  <feature>0</feature>
</nav>
<nav>
  <name>My asset type name</name>
  <url>artifact/100000147</url>
  <feature>0</feature>
</nav>
<nav>
  <name>My asset type name1</name>
  <url>artifact/100000206</url>
  <feature>0</feature>
</nav>
<nav>
  <name>new</name>
  <url>artifact/100000128</url>
  <feature>0</feature>
  <items>
    <nav>
      <name>testnew</name>
      <url>artifact/100000157</url>
      <menuID>Menu_AT100000157</menuID>
      <feature>0</feature>
    </nav>
  </items>
</nav>
<nav>
  <name>NoReadTest</name>
  <url>artifact/55</url>
  <feature>0</feature>
</nav>
<nav>
  <name>Numbers</name>
  <url>artifact/31</url>
  <feature>0</feature>
</nav>
<nav>
  <name>Pappas Term</name>
  <url>artifact/100000279</url>
  <feature>0</feature>
</nav>
<nav>
  <name>PERF2 Load Test API</name>
  <url>artifact/23</url>
  <feature>0</feature>
</nav>
<nav>
  <name>Post for at</name>
  <url>artifact/100000309</url>
  <feature>0</feature>
</nav>
<nav>
  <name>PUT-Delete Fields Test Type</name>
  <url>artifact/61</url>
  <feature>0</feature>
</nav>
<nav>
  <name>relationship lookup</name>
  <url>artifact/100000044</url>
  <feature>0</feature>
</nav>
<nav>
  <name>REQtest</name>
  <url>artifact/100000285</url>
  <feature>0</feature>
</nav>
<nav>
  <name>Reuters</name>
  <url>artifact/58</url>
  <feature>0</feature>
</nav>
<nav>
  <name>shane</name>
  <url>artifact/100000176</url>
  <feature>0</feature>
</nav>
<nav>
  <name>Tag Stuff</name>
  <url>artifact/100000032</url>
  <feature>0</feature>
</nav>
<nav>
  <name>tesetroot</name>
  <url>artifact/100000243</url>
  <feature>0</feature>
</nav>
<nav>
  <name>test</name>
  <url>artifact/100000137</url>
  <feature>0</feature>
</nav>
<nav>
  <name>testsss</name>
  <url>artifact/100000306</url>
  <feature>0</feature>
</nav>
<nav>
  <name>testssssasdfdf</name>
  <url>artifact/100000307</url>
  <feature>0</feature>
</nav>
<nav>
  <name>testssssasdfdfdfgfgf</name>
  <url>artifact/100000308</url>
  <feature>0</feature>
</nav>
<nav>
  <name>two key</name>
  <url>artifact/100000280</url>
  <feature>0</feature>
</nav>
<nav>
  <name>value only</name>
  <url>artifact/100000282</url>
  <feature>0</feature>
</nav>
<nav>
  <name>word wrap</name>
  <url>artifact/100000117</url>
  <feature>0</feature>
</nav>"
            });
            

            navigationController.GenerateSiteMenu(nodes,true,true);

            Assert.True(nodes.Count == 1);
            Assert.True(nodes[0].MenuID == "#Business");
            Assert.True(nodes[0].SortOrder == 3);
            Assert.True(nodes[0].Icon == "fa-book");
            Assert.True(nodes[0].ImageIconUrl == null);
            Assert.True(nodes[0].Title == "Business Assets");
            Assert.True(nodes[0].NavigationItems.Count == 84);
        }

    }
}
