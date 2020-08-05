using d360.core.queue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace igx.jobs.scoreprocessor.ChangeTypes
{
    public class AssetMeasuresProcess : ProcessBase, IScoreProcess
    {
        public async Task Run()
        {
            await Task.Delay(100);
            //using (var company = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password))
            //{
            //    company.Open();
            //    company.Execute("metrics.LoadFromStaging", commandTimeout: 3600);
            //    lock (log)
            //    {
            //        log.WriteLine("Processed scores for company {0}...", scoreInfo.CompanyID);
            //    }
            //}
        }
    }
}
