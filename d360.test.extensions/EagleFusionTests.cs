using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace d360.test.extensions
{
    [TestClass]
    public class EagleFusionTests
    {
        [TestMethod]
        public void BloombergSynchronization_Success()
        {
            var egl = new d360.extensions.fusion.eagle.EagleBloombergSchemaSynchronizationSource();

            #region Mimics how scheduling system will send data to this extension

            var configuration = new Dictionary<string, object>();

            configuration.Add("CompanyID", "831978CA-4D6A-4C71-A0B5-C516802CC242"); // ACI 
            //configuration.Add("CompanyID", "5C2A9509-3085-45D1-92AA-5EA6CC772C4D"); //ATC
            configuration.Add("FusionTypeID", 4);
            configuration.Add("ID", 11);
            configuration.Add("Username", "jshortis");
            configuration.Add("Password", "eagle1");

            #endregion

            egl.Synchronize(configuration);
        }

        [TestMethod]
        public void EagleSynchronization_Success()
        {
            var egl = new d360.extensions.fusion.eagle.EagleSchemaSynchronizationSource();

            #region Mimics how scheduling system will send data to this extension

            var configuration = new Dictionary<string, object>();

            configuration.Add("CompanyID", "831978CA-4D6A-4C71-A0B5-C516802CC242");
            configuration.Add("FusionTypeID", 5);
            configuration.Add("ID", 13);
            configuration.Add("username", "jshortis");
            configuration.Add("password", "eagle1");

            #endregion

            egl.Synchronize(configuration);
        }

        [TestMethod]
        public void SqlServerSynchronization_Success()
        {
            var sql = new d360.extensions.fusion.mssql.SqlServerSchemaSynchronizationSource();

            #region Mimics how scheduling system will send data to this extension

            var configuration = new Dictionary<string, object>();

            //configuration.Add("CompanyID", "2E73A982-8B70-43B1-B476-37E3BE949B38");     // ATC
            configuration.Add("CompanyID", "831978CA-4D6A-4C71-A0B5-C516802CC242");  // ACI 
            //configuration.Add("CompanyID", "5C2A9509-3085-45D1-92AA-5EA6CC772C4D");  // TEST
            configuration.Add("FusionTypeID", 1);
            configuration.Add("ID", 3);//1
            configuration.Add("ConnectionString", "Server=tcp:a4xhimxzit.database.windows.net;Database=system-c93058b1-504b-4142-a4c0-e2a336018f77;User ID=d3s-user;Password=d43S!ioOui!#@#;Trusted_Connection=False;Encrypt=True;");
            //configuration.Add("ConnectionString", "Data Source=sdmaSQL12.AMERICANTOWER.COM;Initial Catalog=ATProduction;Integrated Security=True;");//"Server=tcp:a4xhimxzit.database.windows.net;Database=system-c93058b1-504b-4142-a4c0-e2a336018f77;User ID=d3s-user;Password=d43S!ioOui!#@#;Trusted_Connection=False;Encrypt=True;");

            #endregion

            sql.Synchronize(configuration);
        }

        [TestMethod]
        public void OracleSynchronization_Success()
        {
            var sql = new d360.extensions.fusion.oracle.OracleSchemaSynchronizationSource();

            #region Mimics how scheduling system will send data to this extension

            var configuration = new Dictionary<string, object>();

            configuration.Add("CompanyID", "2E73A982-8B70-43B1-B476-37E3BE949B38");     // ATC
            //configuration.Add("CompanyID", "831978CA-4D6A-4C71-A0B5-C516802CC242");  // ACI 
            //configuration.Add("CompanyID", "5C2A9509-3085-45D1-92AA-5EA6CC772C4D");  // TEST
            configuration.Add("FusionTypeID", 3);
            configuration.Add("ID", 2);//1
            configuration.Add("ConnectionString", "Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=oradevracn2.americantower.com)(PORT=1562)))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=FMWQC)));User Id=xxgam;Password=xxgam01;");
            configuration.Add("schema", "XXGAM");
            //configuration.Add("ConnectionString", "Data Source=sdmaSQL12.AMERICANTOWER.COM;Initial Catalog=ATProduction;Integrated Security=True;");//"Server=tcp:a4xhimxzit.database.windows.net;Database=system-c93058b1-504b-4142-a4c0-e2a336018f77;User ID=d3s-user;Password=d43S!ioOui!#@#;Trusted_Connection=False;Encrypt=True;");

            #endregion

            sql.Synchronize(configuration);
        }
    }
}
