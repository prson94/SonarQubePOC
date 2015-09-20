using d360.core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.workflow
{
    [Flags]
    public enum WorkflowType
    {
        [
        Name("Propose New Artifact"), 
        Description("The workflow that is triggered when a user proposes a new artifact."), 
        EnumMember(Value = "1"), 
        AssignableTypesAttribute(SystemObjects.ArtifactType)]
        SuggestNewArtifact = 1,
        
        [
        Name("Certify Artifact"), 
        Description("The workflow that is triggered when an owner must certify an artifact to validate that all data is correct."), 
        EnumMember(Value = "2"), 
        AssignableTypesAttribute(SystemObjects.ArtifactType, SystemObjects.Vocabulary)
        ]
        CertifyArtifact = 2
    }

    [Flags]
    public enum SuggestNewArtifactSteps
    {
        [Name("Collect Relevant Request Information"), EnumMember(Value = "1")]
        CollectInfo = 1,
        [Name("Assign Responsible Resources"), EnumMember(Value = "2")]
        AssignResponsibleResources = 2,
        [Name("Awaiting Approvals from Assigned Resources"), EnumMember(Value = "3")]
        AwaitingApprovals = 3,
        [Name("Received Approvals from Assigned Resources"), EnumMember(Value = "4")]
        ReceivedApprovals = 4,
        [Name("Artifact Added to System"), EnumMember(Value = "5")]
        ArtifactAdded = 5,
        [Name("Notification Issued"), EnumMember(Value = "6")]
        NotificationIssued = 6,
        [Name("Workflow Complete"), EnumMember(Value = "7")]
        WorkflowComplete = 7
    }

    [Flags]
    public enum CertifyArtifactSteps
    {
        [Name("Collect Relevant Request Information"), EnumMember(Value = "1")]
        CollectInfo = 1,
        [Name("Assign Responsible Resources"), EnumMember(Value = "2")]
        AssignResponsibleResources = 2,
        [Name("Awaiting Certifications from Assigned Resources"), EnumMember(Value = "3")]
        AwaitingCertifications = 3,
        [Name("Received Certifications from Assigned Resources"), EnumMember(Value = "4")]
        ReceivedCertifications = 4,
        [Name("Artifact Certified"), EnumMember(Value = "5")]
        ArtifactCertified = 5,
        [Name("Workflow Complete"), EnumMember(Value = "6")]
        WorkflowComplete = 6
    }

    public class WorkflowTypeInfo
    {
        public WorkflowType ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class WorkflowTypeStepInfo
    {
        public int ID { get; set; }
        public string Name { get; set; }
    }

    public static class WorkflowTypeExtensions
    {
        public static string GetWorkflowTypeDisplayName(this WorkflowType type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<NameAttribute>().Name;
        }

        public static string GetWorkflowTypeDescription(this WorkflowType type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<DescriptionAttribute>().Description;
        }


        public static SystemObjects[] GetWorkflowTypeAssignableTypes(this WorkflowType type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<AssignableTypesAttribute>().Values;
        }

        public static List<WorkflowTypeInfo> GetAllowedWorkflows(this SystemObjects type)
        {
            var list = new List<WorkflowTypeInfo>();

            foreach (MemberInfo tm in (typeof(WorkflowType)).GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                if (tm.GetCustomAttribute<AssignableTypesAttribute>().Values.Contains(type))
                {
                    list.Add(new WorkflowTypeInfo
                    {
                        Name = ((NameAttribute)tm.GetCustomAttribute(typeof(NameAttribute))).Name,
                        Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                        ID = (WorkflowType)Enum.Parse(typeof(WorkflowType), tm.Name)
                    });
                }
            }

            return list;
        }

        public static List<WorkflowTypeInfo> GetWorkflowTypeEnumList(this WorkflowType type)
        {
            var list = new List<WorkflowTypeInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                list.Add(new WorkflowTypeInfo
                {
                    Name = ((NameAttribute)tm.GetCustomAttribute(typeof(NameAttribute))).Name,
                    Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                    ID = (WorkflowType)Enum.Parse(typeof(WorkflowType), tm.Name)
                });
            }

            return list;
        }

        public static List<WorkflowTypeStepInfo> GetWorkflowTypeStepsEnumList(this WorkflowType type)
        {
            var list = new List<WorkflowTypeStepInfo>();

            switch (type)
            { 
                case WorkflowType.CertifyArtifact:
                    foreach (MemberInfo tm in CertifyArtifactSteps.CollectInfo.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
                    {
                        list.Add(new WorkflowTypeStepInfo
                        {
                            Name = ((NameAttribute)tm.GetCustomAttribute(typeof(NameAttribute))).Name,
                            ID = (int)Enum.Parse(typeof(CertifyArtifactSteps), tm.Name)
                        });
                    }
                    break;
                case WorkflowType.SuggestNewArtifact:
                    foreach (MemberInfo tm in SuggestNewArtifactSteps.CollectInfo.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
                    {
                        list.Add(new WorkflowTypeStepInfo
                        {
                            Name = ((NameAttribute)tm.GetCustomAttribute(typeof(NameAttribute))).Name,
                            ID = (int)Enum.Parse(typeof(SuggestNewArtifactSteps), tm.Name)
                        });
                    }
                    break;
            }

            return list;
        }
    }
}
