CREATE procedure [bulkload].[Promotions]
--declare
	@id int
--set @id = 170
as
begin
	set nocount on;

	declare @Object varchar(50),
			@ObjectID int,
			@Action varchar(1),
			@UpdatedOn datetime = getutcdate(),
			@UpdatedBy int = 0,
			@startDynamicFieldColumnIndex int,
			@columnCount int

	declare @ResolvedObjects table ([Object] varchar(50), ObjectID int, [Action] varchar(25), LoadID int, RowIndex int)	--This captures the INSERTED/UPDATED objects from the merge statements below.

	drop table if exists #FieldValidationRows

	create table #FieldValidationRows (
		RowIndex int,
		Valid bit
	)

	select	@Object = [Object],
			@ObjectID = ObjectID,
			@Action = [Action],
			@UpdatedBy = UpdatedBy
	from	[Load]
	where	ID = @id;

	update	LoadItem
	set		Status = null,
			StatusMessage = null
	where	LoadID = @id;

	select	@columnCount = count(1) from LoadColumn where LoadID = @id;

	declare @ParentID int = null,	--Artifact
			@currentLevel int,		--Taxonomy
			@maxLevel int			--Taxonomy

	if @Object = 'ArtifactType'
	begin
		select	@ParentID = ParentID 
		from	ArtifactType 
		where	ID = @ObjectID

		if @ParentID is not null
			begin
				set @startDynamicFieldColumnIndex = 5
			end
		else
			begin
				set @startDynamicFieldColumnIndex = 4
			end
	end
	else if @Object = 'AttributeType'
	begin
		set @startDynamicFieldColumnIndex = 4
	end
	else if @Object = 'Domain'
	begin
		set @startDynamicFieldColumnIndex = 4
	end
	else if @Object = 'DomainType'
	begin
		set @startDynamicFieldColumnIndex = 4
	end
	else if @Object = 'TaxonomyType'
	begin
		select	@currentLevel = 0,
				@maxLevel = max(
								case when isnumeric(replace(Name,'Level','')) = 1 then
									replace(Name,'Level','') 
								else 
									0 
								end) 
		from	LoadColumn 
		where	LoadID = @id 
				and Name like 'Level%';

		set @startDynamicFieldColumnIndex = @maxLevel + 1 + 1 -- the first 1 is for description.  the second 1 is to move to the start column of the dynamic fields, if any.
	end

	-- PARSE any dynamic fields that are specifically lookups.
	exec [bulkload].[UpdateDynamicLookupFieldColumns] @id, @startDynamicFieldColumnIndex, @columnCount

	--Note the dynamic field status for all load items.
	insert into #FieldValidationRows
		select	I.RowIndex,
				case
					when S.InvalidCount = 0 then cast(1 as bit)
					else cast(0 as bit)
				end
		from	LoadItem I
				inner join	(
							select	I.LoadID,
									I.RowIndex,
									C.InvalidCount
							from	[Load] L
									inner join [LoadItem] I on I.LoadID = L.ID
									cross apply (
												select	count(1) as InvalidCount
												from	[LoadItemColumn] IC
														inner join FieldType F on L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
														inner join [LoadColumn] C on C.LoadID = IC.LoadID and F.Name = C.Name and C.ColumnIndex = IC.ColumnIndex and C.ColumnIndex between @startDynamicFieldColumnIndex and @columnCount
												where	IC.LoadID = @id 
														and IC.RowIndex = I.RowIndex
														and IC.LookupObject is null and IC.LookupObjectID is null
														and IC.Value is not null and IC.Value <> ''
												) C
							where	L.ID = @id
							) S on I.LoadID = S.LoadID and S.RowIndex = I.RowIndex

