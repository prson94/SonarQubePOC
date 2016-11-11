CREATE FUNCTION [dbo].[GenerateNgObjectUrl] 
(
	@Type varchar(50),
	@TypeID int,
	@ObjectID int = 0
)
RETURNS varchar(500)
AS
BEGIN
	DECLARE @Prefix varchar(5) = ''--'a/'
	DECLARE @Url varchar(500)
	SET @Url = @Prefix

	SET @Url = CASE @Type
		WHEN 'Artifact' THEN 'artifact/' +  + CAST(@TypeID as varchar(15)) + '/' + CAST(@ObjectID as varchar(15))
		WHEN 'ArtifactType' THEN 'artifact/' + CAST(@TypeID as varchar(15))
		WHEN 'Domain' THEN 'domain/' +  + CAST(@TypeID as varchar(15)) + '/' +  + CAST(@ObjectID as varchar(15))
		WHEN 'DomainType' THEN 'domain/' + CAST(@TypeID as varchar(15))
		WHEN 'FusionAttribute' THEN 'fusion/fusionattribute/' + CAST(@TypeID as varchar(15)) + '/' + CAST(@ObjectID as varchar(15))
		WHEN 'Fusion' THEN 'fusion/' + CAST(@TypeID as varchar(15)) + '/' + + CAST(@ObjectID as varchar(15))
		WHEN 'FusionType' THEN 'fusion/' + CAST(@TypeID as varchar(15))
		WHEN 'Group' THEN 'groups/' + CAST(@ObjectID as varchar(15))	
		WHEN 'Lookup' THEN 'lookups/administration/' + CAST(@TypeID as varchar(15)) + '/' + + CAST(@ObjectID as varchar(15))
		WHEN 'LookupType' THEN 'lookups/administration/' + CAST(@TypeID as varchar(15))
		WHEN 'Policy' THEN 'policy/' + CAST(@TypeID as varchar(15)) + '/' + CAST(@ObjectID as varchar(15))
		WHEN 'PolicyType' THEN 'policy/' + CAST(@TypeID as varchar(15)) + '/structure'
		WHEN 'Resource' THEN 'resource/' + CAST(@ObjectID as varchar(15))
		WHEN 'ResourceType' THEN 'resource/list/' + CAST(@TypeID as varchar(15))
		WHEN 'Rule' THEN 'rule/' + CAST(@ObjectID as varchar(15))
		WHEN 'Taxonomy' THEN 'model/' + CAST(@TypeID as varchar(15)) + '/id/' + CAST(@ObjectID as varchar(15))
		WHEN 'TaxonomyType' THEN 'model/' + CAST(@ObjectID as varchar(15)) + '/structure'
	END

	SET @Url = @Prefix + @Url

	RETURN @Url
END
