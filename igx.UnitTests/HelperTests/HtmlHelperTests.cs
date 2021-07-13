using d360.model;
using d360.model.workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace igx.UnitTests.HtmlHelperTests
{
    [Trait("Unit tests", "Helper Tests - Html Helper")]
    public class HtmlHelperTests : BaseTest
    {        
        public HtmlHelperTests()
        {
            
        }

        [Fact]
        public void BasicRemoveTagsHtml()
        {
            string html = "<div>Test</div>";

            var result = d360.core.helpers.HtmlHelper.RemoveTags(html);

            Assert.Equal("Test", result);
        }

        [Fact]
        public void EmbeddedRemoveTagsHtml()
        {
            string html = "<div>Hi<p>Test</p></div>";

            var result = d360.core.helpers.HtmlHelper.RemoveTags(html);

            Assert.Equal("HiTest", result);
        }

        [Fact]
        public void AttributeRemoveHtml()
        {
            string html = "<span title=\"test\">test</span>";

            var result = d360.core.helpers.HtmlHelper.RemoveTags(html);

            Assert.Equal("test", result);
        }


        [Fact]
        public void MinLenthRemoveHtmlTest()
        {
            string html = "1";

            var result = d360.core.helpers.HtmlHelper.RemoveTags(html);

            Assert.Equal("1", result);
        }

        [Fact]
        public void EmptyStringHtmlTest()
        {            
            var result = d360.core.helpers.HtmlHelper.RemoveTags(string.Empty);

            Assert.Equal(string.Empty, string.Empty);
        }

        [Fact]
        public void NoHtmlCommentOnlyTest()
        {
            Assert.Equal(d360.core.helpers.HtmlHelper.RemoveTags("<!-- invalid invalid -->"), "<!-- invalid invalid -->");            
        }
    }
}