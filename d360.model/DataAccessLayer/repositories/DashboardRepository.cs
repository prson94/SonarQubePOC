using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using d360.core;
using d360.core.entities;
using d360.core.entities.Views;
using d360.core.enums;
using d360.core.exceptions;
using d360.core.resources;
using d360.extensions;
using d360.featureflags;
using d360.model.DataAccessLayer.repositories;

using Dapper;
using DocumentFormat.OpenXml.Bibliography;
using repositories;

namespace d360.model.DataAccessLayer
{
	public class DashboardRepository : BaseRepository, IDashboardRepository
	{
		#region DI

		internal IQueueSource Queue;
		internal IStorageProvider Storage;
		internal ICommunityContext Community;

		public DashboardRepository(
			ICompanyContext companyContext, 
			IQueueSource queue, 
			IStorageProvider storage, 
			ICommunityContext community, 
			IFeatureFlagService ff)
			: base(companyContext, ff)
		{
			Queue = queue;
			Storage = storage;
			Community = community;
		}

		#endregion


		public Task<DashboardApiGetModel> PostDashboardAsync(DashboardApiUpsertModel model)
		{
			ValidateDashboardModel(model);

			var report = new Report();
			report.Name = model.Name;
			report.Description = model.Description.SanitizeHtml();
			report.AssetTypeID = model.AssetTypeId;
			report.AssetTypeUid = model.AssetTypeUid;

			if (model.Definition != null)
			{
				report.Definition = Newtonsoft.Json.JsonConvert.SerializeObject(model.Definition);
			}
			report.ReportType = model.DashboardType.Value;
			report.Location = model.Location.Value;

			CompanyContext.Add(report);
			CompanyContext.SaveChanges();

			UpdateReportResponsibilities(model, report);

			#region "Audit Log"
			try
			{
				addChangeLogDashboard(report, "C");
			}
			catch
			{

			}
			#endregion


			return Task.FromResult(report.ToApiDashboardGetModel());
		}


