CREATE FUNCTION [utility].[GetObjectName] 
(	
	@object varchar(20),
	@objectId int
)
RETURNS nvarchar(500)
AS
BEGIN
	DECLARE @result nvarchar(500)

	SET @result =	(
						select name from Artifact where @object = 'Artifact' and ID = @objectId
						union all
						select name from ReferenceItemType where @object = 'ReferenceItemType' AND ID = @objectId
						union all
						select name from [FusionAttribute] where @object = 'FusionAttribute' and ID = @objectId
						union all
						select name from [Intersect] where @object = 'Intersect' and ID = @objectId
						union all
						select name from [Map] where @object = 'Map' and ID = @objectId
						union all
						select name from [Policy] where @object = 'Policy' and ID = @objectId
						union all
						select name from [Rule] where @object = 'Rule' and ID = @objectId
						union all
						select name from [Taxonomy] where @object = 'Taxonomy' and ID = @objectId
						)

	RETURN @result
END