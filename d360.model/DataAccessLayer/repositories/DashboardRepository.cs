using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using d360.core;
using d360.core.entities;
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

		public async Task<List<DashboardApiGetModel>> GetDashboardsAsync()
		{

			return (await CompanyContext.Database.Connection
				.QueryAsync<DashboardApiGetModel>(@"
					select r.uid, r.Name, r.Description, at.uid as assetTypeUid, r.ReportType as DashboardType, r.Location, Definition as '_definitionJson'
					from dbo.Report r
					left join assettype at on at.id = r.AssetTypeID")).ToList();
		}
	}
}
