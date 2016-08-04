CREATE PROCEDURE [dbo].[GetScoreHistoryByObject]
	@type varchar(50),
	@id int
AS
begin
	declare @Points int = 14,
			@DateStart datetime, @DateEnd datetime, @DateCurrent datetime,
			@Increment int, @CurrentPoints int, @MaxPoints int, @current int,
			@oType varchar(25), @oTypeID int, @score float

	declare @dates table ([Date] datetime, Score float)

	set @DateEnd = DATEADD(dd, 0, DATEDIFF(dd, 0, GETUTCDATE()))
	select	@DateStart = coalesce(min(Date), DATEADD(d, -30, @DateEnd)) 
	from	reporting.Global_Audit
	where	Object = @type 
			and ObjectID = @id
	
	select @Increment = DATEDIFF(hh, @DateStart, @DateEnd) / @Points
	insert into @dates values (@DateEnd, dbo.GetObjectStatisticScore(@type, @id)*100)

	select	@oType = Type,
			@oTypeID = TypeID
	from	utility.ObjectDetail(@type, @id)

	select	@MaxPoints = SUM(Score)
	from	StatisticType
	where	[Object] = @oType
			and ObjectID = @oTypeID
			and PartOfScore = 1

	set @current = 1
	while @current <= @Points
	begin
		set @DateCurrent = DATEADD(hh, -(@current * @Increment), @DateEnd)


		select	@CurrentPoints = SUM(S.Score)
		from	Statistic S
				inner join StatisticType T on S.StatisticTypeID = T.ID and T.PartOfScore = 1
				inner join	(
							select		StatisticTypeID,
										Max(DateStart) D
							from		Statistic S
										inner join StatisticType T on S.StatisticTypeID = T.ID and T.PartOfScore = 1
							where		S.ObjectType = @type
										and S.ObjectID = @id
										and @DateCurrent between S.DateStart and S.DateEnd
							group by	StatisticTypeID
							) M on M.StatisticTypeID = S.StatisticTypeID and M.D = S.DateStart
		where	S.ObjectType = @type
				and S.ObjectID = @id

		select	@score = round(cast(@CurrentPoints as float) / cast(@MaxPoints as float), 2)
		insert into @dates values (@DateCurrent, @score*100)
		set @current = @current + 1
	end
	select * from  @dates order by [Date]
end
