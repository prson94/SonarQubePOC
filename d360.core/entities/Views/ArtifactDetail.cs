using System.Collections.Generic;
using System.Xml.Linq;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Web.Script.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class ArtifactDetail : BaseIntObject, IIntObject
    {
        #region Properties

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "ArtifactType_Name", Description = "ArtifactType_Description")]
        public int ArtifactTypeID { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Description_Name", Description = "Description_Description")]
        public string Description { get; set; }

        [
        DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description"),
        Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(d360.core.resources.Fields), ErrorMessageResourceName = "Name_ErrorRequired"), StringLength(250)
        ]
        public string Name { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Parent_Name", Description = "Parent_Description")]
        public int? ParentID { get; set; }

        [ReadOnly(true), DatabaseGenerated(DatabaseGeneratedOption.Computed), Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Path_Name", Description = "Path_Description")]
        public string Path { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Status_Name", Description = "Status_Description")]
        public string Status { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "ArtifactType_Name", Description = "ArtifactType_Description")]
        public string ArtifactType { get; set; }

        [DataMember]
        public string TextPath { get; set; }
         
        [DataMember]
        public int TaxonomyTypeID { get; set; }

        [DataMember]
        public string TaxonomyType { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Locked_Name", Description = "Locked_Description")]
        public bool Locked { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Promoted_Name", Description = "Promoted_Description")]
        public bool Promoted { get; set; }

        #endregion
    }
}
