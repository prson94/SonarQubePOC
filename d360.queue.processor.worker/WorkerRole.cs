using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Autofac;
using System.IO;
using System.Reflection;
using d360.extensions;
using d360.core.queue;
using Autofac.Features.Metadata;
using d360.model;
using d360.extensions.search;
using d360.extensions.info;
using d360.extensions.queue;
using d360.extensions.caching;
using System.Text;
using Newtonsoft.Json;
using d360.core.entities;
using Microsoft.AspNet.SignalR.Client;
using System.Data.SqlClient;
using Dapper;
using SpreadsheetLight;
using d360.core;

namespace d360.queue.processor.worker
{
    public class WorkerRole : RoleEntryPoint
    {
        //public string HubEndpoint { get { return CloudConfigurationManager.GetSetting("HubEndpoint"); } }
        IContainer Container;
        //ContainerBuilder Builder;

        // The name of your queue
        const string ServiceBusConnectionString = constants.SERVICE_BUS_ACTIONS;
        const string QueueName = "company-actions";

        // QueueClient is thread-safe. Recommended that you cache rather than recreating it on every request
        QueueClient Client;
        ManualResetEvent CompletedEvent = new ManualResetEvent(false);

        SqlConnection getCompanyConnection(int companyID)
        {
            var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION);
            cnn.Open();
            var db = cnn.Query<DatabaseServer>(
                @"select D.* from Company C inner join DatabaseServer D on D.ID = C.DatabaseServerID where C.ID = @id",
                new { id = companyID }
            ).SingleOrDefault();
            cnn.Close();
            cnn.Dispose();

            if (db != null)
            {
                cnn = new SqlConnection(
                    string.Format("server={0};Database=D3S_{1};User ID={2};Password={3}", db.Server, companyID, db.Username, db.Password)
                );
                db = null;
            }
            return cnn;
        }

        public override void Run()
        {
            Trace.WriteLine("Starting processing of messages");

            // Initiates the message pump and callback is invoked for each message that is received, calling close on the client will stop the pump.
            Client.OnMessage((m) =>
                {
                    try
                    {
                        var success = false;
                        QueueAction queueAction;
                        if (Enum.TryParse<QueueAction>(m.To, true, out queueAction))
                        {
                            ISearchSource searchSource = null;
                            //IStorageProvider storage = null;

                            switch (queueAction)
                            {
                                case QueueAction.AddItemVersion:
                                    #region
                                    var obj1 = m.GetBody<AddItemVersionModel>();
                                    break;
                                    #endregion
                                case QueueAction.AddToIndex:
                                    #region
                                    var obj2 = m.GetBody<AddToIndexModel>();
                                    searchSource = Container.Resolve<ISearchSource>();
                                    searchSource.AddToIndex(obj2);
                                    break;
                                    #endregion
                                case QueueAction.RemoveFromIndex:
                                    #region
                                    var obj6 = m.GetBody<RemoveFromIndexModel>();
                                    searchSource = Container.Resolve<ISearchSource>();
                                    searchSource.RemoveFromIndex(obj6);
                                    break;
                                    #endregion
                                case QueueAction.StartFusionRequest:
                                    #region
                                    var obj7 = m.GetBody<FusionStartRequestModel>();
                                    //var hubConnection = new HubConnection(HubEndpoint);
                                    //var hubProxy = hubConnection.CreateHubProxy("AgentHub");
                                    //hubConnection.Start().ContinueWith(task => {
                                    //}).Wait();
                                    //hubProxy.Invoke<FusionStartRequestModel>("", obj7).ContinueWith(task => {
                                    //    if (!task.IsFaulted)
                                    //    { 
                                            
                                    //    }
                                    //});
                                    break;
                                    #endregion
                                case QueueAction.UpdateFusionProgress:
                                    #region
                                    var obj8 = m.GetBody<FusionProgressModel>();
                                    break;
                                    #endregion
                                case QueueAction.UpdateInIndex:
                                    #region
                                    var obj9 = m.GetBody<UpdateInIndexModel>();
                                    searchSource = Container.Resolve<ISearchSource>();
                                    searchSource.UpdateInIndex(obj9);
                                    break;
                                    #endregion
                            }

                            success = true;
                        }

                        if (success)
                            m.Complete();
                        else
                            m.DeadLetter();
                    }
                    catch
                    {
                        if (m.DeliveryCount > 3)
                            m.DeadLetter();
                        else 
                            m.Defer();
                    }
                });

            CompletedEvent.WaitOne();
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // Create the queue if it does not exist already
            var namespaceManager = NamespaceManager.CreateFromConnectionString(ServiceBusConnectionString);
            if (!namespaceManager.QueueExists(QueueName))
            {
                namespaceManager.CreateQueue(QueueName);
            }

            // Initialize the connection to Service Bus Queue
            Client = QueueClient.CreateFromConnectionString(ServiceBusConnectionString, QueueName);
            
            var Builder = new ContainerBuilder();

            Builder.RegisterType<AzureSearchSource>().As<ISearchSource>().InstancePerDependency();

            Container = Builder.Build();
            
            return base.OnStart();
        }

        public override void OnStop()
        {
            // Close the connection to Service Bus Queue
            Client.Close();
            CompletedEvent.Set();
            base.OnStop();
        }
    }
}
