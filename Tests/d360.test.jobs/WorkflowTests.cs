using d360.core;
using d360.core.entities;
using d360.core.queue;
using d360.extensions.caching;
using d360.extensions.info;
using d360.extensions.queue;
using d360.extensions.search;
using d360.extensions.storage;
using d360.model;
using Dapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OptimaJet.Workflow.Core.Builder;
using OptimaJet.Workflow.Core.Bus;
using OptimaJet.Workflow.Core.Parser;
using OptimaJet.Workflow.Core.Runtime;
using OptimaJet.Workflow.DbPersistence;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace d360.test.jobs
{
    [TestClass]
    public class WorkflowTests: BaseTest
    {
        private static volatile WorkflowRuntime _runtime;
        private static readonly object _sync = new object();
        private static Guid processID = Guid.NewGuid();

        public static WorkflowRuntime Runtime
        {
            get {
                if (_runtime == null)
                {
                    lock (_sync)
                    {
                        if (_runtime == null)
                        {
                            var companyID = 4;
                            var connectionString = getStaticCompanyConnectionString(companyID);

                            var builder = new WorkflowBuilder<XElement>(
                                new MSSQLProvider(connectionString),
                                new XmlWorkflowParser(),
                                new MSSQLProvider(connectionString)
                                ).WithDefaultCache();

                            _runtime = new WorkflowRuntime(new Guid("{8D38DB8F-F3D5-4F26-A989-4FDD40F32D9D}"))
                                .WithBuilder(builder)
                                .WithPersistenceProvider(new MSSQLProvider(connectionString))
                                .WithTimerManager(new TimerManager())
                                .WithBus(new NullBus())
                                .SwitchAutoUpdateSchemeBeforeGetAvailableCommandsOn()
                                .Start();
                        }
                    }
                }
                return _runtime;
            }
        }

        [TestMethod]
        public void TestEntireWorkflowProcess()
        {
            createInstance();

            var list = getAvailableCommands().ToList();

            executeCommand(list[0].CommandName);

            var states = getAvailableState().ToList();

            setState(states[0].Name);

            deleteInstance();
        }

        void executeCommand(string commandName)
        {
            WorkflowCommand command = null;
            do
            {
                command = Runtime.GetAvailableCommands(processID, string.Empty).Where(c => c.CommandName == commandName).FirstOrDefault();
                if (command == null)
                    Console.WriteLine("The command isn't found.");
            } while (command == null);

            Runtime.ExecuteCommand(command, string.Empty, string.Empty);
        }

        void createInstance()
        {
            Runtime.CreateInstance("SimpleWF", processID);
        }

        IEnumerable<WorkflowCommand> getAvailableCommands()
        {
            return Runtime.GetAvailableCommands(processID, string.Empty);
        }

        IEnumerable<WorkflowState> getAvailableState()
        {
            return Runtime.GetAvailableStateToSet(processID);
        }

        void setState(string stateName)
        {
            WorkflowState state = Runtime.GetAvailableStateToSet(processID).Where(c => c.Name == stateName).FirstOrDefault();
            if (state != null)
            {
                Runtime.SetState(processID, string.Empty, string.Empty, state.Name, new Dictionary<string, object>());
            }
        }

        void deleteInstance()
        {
            Runtime.PersistenceProvider.DeleteProcess(processID);
        }
    }
}
