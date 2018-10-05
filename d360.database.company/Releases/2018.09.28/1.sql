--alter table [integration].[Setting] add DeleteExecutionTimeoutHours int constraint DF_IntegrationSetting_DeleteExecutionTimeoutHours default(192) not null
--alter table [integration].[SynchedAssetType] add DeleteExecutionTimeoutHours int null
--GO;

--update	T
--set		T.DeleteExecutionTimeoutHours = S.Val + 2
--from	[integration].[SynchedAssetType] T
--		inner join (
--			select	A.ID,
--					A.SourceAssetTypeName,
--					max(DATEDIFF(hh, E.StartedOn, E.CompletedOn)) as Val
--			from	[integration].[SynchedAssetType] A
--					inner join integration.ExecutionAssetType E on E.SynchedAssetTypeID = A.ID and E.CompletedOn is not null and E.StartedOn > '9/1/2018'
--			group by A.ID,
--					A.SourceAssetTypeName		
--		) S on S.ID = T.ID and S.ID <> 20
--GO;

alter table ResponsibilityTypeRelationRule add UpdatedOn Datetime constraint DF_ResponsibilityTypeRelationRule_UpdatedOn default('01/01/2018') not null
GO;

alter table AssetCrossReference add FieldHash varchar(50) null
GO;

ALTER TABLE [dbo].[AssetTypeExportTemplate] DROP CONSTRAINT [FK_AssetTypeExportTemplate_AssetType]
ALTER TABLE [dbo].[AssetTypeExportTemplateStyle] DROP CONSTRAINT [FK_AssetTypeExportTemplateStyle_AssetTypeExportTemplate]
ALTER TABLE [dbo].[AssetTypeStyle] DROP CONSTRAINT [FK_AssetTypeStyle_AssetType]
ALTER TABLE [dbo].[AssetTypeLevel] DROP CONSTRAINT [FK_AssetTypeLevel_AssetType]
ALTER TABLE [dbo].[AssetTypeQuery] DROP CONSTRAINT [FK_AssetTypeQuery_AssetType]
ALTER TABLE [dbo].[Asset] DROP CONSTRAINT [FK_Asset_AssetType]
ALTER TABLE [api].[Entity] DROP CONSTRAINT [FK_Entity_AssetType]
GO;

ALTER TABLE [dbo].[AssetType] DROP CONSTRAINT [PK_AssetType] WITH ( ONLINE = OFF )
GO;

ALTER TABLE [dbo].[AssetType] ADD  CONSTRAINT [PK_AssetType] PRIMARY KEY CLUSTERED ( [ID] ASC )
GO;

ALTER TABLE [dbo].[Asset]  WITH CHECK ADD  CONSTRAINT [FK_Asset_AssetType] FOREIGN KEY([AssetTypeID]) REFERENCES [dbo].[AssetType] ([ID])
ALTER TABLE [dbo].[Asset] CHECK CONSTRAINT [FK_Asset_AssetType]
GO;

ALTER TABLE [api].[Entity]  WITH CHECK ADD  CONSTRAINT [FK_Entity_AssetType] FOREIGN KEY([AssetTypeID]) REFERENCES [dbo].[AssetType] ([ID]) ON DELETE CASCADE
ALTER TABLE [api].[Entity] CHECK CONSTRAINT [FK_Entity_AssetType]
GO;

ALTER TABLE [dbo].[AssetTypeExportTemplate]  WITH CHECK ADD  CONSTRAINT [FK_AssetTypeExportTemplate_AssetType] FOREIGN KEY([AssetTypeID]) REFERENCES [dbo].[AssetType] ([ID])
ALTER TABLE [dbo].[AssetTypeExportTemplate] CHECK CONSTRAINT [FK_AssetTypeExportTemplate_AssetType]
GO;

ALTER TABLE [dbo].[AssetTypeExportTemplateStyle]  WITH CHECK ADD  CONSTRAINT [FK_AssetTypeExportTemplateStyle_AssetTypeExportTemplate] FOREIGN KEY([AssetTypeExportTemplateID]) REFERENCES [dbo].[AssetTypeExportTemplate] ([ID])
ALTER TABLE [dbo].[AssetTypeExportTemplateStyle] CHECK CONSTRAINT [FK_AssetTypeExportTemplateStyle_AssetTypeExportTemplate]
GO;

ALTER TABLE [dbo].[AssetTypeLevel]  WITH CHECK ADD  CONSTRAINT [FK_AssetTypeLevel_AssetType] FOREIGN KEY([AssetTypeID]) REFERENCES [dbo].[AssetType] ([ID])
ALTER TABLE [dbo].[AssetTypeLevel] CHECK CONSTRAINT [FK_AssetTypeLevel_AssetType]
GO;

ALTER TABLE [dbo].[AssetTypeQuery]  WITH CHECK ADD  CONSTRAINT [FK_AssetTypeQuery_AssetType] FOREIGN KEY([ID]) REFERENCES [dbo].[AssetType] ([ID])
ALTER TABLE [dbo].[AssetTypeQuery] CHECK CONSTRAINT [FK_AssetTypeQuery_AssetType]
GO;

ALTER TABLE [dbo].[AssetTypeStyle]  WITH CHECK ADD  CONSTRAINT [FK_AssetTypeStyle_AssetType] FOREIGN KEY([ID]) REFERENCES [dbo].[AssetType] ([ID])
ALTER TABLE [dbo].[AssetTypeStyle] CHECK CONSTRAINT [FK_AssetTypeStyle_AssetType]
GO;



DROP INDEX IX_Field_AssetID ON [dbo].[Field]
GO;

DROP INDEX IX_Field_AssetID_Include_FormatedValue_FieldTypeID ON [dbo].[Field]
GO;



ALTER TRIGGER [dbo].[Field_AfterUpsert]
	ON [dbo].[Field]
	FOR INSERT, UPDATE
AS
	SET NOCOUNT ON;

	UPDATE	T
	SET		T.UpdatedOn = getutcdate()
	FROM	Field T 
			inner join inserted F on F.ID = T.ID;

	UPDATE	T
	SET		T.FormattedValue = utility.GetFormattedFieldLookupValueWithMultiple(FT.Type, FT.LookupDisplayFormat, FT.LookupObjectType, FT.LookupObjectID, F.Value, FT.AllowMultipleValues)
	FROM	Field T 
			inner join inserted F on F.FieldTypeID = T.FieldTypeID and F.ObjectType = T.ObjectType and F.ObjectID = T.ObjectID and F.ObjectType <> 'FusionAttribute' and F.ObjectType <> 'FusionQueryAttribute'
			INNER JOIN FieldType FT ON FT.ID = T.FieldTypeID;

	-- the below section can be slow
	if exists(select 1 from Field TF inner join FieldType FT on FT.ID = TF.FieldTypeID inner join inserted SF on FT.LookupObjectType = SF.ObjectType and FT.LookupObjectID = SF.ObjectID and SF.ObjectType <> 'FusionAttribute' and SF.ObjectType <> 'FusionQueryAttribute')
	begin
		
		UPDATE	TF
		SET		TF.FormattedValue = utility.GetFormattedFieldLookupValueWithMultiple(FT.Type, FT.LookupDisplayFormat, FT.LookupObjectType, FT.LookupObjectID, TF.Value, FT.AllowMultipleValues)
		from	Field TF
				inner join FieldType FT on FT.ID = TF.FieldTypeID
				inner join	inserted SF on FT.LookupObjectType = SF.ObjectType and FT.LookupObjectID = SF.ObjectID and SF.ObjectType <> 'FusionAttribute' and SF.ObjectType <> 'FusionQueryAttribute';
	end

	UPDATE	T
	SET		T.AssetID = A.ID
	FROM	Field T 
			inner join inserted F on F.FieldTypeID = T.FieldTypeID and F.ObjectType = T.ObjectType and F.ObjectID = T.ObjectID and T.AssetID is null
			inner join Asset A on A.Object = F.ObjectType and A.ObjectID = F.ObjectID;
GO;


alter view [dbo].[AssetDetail]
as
	select	A.ID,
			A.AssetTypeID,
			A.State,
			A.Object,
			A.ObjectID,
			A.SourceID,
			D.DisplayValue,
			K.KeyHash,
			F.FieldHash,
			A.CreatedOn,
			A.CreatedBy,
			A.UpdatedOn,
			A.UpdatedBy,
			A.AssetTypeClass,
			A.AssetTypeDescription,
			A.TypeName,
			A.Type,
			A.TypeID,
			A.BackColor,
			A.ForeColor,
			A.Icon,
			A.UID
	from	AssetWithType A
			cross apply dbo.GetAssetDisplayValueById(A.ID) D	--left join GetAssetDisplayValue() D on D.ID = A.ID
			--left join GetAssetKeyHash() K on K.ID = A.ID
			cross apply [dbo].[GetAssetKeyHashById](A.ID) K
			left join GetAssetFieldHash() F on F.ID = A.ID
GO;

