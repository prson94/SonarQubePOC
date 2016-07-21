CREATE PROCEDURE [dbo].[GetFusionTechnicalMapping] 	
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    select 
		mr.name as 'Map', 
		mr.id as 'MapID',
		mriS.FusionAttributeID as 'SourceFusionAttributeID',
		mriS.ID as 'SourceMapRuleItemID',
		faS.TextPath as 'SourceFusionAttributeTextPath',
		odS.Name as 'SourceObjectName',
		odS.objectId as 'SourceObjectID',
		odS.[object] as 'SourceObject',
		mriT.FusionAttributeID as 'TargetFusionAttributeID',
		mriT.ID as 'TargetMapRuleItemID',
		faT.TextPath as 'TargetFusionAttributeTextPath',
		odT.Name as 'TargetObjectName',
		odT.objectId as 'TargetObjectID',
		odT.[object] as 'TargetObject'
	from
		maprule mr
		inner join mapruleitem mriS on(mr.id = mriS.mapruleid and mriS.IsSource = 1)
		inner join mapruleitem mriT on(mr.id = mriT.mapruleid and mriT.IsSource = 0)
		inner join fusionattribute faS on(mriS.FusionAttributeID = faS.ID)
		inner join fusionattribute faT on(mriT.FusionAttributeID = faT.ID)
		inner join maprulemap mrm on (mrm.mapruleid = mr.id)
		inner join mapitem miS on (mrm.mapid = miS.mapid and miS.issource = 1)
		inner join mapitem miT on (mrm.mapid = miT.mapid and miT.issource = 0)
		inner join cache.objectdetails odS on ( miS.[object] = odS.[object] and miS.objectId = odS.objectId)
		inner join cache.objectdetails odT on ( miT.[object] = odT.[object] and miT.objectId = odT.objectId)
	order by mapid
END

GO