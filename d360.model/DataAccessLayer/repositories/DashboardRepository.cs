using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using d360.core;
using d360.core.entities;
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

		public async Task<List<DashboardApiGetModel>> GetDashboardsAsync(Guid? uid, DashboardLocation? location, int? id)
		{
			var dbArgs = new DynamicParameters();
			List<string> whereStatements = new List<string>();

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

			string whereSql = whereStatements.Count == 0 ? "" : " where " + string.Join(" and ", whereStatements);

			return (await CompanyContext.Database.Connection
				.QueryAsync<DashboardApiGetModel>(@$"
					select r.Id, r.uid, r.Name, r.Description, at.uid as assetTypeUid, r.ReportType as DashboardType, r.Location, Definition as '_definitionJson'
					from dbo.Report r
					left join assettype at on at.id = r.AssetTypeID
					{whereSql}", dbArgs)).ToList();
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
