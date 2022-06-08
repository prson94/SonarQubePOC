using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using d360.utils.company;
using d360.core;
using d360.core.entities;
using d360.core.enums;
using Dapper;
using System.Configuration;
using Azure.Storage.Blobs;
using System.IO;
using Azure.Storage.Blobs.Models;
using System.Data.SqlClient;

namespace ThemeMigration
{
	static class Program
	{
		const string THEME_NAME = "Custom Theme";
		const string PRECISELY_THEME_UID = "AAAAAAAA-0000-0000-0000-000000000001";
		const int DEBUG_COMPANY_ID = 6;
		static void Main(string[] args)
		{
			migrateTheme();
		}

		private static void migrateTheme()
		{
			var companies = CompanyConnectionUtils.GetCompaniesWithDatabaseServerSettings(ConfigurationManager.AppSettings["CommunityContext"]);
			var StorageConnectionString = ConfigurationManager.AppSettings["AzureStorageConnectionString"];

			var targetCompanies = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["TargetCompanies"]) ? ConfigurationManager.AppSettings["TargetCompanies"].Split(',') : null;

			if(targetCompanies != null)
			{
				companies = companies.FindAll(c => targetCompanies.Contains(c.CompanyID.ToString()));
			}

			BlobServiceClient serviceClient = new BlobServiceClient(StorageConnectionString);

			var iconContainer = serviceClient.GetBlobContainerClient("company-icons");
			var logoContainer = serviceClient.GetBlobContainerClient("company-logos");
			var backgroundContainer = serviceClient.GetBlobContainerClient("company-resources");
			var cssContainer = serviceClient.GetBlobContainerClient("company-styles");

			companies.ForEach(x => {
				Console.WriteLine("Company ID:" + x.CompanyID);
				Console.WriteLine("Company Prefix:" + x.UrlPrefix);

				try
				{
					var cnn = CompanyConnectionUtils.GetCompanyConnection(x.CompanyID, ConfigurationManager.AppSettings["CommunityContext"]);
					var preciselyTheme = cnn.QueryFirstOrDefault<Theme>("select * from theme where Uid = @uid", new { uid = PRECISELY_THEME_UID });

					var items = cnn.Query<CompanySetting>($"select ID, Value from setting where id in ({Convert.ToInt32(Setting.CompanyLogo)},{Convert.ToInt32(Setting.CompanyIcon)},{Convert.ToInt32(Setting.CustomCSSLocation)},{Convert.ToInt32(Setting.HomePageBackgroundImage)})", commandTimeout: 12000).ToList();

					if (items.Count > 0)
					{


#if DEBUG
						var uid = new Guid();
						if (x.CompanyID == DEBUG_COMPANY_ID)
						{
							uid = createCustomTheme(preciselyTheme, cnn);
						}
#else
										var uid = createCustomTheme(preciselyTheme, cnn);
#endif
						if (uid == Guid.Empty)
						{
							Console.WriteLine($"\tTheme 'Custom Theme' already exist.");
							return;
						}

						var themeContainer = serviceClient.GetBlobContainerClient("themes");
						themeContainer.CreateIfNotExistsAsync();

#if DEBUG
						if (x.CompanyID == DEBUG_COMPANY_ID)
						{
							themeContainer.GetBlobs();
							var list = themeContainer.GetBlobs(prefix: $"{x.CompanyID}").ToList();

							list.ForEach(b =>
							{
								Console.WriteLine("Deleting Blob {0}", b.Name);
								var blobClient = themeContainer.GetBlobClient(b.Name);

								blobClient.DeleteIfExists();
							});
						}
#endif

						items.ForEach(s =>
						{
							Console.WriteLine($"\tSetting: {(Setting)s.ID}");
							Console.WriteLine($"\tSetting Value: {s.Value}");

							Stream downloadStream = null;
							var sourceFileName = s.Value.Split('/').Last();

							var fileSuffix = "";

							switch ((Setting)s.ID)
							{
								case Setting.CompanyLogo:
									fileSuffix = "logo";
									downloadStream = getDownloadStream(sourceFileName, logoContainer);
									break;
								case Setting.CompanyIcon:
									fileSuffix = "icon";
									downloadStream = getDownloadStream(sourceFileName, iconContainer);
									break;
								case Setting.HomePageBackgroundImage:
									fileSuffix = "background";
									downloadStream = getDownloadStream(sourceFileName, backgroundContainer);
									break;
								case Setting.CustomCSSLocation:
									downloadStream = getDownloadStream(sourceFileName, cssContainer);
									break;
								default:
									return;
							}

							if (downloadStream != null)
							{
								if (Setting.CustomCSSLocation == (Setting)s.ID)
								{
									downloadStream = getDownloadStream(sourceFileName, cssContainer);

									if (downloadStream != null)
									{
#if DEBUG
														if (x.CompanyID == DEBUG_COMPANY_ID)
										{
											updateThemeCSS(downloadStream, cnn, uid);
										}
#else
														updateThemeCSS(downloadStream, cnn, uid);
#endif
													}
								}
								else
								{
									var extension = $".{sourceFileName.Split('.').Last()}";

									var fileName = $"{uid}_{fileSuffix}{extension}";

									copyToThemeFolder(downloadStream, themeContainer, x.CompanyID, fileName);
								}
							}
						});

						//only change the theme if the precisesly theme is current.
						if (preciselyTheme.IsCurrent == true)
						{
							Console.WriteLine("\tSetting theme as current.");
							if (uid != Guid.Empty)
							{
								cnn.Execute($@"
													Update theme 
														set isCurrent = 0;

													Update theme 
														set isCurrent = 1
														where uid = @uid;", new { uid });
							}
						}
						else
						{
							Console.WriteLine("\tThe precisely theme is not current so not changing current theme.");
						}
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"\tError: {ex.Message}");
				}
				Console.WriteLine("-------------------------------------------------------------------");
			});

			Console.WriteLine("\nMigration Complete.");
			Console.ReadLine();
		}

