using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace d360.workflow.models
{
    public class CertifyArtifactRequest
    {
        public CertifyArtifactRequest()
        {
            SendMailFromWorkflow = false;
        }

        public int ArtifactID { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime DueDate { get; set; }

        public bool SendMailFromWorkflow { get; set; }

        #region Computed Properties

        public string ArtifactName { get; set; }

        public int ArtifactTypeID { get; set; }

        public string ArtifactTypeName { get; set; }

        public string ArtifactUrl { get; set; }

        #endregion

        public XElement ToXml()
        {
            return new XElement("fields",
                new XElement("ArtifactID", ArtifactID),
                new XElement("StartDate", StartDate),
                new XElement("DueDate", DueDate)
            );        
        }
    }
}
