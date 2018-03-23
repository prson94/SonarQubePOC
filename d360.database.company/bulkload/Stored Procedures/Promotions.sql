create procedure [bulkload].[Promotions]
--declare
	@id int
--set @id = 84
as
begin
	set nocount on;

	declare @levels table (rowIndex int, [level] int, processed bit);

	declare @Object varchar(50),
			@ObjectID int,
			@Action varchar(1),
			@UpdatedOn datetime = getutcdate(),
			@UpdatedBy int = 0
				
	select	@Object = [Object], 
			@ObjectID = ObjectID,
			@Action = [Action],
			@UpdatedBy = UpdatedBy
	from	[Load]
	where	ID = @id;

	update	LoadItem
	set		Object = null, 
			ObjectID = null, 
			Status = null,
			StatusMessage = null
	where	LoadID = @id;

	-- Process hashes for Load Items
	if @Object = 'ReferenceItemType'
	begin
		update	T
		set		T.KeyHash = CONVERT(
									varchar(32), 
									SUBSTRING(HASHBYTES('SHA1', substring(ltrim(rtrim(IC.Value)), 1, 250)), 3, 32), 
									2),
				T.FieldHash = V.FieldHash
		from	LoadItem T
				inner join LoadColumn C on C.LoadID = T.LoadID and C.Name = 'Code'
				inner join LoadItemColumn IC on IC.LoadID = C.LoadID and IC.RowIndex = T.RowIndex and IC.ColumnIndex = C.ColumnIndex
				inner join	(
							select		RowIndex,
										CONVERT(
											varchar(32), 
											SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
											2) as FieldHash
							from		(
										select		top 100 percent
													I.RowIndex,
													FT.ID as FieldTypeID,
													coalesce(IC.Value, '') as Value
										from		LoadItem I
													inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id
													inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex = IC.ColumnIndex
													inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name
										order by	I.RowIndex,
													FT.ID
										) A
							group by	A.RowIndex	
							) V on V.RowIndex = T.RowIndex
		where	T.LoadID = @id;
	end
	else if @Object = 'TaxonomyType'
	begin
		declare @currRow int, @maxRow int, @currLevel int;
		set @currRow = 1;
		set @currLevel = 0;
		set @maxRow = (select max(RowIndex) from LoadItem where LoadID = @id);	

		while @currRow < @maxRow
		begin
			set @currRow = @currRow + 1;

			--get level for current row
			select		@currLevel = coalesce(max(L.[Level]), 1) 
			from		TaxonomyTypeLevel L
						inner join LoadColumn LC on LC.LoadID = @id and L.Name = substring(LC.[Name], 1, len(LC.[Name]) - charindex(' ', reverse(LC.[Name])))
						inner join LoadItemColumn LI on LI.LoadID = @id and LI.RowIndex = @currRow and LI.ColumnIndex = LC.ColumnIndex and LI.[Value] is not null
			where		L.TaxonomyTypeID = @ObjectID

			insert into @levels (rowIndex, level, processed) values (@currRow, @currLevel, 0);

			--update the key hash based on the current level
			update	T
			set		T.KeyHash = K.KeyHash,
					T.FieldHash = V.FieldHash
			from	LoadItem T
					left join	(
								select		RowIndex,
											CONVERT(
												varchar(32), 
												SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
												2) as KeyHash
								from		(
												select top 100 percent
													IC.RowIndex, 
													FT.ID as FieldTypeID, 
													coalesce(IC.[Value],'') as [Value] 
												from LoadColumn LC
												inner join LoadItemColumn IC on IC.LoadID = @id and IC.RowIndex = @currRow and IC.ColumnIndex = LC.ColumnIndex
												inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.IsPartOfKey = 1 and FT.Name = reverse(substring(reverse(LC.[Name]), 0, charindex(' ',reverse(LC.[Name]))))			
												where LC.LoadID = @id and LC.ColumnIndex in (
			 										select		LC.ColumnIndex 
													from		TaxonomyTypeLevel L
																inner join LoadColumn LC on LC.LoadID = @id and L.Name = substring(LC.[Name], 1, len(LC.[Name]) - charindex(' ', reverse(LC.[Name])))
																inner join LoadItemColumn LI on LI.LoadID = @id and LI.RowIndex = @currRow and LI.ColumnIndex = LC.ColumnIndex and LI.[Value] is not null
													where		L.TaxonomyTypeID = @ObjectID and L.[Level] = @currLevel
													)
											) A
								group by	A.RowIndex
								) K on K.RowIndex = T.RowIndex
					inner join	(
								select		RowIndex,
											CONVERT(
												varchar(32), 
												SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
												2) as FieldHash
								from		(
											select		top 100 percent
														I.RowIndex,
														FT.ID as FieldTypeID,
														coalesce(IC.Value, '') as Value
											from		LoadItem I
														inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id
														inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex = IC.ColumnIndex
														inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name
											order by	I.RowIndex,
														FT.ID
											) A
								group by	A.RowIndex	
								) V on V.RowIndex = T.RowIndex
			where	T.LoadID = @id and T.RowIndex = @currRow;
		end
	end
	else
	begin
		update	T
		set		T.KeyHash = K.KeyHash,
				T.FieldHash = V.FieldHash
		from	LoadItem T
				inner join	(
							select		RowIndex,
										CONVERT(
											varchar(32), 
											SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
											2) as KeyHash
							from		(
										select		top 100 percent
													I.RowIndex,
													FT.ID as FieldTypeID,
													coalesce(IC.Value, '') as Value
										from		LoadItem I
													inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id
													inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex = IC.ColumnIndex
													inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.IsPartOfKey = 1 and FT.Name = C.Name
										order by	I.RowIndex,
													FT.ID
										) A
							group by	A.RowIndex
							) K on K.RowIndex = T.RowIndex
				inner join	(
							select		RowIndex,
										CONVERT(
											varchar(32), 
											SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
											2) as FieldHash
							from		(
										select		top 100 percent
													I.RowIndex,
													FT.ID as FieldTypeID,
													coalesce(IC.Value, '') as Value
										from		LoadItem I
													inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id
													inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex = IC.ColumnIndex
													inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name
										order by	I.RowIndex,
													FT.ID
										) A
							group by	A.RowIndex	
							) V on V.RowIndex = T.RowIndex
		where	T.LoadID = @id;
	end
	-- -----------------------------
	
	-- Resolve Single-value LOOKUP fields
	exec [bulkload].[UpdateDynamicLookupFieldColumns] @id

	-- Resolve Multi-value LOOKUP fields
	update	IC
	set		IC.LookupObject = MV.LookupObject,
			IC.LookupValue = MV.LookupValue
	from	LoadItemColumn IC
			inner join	(
						select		IC.LoadID,
									IC.RowIndex,
									IC.ColumnIndex,
									'ReferenceItem' as LookupObject,
									string_agg(AD.ID, ',') as LookupValue
						from		LoadItem LI
									inner join LoadItemColumn IC on LI.LoadID = @id and LI.LoadID = IC.LoadID and IC.RowIndex = LI.RowIndex
									inner join LoadColumn C on C.LoadID = IC.LoadID and C.ColumnIndex = IC.ColumnIndex
									inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and FT.AllowMultipleValues = 1
									cross apply string_split(IC.Value, ',') VS									
									left join ReferenceItem AD on AD.ReferenceITemTypeID = FT.LookupObjectID
									CROSS APPLY [dbo].[GetReferenceItemDisplayValue](AD.ID, FT.ID) GRIDV
						where GRIDV.DisplayValue = ltrim(rtrim(VS.Value))
						group by	IC.LoadID,
									IC.RowIndex,
									IC.ColumnIndex			
						) MV on MV.LoadID = IC.LoadID and MV.RowIndex = IC.RowIndex and MV.ColumnIndex = IC.ColumnIndex

	-- Log error messages for reference list resolution.
	update	LI
	set		LI.StatusMessage = coalesce(LI.StatusMessage,'') + FT.Name + ' could not be resolved to an existing reference item.' 
	from	LoadItem LI
			inner join LoadItemColumn IC on LI.LoadID = @id and IC.LoadID = LI.LoadID and IC.RowIndex = LI.RowIndex
			inner join LoadColumn C on C.ColumnIndex = IC.ColumnIndex and C.LoadID = IC.LoadID
			inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
										and FT.Name = C.Name 
										and FT.Type = 'Lookup' 
										and FT.LookupObjectType = 'ReferenceItem' 
										and (FT.IsRequired = 1 or FT.IsPartOfKey = 1) 
										and ( 
												(FT.AllowMultipleValues = 0 AND IC.LookupObjectID is null) OR 
												(FT.AllowMultipleValues = 1 AND IC.LookupValue is null)
											);

	-- Resolve Allow All LOOKUP field values
	update	IC
	set		IC.LookupObject = REPLACE(FT.LookupObjectType, 'Type', ''),
			IC.LookupObjectID = 0,
			IC.LookupValue = 0
	from	LoadItemColumn IC
			inner join LoadColumn C on C.ColumnIndex = IC.ColumnIndex and C.LoadID = IC.LoadID and C.LoadID = @id
			inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and FT.Type = 'Lookup' and FT.AllowAllValue = 1 and IC.Value = FT.AllowAllLabel;

	-- Resolve RELATIONSHIP fields
	declare @relFieldLookups table (LoadID int, RowIndex int, ColumnIndex int, Object varchar(50), ObjectID int )

	insert into @relFieldLookups
		select	IC.LoadID,
				Ic.RowIndex,
				IC.ColumnIndex,
				D.Object,
				D.ObjectID
		from	LoadItemColumn IC
				inner join LoadColumn C on C.ColumnIndex = IC.ColumnIndex and C.LoadID = IC.LoadID and C.LoadID = @id
				inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and FT.Type = 'Relationship'
				inner join IntersectType IT on FT.LookupObjectType = 'IntersectType' and FT.LookupObjectID = IT.ID
				inner join AssetType DT on DT.Object = case when IT.Subject = @Object and IT.SubjectID = @ObjectID then IT.Object else IT.Subject end
											and DT.ObjectID = case when IT.Subject = @Object and IT.SubjectID = @ObjectID then IT.ObjectID else IT.SubjectID end
				inner join dbo.GetAssetDisplayValue() D on D.AssetTypeID = DT.ID and D.DisplayValue = ltrim(rtrim(IC.Value));

	update	T
	set		T.LookupObject = S.Object,
			T.LookupObjectID = S.ObjectID
	from	LoadItemColumn T
			inner join @relFieldLookups S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex;


	-- Capture changes for logging purposes.
	--declare @tbl table (ObjectID int, RowIndex int, [Action] varchar(1), [FieldsLoaded] bit null, [RelationshipsLoaded] bit null);

	IF OBJECT_ID('tempdb..#tbl') IS NOT NULL
			DROP TABLE #tbl;

	create table #tbl (ObjectID int, RowIndex int, [Action] varchar(1), [FieldsLoaded] bit null, [RelationshipsLoaded] bit null);
	
	CREATE CLUSTERED INDEX PK_tempTbl ON #tbl ([RowIndex] ASC,[Action] ASC);

	--declare @insertToPerform table (RowID int identity, KeyHash varchar(250));
	IF OBJECT_ID('tempdb..#insertToPerform') IS NOT NULL
			DROP TABLE #insertToPerform;

	create table #insertToPerform (RowID int identity, KeyHash varchar(250));
	
	CREATE CLUSTERED INDEX PK_tempinsertToPerform ON #insertToPerform ([KeyHash] ASC);

	--declare @insertOutputID table (RowID int identity, ObjectID int);
	IF OBJECT_ID('tempdb..#insertOutputID') IS NOT NULL
			DROP TABLE #insertOutputID;

	create table #insertOutputID (RowID int identity, ObjectID int);
	
	-- COMMON ------------------
	-- Identify which load items already exist based on key hash.
	update	T
	set		T.Object = A.Object,
			T.ObjectID = A.ObjectID
	from	LoadItem T
			inner join AssetType ST on ST.Object = @Object and ST.ObjectID = @ObjectID
			inner join GetAssetKeyHash() S on S.AssetTypeID = ST.ID and S.KeyHash = T.KeyHash and T.LoadID = @id
			inner join Asset A on A.ID = S.ID;
	
	-- ARTIFACTS ---------------
	if @Object = 'ArtifactType'
	begin
		-- Mark the existing artifacts as being updated.
		update	T
		set		T.UpdatedBy = @UpdatedBy,
				T.UpdatedOn = @UpdatedOn
		from	Artifact T
				inner join LoadItem S on S.LoadID = @id and S.Object = 'Artifact' and S.ObjectID = T.ID and T.ArtifactTypeID = @ObjectID;

		-- Insert the updated records into temp table for logging.
		insert into #tbl 
			select	ObjectID,
					RowIndex,
					'U', null, null
			from	LoadItem
			where	LoadID = @id 
					and ObjectID is not null;

		-- Insert new items into the Artifact table.
		insert into #insertToPerform
			select	distinct
					KeyHash
			from	LoadItem
			where	LoadID = @id
					and ObjectID is null
					and KeyHash is not null;

		--declare @insertOutputID table (RowID int identity, ObjectID int);
		insert Artifact (ArtifactTypeID, UpdatedOn, UpdatedBy, CreatedOn, CreatedBy)
		output inserted.ID into #insertOutputID
			select	@ObjectID, 
					@UpdatedOn, 
					@UpdatedBy, 
					@UpdatedOn, 
					@UpdatedBy
			from	#insertToPerform;

		-- Insert the added records into temp table for logging.
		insert into #tbl 
			select	N.ObjectID,
					I.RowIndex,
					'A', null, null
			from	LoadItem I
					inner join #insertToPerform P on P.KeyHash = I.KeyHash and I.LoadID = @id 
					inner join #insertOutputID N on N.RowID = P.RowID;

		-- Update the LoadItem table with the Object and ObjectID generated from the insert above.
		update	T
		set		T.Object = 'Artifact',
				T.ObjectID = S.ObjectID
		from	LoadItem T
				inner join #tbl S on T.LoadID = @id and S.RowIndex = T.RowIndex and S.[Action] = 'A';
	end
	-------------------------

	-- MODEL ----------------
   if @Object = 'TaxonomyType'
   begin
		declare 
			@row int, 
			@level int, 
			@rows int, 
			@rowObject varchar(50), 
			@rowObjectId int, 
			@parentKeyHash varchar(50),
			@intersectTypeid int,
			@parentObjectId int;

		declare @ids table (id int);

		set @row = 0;
		set @level = 0;

		while (select count(*) from @levels where processed = 0) > 0
		begin
			set @parentKeyHash = null;
			set @parentObjectId = null;
			delete from @ids;

			--need to process rows in order of level (low to high) to make sure parent items are added or exist
			select		top 1
						@row = L.RowIndex, 
						@level = L.[Level], 
						@rowObject = LC.[Object], 
						@rowObjectId = LC.ObjectID 
			from		@levels L
						inner join LoadItem LC on LC.RowIndex = L.RowIndex and LC.LoadID = @id
			where		L.processed = 0
			order by	L.[Level] asc;
			
			if @rowObjectId is not null
			begin
				update	Taxonomy
				set		UpdatedOn = @UpdatedOn,
						UpdatedBy = @UpdatedBy
				where	ID = @rowObjectId;
			end
			else
			begin
				if @level > 1
				begin
					--hash key fields at (level - 1) and check against asset or LoadItem
					select @parentKeyHash = CONVERT(
									varchar(32), 
									SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
									2)
					from		(
									select		top 100 percent
												FT.ID as FieldTypeID, 
												coalesce(IC.[Value],'') as [Value] 
									from		LoadColumn LC
												inner join LoadItemColumn IC on IC.LoadID = @id and IC.RowIndex = @row and IC.ColumnIndex = LC.ColumnIndex
												inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.IsPartOfKey = 1 
													and FT.Name = reverse(substring(reverse(LC.[Name]), 0, charindex(' ',reverse(LC.[Name]))))			
									where		LC.LoadID = @id and LC.ColumnIndex in (
			 										select	LC.ColumnIndex 
													from	TaxonomyTypeLevel L
															inner join LoadColumn LC on LC.LoadID = @id and L.Name = substring(LC.[Name], 1, len(LC.[Name]) - charindex(' ', reverse(LC.[Name])))
															inner join LoadItemColumn LI on LI.LoadID = @id and LI.RowIndex = @row and LI.ColumnIndex = LC.ColumnIndex and LI.[Value] is not null
													where	L.TaxonomyTypeID = @ObjectID and L.[Level] = (@level-1)
													)
								) A;

					select @parentObjectId = coalesce(
							(
							select		top 1 
										a.ObjectID 
							from		Asset A
										inner join AssetType T on T.Object = @Object and T.ObjectID = @ObjectID and A.AssetTypeID = T.ID
										inner join GetAssetKeyHash() H on H.ID = A.ID
							where		H.KeyHash = @parentKeyHash
							),
							(
							select		top 1 
										a.ObjectID 
							from		LoadItem L
										inner join Asset A on A.[Object] = L.[Object] and A.ObjectID = L.ObjectID
							where		LoadID = @id and L.KeyHash = @parentKeyHash
							)
						);
					
					if @parentObjectId is not null
					begin
						insert Taxonomy (TaxonomyTypeID, UpdatedOn, UpdatedBy)
						output inserted.ID into @ids
							select	@ObjectID, 
									@UpdatedOn, 
									@UpdatedBy;

						insert into #tbl
						select	id,
								@row,
								'A', null, null
						from	@ids
					
						select  @intersectTypeId = id 
						from	intersecttypedetail 
						where	[subject] = @Object and subjectid = @ObjectID 
								and [object] = @Object and objectid = @objectID
								and predicatetype = 4;
						
						if @intersectTypeId is not null 
							and not exists (
								select		1 
								from		[Intersect] 
								where		IntersectTypeID = @intersectTypeId 
											and ObjectID = (select id from @ids) 
											and SubjectID = @parentObjectId)
						begin						
							insert into [Intersect] (IntersectTypeId, [Subject], [Object], SubjectID, ObjectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
							select	@intersectTypeId as IntersectTypeId,
									'Taxonomy' as [Subject],
									'Taxonomy' as [Object],
									@parentObjectId as SubjectID,
									(select id from @ids) as ObjectID,
									@UpdatedBy as CreatedBy,
									@UpdatedOn as CreatedOn,
									@UpdatedBy as UpdatedBy,
									@UpdatedOn as UpdatedOn,
									'BulkLoad' as [Owner];
						end
					end
				end
				else --root item
				begin			
					insert Taxonomy (TaxonomyTypeID, UpdatedOn, UpdatedBy)
					output inserted.ID into @ids
						select	@ObjectID, 
								@UpdatedOn, 
								@UpdatedBy;

					insert into #tbl
					select	id,
							@row,
							'A', null, null
					from	@ids;									
				end
			end

			update	@levels 
			set		processed = 1 
			where	rowIndex = @row 
					and [level] = @level;

			update	T
			set		T.Object = 'Taxonomy',
					T.ObjectID = S.ObjectID
			from	LoadItem T
					inner join #tbl S on T.LoadID = @id and S.RowIndex = T.RowIndex and S.[Action] = 'A';
		end
	
	end
	--------------------------

	-- REFERENCE ------------
	if @Object = 'ReferenceItemType'
	begin
		declare @ri_insertToPerform table (RowID int identity, Code nvarchar(250), KeyHash varchar(250));
		declare @ri_insertOutputID table (RowID int identity, ObjectID int);

		-- Mark the existing items as being updated.
		update	T
		set		T.UpdatedBy = @UpdatedBy,
				T.UpdatedOn = @UpdatedOn
		from	ReferenceItem T
				inner join LoadItem S on S.LoadID = @id and S.Object = 'ReferenceItem' and S.ObjectID = T.ID and T.ReferenceItemTypeID = @ObjectID;

		-- Insert the updated records into temp table for logging.
		insert into #tbl 
			select	ObjectID,
					RowIndex,
					'U', null, null
			from	LoadItem
			where	LoadID = @id 
					and ObjectID is not null;

		-- Insert new items into the ReferenceItem table.
		insert into @ri_insertToPerform
			select	distinct
					substring(ltrim(rtrim(IC.Value)), 1, 250),
					I.KeyHash
			from	LoadItem I
					inner join LoadColumn C on C.LoadID = I.LoadID and C.Name = 'Code'
					inner join LoadItemColumn IC on C.ColumnIndex = IC.ColumnIndex and IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex 
			where	I.LoadID = @id
					and I.ObjectID is null
					and I.KeyHash is not null;

		insert ReferenceItem (ReferenceItemTypeID, Code, UpdatedOn, UpdatedBy, CreatedOn, CreatedBy)
		output inserted.ID into @ri_insertOutputID
			select	@ObjectID, 
					Code,
					@UpdatedOn, 
					@UpdatedBy, 
					@UpdatedOn, 
					@UpdatedBy
			from	@ri_insertToPerform;

		-- Insert the added records into temp table for logging.
		insert into #tbl 
			select	N.ObjectID,
					I.RowIndex,
					'A', null, null
			from	LoadItem I
					inner join @ri_insertToPerform P on P.KeyHash = I.KeyHash and I.LoadID = @id 
					inner join @ri_insertOutputID N on N.RowID = P.RowID;

		-- Update the LoadItem table with the Object and ObjectID generated from the insert above.
		update	T
		set		T.Object = 'ReferenceItem',
				T.ObjectID = S.ObjectID
		from	LoadItem T
				inner join #tbl S on T.LoadID = @id and S.RowIndex = T.RowIndex and S.[Action] = 'A';
	end
	-------------------------
	

	-- Capture field logs	
	IF OBJECT_ID('tempdb..#fields') IS NOT NULL
			DROP TABLE #fields;

	create table #fields (RowIndex int, ColumnIndex int, [Action] varchar(25));

	--CREATE CLUSTERED INDEX PK_tempFields ON #fields ([RowIndex] ASC,[ColumnIndex] ASC);

	-- Non-relationship fields
	merge	Field as T
	using	(
			select	I.FieldTypeID,
					I.Type,
					I.AllowMultipleValues,
					I.Object,
					I.ObjectID,
					case 
						when I.Type = 'Lookup' and I.AllowMultipleValues = 0 then cast(C.LookupObjectID as nvarchar)
						when I.Type = 'Lookup' and I.AllowMultipleValues = 1 then C.LookupValue
						else C.Value
					end as [Value],
					C.RowIndex,
					C.ColumnIndex
			from	(
					select		I.LoadID,
								FT.ID as FieldTypeID,
								FT.Type,
								FT.AllowMultipleValues,
								I.Object,
								I.ObjectID,
								min(I.RowIndex) as RowIndex,
								C.ColumnIndex
					from		LoadItem I
								inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id
								inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex
								inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
														and  (
															FT.Name = LC.Name or
																(
																	@Object = 'TaxonomyType'
																	 and LC.ColumnIndex in (
																		select LC2.ColumnIndex from TaxonomyTypeLevel L2
																		inner join LoadColumn LC2 on LC2.LoadID = @id and L2.[Name] = substring(LC2.[Name], 1, len(LC2.[Name]) - charindex(' ', reverse(LC2.[Name])))
																		inner join LoadItemColumn LI2 on LI2.LoadID = @id and LI2.RowIndex = C.RowIndex and LI2.ColumnIndex = LC2.ColumnIndex and LI2.[Value] is not null
																		where L2.TaxonomyTypeID = @ObjectID and L2.[Level] = (select [level] from @levels where rowIndex = C.RowIndex)
																	 )
																	 and FT.Name = reverse(substring(reverse(LC.[Name]), 0, charindex(' ',reverse(LC.[Name])))) 
																)
															)
														and FT.Type <> 'Relationship' 
														and ( 
																(FT.Type <> 'Lookup' and C.Value is not null) OR 
																(FT.Type = 'Lookup' and FT.AllowMultipleValues = 0 and C.LookupObjectID is not null) OR
																(FT.Type = 'Lookup' and FT.AllowMultipleValues = 1 and C.LookupValue is not null)
															)
					where		I.ObjectID is not null
					group by	I.LoadID,
								FT.ID,
								FT.Type,
								FT.AllowMultipleValues,
								I.Object,
								I.ObjectID,
								C.ColumnIndex
					) I
					inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and C.ColumnIndex = I.ColumnIndex
			) S on (T.FieldTypeID = S.FieldTypeID and S.Object = T.ObjectType and S.ObjectID = T.ObjectID)
	when matched then
		update	set
				Value = S.Value
	when not matched then
		insert (FieldTypeID, ObjectType, ObjectID, Value)
		values (S.FieldTypeID, S.Object, S.ObjectID, S.Value)
	output S.RowIndex, S.ColumnIndex, $action into #fields;

	delete	T
	from	FieldValue T
			left join (
				select		FT.ID as FieldTypeID,
							I.Object,
							I.ObjectID,
							VS.Value
				from		LoadItem I
							inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id and I.ObjectID is not null and C.LookupValue is not null
							cross apply string_split(C.LookupValue, ',') VS
							inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex
							inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
													and FT.Name = LC.Name 
													and FT.Type = 'Lookup' 
													and FT.AllowMultipleValues = 1
			) S on S.FieldTypeID = T.FieldTypeID and S.Object = T.ObjectType and S.ObjectID = T.ObjectID and S.value = T.Value
			inner join (	--LIMITS THE IMPACT OF THIS STATEMENT
				select		FT.ID as FieldTypeID,
							I.Object,
							I.ObjectID
				from		LoadItem I
							inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id and I.ObjectID is not null and C.LookupValue is not null
							inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex
							inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
													and FT.Name = LC.Name 
													and FT.Type = 'Lookup' 
													and FT.AllowMultipleValues = 1			
			) L on L.FieldTypeID = T.FieldTypeID and L.Object = T.ObjectType and L.ObjectID = T.ObjectID
	where	S.FieldTypeID is null;

	insert into FieldValue (FieldTypeID, ObjectType, ObjectID, Value)
		select		FT.ID,
					I.Object,
					I.ObjectID,
					VS.Value
		from		LoadItem I
					inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id and I.ObjectID is not null and C.LookupValue is not null
					cross apply string_split(C.LookupValue, ',') VS
					inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex
					inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
											and FT.Name = LC.Name 
											and FT.Type = 'Lookup' 
											and FT.AllowMultipleValues = 1
					left join FieldValue FV on FV.FieldTypeID = FT.ID and FV.ObjectType = I.Object and FV.ObjectID = I.ObjectID and FV.Value = VS.Value
		where		FV.ID is null;

	update	T
	set		T.FieldsLoaded = 1
	from	#tbl T
			inner join	(
						select		RowIndex,
									[Action]
						from		#fields
						group by	RowIndex, 
									[Action]
						) S on S.RowIndex = T.RowIndex;

	truncate table #fields;

	-- Parent fields
	declare @parentTypeID int = null,
			@parentTypeName nvarchar(250) = null;
	declare @parentIntersectTypeId int = null;

	select 
		@parentTypeID = I.SubjectID,
		@parentTypeName = I.SubjectName,
		@parentIntersectTypeId = I.ID
	from 
		intersecttypedetail I                
	where I.[PredicateType] = 3 and [Object] = @Object and ObjectID = @ObjectId;
	
	if @parentTypeID is not null
	begin
	
		-- look for column with the parent type name this contains the parent 
		merge	[Intersect] as T
		using	(
				select	distinct
						AD.ObjectID as ParentObjectID,
						AD.[Object] as ParentObject,
						AD.[TypeID] as ParentTypeID,
						AD.[Type] as ParentType,
						@parentIntersectTypeId as IntersectTypeID,
						LI.[Object] as ItemObject,
						LI.ObjectID as ItemObjectID
				from	LoadItem I
						inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id
						inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex and LC.Name = @parentTypeName
						inner join AssetDetail AD on AD.TypeID = @parentTypeID and AD.DisplayValue = C.Value and AD.[Type] = @Object	
						inner join LoadItem LI on LI.RowIndex = C.RowIndex and LI.LoadID = C.LoadID					
				where	I.ObjectID is not null
				) S on (T.IntersectTypeID = S.IntersectTypeID and T.Subject = S.ParentObject and T.SubjectID = S.ParentObjectID and T.Object = S.ItemObject and T.ObjectID = S.ItemObjectID)

		when matched then
			update	set
					T.Subject	= S.ParentObject,
					T.SubjectID = S.ParentObjectID,
					T.Object	= S.ItemObject,
					T.ObjectID	= S.ItemObjectID
		when not matched then
			insert (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner], Visible)
			values (
					S.IntersectTypeID,
					S.ParentObject, 
					S.ParentObjectID,
					S.ItemObject, 
					S.ItemObjectID, 
					0, @UpdatedBy, @UpdatedOn, @UpdatedBy, @UpdatedOn, 'BulkLoad', 1
					);

	end

	-- Relationship fields
	merge	[Intersect] as T
	using	(
			select	distinct
					FT.LookupObjectID as IntersectTypeID,
					case 
						when IT.Subject = @Object and IT.SubjectID = @ObjectID then cast(1 as bit)
						else cast(0 as bit)
					end as IsSubject,
					case 
						when IT.Subject = @Object and IT.SubjectID = @ObjectID then I.Object
						else C.LookupObject
					end as Subject,
					case 
						when IT.Subject = @Object and IT.SubjectID = @ObjectID then I.ObjectID
						else C.LookupObjectID
					end as SubjectID,
					case 
						when IT.Subject = @Object and IT.SubjectID = @ObjectID then C.LookupObject
						else I.Object
					end as Object,
					case 
						when IT.Subject = @Object and IT.SubjectID = @ObjectID then C.LookupObjectID
						else I.ObjectID
					end as ObjectID
			from	LoadItem I
					inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id
					inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex
					inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
											and FT.Name = LC.Name and FT.Type = 'Relationship' 
											and C.LookupObject is not null and C.LookupObjectID is not null
					inner join IntersectType IT on FT.LookupObjectType = 'IntersectType' and FT.LookupObjectID = IT.ID
			where	I.ObjectID is not null
			) S on (
					T.IntersectTypeID = S.IntersectTypeID 
					and (
							(S.IsSubject = 1 and S.Subject = T.Subject and S.SubjectID = T.SubjectID) OR
							(S.IsSubject = 0 and S.Object = T.Object and S.ObjectID = T.ObjectID)
						)
					)
	when matched then
		update	set
				T.Subject	= S.Subject,
				T.SubjectID = S.SubjectID,
				T.Object	= S.Object,
				T.ObjectID	= S.ObjectID
	when not matched then
		insert (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner], Visible)
		values (
				S.IntersectTypeID,
				S.Subject, 
				S.SubjectID,
				S.Object, 
				S.ObjectID, 
				0, @UpdatedBy, @UpdatedOn, @UpdatedBy, @UpdatedOn, 'BulkLoad', 1
				);
	
	-- Capture logs and update load status. -----
	update	T
	set		T.Status = 1,
			T.StatusMessage = 'Item successfully ' + case S.[Action] when 'A' then 'added' else 'updated' end + '.'
	from	LoadItem T
			inner join #tbl S on T.LoadID = @id and S.RowIndex = T.RowIndex and T.[Object] is not null and T.ObjectID is not null;

	update	LoadItem
	set		Status = 0,
			StatusMessage = 'Item load failed. ' + coalesce(StatusMessage, '')
	where	([Object] is null or ObjectID is null)
			and LoadID = @id;

	----Finally, close out the Load.
	update	[Load] 
	set		DateCompleted = getutcdate()
	where	ID = @id
	---------------------------------------------
end