CREATE PROCEDURE [bulkload].[Unrelate]
--declare 
	@id int --= 297
AS
BEGIN
	SET NOCOUNT ON;


	declare @r int,
			@intersectTypeID int,
			@subjectHasSubjectArea bit,
			@subject varchar(50),
			@subjectID int,
			@objectHasSubjectArea bit,
			@object varchar(50),
			@objectID int,
			@dt datetime = getutcdate(),
			@columnCount int

	select	@r = UpdatedBy,
			@intersectTypeID = ObjectID
	from	[Load] 
	where	[Action] = 'U'
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

				cast(0 as bit) as Status,
				cast('' as nvarchar(500)) as StatusMessage,

				@r as ResourceID  --THE USER THAT ADDED THE LOAD
		into	#Items
		from	LoadItemColumn S
				inner join LoadItemColumn O on O.LoadID = S.LoadID 
											and O.RowIndex = S.RowIndex 
											and S.LoadID = @id
											and S.ColumnIndex = case 
												when @subjectHasSubjectArea = 1 then 2
												else 1
											end
											and O.ColumnIndex = case
																	when @subjectHasSubjectArea = 1 and @objectHasSubjectArea = 1 then 4
																	when @subjectHasSubjectArea = 1 and @objectHasSubjectArea = 0 then 3
																	when @subjectHasSubjectArea = 0 and @objectHasSubjectArea = 1 then 3
																	else 2
																end			

		-- Add indexes to temp table
		CREATE NONCLUSTERED INDEX [IX_Intersect] ON #Items ( Subject ASC, SubjectID ASC, Object ASC, ObjectID ASC )
--select * from #Items

		-- update rows with existing intersects
		update	T
		set		T.IntersectID = S.ID
		from	#Items T
				inner join [Intersect] S on S.IntersectTypeID = @intersectTypeID 
										and T.Subject = S.Subject 
										and T.SubjectID = S.SubjectID 
										and T.Object = S.Object 
										and T.ObjectID = S.ObjectID;

		-- delete relationships
		declare @tbl table (ID int)

		insert into @tbl
			select IntersectID from #Items where IntersectID > 0

		insert into @tbl
			select ID from [Intersect] where Subject = 'Intersect' and SubjectID in (select IntersectID from #Items where IntersectID > 0)

		insert into @tbl
			select ID from [Intersect] where Object = 'Intersect' and ObjectID in (select IntersectID from #Items where IntersectID > 0)

		-- Delete anywhere that the intersect is used.
		delete Field where ObjectType = 'Intersect'and ObjectID in (select ID from @tbl)
		delete [Attribute] where ObjectType = 'Intersect'and ObjectID in (select ID from @tbl)
		delete MapRuleItemMapItem where MapItemID in (
			select	M.ID 
			from	MapItem M
					inner join @tbl I on (I.ID = M.TargetIntersectID) OR (I.ID = M.SourceIntersectID)
		)
		delete MapItemMap where MapItemID in (
			select	M.ID 
			from	MapItem M
					inner join @tbl I on (I.ID = M.TargetIntersectID) OR (I.ID = M.SourceIntersectID)
		)
		delete	MapItem 
		where	SourceIntersectID in (select ID from @tbl)
				or TargetIntersectID in (select ID from @tbl)

		-- now delete the Intersects.
		delete [Intersect] where ID in (select ID from @tbl)

		-- SUCCESS STATUS
		update	#Items
		set		Status = 1,
				StatusMessage = 'Relationship removed. '
		where	IntersectID > 0;

		-- FAILED STATUS
		update	T
		set		T.Status = 0,
				T.StatusMessage = T.StatusMessage +
								'Relationship could not be removed. ' + 
								IIF(T.SubjectID is null, 'Could not find subject. ', '') + 
								IIF(T.ObjectID is null, 'Could not find object. ', '')
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
					@ct = 'Delete'
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
END