CREATE PROCEDURE dbo.GetStatisticDetails
	@Type varchar(25),
	@ID int
AS
	select	* 
	from	(
			select		top 100 percent
						T.Name,
						'Currently' as Slug,
						cast(S.Score as  varchar(25)) as Score
			from		Statistic S
						inner join StatisticType T on S.StatisticTypeID = T.ID and S.ObjectType = @Type and S.ObjectID = @ID and T.PartOfScore = 0
			order by	T.Name
			) S
	union
	select	'Score' as Name,
			'Currently' as Slug,
			cast(dbo.GetObjectStatisticScore(@Type, @ID) * 100 as varchar(25)) + '%' as Score