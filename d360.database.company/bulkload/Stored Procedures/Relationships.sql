CREATE procedure [bulkload].[Relationships]
--declare
	@id int
--set @id = 255
as
begin
	set nocount on;

	declare @r int,
			@intersectTypeID int,
			@subjectHasSubjectArea bit,
			@subject varchar(50),
			@subjectID int,
			@objectHasSubjectArea bit,
			@object varchar(50),
			@objectID int,
			@dt datetime = getutcdate(),
			@columnCount int,
			@startDynamicFieldColumnIndex int

	select	@r = UpdatedBy,
			@intersectTypeID = ObjectID
	from	[Load] 
	where	[Action] = 'R' 
			and ID = @id

	select	@columnCount = count(1) from LoadColumn where LoadID = @id

	select	@subject = Subject,
			@subjectID = SubjectID,
			@object = Object,
			@objectID = ObjectID
	from	IntersectType
	where	ID = @intersectTypeID

	if @subject = 'ArtifactType'
		begin
			set @subjectHasSubjectArea = 1
			exec bulkload.UpdateSubjectAreaColumn @id, 1							-- subject subject area
			exec bulkload.UpdateItemColumnByType @id, @subject, @subjectID, 1, 2	-- subject
		end
	else
		begin
			set @subjectHasSubjectArea = 0
			exec bulkload.UpdateItemColumnByType @id, @subject, @subjectID, 0, 1	-- subject
		end

	if @object = 'ArtifactType'
		begin
			set @objectHasSubjectArea = 1

			if @subjectHasSubjectArea = 1
				begin
					exec bulkload.UpdateSubjectAreaColumn @id, 3							-- object subject area
					exec bulkload.UpdateItemColumnByType @id, @object, @objectID, 3, 4		-- object
				end
			else
				begin 
					exec bulkload.UpdateSubjectAreaColumn @id, 2							-- object subject area
					exec bulkload.UpdateItemColumnByType @id, @object, @objectID, 2, 3		-- object
				end
		end
	else
		begin
			set @objectHasSubjectArea = 0

			if @subjectHasSubjectArea = 1
				begin
					exec bulkload.UpdateItemColumnByType @id, @object, @objectID, 0, 3		-- object
				end
			else
				begin 
					exec bulkload.UpdateItemColumnByType @id, @object, @objectID, 0, 2		-- object
				end
		end

	select @startDynamicFieldColumnIndex =	case
												when @subjectHasSubjectArea = 1 and @objectHasSubjectArea = 1 then 4
												when @subjectHasSubjectArea = 1 and @objectHasSubjectArea = 0 then 3
												when @subjectHasSubjectArea = 0 and @objectHasSubjectArea = 1 then 3
												else 2
											end
	set @startDynamicFieldColumnIndex = @startDynamicFieldColumnIndex + 1

--	select @startDynamicFieldColumnIndex, @columnCount

	drop table if exists #Items

	BEGIN TRANSACTION [Tran1]

	BEGIN TRY
		-- Load Temp table that we are going to work from
		select	S.RowIndex,
		
				S.LookupObject as Subject,
				S.LookupObjectID as SubjectID,

				O.LookupObject as Object,
				O.LookupObjectID as ObjectID,
				
				cast(0 as int) as IntersectID,
				cast('' as char(1)) as IntersectChangeType,

				case 
					when @startDynamicFieldColumnIndex <= @columnCount then cast(0 as bit)
					else cast(1 as bit)
				end as DynamicFieldsAreValid,

				cast(0 as bit) as Status,
				cast('' as nvarchar(500)) as StatusMessage,

				@r as ResourceID  --THE USER THAT ADDED THE LOAD
		into	#Items
		from	LoadItemColumn S
				inner join LoadItemColumn O on O.LoadID = S.LoadID 
											and O.RowIndex = S.RowIndex 
											and O.ColumnIndex = @startDynamicFieldColumnIndex-1
		where	S.LoadID = @id
				and S.ColumnIndex = case 
										when @subjectHasSubjectArea = 1 then 2
										else 1
									end

		-- Add indexes to temp table
		CREATE NONCLUSTERED INDEX [IX_Intersect] ON #Items ( Subject ASC, SubjectID ASC, Object ASC, ObjectID ASC )
