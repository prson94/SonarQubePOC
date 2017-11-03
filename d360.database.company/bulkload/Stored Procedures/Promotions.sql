
CREATE procedure [bulkload].[Promotions]
--declare
	@id int
--set @id = 11
as
begin
	set nocount on;

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
	
	-- Resolve LOOKUP fields
	update	IC
	set		IC.LookupObject = 'ReferenceItem',--L.LookupObjectType,
			IC.LookupObjectID = L.ID --L.LookupObjectID
	from	LoadItemColumn IC
			inner join LoadColumn C on C.ColumnIndex = IC.ColumnIndex and C.LoadID = IC.LoadID and C.LoadID = @id
			inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and FT.Type = 'Lookup'
			inner join ReferenceItem L ON FT.LookupObjectType = 'ReferenceItem' AND FT.LookupObjectID = L.ReferenceItemTypeID and L.Visible = 1 and IC.Value = utility.GetFormattedFieldLookupValue(FT.Type, coalesce(FT.LookupEditFormat, FT.LookupDisplayFormat), FT.LookupObjectType, FT.LookupObjectID, L.ID);

	-- Log error messages for reference list resolution.
	update	LI
	set		LI.StatusMessage = coalesce(LI.StatusMessage,'') + FT.Name + ' could not be resolved to an existing reference item.' 
	from	LoadItem LI
			inner join LoadItemColumn IC on LI.LoadID = @id and IC.LoadID = LI.LoadID and IC.RowIndex = LI.RowIndex
			inner join LoadColumn C on C.ColumnIndex = IC.ColumnIndex and C.LoadID = IC.LoadID
			inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and FT.Type = 'Lookup' and FT.LookupObjectType = 'ReferenceItem' and (FT.IsRequired = 1 or FT.IsPartOfKey = 1) and IC.LookupObjectID is null;

	-- Resolve LOOKUP fields
	update	IC
	set		IC.LookupObject = REPLACE(FT.LookupObjectType, 'Type', ''),
			IC.LookupObjectID = 0
	from	LoadItemColumn IC
			inner join LoadColumn C on C.ColumnIndex = IC.ColumnIndex and C.LoadID = IC.LoadID and C.LoadID = @id
			inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and FT.Type = 'Lookup' and FT.AllowAllValue = 1 and IC.Value = FT.AllowAllLabel;

	-- Resolve RELATIONSHIP fields
	update	IC
	set		IC.LookupObject = 'Artifact',--L.LookupObjectType,
			IC.LookupObjectID = D.ID --L.LookupObjectID
	from	LoadItemColumn IC
			inner join LoadColumn C on C.ColumnIndex = IC.ColumnIndex and C.LoadID = IC.LoadID and C.LoadID = @id
			inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and FT.Type = 'Relationship'
			inner join IntersectType IT on FT.LookupObjectType = 'IntersectType' and FT.LookupObjectID = IT.ID
			inner join Artifact D on --D.ObjectType = case 
										--						when IT.Subject = @Object and IT.SubjectID = @ObjectID then IT.Object 
											--					else IT.Subject 
												--			   end 
										--and	
										D.ArtifactTypeID =	case 
																when IT.Subject = @Object and IT.SubjectID = @ObjectID then IT.ObjectID 
																else IT.SubjectID
															end
										and D.DisplayValue = IC.Value;

	-- Capture changes for logging purposes.
	declare @tbl table (ObjectID int, RowIndex int, [Action] varchar(1), [FieldsLoaded] bit null, [RelationshipsLoaded] bit null);
	declare @insertToPerform table (RowID int identity, KeyHash varchar(250));
	declare @insertOutputID table (RowID int identity, ObjectID int);
	
	-- ARTIFACTS ---------------
	if @Object = 'ArtifactType'
	begin
		-- Identify which load items already exist based on key hash.
		update	T
		set		T.Object = 'Artifact',
				T.ObjectID = S.ID
		from	LoadItem T
				inner join Artifact S on S.ArtifactTypeID = @ObjectID and S.KeyHash = T.KeyHash and S.KeyHash is not null;

		-- Mark the existing artifacts as being updated.
		update	T
		set		T.UpdatedBy = @UpdatedBy,
				T.UpdatedOn = @UpdatedOn
		from	Artifact T
				inner join LoadItem S on S.LoadID = @id and S.Object = 'Artifact' and S.ObjectID = T.ID and T.ArtifactTypeID = @ObjectID;

		-- Insert the updated records into temp table for logging.
		insert into @tbl 
			select	ObjectID,
					RowIndex,
					'U', null, null
			from	LoadItem
			where	LoadID = @id 
					and ObjectID is not null;

		-- Insert new items into the Artifact table.
		insert into @insertToPerform
			select	distinct
					KeyHash
			from	LoadItem
			where	LoadID = @id
					and ObjectID is null
					and KeyHash is not null;

		--declare @insertOutputID table (RowID int identity, ObjectID int);
		insert Artifact (ArtifactTypeID, UpdatedOn, UpdatedBy, CreatedOn, CreatedBy)
		output inserted.ID into @insertOutputID
			select	@ObjectID, 
					@UpdatedOn, 
					@UpdatedBy, 
					@UpdatedOn, 
					@UpdatedBy
			from	@insertToPerform;

		-- Insert the added records into temp table for logging.
		insert into @tbl 
			select	N.ObjectID,
					I.RowIndex,
					'A', null, null
			from	LoadItem I
					inner join @insertToPerform P on P.KeyHash = I.KeyHash and I.LoadID = @id 
					inner join @insertOutputID N on N.RowID = P.RowID;

		-- Update the LoadItem table with the Object and ObjectID generated from the insert above.
		update	T
		set		T.Object = 'Artifact',
				T.ObjectID = S.ObjectID
		from	LoadItem T
				inner join @tbl S on T.LoadID = @id and S.RowIndex = T.RowIndex and S.[Action] = 'A';
	end
	-------------------------

	-- REFERENCE ------------
	if @Object = 'ReferenceItemType'
	begin
		declare @ri_insertToPerform table (RowID int identity, Code nvarchar(250), KeyHash varchar(250));
		declare @ri_insertOutputID table (RowID int identity, ObjectID int);

		-- Identify which load items already exist based on key hash.
		update	T
		set		T.Object = 'ReferenceItem',
				T.ObjectID = S.ID
		from	LoadItem T
				inner join ReferenceItem S on S.ReferenceItemTypeID = @ObjectID and S.KeyHash = T.KeyHash and S.KeyHash is not null;

		-- Mark the existing items as being updated.
		update	T
		set		T.UpdatedBy = @UpdatedBy,
				T.UpdatedOn = @UpdatedOn
		from	ReferenceItem T
				inner join LoadItem S on S.LoadID = @id and S.Object = 'ReferenceItem' and S.ObjectID = T.ID and T.ReferenceItemTypeID = @ObjectID;

		-- Insert the updated records into temp table for logging.
		insert into @tbl 
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
					inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex 
					inner join LoadColumn C on C.LoadID = I.LoadID and C.Name = 'Code'
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
		insert into @tbl 
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
				inner join @tbl S on T.LoadID = @id and S.RowIndex = T.RowIndex and S.[Action] = 'A';
	end
	-------------------------
	

	-- Capture field logs
	declare @fields table (RowIndex int, ColumnIndex int, [Action] varchar(25))

	-- Non-relationship fields
	merge	Field as T
	using	(
			select	I.FieldTypeID,
					I.Type,
					I.Object,
					I.ObjectID,
					C.*
			from	(
					select		I.LoadID,
								FT.ID as FieldTypeID,
								FT.Type,
								I.Object,
								I.ObjectID,
								min(I.RowIndex) as RowIndex,
								C.ColumnIndex
					from		LoadItem I
								inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id
								inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex
								inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
														and FT.Name = LC.Name and FT.Type <> 'Relationship' 
														and ( (FT.Type <> 'Lookup' and C.Value is not null) OR (FT.Type = 'Lookup' and C.LookupObjectID is not null) )			
					where		I.ObjectID is not null
					group by	I.LoadID,
								FT.ID,
								FT.Type,
								I.Object,
								I.ObjectID,
								C.ColumnIndex
					) I
					inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and C.ColumnIndex = I.ColumnIndex
			) S on (T.FieldTypeID = S.FieldTypeID and S.Object = T.ObjectType and S.ObjectID = T.ObjectID)
	when matched then
		update	set
				Value = case S.Type
							when 'Lookup' then cast(S.LookupObjectID as nvarchar)
							else S.Value
						end
	when not matched then
		insert (FieldTypeID, ObjectType, ObjectID, Value)
		values (
				S.FieldTypeID,
				S.Object, 
				S.ObjectID, 
				IIF(S.Type = 'Lookup', cast(S.LookupObjectID as nvarchar), S.Value)
				)
	output S.RowIndex, S.ColumnIndex, $action into @fields;

	update	T
	set		T.FieldsLoaded = 1
	from	@tbl T
			inner join	(
						select		RowIndex,
									[Action]
						from		@fields
						group by	RowIndex, 
									[Action]
						) S on S.RowIndex = T.RowIndex

	delete @fields

	-- Relationship fields
	merge	[Intersect] as T
	using	(
			select	distinct
					FT.LookupObjectID as IntersectTypeID,
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
					end as ObjectID--,
					--C.*
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
					and S.Subject = T.Subject and S.SubjectID = T.SubjectID
					and S.Object = T.Object and S.ObjectID = T.ObjectID
					)
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
--	output S.RowIndex, S.ColumnIndex, $action into @fields;

	--update	T
	--set		T.RelationshipsLoaded = 1
	--from	@tbl T
	--		inner join	(
	--					select		RowIndex,
	--								[Action]
	--					from		@fields
	--					group by	RowIndex, 
	--								[Action]
	--					) S on S.RowIndex = T.RowIndex
	
