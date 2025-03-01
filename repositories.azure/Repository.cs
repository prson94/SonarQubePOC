using d360.core.entities;
using Dapper;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;

namespace repositories.azure
{
	public abstract class Repository
	{
		public int CurrentUserId { get; set; }

		public Platform Platform { get { return Platform.Azure; } }

		public DapperConnectionProvider ConnectionProvider { get; set; }

		protected Repository(DapperConnectionProvider provider)
		{
			ConnectionProvider = provider;
		}

		protected int CommandTimeout
		{
			get
			{
				int commandTimeout;
				if (!int.TryParse(ConnectionProvider.CommandTimeOut, out commandTimeout))
				{
					commandTimeout = 90;
				}
				return commandTimeout;
			}
		}

		protected async Task<GlobalReportingResource> GetUser(int? resId)
		{
			try
			{
				var parameters = new
				{
					ResourceId = resId
				};
				using (var connection = (SqlConnection)ConnectionProvider.Connect(true))
				{
					var user = await connection.QueryFirstOrDefaultAsync<GlobalReportingResource>(
					 @"SELECT 
						g.ResourceID, g.uid as Uid,
						g.LastLoggedInOn,g.State, g.IsAdministrator,
						g.FirstName, g.LastName, g.Email,
						g.CreatedOn, g.UpdatedOn
						from reporting.Global_Resource g
						where g.ResourceID = @ResourceId",
					 parameters,
					 commandTimeout: CommandTimeout
						);

					return user;
				}
			}
			catch (Exception)
			{

				throw;
			}
		}

		protected async Task UpdateExecutionWithErrorFromException(ApiExecution execution, Exception ex)
		{
			try
			{
				string message = GetFullExceptionData(ex, false);
				execution.ErrorMessage = message;
				execution.CompletedOn = DateTime.UtcNow;
				using (var connection = (SqlConnection)ConnectionProvider.Connect())
				{
					await connection.ExecuteAsync($@"
					update api.execution
					set ErrorMessage = @message, CompletedOn = @date
					where executionid = @ExecutionID", new { execution.ExecutionID, message, date = DateTime.UtcNow });
				}
			}
			catch (Exception)
			{
				throw;
			}
		}

		protected async Task UpsertApiExecution(ApiExecution execution)
		{
			try
			{
				if (execution.ExecutionID == Guid.Empty)
				{
					execution.ExecutionID = Guid.NewGuid();
				}

				string sql = $@"
				if (@id = 0)
					begin
						insert into api.execution (executionid,resourceid,total,Processed,
						Error,StartedOn,CompletedOn,Fields,ErrorMessage,Method,Route,
						ProcessingStartedOn,State,ApplicationID,RetryCount,Action)
						values (@executionid,@resourceid,@total,@Processed,
						@Error,@StartedOn,@CompletedOn,@Fields,@ErrorMessage,@Method,@Route,
						@ProcessingStartedOn,@State,@ApplicationID,@RetryCount,@Action);
					end
				else
					begin
						update e
						set executionid = @executionid,
						resourceid = @resourceid,
						total = @total,
						Processed = @Processed,
						Error = @Error,
						StartedOn = @StartedOn,
						CompletedOn = @CompletedOn,
						Fields = @Fields,
						ErrorMessage = @ErrorMessage,
						Method = @Method,
						Route = @Route,
						ProcessingStartedOn = @ProcessingStartedOn,
						State = @State,
						ApplicationID = @ApplicationID,
						RetryCount = @RetryCount,
						Action = @Action
						from api.execution e
						where e.id = @id;
					end
				";

				using (var connection = (SqlConnection)ConnectionProvider.Connect())
				{
					await connection.ExecuteAsync(sql, new {
						execution.Id,
						execution.ExecutionID,
						execution.ResourceID,
						execution.Total,
						execution.Processed,
						execution.Error,
						execution.StartedOn,
						execution.CompletedOn,
						execution.Fields,
						execution.ErrorMessage,
						execution.Method,
						execution.Route,
						execution.ProcessingStartedOn,
						execution.State,
						execution.ApplicationId,
						execution.RetryCount,
						execution.Action
					});
				}
			}
			catch (Exception)
			{
				throw;
			}
		}


		private string GetFullExceptionData(Exception ex, bool includeStacktrace = true, int characterLimit = 2000)
		{
			StringBuilder sb = new StringBuilder();
			bool isSqlException = (ex.InnerException != null && ex.InnerException.InnerException != null && ex.InnerException.InnerException.GetType() == typeof(SqlException));

			if (isSqlException)
			{
				SqlException sqlException = (SqlException)ex.InnerException.InnerException;

				foreach (SqlError sqlError in sqlException.Errors)
				{
					if (sb.Length > 0)
					{
						sb.Append(" ");
					}

					sb.Append(sqlError.Message);
				}
			}
			else
			{
				if (!ex.Message.Contains("inner exception for details"))
				{
					sb.Append(ex.Message);
				}

				var iex = ex.InnerException;
				while (iex != null)
				{
					sb.Append("; ");
					sb.Append(iex.Message);
					if (includeStacktrace)
					{
						sb.Append("-----");
						sb.Append(iex.StackTrace);
					}
					iex = iex.InnerException;
				}
			}

			if (characterLimit == -1)
			{
				return sb.ToString();
			}
			else
			{
				string message = sb.ToString().Substring(0, Math.Min(characterLimit, sb.Length));
				return message;
			}
		}
	}
}
