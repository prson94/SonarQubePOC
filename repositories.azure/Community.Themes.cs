using d360.core.entities;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace repositories.azure
{
	public partial class Community : ICommunity
	{
		public async Task<RepositoryResponse<bool>> MarkThemeCurrentAsync(int companyId, Guid uid)
		{
			var userErrorMessages = new List<string>();

			var response = new RepositoryResponse<bool>(false, 0, false, "");

			if (userErrorMessages.Count > 0)
			{
				response.Message = string.Join("; ", userErrorMessages);
				response.StatusCode = 400;

				return response;
			}

			var sql = @$"
			begin
				update [CompanyTheme] set IsCurrent = 1 where CompanyID = @companyId and Uid = @Uid;
				update [CompanyTheme] set IsCurrent = 0 where CompanyID = @companyId and Uid <> @Uid;
			end			";

			using (var connection = (SqlConnection)Connect())
			{
				await connection.ExecuteAsync(sql, new { companyId, uid });

				response.IsSuccess = true;
				response.StatusCode = 200;
				response.Data = true;
			}

			return response;
		}

		public async Task<ThemewithResource> ReadCurrentThemesByUsersAsync(int companyId, int CurrentUser)
		{
			string sql = @"					
					set nocount on;
					declare @userThemeId int;
					declare @themeid int = null;
					select top 1 @userThemeId = ThemeID from CompanyResourceTheme where CompanyId in (@companyId,0) and ResourceID = @CurrentUser;
					if @userThemeId is not null
					begin
						select @themeid = ID from CompanyTheme where CompanyId in (@companyId,0) and ID = @userThemeId;
					end
					else
					begin
						select top 1 @themeid = ID from CompanyTheme where CompanyId = @companyId and IsCurrent = 1
					end
					
					if (@themeid is null)
					begin
						select top 1 ct.*,
						cast(@emptyuid as uniqueidentifier) CreatedByUid,
						'Unknown' CreatedByFullName,
						cast(@emptyuid as uniqueidentifier) UpdatedByUid,
						'Unknown' UpdatedByFullName
						from CompanyTheme ct 
						where CompanyId = 0;
					end
					else
					begin
						select top 1 ct.*,
						case when cr.id is null then cast(@emptyuid as uniqueidentifier) else cr.uid end CreatedByUid,
						case when cr.id is null then 'Unknown' else cr.FirstName + ' ' + cr.LastName end CreatedByFullName,
						case when ur.id is null then cast(@emptyuid as uniqueidentifier) else ur.uid end UpdatedByUid,
						case when ur.id is null then 'Unknown' else ur.FirstName + ' ' + ur.LastName end UpdatedByFullName
						from CompanyTheme ct
						left join [Resource] cr on ct.Createdby = cr.ID
						left join [Resource] ur on ct.Updatedby = ur.ID
						where ct.CompanyId = @companyId and ct.ID = @themeid;
					end

";
			using (var connection = (SqlConnection)Connect(true))
			{
				return (await connection.QueryAsync<ThemewithResource>(sql, new { companyId, CurrentUser, emptyuid = Guid.Empty })).FirstOrDefault();
			}
		}

		public async Task<string> ReadCurrentThemeCustomCssByUsersAsync(int companyId, int CurrentUser)
		{
			string sql = @"					
					set nocount on;
					declare @userThemeId int;
					declare @themeid int = null;
					select top 1 @userThemeId = ThemeID from CompanyResourceTheme where CompanyId = @companyId and ResourceID = @CurrentUser;
					if @userThemeId is not null
					begin
						select @themeid = ID from CompanyTheme where CompanyId = @companyId and ID = @userThemeId;
					end
					else
					begin
						select top 1 @themeid = ID from CompanyTheme where CompanyId = @companyId and IsCurrent = 1;
					end

					if (@themeid is null)
					begin
						select top 1 @themeid = ID from CompanyTheme where CompanyId = 0;
					end
					
					if (@themeid is null)
					begin
						select '' CustomCss;
					end
					else
					begin
						select CustomCss where CompanyId in (0, @companyId) and ID = @themeid;
					end

";
			using (var connection = (SqlConnection)Connect(true))
			{
				return (await connection.QueryAsync<string>(sql, new { companyId, CurrentUser })).FirstOrDefault();
			}
		}

		public async Task<Theme> ReadThemeAsync(int companyId, string name)
		{
			string sql = "select * from CompanyTheme where CompanyId in (@companyId,0) and Name = @name";
			using (var connection = (SqlConnection)Connect(true))
			{
				return (await connection.QueryAsync<Theme>(sql, new { companyId, name })).ToList().FirstOrDefault();
			}
		}
		
		public async Task<List<ThemewithResource>> ReadThemesAsync(int companyId, Guid themeUid)
		{
			string Companythemefields = @"ct.ID,ct.Uid,ct.Name,ct.HeaderLogoExtension,
										  ct.HomePageBackgroundExtension,ct.BrowserIconExtension,
										  ct.BackColor,ct.BreadcrumbLinkColor,ct.ButtonBackColor,
										  ct.PrimaryButtonBackColor,ct.HeaderBackColor,ct.NavBarBackColor,
										  ct.NavBarBackSelectedColor,ct.TabLinkColor,ct.TableHeaderBackColor,
										  ct.TableRowBackSelectedColor,ct.CustomCss,ct.CreatedBy,
										  ct.CreatedOn,ct.UpdatedBy,ct.UpdatedOn,ct.Locked";

			string addthemefilter = "";

			if (themeUid != Guid.Empty)
			{
				addthemefilter = "and ct.uid = cast(@themeUid as uniqueidentifier)";
			}
			string sql = @$"declare @CurrentThemeID int = 0;

						   select @CurrentThemeID = ID
						   from CompanyTheme
						   where CompanyId = @companyId and IsCurrent =1;

						   if (@CurrentThemeID = 0)
						   begin
							   select @CurrentThemeID = ID
							   from CompanyTheme
							   where CompanyId = 0;
						   end

						   select case when ct.Id = @CurrentThemeID then 1 else 0 end IsCurrent,
						   {Companythemefields},
						   case when cr.id is null then cast(@emptyuid as uniqueidentifier) else cr.uid end CreatedByUid,
						   case when cr.id is null then 'Unknown' else cr.FirstName + ' ' + cr.LastName end CreatedByFullName,
						   case when ur.id is null then cast(@emptyuid as uniqueidentifier) else ur.uid end UpdatedByUid,
						   case when ur.id is null then 'Unknown' else ur.FirstName + ' ' + ur.LastName end UpdatedByFullName
						   from CompanyTheme ct
						   left join [Resource] cr on ct.Createdby = cr.ID
						   left join [Resource] ur on ct.Updatedby = ur.ID
						   where ct.CompanyId in (0,@companyId) 
						   {addthemefilter}
						   order by ct.name,ct.id";
			using (var connection = (SqlConnection)Connect(true))
			{
				return (await connection.QueryAsync<ThemewithResource>(sql, new { companyId, themeUid, emptyuid = Guid.Empty })).ToList();
			}
		}

		public async Task<Theme> ReadThemeUidAsync(int companyId, Guid uid)
		{
			string sql = "select * from CompanyTheme where CompanyId in (@companyId,0) and Uid = @uid";
			using (var connection = (SqlConnection)Connect(true))
			{
				return (await connection.QueryAsync<Theme>(sql, new { companyId, uid })).ToList().FirstOrDefault();
			}
		}

		public async Task<RepositoryResponse<bool>> RemoveThemeAsync(int companyId, Guid uid)
		{
			var userErrorMessages = new List<string>();

			var response = new RepositoryResponse<bool>(false, 0, false, "");

			if (userErrorMessages.Count > 0)
			{
				response.Message = string.Join("; ", userErrorMessages);
				response.StatusCode = 400;

				return response;
			}

			var sql = @"delete ct 
						from [CompanyTheme] ct 
						where CompanyID = @companyId and Uid= @uid and CompanyID > 0;";

			using (var connection = (SqlConnection)Connect())
			{
				await connection.ExecuteAsync(sql, new { companyId, uid });

				response.IsSuccess = true;
				response.StatusCode = 200;
				response.Data = true;
			}

			return response;
		}

		public async Task<RepositoryResponse<bool>> UpsertThemeAsync(int companyId, Theme theme, int CurrentUser, bool isresetCurrent = false)
		{
			var userErrorMessages = new List<string>();

			var response = new RepositoryResponse<bool>(false, 0, false, "");

			if (userErrorMessages.Count > 0)
			{
				response.Message = string.Join("; ", userErrorMessages);
				response.StatusCode = 400;

				return response;
			}

			var sql = @$"
declare @resetcurrent bit = try_cast(@isresetCurrent as bit),
@recIsCurrent bit = try_cast(@IsCurrent as bit);

if exists(select 1 from [CompanyTheme] where CompanyID = @companyId and Name = @Name) 
begin 
	update [CompanyTheme] 
	set IsCurrent				= @IsCurrent,
	HeaderLogoExtension	 		= @HeaderLogoExtension,
	HomePageBackgroundExtension	= @HomePageBackgroundExtension,
	BrowserIconExtension	 	= @BrowserIconExtension,
	BackColor	= @BackColor,
	BreadcrumbLinkColor	 		= @BreadcrumbLinkColor,
	ButtonBackColor	 			= @ButtonBackColor,
	PrimaryButtonBackColor	 	= @PrimaryButtonBackColor,
	HeaderBackColor	 			= @HeaderBackColor,
	NavBarBackColor	 			= @NavBarBackColor,
	NavBarBackSelectedColor	 	= @NavBarBackSelectedColor,
	TabLinkColor	 			= @TabLinkColor,
	TableHeaderBackColor	 	= @TableHeaderBackColor,
	TableRowBackSelectedColor	= @TableRowBackSelectedColor,
	CustomCss	 = @CustomCss,
	UpdatedBy	 = COALESCE(@UpdatedBy,@CurrentUser),
	UpdatedOn	 = COALESCE(@UpdatedOn,getutcdate()),
	Locked	 	= @Locked
	where CompanyID = @companyId and Name = @Name 

	if (@resetcurrent = 1 and @recIsCurrent = 1)
	begin
		update [CompanyTheme] set IsCurrent = 0 where CompanyID = @companyId and Uid <> @Uid;
	end
end 
else 
begin 
	insert [CompanyTheme] (CompanyID,Name,IsCurrent,HeaderLogoExtension,
				HomePageBackgroundExtension,BrowserIconExtension,
				BackColor,BreadcrumbLinkColor,
				ButtonBackColor,PrimaryButtonBackColor,
				HeaderBackColor,NavBarBackColor,
				NavBarBackSelectedColor,TabLinkColor,
				TableHeaderBackColor,TableRowBackSelectedColor,
				CustomCss,
				CreatedBy,CreatedOn,
				UpdatedBy,UpdatedOn,
				Locked
				) 
	values (@companyId, @Name,@IsCurrent,@HeaderLogoExtension,
				@HomePageBackgroundExtension,@BrowserIconExtension,
				@BackColor,@BreadcrumbLinkColor,
				@ButtonBackColor,@PrimaryButtonBackColor,
				@HeaderBackColor,@NavBarBackColor,
				@NavBarBackSelectedColor,@TabLinkColor,
				@TableHeaderBackColor,@TableRowBackSelectedColor,
				@CustomCss,
				COALESCE(@CreatedBy,@CurrentUser),COALESCE(@CreatedOn,getutcdate()),
				COALESCE(@UpdatedBy,@CurrentUser),COALESCE(@UpdatedOn,getutcdate()),
				@Locked) 
end";

			using (var connection = (SqlConnection)Connect())
			{
				await connection.ExecuteAsync(sql, new
				{
					companyId,
					CurrentUser,
					theme.Name,
					theme.IsCurrent,
					theme.HeaderLogoExtension,
					theme.HomePageBackgroundExtension,
					theme.BrowserIconExtension,
					theme.BackColor,
					theme.BreadcrumbLinkColor,
					theme.ButtonBackColor,
					theme.PrimaryButtonBackColor,
					theme.HeaderBackColor,
					theme.NavBarBackColor,
					theme.NavBarBackSelectedColor,
					theme.TabLinkColor,
					theme.TableHeaderBackColor,
					theme.TableRowBackSelectedColor,
					theme.CustomCss,
					theme.CreatedBy,
					theme.CreatedOn,
					theme.UpdatedBy,
					theme.UpdatedOn,
					theme.Locked,
					theme.Uid,
					isresetCurrent
				});

				response.IsSuccess = true;
				response.StatusCode = 200;
				response.Data = true;
			}

			return response;
		}
	}
}
