using System.Runtime.Serialization;

namespace d360.core.entities.Permissions
{
    /// <summary>
    /// Model for example swagger response (swagger doesn't like simple dictionary)
    /// </summary>
    public class PermissionsResponseModel
    {
        [DataMember]
        public bool ReadAsset { get; set; }

        [DataMember]
        public bool ModifyAsset { get; set; }

        [DataMember]
        public bool DeleteAsset { get; set; }

        [DataMember]
        public bool ReadResponsibilities { get; set; }

        [DataMember]
        public bool ModifyResponsibilities { get; set; }

        [DataMember]
        public bool DeleteResponsibilities { get; set; }

        [DataMember]
        public bool ReadRelationships { get; set; }

        [DataMember]
        public bool ModifyRelationships { get; set; }

        [DataMember]
        public bool DeleteRelationships { get; set; }
    }
}
