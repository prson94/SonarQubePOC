CREATE procedure [bulkload].[BusinessLineage]
--declare
	@id int
--set @id = 237
as
begin
	set nocount on;

	declare @r int,
			@dt datetime = getutcdate(),
			@ActionColumn int = 1,
			@SourceIntersectTypeColumn int = 2,
			@SourceSubjectSubjectAreaColumn int = 3,
			@SourceSubjectColumn int = 4,
			@SourceObjectSubjectAreaColumn int = 5,
			@SourceObjectColumn int = 6,
			@SourceFusionConfigColumn int = 7,
			@SourceFusionAttributeColumn int = 8,
			@TargetIntersectTypeColumn int = 9,
			@TargetSubjectSubjectAreaColumn int = 10,
			@TargetSubjectColumn int = 11,
			@TargetObjectSubjectAreaColumn int = 12,
			@TargetObjectColumn int = 13,
			@TargetFusionConfigColumn int = 14,
			@TargetFusionAttributeColumn int = 15,
			@TransformationColumn int = 16,
			@RoleColumn int = 17

	select	@r = UpdatedBy from [Load] where ID = @id

	--Set the default Action to Add if blank or NULL.
	update	LoadItemColumn
	set		Value = 'Add'
	where	LoadID = @id and ColumnIndex = @ActionColumn and (Value is null or Value = '')

	exec bulkload.UpdateIntersectTypeColumn @id, @SourceIntersectTypeColumn																		-- source intersect type
	exec bulkload.UpdateIntersectTypeColumn @id, @TargetIntersectTypeColumn																		-- target intersect type

	exec bulkload.UpdateSubjectAreaColumn @id, @SourceSubjectSubjectAreaColumn																	-- source subject subject area
	exec bulkload.UpdateSubjectAreaColumn @id, @SourceObjectSubjectAreaColumn																	-- source object subject area
	exec bulkload.UpdateSubjectAreaColumn @id, @TargetSubjectSubjectAreaColumn																	-- target subject subject area
	exec bulkload.UpdateSubjectAreaColumn @id, @TargetObjectSubjectAreaColumn																	-- target object subject area

	exec bulkload.UpdateItemColumnByIntersectType @id, @SourceIntersectTypeColumn, 1, @SourceSubjectSubjectAreaColumn, @SourceSubjectColumn		-- source subject
	exec bulkload.UpdateItemColumnByIntersectType @id, @SourceIntersectTypeColumn, 0, @SourceObjectSubjectAreaColumn, @SourceObjectColumn		-- source object
	exec bulkload.UpdateItemColumnByIntersectType @id, @TargetIntersectTypeColumn, 1, @TargetSubjectSubjectAreaColumn, @TargetSubjectColumn		-- target subject
	exec bulkload.UpdateItemColumnByIntersectType @id, @TargetIntersectTypeColumn, 0, @TargetObjectSubjectAreaColumn, @TargetObjectColumn		-- target object

	exec bulkload.UpdateFusionConfigurationColumn @id, @SourceFusionConfigColumn																-- source fusion config
	exec bulkload.UpdateFusionConfigurationColumn @id, @TargetFusionConfigColumn																-- target fusion config

	exec bulkload.UpdateFusionAttributeColumn @id, @SourceFusionConfigColumn, @SourceFusionAttributeColumn										-- source fusion attribute
	exec bulkload.UpdateFusionAttributeColumn @id, @TargetFusionConfigColumn, @TargetFusionAttributeColumn										-- target fusion attribute

	exec bulkload.UpdateIntersectRoleColumn @id, @RoleColumn																					-- intersect role

	drop table if exists #RemoveItems
	drop table if exists #AddItems
