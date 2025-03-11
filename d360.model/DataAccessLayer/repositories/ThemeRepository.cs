using d360.core;
using d360.core.entities;
using d360.core.exceptions;
using d360.core.resources;
using d360.extensions;
using d360.model.DataAccessLayer.repositories;
using Dapper;
using repositories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
	public class ThemeRepository : BaseRepository, IThemeRepository
    {
        #region DI

        internal IQueueSource Queue;
        internal IStorageProvider Storage;

        public ThemeRepository(
			ICompanyContext companyContext,
			ISecurityContextProvider securityContext,
			IQueueSource queue, 
			IStorageProvider storage)
            : base(companyContext, securityContext)
        {
            Queue = queue;
            Storage = storage;
        }

        #endregion

        #region Private

        private void addChangeLog(Theme current, string action, Theme previous = null)
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

            audit.AuditFields.Add(new AuditField { FieldName = "Name", PreviousValue = ((previous != null) ? previous.Name : null), Value = current.Name, FieldTypeID = 0});
            audit.AuditFields.Add(new AuditField { FieldName = "IsCurrent", PreviousValue = ((previous != null) ? (previous.IsCurrent ? "Yes" : "No") : null), Value = (current.IsCurrent ? "Yes" : "No"), FieldTypeID = 0});
            audit.AuditFields.Add(new AuditField { FieldName = "HeaderLogoExtension", PreviousValue = ((previous != null) ? previous.HeaderLogoExtension : null), Value = current.HeaderLogoExtension, FieldTypeID = 0});
            audit.AuditFields.Add(new AuditField { FieldName = "HomePageBackgroundExtension", PreviousValue = ((previous != null) ? previous.HomePageBackgroundExtension : null), Value = current.HomePageBackgroundExtension, FieldTypeID = 0});
            audit.AuditFields.Add(new AuditField { FieldName = "BrowserIconExtension", PreviousValue = ((previous != null) ? previous.BrowserIconExtension : null), Value = current.BrowserIconExtension, FieldTypeID = 0});
            audit.AuditFields.Add(new AuditField { FieldName = "BackColor", PreviousValue = ((previous != null) ? previous.BackColor : null), Value = current.BackColor, FieldTypeID = 0});
            audit.AuditFields.Add(new AuditField { FieldName = "BreadcrumbLinkColor", PreviousValue = ((previous != null) ? previous.BreadcrumbLinkColor : null), Value = current.BreadcrumbLinkColor, FieldTypeID = 0});
            audit.AuditFields.Add(new AuditField { FieldName = "ButtonBackColor", PreviousValue = ((previous != null) ? previous.ButtonBackColor : null), Value = current.ButtonBackColor, FieldTypeID = 0});
            audit.AuditFields.Add(new AuditField { FieldName = "PrimaryButtonBackColor", PreviousValue = ((previous != null) ? previous.PrimaryButtonBackColor : null), Value = current.PrimaryButtonBackColor, FieldTypeID = 0});
            audit.AuditFields.Add(new AuditField { FieldName = "HeaderBackColor", PreviousValue = ((previous != null) ? previous.HeaderBackColor : null), Value = current.HeaderBackColor, FieldTypeID = 0});
            audit.AuditFields.Add(new AuditField { FieldName = "NavBarBackColor", PreviousValue = ((previous != null) ? previous.NavBarBackColor : null), Value = current.NavBarBackColor, FieldTypeID = 0});
            audit.AuditFields.Add(new AuditField { FieldName = "NavBarBackSelectedColor", PreviousValue = ((previous != null) ? previous.NavBarBackSelectedColor : null), Value = current.NavBarBackSelectedColor, FieldTypeID = 0});
            audit.AuditFields.Add(new AuditField { FieldName = "TabLinkColor", PreviousValue = ((previous != null) ? previous.TabLinkColor : null), Value = current.TabLinkColor, FieldTypeID = 0});
            audit.AuditFields.Add(new AuditField { FieldName = "TableHeaderBackColor", PreviousValue = ((previous != null) ? previous.TableHeaderBackColor : null), Value = current.TableHeaderBackColor, FieldTypeID = 0});
            audit.AuditFields.Add(new AuditField { FieldName = "TableRowBackSelectedColor", PreviousValue = ((previous != null) ? previous.TableRowBackSelectedColor : null), Value = current.TableRowBackSelectedColor, FieldTypeID = 0});
            audit.AuditFields.Add(new AuditField { FieldName = "CustomCss", PreviousValue = ((previous != null) ? previous.CustomCss : null), Value = current.CustomCss, FieldTypeID = 0});

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
												where   T.ID = @ID and T.[Version] = 0", new { audit.ID });
        }

        private void addStorageFile(Guid uid, string fileSuffix, byte[] content, string extension)
        {
            if (content != null && content.Length > 0)
            {
                var path = $"{SecurityContext.CompanyID}/{uid}_{fileSuffix}{extension}";
                var contentType = MimeTypeExtensionsMap.GetMimeType(extension);
                var stream = new MemoryStream(content);
                Storage.CreateFile("themes", path, stream, contentType);
            }
        }

        private void deleteStorageFile(Guid uid, string fileSuffix, string extension)
        {
            var path = $"{SecurityContext.CompanyID}/{uid}_{fileSuffix}{extension}";
            Storage.DeleteFile("themes", path);
        }

        #endregion

        public async Task<HttpStatusCode> Delete(Guid uid, Theme theme)
        {
            var iconExt = theme.BrowserIconExtension;
            var headerExt = theme.HeaderLogoExtension;
            var backExt = theme.HomePageBackgroundExtension;

			await Task.Run(() =>
			{
				if (!string.IsNullOrEmpty(iconExt))
				{
					deleteStorageFile(uid, "icon", iconExt);
				}

				if (!string.IsNullOrEmpty(headerExt))
				{
					deleteStorageFile(uid, "logo", headerExt);
				}

				if (!string.IsNullOrEmpty(backExt))
				{
					deleteStorageFile(uid, "background", backExt);
				}

				addChangeLog(theme, "D");

			}).ConfigureAwait(false);

            return HttpStatusCode.OK;
        }

        public async Task<GetTheme> GetCurrentThemeByUserAsync(ThemewithResource dbTheme)
        {
			var baseUri = await GetBaseUriTheme();

            return dbTheme.ToGetModel(baseUri,SecurityContext.CompanyID);
        }

        public async Task<bool> MarkThemeAsCurrentAsync(Theme theme,Guid uid)
        {
            var nowPreviousTheme = theme.CloneThis();
            theme.IsCurrent = true;

            await Task.Run(() =>
            {
                addChangeLog(theme, "U", nowPreviousTheme);
            }).ConfigureAwait(false);

            return true;
        }

        public async Task<HttpStatusCode> PostThemeAsync(Theme repoTheme, bool validationOnly = false)
        {
			await Task.Run(() =>
            {
                addChangeLog(repoTheme, "C");

                addStorageFile(repoTheme.Uid, "icon", repoTheme.BrowserIcon, repoTheme.BrowserIconExtension);
                addStorageFile(repoTheme.Uid, "logo", repoTheme.HeaderLogo, repoTheme.HeaderLogoExtension);
                addStorageFile(repoTheme.Uid, "background", repoTheme.HomePageBackground, repoTheme.HomePageBackgroundExtension);

            }).ConfigureAwait(false);

			return HttpStatusCode.OK;
        }

        public async Task<HttpStatusCode> PutThemeAsync(Theme existingTheme, Theme nowPreviousTheme)
        {

            await Task.Run(() =>
            {
                addChangeLog(existingTheme, "U", nowPreviousTheme);

                if (nowPreviousTheme.BrowserIconExtension != existingTheme.BrowserIconExtension)
                {
                    deleteStorageFile(nowPreviousTheme.Uid, "icon", nowPreviousTheme.BrowserIconExtension);
                }

                if (nowPreviousTheme.HeaderLogoExtension != existingTheme.HeaderLogoExtension)
                {
                    deleteStorageFile(nowPreviousTheme.Uid, "logo", nowPreviousTheme.HeaderLogoExtension);
                }

                if (nowPreviousTheme.HomePageBackgroundExtension != existingTheme.HomePageBackgroundExtension)
                {
                    deleteStorageFile(nowPreviousTheme.Uid, "background", nowPreviousTheme.HomePageBackgroundExtension);
                }

                addStorageFile(existingTheme.Uid, "icon", existingTheme.BrowserIcon, existingTheme.BrowserIconExtension);
                addStorageFile(existingTheme.Uid, "logo", existingTheme.HeaderLogo, existingTheme.HeaderLogoExtension);
                addStorageFile(existingTheme.Uid, "background", existingTheme.HomePageBackground, existingTheme.HomePageBackgroundExtension);
            }).ConfigureAwait(false);

			return HttpStatusCode.OK;
		}

		public async Task<Uri> GetBaseUriTheme()
		{
			Uri baseUri = null;
			await Task.Run(() =>
			{
				baseUri = Storage.GetBaseUri("themes");

			}).ConfigureAwait(false);
			return baseUri;
		}
	}
}
