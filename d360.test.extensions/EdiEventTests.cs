using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using d360.extensions.events.edi;
using System.Collections.Generic;
using d360.extensions.events.edi;

namespace d360.test.extensions
{
    [TestClass]
    public class EdiEventTests
    {
        [TestMethod]
        public void SynchIsSuccessful()
        {
            var ext = new EventSynchronizationSource();
            var config = new Dictionary<string, object>();
            config.Add("CompanyID", "831978ca-4d6a-4c71-a0b5-c516802cc242");
            config.Add("FilePath", @"C:\Fusion\EDI\20131025.680");
            ext.Synchronize(config);
        }
    }
}