alter procedure [bulkload].[Promotions]
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

	
	print '(starting resolve lookup) ' 
	print getdate() 
	-- resolve lookups first as we need the id to generate the hash correctly
	
	-- Resolve Single-value LOOKUP fields
	exec [bulkload].[UpdateDynamicLookupFieldColumns] @id

	print '(completed resolve lookup) ' 
	print getdate() 
	
	
	if exists (select 1 from LoadItem LI
						inner join LoadColumn C on C.LoadID = LI.LoadID
						inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name
				where
					FT.AllowMultipleValues = 1 and LI.LoadID = @id )
	begin
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
	end

	

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

	-- Process hashes for Load Items needs to be after lookup, lookup
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
													coalesce(cast(IC.LookupObjectID as varchar(100)), IC.Value, '') as Value
										from		LoadItem I
													inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id
													inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex = IC.ColumnIndex													
													left join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and C.Name !='Code'
													left join dbo.ReferenceItem RI on C.Name = 'Code' and RI.ID = @ObjectID
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
													coalesce(cast(IC.LookupObjectID as varchar(100)), IC.[Value],'') as [Value] 
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
											SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' +Value, char(59))), 3, 32), 
											2) as KeyHash
							from		(
										select		top 1000000000 --percent
													I.RowIndex,
													FT.ID as FieldTypeID,
													coalesce(cast(IC.LookupObjectID as varchar(100)), IC.Value, '') as Value
										from		LoadItem I
													inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id and IC.Value is not null
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
	

	-- Resolve RELATIONSHIP fields
	if exists (select 1 from LoadColumn C
					inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and FT.Type = 'Relationship' where C.LoadID = @id )
	begin
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
	end


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
	-- oddly wonky
	/*update	T
	set		T.Object = A.Object,
			T.ObjectID = A.ObjectID
	from	LoadItem T
			inner join AssetType ST on ST.Object = @Object and ST.ObjectID = @ObjectID
			inner join GetAssetKeyHash() S on S.AssetTypeID = ST.ID and S.KeyHash = T.KeyHash and T.LoadID = @id
			inner join Asset A on A.ID = S.ID;*/

	/*update	T
	set		T.Object = A.Object,
			T.ObjectID = A.ObjectID
	from	LoadItem T
			inner join AssetType ST on ST.Object = @Object and ST.ObjectID = @ObjectID
			cross apply GetAssetKeyHashById(ST.ID) S 
			inner join Asset A on A.ID = S.ID
	where S.KeyHash = T.KeyHash and T.LoadID = @id*/
	
	update	T
	set		T.Object = A.Object,
			T.ObjectID = A.ObjectID
	from	LoadItem T
			inner join AssetType ST on ST.Object = @Object and ST.ObjectID = @ObjectID			
			inner join Asset A on A.AssetTypeID = ST.ID
			cross apply GetAssetKeyHashById(A.ID) S 
	where S.KeyHash = T.KeyHash and T.LoadID = @id
	
	
	--BEGIN TRANSACTION;
    --SAVE TRANSACTION PromotionCreationTrans;
	
	--BEGIN Try 
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
			print '(starting merge fields) ' 
			print getdate() 

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
				
	print '(end merge fields) ' 
	print getdate() 

	--END TRY
    /*BEGIN CATCH
        IF @@TRANCOUNT > 0
        BEGIN
            ROLLBACK TRANSACTION PromotionCreationTrans; -- rollback to PromotionCreationTrans
        END
    END CATCH
    COMMIT TRANSACTION */
	
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

	print '(starting merge relationship fields) ' 
	print getdate() 
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
	
	print '(done merge relationship fields) ' 
	print getdate()

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
GO;

alter procedure [bulkload].[UpdateDynamicLookupFieldColumns]
	@id int	
as
begin
	set nocount on;
	declare @startColumnIndex int = 0;
	declare @endColumnIndex int = 0;

	print '(starting resolve artifact lookup) ' 
	print getdate() 
	-- Artifact lookups
	update	T
	set		T.LookupObject = S.LookupObject,
			T.LookupObjectID = S.LookupObjectID
	from	LoadItemColumn T
			inner join	(
						select	IC.LoadID,
								IC.RowIndex,
								IC.ColumnIndex,
								case 
									when ( (L_A.ID is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel AND F.LookupObjectType in ('Artifact', 'ArtifactType')) ) then 'Artifact'									
									else NULL
								end as LookupObject,
								case 
									when L_A.ObjectID is not null then L_A.ObjectID
									when F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel then 0
									else NULL
								end as LookupObjectID --coalesce(L_A.ID, L_D.ID, L_DI.ID, L_F.ID, L_L.Value, L_T.ID) as LookupObjectID -- L_I.ID,
						from	FieldType F
								inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
								inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
								inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex								
								inner join AssetDetail L_A on L_A.[Object] = 'Artifact' and L_A.TypeID = F.LookupObjectID and (L_A.DisplayValue = IC.Value)								
						where	F.AllowMultipleValues = 0 and F.LookupObjectType in ('Artifact', 'ArtifactType')
						) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex;

	print '(completed resolve artifact lookup) ' 
	print getdate() 

	print '(started resolve reference item type lookup) ' 
	print getdate() 

	-- Reference Item Type lookups
	update	T
	set		T.LookupObject = S.LookupObject,
			T.LookupObjectID = S.LookupObjectID
	from	LoadItemColumn T
			inner join	(
						select	IC.LoadID,
								IC.RowIndex,
								IC.ColumnIndex,
								case 									
									when ( (L_D.ID is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel AND F.LookupObjectType = 'ReferenceItemType') ) then 'ReferenceItemType'									
									else NULL
								end as LookupObject,
								case 									
									when L_D.ID is not null then L_D.ID
									when F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel then 0																		
									else NULL
								end as LookupObjectID --coalesce(L_A.ID, L_D.ID, L_DI.ID, L_F.ID, L_L.Value, L_T.ID) as LookupObjectID -- L_I.ID,
						from	FieldType F
								inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
								inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
								inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex

								inner join ReferenceItemType L_D on L_D.[Name] = IC.Value								
						where	F.AllowMultipleValues = 0 and F.LookupObjectType = 'ReferenceItemType'
						) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

	print '(completed resolve reference item type lookup) ' 
	print getdate() 

	print '(started resolve reference item lookup) ' 
	print getdate() 
	-- Reference item
	if exists (select 1 from	FieldType F
								inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
								inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name								
				where F.AllowMultipleValues = 0 and F.LookupObjectType = 'ReferenceItem')
	begin
		
		update	T
			set		T.LookupObject = S.LookupObject,
					T.LookupObjectID = S.LookupObjectID
			from	LoadItemColumn T
					inner join	(
								select	IC.LoadID,
										IC.RowIndex,
										IC.ColumnIndex,								
										case
											--when ( (L_DI.ID is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel AND F.LookupObjectType = 'ReferenceItem') ) then 'ReferenceItem'									
											when ( (FLV.Value is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel AND F.LookupObjectType = 'ReferenceItem') ) then 'ReferenceItem'									
											else NULL
										end as LookupObject,
										case 									
											--when L_DI.ID is not null then L_DI.ID
											when FLV.Value is not null then FLV.Value
											when F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel then 0
											else NULL
										end as LookupObjectID --coalesce(L_A.ID, L_D.ID, L_DI.ID, L_F.ID, L_L.Value, L_T.ID) as LookupObjectID -- L_I.ID,
								from	FieldType F
										inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
										inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
										inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
										--inner join ReferenceItem L_DI on L_DI.ReferenceItemTypeID = F.LookupObjectID and L_DI.[DisplayValue] = IC.Value															
										cross apply [dbo].[FieldLookupValueByFieldTypeID](F.ID) FLV
										--inner join FieldLookupValue L_DI on (L_DI.FieldTypeID = F.ID and L_DI.Text = IC.Value)
								where	F.AllowMultipleValues = 0 and F.LookupObjectType = 'ReferenceItem' and  FLV.Text = IC.Value
								) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex
	end

	print '(completed resolve reference item lookup) ' 
	print getdate() 

	print '(started resolve fusion attribute type lookup) ' 
	print getdate() 
-- fusion attribute type
update	T
	set		T.LookupObject = S.LookupObject,
			T.LookupObjectID = S.LookupObjectID
	from	LoadItemColumn T
			inner join	(
						select	IC.LoadID,
								IC.RowIndex,
								IC.ColumnIndex,
								case
									when ( (L_F.ID is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel AND F.LookupObjectType = 'FusionAttributeType') ) then 'FusionAttribute'									
									else NULL
								end as LookupObject,
								case 									
									when L_F.ID is not null then L_F.ID
									when F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel then 0

									else NULL
								end as LookupObjectID --coalesce(L_A.ID, L_D.ID, L_DI.ID, L_F.ID, L_L.Value, L_T.ID) as LookupObjectID -- L_I.ID,
						from	FieldType F
								inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
								inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
								inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex

								inner join FusionAttribute L_F on L_F.FusionAttributeTypeID = F.LookupObjectID and (L_F.[Name] = IC.Value OR L_F.TextPath = IC.Value)								
						where	F.AllowMultipleValues = 0 and F.LookupObjectType = 'FusionAttributeType'
						) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex
-- Lookup 
print '(completed resolve fusion attribute type lookup) ' 
	print getdate() 


	print '(started resolve lookups) ' 
	print getdate() 

update	T
	set		T.LookupObject = S.LookupObject,
			T.LookupObjectID = S.LookupObjectID
	from	LoadItemColumn T
			inner join	(
						select	IC.LoadID,
								IC.RowIndex,
								IC.ColumnIndex,
								case 									
									when ( (L_L.Value is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel AND F.LookupObjectType = 'Lookup') ) then 'Lookup'									
									else NULL
								end as LookupObject,
								case 									
									when L_L.Value is not null then L_L.Value
									when F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel then 0

									else NULL
								end as LookupObjectID 
						from	FieldType F
								inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
								inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
								inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex

								left join [FieldLookupValue] L_L on F.ID = L_L.FieldTypeID and L_L.LookupObjectType = 'Lookup' and L_L.LookupObjectID = F.LookupObjectID and L_L.[Text] = IC.Value								
						where	F.AllowMultipleValues = 0 and F.LookupObjectType = 'Lookup'
						) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

	print '(completed resolve lookup) ' 
	print getdate() 

-- Resource 
print '(started resolve resources) ' 
	print getdate() 
update	T
	set		T.LookupObject = S.LookupObject,
			T.LookupObjectID = S.LookupObjectID
	from	LoadItemColumn T
			inner join	(
						select	IC.LoadID,
								IC.RowIndex,
								IC.ColumnIndex,
								case 									
									when ( (L_L.Value is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel AND F.LookupObjectType = 'Resource') ) then 'Resource'									
									else NULL
								end as LookupObject,
								case 									
									when L_L.Value is not null then L_L.Value
									when F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel then 0

									else NULL
								end as LookupObjectID 
						from	FieldType F
								inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
								inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
								inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex

								left join [FieldLookupValue] L_L on F.ID = L_L.FieldTypeID and L_L.LookupObjectType = 'Resource' and L_L.LookupObjectID = F.LookupObjectID and L_L.[Text] = IC.Value								
						where	F.AllowMultipleValues = 0 and F.LookupObjectType = 'Resource'
						) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex
print '(completed resolve resources) ' 
	print getdate() 


-- taxonomy
update	T
	set		T.LookupObject = S.LookupObject,
			T.LookupObjectID = S.LookupObjectID
	from	LoadItemColumn T
			inner join	(
						select	IC.LoadID,
								IC.RowIndex,
								IC.ColumnIndex,
								case
									when ( (L_T.ID is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel ) ) then 'Taxonomy'
									else NULL
								end as LookupObject,
								case 									
									when L_T.ObjectID is not null then L_T.ObjectID
									when F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel then 0
									else NULL
								end as LookupObjectID --coalesce(L_A.ID, L_D.ID, L_DI.ID, L_F.ID, L_L.Value, L_T.ID) as LookupObjectID -- L_I.ID,
						from	FieldType F
								inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
								inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
								inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex

								inner join AssetDetail L_T on L_T.[Object] = 'Taxonomy' and L_T.TypeID = F.LookupObjectID and (L_T.[DisplayValue] = IC.Value /*OR L_T.TextPath = IC.Value*/)
						where	F.AllowMultipleValues = 0 and F.LookupObjectType in ('Taxonomy', 'TaxonomyType')
						) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

-- taxonomy type
	update	T
	set		T.LookupObject = S.LookupObject,
			T.LookupObjectID = S.LookupObjectID
	from	LoadItemColumn T
			inner join	(
						select	IC.LoadID,
								IC.RowIndex,
								IC.ColumnIndex,
								case
									when ( (L_T.ID is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel ) ) then 'Taxonomy'
									else NULL
								end as LookupObject,
								case 									
									when L_T.ObjectID is not null then L_T.ObjectID
									when F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel then 0
									else NULL
								end as LookupObjectID --coalesce(L_A.ID, L_D.ID, L_DI.ID, L_F.ID, L_L.Value, L_T.ID) as LookupObjectID -- L_I.ID,
						from	FieldType F
								inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
								inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
								inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex

								inner join AssetType L_T on L_T.[Object] = 'TaxonomyType'  and (L_T.Name = IC.Value )
						where	F.AllowMultipleValues = 0 and F.LookupObjectType = 'TaxonomyType' and F.LookupObjectID = 0
						) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

	select @endColumnIndex = max(ColumnIndex) from LoadItemColumn where loadid = @id;

	while @startColumnIndex <= @endColumnIndex
	begin
		update	T
		set		T.StatusMessage = coalesce(T.StatusMessage, '') + S.StatusMessage
		from	LoadItem T
				inner join	(
							select	IC.LoadID,
									IC.RowIndex,
									case 
										when IC.LookupObjectID is null and IC.Value is not null and IC.Value <> '' then ' ' + F.Name + ' does not contain a valid value.'
										else ''
									end StatusMessage
							from	FieldType F
									inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
									inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
									inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex and IC.columnIndex = @startColumnIndex and IC.LookupObjectID is null
							) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex
		set @startColumnIndex = @startColumnIndex + 1
	end
end
GO;

alter procedure [dbo].[DeleteObject]
 @ObjTemp varchar(50),
 @ObjectIDTemp int,
 @ResourceIDTemp int
as 
begin
	set nocount on
	declare    @Obj varchar(50) = @ObjTemp,
		@ObjectID int = @ObjectIDTemp,
		@ResourceID int = @ResourceIDTemp
	
	declare    @Object varchar(50) = @Obj,
		@CurrentDate datetime = getutcdate(),
		@predicateType int = 0,
		@trans varchar(25) = 'Trans',
		@current int = 1,
		@max int,
		@IsType bit = 0

	declare @h table (IntersectID int, ID bigint, ObjectID int, Processed bit null)
	declare @ht table (IntersectTypeID int, ID int, ObjectID int, Processed bit null)

	declare @ClearAttributes bit = 0,
			@ClearComments bit = 0,
			@ClearIntersects bit = 0,
			@ClearFavorites bit = 0,
			@ClearFields bit = 0,
			@ClearFollows bit = 0,
			@ClearIssues bit = 0,
			@ClearNyms bit = 0,
			@ClearResponsibilities bit = 0,
			@ClearSiteNav bit = 0,
			@ClearPromotion bit = 0
			

	if charindex('Type', @Object) > 0
	begin
		set @IsType = 1
	end

	begin try
		begin transaction @trans

		if @Obj = 'Artifact' or @Obj = 'ArtifactType' or @Obj = 'FusionAttribute' or @Obj = 'FusionAttributeType' or @Obj = 'ReferenceItem' or @Obj = 'ReferenceItemType' or @Obj = 'Rule'
		begin
			set @predicateType = 3
		end
		if @Obj = 'Policy' or @Obj = 'PolicyType' or @Obj = 'Taxonomy' or @Obj = 'TaxonomyType'
		begin
			set @predicateType = 4
		end

		if @predicateType > 0
		begin
			if @IsType = 1
				begin
					insert into @ht
						select	null,
								ID,
								ObjectID,
								0
						from	AssetType
						where	[Object] = @Obj and ObjectID = @ObjectID

					while exists(select 1 from @ht where Processed = 0)
					begin
						insert into @ht
							select	I.ID,
									C.ID,
									C.ObjectID,
									null
							from	AssetType C
									inner join IntersectType I on I.Subject = @Obj and I.Object = @Obj and I.ObjectID = C.ObjectID and C.[Object] = @Obj
									inner join [Predicate] PR on PR.ID = I.PredicateID and PR.[Type] = @predicateType
									inner join AssetType P on P.Object = I.Subject and P.ObjectID = I.SubjectID
									inner join @ht T on T.ID = P.ID and T.Processed = 0

						update	@ht set Processed = 1 where Processed = 0
						update	@ht set Processed = 0 where Processed is null
					end

					-- Get all assets based on the types found above.
					insert into @h 
						select null, ID, ObjectID, 1 from Asset where AssetTypeID in (select ID from @ht)
				end
			else
				begin
					insert into @h
						select	null,
								ID,
								ObjectID,
								0
						from	Asset
						where	[Object] = @Obj and ObjectID = @ObjectID

					while exists(select 1 from @h where Processed = 0)
					begin
						insert into @h
							select	I.IntersectID,
									C.ID,
									C.ObjectID,
									null
							from	Asset C
									inner join PredicateIntersect I on I.PredicateType = @predicateType and I.Subject = @Obj and I.Object = @Obj and I.ObjectID = C.ObjectID
									inner join Asset P on P.Object = I.Subject and P.ObjectID = I.SubjectID
									inner join @h T on T.ID = P.ID and T.Processed = 0

						update	@h set Processed = 1 where Processed = 0
						update	@h set Processed = 0 where Processed is null
					end
				end
		end
		
		-- INDEX
		INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID],[AssetID])
			select	'ObjectIndex', 
					'D',
					O.Object, 
					O.ObjectID,
					O.ID
			from	Asset O
					inner join @h I on O.ID = I.ID

		-- AUDIT
		insert into reporting.Global_Audit (Object, ObjectID, ObjectName, ResourceID, Date, Action, ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, ActionDescription)
			select	O.Object, 
					O.ObjectID, 
					O.DisplayValue, 
					@ResourceID, 
					@CurrentDate, 
					'Deleted', 
					O.Object, 
					O.ObjectID, 
					O.TypeName, 
					O.DisplayValue, 
					'This asset has been removed.' 
			from	AssetDetail O
					inner join @h I on O.ID = I.ID
			union
			select	O.Object, 
					O.ObjectID, 
					O.Name, 
					@ResourceID, 
					@CurrentDate, 
					'Deleted', 
					O.Object, 
					O.ObjectID, 
					O.Name, 
					O.Name, 
					'This asset type has been removed.' 
			from	AssetType O
					inner join @ht I on O.ID = I.ID

		-- WORKFLOW

		if @Object = 'Artifact'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1
			set @ClearPromotion = 1

			delete Artifact where ID in (select ObjectID from @h)
		end

		if @Object = 'ArtifactType'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1
			set @ClearSiteNav = 1			
			
			delete	ArtifactTypeExportTemplate
			where	ArtifactTypeID in (select ObjectID from @ht)

			delete	Artifact
			where	ID in (select ObjectID from @h)

			delete	ArtifactType			
			where	ID in (select ObjectID from @ht)
		end

		if @Object = 'AttributeType'
		begin
			declare @at table (ID int)
			declare @a table (ID int);

			with ht as	(
						select	ID, 
								ParentID
						from	AttributeType
						where	ID = @ObjectID
						union all
						select	C.ID,
								C.ParentID
						from	AttributeType C
								inner join ht P on P.ID = C.ParentID
						)

			insert into @at 
				select ID from ht

			insert into @a
				select ID from Attribute where AttributeTypeID in (select ID from @at)

			-- AUDIT
			insert into reporting.Global_Audit (Object, ObjectID, ObjectName, ResourceID, Date, Action, ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, ActionDescription)
				select	A.Object, 
						A.ObjectID, 
						A.DisplayValue, 
						@ResourceID, 
						@CurrentDate, 
						'Deleted', 
						'Attribute', 
						O.ID, 
						O.Name, 
						O.FormattedValue, 
						'This attribute has been removed.' 
				from	AttributeDetail O
						inner join AssetDetail A on A.Object = O.ObjectType and A.ObjectID = O.ObjectID
						inner join @a I on O.ID = I.ID
				union
				select	A.Object, 
						A.ObjectID, 
						A.Name, 
						@ResourceID, 
						@CurrentDate, 
						'Deleted', 
						'AttributeType', 
						O.ID, 
						'Attribute Type', 
						O.Name, 
						'This attribute type has been removed.' 
				from	AttributeType O
						inner join @at I on O.ID = I.ID
						inner join AttributeTypeRelation R on R.AttributeTypeID = O.ID
						inner join AssetType A on A.Object = R.ObjectType and A.ObjectID = R.ObjectID

			update	T
			set		T.UpdatedBy = @ResourceID,
					T.UpdatedOn = @CurrentDate
			from	Asset T
					inner join [Attribute] S on S.ObjectType = T.Object and S.ObjectID = T.ObjectID and S.ID in (select ID from @a)

			update	T
			set		T.UpdatedBy = @ResourceID,
					T.UpdatedOn = @CurrentDate
			from	AssetType T
					inner join AttributeTypeRelation S on S.ObjectType = T.Object and S.ObjectID = T.ObjectID 
					inner join AttributeType A on A.ID = S.AttributeTypeID and A.ID in (select ID from @at)

			delete Field					where ObjectType = 'Attribute' and ObjectID in (select ID from @a)
			delete Attribute				where ID in (select ID from @a)
			delete FieldType				where Object = 'AttributeType' and ObjectID in (select ID from @at)
			delete AttributeTypeRelation	where AttributeTypeID in (select ID from @at)
			delete AttributeType			where ID in (select ID from @at)
		end

		if @Object = 'FieldType'
		begin
			-- AUDIT
			insert into reporting.Global_Audit (Object, ObjectID, ObjectName, ResourceID, Date, Action, ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, ActionDescription)
				select	A.Object, 
						A.ObjectID, 
						A.DisplayValue, 
						@ResourceID, 
						@CurrentDate, 
						'Deleted', 
						A.Object, 
						A.ObjectID, 
						T.Name, 
						O.FormattedValue, 
						'This field has been removed.' 
				from	Field O
						inner join FieldType T on T.ID = O.FieldTypeID and T.ID = @ObjectID
						inner join AssetDetail A on A.Object = O.ObjectType and A.ObjectID = O.ObjectID
				union
				select	A.Object, 
						A.ObjectID, 
						A.Name, 
						@ResourceID, 
						@CurrentDate, 
						'Deleted', 
						'FieldType', 
						O.ID, 
						'Field Type', 
						O.Name, 
						'This field type has been removed.' 
				from	FieldType O
						inner join AssetType A on A.Object = O.Object and A.ObjectID = O.ObjectID and O.ID = @ObjectID

			update	T
			set		T.UpdatedBy = @ResourceID,
					T.UpdatedOn = @CurrentDate
			from	Asset T
					inner join Field S on S.ObjectType = T.Object and S.ObjectID = T.ObjectID and S.FieldTypeID = @ObjectID

			update	T
			set		T.UpdatedBy = @ResourceID,
					T.UpdatedOn = @CurrentDate
			from	AssetType T
					inner join FieldType S on S.Object = T.Object and S.ObjectID = T.ObjectID and S.ID = @ObjectID

			delete	Field 
			where	FieldTypeID = @ObjectID
			
			update	FieldType 
			set		ParentFieldTypeID = null 
			where	ParentFieldTypeID = @ObjectID

			delete	FieldType 
			where	ID = @ObjectID
		end

		if @Object = 'FusionAttribute'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1
			set @ClearPromotion = 1

			delete FusionAttribute where ID in (select ObjectID from @h)
		end

		if @Object = 'FusionAttributeType'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1

			delete FusionAttribute		where ID in (select ObjectID from @h)
			delete FusionAttributeType	where ID in (select ObjectID from @ht)
		end

		if @Object = 'Fusion'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1

			--insert into @h
			--	select	I.ID, null, F.ID, null 
			--	from	[IntersectDetail] I
			--			inner join FusionAttribute F on I.Subject = 'FusionAttribute' 
			--											and I.Object = 'FusionAttribute' 
			--											and (F.ID = I.SubjectID OR F.ID = I.ObjectID) 
			--											and F.FusionID = @ObjectID
			--											and I.PredicateType = 3

			insert into @h								
				select I.ID, null, F.ID, null 
				from [Intersect] I
				inner join IntersectType T on T.ID = I.IntersectTypeID
				inner join [Predicate] P on P.ID = T.PredicateID and P.[Type] = 3
				inner join FusionAttribute F on I.[Subject] = 'FusionAttribute' and I.[Object] = 'FusionAttribute'
					and (I.SubjectID = F.ID or I.ObjectID = F.ID) and F.FusionID = @ObjectID;

			delete FusionAttribute where FusionID = @ObjectID
			delete Fusion where ID = @ObjectID
		end

		if @Object = 'FusionType'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1

			insert into @ht
				select	ID, null, null, null
				from	IntersectType
				where	Subject = 'FusionAttributeType' 
						and Object = 'FusionAttributeType' 
						and (
							SubjectID in (select ID from FusionAttributeType where FusionTypeID = @ObjectID)
							or ObjectID in (select ID from FusionAttributeType where FusionTypeID = @ObjectID)
							)

			insert into @h
				select ID, null, null, null from [Intersect] where IntersectTypeID in (select IntersectTypeID from @ht)

			delete FusionAttribute where FusionAttributeTypeID in (select ID from FusionAttributeType where FusionTypeID = @ObjectID)
			delete Fusion where FusionTypeID = @ObjectID
			delete FusionAttributeType where FusionTypeID = @ObjectID
			delete FusionType where ID = @ObjectID
		end

		if @Object = 'Intersect'
		begin
			update [Intersect] set Deleted = 1 where ID = @ObjectID
		end

		if @Object = 'IntersectType'
		begin
			set @ClearAttributes = 1
			set @ClearFields = 1

			delete [Intersect] where IntersectTypeID = @ObjectID
			delete IntersectType where ID = @ObjectID
		end

		if @Object = 'LookupType'
		begin
			set @ClearFields = 1

			delete [Lookup] where LookupTypeID = @ObjectID
			delete  LookupType where ID=@ObjectID
		end

		if @Object = 'Policy'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1
			set @ClearPromotion = 1

			delete [Policy] where ID in (select ObjectID from @h)
		end

		if @Object = 'PolicyType'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1
			set @ClearSiteNav = 1

			delete [Policy] where PolicyTypeID in (select ObjectID from @ht)
			delete PolicyTypeLevel where PolicyTypeID in (select ObjectID from @ht)
			delete PolicyType where ID in (select ObjectID from @ht)
		end

		if @Object = 'ReferenceItem'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1

			delete ReferenceItem where ID = @ObjectID			
		end

		if @Object = 'ReferenceItemType'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1

			delete ReferenceItem where ReferenceItemTypeID = @ObjectID
			delete ReferenceItemType where ID = @ObjectID
		end

		if @Object = 'ResponsibilityType'
		begin
			delete ResponsibilityTypeRelationOverrideItem where ResponsibilityTypeID = @ObjectID
			delete ResponsibilityTypeRelationRuleResult where ResponsibilityTypeID = @ObjectID
			delete ResponsibilityTypeRelationRule where ResponsibilityTypeID = @ObjectID
			delete ResponsibilityType where ID = @ObjectID
		end

		if @Object = 'Rule'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1

			-- DELETE
			delete T
			from   RuleResultQualifierType T
				   inner join RuleImplementation I on I.ID = T.RuleImplementationID and I.RuleID = @ObjectID

			delete	Q
			from	RuleResultQualifier Q 
					inner join RuleResult S on S.ID = Q.RuleResultID
					inner join RuleImplementation I on I.ID = S.RuleImplementationID and I.RuleID = @ObjectID

			delete	F
			from	RuleResultFusionAttribute F
					inner join RuleResult S on S.ID = F.RuleResultID
					inner join RuleImplementation I on I.ID = S.RuleImplementationID and I.RuleID = @ObjectID

			delete	RuleImplementation where RuleID = @ObjectID

			delete	[Rule] where ID = @ObjectID
		end

		if @Object = 'RuleType'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1

			delete	Q
			from	RuleResultQualifier Q 
					inner join RuleResult S on S.ID = Q.RuleResultID
					inner join RuleImplementation I on I.ID = S.RuleImplementationID
					inner join [Rule] R on R.ID = I.RuleID and R.RuleTypeID = @ObjectID

			delete	F
			from	RuleResultQualifierType F
					inner join RuleImplementation I on I.ID = F.RuleImplementationID
					inner join [Rule] R on R.ID = I.RuleID and R.RuleTypeID = @ObjectID

			delete	F
			from	RuleResultFusionAttribute F
					inner join RuleResult S on S.ID = F.RuleResultID
					inner join RuleImplementation I on I.ID = S.RuleImplementationID
					inner join [Rule] R on R.ID = I.RuleID and R.RuleTypeID = @ObjectID

			delete	S
			from	RuleResult S
					inner join RuleImplementation I on I.ID = S.RuleImplementationID
					inner join [Rule] R on R.ID = I.RuleID and R.RuleTypeID = @ObjectID

			delete [RuleImplementation] where RuleID in (select ID from [Rule] where RuleTypeID = @ObjectID)

			delete [Rule] where RuleTypeID = @ObjectID

			delete RuleType where ID = @ObjectID
		end

		if @Object = 'SurveyType'
		begin
			delete Survey where SurveyTypeID = @ObjectID
			delete SurveyType where ID = @ObjectID
		end

		if @Object = 'Taxonomy'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1
			set @ClearPromotion = 1

			delete Taxonomy where ID in (select ObjectID from @h)
		end

		if @Object = 'TaxonomyType'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1
			set @ClearSiteNav = 1

			delete Taxonomy where TaxonomyTypeID = @ObjectID
			delete TaxonomyTypeLevel where TaxonomyTypeID = @ObjectID
			delete TaxonomyType where ID = @ObjectID
		end

		-- DELETE (Supporting Tables) ---------------------------------------------------------

		-- Attribute deletion
		IF @ClearAttributes = 1 AND @IsType = 0
		BEGIN
			delete Field where ObjectType = 'Attribute' and ObjectID in (select ID from Attribute where ObjectType = @Object and ObjectID in (select ObjectID from @h))
			delete Attribute where ObjectType = @Object and ObjectID in (select ObjectID from @h)
		END

		-- Intersect deletion
		IF @ClearIntersects = 1
		BEGIN
			DECLARE @tblIntersectIDs table (ID int)

			INSERT INTO @tblIntersectIDs
				SELECT	ID
				FROM	[Intersect]
				WHERE	(Subject = @Object and SubjectID in (select ObjectID from @h)) OR (Object = @Object and ObjectID in (select ObjectID from @h))

			--delete	MapItem 
			--where	SourceIntersectID in (select ID from @tblIntersectIDs) OR
			--		TargetIntersectID in (select ID from @tblIntersectIDs)

			delete [Intersect] where ID in (select ID from @tblIntersectIDs)
		END

		-- Comment deletion
		IF @ClearComments = 1 AND @IsType = 0
		BEGIN
			delete	CommentRelation
			where	ObjectType = @Object 
					and ObjectID in (select ObjectID from @h)

			delete	CommentVote
			where	CommentID in (
								select	ID
								from	Comment
								where	OwnerObjectType = @Object 
										and OwnerObjectID in (select ObjectID from @h)			
								)

			delete	Comment
			where	OwnerObjectType = @Object 
					and OwnerObjectID in (select ObjectID from @h)
		END

		-- Site menu deletion
		IF @ClearSiteNav = 1
		BEGIN
			delete sitenav where objectid = @ObjectID and [object] = @Object
			
			--remove child entries
			delete nav
			from sitenav nav
			inner join @ht t on t.ObjectID = nav.ObjectID and nav.Object = @Object;
		END

		IF @ClearPromotion = 1
		BEGIN
			delete from fusion.rulepromotion where objecttype = @Object and objectid = @ObjectID
		END 

		-- Favorite deletion
		IF @ClearFavorites = 1
		BEGIN
			IF @IsType = 1
				BEGIN
					delete	Favorite
					where	Object = @Object 
							and ObjectID in (select ObjectID from @ht)
				END
			ELSE
				BEGIN 
					delete	Favorite
					where	Object = @Object
							and ObjectID in (select ObjectID from @h)
				END
		END

		-- Field deletion
		IF @ClearFields = 1
		BEGIN
			IF @IsType = 1
				BEGIN
					delete	FieldType
					where	[Object] = @Object 
							and ObjectID in (select ObjectID from @ht)
				END
			ELSE
				BEGIN
					delete	Field
					where	ObjectType = @Object 
							and ObjectID in (select ObjectID from @h)
				END
		END

		-- Follow deletion
		IF @ClearFollows = 1
		BEGIN
			IF @IsType = 1
				BEGIN
					delete	Follow
					where	ObjectType = @Object 
							and ObjectID in (select ObjectID from @ht)
				END
			ELSE
				BEGIN 
					delete	Follow
					where	ObjectType = @Object
							and ObjectID in (select ObjectID from @h)
				END
		END

		-- Issue deletion
		IF @ClearIssues = 1 AND @IsType = 0
		BEGIN
			delete	Issue
			where	Object = @Object 
					and ObjectID in (select ObjectID from @h)
		END

		-- Nym deletion
		IF @ClearNyms = 1 AND @IsType = 0
		BEGIN
			IF @IsType = 1
				BEGIN 
					delete	NymRelation
					where	Object = @Object 
							and ObjectID in (select ObjectID from @ht)			
				END
			ELSE
				BEGIN
					delete	Nym
					where	Object = @Object 
							and ObjectID in (select ObjectID from @h)
				END
		END

		-- Responsibility deletion
		IF @ClearResponsibilities = 1 AND @IsType = 0
		BEGIN
			IF @IsType = 1
				BEGIN
					delete ResponsibilityTypeRelation		where ObjectType = @Obj and ObjectID in (select ObjectID from @ht)
				END
			ELSE
				BEGIN
					delete	T
					from	ResponsibilityTypeRelationOverrideItem T
					where   T.AssetID  in (select ID from @h)

					delete	T
					from	ResponsibilityTypeRelationRuleResult T
					where   T.AssetID in (select ID from @h)
				END
		END
		---------------------------------------------------------------------------------------

		if @IsType = 1
		begin
			delete AttributeTypeRelation			where ObjectType = @Obj AND ObjectID in (select ObjectID from @ht)
			delete IntersectType					where (Object = @Obj and ObjectID in (select ObjectID from @ht)) OR (Subject = @Obj and SubjectID in (select ObjectID from @ht))
			delete ObjectStyle						where ObjectType = @Obj AND ObjectID in (select ObjectID from @ht)
		end
		
		commit transaction @trans
	end try
	begin catch
		DECLARE @ErrorMessage NVARCHAR(4000)
		DECLARE @ErrorSeverity INT
	    DECLARE @ErrorState INT

		SELECT 
			@ErrorMessage = ERROR_MESSAGE(),
			@ErrorSeverity = ERROR_SEVERITY(),
			@ErrorState = ERROR_STATE()

		-- Use RAISERROR inside the CATCH block to return error
		-- information about the original error that caused
		-- execution to jump to the CATCH block.
		RAISERROR (@ErrorMessage, -- Message text.
				   @ErrorSeverity, -- Severity.
				   @ErrorState -- State.
				   )

		rollback transaction @trans
	end catch
