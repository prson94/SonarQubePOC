using d360.model;
using Xunit;

namespace igx.UnitTests.CoreTests
{
    [Trait("Unit tests", "CommunityContext - Tests for the methods in the real class")]
    public class CommunityContextTests : BaseTest
    {
        CommunityContext ctx;

        public CommunityContextTests()
        {
            ctx = new CommunityContext(GetCache(), GetQueue(), GetSecurity());
        }

        [Fact]
        public void CheckOpenIdRequest_Success()
        {
            var value = ctx.GenerateOpenIdRequestValue();
            Assert.True(value.Length == 5);
        }

        [Fact]
        public void CheckOpenIdRequest_ValidCharacters()
        {
            var value = ctx.GenerateOpenIdRequestValue();
            var chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            bool isValid = true;
            for (int i = 0; i < value.Length; i++)
            {
                var singleChar = value[i];
                if (!chars.Contains(singleChar.ToString()))
                {
                    isValid = false;
                }
            }
            Assert.True(isValid);
        }
    }
}