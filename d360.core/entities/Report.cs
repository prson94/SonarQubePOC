using System.Collections.Generic;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), ObjectType(d360.core.ObjectTypeInfo.Report, "Report")]
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

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "ReportLayout_Name", Description = "ReportLayout_Description")]
        public int ReportLayoutID { get; set; }

        private const string DEFAULT_REPORT_TYPE = "legacy";
        private string _reportType = DEFAULT_REPORT_TYPE;
        [DataMember]        
        [Column(TypeName = "varchar"), StringLength(25)]
        public string ReportType {
            get
            {
                return _reportType;
            }
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

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        [IgnoreDataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "ReportLayout_Name", Description = "ReportLayout_Description")]
        public virtual ReportLayout ReportLayout { get; set; }

        [IgnoreDataMember]
        public virtual ICollection<ReportTile> ReportTiles { get; set; }
    }
}
