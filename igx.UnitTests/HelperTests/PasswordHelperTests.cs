using d360.core.helpers;
using System;
using System.Linq;
using Xunit;

namespace igx.UnitTests.HtmlHelperTests
{
    [Trait("Unit tests", "Password Helper Tests")]
    public class PasswordHelperTests : BaseTest
    {
        public PasswordHelperTests()
        {

        }
                
        [Fact]
        public void RandomPasswordValidations()
        {
            Assert.False(string.IsNullOrEmpty(PasswordHelper.CreateRandomPassword()), "Random password should not be empty or null.");
            Assert.True(PasswordHelper.CreateRandomPassword().Length >= 7, "Random password length should be >= 7 characters");
            Assert.False(PasswordHelper.CreateRandomPassword().Contains('@'), "Random password contains unallowed special characters"); //!#$%
            Assert.False(PasswordHelper.CreateRandomPassword().Contains('^'), "Random password contains unallowed special characters");
            Assert.False(PasswordHelper.CreateRandomPassword().Contains('&'), "Random password contains unallowed special characters");
        }

        [Fact]
        public void HashPasswordValidationExpectedResult()
        {
            var expected = "226B9C794896D7FB064FDD869EDE3C467CA4E285";
            var res = PasswordHelper.HashPassword("data3sixty");

            Assert.True(expected == res, "Password hash doesnt meet expected hash");
        }

        [Fact]
        public void HashPasswordValidationNull()
        {
            Assert.ThrowsAny<ArgumentNullException>(()=>PasswordHelper.HashPassword(null));
        }
    }
}