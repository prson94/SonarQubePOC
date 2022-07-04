using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;
using d360.core.exceptions;
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

		[DataMember, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public Guid uid { get; set; }

		[NotMapped, DataMember]
		public string VisibleTo { get; set; }

		public DateTime? UpdatedOn { get; set; }

		public int? UpdatedBy { get; set; }

		[IgnoreDataMember, ForeignKey("ReportID")]
		public virtual ICollection<ReportResponsibility> Responsibilities { get; set; }

		[DataMember]
		public string Definition { get; set; }
		[DataMember]
		public int AssetTypeID { get; set; }

		[DataMember]
		public DashboardType ReportType { get; set; }

		[DataMember]
		public DashboardLocation Location { get; set; }


		[NotMapped, DataMember]
		public DashboardDefinition DashboardDefinition
		{
			get
			{
				if (Definition == null)
				{
					return null;
				}
				return JsonConvert.DeserializeObject<DashboardDefinition>(Definition);
			}
		}
	}
	public enum DashboardType
	{
		PowerBi = 1,
		DqPlus = 2,
		Legacy = 3
	}

	public enum DashboardLocation
	{
		List = 1,
		Detail = 2,
		Homepage = 3
	}

	public class DashboardModel
	{
		[DataMember]
		public List<Guid> Responsibilities { get; set; }

		[DataMember]
		public Guid AssetTypeUid { get; set; }
		[DataMember]
		public string Name { get; set; }
		[DataMember]
		public string Description { get; set; }
		[DataMember]
		[JsonConverter(typeof(StringEnumConverter))]
		public DashboardType? DashboardType { get; set; }
		[DataMember]
		[JsonConverter(typeof(StringEnumConverter))]
		public DashboardLocation? Location { get; set; }
		[DataMember]
		public DashboardDefinition Definition
		{
			get
			{
				if (_definitionJson == null)
				{
					return null;
				}
				return JsonConvert.DeserializeObject<DashboardDefinition>(_definitionJson);
			}

			set
			{
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
		public int Id { get; set; }
		[DataMember]
		public Guid uid { get; set; }
	}

	public class DashboardApiPostModel : DashboardModel
	{

	}
	public class DashboardApiPutModel : DashboardApiPostModel
	{
		[DataMember]
		public Guid uid { get; set; }
	}
}