end
GO;

alter procedure [dbo].[ResponsibilityRuleShouldRun]
	@id int-- = 70
as
begin
	set nocount on;

	--update ResponsibilityTypeRelationRule set LastRunOn = '7/20/2018 9:00:00 PM' where ID = 70
	declare @shouldRun bit = 0 ,
			@lastRunOn datetime,
			@o varchar(50),
			@oid int,
			@assignedObject varchar(20),
			@assignedObjectID int,
			@groupUpdatedOn datetime,
			@orgUpdatedOn datetime,
			@ruleUpdatedOn datetime

	select	@lastRunOn = coalesce(LastRunOn, '1/1/2000'),
			@o = Object,
			@oid = ObjectID,
			@ruleUpdatedOn = UpdatedOn
	from	ResponsibilityTypeRelationRule
	where	ID = @id

	declare @assetMaxDate datetime,
			@assetFieldMaxDate datetime,
			@newUsers bit = 0,
			@newAssets bit = 0,
			@ruleUpdated bit = 0

	if @ruleUpdatedOn > @lastRunOn
	begin
		set	@ruleUpdated = 1
	end
	select	@newUsers = IIF(count(1) > 0, 1, 0)
	from	reporting.Global_Resource
	where	CreatedOn > @lastRunOn

	select	@assetMaxDate = max(A.CreatedOn)
	from	Asset A
			inner join AssetType T on T.ID = A.AssetTypeID and T.Object = @o and T.ObjectID = @oid 

	select	@assetMaxDate = IIF(max(A.UpdatedOn) > @assetMaxDate, max(A.UpdatedOn), @assetMaxDate)
	from	Asset A
			inner join AssetType T on T.ID = A.AssetTypeID and T.Object = @o and T.ObjectID = @oid 

	if @assetMaxDate > @lastRunOn
	begin
		set @newAssets = 1
	end

	if @newAssets = 0
	begin
		declare @fIDs table (FieldTypeID int)
		insert into @fIDs
			select	WF.FieldTypeID
			from	ResponsibilityTypeRelationRule R
					cross apply OPENJSON(R.Definition, '$.When') D--with ([When] nvarchar(max) '$.When', [Then] nvarchar(max) '$.Then') D
					cross apply OPENJSON(D.value) with (
							CheckType nvarchar(1) '$.CheckType',
							FieldTypeID int '$.FieldTypeID'--,
							--FieldTypeName nvarchar(250) '$.FieldTypeName' 
						) WF
			where	R.ID = @id
					and WF.CheckType = 'F'

		if exists(select 1 from @fIDs)
		begin
			select	@assetFieldMaxDate = max(F.UpdatedOn)
			from	Field F 
					inner join @fIDs FT on FT.FieldTypeID = F.FieldTypeID
					inner join Asset A on A.ID = F.AssetID
					inner join AssetType T on T.ID = A.AssetTypeID and T.Object = @o and T.ObjectID = @oid 
		end
		else
		begin
			-- slow 3 seconds or so for 400k rows
			/*select	@assetFieldMaxDate = max(F.EffectiveStartDate)
			from	Field F 
					inner join Asset A on A.ID = F.AssetID
					inner join AssetType T on T.ID = A.AssetTypeID and T.Object = @o and T.ObjectID = @oid */
			-- less than 1 sec for 400k rows
			select	@assetFieldMaxDate = max(F.UpdatedOn)
			from	Field F 
					inner join FieldType FT on F.FieldTypeID = FT.id and FT.Object = @o and FT.ObjectID = @oid
		end

		if @assetFieldMaxDate > @lastRunOn
		begin
			set @newAssets = 1
		end	
	end

	-- check if the rule is on a group or organization if so we need to see if any of those changed
	select 
		@assignedObject = res.[Object],
		@assignedObjectID = res.[ObjectID]
	from ResponsibilityTypeRelationRule R
		CROSS APPLY OPENJSON(R.Definition, '$.Then')
			WITH ([Object] varchar(20), ObjectID int) as res
	where R.ID = @id

	if @assignedObject = 'Group' and @assignedObjectID > 0
	begin
		-- check if this group has changed
		select @groupUpdatedOn = g.UpdatedOn from [group] g where g.id = @assignedObjectID

		if @groupUpdatedOn > @lastRunOn
		begin
			set @newUsers = 1
		end
	end

	if @assignedObject = 'OrganizationType' and @assignedObjectID > 0
	begin
		print 'Checking organization fields'
		declare @orgfIDs table (FieldTypeID int)
		insert into @orgfIDs
		select	WF.FieldTypeID
			from	ResponsibilityTypeRelationRule R
					cross apply OPENJSON(R.Definition, '$.Then.Conditions') D
					cross apply OPENJSON(D.value) with (
							FieldTypeID int '$.FieldTypeID'							
						) WF
			where	R.ID = @id
		if exists(select 1 from @fIDs)
		begin
			select	@orgUpdatedOn = max(F.UpdatedOn)
			from	Field F 
					inner join @orgfIDs FT on FT.FieldTypeID = F.FieldTypeID
					
		end

		if @orgUpdatedOn > @lastRunOn
		begin
			set @newUsers = 1
		end
	end

	if @newUsers = 1 or @newAssets = 1 or @ruleUpdated = 1
	begin
		set @shouldRun = 1
	end

	select @shouldRun
