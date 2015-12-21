CREATE FUNCTION [dbo].[GenerateObjectUrl] 
(
	@Type varchar(50),
	@TypeID int,
	@ObjectID int = 0
)
RETURNS varchar(500)
AS
BEGIN
	DECLARE @Url varchar(500)
	SET @Url = '#'

	SET @Url = CASE @Type
	WHEN 'Artifact' THEN '#/artifacts/' +  + CAST(@TypeID as varchar(15)) + '/' + CAST(@ObjectID as varchar(15))
	WHEN 'ArtifactType' THEN '#/artifacts/' + CAST(@TypeID as varchar(15))
	WHEN 'Domain' THEN '#/domains/' +  + CAST(@TypeID as varchar(15)) + '/' +  + CAST(@ObjectID as varchar(15))
	WHEN 'DomainType' THEN '#/domains/' + CAST(@TypeID as varchar(15))
	WHEN 'FusionAttribute' THEN '#/fusion/item/' + CAST(@TypeID as varchar(15)) + '/' + CAST(@ObjectID as varchar(15))
	WHEN 'Fusion' THEN '#/fusion/' + CAST(@TypeID as varchar(15)) + '/' + + CAST(@ObjectID as varchar(15))
	WHEN 'FusionType' THEN '#/fusion/' + CAST(@TypeID as varchar(15))
	WHEN 'Group' THEN '#/groups/' + CAST(@ObjectID as varchar(15))
	WHEN 'Event' THEN '#/monitor/results/' + CAST(@TypeID as varchar(15)) + '/' + + CAST(@ObjectID as varchar(15))
	WHEN 'EventGroup' THEN '#/monitor/results/' + CAST(@TypeID as varchar(15)) + '?group=' + + CAST(@ObjectID as varchar(15))
	WHEN 'EventType' THEN '#/monitor/results/' + CAST(@TypeID as varchar(15))
	WHEN 'Lookup' THEN '#/lookups/administration/' + CAST(@TypeID as varchar(15)) + '/' + + CAST(@ObjectID as varchar(15))
	WHEN 'LookupType' THEN '#/lookups/administration/' + CAST(@TypeID as varchar(15))
	WHEN 'Policy' THEN '#/policies/' + CAST(@TypeID as varchar(15)) + '/' + CAST(@ObjectID as varchar(15))
	WHEN 'PolicyType' THEN '#/policies/' + CAST(@TypeID as varchar(15))
	WHEN 'Resource' THEN '#/resources/' + CAST(@ObjectID as varchar(15))
	WHEN 'ResourceType' THEN '#/resources/list/' + CAST(@TypeID as varchar(15))
	WHEN 'Rule' THEN '#/rules/' + CAST(@ObjectID as varchar(15))
	WHEN 'Taxonomy' THEN '#/catalogs/' + CAST(@TypeID as varchar(15)) + '/' + CAST(@ObjectID as varchar(15))
	WHEN 'TaxonomyType' THEN '#/catalogs/' + CAST(@TypeID as varchar(15))
	END

	RETURN @Url
END
