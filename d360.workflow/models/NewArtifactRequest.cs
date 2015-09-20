using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace d360.workflow.models
{
    public class NewArtifactRequest
    {
        public int RequestingResourceID { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public int ArtifactTypeID { get; set; }

        public int VocabularyID { get; set; }

        public int TaxonomyTypeID { get; set; }

        public int? ParentID { get; set; }

        public Dictionary<string, object> Fields { get; set; }

        public XElement ToXml()
        {
            return new XElement("fields",
                new XElement("ArtifactTypeID", ArtifactTypeID),
                new XElement("Description", Description),
                new XElement("Name", Name),
                new XElement("RequestingResourceID", RequestingResourceID),
                new XElement("TaxonomyTypeID", TaxonomyTypeID)
            ); ;
        }
    }
}
