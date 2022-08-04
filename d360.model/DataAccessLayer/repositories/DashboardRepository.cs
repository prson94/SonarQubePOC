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
				throw new GenericException(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, DashboardMessages.InvalidDashboardType);
			}

			if (model.Location == null)
			{
				throw new GenericException(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, DashboardMessages.InvalidDashboardLocation);
			}

			if (string.IsNullOrEmpty(model.Name))
			{
				throw new GenericException(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, Messages.Error_Name_Required);
			}

			if (CompanyContext.Reports.Any(x => x.Name == model.Name && x.uid != model.Uid))
			{
				throw new GenericException(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, DashboardMessages.NameExists);
			}
		}

		public bool DeleteDashboard(Guid? uid)
		{
			if (uid == null || uid == Guid.Empty)
			{
				throw new GenericException(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, DashboardMessages.InvalidDashboardUid);
			}

			var dashboard = CompanyContext.Reports.FirstOrDefault(x => x.uid == uid);
			if (dashboard == null)
			{
				throw new GenericException(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, String.Format(DashboardMessages.DashboardNotFound, uid));
			}


			CompanyContext.Database.Connection
				.Query(@"
					delete from ReportResponsibility where ReportId = @reportId;
					delete from Report where Id = @reportId", new { reportId = dashboard.ID });
			return true;
		}

		public async Task<List<DashboardApiGetModel>> GetDashboardsAsync(DashboardApiGetModelFilter filters)
		{
			var dbArgs = new DynamicParameters();
			List<string> whereStatements = new List<string>();
			bool isTypePage = false;
			Asset asset = null;
			int assetTypeId = 0;

			if (filters.AssetTypeUid.HasValue)
			{
				dbArgs.Add("assetTypeUid", filters.AssetTypeUid.Value);
				assetTypeId = CompanyContext.AssetTypes.FirstOrDefault(x => x.uid == filters.AssetTypeUid.Value).ID;
				whereStatements.Add("at.uid = @assetTypeUid");
				isTypePage = true;
			}

			if (filters.AssetUid.HasValue)
			{
				asset = CompanyContext.Assets.FirstOrDefault(x => x.uid == filters.AssetUid);
				assetTypeId = asset.AssetTypeID;
				dbArgs.Add("assetTypeId", assetTypeId);
				whereStatements.Add("at.id = @assetTypeId");
				isTypePage = false;
			}

			if (filters.Uid.HasValue)
			{
				dbArgs.Add("uid", filters.Uid);
				whereStatements.Add("r.uid = @uid");
			}

			if (filters.Location.HasValue)
			{
				dbArgs.Add("location", (int)filters.Location);
				whereStatements.Add("r.location = @location");
			}

			if (filters.Id.HasValue)
			{
				dbArgs.Add("id", filters.Id.Value);
				whereStatements.Add("r.id = @id");
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

			if (filters.AssetTypeUid.HasValue || filters.AssetUid.HasValue)
			{
				FilterDashboardsByResponsibilities(isTypePage, asset, assetTypeId, data);
			}

			return data;
		}

		private void FilterDashboardsByResponsibilities(bool isTypePage, Asset asset, int assetTypeId, List<DashboardApiGetModel> data)
		{
			string responsibilityWhereStatement = "";
			var dbArgs = new DynamicParameters();

			dbArgs.Add("assettypeid", assetTypeId);
			dbArgs.Add("resourceid", CompanyContext.CurrentResourceID);
			dbArgs.Add("assetid", asset?.ID);

			if (!isTypePage)
			{
				responsibilityWhereStatement = "(rd.assettypeid = @assettypeid and rd.resourceid = @resourceid and rd.assetid = 0) or (rd.assetid = @assetid and rd.assettypeid = @assettypeid and rd.resourceid = @resourceid)";
			}
			else
			{
				responsibilityWhereStatement = "rd.assettypeid = @assettypeid and rd.resourceid = @resourceid";
			}

			var responsibilitySQL = @$"select distinct rt.uid from dbo.responsibilitydetail rd
inner join dbo.ResponsibilityType rt on rt.ID = rd.ResponsibilityTypeID where {responsibilityWhereStatement}";

			var currentUserResponsibilityTypeUidList = CompanyContext.Database.Connection.Query<Guid>(responsibilitySQL, dbArgs).ToList();

			//check that the current user has access to the current report
			for (int i = data.Count - 1; i >= 0; i--)
			{
				var report = data[i];

				if (report.Responsibilities != null && report.Responsibilities.Count > 0)
				{
					bool userHasAccess = false;

					foreach (var responsibility in report.Responsibilities)
					{
						if (currentUserResponsibilityTypeUidList.Contains(responsibility))
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
