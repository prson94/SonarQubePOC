using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.queue;
using d360.core.resources;
using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace d360.model
{
	public partial interface ICompanyContext : IBaseContext
	{
		#region DbSets

		DbSet<AssetDataProfile> AssetDataProfile { get; set; }
		
		#endregion


		#region Methods

		#endregion
	}

	public partial class CompanyContext : BaseContext, ICompanyContext
	{
		#region DbSets

		public DbSet<AssetDataProfile> AssetDataProfile { get; set; }

		public DbSet<AssetDataProfileSample> AssetDataProfileSample { get; set; }

		public DbSet<AssetDataProfileSampleJson> AssetDataProfileSampleJson { get; set; }

		#endregion


		#region Methods

		#endregion
	}
}
