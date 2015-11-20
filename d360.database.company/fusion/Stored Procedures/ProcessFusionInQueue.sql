CREATE procedure [fusion].[ProcessFusionInQueue]
--declare
	@queueID uniqueidentifier
--set @queueID = '25921D75-6190-430A-A2DC-DC0D360F108A'
as
begin
	set NOCOUNT, ANSI_PADDING ON;
	SET ANSI_WARNINGS ON;

	declare @nextStep int,
			@fusionID int,
			@fusionTypeID int,
			@data xml,
			@loadIsNew bit,
			@objectType varchar(50) = 'FusionAttribute',
			@current int, 
			@max int,
			@start datetime,
			@continue bit = 1,
			@executionID int

	select	@fusionID = FusionID 
	from	[queue].[Fusion]
	where	ID = @queueID

	select	@loadIsNew =	case 
								when count(1) > 0 then 0
								else 1
							end
	from	FusionAttribute where FusionID = @fusionID

	select	@fusionTypeID = FusionTypeID
	from	Fusion 
	where	ID = @fusionID

	set @start = getutcdate()

	select @executionID = ID from fusion.Execution where QueueID = @queueID

	if @executionID is null
	begin
		insert into fusion.Execution (QueueID, FusionID, DateStarted, DateToUseForHistory) values (@queueID, @fusionID, @start, @start)
		set @executionID = SCOPE_IDENTITY()
	end
	else
	begin
		update fusion.Execution set DateStarted = @start where ID = @executionID
	end

	select @nextStep = coalesce(max(Step), 0) + 1 from fusion.StepStatistic where ExecutionID = @executionID

	-- STEP 1.  Load StagingItem table
	if @nextStep = 1
	begin
		begin try
			select	@data = Data
			from	[queue].[Fusion] 
			where	ID = @queueID

			if @data is not null
			begin
				-- Insert staging data to work with.
				insert into fusion.StagingItem (ExecutionID, RowID, Name, Value)
					select	@executionID,
							M.m.value('./@id', 'int') as RowID,
							P.p.value('local-name(.)', 'nvarchar(250)') as Name,
							P.p.value('(./text())[1]', 'nvarchar(250)') as Value
					from	@data.nodes('/import') as I(i)
							cross apply I.i.nodes('ms/m') M(m)
							cross apply M.m.nodes('*') P(p)

				set @data = null
			end


			-- insert this into temp table this takes 30 seconds
			insert into [fusion].[StagingRelationMapping]
					select	
								@executionID,
								replace(R.r.value('@s', 'nvarchar(250)'), ' ', '') as StartID,
								replace(R.r.value('@e', 'nvarchar(250)'), ' ', '') as EndID
						from	[queue].[Fusion] fus
								CROSS APPLY data.nodes('/import/rs/r') as R(r)								
						where fus.id = @queueID
			

			insert into [fusion].[StagingRelation]
				select	@executionID,
						R.StartID,
						R.EndID,
						S.ID,
						E.ID,
						S.FusionAttributeTypeID,
						E.FusionAttributeTypeID,
						RT.SourceIntersectTypeNodeID,
						RT.TargetIntersectTypeNodeID,
						RT.IntersectTypeID,
						V.IntersectID
				from	(
						select	srm.StartID,
								srm.EndID
						from	[fusion].[StagingRelationMapping] srm							
						where srm.ExecutionID = @executionID
						) R
						inner join FusionAttribute S on S.FusionID = @fusionID and S.SourceID = R.StartID
						inner join FusionAttribute E on E.FusionID = @fusionID and E.SourceID = R.EndID
						cross apply (
									select	IntersectTypeID,
											SourceIntersectTypeNodeID,
											TargetIntersectTypeNodeID
									from	utility.RelationshipTypes
									where	SourceObjectType = 'FusionAttributeType' and SourceObjectID = S.FusionAttributeTypeID 
											and TargetObjectType = 'FusionAttributeType' and TargetObjectID = E.FusionAttributeTypeID
									) RT
						left join cache.Relationships V on V.SourceObject = @objectType and V.TargetObject = @objectType and V.SourceObjectID = S.ID and V.TargetObjectID = E.ID						
				where	V.IntersectID is null --only get non-existent relationships
							
			

			insert into fusion.StepStatistic values (@executionID, 1, DATEDIFF(ss, @start, getutcdate()))
			set @nextStep = @nextStep + 1
			set @start = getutcdate()
		end try
		begin catch
			set @continue = 0
			insert into fusion.Error values (@executionID, getutcdate(), 'STEP 1. (Line ' + cast(ERROR_LINE() as varchar(50)) + ') ' + error_message())
		end catch
	end

	-- STEP 2.  Load all IDs based on incoming and derived text-based ID fields.
	if @nextStep = 2 and @continue = 1
	begin
		begin try
			-- Get rid of spaces in source IDs.
			update	fusion.StagingItem
			set		Value = replace(Value, ' ', '')
			where	ExecutionID = @executionID
					and Name in ('SourceID', 'ParentSourceID')

			update	s
			set		s.FusionAttributeTypeID = cast(fat_t.Value as int),
					s.SourceID = src_t.Value,
					s.ParentSourceID = psrc_t.Value,
					s.FusionAttributeID = FA.ID,
					s.ParentFusionAttributeID = PFA.ID,
					s.FieldTypeID = ft.ID,
					s.[Action] = case 
									when da_t.Value = 'D' then 'D'
									when FA.ID is null then 'A'
									else 'U'
								 end,
					S.OldValue = case 
									when S.Name = 'Name' then FA.Name
									else F.Value
								 end,
					S.FieldExists = case 
										when F.ObjectID is not null then cast(1 as bit)
										else cast(0 as bit)
									end
			from	fusion.StagingItem s
					inner join fusion.StagingItem fat_t on s.ExecutionID = @executionID and fat_t.ExecutionID = s.ExecutionID and fat_t.Name = 'FusionAttributeTypeID' and fat_t.RowID = s.RowID
					inner join fusion.StagingItem src_t on s.ExecutionID = @executionID and src_t.ExecutionID = s.ExecutionID and src_t.Name = 'SourceID' and src_t.RowID = s.RowID
					left join fusion.StagingItem psrc_t on s.ExecutionID = @executionID and psrc_t.ExecutionID = s.ExecutionID and psrc_t.Name = 'ParentSourceID' and psrc_t.RowID = s.RowID
					left join fusion.StagingItem da_t on s.ExecutionID = @executionID and da_t.ExecutionID = s.ExecutionID and da_t.Name = 'Action' and da_t.RowID = s.RowID and da_t.Value = 'D'
					left join FusionAttribute FA on FA.FusionID = @fusionID and FA.SourceID = src_t.Value
					left join FusionAttribute PFA on PFA.FusionID = @fusionID and PFA.SourceID = psrc_t.Value
					left join	(
								select	ID,
										ObjectID,
										Name
								from	FieldType
								where	[Object] = 'FusionAttributeType'
								) ft on ft.ObjectID = cast(fat_t.Value as int) and ft.Name = s.Name
					left join Field F on F.FieldTypeID = ft.ID and F.ObjectType = @objectType and F.ObjectID = FA.ID

			insert into fusion.StepStatistic values (@executionID, 2, DATEDIFF(ss, @start, getutcdate()))
			set @nextStep = @nextStep + 1
			set @start = getutcdate()
		end try
		begin catch
			set @continue = 0
			insert into fusion.Error values (@executionID, getutcdate(), 'STEP 2. (Line ' + cast(ERROR_LINE() as varchar(50)) + ') ' + error_message())
		end catch
	end

	-- STEP 3. Remove fields in staging table that are no longer required.
	if @nextStep = 3 and @continue = 1
	begin
		begin try
			delete fusion.StagingItem where ExecutionID = @executionID and Name in ('FusionAttributeTypeID', 'SourceID', 'Action', 'ParentSourceID')

			insert into fusion.StepStatistic values (@executionID, 3, DATEDIFF(ss, @start, getutcdate()))
			set @nextStep = @nextStep + 1
			set @start = getutcdate()
		end try
		begin catch
			insert into fusion.Error values (@executionID, getutcdate(), 'STEP 3. (Line ' + cast(ERROR_LINE() as varchar(50)) + ') ' + error_message())
		end catch
	end

	-- STEP 4.  Insert/Update attributes and fields
	if @nextStep = 4 and @continue = 1
	begin
		begin try
			merge	FusionAttribute as T
			using	(
					select		distinct 
								FusionAttributeTypeID,
								coalesce(max(Value), 'Name not resolved') as Name,
								SourceID,
								case 
									when [Action] = 'D' then cast(1 as bit)
									else cast(0 as bit)
								end as Deleted
					from		fusion.StagingItem 
					where		ExecutionID = @executionID 
								and Name = 'Name'
					group by	FusionAttributeTypeID, 
								SourceID, 
								case 
									when [Action] = 'D' then cast(1 as bit)
									else cast(0 as bit)
								end 
					) as S
			on		T.FusionID = @fusionID and T.FusionAttributeTypeID = S.FusionAttributeTypeID and T.SourceID = S.SourceID
			when	matched then
					update set	T.Name = S.Name,
								T.Deleted = S.Deleted
			when	not matched then
					insert (FusionID, FusionAttributeTypeID, SourceID, Name, Deleted)
					values (@fusionID, S.FusionAttributeTypeID, S.SourceID, S.Name, S.Deleted);

			-- update the fusion attribute id in the staging table.
			update	T
			set		T.FusionAttributeID = S.ID
			from	fusion.StagingItem T
					inner join FusionAttribute S on T.ExecutionID = @executionID
												and S.FusionID = @fusionID
												and S.FusionAttributeTypeID = T.FusionAttributeTypeID
												and T.SourceID = S.SourceID;

			insert into fusion.StepStatistic values (@executionID, 4, DATEDIFF(ss, @start, getutcdate()))
			set @nextStep = @nextStep + 1
			set @start = getutcdate()
		end try
		begin catch
			set @continue = 0
			insert into fusion.Error values (@executionID, getutcdate(), 'STEP 4. (Line ' + cast(ERROR_LINE() as varchar(50)) + ') ' + error_message())
		end catch
	end

	-- STEP 5.  Insert/Update fields
	if @nextStep = 5 and @continue = 1
	begin
		begin try
			merge	Field as T
			using	(
					select	distinct
							@objectType as ObjectType,
							FusionAttributeID as ObjectID,
							FieldTypeID,
							coalesce(max(Value), '') as Value
					from	fusion.StagingItem 
					where	ExecutionID = @executionID
							and Name <> 'Name'
							and FieldTypeID is not null
							and FusionAttributeID is not null
					group by FusionAttributeID, FieldTypeID 
					) as S
			on		T.ObjectType = S.ObjectType and T.ObjectID = S.ObjectID and T.FieldTypeID = S.FieldTypeID
			when	matched then
					update set T.Value = S.Value
			when	not matched then
					insert (FieldTypeID, ObjectType, ObjectID, Value)
					values (S.FieldTypeID, S.ObjectType, S.ObjectID, S.Value);

			insert into fusion.StepStatistic values (@executionID, 5, DATEDIFF(ss, @start, getutcdate()))
			set @nextStep = @nextStep + 1
			set @start = getutcdate()
		end try
		begin catch
			insert into fusion.Error values (@executionID, getutcdate(), 'STEP 5. (Line ' + cast(ERROR_LINE() as varchar(50)) + ') ' + error_message())
		end catch
	end

	-- STEP 6.  Update staging items and fusion attributes
	if @nextStep = 6
	begin
		begin try
			update	T
			set		T.ParentFusionAttributeID = S.FusionAttributeID
			from	fusion.StagingItem T
					inner join fusion.StagingItem S on S.ExecutionID = T.ExecutionID 
													and T.ExecutionID = @executionID
													and S.FusionAttributeID is not null
													and S.SourceID = T.ParentSourceID;

			update	T
			set		T.ParentID = S.ParentFusionAttributeID
			from	FusionAttribute T
					inner join fusion.StagingItem S on S.ExecutionID = @executionID
													and T.ID = S.FusionAttributeID;

			insert into fusion.StepStatistic values (@executionID, 6, DATEDIFF(ss, @start, getutcdate()))
			set @nextStep = @nextStep + 1
			set @start = getutcdate()
		end try
		begin catch
			insert into fusion.Error values (@executionID, getutcdate(), 'STEP 6. (Line ' + cast(ERROR_LINE() as varchar(50)) + ') ' + error_message())
		end catch
	end

	-- STEP 7.  EVENT GENERATION PROCESSING
	if @nextStep = 7 and @continue = 1
	begin
		begin try
			if @loadIsNew = 0	-- No need to load events if this is the very first load of fusion data, since an event will be generated for every single entry.
			begin
				insert into fusion.Result (ExecutionID, FusionAttributeID, Body, FieldTypeID, FieldName, Action, OldValue, NewValue)
					SELECT		distinct
								@executionID,
								[FusionAttributeID],
								case [Action]
									when 'D' then 'Item removed from source.'
									else NULL
								end,
								coalesce(FieldTypeID, 0),
								Name,
								[Action],
								OldValue,
								Value as NewValue
					FROM		fusion.StagingItem R
					where		FusionAttributeID is not null
								and ExecutionID = @executionID
								and [Action] is not null
								and ( (OldValue <> Value) OR (OldValue is null and Value is not null and Value <> '') OR (OldValue is not null and OldValue <> '' and Value is null) )
			end

			insert into fusion.StepStatistic values (@executionID, 7, DATEDIFF(ss, @start, getutcdate()))
			set @nextStep = @nextStep + 1
			set @start = getutcdate()
		end try
		begin catch
			set @continue = 0
			insert into fusion.Error values (@executionID, getutcdate(), 'STEP 7. (Line ' + cast(ERROR_LINE() as varchar(50)) + ') ' + error_message())
		end catch
	end

	-- STEP 8. Process Relations
	if @nextStep = 8 and @continue = 1
	begin
		declare @Intersects IDTable
		
		begin try		
			-- delete any relations we already have that was already added from stagingrelation table so we dont duplicate
			delete from fusion.stagingrelation where 
				executionid = @executionID
					and
				id in(
					select 
						sr.id
					from
						intersectnode inode1
						inner join intersectnode inode2 on(inode1.IntersectID = inode2.IntersectID)
						inner join fusion.stagingrelation sr on(inode1.ObjectID = sr.startfusionattributeid and inode2.ObjectID = sr.endfusionattributeid and inode1.IntersectTypeNodeID = sr.startintersecttypenodeid and inode2.IntersectTypeNodeID = sr.endintersecttypenodeid)
					where 
						inode1.objecttype = @objectType
								and
						inode2.objecttype = @objectType);
					

			select @current = MIN(ID) from [fusion].[StagingRelation] where ExecutionID = @executionID and IntersectID is null  -- only want the relations we didnt already process in a previous pass
			select @max = MAX(ID) from [fusion].[StagingRelation] where ExecutionID = @executionID and IntersectID is null  -- only want the relations we didnt already process in a previous pass

			while (@current <= @max)
			begin
				declare	
						@StartFusionAttributeID int,		@EndFusionAttributeID int,
						@StartFusionAttributeTypeID int,	@EndFusionAttributeTypeID int,
						@StartIntersectNodeTypeID int,		@EndIntersectNodeTypeID int,
						@IntersectTypeID int,				@IntersectID int
			
				select	@StartFusionAttributeID = StartFusionAttributeID,
						@EndFusionAttributeID = EndFusionAttributeID,
						@StartFusionAttributeTypeID = StartFusionAttributeTypeID,
						@EndFusionAttributeTypeID = EndFusionAttributeTypeID,
						@StartIntersectNodeTypeID = StartIntersectTypeNodeID,
						@EndIntersectNodeTypeID = EndIntersectTypeNodeID,
						@IntersectTypeID = IntersectTypeID,
						@IntersectID = IntersectID
				from	[fusion].[StagingRelation]
				where	ExecutionID = @executionID and ID = @current 
							and IntersectID is null  -- only want the relations we didnt already process in a previous pass

				begin try
					INSERT INTO [Intersect] (IntersectTypeID, Classification, Description) VALUES (@IntersectTypeID, 2, NULL)

					SELECT @IntersectID = SCOPE_IDENTITY()

					INSERT INTO IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID) 
					VALUES						(@StartIntersectNodeTypeID, @IntersectID, @objectType, @StartFusionAttributeID)

					INSERT INTO IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
					VALUES						(@EndIntersectNodeTypeID, @IntersectID, @objectType, @EndFusionAttributeID)

					insert into @Intersects values (@IntersectID)

					UPDATE	[fusion].[StagingRelation]
					SET		IntersectID = @IntersectID
					WHERE	ExecutionID = @executionID and ID = @current
				end try
				begin catch
					insert into fusion.Error values (@executionID, getutcdate(), 'STEP 8:LOOP. (Line ' + cast(ERROR_LINE() as varchar(50)) + ') ' + error_message())
				end catch

				set @current = @current +1
			end

			declare @IntersectCount int
			select @IntersectCount = count(1) from @Intersects
			if @IntersectCount > 0 
			begin
				EXEC cache.SynchronizeRelationships @Intersects
			end

			insert into fusion.StepStatistic values (@executionID, 8, DATEDIFF(ss, @start, getutcdate()))
			set @nextStep = @nextStep + 1
			set @start = getutcdate()
		end try
		begin catch
			set @continue = 0
			insert into fusion.Error values (@executionID, getutcdate(), 'STEP 8. (Line ' + cast(ERROR_LINE() as varchar(50)) + ') ' + error_message())
		end catch
	end

	-- STEP 9. Clean up.
	if @nextStep = 9 and @continue = 1
	begin
		begin try
			declare @adds int,
					@updates int,
					@deletes int,
					@errors int

			select	@adds = count(1) from fusion.Result where ExecutionID = @executionID and [Action] = 'A'
			select	@updates = count(1) from fusion.Result where ExecutionID = @executionID and [Action] = 'U'
			select	@deletes = count(1) from fusion.Result where ExecutionID = @executionID and [Action] = 'D'
			select	@errors = count(1) from fusion.Error where ExecutionID = @executionID

			update	fusion.Execution
			set		DateCompleted = @start,
					[Adds] = @adds,
					[Updates] = @updates,
					[Deletes] = @deletes
			where	ID = @executionID

			if @adds > 0 OR @updates > 0 OR @deletes > 0 OR @errors > 0
			begin
				insert into [queue].[Notification] (NotificationType, [Object], ObjectID) values (1, 'FusionExecution', @executionID)
			end

			insert into fusion.StagingItemArchive
				select	* 
				from	fusion.StagingItem 
				where	ExecutionID = @executionID

			delete fusion.StagingItem where ExecutionID = @executionID

			insert into fusion.StagingRelationArchive
				select	* 
				from	fusion.StagingRelation 
				where	ExecutionID = @executionID

			delete fusion.StagingRelation where ExecutionID = @executionID
			delete fusion.StagingRelationMapping where ExecutionID = @executionID

			UPDATE STATISTICS fusion.StagingItem
			UPDATE STATISTICS fusion.StagingRelation
			UPDATE STATISTICS fusion.StagingRelationMapping
			
			insert into fusion.StepStatistic values (@executionID, 9, DATEDIFF(ss, @start, getutcdate()))
			set @nextStep = @nextStep + 1
			set @start = getutcdate()
		end try
		begin catch
			set @continue = 0
			insert into fusion.Error values (@executionID, getutcdate(), 'STEP 9. (Line ' + cast(ERROR_LINE() as varchar(50)) + ') ' + error_message())
		end catch
	end


	--STEP 10. Publish to fusion cache queue
	if @nextStep = 10 and @continue = 1
	begin
		begin try
			insert into [queue].[FusionCache] (FusionID) values (@fusionID)
			insert into fusion.StepStatistic values (@executionID, 10, DATEDIFF(ss, @start, getutcdate()))
		end try
		begin catch
			insert into fusion.Error values (@executionID, getutcdate(), 'STEP 10. (Line ' + cast(ERROR_LINE() as varchar(50)) + ') ' + error_message())
		end catch
	end
end
