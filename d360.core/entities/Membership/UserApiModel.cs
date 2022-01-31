using d360.core.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities.Membership
{
    public class UserApiDeleteModel
    {
        public Guid Uid { get; set; }
        public CompanyResource CompanyResource { get; set; }
        public GlobalReportingResource Resource { get; set; }
    }

    public class UserUpsertModel
    {
        public IEnumerable<UserApiUpdateModel> Users { get; set; }
        public bool LookupFieldsPassedByValue { get; set; }
        public bool IsInsert { get; set; }
    }

    public interface IUserApiUpsertModel : IExecutionItem
    {
        Guid? uid { get; set; }
        string Username { get; set; }
        string FirstName { get; set; }
        string LastName { get; set; }
        string Password { get; set; }
        bool IsAdministrator { get; set; }
        CompanyResourceState? State { get; set; }
        Dictionary<string, string> Fields { get; set; }

        bool IsNew { get; set; }
        int? ResourceID { get; set; }
        CompanyResourceState? CompanyResourceState { get; set; }
        int ItemNumber { get; set; }

    }

    public class UpdateGroupModel
    {
        public Nullable<Guid> Uid { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Guid PrimaryOwnerUid { get; set; }
        public Nullable<Guid> SecondaryOwnerUid { get; set; }
        public bool IsActiveDirectoryGroup { get; set; } = false;
        public Dictionary<string, string> Fields { get; set; } = new Dictionary<string, string>();
    }

    public class UserApiInsertModel : IUserApiUpsertModel
    {
        public Guid? uid { get; set; }
        [DataMember]
        public string Username { get; set; }
        [DataMember]
        public string FirstName { get; set; }
        [DataMember]
        public string LastName { get; set; }
        [DataMember]
        public string Password { get; set; }
        [DataMember]
        public bool IsAdministrator { get; set; }
        [DataMember]
        public Guid? ExecutionItemUid { get; set; }
        [DataMember]
        public CompanyResourceState? State { get; set; }
        [DataMember]
        public Dictionary<string, string> Fields { get; set; } = new Dictionary<string, string>();

        public bool IsNew { get; set; }
        public int? ResourceID { get; set; }
        public CompanyResourceState? CompanyResourceState { get; set; }

        public int ItemNumber { get; set; }
    }

    public class UserApiUpdateModel : IUserApiUpsertModel
    {
        [DataMember]
        public Guid? uid { get; set; }
        [DataMember]
        public string Username { get; set; }
        [DataMember]
        public string FirstName { get; set; }
        [DataMember]
        public string LastName { get; set; }
        [DataMember]
        public string Password { get; set; }
        [DataMember]
        public bool IsAdministrator { get; set; }
        [DataMember]
        public Guid? ExecutionItemUid { get; set; }
        [DataMember]
        public CompanyResourceState? State { get; set; }
        [DataMember]
        public Dictionary<string, string> Fields { get; set; } = new Dictionary<string, string>();

        public bool IsNew { get; set; }
        public int? ResourceID { get; set; }
        public CompanyResourceState? CompanyResourceState { get; set; }

        public int ItemNumber { get; set; }
    }

    public class UserApiUpsertResult : IExecutionItem
    {
        [DataMember]
        public int ItemNumber { get; set; }
        [DataMember]
        public Guid? uid { get; set; }
        [DataMember]
        public Guid? ExecutionItemUid { get; set; }
        [DataMember]
        public string Message { get; set; }
        [DataMember]
        public bool Success { get; set; }
    }
    public class DeleteGroupModel
    {
        public Guid Uid { get; set; }
    }
    public class DeleteUserModel
    {
        public Guid Uid { get; set; }
    }

    public class OrganizationModel
    {
        public Guid uid { get; set; }

        public string Name { get; set; }

        public Guid? AcceptedBy { get; set; }

        public string AcceptedByUserName { get; set; }

        public DateTime? AcceptedOn { get; set; }

        public string AdministratorEmail { get; set; }
    }

    public class OrganizationDetailModel
    {
        public Guid uid { get; set; }
        public string Name { get; set; }
        public Guid? AcceptedBy { get; set; }
        public string AcceptedByUserName { get; set; }
        public DateTime? AcceptedOn { get; set; }
        public string AdministratorEmail { get; set; }
        public List<string> Domains { get; set; }
        public List<string> Users { get; set; }
        public List<string> Invitations { get; set; }
    }

    public class ApiKeyDetailModel
    {
        public string apiKey { get; set; }
        public string apiSecret { get; set; }
    }

    public class UpdateUserWatchModel
    {
        public Guid? assetUid { get; set; }
        public Guid? assetTypeUid { get; set; }
        public bool watches { get; set; }
    }
}
