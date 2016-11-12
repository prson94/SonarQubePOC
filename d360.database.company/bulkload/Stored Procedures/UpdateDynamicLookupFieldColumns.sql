

--CREATE procedure [bulkload].[Synonyms]
----declare
--	@id int
----set @id = 255
--as
--begin
--	set nocount on;

--	declare @synonymErrorDetailMessage varchar(200)

--	declare @current int,
--			@max int,
--			@sourceSubject varchar(50),
--			@sourceObject varchar(50),
--			@sourceObjectID int,
--			@sourceObjectTypeName nvarchar(1000),
--			@sourceName nvarchar(500),
--			@targetObjectTypeName nvarchar(1000),
--			@targetSubject varchar(50),
--			@targetObject varchar(50),
--			@targetObjectID int,
--			@targetName nvarchar(500),
--			@predicateID int,
--			@UpdatedBy int,
--			@rundate timestamp
			
--	select	@current = min(I.RowIndex),
--			@max = max(I.RowIndex)
--	from	LoadItem I
--			inner join LoadItemColumn ST on ST.LoadID = I.LoadID and ST.RowIndex = I.RowIndex and St.ColumnIndex = 1			-- source object type
--			inner join LoadItemColumn STN on STN.LoadID = I.LoadID and STN.RowIndex = I.RowIndex and StN.ColumnIndex = 2		-- source object type name
--			inner join LoadItemColumn S on S.LoadID = I.LoadID and S.RowIndex = I.RowIndex and S.ColumnIndex = 4				-- source object name
--			inner join LoadItemColumn TT on TT.LoadID = I.LoadID and TT.RowIndex = I.RowIndex and TT.ColumnIndex = 5			-- target object type
--			inner join LoadItemColumn TTN on TTN.LoadID = I.LoadID and TTN.RowIndex = I.RowIndex and TTN.ColumnIndex = 6		-- target object type name
--			inner join LoadItemColumn T on T.LoadID = I.LoadID and T.RowIndex = I.RowIndex and T.ColumnIndex = 8				-- target object name
--	where	I.LoadID = @id
			
--	-- go row by row
--	while @current <= @max
--	begin
--		--load the objects / id's for the focal, source, and target objects
--		select	@sourceObject = ST.Value,
--				@sourceObjectTypeName = STN.Value,
--				@sourceName = S.Value,
--				@sourceSubject = SS.Value,
						
--				@targetObject = TT.Value,
--				@targetObjectTypeName = TTN.Value,
--				@targetName = T.Value,
--				@targetSubject = TS.Value
--		from	LoadItem I
--				inner join LoadItemColumn ST on ST.LoadID = I.LoadID and ST.RowIndex = I.RowIndex and St.ColumnIndex = 1		-- source object type
--				inner join LoadItemColumn STN on STN.LoadID = I.LoadID and STN.RowIndex = I.RowIndex and StN.ColumnIndex = 2	-- source object type name
--				inner join LoadItemColumn SS on SS.LoadID = I.LoadID and SS.RowIndex = I.RowIndex and SS.ColumnIndex = 3		-- source object subject
--				inner join LoadItemColumn S on S.LoadID = I.LoadID and S.RowIndex = I.RowIndex and S.ColumnIndex = 4			-- source object name
--				inner join LoadItemColumn TT on TT.LoadID = I.LoadID and TT.RowIndex = I.RowIndex and TT.ColumnIndex = 5		-- target object type
--				inner join LoadItemColumn TTN on TTN.LoadID = I.LoadID and TTN.RowIndex = I.RowIndex and TTN.ColumnIndex = 6	-- target object type name
--				inner join LoadItemColumn TS on TS.LoadID = I.LoadID and TS.RowIndex = I.RowIndex and TS.ColumnIndex = 7		-- target object subject
--				inner join LoadItemColumn T on T.LoadID = I.LoadID and T.RowIndex = I.RowIndex and T.ColumnIndex = 8			-- target object name
--		where	I.LoadID = @id and I.RowIndex = @current

--		select @sourceObjectID = 0, @targetObjectID = 0, @predicateID = 0;

--		select @predicateID = min(ID) from [Predicate] where [Type] = 6;				

--		if @sourceObject = 'Artifact'
--		begin
--			select	top 1
--					@sourceObjectID = cod.objectid										
--			from	[cache].objectdetails cod
--					inner join artifact a on (cod.objectid = a.id)
--					inner join taxonomytype t on (a.taxonomytypeid = t.id)
--			where	cod.[object] = @sourceObject and cod.textpath = @sourceName and cod.objecttypename = @sourceObjectTypeName and t.Name = @sourceSubject
--		end
--		else
--		begin
--			-- load source object
--			select	top 1
--					@sourceObjectID = cod.objectid						
--			from	[cache].objectdetails cod
--			where	cod.[object] = @sourceObject and cod.textpath = @sourceName and cod.objecttypename = @sourceObjectTypeName
--		end

