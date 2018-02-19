CREATE FUNCTION [dbo].[GetParentObjectId]
(
	@type varchar(50),
	@id int
)
RETURNS int
AS
BEGIN
	return
	(
	select I.SubjectID as ParentID from Asset A
	inner join AssetType ST on ST.ID = A.AssetTypeID
	inner join [IntersectType] T on T.Object = ST.Object and T.ObjectID = ST.ObjectID
	inner join [Predicate] P on P.ID = T.PredicateID and P.[Type] = 3
	inner join [Intersect] I on I.Object = @type and I.ObjectID = @id and I.IntersectTypeID = T.ID
	where A.Object = @type and A.ObjectID = @id
	)
END