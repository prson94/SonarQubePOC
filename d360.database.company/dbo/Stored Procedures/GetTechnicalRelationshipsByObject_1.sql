CREATE procedure [dbo].[GetTechnicalRelationshipsByObject]
	@ResponsibleObjectType varchar(50),
	@ResponsibleObjectID int,
	@ObjectType varchar(50),
	@ObjectID int
as
begin
	declare @IntersectID int;

	select	@IntersectID = ID
	from	[Intersect]
	where	(Subject = @ResponsibleObjectType and SubjectID = @ResponsibleObjectID and Object = @ObjectType and ObjectID = @ObjectID) OR
			(Object = @ResponsibleObjectType and ObjectID = @ResponsibleObjectID and Subject = @ObjectType and SubjectID = @ObjectID);

	EXEC GetTechnicalRelationshipsByIntersect @IntersectID;
end