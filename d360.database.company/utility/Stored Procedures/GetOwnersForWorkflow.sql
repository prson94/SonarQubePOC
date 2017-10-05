CREATE PROCEDURE [utility].[GetOwnersForWorkflow]
	@workflowID int,
	@workflowStepID int = 0,
	@workflowItemID int = 0
as
begin
	declare @objectId int,			
			@objectType varchar(50),
			@responsibilityTypeID int,
			@issueId int;

	declare @tbl table (ResourceID int, FirstName nvarchar(250), LastName nvarchar(250), Email nvarchar(500), Username nvarchar(500), DateLastLoggedIn datetime null, ResourceTypeID int, Status nvarchar(25))
	
	--get the responsibility for this step from the settings of the step
	select @responsibilityTypeID = settings.value('(/settings/ResponsibilityTypeID)[1]', 'int') from [workflow].[VersionStep] where id = @workflowStepID
		
	-- check object	
	begin
			select @objectType = object, @objectId = objectid from [workflow].[item] where id = @workflowItemID;
			
			if @objectType = 'Issue'
			begin				
				select @issueId = id, @objectType = [object], @objectId = [objectid] from Issue where id = @objectId
			end

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
		end

	--2. Check for type owners
	if not exists (select 1 from @tbl)
		begin
			if @issueId > 0
			begin
				select @objectType = [object], @objectId = [objectid] from Issue where id = @objectId
			end
			else
			begin
				select @objectType = object, @objectId = objectid from [workflow].[eventregistration] where typeid = @workflowID;
			end

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
		end
	
	
	-- if noone found email admins
	if not exists (select 1 from @tbl)
		begin
			insert into @tbl
				select 
					R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from 
					reporting.Global_Resource R where isadministrator = 1 and status = 'Active'
		end
	

	select * from @tbl
end