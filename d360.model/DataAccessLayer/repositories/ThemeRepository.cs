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
    public class ThemeRepository : BaseRepository, IThemeRepository
    {
        #region DI

        internal ICompanyContext CompanyContext;
        internal IQueueSource QueueSource;
        internal IStorageProvider StorageProvider;
        internal ICommunityContext Community;

        public ThemeRepository(ICompanyContext companyContext, IQueueSource queueSource, IStorageProvider storageProvider, ICommunityContext community)
            : base(companyContext)
        {
            CompanyContext = companyContext;
            QueueSource = queueSource;
            StorageProvider = storageProvider;
            Community = community;
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
                ResourceID = current.UpdatedBy
            };

            audit.AuditFields.Add(new AuditField { FieldName = "Name", PreviousValue = ((previous != null) ? previous.Name : null), Value = current.Name, FieldTypeID = 0, Version = 0 });
            audit.AuditFields.Add(new AuditField { FieldName = "IsCurrent", PreviousValue = ((previous != null) ? (previous.IsCurrent ? "Yes" : "No") : null), Value = (current.IsCurrent ? "Yes" : "No"), FieldTypeID = 0, Version = 0 });
            audit.AuditFields.Add(new AuditField { FieldName = "HeaderLogoExtension", PreviousValue = ((previous != null) ? previous.HeaderLogoExtension : null), Value = current.HeaderLogoExtension, FieldTypeID = 0, Version = 0 });
            audit.AuditFields.Add(new AuditField { FieldName = "HomePageBackgroundExtension", PreviousValue = ((previous != null) ? previous.HomePageBackgroundExtension : null), Value = current.HomePageBackgroundExtension, FieldTypeID = 0, Version = 0 });
            audit.AuditFields.Add(new AuditField { FieldName = "BrowserIconExtension", PreviousValue = ((previous != null) ? previous.BrowserIconExtension : null), Value = current.BrowserIconExtension, FieldTypeID = 0, Version = 0 });
            audit.AuditFields.Add(new AuditField { FieldName = "BackColor", PreviousValue = ((previous != null) ? previous.BackColor : null), Value = current.BackColor, FieldTypeID = 0, Version = 0 });
            audit.AuditFields.Add(new AuditField { FieldName = "BreadcrumbLinkColor", PreviousValue = ((previous != null) ? previous.BreadcrumbLinkColor : null), Value = current.BreadcrumbLinkColor, FieldTypeID = 0, Version = 0 });
            audit.AuditFields.Add(new AuditField { FieldName = "ButtonBackColor", PreviousValue = ((previous != null) ? previous.ButtonBackColor : null), Value = current.ButtonBackColor, FieldTypeID = 0, Version = 0 });
            audit.AuditFields.Add(new AuditField { FieldName = "PrimaryButtonBackColor", PreviousValue = ((previous != null) ? previous.PrimaryButtonBackColor : null), Value = current.PrimaryButtonBackColor, FieldTypeID = 0, Version = 0 });
            audit.AuditFields.Add(new AuditField { FieldName = "HeaderBackColor", PreviousValue = ((previous != null) ? previous.HeaderBackColor : null), Value = current.HeaderBackColor, FieldTypeID = 0, Version = 0 });
            audit.AuditFields.Add(new AuditField { FieldName = "NavBarBackColor", PreviousValue = ((previous != null) ? previous.NavBarBackColor : null), Value = current.NavBarBackColor, FieldTypeID = 0, Version = 0 });
            audit.AuditFields.Add(new AuditField { FieldName = "NavBarBackSelectedColor", PreviousValue = ((previous != null) ? previous.NavBarBackSelectedColor : null), Value = current.NavBarBackSelectedColor, FieldTypeID = 0, Version = 0 });
            audit.AuditFields.Add(new AuditField { FieldName = "TabLinkColor", PreviousValue = ((previous != null) ? previous.TabLinkColor : null), Value = current.TabLinkColor, FieldTypeID = 0, Version = 0 });
            audit.AuditFields.Add(new AuditField { FieldName = "TableHeaderBackColor", PreviousValue = ((previous != null) ? previous.TableHeaderBackColor : null), Value = current.TableHeaderBackColor, FieldTypeID = 0, Version = 0 });
            audit.AuditFields.Add(new AuditField { FieldName = "TableRowBackSelectedColor", PreviousValue = ((previous != null) ? previous.TableRowBackSelectedColor : null), Value = current.TableRowBackSelectedColor, FieldTypeID = 0, Version = 0 });
            audit.AuditFields.Add(new AuditField { FieldName = "CustomCss", PreviousValue = ((previous != null) ? previous.CustomCss : null), Value = current.CustomCss, FieldTypeID = 0, Version = 0 });

            CompanyContext.Add(audit);

            CompanyContext.Connection.Execute(@"
												update	T
												set		T.Version = S.[Count] + 1
												from	[reporting].[Global_FieldAudit] T
														inner join [reporting].[Global_Audit] TA on TA.ID = T.AuditID
														cross apply (
															select	count(1) as [Count]
															from	[reporting].[Global_FieldAudit] F
																	inner join [reporting].[Global_Audit] A on A.ID = F.AuditID 
																	and A.Object = TA.Object 
																	and A.ObjectID = TA.ObjectID
																	and F.FieldName = T.FieldName
														) S
												where   T.AuditID = @ID and T.[Version] = 0", new { audit.ID });
        }

        private void addStorageFile(Guid uid, string fileSuffix, byte[] content, string extension)
        {
            if (content != null && content.Length > 0)
            {
                var path = $"{CompanyContext.CurrentCompanyID}/{uid}_{fileSuffix}{extension}";
                var contentType = MimeTypeExtensionsMap.GetMimeType(extension);
                var stream = new MemoryStream(content);
                StorageProvider.CreateFile("themes", path, stream, contentType);
            }
        }

        private void deleteStorageFile(Guid uid, string fileSuffix, string extension)
        {
            var path = $"{CompanyContext.CurrentCompanyID}/{uid}_{fileSuffix}{extension}";
            StorageProvider.DeleteFile("themes", path);
        }

        #endregion

        public HttpStatusCode Delete(Guid uid)
        {
            var theme = CompanyContext.Filter<Theme>(t => t.Uid == uid).SingleOrDefault();

            if (theme == null)
            {
                throw new GenericException(HttpStatusCode.NotFound, ThemeErrors.ThemeWithUidNotFound);
            }

            if (theme.Locked)
            {
                throw new GenericException(HttpStatusCode.Conflict, ThemeErrors.ThemeIsLockedForRemoval);
            }

            if (theme.IsCurrent)
            {
                throw new GenericException(HttpStatusCode.Conflict, ThemeErrors.ThemeInUseForRemoval);
            }

            var iconExt = theme.BrowserIconExtension;
            var headerExt = theme.HeaderLogoExtension;
            var backExt = theme.HomePageBackgroundExtension;
            CompanyContext.Delete(theme);

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

            return HttpStatusCode.OK;
        }

        public async Task<List<GetTheme>> GetThemesAsync(IEnumerable<KeyValuePair<string, string>> queryParams, CancellationToken? cancellationToken = null)
        {
            Guid themeUid = Guid.Empty;

            if (queryParams.ToList().Any(x => x.Key.ToLower() == "uid"))
            {
                if (!Guid.TryParse(queryParams.FirstOrDefault(x => x.Key.ToLower() == "uid").Value, out themeUid))
                {
                    themeUid = Guid.Empty;
                    throw new GenericException(HttpStatusCode.BadRequest, ThemeErrors.ErrorOnGet, ThemeErrors.InvalidUidParameter);
                }
            }

            List<GetTheme> apiModels = null;

            await Task.Run(() =>
            {
                var dbModels = (from t in CompanyContext.Table<Theme>()
                                join c in CompanyContext.GlobalReportingResources on t.CreatedBy equals c.ResourceID
                                join u in CompanyContext.GlobalReportingResources on t.UpdatedBy equals u.ResourceID
                                select new { t, c, u }
                               );

                if (themeUid != Guid.Empty)
                {
                    dbModels = dbModels.Where(m => m.t.Uid == themeUid);
                }

                var baseUri = StorageProvider.GetBaseUri("themes");

                apiModels = dbModels
                    .ToList()
                    .Select(m => m.t.ToGetModel(baseUri, m.c, m.u, CompanyContext.CurrentCompanyID))
                    .OrderBy(t => t.Name)
                    .ToList();
            }).ConfigureAwait(false);

            return apiModels;
        }

        public string GetCurrentThemeCustomCssByUser()
        {
            var themeSql = @"
							set nocount on;
							declare @userThemeId int;
							select @userThemeId = ThemeID from ResourceTheme where ResourceID = @CurrentResourceID

							if @userThemeId is not null
							begin
								select * from Theme where ID = @userThemeId
							end
							else
							begin
								select top 1 * from Theme where IsCurrent = 1
							end";
            var theme = CompanyContext.Query<Theme>(themeSql, new { CompanyContext.CurrentResourceID }).SingleOrDefault();

            return (theme != null) ? theme.CustomCss + "" : "";
        }

        public async Task<GetTheme> GetCurrentThemeByUserAsync()
        {
            var themeSql = @"
							set nocount on;
							declare @userThemeId int,
									@createdBy int,
									@updatedBy int;
							select @userThemeId = ThemeID from ResourceTheme where ResourceID = @CurrentResourceID

							if @userThemeId is not null
							begin
								select @createdBy = CreatedBy, @updatedBy = UpdatedBy from Theme where ID = @userThemeId
								select * from Theme where ID = @userThemeId
							end
							else
							begin
								select @createdBy = CreatedBy, @updatedBy = UpdatedBy from Theme where IsCurrent = 1
								select top 1 * from Theme where IsCurrent = 1
							end
							select * from reporting.Global_Resource where ResourceID = @createdBy;
							select * from reporting.Global_Resource where ResourceID = @updatedBy;";

            var gridReader = await CompanyContext.Database.Connection.QueryMultipleAsync(
                new CommandDefinition(
                    themeSql,
                    new { CompanyContext.CurrentResourceID },
                    commandTimeout: ApiTimeout
                    )
                );

            var dbTheme = gridReader.Read<Theme>().FirstOrDefault();
            var dbCreatedBy = gridReader.Read<GlobalReportingResource>().FirstOrDefault();
            var dbUpdatedBy = gridReader.Read<GlobalReportingResource>().FirstOrDefault();
            var baseUri = StorageProvider.GetBaseUri("themes");

            if (dbTheme == null)
            {
                throw new GenericException(HttpStatusCode.NotFound, ThemeErrors.ErrorOnGet, ThemeErrors.NoCurrentThemes);
            }

            return dbTheme.ToGetModel(baseUri, dbCreatedBy, dbUpdatedBy, CompanyContext.CurrentCompanyID);
        }

        public Theme GetThemeByUid(Guid uid)
        {
            var theme = CompanyContext.Filter<Theme>(t => t.Uid == uid).SingleOrDefault();

            if (theme == null)
            {
                throw new GenericException(HttpStatusCode.NotFound, ThemeErrors.ThemeWithUidNotFound);
            }

            return theme;
        }

        public async Task<bool> MarkThemeAsCurrentAsync(Guid uid)
        {
            var theme = CompanyContext.Filter<Theme>(t => t.Uid == uid).SingleOrDefault();
            if (theme == null)
            {
                throw new GenericException(HttpStatusCode.NotFound, ThemeErrors.ErrorOnUpdate, ThemeErrors.ThemeWithUidNotFound);
            }
            var nowPreviousTheme = theme.CloneThis();
            theme.IsCurrent = true;

            await Task.Run(() =>
            {
                CompanyContext.Update(theme);
                CompanyContext.Connection.Execute("update Theme set IsCurrent = 0 where Uid <> @Uid", new { theme.Uid });
                addChangeLog(theme, "U", nowPreviousTheme);
            }).ConfigureAwait(false);

            return true;
        }

        public async Task<GetTheme> PostThemeAsync(PostTheme theme, bool validationOnly = false)
        {
            var repoTheme = theme.ToRepositoryModel(CompanyContext.CurrentResourceID);
            repoTheme.Validate();

            if (CompanyContext.Any<Theme>(t => t.Name.ToLower() == repoTheme.Name.ToLower()))
            {
                throw new GenericException(HttpStatusCode.Conflict, ThemeErrors.ErrorOnCreate, ThemeErrors.ThemeNameMustBeUnique);
            }

            if (validationOnly)
            {
                return new GetTheme();
            }

            await Task.Run(() =>
            {
                CompanyContext.Add(repoTheme);
                if (repoTheme.IsCurrent)
                {
                    CompanyContext.Connection.Execute("update Theme set IsCurrent = 0 where Uid <> @Uid", new { repoTheme.Uid });
                }
                addChangeLog(repoTheme, "C");

                addStorageFile(repoTheme.Uid, "icon", repoTheme.BrowserIcon, repoTheme.BrowserIconExtension);
                addStorageFile(repoTheme.Uid, "logo", repoTheme.HeaderLogo, repoTheme.HeaderLogoExtension);
                addStorageFile(repoTheme.Uid, "background", repoTheme.HomePageBackground, repoTheme.HomePageBackgroundExtension);

            }).ConfigureAwait(false);

            var baseUri = StorageProvider.GetBaseUri("themes");
            var resource = CompanyContext.Filter<GlobalReportingResource>(r => r.ResourceID == CompanyContext.CurrentResourceID).SingleOrDefault();

            return repoTheme.ToGetModel(baseUri, resource, resource, CompanyContext.CurrentCompanyID);
        }

        public async Task<GetTheme> PutThemeAsync(Guid uid, PutTheme theme)
        {
            var existingTheme = CompanyContext.Filter<Theme>(t => t.Uid == uid).SingleOrDefault();

            if (existingTheme == null)
            {
                throw new GenericException(HttpStatusCode.NotFound, ThemeErrors.ErrorOnUpdate, ThemeErrors.ThemeWithUidNotFound);
            }

            var nowPreviousTheme = existingTheme.CloneThis();

            if (existingTheme.Locked)
            {
                throw new GenericException(HttpStatusCode.Conflict, ThemeErrors.ErrorOnUpdate, ThemeErrors.ThemeIsLocked);
            }

            existingTheme = theme.ToRepositoryModel(existingTheme, CompanyContext.CurrentResourceID);
            existingTheme.Validate();

            if (CompanyContext.Any<Theme>(t => t.Uid != existingTheme.Uid && t.Name.ToLower() == existingTheme.Name.ToLower()))
            {
                throw new GenericException(HttpStatusCode.Conflict, ThemeErrors.ErrorOnUpdate, ThemeErrors.ThemeNameMustBeUnique);
            }

            await Task.Run(() =>
            {
                CompanyContext.Update(existingTheme);
                if (existingTheme.IsCurrent)
                {
                    CompanyContext.Connection.Execute("update Theme set IsCurrent = 0 where Uid <> @Uid", new { existingTheme.Uid });
                }

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

            var createdBy = CompanyContext.Filter<GlobalReportingResource>(r => r.ResourceID == existingTheme.CreatedBy).SingleOrDefault();
            var updatedBy = CompanyContext.Filter<GlobalReportingResource>(r => r.ResourceID == existingTheme.UpdatedBy).SingleOrDefault();
            var baseUri = StorageProvider.GetBaseUri("themes");

            return existingTheme.ToGetModel(baseUri, createdBy, updatedBy, CompanyContext.CurrentCompanyID);
        }
    }
}
