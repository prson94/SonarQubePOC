create procedure [utility].[CalculateStatistics]
--declare
	@Type varchar(50) = NULL,
	@ID int = NULL,
	@TargetStatisticTypeID int = NULL
as
begin
	SET NOCOUNT ON;

	declare @current int, @max int
	declare @relations table (ID int identity, [Object] varchar(50), ObjectID int)

	IF OBJECT_ID('tempdb..#StatisticTypes') IS NOT NULL
	BEGIN
		DROP TABLE #StatisticTypes
	END
	create table #StatisticTypes (ID int identity, StatisticTypeID int)

	insert into #StatisticTypes
		select ID from StatisticType where (@TargetStatisticTypeID is not null and ID = @TargetStatisticTypeID) OR @TargetStatisticTypeID is null order by ID

	set		@current	= 1
	select	@max		= MAX(ID) from #StatisticTypes

	IF OBJECT_ID('tempdb..#Statistics') IS NOT NULL
	BEGIN
		DROP TABLE #Statistics
	END
	create table #Statistics (StatisticTypeID int, ObjectType varchar(50), ObjectID int, Score int)

--select * from StatisticType

	while @current <= @max
	begin
		declare @StatisticTypeID int,
				@CheckType int,
				@CheckObjectType varchar(25),
				@CheckObjectID int,
				@Object varchar(25),
				@ObjectID int,
				@Score int,
				@PropertyName varchar(250),
				@Value nvarchar(4000),
				@PredicateID int,
				@Configuration xml

		select	@StatisticTypeID = S.ID,
				@CheckType = S.CheckType,
				@Configuration = S.Configuration,
				@Object = [Object],
				@ObjectID = ObjectID,
				@Score = Score 
		from	#StatisticTypes T
				inner join StatisticType S on S.ID = T.StatisticTypeID
		where	T.ID = @current

		delete @relations

		insert into @relations
			select	[Object],
					ObjectID
			from	cache.[Object]
			where	ObjectType = @Object
					and ObjectTypeID = @ObjectID
					and (
						(@Type is not null and [Object] = @Type and ObjectID = @ID) OR (@Type is null) 
						)
						
		-- EXISTENCE
		if (@CheckType = 1)
		begin
			select	@CheckObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)'),
					@CheckObjectID = f.value('(ObjectID/text())[1]', 'int')
			from	@Configuration.nodes('/fields') as F(f)

			if @CheckObjectType = 'AttributeType'
			begin
				insert into #Statistics
					select	@StatisticTypeID as StatisticTypeID,
							R.[Object],
							R.ObjectID,
							case 
								when O.ValueExists <> 0 then @Score
								else 0
							end as Score
					from	@relations R
							outer apply (
										select		ISNULL(AttributeTypeID, 0) as ValueExists
										from		Attribute 
										where		ObjectType = R.[Object] and ObjectID = R.ObjectID and AttributeTypeID = @CheckObjectID
										group by	AttributeTypeID, ObjectType, ObjectID
										) O
			end

			if @CheckObjectType = 'ResponsibilityType'
			begin
				insert into #Statistics
					select	@StatisticTypeID as StatisticTypeID,
							R.[Object],
							R.ObjectID,
							case 
								when P.ValueExists <> 0 then @Score
								else 0
							end as Score
					from	@relations R
							outer apply (
										select		ISNULL(ResponsibilityTypeID, 0) as ValueExists
										from		[cache].[ResponsibilityItem]
										where		[Object] = R.[Object] and ObjectID = R.ObjectID and ResponsibilityTypeID = @CheckObjectID
										group by	ResponsibilityTypeID, [Object], ObjectID
										) P
			end
		end

		-- COUNT (instead of score)
		if (@CheckType = 2)	--COUNT
		begin
			select	@CheckObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)'),
					@CheckObjectID = f.value('(ObjectID/text())[1]', 'int')
			from	@Configuration.nodes('/fields') as F(f)

			if @CheckObjectType = 'AttributeType'
			begin
				insert into #Statistics
					select	@StatisticTypeID as StatisticTypeID,
							R.[Object],
							R.ObjectID,
							COALESCE(O.Score, 0) as Score
					from	@relations R
							outer apply (
										select		COUNT(1) as Score
										from		Attribute 
										where		ObjectType = R.[Object] and ObjectID = R.ObjectID and AttributeTypeID = @CheckObjectID
										group by	AttributeTypeID, ObjectType, ObjectID
										) O
			end

			if @CheckObjectType = 'ResponsibilityType'
			begin
				insert into #Statistics
					select	@StatisticTypeID as StatisticTypeID,
							R.[Object],
							R.ObjectID,
							COALESCE(O.Score, 0) as Score
					from	@relations R
							outer apply (
										select		COUNT(1) as Score
										from		[cache].[ResponsibilityItem]
										where		[Object] = R.[Object] and ObjectID = R.ObjectID and ResponsibilityTypeID = @CheckObjectID
										group by	ResponsibilityTypeID, [Object], ObjectID
										) O
			end

			-- This does a count on relationships
			if @CheckObjectType <> 'AttributeType' and @CheckObjectType <> 'ResponsibilityType'
			begin
				insert into #Statistics
					select	@StatisticTypeID as StatisticTypeID,
							R.[Object],
							R.ObjectID,
							COALESCE(O.Score, 0) as Score
					from	@relations R
							outer apply (
										select		COUNT(1) as Score
										from		[cache].[Relationship] IR
													inner join cache.[Object] ID on ID.[Object] = IR.TargetObject and ID.ObjectID = IR.TargetObjectID 
																				and ID.ObjectType = @CheckObjectType and ID.ObjectTypeID = @CheckObjectID 
																				and IR.SourceObject = R.[Object] and IR.SourceObjectID = R.ObjectID
										group by	ID.ObjectType, ID.ObjectTypeID
										) O
			end
		end

		-- PROPERTY VALUE CHECK
		if (@CheckType = 3)
		begin
			select	@PropertyName = f.value('(PropertyName/text())[1]', 'varchar(250)'),
					@Value = f.value('(PropertyValue/text())[1]', 'nvarchar(4000)')
			from	@Configuration.nodes('/fields') as F(f)

			if @Object = 'ArtifactType' and @PropertyName = 'Status'
				begin
					insert into #Statistics
						select	@StatisticTypeID as StatisticTypeID,
								R.[Object],
								R.ObjectID,
								case 
									when O.ValueExists <> 0 then @Score
									else 0
								end as Score
						from	@relations R
								outer apply (
											select		CASE 
															when [Status] = @Value then 1
															else 0
														END as ValueExists
											from		Artifact
											where		R.[Object] = 'Artifact' and ID = R.ObjectID
											) O
				end
			else
				begin
					-- A dynamic field to check.
					insert into #Statistics
						select	@StatisticTypeID as StatisticTypeID,
								R.[Object],
								R.ObjectID,
								case 
									when O.ValueExists <> 0 then @Score
									else 0
								end as Score
						from	@relations R
								outer apply (
											select	CASE 
														when F.FormattedValue = @Value then 1
														else 0
													END as ValueExists									
											from	Field F
													inner join FieldType FT on FT.[Object] = @Object and FT.ObjectID = @ObjectID 
																			and F.[ObjectType] = R.[Object] and F.ObjectID = R.ObjectID
																			and FT.Name = @PropertyName 
											) O
				end
		end

		-- PROPERTY POPULATED
		if (@CheckType = 4)
		begin
			select	@PropertyName = f.value('(PropertyName/text())[1]', 'varchar(250)')
			from	@Configuration.nodes('/fields') as F(f)

			if @PropertyName = 'Description'
				begin
					insert into #Statistics
						select	@StatisticTypeID as StatisticTypeID,
								R.[Object],
								R.ObjectID,
								case 
									when D.Description is null then 0
									when LEN(D.Description) < 25 then 0
									else @Score
								end as Score
						from	@relations R
								left join cache.ObjectDetails D on D.[Object] = R.[Object] and D.ObjectID = R.ObjectID
				end
			else
				begin
					-- A dynamic field to check.
					insert into #Statistics
						select	@StatisticTypeID as StatisticTypeID,
								R.[Object],
								R.ObjectID,
								case 
									when O.ValueExists <> 0 then @Score
									else 0
								end as Score
						from	@relations R
								outer apply (
											select	case
														when F.FormattedValue is not null then 1
														else 0
													END as ValueExists
											from	Field F
													inner join FieldType FT on FT.[Object] = @Object and FT.ObjectID = @ObjectID 
																			and F.[ObjectType] = R.[Object] and F.ObjectID = R.ObjectID
																			and FT.Name = @PropertyName 
											) O
				end
		end

		-- RELATIONSHIP
		if (@CheckType = 5)
		begin
			declare @checkRelationshipObjects table (Object varchar(50), ObjectID int)

			-- first, check legacy format
			select	@CheckObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)'),
					@CheckObjectID = f.value('(ObjectID/text())[1]', 'int')
			from	@Configuration.nodes('/fields') as F(f)

			if @CheckObjectType is not null and @CheckObjectID is not null
				begin
					insert into @checkRelationshipObjects values (@CheckObjectType, @CheckObjectID)
				end
			else
				begin
					--check new format of multiple options
					insert into @checkRelationshipObjects
						select	f.value('(Object/Type/text())[1]', 'varchar(50)'),
								f.value('(Object/ID/text())[1]', 'int')
						from	@Configuration.nodes('/fields/CheckObjects') as F(f)
				end


			insert into #Statistics
				select	@StatisticTypeID as StatisticTypeID,
						R.[Object],
						R.ObjectID,
						case 
							when O.[Count] > 0 then @Score
							else 0
						end as Score
				from	@relations R
						outer apply (
									select		COUNT(1) as [Count]
									from		[cache].[Relationship] IR
												inner join cache.[Object] D on D.[Object] = IR.TargetObject and D.ObjectID = IR.TargetObjectID 
																			and IR.SourceObject = R.[Object] and IR.SourceObjectID = R.ObjectID
												inner join @checkRelationshipObjects TT on TT.[Object] = D.ObjectType and TT.ObjectID = D.ObjectTypeID
									) O

		end

		-- FUSION OWNERSHIP
		if (@CheckType = 6)
		begin
			--select	@CheckObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)'),
			--		@CheckObjectID = f.value('(ObjectID/text())[1]', 'int')
			--from	@Configuration.nodes('/fields') as F(f)

			insert into #Statistics
				select	@StatisticTypeID as StatisticTypeID,
						R.[Object],
						R.ObjectID,
						case 
							when O.ValueExists <> 0 then @Score
							else 0
						end as Score
				from	@relations R
						outer apply (
									select		ISNULL(RelationshipOwnerObjectID, 0) as ValueExists
									from		FusionAttributeOwnerRule
									where		RelationshipOwnerObjectType = R.[Object] and RelationshipOwnerObjectID = R.ObjectID
									group by	RelationshipOwnerObjectType, RelationshipOwnerObjectID
									) O
		end

		-- ROLLUP VIA RELATIONSHIPS
		if (@CheckType = 7)
		begin
			select	@CheckObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)'),
					@CheckObjectID = f.value('(ObjectID/text())[1]', 'int')
			from	@Configuration.nodes('/fields') as F(f)

			insert into #Statistics
				select	@StatisticTypeID as StatisticTypeID,
						R.[Object],
						R.ObjectID,
						round((T.Total/C.[Count]) * @Score, 0) Score
				from	@relations R
						cross apply (
									select	count(1) as [Count] 
									from	cache.Relationships
									where	SourceObject = R.[Object] and SourceObjectID = R.ObjectID
											and TargetType = @CheckObjectType and TargetTypeID = @CheckObjectID
									) C
						outer apply (
									select	sum(dbo.GetObjectStatisticScore(TargetObject, TargetObjectID)) as Total
									from	cache.Relationships 
									where	SourceObject = R.[Object] and SourceObjectID = R.ObjectID 
											and TargetType = @CheckObjectType and TargetTypeID = @CheckObjectID
									) T
				where C.[Count] > 0
		end

		-- ROLLUP VIA OWNERSHIP
		if (@CheckType = 8)
		begin
			select	@CheckObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)'),
					@CheckObjectID = f.value('(ObjectID/text())[1]', 'int')
			from	@Configuration.nodes('/fields') as F(f)

			insert into #Statistics
				select	@StatisticTypeID as StatisticTypeID,
						R.[Object],
						R.ObjectID,
						round((T.Total/C.[Count]) * @Score, 0) Score
				from	@relations R
						cross apply (
									select	count(1) as [Count] 
									from	cache.Responsibilities
									where	ResponsibleObject = R.[Object] and ResponsibleObjectID = R.ObjectID
											and ObjectType = @CheckObjectType and ObjectTypeID = @CheckObjectID
									) C
						outer apply (
									select	sum(dbo.GetObjectStatisticScore([Object], ObjectID)) as Total
									from	cache.Responsibilities 
									where	ResponsibleObject = R.[Object] and ResponsibleObjectID = R.ObjectID 
											and ObjectType = @CheckObjectType and ObjectTypeID = @CheckObjectID
									) T
				where C.[Count] > 0
		end
		

		-- EVENT METRIC CHECK
		if (@CheckType = 9)
		begin
			declare @ValidField nvarchar(250),-- = 'ValidCount',
					@InvalidField nvarchar(250),-- = 'InvalidCount',
					@Threshold decimal(9,2),-- = 0.10,
					@TotalValid float,
					@TotalInvalid float

			select	@ValidField = f.value('(ValidField/text())[1]', 'nvarchar(250)'),
					@InvalidField = f.value('(InvalidField/text())[1]', 'nvarchar(250)'),
					@Threshold = f.value('(Threshold/text())[1]', 'decimal(9,2)')
			from	@Configuration.nodes('/fields') as F(f)


			select	@TotalValid = sum(cast(V.ValidCount as int)),
					@TotalInvalid = sum(cast(I.InvalidCount as int))
			from	cache.Relationships REL
					inner join [Rule] R on R.ID = REL.TargetObjectID and REL.TargetObject = 'Rule' and R.RuleType in (3,4)
					inner join EventGroup EG on EG.RuleID = R.ID
					inner join [Event] E on E.EventGroupID = EG.ID 
					inner join (
								select	R.ID,
										max(E.Date) as [Date]
								from	cache.Relationships REL
										inner join [Rule] R on R.ID = REL.TargetObjectID and REL.TargetObject = 'Rule' and R.RuleType in (3,4)
										inner join EventGroup EG on EG.RuleID = R.ID
										inner join [Event] E on E.EventGroupID = EG.ID
								group by R.ID					
								) F on F.ID = R.ID and F.[Date] = E.[Date]
					cross apply (
								select	Value as ValidCount
								from	FieldWithRelation
								where	ObjectType = 'Event' and ObjectID = E.ID and Name = @ValidField
								) V
					cross apply (
								select	Value as InvalidCount
								from	FieldWithRelation
								where	ObjectType = 'Event' and ObjectID = E.ID and Name = @InvalidField
								) I

			insert into #Statistics
				select	@StatisticTypeID as StatisticTypeID,
						R.[Object],
						R.ObjectID,
						case 
							when cast(@TotalInvalid / @TotalValid as decimal(9,2)) < @Threshold then @Score
							else 0
						end as Score
				from	@relations R
		end

		-- PREDICATE CHECK
		if (@CheckType = 10)
		begin
			select	@PredicateID = f.value('(Predicate/text())[1]', 'int')
			from	@Configuration.nodes('/fields') as F(f)

			insert into #Statistics
				select	@StatisticTypeID as StatisticTypeID,
						R.[Object],
						R.ObjectID,
						case 
							when O.[Count] > 0 then @Score
							else 0
						end as Score
				from	@relations R
						outer apply (
									select	count(1) as [Count]
									from	IntersectMap M
											inner join cache.Relationship IR on IR.[SourceIntersectNodeID] = M.SubjectIntersectNodeID 
																			and IR.[TargetIntersectNodeID] = M.ObjectIntersectNodeID 
																			and M.PredicateID = @PredicateID
											inner join cache.Relationship T1 on T1.SourceObject = R.[Object] 
																			and T1.SourceObjectID = R.ObjectID
																			and T1.TargetObject = IR.SourceObject 
																			and T1.TargetObjectID = IR.SourceObjectID
											inner join cache.Relationship T2 on T2.SourceObject = R.[Object] 
																			and T2.SourceObjectID = R.ObjectID
																			and T2.TargetObject = IR.TargetObject 
																			and T2.TargetObjectID = IR.TargetObjectID
									) O
		end

		set @current = @current + 1
	end

	-- now merge the Statistics table
	MERGE	Statistic AS T
	USING	(
			select	S.*,
					MS.DateStart
			from	#Statistics S
					outer apply (
								select		StatisticTypeID,
											ObjectType,
											ObjectID,
											MAX(DateStart) as DateStart
								from		Statistic
								where		StatisticTypeID = S.StatisticTypeID
											and ObjectType = S.ObjectType
											and ObjectID = S.ObjectID
								group by	StatisticTypeID,
											ObjectType,
											ObjectID
								) MS
			) AS S
	ON		(
			T.StatisticTypeID = S.StatisticTypeID
			and T.ObjectType = S.ObjectType
			and T.ObjectID = S.ObjectID
			and T.DateStart = S.DateStart
			and T.Score = S.Score
			)
		WHEN MATCHED THEN 
			UPDATE SET T.DateEnd = getutcdate()
		WHEN NOT MATCHED THEN	
			INSERT	
			VALUES	(
					S.StatisticTypeID, 
					S.ObjectType, 
					S.ObjectID,
					getutcdate(), 
					getutcdate(), 
					S.Score
					);
end