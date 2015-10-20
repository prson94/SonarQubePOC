using System;
using System.Activities.Tracking;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using d360.utils.company;
using System.Xml.Linq;

namespace d360.workflow
{
    public class WorkflowStatusTrackingParticipant: TrackingParticipant
    {
        protected override void Track(TrackingRecord record, TimeSpan timeout)
        {
            int companyID = 0;
            string activityName = "";
            string activityStatus = "Executing";
            var data = new XElement("fields");
            var date = DateTime.UtcNow;

            //if (record is ActivityStateRecord)
            //{
            //    #region
            //    var asr = record as ActivityStateRecord;
            //    if (asr != null)
            //    {
            //        companyID = (int)asr.Arguments["CompanyID"];
            //        activityName = asr.Activity.Name;
            //        activityStatus = asr.State;
            //        date = asr.EventTime;

            //        if (asr.Variables.Count > 0)
            //        {
            //            foreach (KeyValuePair<string, object> variable in asr.Variables)
            //            {
            //                data.Add(new XElement(variable.Key, variable.Value));
            //            }
            //        }
            //    }
            //    #endregion
            //}
            //else 
                if (record is CustomTrackingRecord)
            {
                #region
                var ctr = record as CustomTrackingRecord;
                if ((ctr != null) && (ctr.Data.Count > 0))
                {
                    if (ctr.Data.ContainsKey("CompanyID"))
                    {
                        companyID = (int)ctr.Data["CompanyID"];
                    }
                    activityName = ctr.Activity.Name;
                    date = ctr.EventTime;

                    foreach (string k in ctr.Data.Keys)
                    {
                        data.Add(new XElement(k, ctr.Data[k]));
                    }
                }
                #endregion
            }
            else if (record is WorkflowInstanceRecord)
            {
                #region
                //var wir = record as WorkflowInstanceRecord;
                //if (wir != null)
                //{
                //    //string.Format(" Workflow InstanceID: {0} Workflow instance state: {1}", record.InstanceId, wir.State);
                //}
                #endregion
            }
            else
            {
                #region
                #endregion
            }

            #region Add to database

            if (companyID > 0 && activityName != "AddWorkflowRecordToCompany")
            {
                using (var cnn = CompanyConnectionUtils.GetCompanyConnection(companyID))
                {
                    cnn.Open();

                    cnn.Execute(
                        @"insert into WorkflowStatus (WorkflowID, TraceLevel, RecordNumber, ActivityName, ActivityState, Data, [Date]) values (@w, @t, @r, @n, @s, @data, @date)",
                        new
                        {
                            w = record.InstanceId,
                            t = record.Level,
                            r = record.RecordNumber,
                            n = activityName,
                            s = activityStatus,
                            data = data.ToString(),
                            date
                        }
                    );

                    cnn.Close();
                }
            }

            #endregion
        }
    }
}
