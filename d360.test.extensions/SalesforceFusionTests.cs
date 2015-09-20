using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace d360.test.extensions
{
    [TestClass]
    public class SalesforceFusionTests
    {
        [TestMethod]
        public void SalesforceSynchronization_Success()
        {
            var sf = new d360.extensions.fusion.salesforce.SForceSchemaSynchronizationSource();

            #region Mimics how scheduling system will send data to this extension

            var configuration = new Dictionary<string, object>();

            configuration.Add("CompanyID", "831978ca-4d6a-4c71-a0b5-c516802cc242"); // DEMO
            configuration.Add("FusionTypeID", 10);
            configuration.Add("ID", 16); 
            configuration.Add("Username", "egan.patrick@gmail.com");
            configuration.Add("Password", "ire4land");

            #endregion

            sf.Synchronize(configuration);
        }
    }
}
