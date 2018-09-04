-- GOV-5361 --------------------------------
DROP TABLE [dbo].[ScoreMetric]
GO
DROP TABLE [dbo].[Score]
GO
DROP TABLE [dbo].[ScoreTypeMetricVersion]
GO
DROP TABLE [dbo].[ScoreTypeMetric]
GO
DROP TABLE [dbo].[ScoreType]
GO

create Function [dbo].[GetEmailStepRecipients]
(
	@workflowItemStepID int	
)
RETURNS varchar(max)
BEGIN
	declare @tbl table (ResourceID int, FirstName nvarchar(250), LastName nvarchar(250), Email nvarchar(500), Username nvarchar(500), DateLastLoggedIn datetime null, ResourceTypeID int, Status nvarchar(25))
	
	insert into @tbl
		select 
			R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
		from workflow.itemstep s 
			outer apply s.settings.nodes('settings/emails/email') as m(c) 
			inner join reporting.Global_Resource R  on trim(m.c.value('@address', 'varchar(max)')) = R.email
		where id = @workflowItemStepID

	return (select string_agg(FirstName + ' ' + LastName,', ') as Resources from @tbl)

end
GO

--Need to remove these before processing temporal deletions below.
DROP VIEW [utility].[IntersectAsset]
GO
DROP VIEW [utility].[ArtifactAssetParent]
GO
DROP VIEW [utility].[ArtifactAssetParentIntermediate]
GO

-- GOV-5387
ALTER TABLE Asset SET ( SYSTEM_VERSIONING = OFF  )
GO
drop table Asset_History
GO

ALTER TABLE Asset DROP PERIOD FOR SYSTEM_TIME; 
alter table Asset drop column [EffectiveStartDate]
alter table Asset drop column [EffectiveEndDate]
GO

create VIEW [utility].[IntersectAsset]
WITH SCHEMABINDING  
AS  
    select
	I.ID,
	I.ID as IntersectID,
	I.IntersectTypeID as IntersectTypeID,
	P.Type as PredicateType,
	a_o.ID as ObjectAssetID,
	I.[Object] as [Object],
	I.ObjectID as [ObjectID],	
	I.[Subject] as [Subject],
	I.SubjectID as [SubjectID]
from 
	dbo.[Intersect] I
	inner join dbo.Asset a_o on I.[Object] = a_o.[Object] and I.[ObjectID] = a_o.ObjectID	
	inner join dbo.[IntersectType] IT on I.IntersectTypeID = IT.ID
	inner join dbo.[Predicate] P on P.ID = IT.PredicateID
GO

CREATE VIEW [utility].[ArtifactAssetParentIntermediate]
WITH SCHEMABINDING  
AS  
    select	a_o.ID as AssetID,		
			I.SubjectID as ParentArtifactID
	from
		dbo.[Intersect] I
		inner join dbo.Asset a_o on I.[Object] = a_o.[Object] and I.[ObjectID] = a_o.ObjectID	
		inner join dbo.[IntersectType] IT on I.IntersectTypeID = IT.ID
		inner join dbo.[Predicate] P on P.ID = IT.PredicateID and P.[Type] = 3		
	where I.[Object] = 'Artifact'
GO

create VIEW [utility].[ArtifactAssetParent]
WITH SCHEMABINDING  
AS  
    select	
		aim.AssetID,
		aim.ParentArtifactID,
		IA.ID as ParentAssetID
	from [utility].[ArtifactAssetParentIntermediate] aim
		inner join dbo.Asset IA on IA.Object = 'Artifact' and aim.ParentArtifactID = IA.ObjectID 	
GO

--Need to remove this before processing temporal deletions below.
DROP VIEW [dbo].[ResponsibilityAllAsset]
GO

-- GOV-5387
ALTER TABLE [AssetType] SET ( SYSTEM_VERSIONING = OFF  )
GO
drop table AssetType_History
GO

ALTER TABLE AssetType DROP PERIOD FOR SYSTEM_TIME; 
alter table AssetType drop column [EffectiveStartDate]
alter table AssetType drop column [EffectiveEndDate]
GO

