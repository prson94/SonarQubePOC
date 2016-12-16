using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace d360.extensions.workflow.activities
{
    public class SendMailActivity : IWorkflowActivity
    {
        public int ID { get { return 1; } }

        public string Name { get { return "Send Mail Activity"; } }

        //public XElement Fields
        //{
        //    get
        //    {
        //        throw new NotImplementedException();
        //    }

        //    set
        //    {
        //        throw new NotImplementedException();
        //    }
        //}

        public XElement Settings
        {
            get
            {
                return new XElement("settings",
                    new XElement("MessageSubjectTemplate", MessageSubjectTemplate),
                    new XElement("MessageBodyTemplate", MessageBodyTemplate)
                );
            }

            set
            {
                MessageSubjectTemplate = value.Element("MessageSubjectTemplate").Value;
                MessageBodyTemplate = value.Element("MessageBodyTemplate").Value;
            }
        }

        public string UserFullName { get; set; }

        public string UserEmail { get; set; }

        public string MessageSubjectTemplate { get; set; }

        public string MessageBodyTemplate { get; set; }

        public Dictionary<string, string> MessageSubjectTokens { get; set; }

        public Dictionary<string, string> MessageBodyTokens { get; set; }

        public void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
