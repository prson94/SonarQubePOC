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
    public class UpdateEventTypesByObjectAction: IQueueAction
    {
        #region DI

        CompanyContext Context;

        public UpdateEventTypesByObjectAction(CompanyContext context)
        {
            Context = context;
        }

        #endregion

        public bool ProcessMessage(QueueItem item)
        {
            bool successful = false;

            try
            {
                Context.UpdateEventTypesByObject();
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