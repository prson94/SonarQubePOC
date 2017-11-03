CREATE PROCEDURE [dbo].[GetAverageScoreByObjectType]
--declare
	@type varchar(50),-- = 'Artifact',
	@id int-- = 733
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

	select	@oName = utility.GetAssetDisplayValue(A.ID),
			@oTypeName = T.Name,
			@oType = T.Object,
			@oID = T.ObjectID
	from	Asset A
			inner join AssetType T on T.ID = A.AssetTypeID  and A.[Object] = @type and A.ObjectID = @id

	select	@ObjectScore = cast(Value * 100 as int)
	from	metrics.Score
	where	getutcdate() between EffectiveStartDate and EffectiveEndDate
			and Object = @type and ObjectID = @id

	select	@AverageScore = avg(cast(Value * 100 as int))
	from	metrics.Score S
			inner join Asset A on A.Object = S.Object and A.ObjectID = S.ObjectID
			inner join AssetType T on T.ID = A.AssetTypeID and T.Object = @oType and T.ObjectID = @oID
	where	getutcdate() between S.EffectiveStartDate and S.EffectiveEndDate

	select	@type as [Object], @id as ObjectID, @oName as ObjectName, @ObjectScore as ObjectScore, 
			@oType as ObjectType, @oID as ObjectTypeID, @oTypeName as ObjectTypeName, @AverageScore as AverageScore 
end