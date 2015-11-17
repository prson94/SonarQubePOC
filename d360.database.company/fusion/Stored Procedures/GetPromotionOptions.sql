CREATE procedure fusion.GetPromotionOptions
as
begin
	set nocount on;

	declare @tbl table (PromotionObjectType varchar(25), PromotionObjectID int, Name nvarchar(250), TypeName varchar(25), ParentObjectType varchar(25), ParentObjectTypeID int)

	insert into @tbl
		select	'Artifact', 
				ID, 
				Name, 
				'Artifact', 
				'ArtifactType', 
				ParentID 
		from	ArtifactType 

	insert into @tbl
		select 'Taxonomy', 
				ID, 
				Name, 
				'Information Model', 
				'TaxonomyType',
				ID
		from	TaxonomyType 

	insert into @tbl
		select	'Domain', 
				ID, 
				Name, 
				'Domain', 
				NULL, 
				NULL 
		from	DomainType 

	insert into @tbl
		select	'DomainItem', 
				0, 
				Name, 
				'Domain Item', 
				'DomainType',
				ID 
		from	DomainType

	select * from @tbl
end