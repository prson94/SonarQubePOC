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
            TagValidator.ValidateForPost(model);

        }


        [Fact]
        public static void PostTest_NullModel()
        {
            Action act = () => TagValidator.ValidateForPost(null);
            var exception = Assert.Throws<Exception>(act);
            Assert.Contains("[null model]", exception.Message);

        }

        [Fact]
        public static void PostTest_NullValue()
        {
            Action act = () => TagValidator.ValidateForPost(new TagApiModel() { });
            var exception = Assert.Throws<Exception>(act);
            Assert.Contains("[no value]", exception.Message);

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
            Assert.Contains("[too long]", exception.Message);

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
            TagValidator.ValidateForPut(guid, model);

        }


        [Fact]
        public static void PutTest_NullModel()
        {
            Action act = () => TagValidator.ValidateForPost(null);
            var exception = Assert.Throws<Exception>(act);
            Assert.Contains("[null model]", exception.Message);

        }

        [Fact]
        public static void PutTest_NullValue()
        {
            var guid = new Guid();
            Action act = () => TagValidator.ValidateForPut(guid, new TagApiModel() { uid = guid });
            var exception = Assert.Throws<Exception>(act);
            Assert.Contains("[no value]", exception.Message);

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
            Assert.Contains("[too long]", exception.Message);

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
            Assert.Contains("Invalid uid", exception.Message);

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
            Assert.Contains("uid doesnt match model uid", exception.Message);

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
            TagValidator.ValidateForPost(model);

        }
    }

}


