
CREATE procedure [bulkload].[Synonyms]
--declare
	@id int
--set @id = 255
as
begin
	set nocount on;

	declare @synonymErrorDetailMessage varchar(200)

	declare @current int,
			@max int,
			@sourceSubject varchar(50),
			@sourceObject varchar(50),
			@sourceObjectID int,
			@sourceObjectTypeName nvarchar(1000),
			@sourceName nvarchar(500),
			@targetObjectTypeName nvarchar(1000),
			@targetSubject varchar(50),
			@targetObject varchar(50),
			@targetObjectID int,
			@targetName nvarchar(500),
			@predicateID int,
			@UpdatedBy int,
			@rundate timestamp
			
	select	@current = min(I.RowIndex),
			@max = max(I.RowIndex)
	from	LoadItem I
			inner join LoadItemColumn ST on ST.LoadID = I.LoadID and ST.RowIndex = I.RowIndex and St.ColumnIndex = 1			-- source object type
			inner join LoadItemColumn STN on STN.LoadID = I.LoadID and STN.RowIndex = I.RowIndex and StN.ColumnIndex = 2		-- source object type name
			inner join LoadItemColumn S on S.LoadID = I.LoadID and S.RowIndex = I.RowIndex and S.ColumnIndex = 4				-- source object name
			inner join LoadItemColumn TT on TT.LoadID = I.LoadID and TT.RowIndex = I.RowIndex and TT.ColumnIndex = 5			-- target object type
			inner join LoadItemColumn TTN on TTN.LoadID = I.LoadID and TTN.RowIndex = I.RowIndex and TTN.ColumnIndex = 6		-- target object type name
			inner join LoadItemColumn T on T.LoadID = I.LoadID and T.RowIndex = I.RowIndex and T.ColumnIndex = 8				-- target object name
	where	I.LoadID = @id
			
	-- go row by row
	while @current <= @max
	begin
		--load the objects / id's for the focal, source, and target objects
		select	@sourceObject = ST.Value,
				@sourceObjectTypeName = STN.Value,
				@sourceName = S.Value,
				@sourceSubject = SS.Value,
						
				@targetObject = TT.Value,
				@targetObjectTypeName = TTN.Value,
				@targetName = T.Value,
				@targetSubject = TS.Value
		from	LoadItem I
				inner join LoadItemColumn ST on ST.LoadID = I.LoadID and ST.RowIndex = I.RowIndex and St.ColumnIndex = 1		-- source object type
				inner join LoadItemColumn STN on STN.LoadID = I.LoadID and STN.RowIndex = I.RowIndex and StN.ColumnIndex = 2	-- source object type name
				inner join LoadItemColumn SS on SS.LoadID = I.LoadID and SS.RowIndex = I.RowIndex and SS.ColumnIndex = 3		-- source object subject
				inner join LoadItemColumn S on S.LoadID = I.LoadID and S.RowIndex = I.RowIndex and S.ColumnIndex = 4			-- source object name
				inner join LoadItemColumn TT on TT.LoadID = I.LoadID and TT.RowIndex = I.RowIndex and TT.ColumnIndex = 5		-- target object type
				inner join LoadItemColumn TTN on TTN.LoadID = I.LoadID and TTN.RowIndex = I.RowIndex and TTN.ColumnIndex = 6	-- target object type name
				inner join LoadItemColumn TS on TS.LoadID = I.LoadID and TS.RowIndex = I.RowIndex and TS.ColumnIndex = 7		-- target object subject
				inner join LoadItemColumn T on T.LoadID = I.LoadID and T.RowIndex = I.RowIndex and T.ColumnIndex = 8			-- target object name
		where	I.LoadID = @id and I.RowIndex = @current

		select @sourceObjectID = 0, @targetObjectID = 0, @predicateID = 0;

		select @predicateID = min(ID) from [Predicate] where [Type] = 6;				

		if @sourceObject = 'Artifact'
		begin
			select	top 1
					@sourceObjectID = cod.objectid										
			from	[cache].objectdetails cod
					inner join artifact a on (cod.objectid = a.id)
					inner join taxonomytype t on (a.taxonomytypeid = t.id)
			where	cod.[object] = @sourceObject and cod.textpath = @sourceName and cod.objecttypename = @sourceObjectTypeName and t.Name = @sourceSubject
		end
		else
		begin
			-- load source object
			select	top 1
					@sourceObjectID = cod.objectid						
			from	[cache].objectdetails cod
			where	cod.[object] = @sourceObject and cod.textpath = @sourceName and cod.objecttypename = @sourceObjectTypeName
		end

		if @targetObject = 'Artifact'
		begin
			-- load target object
			select	top 1
					@targetObjectID = cod.objectid												
			from	[cache].objectdetails cod
					inner join artifact a on (cod.objectid = a.id)
					inner join taxonomytype t on (a.taxonomytypeid = t.id)
			where	cod.[object] = @targetObject and cod.textpath = @targetName and cod.objecttypename = @targetObjectTypeName and t.Name = @targetSubject
		end
		else
		begin
			-- load target object
			select	top 1
					@targetObjectID = cod.objectid												
			from	[cache].objectdetails cod
			where	cod.[object] = @targetObject and cod.textpath = @targetName and cod.objecttypename = @targetObjectTypeName
		end

		--debug 
		--select @sourceObjectID, @sourceObject, @targetObjectID, @targetObject, @predicateID

		--if all are provided we are good otherwise error
		if @sourceObjectID > 0 and @targetObjectID > 0 and @predicateID > 0
			begin
				-- add intersect between source / target if one doesn't exist
				exec [dbo].[AddRelationship] @UpdatedBy, @rundate, @sourceObject, @sourceObjectID, 2, null, null, @targetObject, @targetObjectID;

				update	LoadItem
				set		[Status] = 1,
						StatusMessage = 'Successfully added synonym'
				where	LoadID = @id
						and RowIndex = @current
			end -- if valid
		else
			begin
				set @synonymErrorDetailMessage = '';

				if @sourceObjectID = 0
				begin
					set @synonymErrorDetailMessage = @synonymErrorDetailMessage + '  Source object is invalid.';
				end

				if @targetObjectID = 0
				begin
					set @synonymErrorDetailMessage = @synonymErrorDetailMessage + '  Target object is invalid.';
				end

				if @predicateID = 0
				begin
					set @synonymErrorDetailMessage = @synonymErrorDetailMessage + '  No predicate of type synonym.';
				end

				update	LoadItem
				set		[Status] = 0,
						StatusMessage = 'Failed to add synonym. ' + @synonymErrorDetailMessage + ' [source id:' + convert(varchar(10),@sourceObjectID) + ' type:' + @sourceObject +'] [target id:' + convert(varchar(10), @targetObjectID) + ' type:' + @targetObject + ']'
				where	LoadID = @id
						and RowIndex = @current
			end -- else not valid
				
		set @current = @current + 1
	end
end