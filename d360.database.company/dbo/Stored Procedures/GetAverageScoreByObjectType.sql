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
			--utility.ObjectDetail(@type, @id)

	select	@MaxPoints = SUM(Score)
	from	StatisticTypeRelation R
			inner join StatisticType T on R.StatisticTypeID = T.ID and T.PartOfScore = 1
	where	R.ObjectType = @oType
			and R.ObjectID = @oID

	select	@AveragePoints = AVG(S.Score)
	from	(
			select	S.ObjectType, S.ObjectID, SUM(S.Score) as Score
			from	(
					select		S.ObjectType, 
								S.ObjectID, 
								S.StatisticTypeID,
								MAX(S.DateEnd) as Date
					from		Statistic S
								left join Artifact O1 on S.ObjectType = 'Artifact' and O1.ID = S.ObjectID and @oType = 'ArtifactType'
								left join Taxonomy O2 on S.ObjectType = 'Taxonomy' and O2.ID = S.ObjectID and @oType = 'TaxonomyType'
								left join Domain O3 on S.ObjectType = 'Domain' and O3.ID = S.ObjectID and @oType = 'DomainType'
								left join reporting.Global_Resource O4 on S.ObjectType = 'Resource' and O4.ResourceID = S.ObjectID and @oType = 'ResourceType'
								left join [Group] O5 on S.ObjectType = 'Group' and O5.ID = S.ObjectID and @oType = 'Group'
								inner join StatisticTypeRelation R on R.StatisticTypeID = S.StatisticTypeID and R.ObjectType = @oType and R.ObjectID = @oID
								inner join StatisticType T on R.StatisticTypeID = T.ID and T.PartOfScore = 1
					where		@oID = coalesce(
												O1.ArtifactTypeID, 
												O2.TaxonomyTypeID, 
												O3.DomainTypeID, 
												iif(O4.ResourceID is not null, 1, null),
												0
												)
					group by	S.ObjectType, S.ObjectID, S.StatisticTypeID
					) FS
					inner join Statistic S on S.ObjectType = FS.ObjectType and S.ObjectID = FS.ObjectID and S.DateEnd = FS.Date and S.StatisticTypeID = FS.StatisticTypeID
			group by	S.ObjectType, S.ObjectID
			) S

	select @AverageScore = cast(round(round(cast(@AveragePoints as float) / cast(@MaxPoints as float), 2) * 100, 0) as int)	
	select @ObjectScore = dbo.GetObjectStatisticScore(@type, @id)*100	 --cast(dbo.GetObjectStatisticScore(@type, @id)*100 as int)		

	select	@type as [Object], @id as ObjectID, @oName as ObjectName, @ObjectScore as ObjectScore, 
			@oType as ObjectType, @oID as ObjectTypeID, @oTypeName as ObjectTypeName, @AverageScore as AverageScore 
end