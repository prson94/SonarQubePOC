CREATE PROCEDURE [dbo].[GetAverageScoreByObjectType]
	@type varchar(50),
	@id int
AS
begin
	declare 
			@oName nvarchar(250),
			@oTypeName nvarchar(250),
			@oType varchar(50),
			@oID int,
			@AveragePoints int,
			@MaxPoints int,
			@AverageScore int,
			@ObjectScore varchar(250)--int

	select	@oName = Name,
			@oTypeName = ObjectTypeName,
			@oType = ObjectType,
			@oID = ObjectTypeID
	from	cache.ObjectDetails 
	where	[Object] = @type and ObjectID = @id

	select	@MaxPoints = SUM(Score)
	from	StatisticType
	where	[Object] = @oType
			and ObjectID = @oID
			and PartOfScore = 1

	select	@AveragePoints = AVG(S.Score)
	from	(
			select	S.ObjectType, S.ObjectID, SUM(S.Score) as Score
			from	(
					select		S.ObjectType, 
								S.ObjectID, 
								S.StatisticTypeID,
								MAX(S.DateEnd) as Date
					from		Statistic S
								inner join cache.[Object] O on O.[Object] = S.ObjectType and O.ObjectID = S.ObjectID and O.ObjectType = @oType
								inner join StatisticType T on S.StatisticTypeID = T.ID and T.[Object] = @oType and T.ObjectID = @oID and T.PartOfScore = 1
					group by	S.ObjectType, S.ObjectID, S.StatisticTypeID
					) FS
					inner join Statistic S on S.ObjectType = FS.ObjectType and S.ObjectID = FS.ObjectID and S.DateEnd = FS.Date and S.StatisticTypeID = FS.StatisticTypeID
			group by	S.ObjectType, S.ObjectID
			) S

	select @AverageScore = cast(round(round(cast(@AveragePoints as float) / cast(@MaxPoints as float), 2) * 100, 0) as int)	
	select @ObjectScore = dbo.GetObjectStatisticScore(@type, @id)*100

	select	@type as [Object], @id as ObjectID, @oName as ObjectName, @ObjectScore as ObjectScore, 
			@oType as ObjectType, @oID as ObjectTypeID, @oTypeName as ObjectTypeName, @AverageScore as AverageScore 
end