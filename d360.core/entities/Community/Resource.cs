using System;
using System.Collections.Generic;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Resource : BaseIntObject, IIntObject, IFieldsObject, ICreatedObject, IUpdatedObject   
    {
        #region Properties

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)] 
        public string APIPrivateKey { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)] 
        public string APIPublicKey { get; set; }

        [DataMember]
        [Required(ErrorMessageResourceType = typeof(d360.core.resources.Fields), ErrorMessageResourceName = "Email_ErrorRequired")]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Email_Name", Description = "Email_Description")]
        public string Email { get; set; }

        [StringLength(250)]
        [DataMember]
        [Required(ErrorMessageResourceType = typeof(d360.core.resources.Fields), ErrorMessageResourceName = "FirstName_ErrorRequired")]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "FirstName_Name", Description = "FirstName_Description")]
        public string FirstName { get; set; }

        [StringLength(250)]
        [DataMember]
        [Required(ErrorMessageResourceType = typeof(d360.core.resources.Fields), ErrorMessageResourceName = "LastName_ErrorRequired")]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "LastName_Name", Description = "LastName_Description")]
        public string LastName { get; set; }

        public string Password { get; set; }

        [DataMember]
        [Required(ErrorMessageResourceType = typeof(d360.core.resources.Fields), ErrorMessageResourceName = "ResourceType_ErrorRequired")]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "ResourceType_Name", Description = "ResourceType_Description")]
        public int ResourceTypeID { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Username_Name", Description = "Username_Description")]
        public string Username { get; set; }

        [DataMember]
        public Guid Uid { get; set; } = Guid.NewGuid();

        [DataMember]
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;

        #endregion

        public virtual ResourceType ResourceType { get; set; }

        public ICollection<CompanyResource> CompanyResources { get; set; }

        public string FormatDisplayName()
        {
            return string.Format("{0} {1}", FirstName, LastName);            
        }

        public FieldsObjectModel GetFieldsObjectInfo()
        {
            return new FieldsObjectModel { Type = SystemObjects.ResourceType, Object = SystemObjects.Resource, TypeID = ResourceTypeID };
        }
    }
}
