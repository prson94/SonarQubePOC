using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using d360.core.entities;
using System.Data.SqlClient;
using d360.core;
using d360.utils.company;
using Dapper;
using d360.workflow;
using System.Activities.Tracking;
using d360.core.enums;
using System.Xml.Linq;

namespace d360.workflow
{
    public sealed class AddWorkflowRecordToCompany : CodeActivity
    {
        public InArgument<int> CompanyID { get; set; }
        public InArgument<XElement> RequestFields { get; set; }
        public InArgument<WorkflowType> WorkflowType { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var companyID = context.GetValue(this.CompanyID);
            var type = context.GetValue(this.WorkflowType);
            var fields = context.GetValue(this.RequestFields);

            using (var cnn = CompanyConnectionUtils.GetCompanyConnection(companyID))
            {
                cnn.Open();

                cnn.Execute(
@"merge Workflow as T
using   (
        select @WorkflowID, @Type, @Fields, @Date
        ) 
as S (ID, WorkflowType, Data, [Date]) 
on (T.ID = S.ID)
when matched then
 update 
 set
  T.DateCompleted = S.[Date]
when not matched then
 insert (ID, WorkflowType, Data, DateStarted, Step)
 values (S.ID, S.WorkflowType, S.Data, S.[Date], 1);",
                    new
                    {
                        WorkflowID = context.WorkflowInstanceId,
                        Type = (short)type,
                        Fields = fields.ToString(),
                        Date = DateTime.UtcNow
                    }
                );

                cnn.Close();
            } 
        }
    }
}
