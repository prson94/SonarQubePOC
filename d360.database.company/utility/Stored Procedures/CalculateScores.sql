CREATE procedure [utility].[CalculateScores]
--declare
	@Object varchar(50) = NULL,
	@ObjectID int = NULL,
	@Date date = null--'04/17/2017'
--set @Object = 'Artifact'
--set @ObjectID = 16437 --select * from Artifact where ID = 16437
as
begin
	SET NOCOUNT ON;

	if @Date is null
	begin
		set @Date = cast(getutcdate() as Date)
	end

	DROP TABLE IF EXISTS #MetricTypes

	create table #MetricTypes (
		ScoreTypeID int,
		ScoreTypeMetricID int,
		ScoreTypeMetricVersionID int,
		ObjectType varchar(50),
		ObjectTypeID int,
		CheckType int,
		Configuration xml,
		MaximumScore int,
		Object varchar(50),
		ObjectID int
	)
/*

	insert into #MetricTypes
		select	M.ScoreTypeID,
				M.ID as ScoreTypeMetricID,
				V.ID as ScoreTypeMetricVersionID,
				M.Object as ObjectType,
				M.ObjectID as ObjectTypeID,
				M.CheckType,
				M.Configuration,
				M.MaximumScore,
				A.Object,
				A.ObjectID
		from	ScoreType ST
				inner join ScoreTypeMetric M on M.ScoreTypeID = ST.ID  and M.Deleted = 0
				inner join	(
							select		ScoreTypeMetricID,
										max(IV.ID) as ID,
										max(IV.UpdatedOn) as UpdatedOn
							from		ScoreTypeMetricVersion IV
							group by	IV.ScoreTypeMetricID
							) V on V.ScoreTypeMetricID = M.ID
				inner join AssetType T on T.Object = M.Object and T.ObjectID = M.ObjectID 
				inner join Asset A on A.AssetTypeID = T.ID and ( (A.Object = @Object and A.ObjectID = @ObjectID) OR @ObjectID is null)

	DROP TABLE IF EXISTS #ScoreMetrics
	create table #ScoreMetrics (
		ScoreID bigint null,
		Object varchar(50),
		ObjectID int,
		ScoreTypeID int,
		[Date] date,
		ScoreTypeMetricVersionID int,
		MetricValue decimal(6,3),
	)

	insert into #ScoreMetrics
		select	NULL,
				T.Object,
				T.ObjectID,
				T.ScoreTypeID,
				@Date,
				T.ScoreTypeMetricVersionID,
				case 
					when T.CheckType = 1 and f.value('(ObjectType/text())[1]', 'varchar(50)') = 'AttributeType' and C1_A.ValueExists <> 0 then T.MaximumScore
					when T.CheckType = 1 and f.value('(ObjectType/text())[1]', 'varchar(50)') = 'ResponsibilityType' and C1_R.ValueExists <> 0 then T.MaximumScore
					when T.CheckType = 2 then C2.Multiplier * T.MaximumScore
					--when T.CheckType = 3 and T.ObjectType = 'ArtifactType' and C3_S.ValueExists <> 0 then T.MaximumScore
					when T.CheckType = 3 and f.value('(PropertyName/text())[1]', 'varchar(250)') <> 'Status' and C3_F.ValueExists <> 0 then T.MaximumScore
					when T.CheckType = 4 and f.value('(PropertyName/text())[1]', 'varchar(250)') = 'Description' and C4_D.ValueExists <> 0 then T.MaximumScore
					when T.CheckType = 4 and f.value('(PropertyName/text())[1]', 'varchar(250)') <> 'Description' and C4_F.ValueExists <> 0 then T.MaximumScore
					when T.CheckType = 5 and (C5_R.ValueExists <> 0 OR C5_R2.ValueExists <> 0) then T.MaximumScore
					when T.CheckType = 6 and C6_F.ValueExists <> 0 then T.MaximumScore
					when T.CheckType = 7 and C7_R.AverageScore is not null then (C7_R.AverageScore / 100) * T.MaximumScore
					when T.CheckType = 8 and C8_O.AverageScore is not null then (C8_O.AverageScore / 100) * T.MaximumScore
					when T.CheckType = 10 and C10_P.ValueExists <> 0 then T.MaximumScore
					else 0
				end as MetricValue
		from	#MetricTypes T
				cross apply Configuration.nodes('/fields') as F(f)
				outer apply (
							select		coalesce(M.Score, 0) as Multiplier
							from		TestExternalMetric M
							where		M.Object = T.[Object]
										and M.ObjectID = T.ObjectID 
										and M.MetricVersionID = T.ScoreTypeMetricVersionID
										and T.CheckType = 2
							) C2
				outer apply (
							select		ISNULL(AttributeTypeID, 0) as ValueExists
							from		Attribute 
							where		ObjectType = T.[Object] 
										and ObjectID = T.ObjectID 
										and AttributeTypeID = f.value('(ObjectID/text())[1]', 'int')
										and f.value('(ObjectType/text())[1]', 'varchar(50)') = 'AttributeType'
										and T.CheckType = 1
							group by	AttributeTypeID, ObjectType, ObjectID
							) C1_A
				outer apply (
							select		ISNULL(ResponsibilityTypeID, 0) as ValueExists
							from		[cache].[ResponsibilityItem]
							where		[Object] = T.[Object] 
										and ObjectID = T.ObjectID 
										and ResponsibilityTypeID = f.value('(ObjectID/text())[1]', 'int')
										and f.value('(ObjectType/text())[1]', 'varchar(50)') = 'ResponsibilityType'
										and T.CheckType = 1
							group by	ResponsibilityTypeID, [Object], ObjectID
							) C1_R
				outer apply (
							select		CASE 
											when F.FormattedValue = f.value('(PropertyValue/text())[1]', 'nvarchar(4000)') then 1
											else 0
										END as ValueExists									
							from		Field F
										inner join FieldType FT on FT.[Object] = T.ObjectType and FT.ObjectID = T.ObjectTypeID 
																and F.[ObjectType] = T.[Object] and F.ObjectID = T.ObjectID
																and FT.Name = f.value('(PropertyName/text())[1]', 'varchar(250)') 
																and T.CheckType = 3
							) C3_F
				outer apply (
							select		case 
											when Description is null then 0
											when LEN(Description) < 25 then 0
											else 1
										end as ValueExists
							from		cache.ObjectDetails
							where		[Object] = T.[Object] and ObjectID = T.ObjectID
										and f.value('(PropertyName/text())[1]', 'varchar(250)') = 'Description'
										and T.CheckType = 4
							) C4_D
				outer apply (
							select		CASE 
											when F.FormattedValue is not null then 1
											else 0
										END as ValueExists									
							from		Field F
										inner join FieldType FT on FT.[Object] = T.ObjectType and FT.ObjectID = T.ObjectTypeID 
																and F.[ObjectType] = T.[Object] and F.ObjectID = T.ObjectID
																and FT.Name = f.value('(PropertyName/text())[1]', 'varchar(250)') 
																and T.CheckType = 4
							) C4_F
				outer apply (
							select		case 
											when COUNT(1) > 0 then 1
											else 0
										end as ValueExists
							from		[Intersect] IR
										inner join IntersectType IRT on IRT.ID = IR.IntersectTypeID and (
																										(IR.Subject = T.Object and IR.SubjectID = T.ObjectID) OR 
																										(IR.Object = T.Object and IR.ObjectID = T.ObjectID)
																										)
										cross apply T.Configuration.nodes('/fields/CheckObjects') as R(r) 
							where		r.value('(Object/Type/text())[1]', 'varchar(50)') = case 
											when (IR.Subject = T.Object and IR.SubjectID = T.ObjectID) then IRT.Object 
											else IRT.Subject
										end
										and r.value('(Object/ID/text())[1]', 'int') = case 
											when (IR.Subject = T.Object and IR.SubjectID = T.ObjectID) then IRT.ObjectID
											else IRT.SubjectID
										end
										and T.CheckType = 5
							) C5_R
				outer apply (
							select		case 
											when COUNT(1) > 0 then 1
											else 0
										end as ValueExists
								from [Intersect] IR
								cross apply T.Configuration.nodes('/fields/CheckObjects') as R(r)
								where IR.IntersectTypeID = r.value('(IntersectType/text())[1]','varchar(50)')
								and ((IR.Subject = T.Object and IR.SubjectID = T.ObjectID) or (IR.Object = T.Object and IR.ObjectID = T.ObjectID))
								and T.CheckType = 5
							
							) C5_R2
				outer apply (
							select		ISNULL(ArtifactID, 0) as ValueExists
							from		FusionOwner
							where		ArtifactID = T.ObjectID
										and T.CheckType = 6
							group by	ArtifactID
							) C6_F
				outer apply (
							select	(sum(S.Value) / Count(1)) as [AverageScore],
									max(S.Date) as [Date] 
							from	[Intersect] I
									inner join IntersectType IT on	IT.ID = I.IntersectTypeID
																	and (
																		(I.Subject = T.[Object] and I.SubjectID = T.ObjectID) OR
																		(I.Object = T.[Object] and I.ObjectID = T.ObjectID)
																		)
																	and (
																		f.value('(ObjectType/text())[1]', 'varchar(25)') = case 
																			when (I.Subject = T.[Object] and I.SubjectID = T.ObjectID) then IT.Object
																			else IT.Subject
																		end 
																		and f.value('(ObjectID/text())[1]', 'int') = case 
																			when (I.Subject = T.[Object] and I.SubjectID = T.ObjectID) then IT.ObjectID
																			else IT.SubjectID
																		end
																		)
									left join Score S on	S.Object =	case 
																			when I.Subject = T.Object and I.SubjectID = T.ObjectID then I.Object
																			else I.Subject
																		end 
															and S.ObjectID =	case 
																					when I.Subject = T.Object and I.SubjectID = T.ObjectID then I.ObjectID
																					else I.SubjectID
																				end
															and S.ScoreTypeID = f.value('(ScoreTypeID/text())[1]', 'int')
							where	T.CheckType = 7
							) C7_R	-- ROLLUP VIA RELATIONSHIPS
				outer apply (
							select	(sum(S.Value) / Count(1)) as [AverageScore],
									max(S.Date) as [Date] 
							from	[cache].[Responsibilities] R
									left join Score S on S.Object = R.Object and S.ObjectID = R.ObjectID --and S.ScoreTypeID = f.value('(ScoreTypeID/text())[1]', 'int')
							where	T.CheckType = 8
									and R.ResponsibleObject = T.[Object] 
									and R.ResponsibleObjectID = T.ObjectID
									and R.ObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)') 
									and R.ObjectTypeID = f.value('(ObjectID/text())[1]', 'int')
							) C8_O	-- ROLLUP VIA OWNERSHIP
				outer apply (
							select	case 
										when COUNT(1) > 0 then 1
										else 0
									end as ValueExists
							from	[Intersect] I
									inner join IntersectType IT on IT.ID = I.IntersectTypeID and 
																IT.PredicateID = f.value('(Predicate/text())[1]', 'int') and 
																(
																(I.Subject = T.Object and I.SubjectID = T.ObjectID) OR
																(I.Object = T.Object and I.ObjectID = T.ObjectID)
																)
							where	T.CheckType = 10
							) C10_P	-- PREDICATE CHECK

	-- Gets results from merge statement below (OUTPUT)
	DROP TABLE IF EXISTS #Scores
	create table #Scores (ScoreID bigint, Object varchar(50), ObjectID int, ScoreTypeID int, Date date, [Action] varchar(15), CurrentScore int not null, NewScore int null)

	MERGE	Score AS T
	USING	(
			select		Object,
						ObjectID,
						ScoreTypeID,
						Date
			from		#ScoreMetrics
			group by	Object,
						ObjectID,
						ScoreTypeID,
						Date
			) AS S
	ON		(
			T.ScoreTypeID = S.ScoreTypeID
			and T.Object = S.Object
			and T.ObjectID = S.ObjectID
			and T.Date = S.Date
			)
	WHEN MATCHED THEN
		UPDATE	
		SET		T.Date = S.Date
	WHEN NOT MATCHED THEN	
		INSERT	
		VALUES	(S.Object, S.ObjectID, S.ScoreTypeID, S.Date, 0)
	OUTPUT inserted.ID, S.Object, S.ObjectID, S.ScoreTypeID, S.Date, $Action, inserted.Value, null into #Scores;

	--update the ScoreID column based on merge above.
	update	T
	set		T.ScoreID = S.ScoreID
	from	#ScoreMetrics T
			inner join #Scores S on S.Object = T.Object and S.ObjectID = T.ObjectID and S.ScoreTypeID = T.ScoreTypeID and S.Date = T.Date; 

	-- merge the results into the ScoreMetric table.
	MERGE	ScoreMetric AS T
	USING	(
			select	distinct
					ScoreID,
					ScoreTypeMetricVersionID,
					MetricValue
			from	#ScoreMetrics
			) AS S
	ON		(
			T.ScoreID = S.ScoreID
			and T.ScoreTypeMetricVersionID = S.ScoreTypeMetricVersionID
			)
	WHEN MATCHED THEN
		UPDATE	
		SET		T.Value = coalesce(S.MetricValue, 0)
	WHEN NOT MATCHED THEN	
		INSERT	
		VALUES	(S.ScoreID, S.ScoreTypeMetricVersionID, coalesce(S.MetricValue, 0));

	update	T
	set		T.Value = coalesce(S.Value, 0)
	from	Score T
	inner join	(
				select		CAST(ROUND( (SUM(MetricValue) / SUM(V.MaximumScore)) * 100, 0) as int) as Value,
							ScoreID
				from		#ScoreMetrics SM
							inner join ScoreTypeMetricVersion V on V.ID = SM.ScoreTypeMetricVersionID
				group by	ScoreID
				) S on S.ScoreID = T.ID;

	-- Now get which scores changed. 
	update	T
	set		T.NewScore = NS.Value
	from	#Scores T
			OUTER APPLY	(
						SELECT		TOP 1 
									*
						FROM		[Score]
						WHERE		Object = T.Object and ObjectID = T.ObjectID and ScoreTypeID = T.ScoreTypeID
						ORDER BY	[Date] DESC
						) NS;

	insert into [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select	'EventTopicNotification', 
				'<fields><ChangeType>ScoreUpdate</ChangeType><ObjectType>' + T.Object + '</ObjectType><ObjectTypeID>' + cast(T.ObjectID as varchar) + '</ObjectTypeID><Score>' + cast(S.NewScore as varchar) + '</Score></fields>',
				S.Object, 
				S.ObjectID
		from	#Scores S
				inner join Asset O on O.Object = S.Object and O.ObjectID = S.ObjectID
				inner join AssetType T on T.ID = O.AssetTypeID
		where	S.CurrentScore <> S.NewScore
				and S.[Action] = 'UPDATE';
*/
end
