create procedure metrics.LoadFromStaging
as
begin
	-- 1. Update pre-existing scores
	update	T
	set		T.Value = S.Score
	from	metrics.Score T
			inner join (
						select		EffectiveDate, Object, ObjectID, Score 
						from		metrics.StagingResult 
						group by	EffectiveDate, Object, ObjectID, Score 
						) S on S.EffectiveDate = T.EffectiveStartDate and S.Object = T.Object and S.ObjectID = T.ObjectID;


	-- 2. Insert new scores
	insert	metrics.Score
			select		R.Object, 
						R.ObjectID, 
						R.EffectiveDate, 
						case
							when M.EffectiveEndDate = cast('12/31/9999' as date) then M.EffectiveEndDate
							else DATEADD(d, -1, M.EffectiveEndDate)
						end as EffectiveEndDate, 
						R.Score 
			from		metrics.StagingResult R
						outer apply	(
									select	coalesce(min(EffectiveStartDate), cast('12/31/9999' as date)) as EffectiveEndDate
									from	metrics.Score
									where	Object = R.Object and ObjectID = R.ObjectID and EffectiveStartDate > R.EffectiveDate
									) M
						left join metrics.Score T on T.EffectiveStartDate = R.EffectiveDate and T.Object = R.Object and T.ObjectID = R.ObjectID
			where		T.ID is null
			group by	R.EffectiveDate, M.EffectiveEndDate, R.Object, R.ObjectID, R.Score;

	-- 3. Merge the metric results, updating existing and adding new ones.
	merge   metrics.MapResult as T 
	using   ( 
			select  SR.MapID,
					S.ID as ScoreID,
					SR.Value
			from    metrics.StagingResult SR
					inner join metrics.Score S on S.Object = SR.Object and S.ObjectID = SR.ObjectID and S.EffectiveStartDate = SR.EffectiveDate
			) as S 
			on  (
				T.MapID = S.MapID and T.ScoreID = S.ScoreID
				)
	when    matched then 
			update
				set
				T.Value = S.Value
	when    not matched by target then 
			insert (MapID, ScoreID, [Value]) 
			values (S.MapID, S.ScoreID, S.Value);

	-- 4. End-date the older scores based on object and effective date comparisons.
	update	T
	set		T.EffectiveEndDate = DATEADD(d, -1, M.EffectiveStartDate)
	from	metrics.Score T
			inner join (
						select		MS.Object,
									MS.ObjectID,
									max(MS.EffectiveStartDate) as EffectiveStartDate 
						from		metrics.Score MS
									inner join (
												select		EffectiveDate, Object, ObjectID, Score 
												from		metrics.StagingResult 
												group by	EffectiveDate, Object, ObjectID, Score 
												) S on S.EffectiveDate = MS.EffectiveStartDate and S.Object = MS.Object and S.ObjectID = MS.ObjectID
						group by	MS.Object, 
									MS.ObjectID
						) M	on M.Object = T.Object and M.ObjectID = T.ObjectID and T.EffectiveStartDate < M.EffectiveStartDate and T.EffectiveEndDate = cast('12/31/9999' as date);

	-- 5. Clear the staging table.
	delete	SR
	from    metrics.StagingResult SR
			inner join metrics.Score S on S.Object = SR.Object and S.ObjectID = SR.ObjectID and S.EffectiveStartDate = SR.EffectiveDate;
end