		private static Stream getDownloadStream(string sourceFileName, BlobContainerClient container)
		{
			var client = container.GetBlobClient(sourceFileName);
			if (client.Exists())
			{
				BlobDownloadInfo download = client.Download();
				return download.Content;
			}
			else
			{
				Console.WriteLine("\t - Source File '{0}' does not exist.\n", sourceFileName);
			}


			return null;
		}

		private static void updateThemeCSS(Stream content, SqlConnection connection, Guid themeUid)
		{
			if (content != null)
			{
				var reader = new StreamReader(content);
				var customCss = reader.ReadToEnd();
				Console.WriteLine("\tUpdate themes CustomCss\n");

				connection.Execute($"Update theme set [CustomCss] = @customCss where uid = @themeUid", new { customCss, themeUid });
			}
		}

		private static void copyToThemeFolder(Stream content, BlobContainerClient themeContainer, int companyID, string fileName)
		{
			var path = $"{companyID}/{fileName}";

			BlobClient blobClient = themeContainer.GetBlobClient(path);

			if (!blobClient.Exists())
			{
				Console.WriteLine("\t - Writing file {0}.\n", fileName);

			#if DEBUG
				if (companyID == DEBUG_COMPANY_ID)
				{
					blobClient.UploadAsync(content, true);
				}
			#else
				blobClient.UploadAsync(content, true);
			#endif
			}
			else
			{
				Console.WriteLine("\t - File \"{0}\" already exists.", fileName);
			}
		}

		private static Guid createCustomTheme(Theme preciselyTheme, SqlConnection connection)
		{			
			var existingTheme = connection.QueryFirstOrDefault<Theme>("select * from theme where name = @themeName", new { themeName = THEME_NAME });
			if (existingTheme == null)
			{
				return connection.Query<Guid>($@"insert into theme ([Uid]
																	,[Name]																			
																	,[BackColor]
																	,[BreadcrumbLinkColor]
																	,[ButtonBackColor]
																	,[PrimaryButtonBackColor]
																	,[HeaderBackColor]
																	,[NavBarBackColor]
																	,[NavBarBackSelectedColor]
																	,[TabLinkColor]
																	,[TableHeaderBackColor]
																	,[TableRowBackSelectedColor]
																	,CreatedBy
																	,CreatedOn
																	,UpdatedBy
																	,UpdatedOn)
													OUTPUT inserted.Uid
													values(
															NEWID(), 
															@themeName, 
															@BackColor, 
															@BreadcrumbLinkColor, 
															@ButtonBackColor, 
															@PrimaryButtonBackColor,
															@HeaderBackColor,
															@NavBarBackColor,
															@NavBarBackSelectedColor,
															@TabLinkColor,
															@TableHeaderBackColor,
															@TableRowBackSelectedColor,
															0, 
															GETDATE(), 
															0, 
															GETDATE())",
										new
										{
											themeName = THEME_NAME,
											preciselyTheme.BackColor,
											preciselyTheme.BreadcrumbLinkColor,
											preciselyTheme.ButtonBackColor,
											preciselyTheme.PrimaryButtonBackColor,
											preciselyTheme.HeaderBackColor,
											preciselyTheme.NavBarBackColor,
											preciselyTheme.NavBarBackSelectedColor,
											preciselyTheme.TabLinkColor,
											preciselyTheme.TableHeaderBackColor,
											preciselyTheme.TableRowBackSelectedColor
										},
										commandTimeout: 12000).FirstOrDefault();
			}
			else
			{
				return new Guid();
			}			
		}
	}

	public class CompanySetting
	{
		public int ID { get; set; }
		public string Value { get; set; }
	}

}
