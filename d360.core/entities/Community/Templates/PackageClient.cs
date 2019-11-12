using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities.Community.Templates
{
    public enum PackageClientState
    {
        Active = 1,
        Inactive = 2
    }

    [Table("PackageClient", Schema = "community")]
    public class PackageClient : BaseTemplateGuidObject
    {
        [DataMember]
        public Guid PackageUid { get; set; }
        [DataMember]
        public Guid PackageVersionUid { get; set; }
        [DataMember]
        public int ClientID { get; set; }

        [IgnoreDataMember, Column("Companies")]
        protected string CompaniesJson { get; set; }

        [DataMember, NotMapped]
        public List<int> Companies { 
            get {
                return JsonConvert.DeserializeObject<List<int>>(string.IsNullOrEmpty(CompaniesJson) ? "[]" : CompaniesJson);
            } 
            set {
                JsonConvert.SerializeObject(value as List<int>);
            } 
        }
    }
}
