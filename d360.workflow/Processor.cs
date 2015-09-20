using d360.core;
using System;
using System.Activities;
using System.Activities.DurableInstancing;
using System.Activities.Tracking;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using System.Threading;

namespace d360.workflow
{
    public class Processor
    {
        SqlWorkflowInstanceStore store;
        AutoResetEvent syncEvent;

        public Processor()
        {
            syncEvent = new AutoResetEvent(false);
            store = new SqlWorkflowInstanceStore(constants.WORKFLOW_DATABASE_CONNECTION);
            WorkflowApplication.CreateDefaultInstanceOwner(store, null, WorkflowIdentityFilter.Any);
        }

        public void ConfigureWorkflowApplication(WorkflowApplication wfApp)
        {
            // Configure the persistence store.
            wfApp.InstanceStore = store;

            #region Extensions to Workflow Application

            var all = "*";
            WorkflowStatusTrackingParticipant stp = new WorkflowStatusTrackingParticipant
            {
                TrackingProfile = new TrackingProfile
                {
                    Queries = {
                        new CustomTrackingQuery 
                        {
                            Name = all,
                            ActivityName = all
                        }//,
                        //new ActivityStateQuery()
                        //{
                        //    // Subscribe for track records from all activities for all states
                        //    ActivityName = all,
                        //    States = { all },

                        //    // Extract workflow variables and arguments as a part of the activity tracking record
                        //    // VariableName = "*" allows for extraction of all variables in the scope
                        //    // of the activity
                        //    Variables = 
                        //    {                                
                        //        { all }   
                        //    }
                        //}    
                        //new ActivityStateQuery
                            //{
                            //    ActivityName = "WriteLine",
                            //    States = { ActivityStates.Executing },
                            //    Arguments = { "Text" }
                            //}
                        }
                }
            };
            wfApp.Extensions.Add(stp);

            #endregion

            #region Event Handlers for Workflow Application

            wfApp.Completed = delegate(WorkflowApplicationCompletedEventArgs e)
            {
                if (e.CompletionState == ActivityInstanceState.Faulted)
                {
                    Trace.TraceError(
                        "Workflow Terminated. Exception: {0}\r\n{1}",
                        e.TerminationException.GetType().FullName,
                        e.TerminationException.Message
                    );
                }
                else if (e.CompletionState == ActivityInstanceState.Canceled)
                {
                    Trace.TraceInformation("Workflow canceled");
                }
                else
                {
                    //int Turns = Convert.ToInt32(e.Outputs["Turns"]);
                    Trace.TraceInformation("Completed");
                }
                syncEvent.Set();
            };

            wfApp.Aborted = delegate(WorkflowApplicationAbortedEventArgs e)
            {
                Trace.TraceWarning(
                    "Workflow Aborted. Exception: {0}\r\n{1}",
                    e.Reason.GetType().FullName,
                    e.Reason.Message
                );
            };

            wfApp.OnUnhandledException = delegate(WorkflowApplicationUnhandledExceptionEventArgs e)
            {
                Trace.TraceInformation(
                    "Unhandled Exception: {0}\r\n{1}",
                    e.UnhandledException.GetType().FullName,
                    e.UnhandledException.Message
                );
                return UnhandledExceptionAction.Terminate;
            };

            wfApp.PersistableIdle = delegate(WorkflowApplicationIdleEventArgs e)
            {
                // Send the current WriteLine outputs to the status window.
                var writers = e.GetInstanceExtensions<StringWriter>();
                foreach (var writer in writers)
                {
                    Trace.TraceInformation(writer.ToString());
                }
                return PersistableIdleAction.Unload;
            };
            
            wfApp.Unloaded = delegate(WorkflowApplicationEventArgs e) 
            {
                syncEvent.Set();
            };

            #endregion
        }

        public Guid CreateNewWorkflowInstance(WorkflowIdentity identity, Dictionary<string, object> input)
        {
            syncEvent = new AutoResetEvent(false);

            var workflow = WorkflowVersionMap.GetWorkflowDefinition(identity);
            var app = new WorkflowApplication(workflow, input, identity);
            ConfigureWorkflowApplication(app);
           
            app.Run();
            syncEvent.WaitOne();
            return app.Id;
        }

        public List<Guid> GetPersistedWorkflows()
        {
            List<Guid> list = null;

            using (SqlConnection localCon = new SqlConnection(constants.WORKFLOW_DATABASE_CONNECTION))
            {
                list = localCon.Query<Guid>("Select [InstanceId] from [System.Activities.DurableInstancing].[Instances] Order By [CreationTime]").ToList();
            }

            return list;
        }

        public void ResumeWorkflowInstance(Guid instanceID, string bookmarkName, object value)
        {
            syncEvent = new AutoResetEvent(false);
            var app = getAppFromInstanceID(instanceID);
            app.ResumeBookmark(bookmarkName, value);
            syncEvent.WaitOne();
        }

        public void TerminateWorkflowInstance(Guid instanceID, string reason)
        {
            syncEvent = new AutoResetEvent(false);
            var app = getAppFromInstanceID(instanceID);
            app.Terminate(reason);
            syncEvent.WaitOne();
        }

        private WorkflowApplication getAppFromInstanceID(Guid instanceID)
        {
            var instance = WorkflowApplication.GetInstance(instanceID, store);
            var workflow = WorkflowVersionMap.GetWorkflowDefinition(instance.DefinitionIdentity);
            var app = new WorkflowApplication(workflow, instance.DefinitionIdentity);
            ConfigureWorkflowApplication(app);
            app.Load(instance);
            return app;
        }
    }
}