end
GO;

alter procedure [lineage].[GetByObject]
--declare
	@Object varchar(50),-- = 'Artifact',
	@ObjectID int-- = 19
as
begin
	declare @usedIntersectIDs table(ID int)
	declare @contextAssetID bigint
	declare @currentLevel int = 0
	declare @lineage table (ID uniqueidentifier null, IntersectTypeID int, [Level] int, IntersectID int, Subject varchar(50), SubjectID int, Object varchar(50), ObjectID int, [State] int, PredicateName nvarchar(250))
	declare @levelResults table (IntersectTypeID int, IntersectID int, Subject varchar(50), SubjectID int, Object varchar(50), ObjectID int, [State] int, PredicateName nvarchar(250))

	select @contextAssetID = ID from Asset where Object = @Object and ObjectID = @ObjectID

	-- GET DOWNSTREAM (FORWARD) LINEAGE ----
	set @currentLevel = @currentLevel + 1
	insert into @levelResults
		select	I.IntersectTypeID,
				I.IntersectID,
				I.Subject,
				I.SubjectID,
				I.Object,
				I.ObjectID,
				I.State,
				I.PredicateName
		from	PredicateIntersect I
		where	I.Subject = @Object and I.SubjectID = @ObjectID and I.PredicateType = 1

	insert into @usedIntersectIDs
		select IntersectID from @levelResults

	while exists(select 1 from @levelResults)
	begin
		declare @newLevel int = @currentLevel + 1

		insert into @lineage
			select	newid(),
					IntersectTypeID,
					@currentLevel,
					IntersectID,
					Subject,
					SubjectID,
					Object,
					ObjectID,
					State,
					PredicateName
			from	@levelResults

		delete @levelResults

		insert into @levelResults
			select	O.IntersectTypeID,
					O.IntersectID,
					O.Subject,
					O.SubjectID,
					O.Object,
					O.ObjectID,
					O.State,
					O.PredicateName
			from	PredicateIntersect O
					inner join @lineage S on S.[Level] = @currentLevel and O.Subject = S.Object and O.SubjectID = S.ObjectID and O.PredicateType = 1
			where	O.IntersectID not in (select ID from @usedIntersectIDs)

		insert into @usedIntersectIDs
			select IntersectID from @levelResults

		set @currentLevel = @newLevel
	end

	-- GET UPSTREAM (BACKWARD) LINEAGE -----
	set @currentLevel = -1
	insert into @levelResults
		select	I.IntersectTypeID,
				I.IntersectID,
				I.Subject,
				I.SubjectID,
				I.Object,
				I.ObjectID,
				I.State,
				I.PredicateName
		from	PredicateIntersect I
		where	I.Object = @Object and I.ObjectID = @ObjectID and I.PredicateType = 1

	insert into @usedIntersectIDs
		select IntersectID from @levelResults

	while exists(select 1 from @levelResults)
	begin
		set @newLevel = @currentLevel - 1

		insert into @lineage
			select	newid(),
					IntersectTypeID,
					@currentLevel,
					IntersectID,
					Subject,
					SubjectID,
					Object,
					ObjectID,
					State,
					PredicateName
			from	@levelResults

		delete @levelResults

		insert into @levelResults
			select	S.IntersectTypeID,
					S.IntersectID,
					S.Subject,
					S.SubjectID,
					S.Object,
					S.ObjectID,
					S.State,
					S.PredicateName
			from	PredicateIntersect S
					inner join @lineage O on O.[Level] = @currentLevel and O.Subject = S.Object and O.SubjectID = S.ObjectID and S.PredicateType = 1
			where	S.IntersectID not in (select ID from @usedIntersectIDs)

		insert into @usedIntersectIDs
			select IntersectID from @levelResults

		set @currentLevel = @newLevel
	end

	--select * from @lineage

	----Hold the raw lineage records.
	--declare @tbl table (IntersectID int, IntersectTypeID int, 
	--					Subject varchar(50), SubjectID int, Object varchar(50), ObjectID int, [State] int, 
	--					PredicateID int, PredicateName nvarchar(250), PredicateInverse nvarchar(250), PredicateType int, 
	--					IntersectGroupID int null
	--					)

	---- Get the direct lineage going backward from the provided object.
	--insert into @tbl
	--	select	L.IntersectID,
	--			L.IntersectTypeID,
	--			L.[Subject],
	--			L.SubjectID,
	--			L.[Object],
	--			L.ObjectID,
	--			L.[State],
	--			L.PredicateID,
	--			L.PredicateName,
	--			L.PredicateInverse,
	--			L.PredicateType,
	--			G.IntersectGroupID 
	--	from	lineage.GetTrailForObject(@Object, @ObjectID, 0) L	
	--			left join IntersectGroupItem G on G.IntersectID = L.IntersectID

	---- Get the direct lineage going foreward from the provided object.
	--insert into @tbl
	--	select	L.IntersectID,
	--			L.IntersectTypeID,
	--			L.[Subject],
	--			L.SubjectID,
	--			L.[Object],
	--			L.ObjectID,
	--			L.[State],
	--			L.PredicateID,
	--			L.PredicateName,
	--			L.PredicateInverse,
	--			L.PredicateType,
	--			G.IntersectGroupID 
	--	from	lineage.GetTrailForObject(@Object, @ObjectID, 1) L	
	--			left join IntersectGroupItem G on G.IntersectID = L.IntersectID

	---- Hold the intersect IDs that are part of an IntersectGroup from one of the retrieved intersects above.
	--declare @groupIntersects table (IntersectGroupID int, IntersectID int)

	---- Get the intersects that are part of an IntersectGroup from one of intersects above, but not yet pulled back in the temp table (i.e. does not exist in the lineage)
	--insert into @groupIntersects
	--	select	GI.IntersectGroupID,
	--			GI.IntersectID
	--	from	@tbl O
	--			inner join IntersectGroupItem GI on GI.IntersectGroupID = O.IntersectGroupID and GI.IntersectID not in (select IntersectID from @tbl)

	---- Get the intersect record itself, for each ID pulled back as part of the group query above.
	--insert into @tbl
	--	select	P.IntersectID,
	--			P.IntersectTypeID,
	--			P.[Subject],
	--			P.SubjectID,
	--			P.[Object],
	--			P.ObjectID,
	--			P.[State],
	--			P.PredicateID,
	--			P.PredicateName,
	--			P.PredicateInverse,
	--			P.PredicateType,
	--			G.IntersectGroupID
	--	from	PredicateIntersect P
	--			inner join @groupIntersects G on G.IntersectID = P.IntersectID

	---- Go back for each group intersectID retrieved above and get backward-facing lineage, that is not already present in the lineage @tbl
	--insert into @tbl
	--	select	Src.IntersectID,
	--			Src.IntersectTypeID,
	--			Src.[Subject],
	--			Src.SubjectID,
	--			Src.[Object],
	--			Src.ObjectID,
	--			Src.[State],
	--			Src.PredicateID,
	--			Src.PredicateName,
	--			Src.PredicateInverse,
	--			Src.PredicateType,
	--			null
	--	from	PredicateIntersect P
	--			inner join @groupIntersects G on G.IntersectID = P.IntersectID
	--			cross apply lineage.GetTrailForObject(P.Subject, P.SubjectID, 0) Src
	--	where	Src.IntersectID not in (select IntersectID from @tbl)


	-- Return the full results to the caller.
	select	distinct
			I.IntersectID,
			cast(NULL as int) as IntersectGroupID, --I.IntersectGroupID,
			T.IntersectTypeID,
			SA.ID as SubjectAssetID,
			I.Subject,
			I.SubjectID,
			SA.DisplayValue as SubjectName,
			SA.BackColor as SubjectBackColor,
			SA.ForeColor as SubjectForeColor,
			SA.TypeName as SubjectTypeName,
			SA.Type as SubjectType,
			SA.TypeID as SubjectTypeID,
			SA.AssetTypeID as SubjectAssetTypeID,

			OA.ID as ObjectAssetID,
			I.Object,
			I.ObjectID,
			OA.DisplayValue as ObjectName,
			OA.BackColor as ObjectBackColor,
			OA.ForeColor as ObjectForeColor,
			OA.TypeName as ObjectTypeName,
			OA.Type as ObjectType,
			OA.TypeID as ObjectTypeID,
			OA.AssetTypeID as ObjectAssetTypeID,

			I.[State],

			I.PredicateName as [Predicate]
	from	@lineage I --@tbl I
			inner join [Intersect] T on T.ID = I.IntersectID
			inner join AssetDetail SA on SA.Object = I.Subject and SA.ObjectID = I.SubjectID
			inner join AssetDetail OA on OA.Object = I.Object and OA.ObjectID = I.ObjectID
