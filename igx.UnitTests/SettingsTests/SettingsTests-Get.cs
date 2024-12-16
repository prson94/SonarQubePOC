using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace igx.UnitTests.SettingsTests
{
    [Trait("Unit tests", "Settings - Get")]
    public class SettingsTestsGet : BaseTest
    {
        [Fact]
        public async Task Settings_CountGreaterThanZero()
        {
            var repo = GetCommunity();
            var settings = await repo.ReadSettingsAsync(-1);
            Assert.True(settings.Count > 0);
        }

        [Fact]
        public async Task Settings_DescriptionsPopulated()
        {
            var repo = GetCommunity();
			var settings = await repo.ReadSettingsAsync(-1);
			Assert.True(!settings.Any(s => string.IsNullOrEmpty(s.Description)));
        }

        [Fact]
        public async Task Settings_NamesPopulated()
        {
            var repo = GetCommunity();
			var settings = await repo.ReadSettingsAsync(-1);
			Assert.True(!settings.Any(s => string.IsNullOrEmpty(s.Name)));
        }
    }
}