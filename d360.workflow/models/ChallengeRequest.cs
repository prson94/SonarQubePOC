using System.Xml.Linq;

namespace d360.workflow.models
{
    public class ChallengeRequest
    {
        public int RequestingResourceID { get; set; }

        public string Reason { get; set; }

        public int ArtifactTypeID { get; set; }

        public int ArtifactID { get; set; }

        public string Name { get; set; }

        public string ArtifactTypeName{ get; set; }

        public int CommentID { get; set; }

        public XElement ToXml()
        {
            return new XElement("fields",
                new XElement("Reason", Reason),                
                new XElement("RequestingResourceID", RequestingResourceID),
                new XElement("ArtifactTypeID", ArtifactTypeID),
                new XElement("ArtifactTypeName", ArtifactTypeName),
                new XElement("ArtifactID", ArtifactID),
                new XElement("Name", Name),
                new XElement("CommentID", CommentID)
            ); ;
        }
    }
}

