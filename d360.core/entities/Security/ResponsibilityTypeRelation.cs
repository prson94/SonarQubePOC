using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    /// <summary>
    /// Defines what types of artifacts can be assigned as a source for a given responsibility type.
    /// </summary>
    [DataContract(Namespace = NAMESPACE)]
    public class ResponsibilityTypeRelation : BaseObject
    {
        [Key, Column(Order = 1), DataMember]
        public int ResponsibilityTypeID { get; set; }

        [Key, Column(Order = 2, TypeName = "varchar"), StringLength(50), DataMember]
        public string ObjectType { get; set; }

        [Key, Column(Order = 3), DataMember]
        public int ObjectID { get; set; }

        [DataMember,  Display(ResourceType = typeof(d360.core.resources.Fields),  Name = "Permissions_ReadObject_Name",  Description = "Permissions_ReadObject_Description")]
        public bool ReadObject { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Permissions_ReadAttributes_Name", Description = "Permissions_ReadAttributes_Description")]
        public bool ReadAttributes { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Permissions_ReadAudit_Name", Description = "Permissions_ReadAudit_Description")]
        public bool ReadAudit { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Permissions_ReadDashboards_Name", Description = "Permissions_ReadDashboards_Description")]
        public bool ReadDashboards { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Permissions_ReadRelationships_Name", Description = "Permissions_ReadRelationships_Description")]
        public bool ReadRelationships { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Permissions_ReadSocial_Name", Description = "Permissions_ReadSocial_Description")]
        public bool ReadSocial { get; set; }


        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Permissions_ModifyObject_Name", Description = "Permissions_ModifyObject_Description")]
        public bool ModifyObject { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Permissions_ModifyAttributes_Name", Description = "Permissions_ModifyAttributes_Description")]
        public bool ModifyAttributes { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Permissions_ModifyRelationships_Name", Description = "Permissions_ModifyRelationships_Description")]
        public bool ModifyRelationships { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Permissions_ModifySocial_Name", Description = "Permissions_ModifySocial_Description")]
        public bool ModifySocial { get; set; }


        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Permissions_DeleteObject_Name", Description = "Permissions_DeleteObject_Description")]
        public bool DeleteObject { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Permissions_DeleteAttributes_Name", Description = "Permissions_DeleteAttributes_Description")]
        public bool DeleteAttributes { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Permissions_DeleteRelationships_Name", Description = "Permissions_DeleteRelationships_Description")]
        public bool DeleteRelationships { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Permissions_DeleteSocial_Name", Description = "Permissions_DeleteSocial_Description")]
        public bool DeleteSocial { get; set; }

        public virtual ResponsibilityType ResponsibilityType { get; set; }
    }
}