end
GO;

ALTER FUNCTION [lineage].[GetTrailForObject]
(	
	@Object varchar(50), 
	@ObjectID int,
	@Forward bit,
	@Depth int = 10
)
RETURNS @tbl TABLE
(
	IntersectID int, 
	IntersectTypeID int, 
	[Subject] varchar(50), 
	SubjectID int, 
	[Object] varchar(50), 
	ObjectID int, 
	[State] int, 
	PredicateID int, 
	PredicateName varchar(max), 
	PredicateInverse varchar(max), 
	PredicateType int, 
	Visited bit,
	Depth int
)
AS
BEGIN


	--TESTING---------------------
	--declare @tbl table (IntersectID int, IntersectTypeID int, Subject varchar(50), SubjectID int, Object varchar(50), ObjectID int, State int, PredicateID int, PredicateName varchar(max), PredicateInverse varchar(max), PredicateType int, Visited bit);
	--declare @Object varchar(50);
	--declare @ObjectID int;
	--declare @Forward bit;

	--select @Object = 'Artifact',
	--	   @ObjectID = 973683,
	--	   @Forward = 1;
	-------------------------------


	insert into @tbl
	select 
		P.*,
		0 as Visited,
		0 as Depth 
	from PredicateIntersect P
	where 
		((@Forward = 1 and [Subject] = @Object and SubjectID = @ObjectID) OR
		(@Forward = 0 and [Object] = @Object and ObjectID = @ObjectID)) AND
		PredicateType = 1;
		

	declare @level int = 1;
	declare @i int;
	select @i = count(*) from @tbl where Visited = 0;

	while @i != 0 and @level <= @Depth
	begin
		declare @intersectId int;
		select top 1 @intersectId = IntersectID from @tbl where Visited = 0; 

		update @tbl
		set Visited = 1
		where IntersectID = @intersectId;

		insert into @tbl
		select 
			P.*,
			0 as Visited,
			@level as Depth 
		from PredicateIntersect P
		cross apply (select * from @tbl where IntersectID = @intersectId) I
		where 
			((@Forward = 1 and P.[Subject] = I.[Object] and P.SubjectID = I.ObjectID) OR
			(@Forward = 0 and P.[Object] = I.[Subject] and P.ObjectID = I.SubjectID)) AND
			P.PredicateType = 1 AND P.IntersectID not in (select IntersectID from @tbl);

		select @i = count(*) from @tbl where Visited = 0;
		set @level = @level + 1
	end

	RETURN
