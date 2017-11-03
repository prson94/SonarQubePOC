create procedure fusion.GenerateCognosLineageData
--declare 
	@fusionId int-- = 4
as
begin
	SET NOCOUNT, ANSI_PADDING ON;
	SET ANSI_WARNINGS ON;

	declare @owner varchar(100) = 'COGNOS LINEAGE';

	declare @maps table (MappingFusionAttributeID int, DatamartFusionAttributeID int, SourceFusionAttributeID int, TargetFusionAttributeID int, TargetFusionAttribute nvarchar(500), BusinessLineageObjectID int)

	insert into @maps
		select	A.ID as MappingFusionAttributeID,
				--F.FormattedValue as SourceValue,
				DM.ID as DatamartFusionAttributeID,
				--SF.FormattedValue as ReferencedFusionAttribute,
				SA.ID as SourceFusionAttributeID,
				TA.ID as TargetFusionAttributeID,
				TA.TextPath,
				BT.ID
		from	FusionAttribute A
				inner join Field F on F.ObjectType = 'FusionAttribute' and F.ObjectID = A.ID
				inner join FieldType FT on FT.ID = F.FieldTypeID and FT.Name = 'Source'
				inner join FusionAttribute SA on SA.FusionAttributeTypeID = 1354 and SA.FusionID = @fusionId and SA.TextPath = REPLACE(REPLACE(F.FormattedValue, '[', ''), ']', '')
			
				inner join Field TF on TF.ObjectType = 'FusionAttribute' and TF.ObjectID = A.ID
				inner join FieldType TFT on TFT.ID = TF.FieldTypeID and TFT.Name = 'Target'
				inner join FusionAttribute TA on TA.FusionAttributeTypeID = 1354 and TA.FusionID = @fusionId and TA.TextPath = REPLACE(REPLACE(TF.FormattedValue, '[', ''), ']', '')

				inner join Field SF on SF.ObjectType = 'FusionAttribute' and SF.ObjectID = SA.ID
				inner join FieldType SFT on SFT.ID = SF.FieldTypeID and SFT.Name = 'ReferencedFusionAttribute'

				inner join FusionAttribute DM on DM.FusionAttributeTypeID = 193 --between 191 and 217 and ltrim(rtrim(DMA.TextPath)) like replace(SF.FormattedValue, 'MMDATAMART', 'Mass Mutual Mart')
				inner join FieldType DMFT on DMFT.Object = 'FusionAttributeType' and DMFT.ObjectID = DM.FusionAttributeTypeID and DMFT.Name = 'dblocation'
				inner join Field DMF on DMF.FieldTypeID = DMFT.ID and DMF.ObjectID = DM.ID and DMF.FormattedValue = SF.FormattedValue
				outer apply (
							select top 1 ID, Name from Artifact where ArtifactTypeID = 1 and ( (Name like '%' + SA.Name + '%') OR Name like '%' + TA.Name + '%' )
							) BT
		where	 A.FusionAttributeTypeID = 1355 and A.FusionID = @fusionId;

	declare @hardcodes table (F int, A nvarchar(250))
	insert into @hardcodes (F, A)
	values	(322266, '144A Flag  (For Derivation Only)'),
			(322416, 'Amount Issued'),
			(322188, 'Currency Code Parent'),
			(321153, 'Effective Date - Security')

	update	T
	set		T.BusinessLineageObjectID = A.ID
	from	@maps T
			inner join @hardcodes H on T.MappingFusionAttributeID = H.F
			inner join Artifact A on A.ArtifactTypeID = 1 and A.Name = H.A

	declare @mapItems table (
		SourceFusionAttributeID int, TargetFusionAttributeID int, 
		MapRuleItemID int, 
		
		SourceSubjectID int, SourceObjectID int, SourceIntersectID int, 
		TargetSubjectID int, TargetObjectID int, TargetIntersectID int, 
		MapItemID int
	)

	insert into @mapItems (SourceFusionAttributeID, TargetFusionAttributeID, SourceObjectID, TargetObjectID)
		select	DatamartFusionAttributeID,
				SourceFusionAttributeID,
				BusinessLineageObjectID,
				BusinessLineageObjectID
		from	@maps;

	insert into @mapItems (SourceFusionAttributeID, TargetFusionAttributeID, SourceObjectID, TargetObjectID)
		select	SourceFusionAttributeID,
				TargetFusionAttributeID,
				BusinessLineageObjectID,
				BusinessLineageObjectID
		from	@maps;


	-- Resolve the first fusion owner for the specific fusion 
	-- attribute in the table to set both the source and target 
	-- subjects for business lineage.
	update	T
	set		T.SourceSubjectID = O.ArtifactID
	from	@mapItems T
			inner join FusionAttribute S on S.ID = T.SourceFusionAttributeID
			cross apply (
						select	top 1
								ArtifactID
						from	FusionOwner
						where	FusionID = S.FusionID
						) O;

	update	T
	set		T.TargetSubjectID = O.ArtifactID
	from	@mapItems T
			inner join FusionAttribute S on S.ID = T.TargetFusionAttributeID
			cross apply (
						select	top 1
								ArtifactID
						from	FusionOwner
						where	FusionID = S.FusionID
						) O;
