using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using d360.extensions;

namespace d360.test.web
{
    public class DummySecurityContextProvider: ISecurityContextProvider
    {
        public string CompanyPrefix { get; set; }
        public int CompanyID { get; set; }
        public int ResourceID { get; set; }
        public bool IsAdministrator { get; set; }

    }
}