END
GO;

ALTER FUNCTION [utility].[ObjectDetail]
(
--declare
	@type varchar(50), 
	@id int
--set @type = 'Domain'
--set @id = 1
)
RETURNS @tbl TABLE 
(
	ID int,
	AssetID bigint,
	UID uniqueidentifier,
	AssetTypeID int,
	Name nvarchar(max),
	TextPath nvarchar(2500),
	Description nvarchar(max),
	ParentID int null,
	ParentType nvarchar(250),
	Url nvarchar(2500),
	TypeID int,
	[Type] varchar(25),
	[TypeName] nvarchar(250),
	IconBackColor varchar(15),
	IconForeColor varchar(15),
	IconText varchar(15),
	Status nvarchar(25) null
) 
AS
BEGIN
	if @type = 'Artifact' or @type = 'Attribute' or @type = 'Fusion' or @type = 'FusionAttribute' or @type = 'Policy' or @type = 'ReferenceItem' or @type = 'Rule' or @type = 'Taxonomy'
	begin
		insert into @tbl (	ID,		UID,	AssetID,	AssetTypeID, Name,			TextPath,		[Description],	ParentID,	ParentType, Url,											TypeID,	[Type],	TypeName, Status)
			SELECT			ObjectID,	UID, ID, 		AssetTypeID, DisplayValue,	DisplayValue,	NULL,			null,		null,		dbo.GenerateObjectUrl(@type, TypeID, ObjectID),	TypeID,	Type,	TypeName, NULL
			FROM	AssetDetail
			where	Object = @type 
					and ObjectID = @id
	end

	if @type = 'ArtifactType' or @type = 'AttributeType' or @type = 'FusionType' or @type = 'FusionAttributeType' or @type = 'PolicyType' or @type = 'ReferenceItemType' or @type = 'RuleType' or @type = 'TaxonomyType'
	begin
		insert into @tbl (	ID,		UID, Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ObjectID, UID,		Name,	Name,		Description,	NULL,		NULL,		turl.[url] as Url,	ObjectID,		@type,	'Asset Type'
			FROM	AssetType O
			cross apply [dbo].GetAssetUrl(@type,@id,0) turl
			WHERE	Object = @type
					and ObjectID = @id
	end

	if @type = 'Group'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		Name,	Name,		Description,	NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	0,		@type,	'Group'
			FROM	[Group]
			WHERE	ID = @id
	end

	if @type = 'Intersect'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,													TypeID,				[Type],				TypeName)
			SELECT			O.ID,	IName.Name,	IName.Name,		'',				NULL,		@type,		dbo.GenerateObjectUrl(@type, O.IntersectTypeID, O.ID),	O.IntersectTypeID,	'IntersectType', ITN.Name	
			FROM	[Intersect] O
					INNER JOIN IntersectType T ON O.IntersectTypeID = T.ID and O.ID = @id
					CROSS APPLY dbo.GetIntersectNames(O.ID) IName	
					CROSS APPLY dbo.GetIntersectTypeNames(T.ID) ITN
	end

	if @type = 'IntersectType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		T.Name,	T.Name,		'',				NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Intersect Type'
			FROM	IntersectType 
			CROSS APPLY dbo.GetIntersectTypeNames(@id) T	
			WHERE	ID = @id
	end

	if @type = 'Issue'
	begin
		insert into @tbl (	ID,		Name,				TextPath,	[Description],	ParentID,	ParentType, Url,												TypeID,			[Type],			TypeName)
			SELECT			O.ID,	'',	'',		'',				NULL,		NULL,		dbo.GenerateObjectUrl(@type, O.IssueTypeID, O.ID),	O.IssueTypeID,	'IssueType',	T.Name
			FROM	Issue O
					INNER JOIN IssueType T ON O.IssueTypeID = T.ID AND O.ID = @id
	end

	if @type = 'IssueType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,													TypeID,				[Type],				TypeName)
			SELECT			O.ID,	O.Name,	O.Name,		O.Description,				NULL,		@type,		NULL,	O.ID,	'IssueType',	'Issue Type'
			FROM	IssueType O
			WHERE	ID = @id
	end

	if @type = 'Lookup'
	begin
		insert into @tbl (	ID,		Name,				TextPath,	[Description],	ParentID,	ParentType, Url,												TypeID,			[Type],			TypeName)
			SELECT			O.ID,	T.Name + ' Item',	T.Name,		'',				NULL,		NULL,		dbo.GenerateObjectUrl(@type, O.LookupTypeID, O.ID),	O.LookupTypeID,	'LookupType',	T.Name
			FROM	[Lookup] O
					INNER JOIN LookupType T ON O.LookupTypeID = T.ID AND O.ID = @id
	end

	if @type = 'LookupType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		Name,	Name,		'',				0,			@type,		dbo.GenerateObjectUrl(@type, ID, 0),	ID,		@type,	'Lookup Type'
			FROM	LookupType O
			WHERE	ID = @id
	end

	if @type = 'FusionQueryAttribute'
	begin
		insert into @tbl (	ID,		Name,		TextPath,	[Description],	ParentID,	ParentType, Url,	TypeID,						[Type],					TypeName)
			SELECT			O.ID,	O.DisplayValue,	O.DisplayValue,	'',				NULL,	@type,		dbo.GenerateObjectUrl(@type, 0, O.ID),
																											O.FusionQueryAttributeTypeID,	'FusionQueryAttributeType',	T.Name
			FROM	FusionQueryAttribute O
					INNER JOIN FusionQueryAttributeType T ON O.FusionQueryAttributeTypeID = T.ID and O.ID = @id					
	end
	
	if @type = 'FusionQueryAttributeType'
	begin
		insert into @tbl (	ID, Name,		TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,	O.Name,	O.Name,	'',				NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Fusion Query Attribute Type'
			FROM	FusionQueryAttributeType O
			WHERE	ID = @id
	end

	if @type = 'Report'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,	TypeID,				[Type],			TypeName)
			SELECT			O.ID,	O.Name,	O.Name,	O.Description,	NULL,		@type,		'#',	0,	'Report',	'Report'
			FROM	Report O
			WHERE	O.ID = @id
	end

	if @type = 'Resource'
	begin
		insert into @tbl (ID, Name, Url, TypeID, [Type], TypeName)
			select	ResourceID, FirstName + ' ' + LastName, dbo.GenerateObjectUrl(@type, 1, @id), 1, 'ResourceType', 'Employee'
			from	reporting.Global_Resource 
			where	ResourceID = @id
	end

	if @type = 'ResponsibilityType'
	begin
		insert into @tbl (	ID, Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,	O.Name,	NULL,		Description,	NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Responsibility Type'
			FROM	ResponsibilityType O
			WHERE	ID = @id
	end

	if @type = 'ResourceType'
	begin
		insert into @tbl (ID, Name, Url, TypeID, [Type], TypeName)
		values			(@id, 'Resource Type', '#/resources/administration', @id, @type, 'Resource Type')
	end

	if @type = 'RuleImplementation'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,	TypeID,				[Type],			TypeName, Status)
			SELECT			O.ID,	coalesce(O.Name,'Implementation ' + cast(o.id as nvarchar)) ,	coalesce(O.Name,'Implementation ' + cast(o.id as nvarchar)),	null,	T.ID,		'Rule',		dbo.GenerateObjectUrl(@type, T.ID, O.ID),	T.RuleTypeID,	'RuleType',	T.DisplayValue, 'Active'
			FROM	[RuleImplementation] O
					inner join [Rule] T on T.ID = O.RuleID
			WHERE	O.ID = @id
	end

	if @type = 'ShoppingCart'
	begin
			insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			O.ID,		Name,	Name,		NULL,	NULL,		NULL,		dbo.GenerateObjectUrl('ShoppingCartType', O.ShoppingCartTypeID, O.ID),	O.ID,		@type,	T.Name
			FROM	ShoppingCart O
			inner join ShoppingCartType T on O.ShoppingCartTypeID = T.ID
			WHERE	O.ID = @id
	end

	update	T
	set		T.IconBackColor = coalesce(S.IconBackColor, '#000000'),
			T.IconForeColor = coalesce(S.IconForeColor, '#ffffff'),
			T.IconText =	--case @type
							--	when 'Taxonomy' then 'IM'
							--	when 'TaxonomyType' then 'IM'
								--else 
								COALESCE(S.IconText, 'leaf') 
							--end
	from	@tbl T
			left join ObjectStyle S ON S.ObjectType = T.[Type] and S.ObjectID = T.TypeID

	RETURN
