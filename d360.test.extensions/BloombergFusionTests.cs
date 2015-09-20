using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace d360.test.extensions
{
    [TestClass]
    public class BloombergFusionTests
    {
        [TestMethod]
        public void BloombergSynchronization_Success()
        {
            var bb = new d360.extensions.fusion.bloomberg.BloombergSchemaSynchronizationSource();

            #region Mimics how scheduling system will send data to this extension

            var configuration = new Dictionary<string, object>();

            configuration.Add("CompanyID", "831978ca-4d6a-4c71-a0b5-c516802cc242"); // DEMO
            configuration.Add("FusionTypeID", 8);
            configuration.Add("ID", 15);
            configuration.Add("FilePath", @"C:\Fusion\Bloomberg\fields.csv");
            configuration.Add("BloombergBackOfficeID", 746);
            configuration.Add("BloombergExtendedBackOfficeID", 1099);
            configuration.Add("BloombergPerSecurityID", 750);

            #endregion

            bb.Synchronize(configuration);
        }
    }
}
