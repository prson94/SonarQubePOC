using d360.core.entities;
using d360.model;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace d360.extensions.queue.actions
{
    public class IncrementFieldVersionAction: IQueueAction
    {
        #region DI

        CompanyContext Context;

        public IncrementFieldVersionAction(CompanyContext context)
        {
            Context = context;
        }

        #endregion

        public bool ProcessMessage(QueueItem item)
        {
            bool successful = false;

            try
            {
                if (!string.IsNullOrEmpty(item.Data))
                {
                    var xml = XElement.Parse(item.Data);
                    int fieldTypeID;
                    if (int.TryParse(xml.Element("FieldTypeID").Value, out fieldTypeID))
                    {
                        var value = xml.Element("Value").Value;
                        Context.IncrementFieldVersion(item.ObjectType, item.ObjectID, fieldTypeID, value);
                        successful = true;
                    }
                }
                else
                {
                    successful = true;
                }
            }
            catch// (Exception ex)
            {
                successful = false;
            }

            return successful;
        }
    }
}