--select * from #FieldValidationRows

	if @Object = 'ArtifactType'
	begin
		exec bulkload.UpdateSubjectAreaColumn @id, 3
		-- Mark the rows with invalid subject areas.
		update	I
		set		I.StatusMessage = I.StatusMessage + ' Subject area could not be found.'
		from	LoadItem I
				inner join LoadItemColumn S on I.LoadID = @id and S.LoadID = I.LoadID and S.RowIndex = I.RowIndex and S.ColumnIndex = 3 and S.LookupObjectID is null

		if @ParentID is not null
			begin
				-- Parse the parents, if any.
				exec bulkload.UpdateItemColumnByType @id, 'ArtifactType', @ParentID, 3, 4

				drop table if exists #ParentArtifacts

				select	I.LoadID,
						I.RowIndex,
						I.LookupObjectID as ID,
						@ObjectID as ArtifactTypeID,
						I.Value as Name,
						D.Value as Description,
						S.LookupObjectID as TaxonomyTypeID,
						P.LookupObjectID as ParentID,
						T.ID as ExistingArtifactID
				into	#ParentArtifacts
				from	LoadItemColumn I
						inner join LoadItemColumn S on S.LoadID = I.LoadID and S.RowIndex = I.RowIndex and S.ColumnIndex = 3 and S.LookupObjectID is not null
						inner join LoadItemColumn P on P.LoadID = I.LoadID and P.RowIndex = I.RowIndex and P.ColumnIndex = 4 and P.LookupObjectID is not null
						inner join LoadItemColumn D on D.LoadID = I.LoadID and D.RowIndex = I.RowIndex and D.ColumnIndex = 2
						inner join #FieldValidationRows V on V.RowIndex = I.RowIndex and V.Valid = 1
						left join Artifact T on T.ArtifactTypeID = @ObjectID and T.TaxonomyTypeID = S.LookupObjectID and T.ParentID = P.LookupObjectID and T.Name = I.Value
				where	I.LoadID = @id and I.ColumnIndex = 1;

				update	T
				set		T.ParentID = null,
						T.[Description] = S.[Description],
						T.[Status] = 'Draft',
						T.UpdatedBy = @UpdatedBy,
						T.UpdatedOn = @UpdatedOn
				from	Artifact T
						inner join #ParentArtifacts S on S.ExistingArtifactID = T.ID;

				insert into @ResolvedObjects
					select 'Artifact', ExistingArtifactID, 'UPDATE', LoadID, RowIndex from #ParentArtifacts where ExistingArtifactID is not null

				insert into Artifact (ArtifactTypeID, TaxonomyTypeID, ParentID, Name, [Description], [Status], CreatedOn, UpdatedOn, UpdatedBy)
					select	ArtifactTypeID, TaxonomyTypeID, ParentID, Name, [Description], 'Draft', @UpdatedOn, @UpdatedOn, @UpdatedBy
					from	#ParentArtifacts 
					where	ExistingArtifactID is null

				insert into @ResolvedObjects
					select	'Artifact', A.ID, 'INSERT', I.LoadID, I.RowIndex 
					from	#ParentArtifacts I
							inner join Artifact A on A.ArtifactTypeID = I.ArtifactTypeID and A.TaxonomyTypeID = I.TaxonomyTypeID and A.ParentID = I.ParentID and A.Name = I.Name and I.ExistingArtifactID is null

				-- Mark the rows with invalid parents.
				update	I
				set		I.StatusMessage = 'Parent could not be found.'
				from	LoadItem I
						inner join LoadItemColumn P on I.LoadID = @id and P.LoadID = I.LoadID and P.RowIndex = I.RowIndex and P.ColumnIndex = 4 and P.LookupObjectID is null
			end
		else
			begin
				drop table if exists #NoParentArtifacts

				select	I.LoadID,
						I.RowIndex,
						I.LookupObjectID as ID,
						I.Value as Name,
						D.Value as Description,
						@ObjectID as ArtifactTypeID,
						S.LookupObjectID as TaxonomyTypeID,
						T.ID as ExistingArtifactID
				into	#NoParentArtifacts
				from	LoadItemColumn I
						inner join LoadItemColumn S on S.LoadID = I.LoadID and S.RowIndex = I.RowIndex and S.ColumnIndex = 3 and S.LookupObjectID is not null
						inner join LoadItemColumn D on D.LoadID = I.LoadID and D.RowIndex = I.RowIndex and D.ColumnIndex = 2
						inner join #FieldValidationRows V on V.RowIndex = I.RowIndex and V.Valid = 1
						left join Artifact T on T.ArtifactTypeID = @ObjectID and T.TaxonomyTypeID = S.LookupObjectID and T.Name = I.Value
				where	I.LoadID = @id and I.ColumnIndex = 1
				
				update	T
				set		T.ParentID = null,
						T.[Description] = S.[Description],
						T.[Status] = 'Draft',
						T.UpdatedBy = @UpdatedBy,
						T.UpdatedOn = @UpdatedOn
				from	Artifact T
						inner join #NoParentArtifacts S on S.ExistingArtifactID = T.ID;

				insert into @ResolvedObjects
					select 'Artifact', ExistingArtifactID, 'UPDATE', LoadID, RowIndex from #NoParentArtifacts where ExistingArtifactID is not null

				insert into Artifact (ArtifactTypeID, TaxonomyTypeID, Name, [Description], [Status], CreatedOn, UpdatedOn, UpdatedBy)
					select	ArtifactTypeID, TaxonomyTypeID, Name, [Description], 'Draft', @UpdatedOn, @UpdatedOn, @UpdatedBy
					from	#NoParentArtifacts 
					where	ExistingArtifactID is null

				insert into @ResolvedObjects
					select	'Artifact', A.ID, 'INSERT', I.LoadID, I.RowIndex 
					from	#NoParentArtifacts I
							inner join Artifact A on A.ArtifactTypeID = I.ArtifactTypeID and A.TaxonomyTypeID = I.TaxonomyTypeID and A.Name = I.Name and I.ExistingArtifactID is null
			end
	end
	else if @Object = 'AttributeType'
	begin
		-- Clean Owner Type field.
		update	LoadItemColumn
		set		Value = case when charindex('Type', Value) > 0 then Value else Value + 'Type' end
		where	LoadID = @id and ColumnIndex = 1;

		-- PARSE Owner Type fields.
		update	T
		set		T.LookupObject = S.LookupObject,
				T.LookupObjectID = S.LookupObjectID
		from	LoadItemColumn T
				inner join	(
							select	LI.LoadID,
									LI.RowIndex,
									C2.ColumnIndex,
									D.[Object] as LookupObject,
									D.ObjectID as LookupObjectID
							from	[Load] L
									inner join LoadItem LI on LI.LoadID = L.ID and L.ID = @id
									inner join [LoadItemColumn] C1 on C1.LoadID = LI.LoadID and C1.RowIndex = LI.RowIndex and C1.ColumnIndex = 1 --'Owner Type' 
									inner join [LoadItemColumn] C2 on C2.LoadID = LI.LoadID and C2.RowIndex = LI.RowIndex and C2.ColumnIndex = 2 --'Owner Type Name'
									inner join cache.ObjectDetails D on D.[Object] = C1.Value and D.[Name] = C2.Value
							) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex;

		-- PARSE Owner fields.
		update	T
		set		T.LookupObject = S.LookupObject,
				T.LookupObjectID = S.LookupObjectID
		from	LoadItemColumn T
				inner join	(
							select	LI.LoadID,
									LI.RowIndex,
									C3.ColumnIndex,
									D.[Object] as LookupObject,
									D.ObjectID as LookupObjectID
							from	[Load] L
									inner join LoadItem LI on LI.LoadID = L.ID and L.ID = @id
									--inner join [LoadItemColumn] C1 on	C1.LoadID = LI.LoadID	and C1.RowIndex = LI.RowIndex	and C1.ColumnIndex = 1 --'Owner Type' 
									inner join [LoadItemColumn] C2 on C2.LoadID = LI.LoadID and C2.RowIndex = LI.RowIndex and C2.ColumnIndex = 2 --'Owner Type Name'
									inner join [LoadItemColumn] C3 on C3.LoadID = LI.LoadID	and C3.RowIndex = LI.RowIndex and C3.ColumnIndex = 3 --'Owner Name'
									inner join cache.ObjectDetails D on D.[ObjectType] = C2.[LookupObject] and D.ObjectTypeID = C2.LookupObjectID and D.[Name] = C3.Value
							) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex;

		merge	[Attribute] T
		using	(
				select	I.LoadID,
						I.RowIndex,
						@ObjectID as AttributeTypeID,
						C.LookupObject as [Object],
						C.LookupObjectID as ObjectID
				from	[LoadItem] I
						inner join [LoadItemColumn] C on I.LoadID = @id and C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and C.ColumnIndex = 3
						and C.LookupObject is not null
						and C.LookupObjectID is not null
						inner join #FieldValidationRows V on V.RowIndex = I.RowIndex and V.Valid = 1
				) S
		on		(T.AttributeTypeID = S.AttributeTypeID and T.[ObjectType] = S.[Object] and T.[ObjectID] = S.[ObjectID] and T.ParentID = NULL)-- and T.Name = S.Name)
		when	matched then
				update	set T.[UpdatedOn] = getutcdate(),
							T.UpdatedBy = @UpdatedBy
		when	not matched then
				insert (AttributeTypeID, ObjectType, ObjectID, UpdatedOn, UpdatedBy)
				values (S.AttributeTypeID, S.[Object], S.ObjectID, getutcdate(), @UpdatedBy)
		output	'Attribute', inserted.ID, $action, S.LoadID, S.RowIndex into @ResolvedObjects;		
	end
	else if @Object = 'Domain'
	begin
		merge	DomainItem T
		using	(
				select	I.LoadID,
						I.RowIndex,
						@ObjectID as DomainID,
						C.Value as Code,
						N.Value as Name,
						D.Value as Description
				from	[LoadItem] I
						inner join [LoadItemColumn] C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and C.ColumnIndex = 1
						inner join [LoadItemColumn] N on N.LoadID = I.LoadID and N.RowIndex = I.RowIndex and N.ColumnIndex = 2
						left join [LoadItemColumn] D on D.LoadID = I.LoadID and D.RowIndex = I.RowIndex and D.ColumnIndex = 3
						inner join #FieldValidationRows V on V.RowIndex = I.RowIndex and V.Valid = 1
				where	I.LoadID = @id
				) S
		on		(T.DomainID = S.DomainID and T.Code = S.Code)
		when	matched then
				update	set T.[Name] = S.[Name],
							T.[Description] = S.[Description],
							T.[DomainID] = S.[DomainID],
							T.UpdatedBy = @UpdatedBy,
							T.UpdatedOn = @UpdatedOn
		when	not matched then
				insert (DomainID, Code, Name, [Description], UpdatedOn, UpdatedBy)
				values (S.DomainID, S.Code, S.Name, S.[Description], @UpdatedOn, @UpdatedBy)
		output	'DomainItem', inserted.ID, $action, S.LoadID, S.RowIndex into @ResolvedObjects;
	end
	else if @Object = 'DomainType'
	begin	
		-- PARSE any Domain Group fields.
		update	T
		set		T.LookupObject = 'DomainGroup',
				T.LookupObjectID = S.ID
		from	LoadItemColumn T
				inner join DomainGroup S on T.LoadID = @id and T.ColumnIndex = 3 and S.Name = T.Value and S.DomainTypeID = @ObjectID;

		-- Mark the rows with invalid domain groups.
		update	I
		set		I.StatusMessage = 'Domain group could not be found.'
		from	LoadItem I
				inner join LoadItemColumn S on I.LoadID = @id and S.LoadID = I.LoadID and S.RowIndex = I.RowIndex and S.ColumnIndex = 3 and S.LookupObjectID is null

		-- Merge domains that are valid.
		merge	Domain T
		using	(
				select	I.LoadID,
						I.RowIndex,
						@ObjectID as DomainTypeID,
						N.Value as Name,
						D.Value as Description,
						G.LookupObjectID as DomainGroupID
				from	[LoadItem] I
						inner join [LoadItemColumn] N on N.LoadID = I.LoadID and N.RowIndex = I.RowIndex and N.ColumnIndex = 1
						left join [LoadItemColumn] D on D.LoadID = I.LoadID and D.RowIndex = I.RowIndex and D.ColumnIndex = 2
						inner join [LoadItemColumn] G on G.LoadID = I.LoadID and G.RowIndex = I.RowIndex and G.ColumnIndex = 3 and G.LookupObjectID is not null
						inner join #FieldValidationRows V on V.RowIndex = I.RowIndex and V.Valid = 1
				where	I.LoadID = @id
				) S
		on		(T.DomainTypeID = S.DomainTypeID and T.Name = S.Name)
		when	matched then
				update	set T.[Description] = S.Description,
							T.DomainGroupID = S.DomainGroupID,
							T.UpdatedOn = @UpdatedOn,
							T.UpdatedBy = @UpdatedBy
		when	not matched then
				insert (DomainTypeID, DomainGroupID, Name, Description, UpdatedOn, UpdatedBy)
				values (S.DomainTypeID, S.DomainGroupID, S.Name, S.Description, @UpdatedOn, @UpdatedBy)
		output	'Domain', inserted.ID, $action, S.LoadID, S.RowIndex into @ResolvedObjects;
	end
	else if @Object = 'TaxonomyType'
	begin
		declare @rowCount int,
				@rowCurr int;

		declare @levels table (id int, ColumnIndex int, RowIndex int, [Level] varchar(50), Value varchar(250),MaxLevel int, TaxonomyID int, ParentID int, [Status] varchar(50));

		with v as
		(
			select	L.ID, 
					L.Object, 
					L.ObjectID, 
					LC.Name, 
					LC.ColumnIndex, 
					IC.RowIndex, 
					IC.Value, 
					replace(LC.Name,'Level','') as [Level], 
					T.ID as TaxonomyID 
			from	[Load] L
					join LoadColumn LC on LC.LoadID = L.ID
					join LoadItemColumn IC on IC.LoadID = LC.LoadID AND IC.ColumnIndex = LC.ColumnIndex
					left join Taxonomy T on T.TaxonomyTypeID = L.ObjectID and T.[Level] = replace(LC.Name,'Level','') and T.Name = IC.Value
			where	L.ID = @id 
					AND ltrim(rtrim(IC.Value)) != '' 
					and LC.Name like 'Level%'  
		)

		insert into @levels
			select		distinct
						row_number() over (partition by 1 order by v.[Level]) as ID,
						v.ColumnIndex,
						v.RowIndex,
						v.[Level],
						v.Value,
						m.[Level] as MaxLevel,
						v.TaxonomyID,
						p.TaxonomyID as ParentID,
						'UPDATE' as [Status]
			from		v	
						left join v p on p.RowIndex = v.RowIndex and v.TaxonomyID is null and p.ColumnIndex = (v.ColumnIndex - 1)
						inner join v m on m.RowIndex = v.RowIndex and m.[Level] = (select max([Level]) from v where RowIndex = m.RowIndex)
			order by	v.[Level] asc;

		-- calculate hierarchy
		while @currentLevel <= @maxLevel
		begin
			set @currentLevel = @currentLevel + 1;
				
			update	LV
			set		LV.ParentID = P.ID
			from	@levels LV
					left join @levels P on P.[Level] = (LV.[Level] - 1) AND LV.RowIndex = P.RowIndex
			where	LV.[Level] = @currentLevel;
		end

		select @rowCurr = 0, @rowCount = count(*) from @levels;

		while @rowCurr <= @rowCount
		begin
			set @rowCurr = @rowCurr + 1;

			--parent does not exist or leading columns were not filled
			if (select ParentID from @levels where id = @rowCurr) IS NULL AND (select Level from @levels where id = @rowCurr) > 1
			begin
				update	@levels 
				set		[Status] = 'ERROR' 
				where	rowIndex = (select rowindex from @levels where id = @rowCurr);
				continue;
			end

			--update the TaxonomyID for records that do not yet have it
			if (select level from @levels where id = @rowCurr) = 1
			begin
				update	LV
				set		TaxonomyID = T.ID
				from	@levels LV
						join Load L on L.ID = @id
						join Taxonomy T on T.Name = LV.Value and T.ParentID is NULL and T.Level = LV.Level and T.TaxonomyTypeID = L.ObjectID
				where	LV.ID = @rowCurr;
			end
			else
			begin
				update	LV
				set		TaxonomyID = T.ID
				from	@levels LV
						left join @levels P on P.ID = LV.ParentID
						join Taxonomy T on T.Name = LV.Value and T.ParentID = P.TaxonomyID and T.Level = LV.Level
				where	LV.ID = @rowCurr;
			end

			if (select TaxonomyID from @levels where id = @rowCurr) IS NULL
			begin
				--insert the new taxonomy
				insert into Taxonomy (TaxonomyTypeID, ParentID, Name, [Description], UpdatedOn, UpdatedBy)
					select	distinct
							L.ObjectID as TaxonomyTypeID,
							LVP.TaxonomyID as ParentID,
							LV.Value as Name,
							case 
								when LV.Level = LV.MaxLevel then LI.Value
								else ''
							end as Description,
							@UpdatedOn as UpdatedOn,
							@UpdatedBy as UpdatedBy
					from	@levels LV
							left join @levels LVP on LVP.ID = LV.ParentID
							join [Load] L on L.ID = @id
							inner join LoadColumn LC on LC.Name = 'Description' and LC.LoadID = @id
							inner join LoadItemColumn LI on LI.RowIndex = LV.RowIndex AND LI.ColumnIndex = LC.ColumnIndex AND LI.LoadID = @id
							inner join #FieldValidationRows V on V.RowIndex = LI.RowIndex and V.Valid = 1
					where	LV.ID = @rowCurr

				update	@levels 
				set		[Status] = 'INSERT' 
				where	id = @rowCurr;

				--set the levels taxonomy id after insert
				update	LV
				set		TaxonomyID = T.ID
				from	@levels LV
						left join @levels P on P.ID = LV.ParentID
						join Taxonomy T on T.Name = LV.Value and coalesce(T.ParentID,-1) = coalesce(P.TaxonomyID,-1) and T.Level = LV.Level
				where	LV.ID = @rowCurr;
			end
				
			--if level = max, update the description
			if (select level from @levels where id = @rowCurr) = (select maxlevel from @levels where id = @rowCurr)
			begin
				update	T
				set		T.Description = case when LI.Value = '' then T.Description else LI.Value end,
						T.UpdatedOn = getutcdate(),
						T.UpdatedBy = @UpdatedBy
				from	Taxonomy T
						join @levels LV on LV.ID = @rowCurr and T.ID = LV.TaxonomyID
						inner join LoadColumn LC on LC.Name = 'Description' and LC.LoadID = @id
						inner join LoadItemColumn LI on LI.RowIndex = LV.RowIndex AND LI.ColumnIndex = LC.ColumnIndex AND LI.LoadID = @id
						inner join #FieldValidationRows V on V.RowIndex = LI.RowIndex and V.Valid = 1;
			end
		end --end while
			

		--remove error rows
		delete from @levels
		where rowindex in (select rowindex from @levels where status is null or status = 'ERROR');

					--insert object statuses
		insert into @ResolvedObjects ([Object], ObjectID, [Action], LoadID, RowIndex)
			select	'Taxonomy',
					TaxonomyID,
					[Status],
					@id,
					RowIndex
			from	@levels;
	end

	-- Update the LoadItem table with the IDs we recieved in the merge statements above.
	update	T
	set		T.[Object] = S.[Object],
			T.ObjectID = S.ObjectID,
			T.[Status] = 1,
			T.StatusMessage = case S.[Action]
								when 'INSERT' then 'Added item'
								when 'UPDATE' then 'Updated item'
								else NULL
								end
	from	LoadItem T
			inner join	@ResolvedObjects S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex;

	-- Update the LoadItems that were not successfully added or updated.
	update	LoadItem
	set		[Status] = 0,
			[StatusMessage] = coalesce([StatusMessage], 'Item could not be added nor updated.')
	where	LoadID = @id
			and [ObjectID] is null

	update	LoadItem
	set		[Status] = 0,
			[StatusMessage] = coalesce([StatusMessage], 'Item could not be added nor updated.')
	where	LoadID = @id
			and RowIndex in (select RowIndex from #FieldValidationRows where Valid = 0)
			and [ObjectID] is null

	-- merge the dynamic fields involved with this load into the Fields table.  Needs to be here as this proc looks at the LaodItem table for the Object and ObjectID.
	exec [bulkload].MergeDynamicLookupFields @id, @startDynamicFieldColumnIndex, @columnCount

	--Finally, close out the Load.
	update	[Load] 
	set		DateCompleted = getutcdate()
	where	ID = @id
end