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
            var model = new TagApiModel() { Value = "valid length name" };


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
            Action act = () => TagValidator.ValidateForPost(new TagApiModel() { });
            var exception = Assert.Throws<Exception>(act);

            Assert.True(exception.Message.Contains("[no value]"), XMsg.BadResponseMessage);

        }

        [Fact]
        public static void PostTest_TooLongValue()
        {
            var model = new TagApiModel()
            {
                Value = string.Join("", Enumerable.Repeat(0, 251).Select(n => (char)new Random().Next(127)))
            };

            Action act = () => TagValidator.ValidateForPost(model);
            var exception = Assert.Throws<Exception>(act);

            Assert.True(exception.Message.Contains("[too long]"), XMsg.BadResponseMessage);

        }

        [Fact]
        public static void PostTest_EdgeValueLength()
        {
            var model = new TagApiModel()
            {
                Value = string.Join("", Enumerable.Repeat(0, 250).Select(n => (char)new Random().Next(127)))
            };
            TagValidator.ValidateForPost(model);

        }

        [Fact]
        public static void PutTest_ValidMode()
        {
            var model = new TagApiModel() { Value = "valid length name" };
            var guid = Guid.NewGuid();
            model.uid = guid;

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
            Action act = () => TagValidator.ValidateForPut(guid, new TagApiModel() { uid = guid });
            var exception = Assert.Throws<Exception>(act);

            Assert.True(exception.Message.Contains("[no value]"), XMsg.BadResponseMessage);

        }

        [Fact]
        public static void PutTest_TooLongValue()
        {
            var model = new TagApiModel()
            {
                Value = string.Join("", Enumerable.Repeat(0, 251).Select(n => (char)new Random().Next(127)))
            };
            Guid guid = new Guid();
            model.uid = guid;

            Action act = () => TagValidator.ValidateForPut(guid, model);
            var exception = Assert.Throws<Exception>(act);

            Assert.True(exception.Message.Contains("[too long]"), XMsg.BadResponseMessage);

        }

        [Fact]
        public static void PutTest_GuidEmpty()
        {
            var model = new TagApiModel()
            {
                Value = "valid name"
            };
            Guid guid = Guid.Empty;
            model.uid = guid;

            Action act = () => TagValidator.ValidateForPut(guid, model);
            var exception = Assert.Throws<Exception>(act);

            Assert.True(exception.Message.Contains("Invalid uid"), XMsg.BadResponseMessage);

        }

        [Fact]
        public static void PutTest_GuidsDifferent()
        {
            var model = new TagApiModel()
            {
                Value = "valid name"
            };
            Guid guid = Guid.NewGuid();
            model.uid = Guid.NewGuid();

            Action act = () => TagValidator.ValidateForPut(guid, model);
            var exception = Assert.Throws<Exception>(act);

            Assert.True(exception.Message.Contains("uid doesnt match model uid"), XMsg.BadResponseMessage);

        }


        [Fact]
        public static void PutTest_EdgeValueLength()
        {
            var model = new TagApiModel()
            {
                Value = string.Join("", Enumerable.Repeat(0, 250).Select(n => (char)new Random().Next(127)))
            };
            Guid guid = new Guid();
            model.uid = guid;
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


