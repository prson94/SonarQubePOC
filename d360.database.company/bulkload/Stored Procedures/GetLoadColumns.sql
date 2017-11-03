
CREATE procedure [bulkload].[GetLoadColumns]
--declare	
	@action varchar(2) = 'P', --P = Promotion, R = Relation, O = Responsibilities, BL = Business Lineage, TL = Technical Lineage
	@type varchar(50) = 'AttributeType',--'ArtifactType',--'IntersectType',--'ArtifactType',
	@id int = 50019,--7,--2,--7,
	@getLookups bit = 1
as
begin
	declare @fields table (ID int identity, FieldTypeID int, Name nvarchar(250), Required bit, PartOfKey bit, IsLookup bit)
	declare @lookups table (FieldID int, Value nvarchar(500))
	declare @current int = 1,
			@max int,
			@isLookup bit = 0,
			@fieldTypeID int

	if @action = 'O'
	begin
			insert into @fields 
				select	-1, 'Owner Type', 1, 1, 1

			insert into @lookups
				select	-1,
						'Glossary: ' + Name from ArtifactType order by Name
				select	-1,
						'Model: ' + Name from TaxonomyType order by Name
				select	-1,
						'Policy: ' + Name from PolicyType order by Name

			insert into @fields 
				select	0, 'Owner ID', 1, 1, 0

			insert into @fields 
				select	1, 'Responsibility', 1, 1, 1

			insert into @lookups
				select	1,
						Name from ResponsibilityType order by Name

			insert into @fields 
				select	2, 'Resource', 1, 1, 1

			insert into @lookups
				select	2,
						'User: ' + LastName + ', ' + FirstName from reporting.Global_Resource order by LastName, FirstName

			insert into @lookups
				select	2,
						'Group: ' + Name from [Group] order by Name
	end

	if @action = 'P'
	begin
		if @type = 'AttributeType'
		begin
			insert into @fields 
				select	-1, 'Owner Type', 1, 1, 1

			insert into @lookups
				select	-1,
						'Glossary: ' + Name from ArtifactType order by Name
			insert into @lookups
				select	-1,
						'Model: ' + Name from TaxonomyType order by Name

			insert into @fields 
				select	0, 'Owner ID', 1, 1, 0
		end --AttributeType

		if @type = 'IntersectType'
		begin
			declare @s varchar(50),
					@sid int,
					@o varchar(50),
					@oid int

			select	@s = Subject,
					@sid = SubjectID,
					@o = Object,
					@oid = ObjectID
			from	IntersectType
			where	ID = @id


			if @s = 'TaxonomyType'
			begin
				insert into @fields
					select	0, 
							'Subject Path', 
							1, 
							1, 
							0
			end
			else
			begin
				insert into @fields
					select	FT.ID, 
							'Subject ' + FT.Name, 
							FT.IsRequired, 
							FT.IsPartOfKey, 
							case FT.Type
								when 'Lookup' then cast(1 as bit)
								else cast(0 as bit)
							end as IsLookup
					from	FieldType FT 
					where	FT.IsPartOfKey = 1 and FT.Object = @s and FT.ObjectID = @sid
			end

			if @s = 'TaxonomyType'
			begin
				insert into @fields
					select	0, 
							'Object Path', 
							1, 
							1, 
							0
			end
			else
			begin
				insert into @fields
					select	FT.ID, 
							'Object ' + FT.Name, 
							FT.IsRequired, 
							FT.IsPartOfKey, 
							case FT.Type
								when 'Lookup' then cast(1 as bit)
								else cast(0 as bit)
							end as IsLookup
					from	FieldType FT 
					where	FT.IsPartOfKey = 1 and FT.Object = @o and FT.ObjectID = @oid
			end

		end --IntersectType

		if @type = 'ArtifactType'
		begin
			declare @parentTypeID int = null,
					@parentTypeName nvarchar(250) = null
			select	@parentTypeID = T.ParentID,
					@parentTypeName = P.Name
			from	ArtifactType T 
					inner join ArtifactType P on P.ID = T.ParentID
			where	T.ID = @id

			if @parentTypeID is not null
			begin
				insert into @fields 
					select	0, 
							@parentTypeName + ' ID', 
							cast(1 as bit) as IsRequired, 
							cast(1 as bit) as IsPartOfKey, 
							cast(0 as bit) as IsLookup
			end
		end --ArtifactType

		if @type = 'ReferenceItemType'
		begin
			insert into @fields values (0, 'Code', 1, 1, 0)
		end --ReferenceItemType

		if @type = 'TaxonomyType'
		begin
			declare @initialDepth int = 1,
					@maxDepth int = 1
			select @maxDepth = MaximumDepth from TaxonomyType where ID = @id
			declare @levels table (Value int)
			while  @initialDepth <= @maxDepth
			begin
				insert into @levels values (@initialDepth)
				set @initialDepth = @initialDepth + 1
			end

			insert into @fields 
				select	FT.ID, 
						case
							when TTL.Name is not null then TTL.Name + ' ' + FT.Name
							else 'Level ' + cast(L.Value as nvarchar)  + ' ' + FT.Name
						end, 
						FT.IsRequired, 
						FT.IsPartOfKey, 
						case FT.Type
							when 'Lookup' then cast(1 as bit)
							else cast(0 as bit)
						end as IsLookup
				from	@levels L 
						inner join FieldType FT on FT.IsPartOfKey = 1 and FT.Object = @type and FT.ObjectID = @id
						left join TaxonomyTypeLevel TTL on TTL.[Level] = L.Value and TaxonomyTypeID = @id
		end --TaxonomyType

		insert into @fields
			select		ID,
						Name, 
						IsRequired,
						IsPartOfKey,
						case Type
							when 'Lookup' then cast(1 as bit)
							else cast(0 as bit)
						end as IsLookup
			from		FieldType 
			where		Object = @type 
						and ObjectID = @id 
						and Type not in ('Attribute', 'ComplexRelationLookup', 'FieldFromRelationship', 'FilteredLookup', 'FusionLookup', 'OwnershipLookup', 'RefListRelationship')
						and ( (@type = 'IntersectType' and IsPartOfKey = 0) OR (@type = 'TaxonomyType' and IsPartOfKey = 0) OR (@type <> 'TaxonomyType') )
						and IsEditable = 1
			order by	ColumnOrder
		
		select @max = max(ID) from @fields

		while @current <= @max
		begin
			select	@isLookup = IsLookup, 
					@fieldTypeID = FieldTypeID
			from	@fields 
			where	ID = @current

			if @isLookup = 1 and @getLookups = 1
			begin
				insert into @lookups
					select		@current,
								[Text]
					from		FieldLookupValue
					where		FieldTypeID = @fieldTypeID
					order by	[Text]
			end
			
			set @current = @current + 1
		end
	end -- P	
	else if (@action = 'R' or @action = 'U')
	begin
		--relate / unrelate
		print 'relate / unrelate'
				
		-- look up the intersect type and get the source / target type
		
		declare @subjectType varchar(50),
				@subjectTypeName nvarchar(500),
				@subjectTypeID int,
				@objectType varchar(50),
				@objectTypeName nvarchar(500),
				@objectTypeID int
		select	@subjectType = Subject,
				@subjectTypeName = SubjectName,
				@subjectTypeID = SubjectID,
				@objectTypeName = ObjectName,
				@objectType = Object,
				@objectTypeID = ObjectID
		from	IntersectTypeDetail
		where	ID = @id
		

		-- if its a fusion attribute type we just use the name

		-- get the key fields for the target / source		

		if @objectType = 'FusionAttributeType'
		begin
			insert into @fields values (0, @subjectTypeName, 1, 1, 0)
		end
		else
		begin
			--select * from fieldtype where [object] = 'ArtifactType' and objectid = 1 and IsPartOfKey = 1
			insert into @fields
				select		ID,
							@objectTypeName + ' ' + Name, 
							1,
							1,
							case Type
								when 'Lookup' then cast(1 as bit)
								else cast(0 as bit)
							end as IsLookup
				from		FieldType 
				where		Object = @objectType 
							and ObjectID = @objectTypeID 						
							and IsPartOfKey = 1
				order by	ColumnOrder
		end

		if @objectType = 'FusionAttributeType'
		begin
			insert into @fields values (0, @objectTypeName, 1, 1, 0)
		end
		else
		begin
			insert into @fields
				select		ID,
							@subjectTypeName + ' ' + Name, 
							1,
							1,
							case Type
								when 'Lookup' then cast(1 as bit)
								else cast(0 as bit)
							end as IsLookup
				from		FieldType 
				where		Object = @subjectType 
							and ObjectID = @subjectTypeID 						
							and IsPartOfKey = 1
				order by	ColumnOrder
		end
	end -- R or U




	--Return the data
	select	Name,
			Required,
			PartOfKey,
			IsLookup,
			(
			select	Value
			from	@lookups
			where	FieldID = F.ID
			for json path
			) as Lookups
	from	@fields F
	for json path
end