END
GO;

ALTER FUNCTION [dbo].[GetWorkflowConditionLabels]
(
	@conditions xml
)
RETURNS xml
AS
BEGIN
	declare @recordCount int;

	declare @results table (id int, FieldTypeID int, ValueType varchar(max), [Value] nvarchar(max), Operator varchar(max), VersionStepID int, FormInputID varchar(max), ContextualFieldID varchar(max), ValueLabel varchar(max), FieldLabel varchar(max));

	select 
		 @recordCount = count(*)
	from 
		@conditions.nodes('/Conditions/Condition') c(x);

		insert into @results (id, FieldTypeID, VersionStepID, FormInputID, ValueType, [Value], Operator, ContextualFieldID, ValueLabel, FieldLabel)
			select
			row_number() over (order by x.value('@FieldTypeID', 'int'), x.value('@VersionStepID', 'int'), x.value('@FormInputID', 'varchar(max)')) as id,
			 x.value('@FieldTypeID', 'int') as FieldTypeID
			,x.value('@VersionStepID', 'int') as VersionStepID  
			,x.value('@FormInputID', 'varchar(max)') as FormInputID
			,x.value('@ValueType', 'varchar(max)') as ValueType  
			,x.value('@Value', 'varchar(max)') as [Value]  
			,x.value('@Operator', 'varchar(max)') as [Operator] 
			,x.value('@ContextualFieldID', 'varchar(max)') as ContextualFieldID
			,null as ValueLabel
			,null as FieldLabel
		from 
			@conditions.nodes('/Conditions/Condition') c(x)
		left join FieldType FT on FT.ID = x.value('@FieldTypeID', 'int')
		left join workflow.VersionStep VS on VS.ID = x.value('@VersionStepID', 'int')

		
	while(@recordCount > 0)
	begin
		if (select top 1 ValueType from @results where id = @recordCount) in ('U', 'L')
		begin
		
			if ((select FieldTypeID from @results where id = @recordCount) is not null)
			begin
				declare @valueLabel varchar(max), @fieldLabel varchar(max);

				select 
					@valueLabel = coalesce(RI.DisplayValue, R.[Value]),
					@fieldLabel = FT.FriendlyName
				from 
					FieldType FT
				inner join @results R on R.id = @recordCount and FT.ID = R.FieldTypeID
				left join LookupType LT on FT.LookupObjectType = 'Lookup' and LT.ID = FT.LookupObjectID
				left join [Lookup] L on L.ID = (select top 1 [Value] from @results where id = @recordCount)
				left join ReferenceItem RI on FT.LookupObjectType = 'ReferenceItem' and FT.LookupObjectID = RI.ReferenceItemTypeID and RI.ID = R.[Value]

				update r
				set 
					r.ValueLabel = @valueLabel,
					r.FieldLabel = @fieldLabel
				from @results r
				where r.id = @recordCount;

			end
			
			if ((select FormInputID from @results where id = @recordCount) is not null)
			begin
				declare @fields xml, @valueLabel2 varchar(max);

				select @fields = VS.fields from 
				workflow.VersionStep VS
				inner join @results R on R.id = @recordCount and VS.ID = R.VersionStepID;


				select 
					@valueLabel2 = coalesce(RI.DisplayValue, R.[Value])
				from @fields.nodes('fields/form/field') f(x)
				inner join @results R on R.id = @recordCount
				inner join FieldType FT on FT.ID = x.value('@referenceFieldId', 'int')
				left join LookupType LT on FT.LookupObjectType = 'Lookup' and LT.ID = FT.LookupObjectID
				left join [Lookup] L on L.ID = (select top 1 [Value] from @results where id = @recordCount)
				left join ReferenceItem RI on FT.LookupObjectType = 'ReferenceItem' and FT.LookupObjectID = RI.ReferenceItemTypeID and RI.ID = R.[Value]
				where x.value('@id', 'varchar(max)') = R.FormInputID;


				update r
				set r.ValueLabel = @valueLabel2
				from @results r
				where r.id = @recordCount;


			end
		end	
		else
		begin

			if ((select FieldTypeID from @results where id = @recordCount) is not null)
			begin
				update r
				set r.ValueLabel = r.[Value],
				r.FieldLabel = coalesce(FT.FriendlyName,'[unknown field]')
				from @results r
				left join FieldType FT on FT.ID = r.FieldTypeID
				where r.id = @recordCount;
			end
			else
			begin
				update r
				set r.ValueLabel = r.[Value]
				from @results r
				where r.id = @recordCount;
			end
			

		end


		set @recordCount = @recordCount - 1;
	end

	RETURN 
		coalesce(
		 (select 
			r.FieldTypeID as 'Condition/@FieldTypeID',
			r.VersionStepID as 'Condition/@VersionStepID',
			r.FormInputID as 'Condition/@FormInputID',
			r.ValueType as 'Condition/@ValueType',
			r.[Value] as 'Condition/@Value',
			r.Operator as 'Condition/@Operator',
			r.ContextualFieldID as 'Condition/@ContextualFieldID',
			r.ValueLabel as 'Condition/@ValueLabel',
			r.FieldLabel as 'Condition/@FieldLabel' 
		from @results r
		for xml path(''), root('Conditions'))
		,
		'<Conditions />');
