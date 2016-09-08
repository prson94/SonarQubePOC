CREATE VIEW [dbo].[MapRuleItemDetail]
AS
	select 	'MapRule' as Type,
			0 as ID,
			'MapRule|0' TextID,
			NULL as ParentTextID,
			NULL as Transformation,
			NULL as SourceFusion,
			NULL as SourceFusionAttributeID,		
			NULL as SourceFusionAttributeTextPath,
			NULL as SourceObjectName,
			NULL as SourceObjectID,
			NULL as SourceObject,
			NULL as TargetFusion,
			NULL as TargetFusionAttributeID,		
			NULL as TargetFusionAttributeTextPath,
			NULL as TargetObjectName,
			NULL as TargetObjectID,
			NULL as TargetObject
	union
	select 	'MapRule' as Type,
			mr.ID,
			'MapRule|' + cast(mr.ID as varchar) TextID,
			NULL as ParentTextID,
			mr.Transformation,
			NULL as SourceFusion,
			NULL as SourceFusionAttributeID,		
			NULL as SourceFusionAttributeTextPath,
			NULL as SourceObjectName,
			NULL as SourceObjectID,
			NULL as SourceObject,
			NULL as TargetFusion,
			NULL as TargetFusionAttributeID,		
			NULL as TargetFusionAttributeTextPath,
			NULL as TargetObjectName,
			NULL as TargetObjectID,
			NULL as TargetObject
	from	MapRule mr
	union
	select 	'MapRuleItem' as Type,
			mri.ID,
			'MapRuleItem|' + cast(mri.ID as varchar) TextID,
			'MapRule|' + cast(coalesce(mr.ID, 0) as varchar) as ParentTextID,
			NULL as Transformation,
			fS.Name as SourceFusion,
			mri.SourceFusionAttributeID as SourceFusionAttributeID,		
			faS.TextPath as SourceFusionAttributeTextPath,
			odS.Name as SourceObjectName,
			mri.SourceOwnerID as SourceObjectID,
			mri.SourceOwner as SourceObject,
			fT.Name as TargetFusion,
			mri.TargetFusionAttributeID as TargetFusionAttributeID,		
			faT.TextPath as TargetFusionAttributeTextPath,
			odT.Name as TargetObjectName,
			mri.TargetOwnerID as TargetObjectID,
			mri.TargetOwner as TargetObject
	from	MapRuleItem mri
			left join MapRuleItemMapRule mrim on mrim.MapRuleItemID = mri.ID
			left join MapRule mr on mr.ID = mrim.MapRuleID

			inner join FusionAttribute faS on mri.SourceFusionAttributeID = faS.ID
			inner join Fusion fS on fS.ID = faS.FusionID
			left join cache.ObjectDetails odS on mri.[SourceOwner] = odS.[Object] and mri.SourceOwnerID = odS.ObjectID

			inner join FusionAttribute faT on mri.TargetFusionAttributeID = faT.ID	
			inner join Fusion fT on fT.ID = faT.FusionID
			left join cache.ObjectDetails odT on mri.[TargetOwner] = odT.[Object] and mri.TargetOwnerID = odT.ObjectID