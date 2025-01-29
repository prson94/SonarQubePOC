using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

using d360.core.enums;
using Newtonsoft.Json;

namespace d360.core.entities.Membership
{
    public class UserUpsertModel
    {
        public IEnumerable<UserApiModel> Users { get; set; }

        public bool LookupFieldsPassedByValue { get; set; }

        public bool IsInsert { get; set; }
    }

    public class UpdateGroupModel
    {
        public Guid? Uid { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public Guid? PrimaryOwnerUid { get; set; }

        public Guid? SecondaryOwnerUid { get; set; }

        public bool IsActiveDirectoryGroup { get; set; } = false;

        public Dictionary<string, string> Fields { get; set; } = new Dictionary<string, string>();
    }

	public class UserUpsertValidateModel
	{
		public UserApiModel users { get; set; }

		public string Message { get; set; }

		public bool? Success { get; set; }
	}

	public class UserApiModel
    {
        [DataMember]
        public Guid? uid { get; set; }

		[DataMember]
		public string Email { get; set; }
		
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

		[IgnoreDataMember, JsonIgnore]
        public bool IsNew { get; set; }

		[IgnoreDataMember, JsonIgnore]
		public int? ResourceID { get; set; }

		[IgnoreDataMember, JsonIgnore]
		public CompanyResourceState? CompanyResourceState { get; set; }

		[IgnoreDataMember, JsonIgnore]
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
