using Microsoft.Reporting.WebForms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace d360.web.Models
{
    public class ReportServerCredentials : IReportServerCredentials
    {
        public System.Net.ICredentials NetworkCredentials
        {
            get
            {
                return new System.Net.NetworkCredential("d3s-admin", "My!R3p0rts!", "D3S-SSRS");
            }
        }

        public bool GetFormsCredentials(out System.Net.Cookie authCookie, out string userName, out string password, out string authority)
        {
            authCookie = null;
            userName = null;
            password = null;
            authority = null;
            return false;
        }

        public System.Security.Principal.WindowsIdentity ImpersonationUser
        {
            get { return null; }
        }
    }
}