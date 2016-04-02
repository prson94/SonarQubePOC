CREATE FUNCTION [dbo].[GetObjectStatisticScore]
(
--declare
	@type varchar(25) = 'Resource',
	@id int = 1
)
RETURNS float
AS
BEGIN
	declare @current int,
			@max int,
			@oType varchar(25),
			@oTypeID int,
			@score float

	select	@oType = ObjectType,
			@oTypeID = ObjectTypeID
	from	cache.[Object]
	where	[Object] = @type and ObjectID = @id

	select	@current = SUM(S.Score)
	from	Statistic S
			inner join StatisticType T on S.StatisticTypeID = T.ID and T.PartOfScore = 1 and T.[Object] = @oType and T.ObjectID = @oTypeID
			inner join	(
						select		StatisticTypeID,
									Max(DateStart) D
						from		Statistic S
									inner join StatisticType T on S.StatisticTypeID = T.ID and T.PartOfScore = 1
						where		S.ObjectType = @type
									and S.ObjectID = @id
						group by	StatisticTypeID
						) M on M.StatisticTypeID = S.StatisticTypeID and M.D = S.DateStart
	where	S.ObjectType = @type
			and S.ObjectID = @id

	select	@max = SUM(Score)
	from	StatisticType
	where	[Object] = @oType
			and ObjectID = @oTypeID
			and PartOfScore = 1

	select	@score = round(cast(cast(@current as float) / cast(@max as float) as float), 2)

	return @score
END
