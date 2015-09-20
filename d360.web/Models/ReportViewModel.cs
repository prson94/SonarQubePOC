using d360.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace d360.web.Models
{
    public class ReportViewModel
    {
        public int CompanyID { get; set; }

        public SystemObjects Type { get; set; }

        public int ID { get; set; }

        public d360.core.entities.Report Report { get; set; }

    }
}