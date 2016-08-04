CREATE PROCEDURE [dbo].[GetTechnicalMapping] 	
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    
select 		
		mr.id as 'MapID',
		mr.transformation as 'Transformation',
		mri.ID as 'MapRuleItemID',
		mri.SourceFusionAttributeID as 'SourceFusionAttributeID',		
		faS.TextPath as 'SourceFusionAttributeTextPath',
		odS.Name as 'SourceObjectName',
		mri.sourceownerid as 'SourceObjectID',
		mri.[sourceowner] as 'SourceObject',
		mri.TargetFusionAttributeID as 'TargetFusionAttributeID',		
		faT.TextPath as 'TargetFusionAttributeTextPath',
		odT.Name as 'TargetObjectName',
		mri.targetownerid as 'TargetObjectID',
		mri.[targetowner] as 'TargetObject'
	from
		maprule mr
		inner join mapruleitemmaprule mrim on (mr.id = mrim.mapruleid)
		inner join mapruleitem mri on(mrim.mapruleitemid = mri.id)		
		inner join fusionattribute faT on(mri.TargetFusionAttributeID = faT.ID)		
		inner join fusionattribute faS on(mri.SourceFusionAttributeID = faS.ID)		
		left join cache.objectdetails odS on ( mri.[sourceowner] = odS.[object] and mri.sourceownerId = odS.objectId)
		left join cache.objectdetails odT on ( mri.[targetowner] = odT.[object] and mri.targetownerId = odT.objectId)		
	order by mapid

END