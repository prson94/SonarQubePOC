CREATE procedure [utility].[GetOwnersForWorkflow]
--declare 
	@workflowID uniqueidentifier
--set @workflowID = '387A8094-565E-45AF-B049-01329EEF2209' --=> wt 1
--set @workflowID = '0C573C9B-D237-4468-8822-7D515750675B'--'CEE2AF0D-DAB8-432B-AF08-00E52B808C52' --=> wt 2
--set @workflowID = 'FD3C4A3D-C9BB-477A-B5CD-BC99C62AF53F' --=> wt 3
as
begin
	declare @workflowType int,
			@fields xml
	declare @tbl table (ID int, FirstName nvarchar(250), LastName nvarchar(250), Email nvarchar(500), Username nvarchar(500), DateLastLoggedIn datetime null, ResourceTypeID int, Status nvarchar(25))

	select	@workflowType = WorkflowType,
			@fields = Data
	from	Workflow
	where	ID = @workflowID

	if @workflowType = 1
	begin
		--1. Check for vocabulary owners
		insert into @tbl
			select	R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
			from	ResponsibilityDetail RD 
					inner join WorkflowTypeRelation WTR on WTR.Parent = 'TaxonomyType' and WTR.ParentID = @fields.value('(/fields/TaxonomyTypeID)[1]', 'int') and WTR.WorkflowType = @workflowType and WTR.ResponsibilityTypeID = RD.ResponsibilityTypeID
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
						inner join WorkflowTypeRelation WTR on WTR.[Object] = 'ArtifactType' and WTR.ObjectID = @fields.value('(/fields/ArtifactTypeID)[1]', 'int') and WTR.Parent is null and WTR.WorkflowType = @workflowType and WTR.ResponsibilityTypeID = RD.ResponsibilityTypeID
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

	if @workflowType = 2
	begin
		insert into @tbl
			select	R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
			from	ResponsibilityDetail RD 
					inner join Artifact A on RD.ObjectType = 'Artifact' and RD.ObjectID = A.ID and A.ID = @fields.value('(/fields/ArtifactID)[1]', 'int')
					inner join WorkflowTypeRelation WTR		on WTR.[Object] = 'ArtifactType' and WTR.ObjectID = A.ArtifactTypeID 
															and WTR.Parent = 'TaxonomyType' and WTR.ParentID = A.TaxonomyTypeID
															and WTR.WorkflowType = @workflowType 
															and WTR.ResponsibilityTypeID = RD.ResponsibilityTypeID
					inner join reporting.Global_Resource R 
						on	(
								(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
								(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
							)
						and R.Email not like '%?subject=%' and R.Status = 'Active' 

		if not exists(select * from @tbl)
		begin
			insert into @tbl
				select	R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from	ResponsibilityDetail RD 
						inner join Artifact A on RD.ObjectType = 'Artifact' and RD.ObjectID = A.ID and A.ID = @fields.value('(/fields/ArtifactID)[1]', 'int')
						inner join WorkflowTypeRelation WTR		on WTR.[Object] = 'ArtifactType' and WTR.ObjectID = A.ArtifactTypeID 
																and WTR.WorkflowType = @workflowType 
																and WTR.ResponsibilityTypeID = RD.ResponsibilityTypeID
						inner join reporting.Global_Resource R 
							on	(
									(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
									(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
								)
							and R.Email not like '%?subject=%' and R.Status = 'Active' 
		end
	end

	if @workflowType = 3
	begin
		insert into @tbl
			select	distinct
					R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
			from	Comment C
					inner join CommentRelation CR on CR.CommentID = C.ID and C.ID = @fields.value('(fields/CommentID)[1]', 'int') and CR.ObjectType not in ('Resource', 'Group')
					inner join ResponsibilityDetail RD on RD.ObjectType = CR.ObjectType and RD.ObjectID = CR.ObjectID 
					inner join reporting.Global_Resource R 
						on	(
								(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
								(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
							) 
							and R.Email not like '%?subject=%' and R.Status = 'Active'

		if not exists (select 1 from @tbl)
		begin
			insert into @tbl
				select 
					R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from 
					reporting.Global_Resource R where isadministrator = 1
		end
	end

	if @workflowType = 4
	begin
		insert into @tbl
			select	distinct
					R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
			from	Comment C
					inner join CommentRelation CR on CR.CommentID = C.ID and C.ID = @fields.value('(fields/CommentID)[1]', 'int') and CR.ObjectType not in ('Resource', 'Group')
					inner join ResponsibilityDetail RD on RD.ObjectType = CR.ObjectType and RD.ObjectID = CR.ObjectID 
					inner join reporting.Global_Resource R 
						on	(
								(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
								(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
							) 
							and R.Email not like '%?subject=%' and R.Status = 'Active'

		if not exists (select 1 from @tbl)
		begin
			insert into @tbl
				select 
					R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from 
					reporting.Global_Resource R where isadministrator = 1
		end
	end

	select * from @tbl
end