using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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
	//	{

	// Definition: {
	//  powerBiReportId: "",
	//  powerBiDatasetId: "",
	//  fileName: "",
	//  parameters: [{
	//   name: "",
	//   valueToProvide: "--------TBD--------"
	//  }]

	//  // OR

	//  url: "",
	//  parameters:
	//[{
	//name: "",
	//   valueToProvide: "--------TBD--------"
	//  }]  
	// }
	//}
	public enum DashboardType
	{
		PowerBi = 1,
		DqPlus = 2
	}

	public enum Location
	{
		List = 1,
		Detail = 2,
		Homepage = 3
	}

	public class DashboardModel
	{
		[DataMember]
		public Guid AssetTypeUid { get; set; }
		[DataMember]
		public string Name { get; set; }
		[DataMember]
		public string Description { get; set; }
		[DataMember]
		[JsonConverter(typeof(StringEnumConverter))]
		public DashboardType DashboardType { get; set; }
		[DataMember]
		[JsonConverter(typeof(StringEnumConverter))]
		public Location Location { get; set; }
		[DataMember]
		public DashboardDefinition Definition
		{
			get
			{
				if(_definitionJson == null)
				{
					return null;
				}
				return JsonConvert.DeserializeObject<DashboardDefinition>(_definitionJson);
			}

			set {
				_definitionJson = JsonConvert.SerializeObject(value);
			}
		}
		[JsonIgnore]
		public string _definitionJson { get; set; }
	}

	public class DashboardDefinition
	{
		[DataMember]
		public string url { get; set; }
		[DataMember]
		public string fileName { get; set; }
		[DataMember]
		public Guid? powerBiReportId { get; set; }
		[DataMember]
		public Guid? powerBiDatasetId { get; set; }
		[DataMember]
		public List<DashboardDefinitionParameter> parameters { get; set; }
	}

	public class DashboardDefinitionParameter
	{
		public string name { get; set; }
		public string valueToProvide { get; set; }
	}

	public class DashboardApiGetModel : DashboardModel
	{
		[DataMember]
		public Guid uid { get; set; }
	}
}
