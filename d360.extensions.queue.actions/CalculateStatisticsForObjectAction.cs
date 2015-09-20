using d360.core.entities;
using d360.model;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.extensions.queue.actions
{
    public class CalculateStatisticsForObjectAction: IQueueAction
    {
        #region DI

        CompanyContext Context;

        public CalculateStatisticsForObjectAction(CompanyContext context)
        {
            Context = context;
        }

        #endregion

        public bool ProcessMessage(QueueItem item)
        {
            bool successful = false;

            try
            {
                Context.CalculateStatistics(item.ObjectType, item.ObjectID);
                successful = true;
            }
            catch
            {
                successful = false;
            }

            return successful;
        }
    }
}