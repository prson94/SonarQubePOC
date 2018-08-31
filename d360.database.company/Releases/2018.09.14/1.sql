-- GOV-5361 --------------------------------
DROP TABLE [dbo].[ScoreMetric]
GO
DROP TABLE [dbo].[Score]
GO
DROP TABLE [dbo].[ScoreTypeMetricVersion]
GO
DROP TABLE [dbo].[ScoreTypeMetric]
GO
DROP TABLE [dbo].[ScoreType]
GO

create Function [dbo].[GetEmailStepRecipients]
(
	@workflowItemStepID int	
)
RETURNS varchar(max)
BEGIN
	declare @tbl table (ResourceID int, FirstName nvarchar(250), LastName nvarchar(250), Email nvarchar(500), Username nvarchar(500), DateLastLoggedIn datetime null, ResourceTypeID int, Status nvarchar(25))
	
	insert into @tbl
		select 
			R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
		from workflow.itemstep s 
			outer apply s.settings.nodes('settings/emails/email') as m(c) 
			inner join reporting.Global_Resource R  on trim(m.c.value('@address', 'varchar(max)')) = R.email
		where id = @workflowItemStepID

	return (select string_agg(FirstName + ' ' + LastName,', ') as Resources from @tbl)

end
GO

