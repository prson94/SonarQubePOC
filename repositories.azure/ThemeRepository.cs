using d360.core;
using d360.core.entities;
using d360.extensions;
using Dapper;
using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace repositories.azure
{
	public class ThemeRepository : Repository, IThemeRepository
	{
		#region DI

		internal IQueueSource Queue;
		internal IStorageProvider Storage;

		public ThemeRepository(
			DapperConnectionProvider provider,
			IQueueSource queue,
			IStorageProvider storage)
			: base(provider)
		{
			Queue = queue;
			Storage = storage;
		}

		#endregion DI

		#region Private

		private async Task addChangeLog(Theme current, string action, Theme previous = null)
		{
			switch (action)
			{
				case "C":
					action = "Created";
					break;

				case "U":
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
				Date = current.UpdatedOn,
				ActionDescription = $"Theme {action.ToLower(System.Globalization.CultureInfo.InvariantCulture)}.",
				Action = action,
				ActionObjectID = current.ID,
				ActionObject = "Theme",
				ActionObjectName = current.Name,
				ActionObjectTypeName = "Theme",
				Object = "Theme",
				ObjectID = current.ID,
				ObjectName = current.Name,
				ResourceID = current.UpdatedBy,
				Version = 0
			};

			audit.AuditFields.Add(new AuditField { FieldName = "Name", PreviousValue = previous != null ? previous.Name : null, Value = current.Name, FieldTypeID = 0 });
			audit.AuditFields.Add(new AuditField { FieldName = "IsCurrent", PreviousValue = previous != null ? previous.IsCurrent ? "Yes" : "No" : null, Value = current.IsCurrent ? "Yes" : "No", FieldTypeID = 0 });
			audit.AuditFields.Add(new AuditField { FieldName = "HeaderLogoExtension", PreviousValue = previous != null ? previous.HeaderLogoExtension : null, Value = current.HeaderLogoExtension, FieldTypeID = 0 });
			audit.AuditFields.Add(new AuditField { FieldName = "HomePageBackgroundExtension", PreviousValue = previous != null ? previous.HomePageBackgroundExtension : null, Value = current.HomePageBackgroundExtension, FieldTypeID = 0 });
			audit.AuditFields.Add(new AuditField { FieldName = "BrowserIconExtension", PreviousValue = previous != null ? previous.BrowserIconExtension : null, Value = current.BrowserIconExtension, FieldTypeID = 0 });
			audit.AuditFields.Add(new AuditField { FieldName = "BackColor", PreviousValue = previous != null ? previous.BackColor : null, Value = current.BackColor, FieldTypeID = 0 });
			audit.AuditFields.Add(new AuditField { FieldName = "BreadcrumbLinkColor", PreviousValue = previous != null ? previous.BreadcrumbLinkColor : null, Value = current.BreadcrumbLinkColor, FieldTypeID = 0 });
			audit.AuditFields.Add(new AuditField { FieldName = "ButtonBackColor", PreviousValue = previous != null ? previous.ButtonBackColor : null, Value = current.ButtonBackColor, FieldTypeID = 0 });
			audit.AuditFields.Add(new AuditField { FieldName = "PrimaryButtonBackColor", PreviousValue = previous != null ? previous.PrimaryButtonBackColor : null, Value = current.PrimaryButtonBackColor, FieldTypeID = 0 });
			audit.AuditFields.Add(new AuditField { FieldName = "HeaderBackColor", PreviousValue = previous != null ? previous.HeaderBackColor : null, Value = current.HeaderBackColor, FieldTypeID = 0 });
			audit.AuditFields.Add(new AuditField { FieldName = "NavBarBackColor", PreviousValue = previous != null ? previous.NavBarBackColor : null, Value = current.NavBarBackColor, FieldTypeID = 0 });
			audit.AuditFields.Add(new AuditField { FieldName = "NavBarBackSelectedColor", PreviousValue = previous != null ? previous.NavBarBackSelectedColor : null, Value = current.NavBarBackSelectedColor, FieldTypeID = 0 });
			audit.AuditFields.Add(new AuditField { FieldName = "TabLinkColor", PreviousValue = previous != null ? previous.TabLinkColor : null, Value = current.TabLinkColor, FieldTypeID = 0 });
			audit.AuditFields.Add(new AuditField { FieldName = "TableHeaderBackColor", PreviousValue = previous != null ? previous.TableHeaderBackColor : null, Value = current.TableHeaderBackColor, FieldTypeID = 0 });
			audit.AuditFields.Add(new AuditField { FieldName = "TableRowBackSelectedColor", PreviousValue = previous != null ? previous.TableRowBackSelectedColor : null, Value = current.TableRowBackSelectedColor, FieldTypeID = 0 });
			audit.AuditFields.Add(new AuditField { FieldName = "CustomCss", PreviousValue = previous != null ? previous.CustomCss : null, Value = current.CustomCss, FieldTypeID = 0 });

			var sql = @"
												update	T
												set		T.Version = coalesce(S.[maxversion],0) + 1
												from	[reporting].[Global_Audit] T
												outer apply (
															select	max(version) as [maxversion]
															from	[reporting].[Global_Audit] A
															where A.Object = T.Object
															and A.ObjectID = T.ObjectID
														) S
												where   T.ID = @ID and T.[Version] = 0";
			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				await connection.InsertAsync(audit);

				await connection.ExecuteScalarAsync(sql, new { audit.ID });
			}
		}

		private async Task addStorageFile(Guid uid, string fileSuffix, byte[] content, string extension, int CurrentCompanyId)
		{
			if (content?.Length > 0)
			{
				var path = $"{CurrentCompanyId}/{uid}_{fileSuffix}{extension}";
				var contentType = MimeTypeExtensionsMap.GetMimeType(extension);
				var stream = new MemoryStream(content);
				await Storage.CreateFile("themes", path, stream, contentType);
			}
		}

		private async Task deleteStorageFile(Guid uid, string fileSuffix, string extension, int CurrentCompanyId)
		{
			var path = $"{CurrentCompanyId}/{uid}_{fileSuffix}{extension}";
			await Storage.DeleteFile("themes", path);
		}

		#endregion Private

		public async Task<HttpStatusCode> Delete(Guid uid, Theme theme, int CurrentCompanyId)
		{
			var iconExt = theme.BrowserIconExtension;
			var headerExt = theme.HeaderLogoExtension;
			var backExt = theme.HomePageBackgroundExtension;

			if (!string.IsNullOrEmpty(iconExt))
			{
				await deleteStorageFile(uid, "icon", iconExt, CurrentCompanyId);
			}

			if (!string.IsNullOrEmpty(headerExt))
			{
				await deleteStorageFile(uid, "logo", headerExt, CurrentCompanyId);
			}

			if (!string.IsNullOrEmpty(backExt))
			{
				await deleteStorageFile(uid, "background", backExt, CurrentCompanyId);
			}

			await addChangeLog(theme, "D");

			return HttpStatusCode.OK;
		}

		public async Task<GetTheme> GetCurrentThemeByUserAsync(ThemewithResource dbTheme, int CurrentCompanyId)
		{
			var baseUri = await GetBaseUriTheme();

			return dbTheme.ToGetModel(baseUri, CurrentCompanyId);
		}

		public async Task<bool> MarkThemeAsCurrentAsync(Theme theme, Guid uid)
		{
			var nowPreviousTheme = theme.CloneThis();
			theme.IsCurrent = true;

			await addChangeLog(theme, "U", nowPreviousTheme);

			return true;
		}

		public async Task<HttpStatusCode> PostThemeAsync(Theme repoTheme, int CurrentCompanyId, bool validationOnly = false)
		{
			await addChangeLog(repoTheme, "C");
			await addStorageFile(repoTheme.Uid, "icon", repoTheme.BrowserIcon, repoTheme.BrowserIconExtension, CurrentCompanyId);
			await addStorageFile(repoTheme.Uid, "logo", repoTheme.HeaderLogo, repoTheme.HeaderLogoExtension, CurrentCompanyId);
			await addStorageFile(repoTheme.Uid, "background", repoTheme.HomePageBackground, repoTheme.HomePageBackgroundExtension, CurrentCompanyId);

			return HttpStatusCode.OK;
		}

		public async Task<HttpStatusCode> PutThemeAsync(Theme existingTheme, Theme nowPreviousTheme, int CurrentCompanyId)
		{
			await addChangeLog(existingTheme, "U", nowPreviousTheme);

			if (nowPreviousTheme.BrowserIconExtension != existingTheme.BrowserIconExtension)
			{
				await deleteStorageFile(nowPreviousTheme.Uid, "icon", nowPreviousTheme.BrowserIconExtension, CurrentCompanyId);
			}

			if (nowPreviousTheme.HeaderLogoExtension != existingTheme.HeaderLogoExtension)
			{
				await deleteStorageFile(nowPreviousTheme.Uid, "logo", nowPreviousTheme.HeaderLogoExtension, CurrentCompanyId);
			}

			if (nowPreviousTheme.HomePageBackgroundExtension != existingTheme.HomePageBackgroundExtension)
			{
				await deleteStorageFile(nowPreviousTheme.Uid, "background", nowPreviousTheme.HomePageBackgroundExtension, CurrentCompanyId);
			}

			await addStorageFile(existingTheme.Uid, "icon", existingTheme.BrowserIcon, existingTheme.BrowserIconExtension, CurrentCompanyId);
			await addStorageFile(existingTheme.Uid, "logo", existingTheme.HeaderLogo, existingTheme.HeaderLogoExtension, CurrentCompanyId);
			await addStorageFile(existingTheme.Uid, "background", existingTheme.HomePageBackground, existingTheme.HomePageBackgroundExtension, CurrentCompanyId);

			return HttpStatusCode.OK;
		}

		public async Task<Uri> GetBaseUriTheme()
		{
			Uri baseUri = null;

			await Task.Run(() => baseUri = Storage.GetBaseUri("themes"));

			return baseUri;
		}
	}
}