--select * from #Items
		if @startDynamicFieldColumnIndex <= @columnCount	--has dynamic fields
		begin
			--DynamicFieldsAreValid

			-- PARSE any dynamic fields that are specifically lookups.
			exec [bulkload].[UpdateDynamicLookupFieldColumns] @id, @startDynamicFieldColumnIndex, @columnCount

			update	T
			set		T.DynamicFieldsAreValid = case
												when S.InvalidCount = 0 then cast(1 as bit)
												else cast(0 as bit)
											end
			from	#Items T
					inner join	(
								select	I.LoadID,
										I.RowIndex,
										C.InvalidCount
								from	[Load] L
										inner join [LoadItem] I on I.LoadID = L.ID
										cross apply (
													select	count(1) as InvalidCount
													from	[LoadItemColumn] IC
															inner join FieldType F on L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
															inner join [LoadColumn] C on C.LoadID = IC.LoadID and F.Name = C.Name and C.ColumnIndex = IC.ColumnIndex and C.ColumnIndex between @startDynamicFieldColumnIndex and @columnCount
													where	IC.LoadID = @id 
															and IC.RowIndex = I.RowIndex
															and IC.LookupObject is null and IC.LookupObjectID is null
													) C
								where	L.ID = @id
								) S on S.RowIndex = T.RowIndex
		end

		-- update rows with existing intersects
		update	T
		set		T.IntersectID = S.ID,
				T.IntersectChangeType = 'U'
		from	#Items T
				inner join [Intersect] S on S.IntersectTypeID = @intersectTypeID 
										and T.Subject = S.Subject 
										and T.SubjectID = S.SubjectID 
										and T.Object = S.Object 
										and T.ObjectID = S.ObjectID
										--and DynamicFieldsAreValid = 0

		-- insert relationships
		insert into [Intersect] (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
			select	distinct
					@intersectTypeID, 
					Subject, SubjectID, Object, ObjectID,
					0, ResourceID, @dt, ResourceID, @dt
			from	#Items
			where	IntersectID = 0
					and IntersectChangeType <> 'U'
					and Subject is not null 
					and SubjectID is not null
					and Object is not null 
					and ObjectID is not null
					and DynamicFieldsAreValid = 1;

		-- update rows with new intersect
		update	T
		set		T.IntersectID = S.ID,
				T.IntersectChangeType = 'A'
		from	#Items T
				inner join [Intersect] S on S.IntersectTypeID = @intersectTypeID 
										and T.Subject = S.Subject 
										and T.SubjectID = S.SubjectID 
										and T.Object = S.Object 
										and T.ObjectID = S.ObjectID
										and T.IntersectChangeType <> 'U';

		-- update status & status message for Items table
		
		-- SUCCESS STATUS
		update	#Items
		set		Status = 1,
				StatusMessage = case IntersectChangeType
									when 'A' then 'Relationship created. '
									when 'U' then 'Relationship updated. '
								end
		where	IntersectID > 0;

		-- FAILED STATUS
		update	T
		set		T.Status = 0,
				T.StatusMessage = T.StatusMessage +
								'Relationship could not be created nor updated. ' + 
								IIF(T.SubjectID is null, 'Could not find subject. ', '') + 
								IIF(T.ObjectID is null, 'Could not find object. ', '') + 
								IIF(T.DynamicFieldsAreValid = 0, 'One or more dynamic lookup fields is invalid. ', '') 
		from	#Items T
		where	IntersectID = 0;

		-- Now update LoadItems on original Load with status and messages created above
		update	T
		set		T.Status = S.Status,
				T.StatusMessage = S.StatusMessage,
				T.Object = case S.Status
							when 1 then 'Intersect'
							else NULL
						   end,
				T.ObjectID = case S.Status
							when 1 then S.IntersectID
							else NULL
						   end
		from	LoadItem T
				inner join #Items S on T.LoadID = @id and S.RowIndex = T.RowIndex;


		-- merge the dynamic fields involved with this load into the Fields table.  Needs to be here as this proc looks at the LaodItem table for the Object and ObjectID.
		exec [bulkload].MergeDynamicLookupFields @id, @startDynamicFieldColumnIndex, @columnCount


		-- Now perform audit
		declare @current int = 2,
				@max int,
				@s varchar(50),
				@sid int,
				@o varchar(50),
				@oid int,
				@intersect int,
				@ct varchar(25)
		select	@max = max(Rowindex) from #Items

		while @current <= @max
		begin
			select	@s = Subject,
					@sid = SubjectID,
					@o = Object,
					@oid = ObjectID,
					@intersect = IntersectID,
					@ct = case IntersectChangeType
							when 'A' then 'Created'
							else 'Updated'
						end
			from	#items
			where	RowIndex = @current

			if @intersect > 0
			begin
				exec utility.AddAuditEntry @s, @sid, @r, @dt, @ct, 'Intersect', @intersect
				exec utility.AddAuditEntry @o, @oid, @r, @dt, @ct, 'Intersect', @intersect
			end

			set @current = @current + 1
		end

		-- Close out the Load job
		update	[Load]
		set		DateCompleted = getutcdate()
		where	ID = @id;

		COMMIT TRANSACTION [Tran1]
	END TRY
	BEGIN CATCH
		ROLLBACK TRANSACTION [Tran1]
	END CATCH
end