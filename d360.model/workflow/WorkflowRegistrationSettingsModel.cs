using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace d360.model.workflow
{
    public class WorkflowRegistrationSettingsModel
    {
        public bool? Visible { get; set; }

        public static WorkflowRegistrationSettingsModel parseXml(XElement xml)
        {
            bool? vis = null;

            if (xml == null) throw new Exception("INVALID XML SPECIFIED");

            if (xml.HasElements) {
                var visString = xml.Element("Visible").Value;

                if (visString == "1") vis = true;
                else vis = false;
            }

            return new WorkflowRegistrationSettingsModel
            {
                Visible = vis
            };
        }
    }
}