--		if @targetObject = 'Artifact'
--		begin
--			-- load target object
--			select	top 1
--					@targetObjectID = cod.objectid												
--			from	[cache].objectdetails cod
--					inner join artifact a on (cod.objectid = a.id)
--					inner join taxonomytype t on (a.taxonomytypeid = t.id)
--			where	cod.[object] = @targetObject and cod.textpath = @targetName and cod.objecttypename = @targetObjectTypeName and t.Name = @targetSubject
--		end
--		else
--		begin
--			-- load target object
--			select	top 1
--					@targetObjectID = cod.objectid												
--			from	[cache].objectdetails cod
--			where	cod.[object] = @targetObject and cod.textpath = @targetName and cod.objecttypename = @targetObjectTypeName
--		end

--		--debug 
--		--select @sourceObjectID, @sourceObject, @targetObjectID, @targetObject, @predicateID

--		--if all are provided we are good otherwise error
--		if @sourceObjectID > 0 and @targetObjectID > 0 and @predicateID > 0
--			begin
--				-- add intersect between source / target if one doesn't exist
--				exec [dbo].[AddRelationship] @UpdatedBy, @rundate, @sourceObject, @sourceObjectID, 2, null, null, @targetObject, @targetObjectID;

--				update	LoadItem
--				set		[Status] = 1,
--						StatusMessage = 'Successfully added synonym'
--				where	LoadID = @id
--						and RowIndex = @current
--			end -- if valid
--		else
--			begin
--				set @synonymErrorDetailMessage = '';

--				if @sourceObjectID = 0
--				begin
--					set @synonymErrorDetailMessage = @synonymErrorDetailMessage + '  Source object is invalid.';
--				end

--				if @targetObjectID = 0
--				begin
--					set @synonymErrorDetailMessage = @synonymErrorDetailMessage + '  Target object is invalid.';
--				end

--				if @predicateID = 0
--				begin
--					set @synonymErrorDetailMessage = @synonymErrorDetailMessage + '  No predicate of type synonym.';
--				end

--				update	LoadItem
--				set		[Status] = 0,
--						StatusMessage = 'Failed to add synonym. ' + @synonymErrorDetailMessage + ' [source id:' + convert(varchar(10),@sourceObjectID) + ' type:' + @sourceObject +'] [target id:' + convert(varchar(10), @targetObjectID) + ' type:' + @targetObject + ']'
--				where	LoadID = @id
--						and RowIndex = @current
--			end -- else not valid
				
--		set @current = @current + 1
--	end
--end
--GO

CREATE procedure [bulkload].[UpdateDynamicLookupFieldColumns]
	@id int,
	@startColumnIndex int,
	@endColumnIndex int
as
begin
	set nocount on;
	update	T
	set		T.LookupObject = S.LookupObject,
			T.LookupObjectID = S.LookupObjectID
	from	LoadItemColumn T
			inner join	(
						select	IC.LoadID,
								IC.RowIndex,
								IC.ColumnIndex,
								case 
									when L_A.ID is not null then 'Artifact'
									when L_D.ID is not null then 'Domain'
									when L_DI.ID is not null then 'DomainItem'
									when L_F.ID is not null then 'FusionAttribute'
									--when L_I.ID is not null then 'Intersect'
									when L_L.Value is not null then 'Lookup'
									when L_T.ID is not null then 'Taxonomy'
									else NULL
								end as LookupObject,
								coalesce(L_A.ID, L_D.ID, L_DI.ID, L_F.ID, L_L.Value, L_T.ID) as LookupObjectID -- L_I.ID,
						from	FieldType F
								inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
								inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
								inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
								
								left join Artifact L_A on F.LookupObjectType in ('Artifact', 'ArtifactType') and L_A.ArtifactTypeID = F.LookupObjectID and (L_A.[Name] = IC.Value OR L_A.TextPath = IC.Value)
								left join Domain L_D on F.LookupObjectType in ('Domain', 'DomainType') and L_D.DomainTypeID = F.LookupObjectID and L_D.[Name] = IC.Value
								left join DomainItem L_DI on F.LookupObjectType = 'DomainItem' and L_DI.DomainID = F.LookupObjectID and L_DI.[Name] = IC.Value
								left join FusionAttribute L_F on F.LookupObjectType = 'FusionAttributeType' and L_F.FusionAttributeTypeID = F.LookupObjectID and (L_F.[Name] = IC.Value OR L_F.TextPath = IC.Value)
								--left join [Intersect] L_I on F.LookupObjectType = 'IntersectType' and L_I.IntersectTypeID = F.LookupObjectID and L_I.[Name] = IC.Value
								left join [FieldLookupValue] L_L on F.ID = L_L.FieldTypeID and F.LookupObjectType = 'Lookup' and L_L.LookupObjectType = 'Lookup' and L_L.LookupObjectID = F.LookupObjectID and L_L.[Text] = IC.Value
								left join Taxonomy L_T on F.LookupObjectType in ('Taxonomy', 'TaxonomyType') and L_T.TaxonomyTypeID = F.LookupObjectID and (L_T.[Name] = IC.Value OR L_T.TextPath = IC.Value)
						where	C.ColumnIndex between @startColumnIndex and @endColumnIndex
						) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

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