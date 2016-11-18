using System;

namespace d360.workflow
{
    public static class WorkflowExtensions
    {
        public static string GetWorkflowActionUrl(this Guid id, string prefix, WorkflowType type)
        {
            return $"https://{prefix}.data3sixty.com/workflow/work/{(int)type}/{id}";
        }

        public static string GetWorkflowStatusUrl(this Guid id, string prefix, WorkflowType type)
        {
            return $"https://{prefix}.data3sixty.com/workflow/status/{id}";
        }

        public static string GetArtifactUrl(this int id, string prefix, int typeID)
        {
            return $"https://{prefix}.data3sixty.com/artifact/{typeID}/{id}";
        }

        public static string GetCompanyUrl(this string prefix)
        {
            return $"https://{prefix}.data3sixty.com";
        }
    }
}
