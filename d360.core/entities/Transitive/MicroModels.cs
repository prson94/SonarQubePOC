using d360.core.enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    public class AllocationPossibilityComparer : IEqualityComparer<AllocationPossibility>
    {
        public bool Equals(AllocationPossibility x, AllocationPossibility y)
        {
            return (x.ObjectType == y.ObjectType && x.ObjectTypeID == y.ObjectTypeID);
        }

        public int GetHashCode(AllocationPossibility obj)
        {
            return obj.ObjectType.GetHashCode() ^ obj.ObjectTypeID.GetHashCode();
        }
    }

    public class AllocationPossibility
    {
        public int ObjectTypeID { get; set; }
        public string ObjectType { get; set; }
        public string Name { get; set; }
    }

    public class CompanySsoModel
    {
        public bool AllowNewUserLogin { get; set; }
        public AuthenticationType AuthenticationType { get; set; }
        public byte[] IdpCertificateFile { get; set; }
        public string IdpCertificatePassword { get; set; }
        public string IdpSloEndpoint { get; set; }
        public string IdpSsoEndpoint { get; set; }
        public byte[] SpCertificateFile { get; set; }
        public string SpCertificatePassword { get; set; }
        public HashAlgorithmType HashAlgorithmType { get; set; }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class ContextModel : BaseObject
    {
        public string ObjectType { get; set; }
        public int ObjectID { get; set; }
    }

    [DataContract(Namespace = NAMESPACE), System.ComponentModel.DataAnnotations.Schema.Table("Global_Resource", Schema = "reporting")]
    public class GlobalReportingResource : BaseObject
    {
        [DataMember, Key]
        public int ResourceID { get; set; }

        [DataMember]
        public string FirstName { get; set; }

        [DataMember]
        public string LastName { get; set; }

        [DataMember]
        public DateTime? DateLastLoggedIn { get; set; }

        [DataMember]
        public string Email { get; set; }

        [DataMember]
        public string Status { get; set; }

        [DataMember]
        public bool IsAdministrator { get; set; }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class OverlayEventHeader : BaseObject
    {
        [DataMember]
        public string Rule { get; set; }

        [DataMember]
        public int ID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Status { get; set; }

        [DataMember]
        public DateTime? Date { get; set; }

        [DataMember]
        public int Count { get; set; }
    }

    public partial class IntersectTypeOption
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class LoadDetail: BaseObject
    {
        [DataMember]
        public int ID { get; set; }
        [DataMember]
        public string Object { get; set; }
        [DataMember]
        public int ObjectID { get; set; }
        [DataMember]
        public string ObjectName { get; set; }
        [DataMember]
        public string Notes { get; set; }
        [DataMember]
        public string FilePath { get; set; }
        [DataMember]
        public DateTime? DateStarted { get; set; }
        [DataMember]
        public DateTime? DateCompleted { get; set; }
        [DataMember]
        public string Action { get; set; }
    }

    public class TopNavigation
    {
        public int ResourceID { get; set; }
        public string ResourceName { get; set; }
        public string ResourceImageUrl { get; set; }
        public string ResourceUrl { get; set; }
        public string LastLoggedInDate { get; set; }
        public List<TopNavigationItem> NavigationItems { get; set; }

    }
    public class TopNavigationItem
    {
        public string MenuID { get; set; }
        public Feature Feature { get; set; }
        public bool ShouldDisplay { get; set; }
        public string Items { get; set; }
        public List<NavigationItem> NavigationItems { get; set; }

    }
    public class NavigationItem
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string MenuID { get; set; }
        public List<NavigationItem> Items { get; set; }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class ObjectModel: BaseObject
    {
        public string ObjectType { get; set; }
        public int ObjectID { get; set; }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class PermissionModel : BaseObject
    {
        [DataMember]
        public string Claim { get; set; }

        [DataMember]
        public string ClaimObject { get; set; }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class ObjectStatisticTileModel : BaseObject
    {
        [DataMember]
        public int EventCount { get; set; }

        [DataMember]
        public string EventUrl { get; set; }

        [DataMember]
        public int FollowerCount { get; set; }
        
        [DataMember]
        public string FollowerUrl { get; set; }
        
        [DataMember]
        public int CommentCount { get; set; }
        
        [DataMember]
        public string CommentUrl { get; set; }

        [DataMember]
        public int Score { get; set; }

        [DataMember]
        public string ScoreUrl { get; set; }

        [DataMember]
        public List<ObjectStatisticTileItemModel> Items { get; set; }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class ObjectStatisticTileItemModel : BaseObject
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Url { get; set; }

        [DataMember]
        public int Count { get; set; }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class RawObjectStatistic : BaseObject
    {
        [DataMember]
        public string Name { get; set; }
        
        [DataMember]
        public int Value { get; set; }

        [DataMember]
        public string Group { get; set; }

        [DataMember]
        public string Url { get; set; }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class ReportSchemaModel: BaseObject
    {
        [DataMember]
        public string ID { get; set; }

        [DataMember]
        public string ParentID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Schema { get; set; }

        [DataMember]
        public int Position { get; set; }

        [DataMember]
        public string Type { get; set; }

        [DataMember]
        public List<ReportSchemaModel> Items { get; set; }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class StatusCount : BaseObject
    {
        [DataMember]
        public string Status { get; set; }
        
        [DataMember]
        public int Count { get; set; }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class Property: BaseObject
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Value { get; set; }
    }
}
