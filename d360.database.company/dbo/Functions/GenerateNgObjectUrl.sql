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
		WHEN 'Artifact' THEN 'artifact/' +  + CAST(@TypeID as varchar) + '/' + CAST(@ObjectID as varchar)
		WHEN 'ArtifactType' THEN 'artifact/' + CAST(@TypeID as varchar)
		WHEN 'Domain' THEN 'domain/' +  + CAST(@TypeID as varchar) + '/' +  + CAST(@ObjectID as varchar)
		WHEN 'DomainType' THEN 'domain/' + CAST(@TypeID as varchar)
		WHEN 'ReferenceItem' THEN 'reference/' +  + CAST(@TypeID as varchar)-- + '/' +  + CAST(@ObjectID as varchar)
		WHEN 'ReferenceItemType' THEN 'reference/' + CAST(@TypeID as varchar)
		WHEN 'FusionAttribute' THEN 'fusion/fusionattribute/' + CAST(@TypeID as varchar) + '/' + CAST(@ObjectID as varchar)		
		WHEN 'Fusion' THEN 'fusion/' + CAST(@TypeID as varchar) + '/' + + CAST(@ObjectID as varchar)
		WHEN 'FusionType' THEN 'fusion/' + CAST(@TypeID as varchar)
		WHEN 'Group' THEN 'group/' + CAST(@ObjectID as varchar)	
		WHEN 'Lookup' THEN 'admin/lookups/' + CAST(@TypeID as varchar) + '/' + + CAST(@ObjectID as varchar)
		WHEN 'LookupType' THEN 'admin/lookups/' + CAST(@TypeID as varchar)
		WHEN 'Policy' THEN 'policy/' + CAST(@TypeID as varchar(15)) + '/id/' + CAST(@ObjectID as varchar)
		WHEN 'PolicyType' THEN 'policy/' + CAST(@TypeID as varchar) + '/structure'		
		WHEN 'Resource' THEN 'resource/' + CAST(@ObjectID as varchar)
		WHEN 'ResourceType' THEN 'resource/list/' + CAST(@TypeID as varchar)
		WHEN 'Rule' THEN 'quality/rule/' + CAST(@TypeID as varchar) + '/' + CAST(@ObjectID as varchar)
		WHEN 'RuleType' THEN 'quality/rule/' + CAST(@TypeID as varchar)	
		WHEN 'Taxonomy' THEN 'model/' + CAST(@TypeID as varchar) + '/id/' + CAST(@ObjectID as varchar)
		WHEN 'TaxonomyType' THEN 'model/' + CAST(@ObjectID as varchar) + '/structure'	
		WHEN 'ShoppingCartType' THEN 'cart/' + CAST(@ObjectID as varchar)	
	END

	SET @Url = @Prefix + @Url

	RETURN @Url
END
