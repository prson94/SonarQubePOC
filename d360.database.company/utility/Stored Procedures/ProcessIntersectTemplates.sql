CREATE PROCEDURE [utility].[ProcessIntersectTemplates]
AS
BEGIN
	SET NOCOUNT ON;

	Declare @ExecutionID int = 0,			
			@NumberOfObjectsUpdated int = 0,
			@currentTemplateID int = 1,
			@maxTemplateID int = 0,
			@NumberOfObjectsConsidered int = 0,
			@NumberOfIntersectsAdded int = 0;		

	declare @intersectTable table	(
		IntersectTypeID int, IntersectID int, ID int, SourceObject varchar(50),
		SourceObjectID int, SourceIntersectTypeNodeID int, [TargetObject] varchar(50), TargetObjectID int, TargetIntersectTypeNodeID int,
		[type] int, predicateid int	
	)

	declare @intersectToItemTable table	(
		IntersectTypeID int, IntersectID int, ID int, SourceObject varchar(50),
		SourceObjectID int, SourceIntersectTypeNodeID int, [TargetObject] varchar(50), TargetObjectID int, TargetIntersectTypeNodeID int,
		[type] int, predicateid int				
	)

	declare @itemsWeNeedIntersectsTO table	(
		SourceObject varchar(50),SourceObjectID int
	)

	declare @intersectToItemTempTable [utility].[DiagramRelationshipTable];
	
	declare @intersectToItemNotInDiagramTempTable table	(
		IntersectTypeID int, SourceObject varchar(50),SourceObjectID int, SourceIntersectTypeNodeID int, 
		[TargetObject] varchar(50), TargetObjectID int, TargetIntersectTypeNodeID int							
	)


	declare @intersectToItemNotInDiagramTable table	(
		IntersectTypeID int, SourceObject varchar(50),SourceObjectID int, SourceIntersectTypeNodeID int, 
		[TargetObject] varchar(50), TargetObjectID int, TargetIntersectTypeNodeID int							
	)


	Declare @TemplateTable Table(ID int identity,TemplateID int,Query varchar(max), [Object] varchar(50), [ObjectID] int);

	IF OBJECT_ID('tempdb..#itemsToCopyToTable') IS NOT NULL
		DROP TABLE #itemsToCopyToTable;

	create table #itemsToCopyToTable (ID int identity, ObjectID int, [Object] varchar(50))
	
	-- check if there is any work
		-- any templates?
	insert into @TemplateTable
		select	ID, Query,[Object],[ObjectID]
		from	[dbo].[IntersectMapTemplate]
		where [Enabled] = 1;
	
	if not exists(select 1 from @TemplateTable)
	begin
		Print 'No enabled templates'
		return;
	end;

	-- log start
	--Log this run get a new id from the fusion.promotion table
	insert into [dbo].[IntersectMapTemplateLogSummary] ( DateStarted )
									values ( CURRENT_TIMESTAMP)

	select @ExecutionID =  SCOPE_IDENTITY()


	-- loop through the templates
	select @maxTemplateID = max(ID) from @TemplateTable;

	while (@currentTemplateID <= @maxTemplateID)
	begin
		
		declare @objectID int,
				@object varchar(50),
				@query nvarchar(max)

		select	@objectID = [ObjectID],
				@object = [Object],
				@query = Query					
			from	@TemplateTable
			where	ID = @currentTemplateID

		
		--load relations for item in object/objectid
		
		delete from @intersectTable;
		delete from @intersectToItemTable;
		truncate table #itemsToCopyToTable;
		delete from @itemsWeNeedIntersectsTO;
		delete from @intersectToItemNotInDiagramTable;
		
		insert into @intersectTable			
			select	distinct
					R.IntersectTypeID,
					R.IntersectID,
					M.ID,	
					R.SourceObject,		
					R.SourceObjectID,						
					R.SourceIntersectTypeNodeID,
					R.TargetObject,
					R.TargetObjectID,
					R.TargetIntersecttypeNodeID,
					m.[type],
					m.[predicateid]
			from	IntersectMap M
					inner join [cache].[Relationships] R on M.SubjectIntersectNodeID = R.SourceIntersectNodeID and M.ObjectIntersectNodeID = R.TargetintersectNodeID and M.[Type] = 1
					inner join [cache].ObjectDetails SD on SD.[Object] = R.SourceObject and SD.ObjectID = R.SourceObjectID
					inner join [cache].ObjectDetails TD on TD.[Object] = R.TargetObject and TD.ObjectID = R.TargetObjectID
					inner join Predicate P on P.ID = M.PredicateID
					inner join [cache].[Relationship] SR on SR.SourceObject = @object and SR.SourceObjectID = @objectID and SR.TargetObject = R.SourceObject and SR.TargetObjectID = R.SourceObjectID
					inner join [cache].[Relationship] TR on TR.SourceObject = @object and TR.SourceObjectID = @objectID and TR.TargetObject = R.TargetObject and TR.TargetObjectID = R.TargetObjectID
			union
			select	distinct
					R.IntersectTypeID,
					R.IntersectID,
					M.ID,	
					R.SourceObject,		
					R.SourceObjectID,
					R.SourceIntersectTypeNodeID,						
					R.TargetObject,
					R.TargetObjectID,
					R.TargetIntersecttypeNodeID,
					m.[type],
					m.[predicateid]				
			from	IntersectMap M
					inner join [cache].[Relationships] R on M.SubjectIntersectNodeID = R.SourceIntersectNodeID and M.ObjectIntersectNodeID = R.TargetintersectNodeID and R.SourceObject = @object and R.SourceObjectID = @objectID and M.[Type] = 1
					inner join [cache].ObjectDetails SD on SD.[Object] = R.SourceObject and SD.ObjectID = R.SourceObjectID
					inner join [cache].ObjectDetails TD on TD.[Object] = R.TargetObject and TD.ObjectID = R.TargetObjectID
					inner join Predicate P on P.ID = M.PredicateID
			union
			select	distinct
					R.IntersectTypeID,
					R.IntersectID,
					M.ID,		
					R.SourceObject,	
					R.SourceObjectID,	
					R.SourceIntersectTypeNodeID,					
					R.TargetObject,
					R.TargetObjectID,
					R.TargetIntersecttypeNodeID,
					m.[type],
					m.[predicateid]
			from	IntersectMap M
					inner join [cache].[Relationships] R on M.SubjectIntersectNodeID = R.SourceIntersectNodeID and M.ObjectIntersectNodeID = R.TargetintersectNodeID and R.TargetObject = @object and R.TargetObjectID = @objectID and M.[Type] = 1
					inner join [cache].ObjectDetails SD on SD.[Object] = R.SourceObject and SD.ObjectID = R.SourceObjectID
					inner join [cache].ObjectDetails TD on TD.[Object] = R.TargetObject and TD.ObjectID = R.TargetObjectID
					inner join Predicate P on P.ID = M.PredicateID
							
		-- insert the intersect to the items that will be replaced into separate table
		insert into @intersectToItemTable select * from @intersectTable where (sourceobject = @object and sourceobjectid = @objectid) or (targetobject = @object and targetobjectid = @objectid)
		
		--delete the intersects that need to be updated
		delete from @intersectTable where (sourceobject = @object and sourceobjectid = @objectid) or (targetobject = @object and targetobjectid = @objectid)

		
		insert into @itemsWeNeedIntersectsTO
			select distinct sourceobject, sourceobjectid from @intersectTable
			union
			select distinct targetobject, targetobjectid from @intersectTable

		-- remove items that will be added as result of relation in diagram
		delete w
			from @itemsWeNeedIntersectsTO w
			inner join @intersectToItemTable it
			on w.sourceobject = it.sourceobject and w.sourceobjectid = it.sourceobjectid

		delete w
			from @itemsWeNeedIntersectsTO w
			inner join @intersectToItemTable it
			on w.sourceobject = it.targetobject and w.sourceobjectid = it.targetobjectid

		
		-- load intersects between above objects and the source object
		
		-- load the intersect type info for the intersects to all items in the diagram
		insert into @intersectToItemNotInDiagramTable
			select
				inter.intersecttypeid as IntersectTypeID,
				inode1.objecttype as SourceObject,
				inode1.objectid as SourceObjectID,
				inode1.intersecttypenodeid as SourceIntersectTypeNodeID,			
				inode2.objecttype as TargetObject,
				inode2.objectid as TargetObjectID,
				inode2.intersecttypenodeid as TargetIntersectTypeNodeID		
			from 
				intersectnode inode1
				inner join @itemsWeNeedIntersectsTO objs on(inode1.objectid = objs.sourceobjectid and inode1.objecttype = objs.sourceobject)
				inner join intersectnode inode2 on(inode1.intersectid = inode2.intersectid and inode2.objectid = @objectID and inode2.objecttype = @object)
				inner join [intersect] inter on (inter.id = inode2.intersectid)
			
		
		-- execute the query which will give us the objects we need to copy the above intersects to

		select @query = 'INSERT INTO #itemsToCopyToTable ' + @query;
		
		exec sp_executesql @query;
		
		declare @currentItemToUpdate int = 1,
				@maxItemToUpdate int = 0;

		select @currentItemToUpdate = min(ID) from #itemsToCopyToTable; -- table variable cleared but cant be truncated
		select @maxItemToUpdate = max(ID) from #itemsToCopyToTable;
		
		select @NumberOfObjectsConsidered = @NumberOfObjectsConsidered + @maxItemToUpdate;
		
		-- loop through the items we are going to clone too
		while (@currentItemToUpdate <= @maxItemToUpdate)
		begin			
			declare @currentObjectID int,
					@currentObjectType varchar(50);

			delete from @intersectToItemTempTable;
			delete from @intersectToItemNotInDiagramTempTable;

			select @currentObjectID = ObjectID,
					@currentObjectType = [Object]
				from #itemsToCopyToTable where id = @currentItemToUpdate;

			-- for each item in the query we need to 
			-- replace object/objectid with current object and insert new relations in
			insert into @intersectToItemTempTable select *,1 from @intersectToItemTable;
				
			if exists (select 1 from @intersectToItemTempTable)
			begin
				-- udpate any items in diagram to have right ids
				update @intersectToItemTempTable set sourceobjectid = @currentObjectID, sourceobject = @currentObjectType where sourceobjectid = @objectid and sourceobject = @object;
				update @intersectToItemTempTable set targetobjectid = @currentObjectID, targetobject = @currentObjectType where targetobjectid = @objectid and targetobject = @object;
			end

			if exists (select 1 from @intersectToItemNotInDiagramTable)
			begin
				insert into @intersectToItemNotInDiagramTempTable select * from @intersectToItemNotInDiagramTable;

				-- update any items in referenced in diagram to have right ids
				update @intersectToItemNotInDiagramTempTable set targetobjectid = @currentObjectID, targetobject = @currentObjectType where targetobjectid = @objectid and targetobject = @object;
				
				--add relations that dont need map records
				insert into @intersectToItemTempTable
					select
						IntersectTypeID,
						-1,
						-1,
						SourceObject,
						SourceObjectID,
						SourceIntersectTypeNodeID,
						TargetObject,
						TargetObjectID,
						TargetIntersectTypeNodeID,
						-1,
						-1,
						0
					from @intersectToItemNotInDiagramTempTable
				
				--debug print out what we are gonna add
				--select * from @intersectToItemTempTable
			end
						
			-- delete relations that already exist for the item from what we are about to insert												
			delete w
				from @intersectToItemTempTable w
				inner join intersectnode inode1 on(w.sourceobject = inode1.objecttype and w.sourceobjectid = inode1.objectid)
				inner join intersectnode inode2 on(inode1.intersectid = inode2.intersectid and inode2.objectid = @currentObjectID and inode2.objecttype = @currentObjectType)
							
			-- call proce to add the relations for this item
			exec [utility].[AddRelationDiagramRelations] @intersectToItemTempTable, @NumberOfIntersectsAdded, @NumberOfObjectsUpdated
					
			--next item
			select @currentItemToUpdate = @currentItemToUpdate +1;
		end	-- end of this target item

		select @currentTemplateID = @currentTemplateID +1;

	end -- end of templates loop

	-- log finish
	update [dbo].[IntersectMapTemplateLogSummary]
	set DateCompleted = CURRENT_TIMESTAMP, 
		[NumberOfTemplatesProcessed] = @maxTemplateID, 
		[NumberOfObjectsUpdated] = @NumberOfObjectsUpdated,
		[NumberOfObjectsConsidered] = @NumberOfObjectsConsidered,
		[NumberOfIntersectsAdded] = @NumberOfIntersectsAdded	
	where ID = @ExecutionID;
END

