CREATE PROCEDURE GetAvailableSiteNavigation
AS
BEGIN
	SET NOCOUNT ON;

	select
		u.ID as ObjectID,
		u.Name,
		u.url as Route,
		u.Object,
		null as SortOrder,
		null as ParentID
	from
	(
		select
		ID,
		Name,
		dbo.GenerateNgObjectUrl('ArtifactType', ID, 0) As url,
		'ArtifactType' as [Object]
		FROM ArtifactType
		
		UNION ALL
		
		SELECT
		ID,
		Name,
		'a/model/classification/' + name as url,
		'TaxonomyTypeClass' as [Object]
		from TaxonomyTypeClass

		UNION ALL

		SELECT
		ID,
		Name,
		dbo.GenerateNgObjectUrl('TaxonomyType', ID, 0)  As url,
		'TaxonomyType' as [Object]
		FROM TaxonomyType
		
		UNION ALL
		
		SELECT
		ID,
		Name,
		'a/home' as url,
		'PolicyTypeClass' as [Object]
		from PolicyTypeClass
		
		UNION ALL
		
		SELECT
		ID,
		Name,
		dbo.GenerateNgObjectUrl('PolicyType', ID, 0)  As url,
		'PolicyType' as [Object]
		FROM PolicyType
	) u
	left join SiteNav v on v.Object = u.Object and v.ObjectID = u.ID
	where v.ObjectID is null 
END