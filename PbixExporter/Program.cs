using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.PowerBI.Api.V2;
using Microsoft.Rest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PbixExporter
{
	/*
	 * SQL to generate the data we need.
select	Value as GroupId,
		(
		select	json_arrayAgg(json_value(Definition, '$."powerBiDatasetId"'))
		from	Report
		) as DatasetIds,
		(
		select	json_arrayAgg(json_value(Definition, '$.powerBiReportId'))
		from	Report
		) as ReportIds
from	Setting 
where	ID = 56	 
	 */
	internal class Program
    {
		static async Task<PowerBIClient> CreateClient(AuthenticationResult auth = null)
		{
			if (auth == null)
			{
				var credential = new UserPasswordCredential("svc-d3spowerbi@infogix.com", "20Infogix18!");

				// Authenticate using created credentials
				var authenticationContext = new AuthenticationContext("https://login.microsoftonline.com/02292cae-2fe6-4371-8da1-b03d14808575");
				var authenticationResult = await authenticationContext.AcquireTokenAsync("https://analysis.windows.net/powerbi/api", "2ec97ecb-f620-40ba-a109-afcd2e89be0f", credential);

				if (authenticationResult == null)
				{
					throw new Exception("authentication failed");
				}

				var tokenCredentials = new TokenCredentials(authenticationResult.AccessToken, "Bearer");

				return new PowerBIClient(new Uri("https://api.powerbi.com"), tokenCredentials);
			}
			else
			{
				var tokenCredentials = new TokenCredentials(auth.AccessToken, "Bearer");

				return new PowerBIClient(new Uri("https://api.powerbi.com"), tokenCredentials);
			}
		}

		static async Task RemoveOrphanedReports(string groupId, List<string> validReportIds)
		{
			using (var client = await CreateClient())
			{
				var rpts = client.Reports.GetReports(groupId);
				foreach (var report in rpts.Value)
				{
					if (!validReportIds.Contains(report.Id))
					{ 
						client.Reports.DeleteReport(groupId, report.Id);
					}
				}
			}
		}

		static async Task RemoveOrphanedDatasets(string groupId, List<string> validDatasetIds)
		{
			using (var client = await CreateClient())
			{
				var items = client.Datasets.GetDatasets(groupId);
				foreach (var item in items.Value)
				{
					if (!validDatasetIds.Contains(item.Id))
					{
						client.Datasets.DeleteDatasetByIdInGroup(groupId, item.Id);
					}
				}
			}
		}

		static async Task ExportReports()
		{
			using (var client = await CreateClient())
			{
				var groups = await client.Groups.GetGroupsAsync();
				foreach (var group in groups.Value)
				{
					var rpts = client.Reports.GetReports(group.Id);
					foreach (var report in rpts.Value)
					{
						if (!Directory.Exists($"{group.Id}"))
						{
							Directory.CreateDirectory($"{group.Id}");
						}

						var reportName = Regex.Replace(report.Name, @"[^A-Za-z0-9\s]", "");
						var reportPath = $"{group.Id}\\{reportName}-{report.Id}.pbix";

						if (!File.Exists(reportPath))
						{
							using (var memStream = new MemoryStream())
							{
								try
								{
									var reportStream = client.Reports.ExportReport(group.Id, report.Id);
									reportStream.CopyTo(memStream);
									var bytes = memStream.ToArray();
									reportStream.Dispose();

									File.WriteAllBytes(reportPath, bytes);
								}
								catch
								{
									Console.WriteLine($"Failed to get report at Path: {reportPath}");
								}
							}
						}
					}
				}

				//reports.ForEach(reportId =>
				//{
				//	using (var memStream = new MemoryStream())
				//	{
				//		var reportStream = client.Reports.ExportReport(groupId.ToString(), reportId.ToString());
				//		reportStream.CopyTo(memStream);
				//		var bytes = memStream.ToArray();
				//		reportStream.Dispose();

				//		File.WriteAllBytes($"{reportId}.pbix", bytes);
				//	}
				//});
			}
		}

		static async Task Main(string[] args)
        {
			//await ExportReports();
			var stream = File.OpenText("payload.json");
			var json = await stream.ReadToEndAsync();
			stream.Dispose();

			var groups = JsonConvert.DeserializeObject<List<GroupModel>>(json);
			foreach (var group in groups) {
				await RemoveOrphanedDatasets(group.groupId, group.datasetIds);
				await RemoveOrphanedReports(group.groupId, group.reportIds);
			}

			//await RemoveOrphanedDatasets(
			//	"b3caca78-3f06-43ba-87cd-30bdd3a5663a",
			//	new List<string> {
			//		"0a2c25f0-3d6f-4b15-bba1-7ffd7a09d02f",
			//		"58e80b39-5392-4b02-98ae-21a2921d3fe0",
			//		"5124a21b-d033-41bc-abd9-e64a0cd145bc"
			//	}
			//);

			//await RemoveOrphanedReports(
			//	"b3caca78-3f06-43ba-87cd-30bdd3a5663a",
			//	new List<string> {
			//		"806012cd-c95d-4ebe-bda5-884bc58ad6ef",
			//		"bfd540ff-2dba-4b71-85a3-4c34bbeda18e",
			//		"3f8dd3ad-804c-4190-b910-870a953f337b"
			//	}
			//);
		}
    }
}
