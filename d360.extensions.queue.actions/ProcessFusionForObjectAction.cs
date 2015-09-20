using d360.model;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using d360.core.entities;
using d360.core;

namespace d360.extensions.queue.actions
{
    public class ProcessFusionForObjectAction: IQueueAction
    {
        #region DI

        CompanyContext Context;

        public ProcessFusionForObjectAction(CompanyContext context)
        {
            Context = context;
        }

        #endregion

        public bool ProcessMessage(QueueItem item)
        {
            bool successful = false;

            try
            {
                Context.ProcessFusionInQueue(item.ID);
                successful = true;
            }
            catch// (Exception ex)
            {
                successful = false;
            }

            return successful;
        }
    }
}