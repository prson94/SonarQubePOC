CREATE procedure [dbo].[GetTechnicalRelationshipsByObject]
	@ResponsibleObjectType varchar(50),
	@ResponsibleObjectID int,
	@ObjectType varchar(50),
	@ObjectID int
as
begin

declare @IntersectID int;

select @IntersectID = i.ID 
from [Intersect] i
join IntersectNode n1 on n1.IntersectID = i.ID
join IntersectNode n2 on n2.IntersectID = i.ID and n2.ID != n1.ID
where n1.ObjectType = @ResponsibleObjectType and n1.ObjectID = @ResponsibleObjectID
and	  n2.ObjectType = @ObjectType and n2.ObjectID = @ObjectID;

EXEC GetTechnicalRelationshipsByIntersect @IntersectID;

end