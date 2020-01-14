using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using d360.model.validators;
using d360.core.entities;

namespace igx.UnitTests.ValidatorTests
{
    [Trait("Unit tests", "TagAPI-Model Validator")]
    public class TagValidatorTest
    {

        [Fact]
        public static void PostTest_ValidMode()
        {
            var model = new TagApiUpsertModel() { Value = "valid length name" };


            //implicit "DoesNotThrow" check
            try
            {
                TagValidator.ValidateForPost(model);
            }
            catch
            {
                Assert.True(false, "Should throw error");
            }
        }


        [Fact]
        public static void PostTest_NullModel()
        {
            Action act = () => TagValidator.ValidateForPost(null);
            var exception = Assert.Throws<Exception>(act);

            Assert.True(exception.Message.Contains("[null model]"), XMsg.BadResponseMessage);

        }

        [Fact]
        public static void PostTest_NullValue()
        {
            Action act = () => TagValidator.ValidateForPost(new TagApiUpsertModel() { });
            var exception = Assert.Throws<Exception>(act);

            Assert.True(exception.Message.Contains("[no value]"), XMsg.BadResponseMessage);

        }

        [Fact]
        public static void PostTest_TooLongValue()
        {
            var model = new TagApiUpsertModel()
            {
                Value = GetRandomString(251)
            };

            Action act = () => TagValidator.ValidateForPost(model);
            var exception = Assert.Throws<Exception>(act);

            Assert.True(exception.Message.Contains("[too long]"), XMsg.BadResponseMessage);

        }

        [Fact]
        public static void PostTest_EdgeValueLength()
        {
            var model = new TagApiUpsertModel()
            {
                Value = GetRandomString(100)
            };
            TagValidator.ValidateForPost(model);

        }
        private static string GetRandomString(int stringLength)
        {
            StringBuilder sb = new StringBuilder();
            int numGuidsToConcat = (((stringLength - 1) / 32) + 1);
            for (int i = 1; i <= numGuidsToConcat; i++)
            {
                sb.Append(Guid.NewGuid().ToString("N"));
            }

            return sb.ToString(0, stringLength);
        }

        [Fact]
        public static void PutTest_ValidMode()
        {
            var model = new TagApiUpsertModel() { Value = "valid length name" };
            var guid = Guid.NewGuid();

            //implicit "DoesNotThrow" check
            try
            {
                TagValidator.ValidateForPut(guid, model);
            }
            catch
            {
                Assert.True(false, "Should throw error");
            }
        }


        [Fact]
        public static void PutTest_NullModel()
        {
            Action act = () => TagValidator.ValidateForPost(null);
            var exception = Assert.Throws<Exception>(act);

            Assert.True(exception.Message.Contains("[null model]"), XMsg.BadResponseMessage);

        }

        [Fact]
        public static void PutTest_NullValue()
        {
            var guid = new Guid();
            Action act = () => TagValidator.ValidateForPut(guid, new TagApiUpsertModel() { });
            var exception = Assert.Throws<Exception>(act);

            Assert.True(exception.Message.Contains("[no value]"), XMsg.BadResponseMessage);

        }

        [Fact]
        public static void PutTest_TooLongValue()
        {
            var model = new TagApiUpsertModel()
            {
                Value = string.Join("", Enumerable.Repeat(0, 251).Select(n => (char)new Random().Next(127)))
            };
            Guid guid = new Guid();

            Action act = () => TagValidator.ValidateForPut(guid, model);
            var exception = Assert.Throws<Exception>(act);

            Assert.True(exception.Message.Contains("[too long]"), XMsg.BadResponseMessage);

        }

        [Fact]
        public static void PutTest_GuidEmpty()
        {
            var model = new TagApiUpsertModel()
            {
                Value = "valid name"
            };
            Guid guid = Guid.Empty;

            Action act = () => TagValidator.ValidateForPut(guid, model);
            var exception = Assert.Throws<Exception>(act);

            Assert.True(exception.Message.Contains("Invalid uid"), XMsg.BadResponseMessage);

        }


        [Fact]
        public static void PutTest_EdgeValueLength()
        {
            var model = new TagApiUpsertModel()
            {
                Value = GetRandomString(100)
            };
            try
            {
                TagValidator.ValidateForPost(model);
            }
            catch
            {
                Assert.True(false, "Should throw error");
            }

        }
    }

}


