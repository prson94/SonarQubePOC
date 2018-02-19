CREATE FUNCTION [dbo].[GetChildObjectIds]
(
	@type varchar(50),
	@id int
)
RETURNS TABLE
AS
RETURN
(
	select I.ObjectID as ChildID from Asset A
	inner join AssetType ST on ST.ID = A.AssetTypeID
	inner join [IntersectType] T on T.[Subject] = ST.[Object] and T.SubjectID = ST.ObjectID
	inner join [Predicate] P on P.ID = T.PredicateID and P.[Type] = 3
	inner join [Intersect] I on I.Subject = @type and I.SubjectID = @id and I.IntersectTypeID = T.ID
	where A.[Object] = @type and A.ObjectID = @id
)
