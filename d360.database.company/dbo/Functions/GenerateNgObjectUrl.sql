CREATE FUNCTION [dbo].[GenerateNgObjectUrl] 
(
	@Type varchar(50),
	@TypeID int,
	@ObjectID int = 0
)
RETURNS varchar(500)
AS
BEGIN
	DECLARE @Url varchar(500)
	SET @Url = 'a'

	SET @Url = CASE @Type
	WHEN 'Artifact' THEN 'a/artifact/' +  + CAST(@TypeID as varchar(15)) + '/' + CAST(@ObjectID as varchar(15))
	WHEN 'ArtifactType' THEN 'a/artifact/' + CAST(@TypeID as varchar(15))
	WHEN 'Domain' THEN 'a/domains/' +  + CAST(@TypeID as varchar(15)) + '/' +  + CAST(@ObjectID as varchar(15))
	WHEN 'DomainType' THEN 'a/domains/' + CAST(@TypeID as varchar(15))
	WHEN 'FusionAttribute' THEN 'a/fusion/item/' + CAST(@TypeID as varchar(15)) + '/' + CAST(@ObjectID as varchar(15))
	WHEN 'Fusion' THEN 'a/fusion/' + CAST(@TypeID as varchar(15)) + '/' + + CAST(@ObjectID as varchar(15))
	WHEN 'FusionType' THEN 'a/fusion/' + CAST(@TypeID as varchar(15))
	WHEN 'Group' THEN 'a/groups/' + CAST(@ObjectID as varchar(15))	
	WHEN 'Lookup' THEN 'a/lookups/administration/' + CAST(@TypeID as varchar(15)) + '/' + + CAST(@ObjectID as varchar(15))
	WHEN 'LookupType' THEN 'a/lookups/administration/' + CAST(@TypeID as varchar(15))
	WHEN 'Policy' THEN 'a/policies/' + CAST(@TypeID as varchar(15)) + '/' + CAST(@ObjectID as varchar(15))
	WHEN 'PolicyType' THEN 'a/policies/' + CAST(@TypeID as varchar(15))
	WHEN 'Resource' THEN 'a/resources/' + CAST(@ObjectID as varchar(15))
	WHEN 'ResourceType' THEN 'a/resources/list/' + CAST(@TypeID as varchar(15))
	WHEN 'Rule' THEN 'a/rules/' + CAST(@ObjectID as varchar(15))
	WHEN 'Taxonomy' THEN 'a/model/' + CAST(@TypeID as varchar(15)) + '/' + CAST(@ObjectID as varchar(15))
	WHEN 'TaxonomyType' THEN 'a/model/' + CAST(@TypeID as varchar(15))
	END

	RETURN @Url
END

