using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using d360.extensions.fusion.crd;

namespace d360.extensions.tests
{
    [TestClass]
    public class CRDTests
    {
        [TestMethod]
        public void SynchronizeSuccessful()
        {
            var source = new SchemaSynchronizationSource();
            source.Synchronize();
        }
    }
}
