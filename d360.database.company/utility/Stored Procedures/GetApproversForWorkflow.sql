create procedure [utility].[GetApproversForWorkflow]
	@workflowID uniqueidentifier
as
begin
	declare @workflowType int,
			@fields xml
	declare @tbl table (ID int, FirstName nvarchar(250), LastName nvarchar(250), Email nvarchar(500), Username nvarchar(500), DateLastLoggedIn datetime null, ResourceTypeID int, Status nvarchar(25))

	select	@workflowType = WorkflowType,
			@fields = Data
	from	Workflow
	where	ID = @workflowID

	if @workflowType = 5
	begin
		--1. Check for vocabulary owners
		insert into @tbl
			select	R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
			from	ResponsibilityDetail RD 
					inner join WorkflowTypeRelation WTR on WTR.Parent = 'TaxonomyType' and WTR.ParentID = @fields.value('(/fields/TaxonomyTypeID)[1]', 'int') and WTR.WorkflowType = @workflowType and WTR.Fields.value('(/fields/ResponsibilityFinalApproval)[1]', 'int') = RD.ResponsibilityTypeID
					inner join reporting.Global_Resource R 
						on RD.ObjectType = 'TaxonomyType' 
						and RD.ObjectID = @fields.value('(/fields/TaxonomyTypeID)[1]', 'int')
						and	(
								(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
								(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
							)
						and R.Email not like '%?subject=%' and R.Status = 'Active'

		if not exists(select * from @tbl)
		begin
			insert into @tbl
				select	R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from	ResponsibilityDetail RD 
						inner join WorkflowTypeRelation WTR on WTR.[Object] = 'ArtifactType' and WTR.ObjectID = @fields.value('(/fields/ArtifactTypeID)[1]', 'int') and WTR.Parent is null and WTR.WorkflowType = @workflowType and WTR.Fields.value('(/fields/ResponsibilityFinalApproval)[1]', 'int') = RD.ResponsibilityTypeID
						inner join reporting.Global_Resource R 
							on RD.ObjectType = 'ArtifactType' 
							and RD.ObjectID = @fields.value('(/fields/ArtifactTypeID)[1]', 'int')
							and (
									(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
									(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
								)
							and R.Email not like '%?subject=%' and R.Status = 'Active'
		end
	end

	select * from @tbl
end