using d360.core;
using d360.core.helpers;
using System.Globalization;
using Xunit;

namespace igx.UnitTests.CoreTests
{
    [Trait("Unit tests", "Plural service helper tests")]
    public class PluralServiceHelperTests : BaseTest
    {

        public PluralServiceHelperTests()
        {

        }

        [Fact]
        public void CheckCurrentCultureIsEnglish()
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US", false);
            Assert.True(PluralCultureHelper.IsNeutralCultureEnglish(), "Current culture is English but plural service doesnt agree.");            
        }

        [Fact]
        public void CheckCurrentCultureIsNonEnglish()
        {
            CultureInfo.CurrentCulture = new CultureInfo("ja-JP", false);
            Assert.False(PluralCultureHelper.IsNeutralCultureEnglish(), "Current culture is Japanese but plural service thinks its English.");
        }
    }
}