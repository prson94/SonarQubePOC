using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace d360.jobs.FusionPromotion
{
    public class Functions : FunctionsBase
    {
        [NoAutomaticTrigger]
        public static void CallDatabase()
        {
            ExecuteActionOnAllCompanies("FusionPromotion.CallDatabase", "EXEC utility.PromoteFusionAttributes", 3600);
        }
    }
}
