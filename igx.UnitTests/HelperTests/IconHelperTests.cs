using d360.core.helpers;
using System;
using System.Linq;
using Xunit;

namespace igx.UnitTests.IconHelperTests
{
    [Trait("Unit tests", "Icon Helper Tests")]
    public class IconHelperTests : BaseTest
    {
        public IconHelperTests()
        {

        }

        [Fact]
        public void IconHelperNullText()
        {
            Assert.True(IconHelper.GetIconText(null) == "Tx");
        }

        [Fact]
        public void IconHelperSingleWordText()
        {            
            Assert.True(IconHelper.GetIconText("Banana") == "Ba");
        }

        [Fact]
        public void IconHelperMultiWordLeadingSpaceText()
        {
            Assert.True(IconHelper.GetIconText(" \n Banana Taco Bird") == "Bt");
        }

        [Fact]
        public void IconHelperMultiWordText()
        {
            Assert.True(IconHelper.GetIconText("Banana Turkey") == "Bt");
        }
    }
}