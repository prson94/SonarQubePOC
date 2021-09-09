using System.Linq;
using Xunit;

namespace igx.UnitTests.SettingsTests
{
    [Trait("Unit tests", "Settings - Get")]
    public class SettingsTestsGet : BaseTest
    {
        [Fact]
        public void Settings_CountGreaterThanZero()
        {
            var repo = GetSettingsRepository();
            var settings = repo.GetSettings();
            Assert.True(settings.Count > 0);
        }

        [Fact]
        public void Settings_DescriptionsPopulated()
        {
            var repo = GetSettingsRepository();
            var settings = repo.GetSettings();
            Assert.True(!settings.Any(s => string.IsNullOrEmpty(s.Description)));
        }

        [Fact]
        public void Settings_NamesPopulated()
        {
            var repo = GetSettingsRepository();
            var settings = repo.GetSettings();
            Assert.True(!settings.Any(s => string.IsNullOrEmpty(s.Name)));
        }
    }
}