--select * from #RemoveItems
	BEGIN TRANSACTION [Tran1]

	BEGIN TRY
		-- HANDLE THE REMOVEs

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

				MRI.ID as MapRuleItemID,

				cast(0 as bit) as Status,
				cast('' as nvarchar(500)) as StatusMessage,

				@r as ResourceID  --THE USER THAT ADDED THE LOAD
		into	#RemoveItems
		from	LoadItemColumn SS
				inner join LoadItemColumn SO	on SO.LoadID = SS.LoadID	and SO.RowIndex = SS.RowIndex 	and SS.ColumnIndex = @SourceSubjectColumn 	and SO.ColumnIndex = @SourceObjectColumn
				inner join LoadItemColumn SA	on SA.LoadID = SS.LoadID	and SA.RowIndex = SS.RowIndex 	and SA.ColumnIndex = @ActionColumn and SA.Value = 'Remove'
				inner join LoadItemColumn SIT	on SIT.LoadID = SS.LoadID	and SIT.RowIndex = SS.RowIndex 	and SIT.ColumnIndex = @SourceIntersectTypeColumn
				left join [Intersect] SI		on SIT.LookupObject = 'IntersectType' and SI.IntersectTypeID = SIT.LookupObjectID 
												and SI.Subject = SS.LookupObject and SI.SubjectID = SS.LookupObjectID 
												and SI.Object = SO.LookupObject and SI.ObjectID = SO.LookupObjectID

				inner join LoadItemColumn TS 	on TS.LoadID = SS.LoadID 	and TS.RowIndex = SS.RowIndex	and TS.ColumnIndex = @TargetSubjectColumn
				inner join LoadItemColumn [TO]	on [TO].LoadID = SS.LoadID	and [TO].RowIndex = SS.RowIndex	and [TO].ColumnIndex = @TargetObjectColumn
				inner join LoadItemColumn TIT	on TIT.LoadID = SS.LoadID	and TIT.RowIndex = SS.RowIndex 	and TIT.ColumnIndex = @TargetIntersectTypeColumn
				left join [Intersect] TI		on TIT.LookupObject = 'IntersectType' and TI.IntersectTypeID = TIT.LookupObjectID 
												and TI.Subject = TS.LookupObject and TI.SubjectID = TS.LookupObjectID 
												and TI.Object = [TO].LookupObject and TI.ObjectID = [TO].LookupObjectID

				left join MapItem M				on M.SourceIntersectID = SI.ID and M.TargetIntersectID = TI.ID

				left join LoadItemColumn SFA	on SFA.LoadID = SS.LoadID	and SFA.RowIndex = SS.RowIndex 	and SFA.ColumnIndex = @SourceFusionAttributeColumn
				left join LoadItemColumn TFA	on TFA.LoadID = SS.LoadID	and TFA.RowIndex = SS.RowIndex 	and TFA.ColumnIndex = @TargetFusionAttributeColumn
				left join MapRuleItem MRI		on	SFA.LookupObject = 'FusionAttribute' and MRI.SourceFusionAttributeID = SFA.LookupObjectID and
													TFA.LookupObject = 'FusionAttribute' and MRI.TargetFusionAttributeID = TFA.LookupObjectID

		where	SS.LoadID = @id


		-- Add indexes to temp table
		CREATE NONCLUSTERED INDEX [IX_TempRemoveItems_MapItem] ON #RemoveItems ( MapItemID ASC )
		CREATE NONCLUSTERED INDEX [IX_TempRemoveItems_MapRuleItem] ON #RemoveItems ( MapRuleItemID ASC )
		CREATE NONCLUSTERED INDEX [IX_TempRemoveItems_SourceIntersect] ON #RemoveItems ( SourceIntersectID ASC )
		CREATE NONCLUSTERED INDEX [IX_TempRemoveItems_TargetIntersect] ON #RemoveItems ( TargetIntersectID ASC )

		/*	BEGIN: REMOVE TECHNICAL MAPPINGS THAT ARE TIED TO FOUND MAP ITEMS */
		declare @mapRuleItems table(MapRuleItemID int, MapRuleID int)
		insert into @mapRuleItems
			select	T.MapRuleItemID,
					TJ.MapRuleID
			from	MapRuleItemMapItem T
					inner join #RemoveItems S on S.MapItemID = T.MapItemID
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

		/*	BEGIN: REMOVE TECHNICAL MAPPING OPTIONALLY SPECIFIED IF NOT TIED ANYWHERE ELSE */
		declare @mapRuleItemIDs table(MapRuleItemID int)
		insert into @mapRuleItemIDs
			select	S.MapRuleItemID
			from	#RemoveItems S
					left join MapRuleItemMapItem J on J.MapRuleItemID = S.MapRuleItemID
			where	S.MapRuleItemID is not null;

		delete	T
		from	MapRuleItem T
				inner join @mapRuleItemIDs S on S.MapRuleItemID = T.ID;

		/*	END: REMOVE TECHNICAL MAPPING OPTIONALLY SPECIFIED IF NOT TIED ANYWHERE ELSE */

		/*	BEGIN: MAPPINGS FOUND MAP ITEMS */
		declare @mapItems table(MapItemID int, MapID int)
		insert into @mapItems
			select	S.MapItemID,
					J.MapID
			from	#RemoveItems S
					left join MapItemMap J on J.MapItemID = S.MapItemID;

		delete	T
		from	MapItemMap T
				inner join @mapItems S on S.MapItemID = T.MapItemID;

		delete	T
		from	MapSequence T
				inner join @mapItems S on S.MapItemID = T.MapItemID;

		delete	T
		from	MapItem T
				inner join @mapItems S on S.MapItemID = T.ID;

		delete	T
		from	MapRule T
				inner join @mapRuleItems S on S.MapRuleID = T.ID
				left join MapRuleItemMapRule NTJ on NTJ.MapRuleID = S.MapRuleID and NTJ.MapRuleItemID <> S.MapRuleItemID	--get all map rules that are used only once.
		where	NTJ.MapRuleID is null;
		/*	END: REMOVE FOUND MAP ITEMS */

		/*	BEGIN: REMOVE SOURCE AND TARGET INTERSECTS THAT ARE NOT REFERENCED ANYWHERE ELSE */
		delete	T
		from	[Intersect] T
				inner join #RemoveItems S on (S.SourceIntersectID = T.ID or S.TargetIntersectID = T.ID)
				left join IntersectGroup CG on CG.IntersectID = T.ID
				left join MapItem CSM on CSM.SourceIntersectID = T.ID
				left join MapItem CTM on CTM.TargetIntersectID = T.ID
				left join [Intersect] CI on (CI.Subject = 'Intersect' and CI.SubjectID = T.ID) or (CI.Object = 'Intersect' and CI.ObjectID = T.ID)
		where	CG.ID is null and
				CSM.ID is null and 
				CTM.ID is null and
				CI.ID is null;
		/*	BEGIN: REMOVE SOURCE INTERSECTS THAT ARE NOT REFERENCED ANYWHERE ELSE */

		-- update status & status message for Items table
		
		-- SUCCESS STATUS
		update	T
		set		T.Status = 1,
				T.StatusMessage = coalesce(T.StatusMessage,'') + 'Business map removed. '
		from	#RemoveItems T
				left join MapItem S on S.ID = T.MapItemID
		where	T.MapItemID is not null and S.ID is null;

		update	T
		set		T.StatusMessage = coalesce(T.StatusMessage,'') + 'Source relationship removed. '
		from	#RemoveItems T
				left join [Intersect] S on S.ID = T.SourceIntersectID
		where	T.SourceIntersectID is not null and S.ID is null;

		update	T
		set		T.StatusMessage = coalesce(T.StatusMessage,'') + 'Target relationship removed. '
		from	#RemoveItems T
				left join [Intersect] S on S.ID = T.TargetIntersectID
		where	T.TargetIntersectID is not null and S.ID is null;

		-- FAILED STATUS
		update	T
		set		T.Status = 0,
				T.StatusMessage = coalesce(T.StatusMessage,'') + 'Could not find source relationship. '
		from	#RemoveItems T
		where	SourceIntersectID is null;

		update	T
		set		T.Status = 0,
				T.StatusMessage = coalesce(T.StatusMessage,'') + 'Could not find target relationship. '
		from	#RemoveItems T
		where	TargetIntersectID is null;

		update	T
		set		T.Status = 0,
				T.StatusMessage = coalesce(T.StatusMessage,'') + 'Could not find business map. '
		from	#RemoveItems T
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
				inner join #RemoveItems S on T.LoadID = @id and S.RowIndex = T.RowIndex;



		-- NOW HANDLE THE ADDs ---------------------------------------------------------------------------

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

				SFA.LookupObjectID as SourceFusionAttributeID,
				SFA.Value as SourceFusionAttributeRaw,
				TFA.LookupObjectID as TargetFusionAttributeID,
				TFA.Value as TargetFusionAttributeRaw,

				SI.ID as SourceIntersectID,
				TI.ID as TargetIntersectID,
				M.ID as MapItemID,
				MRI.ID as MapRuleItemID,

				SIFT.ID as SourceFusionIntersectTypeID,
				TIFT.ID as TargetFusionIntersectTypeID,
				SIF.ID as SourceFusionIntersectID,
				TIF.ID as TargetFusionIntersectID,

				cast(null as bit) as Status,
				cast('' as nvarchar(500)) as StatusMessage,

				@r as ResourceID  --THE USER THAT ADDED THE LOAD
		into	#AddItems
		from	LoadItemColumn SS
				inner join LoadItemColumn SO	on SO.LoadID = SS.LoadID	and SO.RowIndex = SS.RowIndex 	and SS.ColumnIndex = @SourceSubjectColumn 	and SO.ColumnIndex = @SourceObjectColumn
				inner join LoadItemColumn SA	on SA.LoadID = SS.LoadID	and SA.RowIndex = SS.RowIndex 	and SA.ColumnIndex = @ActionColumn and SA.Value = 'Add'
				inner join LoadItemColumn SIT	on SIT.LoadID = SS.LoadID	and SIT.RowIndex = SS.RowIndex 	and SIT.ColumnIndex = @SourceIntersectTypeColumn
				left join [Intersect] SI		on SIT.LookupObject = 'IntersectType' and SI.IntersectTypeID = SIT.LookupObjectID 
												and SI.Subject = SS.LookupObject and SI.SubjectID = SS.LookupObjectID 
												and SI.Object = SO.LookupObject and SI.ObjectID = SO.LookupObjectID

				inner join LoadItemColumn TS 	on TS.LoadID = SS.LoadID 	and TS.RowIndex = SS.RowIndex	and TS.ColumnIndex = @TargetSubjectColumn
				inner join LoadItemColumn [TO]	on [TO].LoadID = SS.LoadID	and [TO].RowIndex = SS.RowIndex	and [TO].ColumnIndex = @TargetObjectColumn
				inner join LoadItemColumn TIT	on TIT.LoadID = SS.LoadID	and TIT.RowIndex = SS.RowIndex 	and TIT.ColumnIndex = @TargetIntersectTypeColumn
				left join [Intersect] TI		on TIT.LookupObject = 'IntersectType' and TI.IntersectTypeID = TIT.LookupObjectID 
												and TI.Subject = TS.LookupObject and TI.SubjectID = TS.LookupObjectID 
												and TI.Object = [TO].LookupObject and TI.ObjectID = [TO].LookupObjectID

				left join MapItem M				on M.SourceIntersectID = SI.ID and M.TargetIntersectID = TI.ID

				left join LoadItemColumn SFA	on SFA.LoadID = SS.LoadID	and SFA.RowIndex = SS.RowIndex 	and SFA.ColumnIndex = @SourceFusionAttributeColumn
				left join LoadItemColumn TFA	on TFA.LoadID = SS.LoadID	and TFA.RowIndex = SS.RowIndex 	and TFA.ColumnIndex = @TargetFusionAttributeColumn

				left join MapRuleItem MRI		on	SFA.LookupObject = 'FusionAttribute' and MRI.SourceFusionAttributeID = SFA.LookupObjectID and
													TFA.LookupObject = 'FusionAttribute' and MRI.TargetFusionAttributeID = TFA.LookupObjectID

				left join FusionAttribute SFAO	on SFA.LookupObject = 'FusionAttribute' and SFAO.ID = SFA.LookupObjectID 
				outer apply (
						SELECT  MIN(ID) as ID
						FROM    IntersectType
						WHERE   Subject = 'IntersectType' and SubjectID = SIT.LookupObjectID and Object = 'FusionAttributeType' and ObjectID = SFAO.FusionAttributeTypeID
				) SIFT
				left join [Intersect] SIF		on	SIF.IntersectTypeID = SIFT.ID 
													and SIF.Subject = 'Intersect' and SIF.SubjectID = SI.ID
													and SIF.Object = SFA.LookupObject and SIF.ObjectID = SFA.LookupObjectID

				left join FusionAttribute TFAO	on TFA.LookupObject = 'FusionAttribute' and TFAO.ID = TFA.LookupObjectID 
				outer apply (
						SELECT  MIN(ID) as ID
						FROM    IntersectType
						WHERE   Subject = 'IntersectType' and SubjectID = TIT.LookupObjectID and Object = 'FusionAttributeType' and ObjectID = TFAO.FusionAttributeTypeID
				) TIFT
				left join [Intersect] TIF		on	TIF.IntersectTypeID = TIFT.ID 
													and TIF.Subject = 'Intersect' and TIF.SubjectID = TI.ID
													and TIF.Object = TFA.LookupObject and TIF.ObjectID = TFA.LookupObjectID

		where	SS.LoadID = @id

		-- Add indexes to temp table
		CREATE NONCLUSTERED INDEX [IX_SourceBusinessIntersect] ON #AddItems ( SourceIntersectTypeID ASC, SourceSubject ASC, SourceSubjectID ASC, SourceObject ASC, SourceObjectID ASC )
