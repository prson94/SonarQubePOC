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
        public int TaxonomyTypeID { get; set; }

        public static WorkflowRegistrationSettingsModel parseXml(XElement xml)
        {
            bool? vis = null;
            int taxonomyTypeId = 0;

            if (xml == null) throw new Exception("INVALID XML SPECIFIED");

            if (xml.HasElements) {
                var visString = "";
                var taxonomyTypeIdString = "";

                if(xml.Element("Visible")!=null)
                    visString = xml.Element("Visible").Value;
                
                if (visString == "1" || (visString ??"").ToUpper() == "TRUE") vis = true;
                else vis = false;

                if (xml.Element("TaxonomyTypeID") != null)
                    taxonomyTypeIdString = xml.Element("TaxonomyTypeID").Value;

                if (!string.IsNullOrEmpty(taxonomyTypeIdString))
                {
                    int.TryParse(taxonomyTypeIdString, out taxonomyTypeId);                    
                }

            }

            return new WorkflowRegistrationSettingsModel
            {
                Visible = vis,
                TaxonomyTypeID = taxonomyTypeId
            };
        }
    }
}
