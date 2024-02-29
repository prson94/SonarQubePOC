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
            var repo = GetWorkspacesRepository();
            var settings = await repo.ReadSettingsAsync();
            Assert.True(settings.Count > 0);
        }

        [Fact]
        public async Task Settings_DescriptionsPopulated()
        {
            var repo = GetWorkspacesRepository();
			var settings = await repo.ReadSettingsAsync();
			Assert.True(!settings.Any(s => string.IsNullOrEmpty(s.Description)));
        }

        [Fact]
        public async Task Settings_NamesPopulated()
        {
            var repo = GetWorkspacesRepository();
			var settings = await repo.ReadSettingsAsync();
			Assert.True(!settings.Any(s => string.IsNullOrEmpty(s.Name)));
        }
    }
}