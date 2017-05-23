using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using d360.extensions.storage;
using System.IO;

namespace d360.test.web.Tests.Extensions
{
    [TestClass]
    public class AzureMediaServiceTests
    {
        [TestMethod]
        public void TestUpload()
        {
            string file = @"c:\users\mike\desktop\kittens.mp4";
            if (!File.Exists(file))
                Assert.Fail("File not found");
            var provider = new AzureMediaProvider();
            provider.UploadAsset(file);
        }
    }
}
