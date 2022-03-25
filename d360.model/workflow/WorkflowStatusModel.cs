using System;
using System.Xml.Linq;

namespace d360.model.workflow
{
    public class WorkflowStatusModel
    {
        public string Status { get; set; }

        public static WorkflowStatusModel ParseFromXml(XElement xml)
        {
            if (xml == null)
            {
                throw new Exception("INVALID XML SPECIFIED");
            }

            string status = string.Empty;

            if (xml.Element("Status") != null)
            {
                status = xml.Element("Status").Value;
            }

            return new WorkflowStatusModel
            {
                Status = status ?? "Draft",
            };
        }
    }
}
