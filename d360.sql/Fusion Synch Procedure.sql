create procedure fusion.ProcessFusionInQueue
	@companyID int,
	@queueID uniqueidentifier
as
begin
	IF OBJECT_ID('tempdb..#mappings') IS NOT NULL
		DROP TABLE #mappings

	declare @fusionID int,
			@data xml,
			@date datetime,
			@current int,
			@max int

	select	@data = Data,
			@fusionID = ObjectID
	from	utility.Queue
	where	CompanyID = @companyID
			and ID = @queueID
			and ObjectType = 'Fusion'

	set @date = getutcdate()

	if (@data is not null)
	begin

		/*
			BEGIN FUSION ATTRIBUTE PROCESSING
		*/

		begin try

			create table #mappings (RowID int, Name nvarchar(250), Value nvarchar(250), FusionAttributeTypeID int, FusionAttributeID int, SourceID nvarchar(250), ParentFusionAttributeID int, ParentSourceID nvarchar(250), FieldTypeID int, OldValue nvarchar(4000), [Action] varchar(1), Message nvarchar(4000))
			create nonclustered index IX_TempMappings_RowID ON #mappings (RowID asc)
			create nonclustered index IX_TempMappings_Name ON #mappings (Name asc)

			insert into #mappings (RowID, Name, Value)
				select	M.m.value('./@id', 'int') as RowID,
						P.p.value('local-name(.)', 'nvarchar(250)') as Name,
						P.p.value('(./text())[1]', 'nvarchar(250)') as Value
				from	@data.nodes('/import') as I(i)
						cross apply I.i.nodes('ms/m') M(m)
						cross apply M.m.nodes('*') P(p)

			update	m
			set		m.FusionAttributeTypeID = cast(t.Value as int)
			from	#mappings m
					inner join	(
								select	distinct 
										RowID, 
										Value 
								from	#mappings 
								where	Name = 'FusionAttributeTypeID'
								) t on m.RowID = t.RowID

			update	m
			set		m.SourceID = t.Value
			from	#mappings m
					inner join	(
								select	distinct 
										RowID, 
										Value 
								from	#mappings 
								where	Name = 'SourceID'
								) t on m.RowID = t.RowID

			update	m
			set		m.ParentSourceID = t.Value
			from	#mappings m
					inner join	(
								select	distinct 
										RowID, 
										Value 
								from	#mappings 
								where	Name = 'ParentSourceID'
								) t on m.RowID = t.RowID

			update	m
			set		m.FusionAttributeID = t.FusionAttributeID,
					m.ParentFusionAttributeID = pt.FusionAttributeID,
					m.FieldTypeID = ft.FieldTypeID
			from	#mappings m
					left join	(
								select	distinct 
										MI.RowID, 
										MI.Value,
										FA.ID as FusionAttributeID 
								from	#mappings MI
										left join FusionAttribute FA on FA.CompanyID = @companyID and FA.FusionID = @fusionID and FA.SourceID = MI.Value
								where	MI.Name = 'SourceID'
								) t on m.RowID = t.RowID
					left join	(
								select	distinct 
										MI.RowID, 
										MI.Value,
										FA.ID as FusionAttributeID 
								from	#mappings MI
										left join FusionAttribute FA on FA.CompanyID = @companyID and FA.FusionID = @fusionID and FA.SourceID = MI.Value
								where	MI.Name = 'ParentSourceID'
								) pt on m.RowID = pt.RowID
					left join	(
								select	FTR.FieldTypeID,
										FTR.ObjectType,
										FTR.ObjectID,
										FT.Name
								from	FieldTypeRelation FTR
										inner join FieldType FT on FT.CompanyID = FTR.CompanyID and FT.ID = FTR.FieldTypeID
								where	 FTR.ObjectType = 'FusionAttributeType' and FTR.CompanyID = @companyID
								) ft on ft.ObjectID = M.FusionAttributeTypeID and ft.Name = M.Name

			-- Update the temp table with current attribute Name.
			update	m
			set		m.OldValue = t.Name
			from	#mappings m
					left join	(
								select	distinct 
										MI.RowID, 
										FA.Name,
										FA.ID as FusionAttributeID 
								from	#mappings MI
										left join FusionAttribute FA on FA.CompanyID = @companyID and FA.FusionID = @fusionID and FA.ID = MI.FusionAttributeID
								where	MI.Name = 'Name'
								) t on m.RowID = t.RowID and m.Name = 'Name'

			-- Update the temp table with current dynamic field values.
			update	m
			set		m.OldValue = t.Value
			from	#mappings m
					inner join	(
								select	distinct 
										MI.RowID, 
										MI.FieldTypeID,
										F.Value
								from	#mappings MI
										left join Field F on F.CompanyID = @companyID and F.FieldTypeID = MI.FieldTypeID and F.ObjectType = 'FusionAttribute' and F.ObjectID = MI.FusionAttributeID
								where	MI.FieldTypeID is not null
								) t on m.RowID = t.RowID and m.FieldTypeID = T.FieldTypeID

			--Clear out static fields that were only used as reference
			delete	#mappings where	Name in ('FusionAttributeTypeID', 'SourceID', 'ParentSourceID')


			-- Update the Action we need to perform for each row.
			update	#mappings
			set		[Action] = 'A'
			where	FusionAttributeID is null

			update	#mappings
			set		[Action] = 'U'
			where	FusionAttributeID is not null
					and (
						(FieldTypeID is not null and Name <> 'Name')
						or 
						(FieldTypeID is null and Name = 'Name')
						)
					and (
						OldValue <> Value
						and 
							(
							(OldValue is null and Value is not null and Value <> '')
							or 
							(OldValue is not null and Value is null)
							)
						)

			update	#mappings
			set		[Message] = Name + 
								case [Action] 
									when 'A' then ' added. Value is now ' + Value
									when 'U' then ' updated. Value changed from ' + case 
																						when OldValue is null then '"" to '
																						else OldValue
																					end +	
																					case 
																						when Value is null then '"."'
																						else '"' + Value + '."'
																					end	
								end
			where	[Action] is not null 

			declare @fusionTypeID int,
					@currentFusionAttributeTypeID int;
			declare @tbl table (ID int identity, FusionAttributeTypeID int);

			select @fusionTypeID = FusionTypeID from Fusion where CompanyID = @companyID and ID = @fusionID;

			with cte as
			(
				select	ID,
						ParentID,
						1 as [Level]
				from	FusionAttributeType
						where CompanyID = @companyID and FusionTypeID = @fusionTypeID and ParentID is null
				union all
				select	T.ID,
						T.ParentID,
						P.[Level] + 1 as [Level]
				from	FusionAttributeType T
						inner join cte P on T.CompanyID = @companyID and T.FusionTypeID = @fusionTypeID and T.ParentID = P.ID
			)
			insert into @tbl 
				select ID from cte order by [Level]

			set @current = 1
			select @max = MAX(ID) from @tbl

			while (@current <= @max)
			begin
				select @currentFusionAttributeTypeID = FusionAttributeTypeID from @tbl where ID = @current

				insert FusionAttribute (CompanyID, ParentID, Name, SourceID, FusionID, FusionAttributeTypeID, DateCreated, CreatingResourceID, DateUpdated, UpdatingResourceID)
					select	@companyID, 
							ParentFusionAttributeID, 
							Value, 
							SourceID,
							@fusionID, 
							FusionAttributeTypeID, 
							@date, 
							0, 
							@date, 
							0
					from	#mappings
					where	FusionAttributeTypeID = @currentFusionAttributeTypeID
							and Name = 'Name'
							and [Action] = 'A'

				update	T
				set		Name = S.Value,
						DateUpdated = @date
				from	FusionAttribute T
						inner join #mappings S on T.CompanyID = @companyID and S.FusionAttributeTypeID = @currentFusionAttributeTypeID and S.Name = 'Name' and S.[Action] = 'U'

				update	m
				set		m.FusionAttributeID = t.FusionAttributeID,
						m.ParentFusionAttributeID = pt.FusionAttributeID
				from	#mappings m
						left join	(
									select	distinct 
											MI.RowID, 
											FA.ID as FusionAttributeID 
									from	#mappings MI
											inner join FusionAttribute FA	on FA.CompanyID = @companyID 
																			and FA.FusionID = @fusionID 
																			and (
																				MI.ParentSourceID is null
																				or	(
																					MI.ParentSourceID is not null and FA.ParentID = MI.ParentFusionAttributeID
																					)
																				)
																			and FA.SourceID = MI.SourceID
									--where	MI.Name = 'SourceID'
									) t on m.RowID = t.RowID
						left join	(
									select	distinct 
											MI.RowID, 
											FA.ID as FusionAttributeID 
									from	#mappings MI
											inner join FusionAttribute FA	on FA.CompanyID = @companyID 
																			and FA.FusionID = @fusionID 
																			and FA.SourceID = MI.ParentSourceID
									--where	MI.Name = 'ParentSourceID'
									) pt on m.RowID = pt.RowID
				where	m.FusionAttributeTypeID = @currentFusionAttributeTypeID

				insert Field (CompanyID, ObjectType, ObjectID, FieldTypeID, Value)
					select	@companyID,
							'FusionAttribute', 
							FusionAttributeID, 
							FieldTypeID,
							coalesce(Value,'')
					from	#mappings
					where	FusionAttributeTypeID = @currentFusionAttributeTypeID
							and Name <> 'Name'
							and [Action] = 'A'
							and FusionAttributeID is not null

				update	T
				set		T.Value = coalesce(S.Value,'')
				from	Field T
						inner join #mappings S	on T.CompanyID = @companyID 
												and T.ObjectType = 'FusionAttribute'
												and S.FusionAttributeTypeID = @currentFusionAttributeTypeID 
												and T.ObjectID = S.FusionAttributeID 
												and S.FusionAttributeID is not null
												and S.FieldTypeID = S.FieldTypeID 
												and S.Name <> 'Name' 
												and S.[Action] = 'U'
	
				set @current = @current + 1
			end
		end try
		begin catch
	
		end catch
		/*
			END FUSION ATTRIBUTE PROCESSING
		*/

		/*
			BEGIN EVENT GENERATION PROCESSING
		*/
		declare @fusionName nvarchar(250),
				@eventGroupID int	--The EventGroup ID we will generate

		select @fusionName = Name from Fusion where CompanyID = @companyID and ID = @fusionID 
		insert into EventGroup (CompanyID, EventTypeID, Name, PublicID, CreatingResourceID, DateCreated) 
					values (@companyID, 1, 'Load for fusion ' + @fusionName + ' on ' + cast(@date as varchar(250)), newid(), 0, @date)
		select @eventGroupID = MAX(ID) from EventGroup where CompanyID = @companyID

		declare @errors table (ID int identity, RowID int, SourceID nvarchar(250), [Message] nvarchar(max))
		insert into @errors
			select	RowID,
					SourceID,
					m
			from	(
					SELECT 
					  [RowID],
					  SourceID,
					  STUFF((
						SELECT '  ' + [Message]
						FROM #mappings 
						WHERE RowID = e.RowID and Message is not null
						FOR XML PATH(''),TYPE).value('(./text())[1]','VARCHAR(MAX)')
					  ,1,2,'') AS m
					FROM #mappings e
					GROUP BY RowID, SourceID
					) e
			where	m is not null

		set @current = 1
		select @max = MAX(ID) from @errors

		declare @message nvarchar(max),
				@sourceID nvarchar(250),
				@nameFieldTypeID int,
				@eventID int

		select	@nameFieldTypeID = R.FieldTypeID 
		from	FieldTypeRelation R
				inner join FieldType T	on T.CompanyID = R.CompanyID 
										and R.CompanyID = @companyID 
										and T.ID = R.FieldTypeID 
										and T.Name = 'Name' 
										and R.ObjectType = 'EventType' 
										and R.ObjectID = 1

		while (@current <= @max)
		begin
			select	@message = Message,
					@sourceID = SourceID
			from	@errors 
			where	ID = @current

			insert into [Event] (CompanyID, EventTypeID, EventGroupID, SourceID, Status, CreatingResourceID, DateCreated)
			values (@companyID, 1, @eventGroupID, @sourceID, 'Open', 0, @date)

			select @eventID = max(ID) from [Event] where CompanyID = @companyID
			insert into Field	(CompanyID, ObjectType, ObjectID, FieldTypeID, Value) 
			values				(@companyID, 'Event', @eventID, @nameFieldTypeID, @message) 

			set @current = @current + 1
		end

		/*
			END EVENT GENERATION PROCESSING
		*/

		--select * from #mappings order by RowID


		/*
			BEGIN RELATIONS PROCESSING
		*/

		IF OBJECT_ID('tempdb..#relations') IS NOT NULL
			DROP TABLE #relations
		create table #relations (StartID nvarchar(250), EndID nvarchar(250), StartFusionAttributeID int, EndFusionAttributeID int)
		insert into #relations
		select	R.r.value('@s', 'nvarchar(250)') as StartID,
				R.r.value('@e', 'nvarchar(250)') as EndID,
				NULL,
				NULL
		from	@data.nodes('/import') as I(i)
				cross apply I.i.nodes('rs/r') R(r)
		/*
			END RELATIONS PROCESSING
		*/

	end
end