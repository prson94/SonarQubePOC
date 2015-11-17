

CREATE PROCEDURE [dbo].[GetFieldNamesByObjectType]
	@type varchar(50),
	@id int
AS
BEGIN
	declare @t table (Name nvarchar(250))

	if (@type = 'ArtifactType')
	begin
		insert into @t values ('Name')
		insert into @t values ('Description')
		--insert into @t values ('Status')
		insert into @t values ('ParentID')
		insert into @t values ('TaxonomyTypeID')
	end
	if (@type = 'AttributeType')
	begin
		insert into @t values ('ObjectID')
	end
	if (@type = 'Domain')
	begin
		insert into @t values ('Name')
		insert into @t values ('Description')
	end
	if (@type = 'DomainItem')
	begin
		insert into @t values ('Code')
		insert into @t values ('Name')
		insert into @t values ('Description')
	end
	--if (@type = 'LookupType')
	--begin
	--end
	if (@type = 'TaxonomyType')
	begin
		insert into @t values ('Name')
		insert into @t values ('Description')
	end

	select Name, cast(0 as bit) as IsCustomField from @t
	union
	select Name, cast(1 as bit) as IsCustomField from FieldTypeWithRelation where [Object] = @type and ObjectID = @id
END



