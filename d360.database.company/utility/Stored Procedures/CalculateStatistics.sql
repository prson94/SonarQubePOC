CREATE procedure [utility].[CalculateStatistics]
--declare
	@Type varchar(50) = NULL,
	@ID int = NULL,
	@TargetStatisticTypeID int = NULL
as
begin
	SET NOCOUNT ON;

	declare @current int, @max int
	declare @relations table (ID int identity, ObjectType varchar(50), ObjectID int, PossibleScore int)

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
				@ObjectType varchar(25),
				@ObjectID int,
				@PropertyName varchar(250),
				@Value nvarchar(4000),
				@Configuration xml

		select	@StatisticTypeID = S.ID,
				@CheckType = S.CheckType,
				@Configuration = S.Configuration 
		from	#StatisticTypes T
				inner join StatisticType S on S.ID = T.StatisticTypeID
		where	T.ID = @current

		delete @relations

		insert into @relations
			select	O.ObjectType, 
					O.ObjectID,
					R.Score
			from	StatisticTypeRelation R
					cross apply	(
								select	ID as ObjectID,
										'Artifact' as ObjectType,
										ArtifactTypeID as TypeID
								from	Artifact 
								where	R.ObjectType = 'ArtifactType' and ArtifactTypeID = R.ObjectID
										and (
											(@Type = 'Artifact' and ID = @ID and @Type is not null) OR (@Type is null) 
											)
								union
								select	ID as ObjectID,
										'Domain' as ObjectType,
										DomainTypeID as TypeID
								from	Domain 
								where	R.ObjectType = 'DomainType' and DomainTypeID = R.ObjectID
										and (
											(@Type = 'Domain' and ID = @ID and @Type is not null) OR (@Type is null) 
											)
								union
								select	ID as ObjectID,
										'Fusion' as ObjectType,
										FusionTypeID as TypeID
								from	Fusion 
								where	R.ObjectType = 'FusionType' and FusionTypeID = R.ObjectID
										and (
											(@Type = 'Fusion' and ID = @ID and @Type is not null) OR (@Type is null) 
											)
								union	
								select	ID as ObjectID,
										'Taxonomy' as ObjectType,
										TaxonomyTypeID as TypeID
								from	Taxonomy 
								where	R.ObjectType = 'TaxonomyType' and TaxonomyTypeID = R.ObjectID
										and (
											(@Type = 'Taxonomy' and ID = @ID and @Type is not null) OR (@Type is null) 
											)
								union	
								select	ID as ObjectID,
										'Group' as ObjectType,
										0 as TypeID
								from	[Group]
								where	R.ObjectType = 'Group' and R.ObjectID = 0
										and (
											(@Type = 'Group' and ID = @ID and @Type is not null) OR (@Type is null) 
											)
								union	
								select	ResourceID as ObjectID,
										'Resource' as ObjectType,
										1 as TypeID
								from	reporting.Global_Resource 
								where	R.ObjectType = 'Resource' and R.ObjectID = 1
										and (
											(@Type = 'Resource' and ResourceID = @ID and @Type is not null) OR (@Type is null) 
											)
								) O
			where	StatisticTypeID = @StatisticTypeID

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
							R.ObjectType,
							R.ObjectID,
							case 
								when O.ValueExists <> 0 then R.PossibleScore
								else 0
							end as Score
					from	@relations R
							outer apply (
										select		ISNULL(AttributeTypeID, 0) as ValueExists
										from		Attribute 
										where		ObjectType = R.ObjectType and ObjectID = R.ObjectID and AttributeTypeID = @CheckObjectID
										group by	AttributeTypeID, ObjectType, ObjectID
										) O
			end

			if @CheckObjectType = 'ResponsibilityType'
			begin
				insert into #Statistics
					select	@StatisticTypeID as StatisticTypeID,
							R.ObjectType,
							R.ObjectID,
							case 
								when P.ValueExists <> 0 then R.PossibleScore
								when S.ValueExists <> 0 then R.PossibleScore
								else 0
							end as Score
					from	@relations R
							outer apply (
										select		ISNULL(ResponsibilityTypeID, 0) as ValueExists
										from		ResponsibilityDetail
										where		ObjectType = R.ObjectType and ObjectID = R.ObjectID and ResponsibilityTypeID = @CheckObjectID
										group by	ResponsibilityTypeID, ObjectType, ObjectID
										) P
							outer apply (
										select		ISNULL(ResponsibilityTypeID, 0) as ValueExists
										from		SourcingResponsibilityDetail
										where		ObjectType = R.ObjectType and ObjectID = R.ObjectID and ResponsibilityTypeID = @CheckObjectID
										group by	ResponsibilityTypeID, ObjectType, ObjectID
										) S
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
							R.ObjectType,
							R.ObjectID,
							COALESCE(O.Score, 0) as Score
					from	@relations R
							outer apply (
										select		COUNT(AttributeTypeID) as Score
										from		Attribute 
										where		ObjectType = R.ObjectType and ObjectID = R.ObjectID and AttributeTypeID = @CheckObjectID
										group by	AttributeTypeID, ObjectType, ObjectID
										) O
			end

			--if @CheckObjectType = 'EventType'
			--begin
			--	insert into #Statistics
			--		select	@StatisticTypeID as StatisticTypeID,
			--				R.ObjectType,
			--				R.ObjectID,
			--				O.Score
			--		from	@relations R
			--				outer apply (
			--							select		COUNT(RoleID) as Score
			--							from		[Event]
			--							where		EventTypeID = R.ObjectType and ObjectID = R.ObjectID and RoleID = @CheckObjectID
			--							group by	EventTypeID, ObjectID, RoleID
			--							) O
			--end

			if @CheckObjectType = 'ResponsibilityType' --this is different in that it stores the role that you need to check instead of the OwnershipTypeID.
			begin
				insert into #Statistics
					select	@StatisticTypeID as StatisticTypeID,
							R.ObjectType,
							R.ObjectID,
							COALESCE(O.Score, 0) as Score
					from	@relations R
							outer apply (
										select		COUNT(1) as Score
										from		ResponsibilityDetail
										where		ObjectType = R.ObjectType and ObjectID = R.ObjectID and ResponsibilityTypeID = @CheckObjectID
										group by	ResponsibilityTypeID, ObjectType, ObjectID
										) O
			end
		end

		-- PROPERTY VALUE CHECK
		if (@CheckType = 3)
		begin
			select	@CheckObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)'),
					@CheckObjectID = f.value('(ObjectID/text())[1]', 'int'),
					@PropertyName = f.value('(PropertyName/text())[1]', 'varchar(250)'),
					@Value = f.value('(Value/text())[1]', 'nvarchar(4000)')
			from	@Configuration.nodes('/fields') as F(f)

			if @PropertyName = 'Status'
			begin
				insert into #Statistics
					select	@StatisticTypeID as StatisticTypeID,
							R.ObjectType,
							R.ObjectID,
							case 
								when O.ValueExists <> 0 then R.PossibleScore
								else 0
							end as Score
					from	@relations R
							outer apply (
										select		CASE 
														when [Status] = @Value then 1
														else 0
													END as ValueExists
										from		Artifact
										where		R.ObjectType = 'Artifact' and ID = R.ObjectID
										) O
			end
		end

		-- PROPERTY POPULATED
		if (@CheckType = 4)
		begin
			select	@CheckObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)'),
					@CheckObjectID = f.value('(ObjectID/text())[1]', 'int'),
					@PropertyName = f.value('(PropertyName/text())[1]', 'varchar(250)')
			from	@Configuration.nodes('/fields') as F(f)

			if @PropertyName = 'Description'
			begin
				insert into #Statistics
					select	@StatisticTypeID as StatisticTypeID,
							R.ObjectType,
							R.ObjectID,
							case 
								when D.Description is null then 0
								when LEN(D.Description) < 25 then 0
								else R.PossibleScore
							end as Score
					from	@relations R
							left join cache.ObjectDetails D on D.[Object] = R.ObjectType and D.ObjectID = R.ObjectID
							--outer apply utility.ObjectDetail(R.ObjectType, R.ObjectID) D
			end
		end

		-- RELATIONSHIP
		if (@CheckType = 5)
		begin
			select	@CheckObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)'),
					@CheckObjectID = f.value('(ObjectID/text())[1]', 'int')
			from	@Configuration.nodes('/fields') as F(f)

			insert into #Statistics
				select	@StatisticTypeID as StatisticTypeID,
						R.ObjectType,
						R.ObjectID,
						case 
							when O.ValueExists <> 0 then R.PossibleScore
							else 0
						end as Score
				from	@relations R
						outer apply (
									select	count(1) as ValueExists
									from	IntersectNode SN 
											inner join IntersectNode TN on TN.IntersectID = SN.IntersectID
											inner join IntersectTypeNode TNT on TNT.ID = TN.IntersectTypeNodeID and TNT.ObjectType = @CheckObjectType and TNT.ObjectID = @CheckObjectID
									where	SN.ObjectType = R.ObjectType 
											and SN.ObjectID = R.ObjectID
									) O

		end

		-- FUSION OWNERSHIP
		if (@CheckType = 6)
		begin
			select	@CheckObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)'),
					@CheckObjectID = f.value('(ObjectID/text())[1]', 'int')
			from	@Configuration.nodes('/fields') as F(f)

			insert into #Statistics
				select	@StatisticTypeID as StatisticTypeID,
						R.ObjectType,
						R.ObjectID,
						case 
							when O.ValueExists <> 0 then R.PossibleScore
							else 0
						end as Score
				from	@relations R
						outer apply (
									select		ISNULL(RelationshipOwnerObjectID, 0) as ValueExists
									from		FusionAttributeOwnerRule
									where		RelationshipOwnerObjectType = R.ObjectType and RelationshipOwnerObjectID = R.ObjectID
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
						R.ObjectType,
						R.ObjectID,
						round((T.Total/C.[Count]) * R.PossibleScore, 0) Score
				from	@relations R
						cross apply (
									select	count(1) as [Count] 
									from	cache.Relationships 
									where	SourceObject = R.ObjectType and SourceObjectID = R.ObjectID
											and TargetType = @CheckObjectType and TargetTypeID = @CheckObjectID
									) C
						outer apply (
									select	sum(dbo.GetObjectStatisticScore(TargetObject, TargetObjectID)) as Total
									from	cache.Relationships 
									where	SourceObject = R.ObjectType and SourceObjectID = R.ObjectID 
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
						R.ObjectType,
						R.ObjectID,
						round((T.Total/C.[Count]) * R.PossibleScore, 0) Score
				from	@relations R
						cross apply (
									select	count(1) as [Count] 
									from	cache.Responsibilities
									where	ResponsibleObject = R.ObjectType and ResponsibleObjectID = R.ObjectID
											and ObjectType = @CheckObjectType and ObjectTypeID = @CheckObjectID
									) C
						outer apply (
									select	sum(dbo.GetObjectStatisticScore([Object], ObjectID)) as Total
									from	cache.Responsibilities 
									where	ResponsibleObject = R.ObjectType and ResponsibleObjectID = R.ObjectID 
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
						R.ObjectType,
						R.ObjectID,
						case 
							when cast(@TotalInvalid / @TotalValid as decimal(9,2)) < @Threshold then R.PossibleScore
							else 0
						end as Score
				from	@relations R
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