END
GO;

--CREATE PROCEDURE [dbo].[GetAverageScoreByAsset]
----declare
--	@assetID bigint-- = 42
--AS
--begin
--	declare @date date = getutcdate(),
--			@name nvarchar(250),
--			@assetTypeID int,
--			@typeName nvarchar(250),
--			@averageScore int,
--			@score int

--	select	@name = utility.GetAssetDisplayValue(A.ID),
--			@typeName = T.Name,
--			@assetTypeID = T.ID
--	from	Asset A
--			inner join AssetType T on T.ID = A.AssetTypeID and A.ID = @assetID;

--	select	top 1
--			@score = cast(Value * 100 as int)
--	from	metrics.Score
--	where	AssetID = @assetID
--			and EffectiveDate in (
--				select	min(EffectiveDate) as EffectiveDate
--				from	metrics.Score
--				where	AssetID = @assetID
--						and EffectiveDate <= @date
--			);
			
--	select	@averageScore = avg(cast(SC.Value * 100 as int))
--	from	metrics.Score SC
--			inner join (
--			select		S.AssetID,
--						min(S.EffectiveDate) as EffectiveDate
--			from		metrics.Score S
--						inner join Asset A on A.AssetTypeID = @assetTypeID and S.EffectiveDate <= @date
--			group by	S.AssetID
--			) S on S.AssetID = SC.AssetID and S.EffectiveDate = SC.EffectiveDate;

--	select	@assetID as AssetID, 
--			@name as AssetName, 
--			@assetTypeID as AssetTypeID,
--			@typeName as AssetTypeName, 
--			@score as Score, 
--			@averageScore as AverageScore 
--end
--GO;

--CREATE PROCEDURE [dbo].[GetScoreHistoryByAsset]
----declare
--	@assetID bigint --= 42
--AS
--begin
--	select		EffectiveDate as [Date],
--				cast(Value * 100 as int) as Score
--	from		metrics.Score
--	where		AssetID = @assetID
--	order by	EffectiveDate asc
--end
--GO;

CREATE procedure [utility].[GetAssignedResponsibilityNameForWorkflow]
	@workflowID int,
	@workflowStepID int = 0,
	@workflowItemID int = 0
as
begin
	declare @objectId int,			
			@objectType varchar(50),
			@assetId bigint,
			@assetTypeId bigint,
			@responsibilityTypeID int,
			@issueId int;
	declare @xmlSettings xml;
	declare @responsibleSide varchar(50);

	declare @tbl table (ResponsibilityName nvarchar(25))
	declare @responsibilityIDTbl table (RowID int not null identity(1,1) primary key, ResponsibilityTypeID int not null);
	--get the responsibility for this step from the settings of the step

	select @xmlSettings = settings from [workflow].[VersionStep] where id = @workflowStepID
	
	insert into @responsibilityIDTbl select T.C.value('.','int') as responsibility from @xmlSettings.nodes('(/settings/ResponsibilityTypeID)') as T(C) ;

	select @responsibleSide = upper(T.C.value('.','varchar(50)')) from @xmlSettings.nodes('(/settings/ResponsibilitySide)') as T(C);
		
	declare @i int
	select @i = min(RowID) from @responsibilityIDTbl
	declare @max int
	select @max = max(RowID) from @responsibilityIDTbl

	while @i <= @max and not exists (select 1 from @tbl) begin
		select @responsibilityTypeID = ResponsibilityTypeID from @responsibilityIDTbl where RowID = @i
		set @i = @i + 1

		-- check object	
		begin
			select 
				@objectType = i.object, 
				@objectId = i.objectid,
				@assetId = a.id,
				@assetTypeId = a.assetTypeId 
			from [workflow].[item] i
			left join Asset a on a.object = i.object and A.objectid = i.objectid 
			where i.id = @workflowItemID;
			
			if @objectType = 'Issue'
			begin				
				select @issueId = id, @objectType = [object], @objectId = [objectid] from Issue where id = @objectId
			end

			--if the object is an intersect we need to look at the settings to see what side of the intersect to look at
			-- then we need to load the object from the corresponding side.
			
			if @objectType = 'Intersect'
			begin				
				if @responsibleSide = 'SUBJECT'
				begin
					select @objectType = [subject], @objectId = [subjectId] from [intersect] where id = @objectId;
				end
				else if @responsibleSide = 'OBJECT'
				begin
					select @objectType = [object], @objectId = [objectId] from [intersect] where id = @objectId;
				end
			end

			insert into @tbl
				select	distinct RD.ResponsibilityTypeName
				from	ResponsibilityDetail RD
						inner join reporting.Global_Resource R on 
								((RD.Object = @objectType and RD.ObjectID = @objectId) 
									or (@assetTypeId != 0 and RD.AssetID = 0 and RD.AssetTypeID = @assetTypeId))
								and RD.ResponsibilityTypeID = @responsibilityTypeID
								and RD.ResourceID = R.ResourceID
								and R.Email not like '%?subject=%' and R.Status = 'Active'
		end		
	end;

	select * from @tbl;
end
GO;

ALTER TRIGGER [dbo].[Artifact_AfterDelete]
	ON [dbo].[Artifact]
	AFTER DELETE
AS
	SET NOCOUNT ON
	delete Asset
	where Object = 'Artifact' and ObjectID in (select ID from deleted);
GO;

ALTER TRIGGER [dbo].[ArtifactType_AfterDelete]
   ON  [dbo].[ArtifactType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON
	delete AssetType where Object = 'ArtifactType' and ObjectID in (select ID from deleted)
GO;

ALTER TRIGGER [dbo].[Attribute_AfterDelete]
   ON  [dbo].[Attribute] 
   AFTER DELETE
AS 
	SET NOCOUNT ON
	delete Asset where Object = 'Attribute' and ObjectID in (select ID from deleted)
GO;

ALTER TRIGGER [dbo].[AttributeType_AfterDelete]
   ON  [dbo].[AttributeType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON
	delete AssetType where Object = 'AttributeType' and ObjectID in (select ID from deleted)
GO;

ALTER TRIGGER [dbo].[Fusion_AfterDelete]
   ON  [dbo].[Fusion] 
   AFTER DELETE
AS 
	SET NOCOUNT ON
	delete Asset where Object = 'Fusion' and ObjectID in (select ID from deleted)
GO;

ALTER TRIGGER [dbo].[FusionType_AfterDelete]
   ON  [dbo].[FusionType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON
	delete AssetType where Object = 'FusionType' and ObjectID in (select ID from deleted)
GO;

ALTER TRIGGER [dbo].[FusionAttribute_AfterDelete]
   ON  [dbo].[FusionAttribute] 
   AFTER DELETE
AS 
	SET NOCOUNT ON
	delete Asset where Object = 'FusionAttribute' and ObjectID in (select ID from deleted)
GO;

ALTER TRIGGER [dbo].[FusionAttributeType_AfterDelete]
   ON  [dbo].[FusionAttributeType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON
	delete AssetType where Object = 'FusionAttributeType' and ObjectID in (select ID from deleted)
GO;

ALTER TRIGGER [dbo].[FusionQueryAttribute_AfterDelete]
	ON [dbo].[FusionQueryAttribute]
	AFTER DELETE
AS
	SET NOCOUNT ON
	delete Asset
	where Object = 'FusionQueryAttribute' and ObjectID in (select ID from deleted);
GO;

ALTER TRIGGER [dbo].[FusionQueryAttributeType_AfterDelete]
   ON  [dbo].[FusionQueryAttributeType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON
	delete AssetType where Object = 'FusionQueryAttributeType' and ObjectID in (select ID from deleted)
GO;

ALTER TRIGGER [dbo].[Group_AfterDelete]
   ON  [dbo].[Group] 
   AFTER DELETE
AS 
	SET NOCOUNT ON
	delete Asset where Object = 'Group' and ObjectID in (select ID from deleted)
GO;

ALTER TRIGGER [dbo].[Organization_AfterDelete]
	ON [dbo].[Organization]
	AFTER DELETE
AS
	SET NOCOUNT ON
	delete Asset where Object = 'Organization' and ObjectID in (select ID from deleted)
GO;

ALTER TRIGGER [dbo].[OrganizationType_AfterDelete]
   ON  [dbo].[OrganizationType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON
	delete AssetType where Object = 'OrganizationType' and ObjectID in (select ID from deleted)
GO;

ALTER TRIGGER [dbo].[Policy_AfterDelete]
   ON  [dbo].[Policy] 
   AFTER DELETE
AS 
	SET NOCOUNT ON
	delete Asset where Object = 'Policy' and ObjectID in (select ID from deleted)
GO;

ALTER TRIGGER [dbo].[PolicyType_AfterDelete]
   ON  [dbo].[PolicyType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON
	delete AssetType where Object = 'PolicyType' and ObjectID in (select ID from deleted)
GO;

ALTER TRIGGER [dbo].[ReferenceItem_AfterDelete]
   ON  [dbo].[ReferenceItem] 
   AFTER DELETE
AS 
	SET NOCOUNT ON
	delete Asset where Object = 'ReferenceItem' and ObjectID in (select ID from deleted)
GO;

ALTER TRIGGER [dbo].[ReferenceItemType_AfterDelete]
   ON  [dbo].[ReferenceItemType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON
	delete AssetType where Object = 'ReferenceItemType' and ObjectID in (select ID from deleted)
GO;

ALTER TRIGGER [dbo].[Rule_AfterDelete]
   ON  [dbo].[Rule] 
   AFTER DELETE
AS 
	SET NOCOUNT ON
	delete Asset where Object = 'Rule' and ObjectID in (select ID from deleted)
GO;

ALTER TRIGGER [dbo].[RuleType_AfterDelete]
   ON  [dbo].[RuleType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON
	delete AssetType where Object = 'RuleType' and ObjectID in (select ID from deleted)
GO;

ALTER TRIGGER [dbo].[Taxonomy_AfterDelete]
	ON [dbo].[Taxonomy]
	AFTER DELETE
AS
	SET NOCOUNT ON
	delete Asset where Object = 'Taxonomy' and ObjectID in (select ID from deleted)
GO;

ALTER TRIGGER [dbo].[TaxonomyType_AfterDelete]
   ON  [dbo].[TaxonomyType] 
   AFTER DELETE
AS 
BEGIN
	SET NOCOUNT ON
	delete AssetType where Object = 'TaxonomyType' and ObjectID in (select ID from deleted)
END
GO;