/*
update LoadItemColumn set Value = 'Bloomberg LP/Back Office Data License' where LoadID =  270 and RowIndex = 2 and ColumnIndex = 4
select * from LoadItemColumn where LoadID = 270
select * from #AddItems
select * from LoadItem where LoadID = 270

select I.LoadID, I.RowIndex, case I.[Status] when 1 then 'Complete' when 0 then 'Failed' else 'Queued' end as [Status], I.StatusMessage
from LoadItem I
where I.LoadID = 270
order by I.RowIndex
*/
		-- ERROR OUT THE ROWS THAT DO NOT HAVE THE APPROPRIATE FUSION INTERSECT TYPE IDs.
		update	#AddItems
		set		Status = 0,
				StatusMessage = coalesce(StatusMessage,'') +
								IIF(SourceFusionIntersectTypeID is null, 'Could not find source fusion relationship type. ', '') + 
								IIF(SourceFusionAttributeID is null, 'Could not find source fusion path. ', '') + 
								IIF(TargetFusionIntersectTypeID is null, 'Could not find target fusion relationship type. ', '') + 
								IIF(TargetFusionAttributeID is null, 'Could not find target fusion path. ', '')
		where	(SourceFusionAttributeRaw is not null and SourceFusionIntersectTypeID is null) OR (TargetFusionAttributeRaw is not null and TargetFusionIntersectTypeID is null);

		-- ERROR OUT THE ROWS THAT DO NOT HAVE THE APPROPRIATE SOURCEs.
		update	#AddItems
		set		Status = 0,
				StatusMessage = coalesce(StatusMessage,'') +
								IIF(SourceSubjectID is null, 'Could not find source subject. ', '') + 
								IIF(SourceObjectID is null, 'Could not find source object. ', '')
		where	(SourceSubjectID is null) OR (SourceObjectID is null);

		-- ERROR OUT THE ROWS THAT DO NOT HAVE THE APPROPRIATE TARGETs.
		update	#AddItems
		set		Status = 0,
				StatusMessage = coalesce(StatusMessage,'') +
								IIF(TargetSubjectID is null, 'Could not find target subject. ', '') + 
								IIF(TargetObjectID is null, 'Could not find target object. ', '')
		where	(TargetSubjectID is null) OR (TargetObjectID is null);




		/*	BEGIN: SOURCE BUSINESS INTERSECT LOGIC */

		-- insert source business relationships
		insert into [Intersect] (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
			select	SourceIntersectTypeID, 
					SourceSubject, SourceSubjectID, 
					SourceObject, SourceObjectID,
					0, ResourceID, @dt, ResourceID, @dt
			from	(
					select		SourceIntersectTypeID, SourceSubject, SourceSubjectID, SourceObject, SourceObjectID, ResourceID
					from		#AddItems
					where		Status is null 
								and SourceIntersectID is null
					group by	SourceIntersectTypeID, SourceSubject, SourceSubjectID, SourceObject, SourceObjectID, ResourceID
					) O


		-- update rows with existing source business intersect
		update	T
		set		T.SourceIntersectID = S.ID,
				T.StatusMessage = coalesce(T.StatusMessage,'') + ' Source business relationship created.'
		from	#AddItems T
				inner join [Intersect] S on S.IntersectTypeID = T.SourceIntersectTypeID 
											and T.SourceSubject = S.Subject and T.SourceSubjectID = S.SubjectID 
											and T.SourceObject = S.Object and T.SourceObjectID = S.ObjectID
											and T.SourceIntersectID is null
											and T.Status is null;
		
		-- update rows with existing target business intersect
		update	T
		set		T.TargetIntersectID = S.ID
		from	#AddItems T
				inner join [Intersect] S on S.IntersectTypeID = T.TargetIntersectTypeID 
											and T.TargetSubject = S.Subject and T.TargetSubjectID = S.SubjectID 
											and T.TargetObject = S.Object and T.TargetObjectID = S.ObjectID
											and T.TargetIntersectID is null
											and T.Status is null;

		/*	END: SOURCE BUSINESS INTERSECT LOGIC */


		/*	BEGIN: TARGET BUSINESS INTERSECT LOGIC */

		-- insert target business relationships
		insert into [Intersect] (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
			select	TargetIntersectTypeID, 
					TargetSubject, TargetSubjectID, 
					TargetObject, TargetObjectID,
					0, ResourceID, @dt, ResourceID, @dt
			from	(
					select		TargetIntersectTypeID, TargetSubject, TargetSubjectID, TargetObject, TargetObjectID, ResourceID
					from		#AddItems
					where		Status is null 
								and TargetIntersectID is null
					group by	TargetIntersectTypeID, TargetSubject, TargetSubjectID, TargetObject, TargetObjectID, ResourceID
					) O

		-- update rows with existing target business intersect
		update	T
		set		T.TargetIntersectID = S.ID,
				T.StatusMessage = coalesce(T.StatusMessage,'') + ' Target business relationship created.'
		from	#AddItems T
				inner join [Intersect] S on S.IntersectTypeID = T.TargetIntersectTypeID 
											and T.TargetSubject = S.Subject and T.TargetSubjectID = S.SubjectID 
											and T.TargetObject = S.Object and T.TargetObjectID = S.ObjectID
											and T.TargetIntersectID is null
											and T.Status is null;

		/*	END: TARGET BUSINESS INTERSECT LOGIC */


		/*	BEGIN: SOURCE TECHNICAL INTERSECT LOGIC */

		-- insert source technical relationships
		insert into [Intersect] (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
			select	SourceFusionIntersectTypeID, 
					'Intersect', SourceIntersectID, 'FusionAttribute', SourceFusionAttributeID,
					0, ResourceID, @dt, ResourceID, @dt
			from	(
					select		SourceFusionIntersectTypeID, SourceIntersectID, SourceFusionAttributeID, ResourceID
					from		#AddItems
					where		Status is null
								and SourceFusionIntersectTypeID is not null
								and SourceFusionIntersectID is null
								and SourceIntersectID is not null
								and SourceFusionAttributeID is not null
					group by	SourceFusionIntersectTypeID, SourceIntersectID, SourceFusionAttributeID, ResourceID
					) O;

		-- update rows with new source technical intersect
		update	T
		set		T.SourceFusionIntersectID = S.ID,
				T.StatusMessage = coalesce(T.StatusMessage,'') + ' Source technical relationship created.'
		from	#AddItems T
				inner join [Intersect] S on S.IntersectTypeID = T.SourceFusionIntersectTypeID 
											and S.Subject = 'Intersect' and S.SubjectID = T.SourceIntersectID 
											and S.Object = 'FusionAttribute' and S.ObjectID = T.SourceFusionAttributeID
											and T.SourceFusionIntersectID is null 
											and T.Status is null;

		-- update rows with new target technical intersect
		update	T
		set		T.TargetFusionIntersectID = S.ID
		from	#AddItems T
				inner join [Intersect] S on S.IntersectTypeID = T.TargetFusionIntersectTypeID 
											and S.Subject = 'Intersect' and S.SubjectID = T.TargetIntersectID 
											and S.Object = 'FusionAttribute' and S.ObjectID = T.TargetFusionAttributeID
											and T.TargetFusionIntersectID is null 
											and T.Status is null;

		/*	END: SOURCE TECHNICAL INTERSECT LOGIC */


		/*	BEGIN: TARGET TECHNICAL INTERSECT LOGIC */
		
		-- insert target technical relationships
		insert into [Intersect] (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
			select	TargetFusionIntersectTypeID, 
					'Intersect', TargetIntersectID, 'FusionAttribute', TargetFusionAttributeID,
					0, ResourceID, @dt, ResourceID, @dt
			from	(
					select		TargetFusionIntersectTypeID, TargetIntersectID, TargetFusionAttributeID, ResourceID
					from		#AddItems
					where		Status is null
								and TargetFusionIntersectTypeID is not null
								and TargetFusionIntersectID is null
								and TargetIntersectID is not null
								and TargetFusionAttributeID is not null			
					group by	TargetFusionIntersectTypeID, TargetIntersectID, TargetFusionAttributeID, ResourceID
					) O;

		-- update rows with new target technical intersect
		update	T
		set		T.TargetFusionIntersectID = S.ID,
				T.StatusMessage = coalesce(T.StatusMessage,'') + ' Target technical relationship created.'
		from	#AddItems T
				inner join [Intersect] S on S.IntersectTypeID = T.TargetFusionIntersectTypeID 
											and S.Subject = 'Intersect' and S.SubjectID = T.TargetIntersectID 
											and S.Object = 'FusionAttribute' and S.ObjectID = T.TargetFusionAttributeID
											and T.TargetFusionIntersectID is null 
											and T.Status is null;

		/*	END: TARGET TECHNICAL INTERSECT LOGIC */

		-- insert new map items
		insert into MapItem (SourceIntersectID, TargetIntersectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
			select	distinct
					SourceIntersectID, 
					TargetIntersectID,
					ResourceID,
					@dt, 
					ResourceID,
					@dt
			from	#AddItems
			where	SourceIntersectID is not null 
					and TargetIntersectID is not null 
					and MapItemID is null
					and Status is null;

		-- update source data with newly created map item IDs
		update	T
		set		T.MapItemID = S.ID,
				T.StatusMessage = coalesce(T.StatusMessage,'') + ' Business map created.'
		from	#AddItems T
				inner join [MapItem] S on	S.SourceIntersectID = T.SourceIntersectID 
											and S.TargetIntersectID = T.TargetIntersectID 
											and T.MapItemID is null 
											and T.Status is null;

		-- insert new map rule items
		insert into MapRuleItem (SourceFusionAttributeID, TargetFusionAttributeID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
			select	distinct
					SourceFusionAttributeID, 
					TargetFusionAttributeID,
					ResourceID,
					@dt, 
					ResourceID,
					@dt
			from	#AddItems
			where	SourceIntersectID is not null 
					and TargetIntersectID is not null
					and SourceFusionAttributeID is not null 
					and TargetFusionAttributeID is not null
					and Status is null;

		-- update source data with newly created map rule item IDs
		update	T
		set		T.MapRuleItemID = S.ID,
				T.StatusMessage = coalesce(T.StatusMessage,'') + ' Technical map created.'
		from	#AddItems T
				inner join [MapRuleItem] S on	S.SourceFusionAttributeID = T.SourceFusionAttributeID 
												and S.TargetFusionAttributeID = T.TargetFusionAttributeID 
												and T.MapRuleItemID is null 
												and Status is null;

		-- MERGE MapRuleItemMapItem with all the IDs above
		merge	MapRuleItemMapItem as T
		using	(
				select		MapItemID, 
							MapRuleItemID
				from		#AddItems
				where		MapItemID is not null
							and MapRuleItemID is not null
				group by	MapItemID, 
							MapRuleItemID
				) as S
		on		T.MapRuleItemID = S.MapRuleItemID and T.MapItemID = S.MapItemID
		when	not matched by target then
				insert (MapRuleItemID, MapItemID)
				values (S.MapRuleItemID, S.MapItemID);

		
		-- CALCULATE STATUS BASED ON POPULATED IDs
		update	#AddItems
		set		Status = 1
		where	MapItemID is not null 
				and (
					(SourceFusionAttributeRaw is not null and TargetFusionAttributeRaw is not null and MapRuleItemID is not null) 
					or 
					(SourceFusionAttributeRaw is null and TargetFusionAttributeRaw is null)
				);

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
				inner join #AddItems S on T.LoadID = @id and S.RowIndex = T.RowIndex;


--select *,  case [Status] when 1 then 'Complete' when 0 then 'Failed' else 'Queued' end as [Status] from LoadItem where LoadID = 270

		-- NOW, Close out the Load job ----------------------------------------------------------------------------------
		update	LoadItem
		set		Status = cast(0 as bit),
				StatusMessage = 'Incomplete : ' + coalesce(StatusMessage,''),
				Object = null,
				ObjectID = null
		where	LoadID = @id and Status is null;

		update	[Load]
		set		DateCompleted = getutcdate()
		where	ID = @id;

		COMMIT TRANSACTION [Tran1]
	END TRY
	BEGIN CATCH
		ROLLBACK TRANSACTION [Tran1]
		select ERROR_MESSAGE()
		update	[Load]
		set		Notes = Notes + '<br/> ' + ERROR_MESSAGE()
		where	ID = @id;
	END CATCH
end
GO