--select * from @mapItems
	----------------------------------------------------------

	declare @intersects table ( ID int, SubjectID int, ObjectID int);

	----------------------------------------------------------
	-- MERGE SOURCE INTERSECTS
	----------------------------------------------------------	
	merge	[Intersect] as T
	using	(
			select	distinct
					SourceSubjectID,
					SourceObjectID
			from	@mapItems
			where	SourceSubjectID is not null 
					and SourceObjectID is not null
			) S
	on		(T.IntersectTypeID = 1 and T.Subject = 'Artifact' and T.Object = 'Artifact' and T.SubjectID = S.SourceSubjectID and T.ObjectID = S.SourceObjectID)
	when matched then
		update	set T.Deleted = 0
	when not matched then
		insert	(IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner], Visible)
		values	(1, 'Artifact', S.SourceSubjectID, 'Artifact', S.SourceObjectID, 0, 0, getutcdate(), 0, getutcdate(), @owner, 1)
	output inserted.ID, inserted.SubjectID, inserted.ObjectID into @intersects;

	update	T
	set		T.SourceIntersectID = S.ID
	from	@mapItems T
			inner join @intersects S on S.SubjectID = T.SourceSubjectID and S.ObjectID = T.SourceObjectID;

	----------------------------------------------------------
	-- MERGE TARGET INTERSECTS
	----------------------------------------------------------	
	delete @intersects;

	merge	[Intersect] as T
	using	(
			select	distinct
					TargetSubjectID,
					TargetObjectID
			from	@mapItems
			where	TargetSubjectID is not null 
					and TargetObjectID is not null
			) S
	on		(T.IntersectTypeID = 1 and T.Subject = 'Artifact' and T.Object = 'Artifact' and T.SubjectID = S.TargetSubjectID and T.ObjectID = S.TargetObjectID)
	when matched then
		update	set T.Deleted = 0
	when not matched then
		insert	(IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner], Visible)
		values	(1, 'Artifact', S.TargetSubjectID, 'Artifact', S.TargetObjectID, 0, 0, getutcdate(), 0, getutcdate(), @owner, 1)
	output inserted.ID, inserted.SubjectID, inserted.ObjectID into @intersects;

	update	T
	set		T.TargetIntersectID = S.ID
	from	@mapItems T
			inner join @intersects S on S.SubjectID = T.TargetSubjectID and S.ObjectID = T.TargetObjectID;

--	CREATE NONCLUSTERED INDEX [nci_wi_Intersect_5DC2E404C7EECD8B76FC0643522F7047] ON [dbo].[Intersect] ([IntersectTypeID], [Deleted]) INCLUDE ([Object], [ObjectID], [Subject], [SubjectID]) WITH (ONLINE = ON)

--select * from @mapItems

	----------------------------------------------------------
	-- INSERT TECHINCAL LINEAGE
	----------------------------------------------------------
	update	T
	set		T.MapRuleItemID = S.ID
	from	@mapItems T
			inner join MapRuleItem S on S.[owner] = @owner and S.SourceFusionAttributeID = T.SourceFusionAttributeID and S.TargetFusionAttributeID = T.TargetFusionAttributeID;
	
	-- insert new mapruleitem records
	DECLARE @NewMapRuleItems table( ID int, SourceFusionAttributeID int, TargetFusionAttributeID int);  
	INSERT MapRuleItem (SourceFusionAttributeID, TargetFusionAttributeID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
		OUTPUT INSERTED.ID, INSERTED.SourceFusionAttributeID, INSERTED.TargetFusionAttributeID INTO @NewMapRuleItems  
		select	SourceFusionAttributeID, TargetFusionAttributeID, 0, getutcdate(), 0, getutcdate(), @owner 
		from	@mapItems 
		where	MapRuleItemID is null;

	update	T
	set		T.MapRuleItemID = S.ID
	from	@mapItems T
			inner join @NewMapRuleItems S on S.SourceFusionAttributeID = T.SourceFusionAttributeID and S.TargetFusionAttributeID = T.TargetFusionAttributeID;
	
	--delete any maprule item records that are not in the map
	delete	MapRuleItem 
	where	[Owner] = @owner
			and ID not in(select MapRuleItemID from @mapItems);

	----------------------------------------------------------
	-- INSERT BUSINESS LINEAGE
	----------------------------------------------------------
	update	T
	set		T.MapItemID = S.ID
	from	@mapItems T
			inner join MapItem S on S.[owner] = @owner and S.SourceIntersectID = T.SourceIntersectID and S.TargetIntersectID = T.TargetIntersectID;
	
	-- insert new mapitem records
	DECLARE @NewMapItems table( ID int, SourceIntersectID int, TargetIntersectID int);  
	INSERT MapItem (SourceIntersectID, TargetIntersectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
		OUTPUT INSERTED.ID, INSERTED.SourceIntersectID, INSERTED.TargetIntersectID INTO @NewMapItems  
		select	SourceIntersectID, TargetIntersectID, 0, getutcdate(), 0, getutcdate(), @owner 
		from	@mapItems 
		where	SourceIntersectID is not null and TargetIntersectID is not null and SourceIntersectID <> TargetIntersectID and MapItemID is null;

	update	T
	set		T.MapItemID = S.ID
	from	@mapItems T
			inner join @NewMapItems S on S.SourceIntersectID = T.SourceIntersectID and S.TargetIntersectID = T.TargetIntersectID;
	
	--delete any mapitem records that are no longer valid
	delete	MapItem 
	where	[Owner] = @owner
			and ID not in(select MapItemID from @mapItems where MapItemID is not null);

	----------------------------------------------------------
	-- LINK BUSINESS LINEAGE TO TECHNICAL LINEAGE
	----------------------------------------------------------
	merge	MapRuleItemMapItem as T
	using	(
			select	distinct
					MapRuleItemID,
					MapItemID
			from	@mapItems
			where	MapRuleItemID is not null 
					and MapItemID is not null
			) S
	on		(T.MapRuleItemID = S.MapRuleItemID and T.MapItemID = S.MapItemID)
	when not matched then
		insert	(MapRuleItemID, MapItemID, [Owner])
		values	(S.MapRuleItemID, S.MapItemID, @owner);

end