-- GOV-5388
ALTER TABLE [Field] SET ( SYSTEM_VERSIONING = OFF  )
GO
drop table Field_History
GO

ALTER TABLE Field DROP PERIOD FOR SYSTEM_TIME; 
alter table Field drop column [EffectiveStartDate]
alter table Field drop column [EffectiveEndDate]
GO

ALTER TABLE [FieldType] SET ( SYSTEM_VERSIONING = OFF  )
GO
drop table FieldType_History
GO

ALTER TABLE FieldType DROP PERIOD FOR SYSTEM_TIME; 
alter table FieldType drop column [EffectiveStartDate]
alter table FieldType drop column [EffectiveEndDate]
GO



CREATE VIEW [dbo].[ResponsibilityAllAsset] with SCHEMABINDING as 
	-- users
	(select	O.RuleID,
			O.ResponsibilityTypeID,
			RT.Name as ResponsibilityTypeName,
			O.AssetID,
			O.AssetTypeID,
			O.SecurityAssetID as ResourceID,
			R.FirstName + ' ' + R.LastName as ResourceName,
			O.SecurityAsset,
			O.SecurityAssetID,
			(R.FirstName + ' ' + R.LastName) as SecurityAssetName,  
			O.Context,
			O.ApplyToType,
			O.PermissionsBitMask,
			O.IsVisible,
			O.OverrideID,			
			T.Object as [Type],
			T.ObjectID as TypeID
	from	dbo.ResponsibilityTypeRelationRuleResult O
			inner join dbo.ResponsibilityType RT on RT.ID = O.ResponsibilityTypeID
			inner join dbo.AssetType T on T.ID = O.AssetTypeID			
			inner join reporting.Global_Resource R on R.ResourceID = O.SecurityAssetID
	where	O.Overridden = 0 and O.SecurityAsset != 'G' and O.SecurityAsset !='O')
	union
	(select	O.RuleID,
			O.ResponsibilityTypeID,
			RT.Name as ResponsibilityTypeName,
			O.AssetID,
			O.AssetTypeID,
			RG.ResourceID as ResourceID,
			R.FirstName + ' ' + R.LastName as ResourceName,
			O.SecurityAsset,
			O.SecurityAssetID,
			G.Name as SecurityAssetName,  
			O.Context,
			O.ApplyToType,
			O.PermissionsBitMask,
			O.IsVisible,
			O.OverrideID,			
			T.Object as [Type],
			T.ObjectID as TypeID
	from	dbo.ResponsibilityTypeRelationRuleResult O
			inner join dbo.ResponsibilityType RT on RT.ID = O.ResponsibilityTypeID
			inner join dbo.AssetType T on T.ID = O.AssetTypeID			
			inner join dbo.[Group] G on G.ID = O.SecurityAssetID
			inner join dbo.ResourceGroup RG on RG.GroupID = G.ID			
			inner join reporting.Global_Resource R on R.ResourceID = RG.ResourceID
	where	O.Overridden = 0 and O.SecurityAsset = 'G')
	union
	(select	O.RuleID,
			O.ResponsibilityTypeID,
			RT.Name as ResponsibilityTypeName,
			O.AssetID,
			O.AssetTypeID,
			RD.ResourceID as ResourceID,
			R.FirstName + ' ' + R.LastName as ResourceName,
			O.SecurityAsset,
			O.SecurityAssetID,
			D.Name as SecurityAssetName,  
			O.Context,
			O.ApplyToType,
			O.PermissionsBitMask,
			O.IsVisible,
			O.OverrideID,			
			T.Object as [Type],
			T.ObjectID as TypeID
	from	dbo.ResponsibilityTypeRelationRuleResult O
			inner join dbo.ResponsibilityType RT on RT.ID = O.ResponsibilityTypeID
			inner join dbo.AssetType T on T.ID = O.AssetTypeID			
			inner join dbo.[Organization] D on O.SecurityAsset = 'O' and D.ID = O.SecurityAssetID
			inner join dbo.OrganizationResource RD on RD.OrganizationID = D.ID
			inner join reporting.Global_Resource R on R.ResourceID = RD.ResourceID
	where	O.Overridden = 0 and O.SecurityAsset = 'O')
GO