		public Task<DashboardApiGetModel> PutDashboardAsync(DashboardApiUpsertModel model)
		{
			string currentStep = "";
			Report report = null;
			Report nowPreviousreport = null;
			try
			{
				currentStep = "Update->ValidateDashboardModel";
				
				ValidateDashboardModel(model);

				currentStep = "Update->Report";
				report = CompanyContext.Reports.FirstOrDefault(x => x.uid == model.Uid);
				if (report == null)
				{
					throw new GenericException(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, String.Format(FieldErrors.InvalidAssetTypeUid, model.AssetTypeUid));
				}

				nowPreviousreport = report.CloneThis();


				report.Name = model.Name;
				report.Description = model.Description.SanitizeHtml();
				report.AssetTypeID = model.AssetTypeId;
				report.AssetTypeUid = model.AssetTypeUid;

				currentStep = "Update->Definition";

				if (model.Definition != null)
				{
					report.Definition = Newtonsoft.Json.JsonConvert.SerializeObject(model.Definition);
				}
				report.ReportType = model.DashboardType.Value;
				report.Location = model.Location.Value;

				currentStep = "Update->SaveChanges";

				CompanyContext.SaveChanges();


				currentStep = "Update->UpdateResponsibility";
				UpdateReportResponsibilities(model, report);
			}
			catch (Exception ex)
			{
				string errorMessage = currentStep + ":" + ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
				errorMessage = errorMessage.Length <= 2000 ? errorMessage : errorMessage.Substring(0, 2000);
				throw new GenericException(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, errorMessage);
			}

			#region "Audit Log"
			try
			{
				addChangeLogDashboard(report, "U", nowPreviousreport);
			}
			catch 
			{

			}
			#endregion
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
				throw new GenericException(HttpStatusCode.NotFound, AssetTypeErrors.InvalidRequestHttpErrorTitle, String.Format(Messages.AssetTypeNotFound, model.AssetTypeUid));
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

			if(!string.IsNullOrEmpty(model.Definition?.url))
			{
				var validProtools = new string[] { "http", "https", "mailto" };
				var colonPos = model.Definition.url.IndexOf(":");
				if (colonPos > 2) //Allow a file path with a one letter drive
				{
					var protocol = model.Definition.url.Substring(0, colonPos).ToLower();
					if(!validProtools.Contains(protocol))
					{
						throw new GenericException(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, DashboardMessages.InvalidDashboardURL);
					}
				}
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
				throw new GenericException(HttpStatusCode.NotFound, AssetTypeErrors.InvalidRequestHttpErrorTitle, DashboardMessages.DashboardNotFound);
			}


			CompanyContext.Database.Connection
				.Query(@"
					delete from ReportResponsibility where ReportId = @reportId;
					delete from Report where Id = @reportId", new { reportId = dashboard.ID });


			#region "Audit Log"
			try
			{
				addChangeLogDashboard(dashboard, "D");
			}
			catch
			{

			}
			#endregion
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
			if (responsibilities.Count == 0)
			{
				CompanyContext.Database.Connection.Query(@"
					delete from dbo.ReportResponsibility where ReportID = @reportId",
					new { reportId = report.ID, responsibilities });
			}
			else
			{
				CompanyContext.Database.Connection.Query(@"
					delete from dbo.ReportResponsibility where ReportID = @reportId;

					insert into dbo.ReportResponsibility (ReportID, ResponsibilityTypeID)
					select @reportId, rt.ID as responsibilitytypeid from dbo.ResponsibilityType rt
					where rt.uid in @Responsibilities",
					new { reportId = report.ID, responsibilities });
			}
		}

		private void addChangeLogDashboard(Report current, string action, Report previous = null)
		{
			int deleteSameValue = 0;

			switch (action)
			{
				case "C":
					action = "Created";
					break;
				case "U":
					deleteSameValue = 1;
					action = "Updated";
					break;
				case "D":
				case "R":
					action = "Removed";
					break;
				default:
					// No action, leave the value as is.
					break;
			}
			var audit = new Audit
			{
				AuditFields = new List<AuditField>(),
				Date = (DateTime)((DateTime)current.UpdatedOn == null ? DateTime.Now : current.UpdatedOn),
				ActionDescription = $"Report {action.ToLower(System.Globalization.CultureInfo.InvariantCulture)}.",
				Action = action,
				ActionObjectID = current.ID,
				ActionObject = "Report",
				ActionObjectName = current.Name,
				ActionObjectTypeName = "Report",
				Object = "Report",
				ObjectID = current.ID,
				ObjectName = current.Name,
				ResourceID = (int)((int)current.UpdatedBy == 0 ? CompanyContext.CurrentResourceID : current.UpdatedBy),
				Version = 0
			};

			if (action == "Created" || (action == "Updated"))
			{ 
				audit.AuditFields.Add(new AuditField { FieldName = "Name", PreviousValue = ((previous != null) ? previous.Name : null), Value = current.Name, FieldTypeID = 0 });
				audit.AuditFields.Add(new AuditField { FieldName = "Description", PreviousValue = ((previous != null) ? previous.Description : null), Value = current.Description, FieldTypeID = 0 });
				audit.AuditFields.Add(new AuditField { FieldName = "ReportType", PreviousValue = ((previous != null) ? ((int)previous.ReportType).ToString() : null), Value = ((int)current.ReportType).ToString(), FieldTypeID = 0 });
				audit.AuditFields.Add(new AuditField { FieldName = "AssetTypeID", PreviousValue = ((previous != null) ? previous.AssetTypeID.ToString() : null), Value = current.AssetTypeID.ToString(), FieldTypeID = 0 });
				audit.AuditFields.Add(new AuditField { FieldName = "Location", PreviousValue = ((previous != null) ? ((int)previous.Location).ToString() : null), Value = ((int)current.Location).ToString(), FieldTypeID = 0 });
				audit.AuditFields.Add(new AuditField { FieldName = "Definition", PreviousValue = ((previous != null) ? previous.Definition : null), Value = current.Definition, FieldTypeID = 0 });
			}
			CompanyContext.Add(audit);

			CompanyContext.Connection.Execute(@"
												update	T
												set		T.Version = coalesce(S.[maxversion],0) + 1
												from	[reporting].[Global_Audit] T
												outer apply (
															select	max(version) as [maxversion]
															from	[reporting].[Global_Audit] A  
															where A.Object = T.Object 
															and A.ObjectID = T.ObjectID
														) S
												where   T.ID = @ID and T.[Version] = 0

												if (@deleteSameValue = 1)
												begin
													delete f
													from [reporting].[Global_FieldAudit] f
													where auditid = @ID and coalesce(value,'') = coalesce(Previousvalue,'')
												end
												", new { audit.ID, deleteSameValue});
		}
		
	}
}
