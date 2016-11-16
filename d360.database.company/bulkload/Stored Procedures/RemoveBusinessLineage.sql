create procedure [bulkload].[RemoveBusinessLineage]
--declare
	@id int
--set @id = 264
as
begin
	set nocount on;

	declare @r int,
			@dt datetime = getutcdate()

	select	@r = UpdatedBy from [Load] where ID = @id

	exec bulkload.UpdateIntersectTypeColumn @id, 1					-- source intersect type
	exec bulkload.UpdateIntersectTypeColumn @id, 6					-- target intersect type

	exec bulkload.UpdateSubjectAreaColumn @id, 2					-- source subject subject area
	exec bulkload.UpdateSubjectAreaColumn @id, 4					-- source object subject area
	exec bulkload.UpdateSubjectAreaColumn @id, 7					-- target subject subject area
	exec bulkload.UpdateSubjectAreaColumn @id, 9					-- target object subject area

	exec bulkload.UpdateItemColumnByIntersectType @id, 1, 1, 2, 3	-- source subject
	exec bulkload.UpdateItemColumnByIntersectType @id, 1, 0, 4, 5	-- source object
	exec bulkload.UpdateItemColumnByIntersectType @id, 6, 1, 7, 8	-- target subject
	exec bulkload.UpdateItemColumnByIntersectType @id, 6, 0, 9, 10	-- target object

	drop table if exists #Items

	BEGIN TRANSACTION [Tran1]

	BEGIN TRY
		-- Load Temp table that we are going to work from
		select	SS.RowIndex,
		
				SIT.LookupObjectID as SourceIntersectTypeID,
				SS.LookupObject as SourceSubject,
				SS.LookupObjectID as SourceSubjectID,
				SO.LookupObject as SourceObject,
				SO.LookupObjectID as SourceObjectID,

				TIT.LookupObjectID as TargetIntersectTypeID,
				TS.LookupObject as TargetSubject,
				TS.LookupObjectID as TargetSubjectID,
				[TO].LookupObject as TargetObject,
				[TO].LookupObjectID as TargetObjectID,

				SI.ID as SourceIntersectID,
				TI.ID as TargetIntersectID,
				M.ID as MapItemID,

				cast(0 as bit) as Status,
				cast('' as nvarchar(500)) as StatusMessage,

				@r as ResourceID  --THE USER THAT ADDED THE LOAD
		into	#Items
		from	LoadItemColumn SS
				inner join LoadItemColumn SO	on SO.LoadID = SS.LoadID	and SO.RowIndex = SS.RowIndex 	and SS.ColumnIndex = 3 	and SO.ColumnIndex = 5
				inner join LoadItemColumn SIT	on SIT.LoadID = SS.LoadID	and SIT.RowIndex = SS.RowIndex 	and SIT.ColumnIndex = 1
				left join [Intersect] SI		on SIT.LookupObject = 'IntersectType' and SI.IntersectTypeID = SIT.LookupObjectID 
												and SI.Subject = SS.LookupObject and SI.SubjectID = SS.LookupObjectID 
												and SI.Object = SO.LookupObject and SI.ObjectID = SO.LookupObjectID

				inner join LoadItemColumn TS 	on TS.LoadID = SS.LoadID 	and TS.RowIndex = SS.RowIndex	and TS.ColumnIndex = 8
				inner join LoadItemColumn [TO]	on [TO].LoadID = SS.LoadID	and [TO].RowIndex = SS.RowIndex	and [TO].ColumnIndex = 10
				inner join LoadItemColumn TIT	on TIT.LoadID = SS.LoadID	and TIT.RowIndex = SS.RowIndex 	and TIT.ColumnIndex = 6
				left join [Intersect] TI		on TIT.LookupObject = 'IntersectType' and TI.IntersectTypeID = TIT.LookupObjectID 
												and TI.Subject = TS.LookupObject and TI.SubjectID = TS.LookupObjectID 
												and TI.Object = [TO].LookupObject and TI.ObjectID = [TO].LookupObjectID

				left join MapItem M				on M.SourceIntersectID = SI.ID and M.TargetIntersectID = TI.ID
		where	SS.LoadID = @id

		-- Add indexes to temp table
		CREATE NONCLUSTERED INDEX [IX_TempItems_MapItem] ON #Items ( MapItemID ASC )		
		CREATE NONCLUSTERED INDEX [IX_TempItems_SourceIntersect] ON #Items ( SourceIntersectID ASC )
		CREATE NONCLUSTERED INDEX [IX_TempItems_TargetIntersect] ON #Items ( TargetIntersectID ASC )

		/*	BEGIN: REMOVE TECHNICAL MAPPINGS THAT ARE TIED TO FOUND MAP ITEMS */
		declare @mapRuleItems table(MapRuleItemID int, MapRuleID int)
		insert into @mapRuleItems
			select	T.MapRuleItemID,
					TJ.MapRuleID
			from	MapRuleItemMapItem T
					inner join #Items S on S.MapItemID = T.MapItemID
					left join MapRuleItemMapRule TJ on TJ.MapRuleItemID = T.MapRuleItemID

		delete	T
		from	MapRuleItemMapItem T
				inner join @mapRuleItems S on S.MapRuleItemID = T.MapRuleItemID

		delete	T
		from	MapRuleItemMapRule T
				inner join @mapRuleItems S on S.MapRuleItemID = T.MapRuleItemID

		delete	T
		from	MapRule T
				inner join @mapRuleItems S on S.MapRuleID = T.ID
				left join MapRuleItemMapRule NTJ on NTJ.MapRuleID = S.MapRuleID and NTJ.MapRuleItemID <> S.MapRuleItemID	--get all map rules that are used only once.
		where	NTJ.MapRuleID is null
		/*	END: REMOVE TECHNICAL MAPPINGS THAT ARE TIED TO FOUND MAP ITEMS */

		/*	BEGIN: MAPPINGS FOUND MAP ITEMS */
		declare @mapItems table(MapItemID int, MapID int)
		insert into @mapItems
			select	S.MapItemID,
					J.MapID
			from	#Items S
					left join MapItemMap J on J.MapItemID = S.MapItemID

		delete	T
		from	MapItemMap T
				inner join @mapItems S on S.MapItemID = T.MapItemID

		delete	T
		from	MapSequence T
				inner join @mapItems S on S.MapItemID = T.MapItemID

		delete	T
		from	MapItem T
				inner join @mapItems S on S.MapItemID = T.ID

		delete	T
		from	MapRule T
				inner join @mapRuleItems S on S.MapRuleID = T.ID
				left join MapRuleItemMapRule NTJ on NTJ.MapRuleID = S.MapRuleID and NTJ.MapRuleItemID <> S.MapRuleItemID	--get all map rules that are used only once.
		where	NTJ.MapRuleID is null
		/*	END: REMOVE FOUND MAP ITEMS */

		/*	BEGIN: REMOVE SOURCE AND TARGET INTERSECTS THAT ARE NOT REFERENCED ANYWHERE ELSE */
		delete	T
		from	[Intersect] T
				inner join #Items S on (S.SourceIntersectID = T.ID or S.TargetIntersectID = T.ID)
				left join IntersectGroup CG on CG.IntersectID = T.ID
				left join MapItem CSM on CSM.SourceIntersectID = T.ID
				left join MapItem CTM on CTM.TargetIntersectID = T.ID
				left join [Intersect] CI on (CI.Subject = 'Intersect' and CI.SubjectID = T.ID) or (CI.Object = 'Intersect' and CI.ObjectID = T.ID)
		where	CG.ID is null and
				CSM.ID is null and 
				CTM.ID is null and
				CI.ID is null
		/*	BEGIN: REMOVE SOURCE INTERSECTS THAT ARE NOT REFERENCED ANYWHERE ELSE */

		-- update status & status message for Items table
		
		-- SUCCESS STATUS
		update	T
		set		T.Status = 1,
				T.StatusMessage = coalesce(T.StatusMessage,'') + 'Business map removed. '
		from	#Items T
				left join MapItem S on S.ID = T.MapItemID
		where	T.MapItemID is not null and S.ID is null;

		update	T
		set		T.StatusMessage = coalesce(T.StatusMessage,'') + 'Source relationship removed. '
		from	#Items T
				left join [Intersect] S on S.ID = T.SourceIntersectID
		where	T.SourceIntersectID is not null and S.ID is null;

		update	T
		set		T.StatusMessage = coalesce(T.StatusMessage,'') + 'Target relationship removed. '
		from	#Items T
				left join [Intersect] S on S.ID = T.TargetIntersectID
		where	T.TargetIntersectID is not null and S.ID is null;

		-- FAILED STATUS
		update	T
		set		T.Status = 0,
				T.StatusMessage = coalesce(T.StatusMessage,'') + 'Could not find source relationship. '
		from	#Items T
		where	SourceIntersectID is null;

		update	T
		set		T.Status = 0,
				T.StatusMessage = coalesce(T.StatusMessage,'') + 'Could not find target relationship. '
		from	#Items T
		where	TargetIntersectID is null;

		update	T
		set		T.Status = 0,
				T.StatusMessage = coalesce(T.StatusMessage,'') + 'Could not find business map. '
		from	#Items T
		where	MapItemID is null;


		-- Now update LoadItems on original Load with status and messages created above
		update	T
		set		T.Status = S.Status,
				T.StatusMessage = S.StatusMessage,
				T.Object = case S.Status
							when 1 then 'MapItem'
							else NULL
						   end,
				T.ObjectID = case S.Status
							when 1 then S.MapItemID
							else NULL
						   end
		from	LoadItem T
				inner join #Items S on T.LoadID = @id and S.RowIndex = T.RowIndex;

		update	LoadItem
		set		Status = cast(0 as bit),
				StatusMessage = 'Incomplete',
				Object = null,
				ObjectID = null
		where	Status is null;

		-- Close out the Load job
		update	[Load]
		set		DateCompleted = getutcdate()
		where	ID = @id;

		COMMIT TRANSACTION [Tran1]
	END TRY
	BEGIN CATCH
		ROLLBACK TRANSACTION [Tran1]
	END CATCH
end