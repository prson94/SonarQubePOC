using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using d360.extensions.fusion.eagle;

namespace d360.extensions.tests
{
    [TestClass]
    public class EagleTests
    {
        [TestMethod]
        public void SynchronizeSuccessful()
        {
            var source = new SchemaSynchronizationSource();
            //source.SynchronizeMock();
            source.Synchronize();
            source = null;
            
        }
    }
}