/*	UPDATE	T
	SET		T.FormattedValue = utility.GetFormattedFieldLookupValue(FT.Type, FT.LookupDisplayFormat, FT.LookupObjectType, FT.LookupObjectID, T.Value)
	FROM	Field T 
			INNER JOIN FieldType FT ON FT.ID = T.FieldTypeID and T.FormattedValue is null or T.FormattedValue = '' and FT.Object = @Object and FT.ObjectID = @ObjectID*/

	--if @Object = 'ArtifactType'
	--begin
	--	update	T
	--	set		--T.KeyHash = K.KeyHash,
	--			T.FieldHash = V.FieldHash--,
	--			--T.DisplayValue = [utility].GetObjectDisplayValue('Artifact', T.ID, T.ArtifactTypeID)
	--	from	Artifact T
	--			--inner join	(
	--			--			select		ID,
	--			--						CONVERT(
	--			--							varchar(32), 
	--			--							SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
	--			--							2) as KeyHash
	--			--			from		(
	--			--						select		top 100 percent
	--			--									A.ID,
	--			--									F.FieldTypeID,
	--			--									coalesce(F.Value, '') as Value
	--			--						from		Artifact A
	--			--									inner join FieldType FT on FT.Object = 'ArtifactType' and FT.ObjectID = A.ArtifactTypeID and FT.IsPartOfKey = 1 and A.ArtifactTypeID = @ObjectID
	--			--									left join Field F on FT.ID = F.FieldTypeID and F.ObjectType = 'Artifact' and F.ObjectID = A.ID
	--			--						order by	A.ID,
	--			--									F.FieldTypeID
	--			--						) A
	--			--			group by	A.ID		
	--			--			) K on K.ID = T.ID
	--			inner join	(
	--						select		ID,
	--									CONVERT(
	--										varchar(32), 
	--										SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
	--										2) as FieldHash
	--						from		(
	--									select		top 100 percent
	--												A.ID,
	--												F.FieldTypeID,
	--												coalesce(F.Value, '') as Value
	--									from		Artifact A
	--												inner join FieldType FT on FT.Object = 'ArtifactType' and FT.ObjectID = A.ArtifactTypeID and A.ArtifactTypeID = @ObjectID
	--												left join Field F on FT.ID = F.FieldTypeID and F.ObjectType = 'Artifact' and F.ObjectID = A.ID
	--									order by	A.ID,
	--												F.FieldTypeID
	--									) A
	--						group by	A.ID
	--						) V on V.ID = T.ID;
	--end

	--if @Object = 'ReferenceItemType'
	--begin
	--	update	T
	--	set		T.FieldHash = V.FieldHash--,
	--			--T.DisplayValue = [utility].GetObjectDisplayValue('ReferenceItem', T.ID, T.ReferenceItemTypeID)
	--	from	ReferenceItem T
	--			inner join	(
	--						select		ID,
	--									CONVERT(
	--										varchar(32), 
	--										SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
	--										2) as FieldHash
	--						from		(
	--									select		top 100 percent
	--												A.ID,
	--												F.FieldTypeID,
	--												coalesce(F.Value, '') as Value
	--									from		ReferenceItem A
	--												inner join FieldType FT on FT.Object = 'ReferenceItemType' and FT.ObjectID = A.ReferenceItemTypeID and A.ReferenceItemTypeID = @ObjectID
	--												left join Field F on FT.ID = F.FieldTypeID and F.ObjectType = 'ReferenceItem' and F.ObjectID = A.ID
	--									order by	A.ID,
	--												F.FieldTypeID
	--									) A
	--						group by	A.ID
	--						) V on V.ID = T.ID;
	--end
	
	-- Capture logs and update load status. -----
	update	T
	set		T.Status = 1,
			T.StatusMessage = 'Item successfully ' + case S.[Action] when 'A' then 'added' else 'updated' end + '.'
	from	LoadItem T
			inner join @tbl S on T.LoadID = @id and S.RowIndex = T.RowIndex and T.[Object] is not null and T.ObjectID is not null;

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
