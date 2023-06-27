using d360.core.entities;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace d360.model
{
	public partial interface ICompanyContext : IBaseContext
	{
		#region Methods
		
		Task<List<AssetCrossReferenceResult>> GetExecutionCrossReferenceResultsAsync(Guid executionId);
		
		Task ImportCrossReferencesAsync(ApiExecution execution, IEnumerable<AssetCrossReference> import, int timeout = 3600);
		
		#endregion
	}

	public partial class CompanyContext : BaseContext, ICompanyContext
	{
		#region Utility

		private void ValidateAssetCrossReference(ApiExecution execution, int timeout = 3600)
		{
			Connection.Execute(@"Update api.ExecutionAssetCrossReference
								Set Success=0,
								Message='Does not contain valid Uid.' 
								Where ExecutionID = @executionID and Success is null and
								(Uid is null or  UID ='00000000-0000-0000-0000-000000000000' ) ", new { executionID = execution.ExecutionID }, commandTimeout: timeout);



			Connection.Execute(@"Update api.ExecutionAssetCrossReference
								Set Success=0,
								Message='DataSource is required.' 
								Where ExecutionID = @executionID and Success is null and
								( DataSource is null or Trim(DataSource) ='') ", new { executionID = execution.ExecutionID }, commandTimeout: timeout);



			Connection.Execute(@"Update api.ExecutionAssetCrossReference
								Set Success=0,
								Message='Type is required.' 
								Where ExecutionID = @executionID and Success is null and
								([Type] is null  or TRIM([Type]) = '' ) ", new { executionID = execution.ExecutionID }, commandTimeout: timeout);

			Connection.Execute(@"Update api.ExecutionAssetCrossReference
								Set Success=0,
								Message='ExternalID is required.' 
								Where ExecutionID = @executionID and Success is null and
								( ExternalID is null or TRIM(ExternalID) ='') ", new { executionID = execution.ExecutionID }, commandTimeout: timeout);

			Connection.Execute(@"Update api.ExecutionAssetCrossReference
								Set Success=0,
								Message='Does not contain required fields.' 
								Where ExecutionID = @executionID and Success is null and
								(Uid is null or DataSource is null or [Type] is null or ExternalID is null
								or UID ='00000000-0000-0000-0000-000000000000' or Trim(DataSource) ='' or TRIM([Type]) = '' or TRIM(ExternalID) ='') ", new { executionID = execution.ExecutionID }, commandTimeout: timeout);

			Connection.Execute(@"
					Update  ECR
					SET Success=0,
					Message='Asset cross reference already exists'
					from api.ExecutionAssetCrossReference ECR
					Where ECR.ExecutionID = @executionID and Success is null and exists (Select 1 from AssetCrossReference where UID=ECR.UID and DataSource= ECR.DataSource and
					[Type]=ECR.[Type] and ExternalID =ECR.ExternalID)",
						new { executionID = execution.ExecutionID }, commandTimeout: timeout);

			Connection.Execute(@"
					Update ECR
						Set Success=0,
						Message ='Duplicate asset cross reference;'
						From api.ExecutionAssetCrossReference ECR
						inner join 
						(Select Uid,DataSource,Type,ExternalID from api.ExecutionAssetCrossReference
						where Success is null and ExecutionID=@executionID
						group by Uid,DataSource,Type,ExternalID
						having(count(*)>1)) T on
						ECR.[Uid] = T.[UID] and
						ECR.DataSource = T.DataSource and
						ECR.[Type] = T.[Type] and
						ECR.ExternalID = T.ExternalID
						Where ECR.Success is null  and ExecutionID=@executionID ",
						new { executionID = execution.ExecutionID }, commandTimeout: timeout);
		}

		#endregion

		#region Methods

		public async Task<List<AssetCrossReferenceResult>> GetExecutionCrossReferenceResultsAsync(Guid executionId)
		{
			var qry = await Connection.QueryAsync<AssetCrossReferenceResult>(
				"select ItemNumber, Uid, Message, Success from [api].[ExecutionAssetCrossReference] where ExecutionID = @executionId",
				new { executionId }
			);

			return qry.ToList();
		}

		public async Task ImportCrossReferencesAsync(ApiExecution execution, IEnumerable<AssetCrossReference> import, int timeout = 3600)
		{
			SetApiExecutionProcessingStartTime(execution.ExecutionID);

			#region Build data tables for bulk load

			DataTable table = new DataTable();
			table.Columns.Add("ExecutionID", typeof(Guid));
			table.Columns.Add("ItemNumber", typeof(int));
			table.Columns.Add("Uid", typeof(Guid));
			table.Columns.Add("DataSource", typeof(string));
			table.Columns.Add("Type", typeof(string));
			table.Columns.Add("ExternalID", typeof(string));
			table.Columns.Add("FieldHash", typeof(string));
			table.Columns.Add("Message", typeof(string));
			table.Columns.Add("Success", typeof(bool));

			int i = 0;
			foreach (AssetCrossReference item in import)
			{
				DataRow row = table.NewRow();

				row["ExecutionID"] = execution.ExecutionID;
				row["ItemNumber"] = i++;
				row["uid"] = item.uid;
				row["DataSource"] = item.DataSource != null ? item.DataSource.Trim() : item.DataSource;
				row["Type"] = item.Type != null ? item.Type.Trim() : item.Type;
				row["ExternalID"] = item.ExternalID != null ? item.ExternalID.Trim() : item.ExternalID;
				row["FieldHash"] = item.FieldHash;

				table.Rows.Add(row);
			}

			#endregion

			try
			{
				await Connection.OpenIfClosed();

				#region Bulk Copy

				using (SqlBulkCopy bulkCopy = new SqlBulkCopy(Connection)
				{
					BatchSize = SqlBulkBatchSize,
					DestinationTableName = "api.ExecutionAssetCrossReference",
					BulkCopyTimeout = SqlBulkBatchTimeout
				})
				{

					bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
					bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
					bulkCopy.ColumnMappings.Add("uid", "uid");
					bulkCopy.ColumnMappings.Add("DataSource", "DataSource");
					bulkCopy.ColumnMappings.Add("Type", "Type");
					bulkCopy.ColumnMappings.Add("ExternalID", "ExternalID");
					bulkCopy.ColumnMappings.Add("FieldHash", "FieldHash");


					bulkCopy.WriteToServer(table);
				}

				#endregion

				ValidateAssetCrossReference(execution, timeout);

				Connection.Execute(@"
						insert into AssetCrossReference
						(Uid,DataSource,Type,ExternalID,FieldHash)
						Select Uid,DataSource,Type,ExternalID,FieldHash from api.ExecutionAssetCrossReference
						Where ExecutionID=@executionID and Success is null;

						Update api.ExecutionAssetCrossReference
						Set Success =1,
						Message ='Added Successfully'
						Where ExecutionID=@executionID and Success is null; ",
					new { executionID = execution.ExecutionID }, commandTimeout: timeout);

				CompleteApiExecutionAndGetCounts(execution.ExecutionID, "ExecutionAssetCrossReference");
			}
			finally
			{
				Connection.CloseIfOpened();
			}
		}

		#endregion
	}
}
