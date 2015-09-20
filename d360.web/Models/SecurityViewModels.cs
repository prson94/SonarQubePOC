using System.Collections.Generic;
using d360.core.entities;
using System.Linq;
using d360.core;
using System.Runtime.Serialization;
namespace d360.web.Models
{
    //public class AdministrationResourceViewModel
    //{
    //    public List<ResourceType> Types { get; set; }
    //    public List<CompanyResource> Items { get; set; }
    //}
    //public class DetailResourceViewModel
    //{
    //    public DetailResourceViewModel()
    //    {
    //        IsMe = false;
    //    }

    //    public Resource Item { get; set; }
    //    public bool IsMe { get; set; }
    //}

    //public class OwnershipTypeModel
    //{
    //    public SystemObjects ID { get; set; }
    //    public bool GroupAllowed { get; set; }
    //    public bool ResourceAllowed { get; set; }
    //    public List<SystemObjectInfo> Options { get; set; }

    //    public OwnershipTypeModel()
    //    {
    //        Options = Enums.GetSystemObjectInfoList().Where(i => i.AllowOwnership).OrderBy(i => i.Description).ToList();
    //    }
    //}

    //[DataContract(Name = "ownershipType")]
    //public class OwnershipTypeApiModel
    //{
    //    [DataMember]
    //    public bool GroupAllowed { get; set; }
    //    [DataMember]
    //    public bool ResourceAllowed { get; set; }
    //    [DataMember]
    //    public string Description { get; set; }
    //    [DataMember]
    //    public int ID { get; set; }
    //}

    //public class OwnerEditModel
    //{
    //    public SystemObjects ObjectType { get; set; }
    //    public string ResourceObjectType { get; set; }
    //    public int ResourceObjectID { get; set; }
    //    public int ObjectID { get; set; }
    //    public int RoleID { get; set; }
    //    public List<Role> Roles { get; set; }
    //    public List<Resource> Resources { get; set; }
    //}

    //public class OwnerViewModel
    //{ 
    
    //}

    //public class UsersViewModel
    //{
    //    public string ObjectType { get; set; }
    //    public int ObjectTypeID { get; set; }
    //    public string JsonUri { get; set; }
    //}

    //public class UserEditModel 
    //{
    //    public Resource Resource { get; set; }
    //    public Fields Fields { get; set; }
    //    public List<Role> Roles { get; set; }
    //    public ResourceType ResourceType { get; set; }
    //}
}