using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using d360.core.entities;
using d360.core.entities.Views;
using d360.core.enums;
using d360.core.exceptions;
using d360.core.resources;
using d360.extensions;
using d360.model.DataAccessLayer.repositories;

using Dapper;

namespace d360.model.DataAccessLayer
{
	public class DashboardRepository : BaseRepository, IDashboardRepository
	{
		#region DI

		internal ICompanyContext CompanyContext;
		internal IQueueSource QueueSource;
		internal IStorageProvider StorageProvider;
		internal ICommunityContext Community;

		public DashboardRepository(ICompanyContext companyContext, IQueueSource queueSource, IStorageProvider storageProvider, ICommunityContext community)
			: base(companyContext)
		{
			CompanyContext = companyContext;
			QueueSource = queueSource;
			StorageProvider = storageProvider;
			Community = community;
		}

		#endregion


		public Task<DashboardApiGetModel> PostDashboardAsync(DashboardApiUpsertModel model)
		{
			ValidateDashboardModel(model);

			var report = new Report();
			report.Name = model.Name;
			report.Description = model.Description;
			report.AssetTypeID = model.AssetTypeId;
			if (model.Definition != null)
			{
				report.Definition = Newtonsoft.Json.JsonConvert.SerializeObject(model.Definition);
			}
			report.ReportType = model.DashboardType.Value;
			report.Location = model.Location.Value;

			CompanyContext.Add(report);
			CompanyContext.SaveChanges();

			UpdateReportResponsibilities(model, report);

			return Task.FromResult(report.ToApiDashboardGetModel());
		}


		public Task<DashboardApiGetModel> PutDashboardAsync(DashboardApiUpsertModel model)
		{
			ValidateDashboardModel(model);

			var report = CompanyContext.Reports.FirstOrDefault(x => x.uid == model.Uid);
			report.Name = model.Name;
			report.Description = model.Description;
			report.AssetTypeID = model.AssetTypeId;
			if (model.Definition != null)
			{
				report.Definition = Newtonsoft.Json.JsonConvert.SerializeObject(model.Definition);
			}
			report.ReportType = model.DashboardType.Value;
			report.Location = model.Location.Value;

			CompanyContext.SaveChanges();

			UpdateReportResponsibilities(model, report);

			return Task.FromResult(report.ToApiDashboardGetModel());
		}

		public void ValidateDashboardModel(DashboardApiUpsertModel model)
		{
			if (model.AssetTypeUid == null || model.AssetTypeUid == Guid.Empty)
			{
				throw new GenericException(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, String.Format(FieldErrors.InvalidAssetTypeUid, model.AssetTypeUid));
			}

			var assetType = CompanyContext.AssetTypes.Where(x => x.uid == model.AssetTypeUid).Select(x => new { x.uid, x.ID, x.Class }).FirstOrDefault();

			if (assetType == null)
			{
				throw new GenericException(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, String.Format(Messages.AssetTypeNotFound, model.AssetTypeUid));
			}
			model.AssetTypeId = assetType.ID;
			var allowedClasses = new List<AssetTypeClass> { AssetTypeClass.Model, AssetTypeClass.Policy, AssetTypeClass.Rule, AssetTypeClass.BusinessAsset, AssetTypeClass.TechnicalAsset, AssetTypeClass.User };
			if (!allowedClasses.Contains(assetType.Class))
			{
				throw new GenericException(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, String.Format(Messages.AssetTypeInvalidClass, string.Join(",", allowedClasses.Select(x => x.ToString()))));
			}

			if (model.DashboardType == null)
			{
				throw new GenericException(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, Dashboards.InvalidDashboardType);
			}

			if (model.Location == null)
			{
				throw new GenericException(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, Dashboards.InvalidDashboardLocation);
			}

			if (string.IsNullOrEmpty(model.Name))
			{
				throw new GenericException(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, Messages.Error_Name_Required);
			}

			if (CompanyContext.Reports.Any(x => x.Name == model.Name && x.uid != model.Uid))
			{
				throw new GenericException(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, Dashboards.NameExists);
			}
		}

