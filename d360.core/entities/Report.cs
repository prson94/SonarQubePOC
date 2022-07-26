using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;
using d360.core.exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Linq;
using System.Web;
using d360.core.resources;

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
		public int? AssetTypeID { get; set; }

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

		public DashboardApiGetModel ToApiDashboardGetModel()
		{
			var model = new DashboardApiGetModel();
			model.Id = this.ID;
			model.Name = this.Name;
			model.uid = this.uid;
			return model;
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
		public Guid? AssetTypeUid { get; set; }
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
		[JsonIgnore]
		public string _responsibilities { get; set; }
	}

	public class DashboardDefinition
	{
		[DataMember]
		public string url { get; set; }
		[DataMember]
		public string fileName { get; set; }
		[DataMember]
		public string powerBiReportId { get; set; }
		[DataMember]
		public string powerBiDatasetId { get; set; }
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

	public class DashboardApiUpsertModel : DashboardModel
	{
		[NotMapped]
		public int AssetTypeId { get; set; }

		public Guid? Uid { get; set; }

		public void FillDataFromFormData(NameValueCollection formData)
		{
			if (!string.IsNullOrEmpty(formData["assettypeuid"]))
			{
				Guid value;
				Guid.TryParse(formData["assettypeuid"], out value);
				this.AssetTypeUid = value;
			}
			if (!string.IsNullOrEmpty(formData["dashboardtype"]))
			{
				try
				{
					this.DashboardType = (DashboardType)Enum.Parse(typeof(DashboardType), formData["dashboardtype"], true);
				}
				catch
				{
					throw new GenericException(System.Net.HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, DashboardMessages.InvalidDashboardType);
				}
			}
			if (!string.IsNullOrEmpty(formData["definition"]))
			{
				try
				{
					this.Definition = JsonConvert.DeserializeObject<DashboardDefinition>(formData["definition"]);
				}
				catch
				{
					throw new GenericException(System.Net.HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, DashboardMessages.InvalidDefinitionValue);
				}
			}
			if (!string.IsNullOrEmpty(formData["description"]))
			{
				this.Description = formData["description"];
			}
			if (!string.IsNullOrEmpty(formData["uid"]))
			{
				Guid value;
				Guid.TryParse(formData["uid"], out value);
				this.Uid = value;
			}
			if (!string.IsNullOrEmpty(formData["location"]))
			{
				try
				{
					this.Location = (DashboardLocation)Enum.Parse(typeof(DashboardLocation), formData["location"], true);
				}
				catch
				{
					throw new GenericException(System.Net.HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, DashboardMessages.InvalidDashboardLocation);
				}
			}
			if (!string.IsNullOrEmpty(formData["name"]))
			{
				this.Name = formData["name"];
			}
			if (!string.IsNullOrEmpty(formData["responsibilities"]))
			{
				this.Responsibilities = JsonConvert.DeserializeObject<List<Guid>>(formData["responsibilities"]);
			}
		}
	}

	public class PowerBiCredentials
	{
		[DataMember]
		public string Username { get; set; }
		[DataMember]
		public string Password { get; set; }
	}

	public class DashboardApiGetModelFilter
	{
		private Guid? uid = null;
		private DashboardLocation? location = null;
		private int? id = null;
		private Guid? assetTypeUid = null;
		private Guid? assetUid = null;

		private List<string> errors = new List<string>();

		public Guid? Uid { get { return this.uid; } }
		public DashboardLocation? Location { get { return this.location; } }
		public int? Id { get { return this.id; } }
		public Guid? AssetTypeUid { get { return this.assetTypeUid; } }
		public Guid? AssetUid { get { return this.assetUid; } }
		public List<string> Errors { get { return this.errors; } }

		public DashboardApiGetModelFilter(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			if (queryParams != null)
			{
				if (queryParams.Any(x => x.Key.ToLowerInvariant() == "uid")
					&& !string.IsNullOrEmpty(queryParams.FirstOrDefault(x => x.Key.ToLowerInvariant() == "uid").Value))
				{
					Guid _uid;
					Guid.TryParse(queryParams.FirstOrDefault(x => x.Key.ToLowerInvariant() == "uid").Value, out _uid);
					this.uid = _uid;
					if (this.uid == Guid.Empty)
					{
						this.errors.Add(DashboardMessages.InvalidDashboardUid);
					}
				}

				if (queryParams.Any(x => x.Key.ToLowerInvariant() == "location")
					&& !string.IsNullOrEmpty(queryParams.FirstOrDefault(x => x.Key.ToLowerInvariant() == "location").Value))
				{
					var value = queryParams.FirstOrDefault(x => x.Key.ToLowerInvariant() == "location").Value.ToLowerInvariant().Trim();
					switch (value)
					{
						case "1":
						case "list": this.location = DashboardLocation.List; break;
						case "2":
						case "detail": this.location = DashboardLocation.Detail; break;
						case "3":
						case "homepage": this.location = DashboardLocation.Homepage; break;
						default: this.errors.Add(DashboardMessages.InvalidDashboardLocation); break;
					}
				}

				if (queryParams.Any(x => x.Key.ToLowerInvariant() == "assettypeuid")
					&& !string.IsNullOrEmpty(queryParams.FirstOrDefault(x => x.Key.ToLowerInvariant() == "assettypeuid").Value))
				{
					Guid _uid;
					Guid.TryParse(queryParams.FirstOrDefault(x => x.Key.ToLowerInvariant() == "assettypeuid").Value, out _uid);
					this.assetTypeUid = _uid;
					if (this.assetTypeUid == Guid.Empty)
					{
						this.errors.Add(DashboardMessages.InvalidDashboardDashboardAssetTypeUid);
					}
				}
				if (queryParams.Any(x => x.Key.ToLowerInvariant() == "assetuid")
					&& !string.IsNullOrEmpty(queryParams.FirstOrDefault(x => x.Key.ToLowerInvariant() == "assetuid").Value))
				{
					Guid _uid;
					Guid.TryParse(queryParams.FirstOrDefault(x => x.Key.ToLowerInvariant() == "assetuid").Value, out _uid);
					this.assetUid = _uid;
					if (this.assetUid == Guid.Empty)
					{
						this.errors.Add(DashboardMessages.InvalidDashboardDashboardAssetUid);
					}
				}
				if (queryParams.Any(x => x.Key.ToLowerInvariant() == "id")
					&& !string.IsNullOrEmpty(queryParams.FirstOrDefault(x => x.Key.ToLowerInvariant() == "id").Value))
				{
					int _id;
					int.TryParse(queryParams.FirstOrDefault(x => x.Key.ToLowerInvariant() == "id").Value, out _id);
					this.id = _id;
				}
			}
		}


	}
}