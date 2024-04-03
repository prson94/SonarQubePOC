using d360.core.entities;
using d360.core.queue;
using d360.core.resources;
using d360.extensions;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using repositories.azure;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace igx.jobs.apiexecutionprocessor
{
	public class NotificationHandler : BaseWebJob
	{
		const string FUNCTION_NAME = "NotificationHandler";
		readonly IMailProvider Mail;

		public NotificationHandler(IConfiguration config, IMailProvider mail) : base(config)
		{
			Mail = mail;
		}

		[FunctionName(FUNCTION_NAME)]
		public async Task Run([QueueTrigger(constants.Queue.Notification, Connection = constants.Setting.Storage)] string payload, ILogger log)
		{
			var info = JsonConvert.DeserializeObject<QueueMessage<int>>(payload);
			var logProperties = new Dictionary<string, object> {
				{ "Function", FUNCTION_NAME },
				{ "CompanyID", info.CompanyId }
			};

			using (log.BeginScope(logProperties))
			{
				var community = new Community(ConnString);
				var tenantConnectionString = await community.GetConnectionStringForTenantAsync(info.CompanyId);
				var conn = new SqlConnection(tenantConnectionString);

				try
				{
					var resourceSql =
"select distinct * " +
"from (" +
"select gr.* from CommentRelation cr inner join Asset a on a.ID = cr.AssetId inner join reporting.Global_Resource gr on a.Object = 'Resource' and gr.ResourceId = a.ObjectId and cr.CommentId = @CommentId " +
"union " +
"select gr.* from CommentRelation cr inner join Asset a on a.ID = cr.AssetId inner join ResourceGroup rg on a.Object = 'Group' and rg.GroupID = a.ObjectId inner join reporting.Global_Resource gr on gr.ResourceId = rg.ResourceID and cr.CommentId = @CommentId " +
") a;";

					await conn.OpenAsync();

					var query = await conn.QueryMultipleAsync(
						"select * from Comment where ID = @CommentId; " +
						resourceSql +
						"select GR.FirstName + ' ' + GR.LastName as ResourceName from reporting.Global_Resource gr inner join Comment c on c.CreatedBy = gr.ResourceID and c.ID = @CommentId;" +
						"select * from AssetDetail d inner join Comment c on c.ID = @CommentId and c.AssetID = d.ID;",
						new { CommentId = info.Payload }
					);
					var comment = await query.ReadFirstAsync<Comment>();
					var taggedUsers = await query.ReadAsync<GlobalReportingResource>();
					var commenterName = await query.ReadFirstAsync<string>();
					var asset = await query.ReadFirstAsync<AssetDetail>();

					if (taggedUsers.Count() > 0)
					{
						var rootUrl = $"https://{info.CompanyPrefix}.data3sixty.com";
						var assetUrl = $"{rootUrl}/asset/{asset.uid}";
						var commentUrl = $"{rootUrl}/asset/{asset.uid}/comments";

						var subject = string.Format(Notifications.TaggedCommentMailSubject, commenterName, asset.DisplayValue);
						var heading = string.Format(Notifications.TaggedCommentMailHeader, commenterName);
						var body = string.Format(
							Notifications.TaggedCommentMailBody, 
							commenterName, 
							assetUrl,
							asset.DisplayValue, 
							comment.CreatedOn.Value.ToString("hh:mm tt 'UTC' 'on' dd MMM yyyy")
						);

						foreach (var user in taggedUsers)
						{
							Mail.SendMessage(
								subject,
								user.Email,
								$"{user.FirstName} {user.LastName}",
								new Dictionary<string, string> {
									{ "subject", subject },
									{ "notify_header", heading },
									{ "notify_message", body },
									{ "comment_link", commentUrl },
									{ "comment_link_text", Notifications.TaggedCommentMailCommentLink }
								},
								"tagged-in-comment-notification"
							);
						}
					}
				}
				catch (Exception ex)
				{
					log.LogError(ex, "Error in {FUNCTION_NAME}, on try/catch retry connection attempt.", FUNCTION_NAME);
				}
			}
		}

	}
}
