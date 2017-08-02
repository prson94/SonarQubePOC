CREATE FUNCTION GetOwnersListForWorkflow
(
	@workflowID int,
	@workflowStepID int = 0
)
RETURNS varchar(max)
AS
BEGIN
	declare @objectId int,			
			@objectType varchar(50),
			@responsibilityTypeID int;

	declare @tbl table (ResourceID int, FirstName nvarchar(250), LastName nvarchar(250), Email nvarchar(500), Username nvarchar(500), DateLastLoggedIn datetime null, ResourceTypeID int, Status nvarchar(25))

	select @objectType = object, @objectId = objectid from [workflow].[eventregistration] where typeid = @workflowID;
	
	--get the responsibility for this step from the settings of the step
	select @responsibilityTypeID = settings.value('(/settings/ResponsibilityTypeID)[1]', 'int') from [workflow].[VersionStep] where id = @workflowStepID
	
		--1. Check for vocabulary owners
	insert into @tbl
		select	R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
		from	ResponsibilityDetail RD 					
					inner join reporting.Global_Resource R 
						on RD.ObjectType = @objectType
						and RD.ObjectID = @objectId
						and RD.ResponsibilityTypeID = @responsibilityTypeID
						and	(
								--(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
								(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
							)
						and R.Email not like '%?subject=%' and R.Status = 'Active'
		union all		
		select	R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
		from	ResponsibilityDetail RD 					
					inner join resourcegroup Rg on (RD.ResponsibleObjectID = Rg.GroupID 
						and RD.ObjectType = @objectType
						and RD.ObjectID = @objectId
						and RD.ResponsibilityTypeID = @responsibilityTypeID
						and	RD.ResponsibleObjectType = 'Group')
					inner join reporting.Global_Resource R 
						on (Rg.ResourceID = R.ResourceID and R.Email not like '%?subject=%' and R.Status = 'Active');

	
	
	-- if noone found email admins
	if not exists (select 1 from @tbl)
		begin
			insert into @tbl
				select 
					R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from 
					reporting.Global_Resource R where isadministrator = 1 and status = 'Active'
		end
	

	return (select string_agg(FirstName + ' ' + LastName,', ') as Resources from @tbl)

END
GO

