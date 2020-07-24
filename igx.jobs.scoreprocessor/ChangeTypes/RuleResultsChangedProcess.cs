using d360.core.queue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace igx.jobs.scoreprocessor.ChangeTypes
{
    public class RuleResultsChangedProcess : ProcessBase, IScoreProcess
    {
        public async Task Run()
        {
            await Task.Delay(100);
        }
    }
}
