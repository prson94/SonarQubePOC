using d360.web.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.SessionState;
using Xunit;


namespace igx.UnitTests.WebUtilitiesTests
{
    [Trait("Unit tests", "Web Utilities - Internationalization Utilities")]
    public class InternationalizationUtilitiesTest
    {
        [Fact]
        public void EnglishUnitedKingdomTest()
        {
            InternationalizationUtilities.SetUserLocale("en-gb");

            Assert.NotNull(CultureInfo.CurrentCulture);
            Assert.Equal("EN-GB", (CultureInfo.CurrentCulture.Name ?? "").ToUpper());
            Assert.Equal("EN-GB", (CultureInfo.CurrentUICulture.Name ?? "").ToUpper());
        }

        [Fact]
        public void GarbageTest()
        {
            InternationalizationUtilities.SetUserLocale("sdfdsfdsfd");

            Assert.NotNull(CultureInfo.CurrentCulture);
            Assert.NotEqual("sdfdsfdsfd", (CultureInfo.CurrentCulture.Name ?? "").ToLower());
            Assert.NotEqual("sdfdsfdsfd", (CultureInfo.CurrentUICulture.Name ?? "").ToLower());
        }

        [Fact]
        public void DefaultTest()
        {
            InternationalizationUtilities.SetUserLocale();

            Assert.NotNull(CultureInfo.CurrentCulture);
            Assert.True(!string.IsNullOrEmpty(CultureInfo.CurrentUICulture.Name));
        }
    }
}
