using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Report : BaseIntObject, IIntObject, IUpdatedMetadata
    {
        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "ReportName_Description")]
        public string Name { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Description_Name", Description = "ReportDescription_Description")]
        public string Description { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "ReportObjectType_Name", Description = "ReportObjectType_Description")]
        [Column(TypeName = "varchar"), StringLength(25)]
        public string ObjectType { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "ReportObjectType_Name", Description = "ReportObjectType_Description")]
        public int ObjectID { get; set; }

        private const string DEFAULT_REPORT_TYPE = "legacy";
        private string _reportType = DEFAULT_REPORT_TYPE;

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(25)]
        public string ReportType
        {
            get => _reportType;
            set
            {
                _reportType = value;
            }
        }

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(50)]
        public string PowerBIReportID { get; set; }

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(50)]
        public string PowerBIDatasetID { get; set; }

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(260)]
        public string FileName { get; set; }

        [DataMember]
        [Column(TypeName = "nvarchar"), StringLength(500)]
        public string Url { get; set; }

        [DataMember]
        public bool ShowOnHomePage { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid uid { get; set; }

        [NotMapped, DataMember]
        public string VisibleTo { get; set; }

        public DateTime? UpdatedOn { get; set; }
        
        public int? UpdatedBy { get; set; }

        [IgnoreDataMember, ForeignKey("ReportID")]
        public virtual ICollection<ReportResponsibility> Responsibilities { get; set; }
    }
}