		public bool DeleteDashboard(Guid? uid)
		{
			if (uid == null || uid == Guid.Empty)
			{
				throw new GenericException(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, Dashboards.InvalidDashboardUid);
			}

			var dashboard = CompanyContext.Reports.FirstOrDefault(x => x.uid == uid);
			if (dashboard == null)
			{
				throw new GenericException(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, Dashboards.DashboardNotFound);
			}


			CompanyContext.Database.Connection
				.Query(@"
					delete from ReportResponsibility where ReportId = @reportId;
					delete from Report where Id = @reportId", new { reportId = dashboard.ID });
			return true;
		}

		public async Task<List<DashboardApiGetModel>> GetDashboardsAsync(Guid? uid, DashboardLocation? location, int? id, Guid? assetTypeUid, Guid? assetUid)
		{
			var dbArgs = new DynamicParameters();
			List<string> whereStatements = new List<string>();
			bool isTypePage = false;
			Asset asset = null;
			int assetTypeId = 0;

			if (uid.HasValue)
			{
				dbArgs.Add("uid", uid);
				whereStatements.Add("r.uid = @uid");
			}

			if (location.HasValue)
			{
				dbArgs.Add("location", (int)location);
				whereStatements.Add("r.location = @location");
			}

			if (id.HasValue)
			{
				dbArgs.Add("id", id.Value);
				whereStatements.Add("r.id = @id");
			}

			if (assetTypeUid.HasValue)
			{
				dbArgs.Add("assetTypeUid", assetTypeUid.Value);
				whereStatements.Add("at.uid = @assetTypeUid");
				isTypePage = true;
			}

			if (assetUid.HasValue)
			{
				asset = CompanyContext.Assets.FirstOrDefault(x => x.uid == assetUid);
				assetTypeId = asset.AssetTypeID;
				dbArgs.Add("assetTypeId", assetTypeId);
				whereStatements.Add("at.id = @assetTypeId");
				isTypePage = false;
			}

			string whereSql = whereStatements.Count == 0 ? "" : " where " + string.Join(" and ", whereStatements);
			var data = (await CompanyContext.Database.Connection
				.QueryAsync<DashboardApiGetModel>(@$"select r.Id, 
					r.uid, 
					r.Name, 
					r.Description, 
					at.uid as assetTypeUid, 
					r.ReportType as DashboardType, 
					r.Location, 
					Definition as '_definitionJson',
					Responsibilities.val as '_responsibilities'
					from dbo.Report r
					left join assettype at on at.id = r.AssetTypeID
					outer apply (select string_agg(cast(uid as nvarchar(36)),',') from ResponsibilityType rt 
								inner join dbo.ReportResponsibility rrt on rrt.ResponsibilityTypeID = rt.ID and rrt.ReportID = r.ID
								)Responsibilities(val)
					{whereSql}", dbArgs)).ToList();

			data.ForEach(data =>
			{
				data.Responsibilities = string.IsNullOrEmpty(data._responsibilities) ? null : data._responsibilities.Split(',').Select(x => Guid.Parse(x)).ToList();
			});

			if (!CompanyContext.CurrentResourceIsAdmin)
			{
				FilterDashboardsByResponsibilities(isTypePage, asset, assetTypeId, data);
			}

			return data;
		}

		private void FilterDashboardsByResponsibilities(bool isTypePage, Asset asset, int assetTypeId, List<DashboardApiGetModel> data)
		{
			List<ResponsibilityDetail> currentUserResponsibilityTypeList = new List<ResponsibilityDetail>();

			if (!isTypePage)
			{
				currentUserResponsibilityTypeList = CompanyContext.ResponsibilityDetails.Where(x => x.AssetTypeID == assetTypeId && x.ResourceID == CompanyContext.CurrentResourceID).ToList();

				if (asset != null)
				{
					currentUserResponsibilityTypeList.AddRange(CompanyContext.ResponsibilityDetails.Where(x => x.AssetTypeID == asset.AssetTypeID && x.AssetID == 0 && x.ResourceID == CompanyContext.CurrentResourceID).ToList());
				}
			}
			else
			{
				currentUserResponsibilityTypeList = CompanyContext.ResponsibilityDetails.Where(x => x.AssetTypeID == assetTypeId && x.ResourceID == CompanyContext.CurrentResourceID).ToList();
			}

			var currentUserResponsibilityTypeIDList = new List<int>();

			if (currentUserResponsibilityTypeList != null && currentUserResponsibilityTypeList.Count() > 0)
			{
				currentUserResponsibilityTypeIDList = currentUserResponsibilityTypeList.Select(i => i.ResponsibilityTypeID).ToList();
			}

			//check that the current user has access to the current report
			for (int i = data.Count - 1; i >= 0; i--)
			{
				var report = data[i];

				if (report.Responsibilities != null && report.Responsibilities.Count > 0)
				{
					bool userHasAccess = false;

					foreach (var responsibility in report.Responsibilities)
					{
						if (currentUserResponsibilityTypeIDList.Contains(responsibility.ResponsibilityTypeID))
						{
							userHasAccess = true;
							break;
						}
					}

					if (!userHasAccess)
					{
						data.RemoveAt(i);
					}
				}
			}
		}

		private void UpdateReportResponsibilities(DashboardApiUpsertModel model, Report report)
		{
			var responsibilities = model.Responsibilities ?? new List<Guid>();
			CompanyContext.Database.Connection.Query(@"
					delete from dbo.ReportResponsibility where ReportID = @reportId
					insert into dbo.ReportResponsibility (ReportID, ResponsibilityTypeID)
					select @reportId, rt.ID as responsibilitytypeid from dbo.ResponsibilityType rt
					where rt.uid in @Responsibilities",
				new { reportId = report.ID, responsibilities });
		}
	}
}
