using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace d360.core.workflow
{
    public class WorkflowEmailModel
    {        
        public string SubjectTemplate { get; set; }
        public string BodyTemplate { get; set; }

        public static WorkflowEmailModel ParseFromXml(XElement xml)
        {
            if (xml == null) throw new Exception("INVALID XML SPECIFIED");

            var subject = xml.Element("MessageSubjectTemplate").Value;
            var body = xml.Element("MessageBodyTemplate").Value;

            return new WorkflowEmailModel
            {
                SubjectTemplate = subject,
                BodyTemplate = body
            };
        }

    }
}
