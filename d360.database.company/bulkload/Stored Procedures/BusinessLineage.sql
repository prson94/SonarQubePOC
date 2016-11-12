CREATE procedure [bulkload].[BusinessLineage]
--declare
	@id int
--set @id = 19
as
begin
	set nocount on;

	declare @r int,
			@dt datetime = getutcdate()

	select	@r = UpdatedBy from [Load] where ID = @id

	exec bulkload.UpdateTypeColumn @id, 1, 2				-- source subject type
	exec bulkload.UpdateTypeColumn @id, 5, 6				-- source object type
	exec bulkload.UpdateTypeColumn @id, 11, 12				-- target subject type
	exec bulkload.UpdateTypeColumn @id, 15, 16				-- target object type

	exec bulkload.UpdateSubjectAreaColumn @id, 3			-- source subject subject area
	exec bulkload.UpdateSubjectAreaColumn @id, 7			-- source object subject area
	exec bulkload.UpdateSubjectAreaColumn @id, 13			-- target subject subject area
	exec bulkload.UpdateSubjectAreaColumn @id, 17			-- target object subject area

	exec bulkload.UpdateItemColumn @id, 1, 2, 3, 4			-- source subject
	exec bulkload.UpdateItemColumn @id, 5, 6, 7, 8			-- source object
	exec bulkload.UpdateItemColumn @id, 11, 12, 13, 14		-- target subject
	exec bulkload.UpdateItemColumn @id, 15, 16, 17, 18		-- target object

	exec bulkload.UpdateFusionConfigurationColumn @id, 9	-- source fusion config
	exec bulkload.UpdateFusionConfigurationColumn @id, 19	-- target fusion config

	exec bulkload.UpdateFusionAttributeColumn @id, 9, 10	-- source fusion attribute
	exec bulkload.UpdateFusionAttributeColumn @id, 19, 20	-- target fusion attribute

	exec bulkload.UpdateIntersectRoleColumn @id, 22			-- intersect role

	drop table if exists #Items

	BEGIN TRANSACTION [Tran1]

	BEGIN TRY
		-- Load Temp table that we are going to work from
		select	SS.RowIndex,
		
				SIT.ID as SourceIntersectTypeID,
				SST.LookupObject as SourceSubjectType,
				SST.LookupObjectID as SourceSubjectTypeID,
				SS.LookupObject as SourceSubject,
				SS.LookupObjectID as SourceSubjectID,

				SOT.LookupObject as SourceObjectType,
				SOT.LookupObjectID as SourceObjectTypeID,
				SO.LookupObject as SourceObject,
				SO.LookupObjectID as SourceObjectID,

				SIFT.ID as SourceFusionIntersectTypeID,
				SF.LookupObject as SourceFusion,
				SF.Value as SourceFusionRaw,
				SF.LookupObjectID as SourceFusionID,

				TIT.ID as TargetIntersectTypeID,
				TST.LookupObject as TargetSubjectType,
				TST.LookupObjectID as TargetSubjectTypeID,
				TS.LookupObject as TargetSubject,
				TS.LookupObjectID as TargetSubjectID,

				TOT.LookupObject as TargetObjectType,
				TOT.LookupObjectID as TargetObjectTypeID,
				[TO].LookupObject as TargetObject,
				[TO].LookupObjectID as TargetObjectID,

				TIFT.ID as TargetFusionIntersectTypeID,
				TF.Value as TargetFusionRaw,
				TF.LookupObject as TargetFusion,
				TF.LookupObjectID as TargetFusionID,

				cast(0 as int) as SourceIntersectID,
				cast('' as char(1)) as SourceIntersectChangeType,
				cast(0 as int) as TargetIntersectID,
				cast('' as char(1)) as TargetIntersectChangeType,
				cast(0 as int) as SourceFusionIntersectID,
				cast('' as char(1)) as SourceFusionIntersectChangeType,
				cast(0 as int) as TargetFusionIntersectID,
				cast('' as char(1)) as TargetFusionIntersectChangeType,
				cast(0 as int) as MapItemID,
				cast('' as char(1)) as MapItemChangeType,
				cast(0 as int) as MapRuleItemID,
				cast('' as char(1)) as MapRuleItemChangeType,

				cast(0 as bit) as Status,
				cast('' as nvarchar(500)) as StatusMessage,

				@r as ResourceID  --THE USER THAT ADDED THE LOAD
		into	#Items
		from	LoadItemColumn SS
				inner join LoadItemColumn SST	on SST.LoadID = SS.LoadID	and SST.RowIndex = SS.RowIndex	and SST.ColumnIndex = 2
				inner join LoadItemColumn SO	on SO.LoadID = SS.LoadID	and SO.RowIndex = SS.RowIndex 	and SO.ColumnIndex = 8
				inner join LoadItemColumn SOT	on SOT.LoadID = SS.LoadID	and SOT.RowIndex = SS.RowIndex	and SOT.ColumnIndex = 6
				left join IntersectType SIT on SIT.Subject = SST.LookupObject and SIT.SubjectID = SST.LookupObjectID and SIT.Object = SOT.LookupObject and SIT.ObjectID = SOT.LookupObjectID

				left join LoadItemColumn SF	on SF.LoadID = SS.LoadID	    and SF.RowIndex = SS.RowIndex	and SF.ColumnIndex = 10
				left join FusionAttribute SFA on SFA.ID = SF.LookupObjectID
				left join IntersectType SIFT on SIFT.Subject = 'IntersectType' and SIFT.SubjectID = SIT.ID and SIFT.Object = 'FusionAttributeType' and SIFT.ObjectID = SFA.FusionAttributeTypeID

				inner join LoadItemColumn TS 	on TS.LoadID = SS.LoadID 	and TS.RowIndex = SS.RowIndex	and TS.ColumnIndex = 14
				inner join LoadItemColumn TST	on TST.LoadID = SS.LoadID	and TST.RowIndex = SS.RowIndex	and TST.ColumnIndex = 12 
				inner join LoadItemColumn [TO]	on [TO].LoadID = SS.LoadID	and [TO].RowIndex = SS.RowIndex	and [TO].ColumnIndex = 18
				inner join LoadItemColumn TOT	on TOT.LoadID = SS.LoadID	and TOT.RowIndex = SS.RowIndex	and TOT.ColumnIndex = 16
				left join IntersectType TIT on TIT.Subject = TST.LookupObject and TIT.SubjectID = TST.LookupObjectID and TIT.Object = TOT.LookupObject and TIT.ObjectID = TOT.LookupObjectID

				left join LoadItemColumn TF	on TF.LoadID = SS.LoadID	    and TF.RowIndex = SS.RowIndex	and TF.ColumnIndex = 20
				left join FusionAttribute TFA on TFA.ID = TF.LookupObjectID
				left join IntersectType TIFT on TIFT.Subject = 'IntersectType' and TIFT.SubjectID = TIT.ID and TIFT.Object = 'FusionAttributeType' and TIFT.ObjectID = TFA.FusionAttributeTypeID

		where	SS.LoadID = @id
				and SS.ColumnIndex = 4

		-- Add indexes to temp table
		CREATE NONCLUSTERED INDEX [IX_SourceBusinessIntersect] ON #Items ( SourceIntersectTypeID ASC, SourceSubject ASC, SourceSubjectID ASC, SourceObject ASC, SourceObjectID ASC )

		/*	BEGIN: SOURCE BUSINESS INTERSECT LOGIC */

		-- update rows with existing source business intersects
		update	T
		set		T.SourceIntersectID = S.ID,
				T.SourceIntersectChangeType = 'U'
		from	#Items T
				inner join [Intersect] S on S.IntersectTypeID = T.SourceIntersectTypeID and T.SourceSubject = S.Subject and T.SourceSubjectID = S.SubjectID and T.SourceObject = S.Object and T.SourceObjectID = S.ObjectID

		-- insert source business relationships
		insert into [Intersect] (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
			select	distinct
					SourceIntersectTypeID, 
					SourceSubject, SourceSubjectID, SourceObject, SourceObjectID,
					0, ResourceID, @dt, ResourceID, @dt
			from	#Items
			where	SourceIntersectTypeID is not null
					and SourceIntersectID = 0
					and SourceIntersectChangeType <> 'U'
					and SourceSubject is not null and SourceSubjectID is not null
					and SourceObject is not null and SourceObjectID is not null;

		-- update rows with existing source business intersect
		update	T
		set		T.SourceIntersectID = S.ID,
				T.SourceIntersectChangeType = 'A'
		from	#Items T
				inner join [Intersect] S on S.IntersectTypeID = T.SourceIntersectTypeID and T.SourceSubject = S.Subject and T.SourceSubjectID = S.SubjectID and T.SourceObject = S.Object and T.SourceObjectID = S.ObjectID
				and T.SourceIntersectChangeType <> 'U';

		/*	END: SOURCE BUSINESS INTERSECT LOGIC */


		/*	BEGIN: TARGET BUSINESS INTERSECT LOGIC */

		-- update rows with existing target business intersects
		update	T
		set		T.TargetIntersectID = S.ID,
				T.TargetIntersectChangeType = 'U'
		from	#Items T
				inner join [Intersect] S on S.IntersectTypeID = T.TargetIntersectTypeID and T.TargetSubject = S.Subject and T.TargetSubjectID = S.SubjectID and T.TargetObject = S.Object and T.TargetObjectID = S.ObjectID

		-- insert target business relationships
		insert into [Intersect] (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
			select	distinct
					TargetIntersectTypeID, 
					TargetSubject, TargetSubjectID, TargetObject, TargetObjectID,
					0, ResourceID, @dt, ResourceID, @dt
			from	#Items
			where	TargetIntersectTypeID is not null
					and TargetIntersectID = 0
					and TargetIntersectChangeType <> 'U'
					and TargetSubject is not null and TargetSubjectID is not null
					and TargetObject is not null and TargetObjectID is not null;

		-- update rows with existing target business intersect
		update	T
		set		T.TargetIntersectID = S.ID,
				T.TargetIntersectChangeType = 'A'
		from	#Items T
				inner join [Intersect] S on S.IntersectTypeID = T.TargetIntersectTypeID and T.TargetSubject = S.Subject and T.TargetSubjectID = S.SubjectID and T.TargetObject = S.Object and T.TargetObjectID = S.ObjectID
				and T.TargetIntersectChangeType <> 'U';

		/*	END: TARGET BUSINESS INTERSECT LOGIC */


		/*	BEGIN: SOURCE TECHNICAL INTERSECT LOGIC */

		-- update rows with existing source technical intersects
		update	T
		set		T.SourceFusionIntersectID = S.ID,
				T.SourceFusionIntersectChangeType = 'U'
		from	#Items T
				inner join [Intersect] S on S.IntersectTypeID = T.SourceFusionIntersectTypeID and S.Subject = 'Intersect' and S.SubjectID = TargetIntersectID and S.Object = 'FusionAttribute' and S.ObjectID = T.SourceFusionID

		-- insert source technical relationships
		insert into [Intersect] (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
			select	distinct
					SourceFusionIntersectTypeID, 
					'Intersect', SourceIntersectID, 'FusionAttribute', SourceFusionID,
					0, ResourceID, @dt, ResourceID, @dt
			from	#Items
			where	SourceFusionIntersectTypeID is not null
					and SourceFusionIntersectID = 0
					and SourceIntersectID <> 0
					and SourceFusionIntersectChangeType <> 'U'
					and SourceFusionID is not null;

		-- update rows with existing source technical intersect
		update	T
		set		T.SourceFusionIntersectID = S.ID,
				T.SourceFusionIntersectChangeType = 'A'
		from	#Items T
				inner join [Intersect] S on S.IntersectTypeID = T.SourceFusionIntersectTypeID and S.Subject = 'Intersect' and S.SubjectID = T.SourceIntersectID and S.Object = 'FusionAttribute' and S.ObjectID = T.SourceFusionID
				and T.SourceFusionIntersectChangeType <> 'U';

		/*	END: SOURCE TECHNICAL INTERSECT LOGIC */


		/*	BEGIN: TARGET TECHNICAL INTERSECT LOGIC */
		
		-- update rows with existing target technical intersects
		update	T
		set		T.TargetFusionIntersectID = S.ID,
				T.TargetFusionIntersectChangeType = 'U'
		from	#Items T
				inner join [Intersect] S on S.IntersectTypeID = T.TargetFusionIntersectTypeID and S.Subject = 'Intersect' and S.SubjectID = T.TargetIntersectID and S.Object = 'FusionAttribute' and S.ObjectID = T.TargetFusionID

		-- insert target technical relationships
		insert into [Intersect] (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
			select	distinct
					TargetFusionIntersectTypeID, 
					'Intersect', TargetIntersectID, 'FusionAttribute', TargetFusionID,
					0, ResourceID, @dt, ResourceID, @dt
			from	#Items
			where	TargetFusionIntersectTypeID is not null
					and TargetFusionIntersectID = 0
					and TargetIntersectID <> 0
					and TargetFusionIntersectChangeType <> 'U'
					and TargetFusionID is not null;

		-- update rows with existing target technical intersect
		update	T
		set		T.TargetFusionIntersectID = S.ID,
				T.TargetFusionIntersectChangeType = 'A'
		from	#Items T
				inner join [Intersect] S on S.IntersectTypeID = T.TargetFusionIntersectTypeID and S.Subject = 'Intersect' and S.SubjectID = T.TargetIntersectID and S.Object = 'FusionAttribute' and S.ObjectID = T.TargetFusionID
				and T.TargetFusionIntersectChangeType <> 'U';

		/*	END: TARGET TECHNICAL INTERSECT LOGIC */

		-- update rows with existing map items
		update	T
		set		T.MapItemID = S.ID,
				T.MapItemChangeType = 'U'
		from	#Items T
				inner join [MapItem] S on S.SourceIntersectID = T.SourceIntersectID and S.TargetIntersectID = T.TargetIntersectID

		-- insert new map items
		insert into MapItem (SourceIntersectID, TargetIntersectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
			select	distinct
					SourceIntersectID, 
					TargetIntersectID,
					ResourceID,
					@dt, 
					ResourceID,
					@dt
			from	#Items
			where	SourceIntersectID <> 0 and TargetIntersectID <> 0 and MapItemChangeType <> 'U'

		-- update source data with newly created map item IDs
		update	T
		set		T.MapItemID = S.ID,
				T.MapItemChangeType = 'A'
		from	#Items T
				inner join [MapItem] S on S.SourceIntersectID = T.SourceIntersectID and S.TargetIntersectID = T.TargetIntersectID and MapItemChangeType <> 'U'

		-- update rows with existing map rule items
		update	T
		set		T.MapRuleItemID = S.ID,
				T.MapRuleItemChangeType = 'U'
		from	#Items T
				inner join [MapRuleItem] S on S.SourceFusionAttributeID = T.SourceFusionID and S.TargetFusionAttributeID = T.TargetFusionID and T.SourceFusionID is not null and T.TargetFusionID is not null

		-- insert new map rule items
		insert into MapRuleItem (SourceFusionAttributeID, TargetFusionAttributeID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
			select	distinct
					SourceFusionID, 
					TargetFusionID,
					ResourceID,
					@dt, 
					ResourceID,
					@dt
			from	#Items
			where	SourceIntersectID <> 0 and TargetIntersectID <> 0 and MapRuleItemChangeType <> 'U' and SourceFusionID is not null and TargetFusionID is not null;

		-- update source data with newly created map rule item IDs
		update	T
		set		T.MapRuleItemID = S.ID,
				T.MapRuleItemChangeType = 'A'
		from	#Items T
				inner join [MapRuleItem] S on S.SourceFusionAttributeID = T.SourceFusionID and S.TargetFusionAttributeID = T.TargetFusionID and T.SourceFusionID is not null and T.TargetFusionID is not null and MapRuleItemChangeType <> 'U';

		-- MERGE MapRuleItemMapItem with all the IDs above
		merge	MapRuleItemMapItem as T
		using	(
				select		MapItemID, 
							MapRuleItemID
				from		#Items
				where		MapItemID > 0 and MapRuleItemID > 0
				group by	MapItemID, 
							MapRuleItemID
				) as S
		on		T.MapRuleItemID = S.MapRuleItemID and T.MapItemID = S.MapItemID
		when	not matched by target then
				insert (MapRuleItemID, MapItemID)
				values (S.MapRuleItemID, S.MapItemID);

		-- update status & status message for Items table
		
		-- SUCCESS STATUS
		update	#Items
		set		Status = 1,
				StatusMessage = case MapItemChangeType
									when 'A' then 'Business map created. '
									when 'U' then 'Business map updated. '
								end
		where	MapItemID > 0;

		update	#Items
		set		StatusMessage = StatusMessage + 
								case MapRuleItemChangeType
									when 'A' then 'Technical map created. '
									when 'U' then 'Technical map updated. '
								end
		where	Status = 1 and MapRuleItemID > 0;

		-- FAILED STATUS

		-- Business failures
		update	T
		set		T.Status = 0,
				T.StatusMessage = T.StatusMessage +
								'Business map could not be created nor updated. ' + 
								IIF(SrcS.LookupObjectID is null, 'Could not find source subject. ', '') + 
								IIF(SrcO.LookupObjectID is null, 'Could not find source object. ', '') + 
								IIF(TgtS.LookupObjectID is null, 'Could not find target subject. ', '') + 
								IIF(TgtO.LookupObjectID is null, 'Could not find target object. ', '')
		from	#Items T
				left join LoadItemColumn SrcS on SrcS.LoadID = @id and SrcS.RowIndex = T.RowIndex and SrcS.ColumnIndex = 4
				left join LoadItemColumn SrcO on SrcO.LoadID = @id and SrcO.RowIndex = T.RowIndex and SrcO.ColumnIndex = 8
				left join LoadItemColumn TgtS on TgtS.LoadID = @id and TgtS.RowIndex = T.RowIndex and TgtS.ColumnIndex = 14
				left join LoadItemColumn TgtO on TgtO.LoadID = @id and TgtO.RowIndex = T.RowIndex and TgtO.ColumnIndex = 18
		where	MapItemID = 0;

		update	T
		set		T.Status = 0,
				T.StatusMessage = T.StatusMessage +
								IIF(T.SourceIntersectTypeID is null, 'Could not find source relationship type. ', '') + 
								IIF(T.TargetIntersectTypeID is null, 'Could not find target relationship type. ', '')
		from	#Items T
		where	(T.SourceIntersectTypeID is null OR T.TargetIntersectTypeID is null);

		-- Technical failures
		update	T
		set		T.StatusMessage = T.StatusMessage +
								'Technical map could not be created nor updated. ' +
								IIF(src.LookupObjectID is null, 'Could not find source fusion attribute. ', '') + 
								IIF(tgt.LookupObjectID is null, 'Could not find target fusion attribute. ', '')
		from	#Items T
				left join LoadItemColumn src on src.LoadID = @id and src.RowIndex = T.RowIndex and src.ColumnIndex = 10
				left join LoadItemColumn tgt on tgt.LoadID = @id and tgt.RowIndex = T.RowIndex and tgt.ColumnIndex = 20
		where	MapRuleItemID = 0 and T.SourceFusionRaw <> '' and T.SourceFusionRaw is not null and T.TargetFusionRaw <> '' and T.TargetFusionRaw is not null;

		update	T
		set		T.StatusMessage = T.StatusMessage +
								IIF(T.SourceFusionIntersectTypeID is null, 'Could not find source fusion relationship type. ', '') + 
								IIF(T.TargetFusionIntersectTypeID is null, 'Could not find target fusion relationship type. ', '')
		from	#Items T
		where	(T.SourceFusionRaw is not null AND T.SourceFusionIntersectTypeID is null) OR (T.TargetFusionRaw is not null AND T.TargetFusionIntersectTypeID is null);

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
GO

