CREATE NONCLUSTERED INDEX IX_ResponsibilityTypeRelationRuleResult_SecurityAsset_Include ON [dbo].[ResponsibilityTypeRelationRuleResult] ([SecurityAsset]) INCLUDE ([SecurityAssetID]) WITH (ONLINE = ON)


-- BEGIN:  Asset Type - uid addition ------------------------------------------
ALTER TABLE AssetType SET ( SYSTEM_VERSIONING = OFF  )
GO
alter table AssetType add [uid] uniqueidentifier constraint DF_AssetType_uid default(newid()) not null
GO
alter table AssetType_History add [uid] uniqueidentifier null
GO
update	T 
set		T.[uid] = S.[uid]
from	AssetType_History T
		inner join  AssetType S on S.ID = T.ID

update	T 
set		T.[uid] = S.[uid]
from	AssetType_History T
		inner join  (
			select	D.ID,
					newid() as [uid]
			from	(
					select	distinct
							ID
					from	AssetType_History 
					where	[uid] is null
					) D		
		) S on S.ID = T.ID and T.[uid] is null
GO
alter table AssetType_History alter column [uid] uniqueidentifier not null
GO
ALTER TABLE AssetType SET ( SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.AssetType_History) );
GO
-- END:  Asset Type - uid addition --------------------------------------------


-- BEGIN:  Asset - uid addition -----------------------------------------------
DROP INDEX [IDEX_IntersectAsset_ObjectAsset_Predicate_IntersectType] ON [utility].[IntersectAsset] WITH ( ONLINE = OFF )
GO
DROP VIEW [utility].[IntersectAsset]
GO
DROP VIEW [utility].[ArtifactAssetParent]
GO
DROP VIEW [utility].[ArtifactAssetParentIntermediate]
GO

ALTER TABLE Asset SET ( SYSTEM_VERSIONING = OFF  )
GO
alter table Asset add [uid] uniqueidentifier constraint DF_Asset_uid default(newid()) not null
GO
alter table Asset_History add [uid] uniqueidentifier null
GO

CREATE NONCLUSTERED INDEX [IX_Asset_uid] ON Asset ( [uid] ASC )
GO

update	T 
set		T.[uid] = S.[uid]
from	Asset_History T
		inner join  Asset S on S.ID = T.ID

update	T 
set		T.[uid] = S.[uid]
from	Asset_History T
		inner join  (
			select	D.ID,
					newid() as [uid]
			from	(
					select	distinct
							ID
					from	Asset_History 
					where	[uid] is null
					) D		
		) S on S.ID = T.ID and T.[uid] is null
GO
alter table Asset_History alter column [uid] uniqueidentifier not null
GO
ALTER TABLE Asset SET ( SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.Asset_History) );
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

SET ARITHABORT ON
SET CONCAT_NULL_YIELDS_NULL ON
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
SET ANSI_PADDING ON
SET ANSI_WARNINGS ON
SET NUMERIC_ROUNDABORT OFF
GO

CREATE UNIQUE CLUSTERED INDEX [IDEX_IntersectAsset_ObjectAsset_Predicate_IntersectType] ON [utility].[IntersectAsset]
(
	[ID] ASC,
	[ObjectAssetID] ASC,
	[PredicateType] ASC,
	[IntersectTypeID] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF)
GO

-- END:  Asset - uid addition -------------------------------------------------


CREATE NONCLUSTERED INDEX [IX_ResponsibilityTypeRelationRuleResult_Rule] ON [dbo].[ResponsibilityTypeRelationRuleResult] ( [RuleID] ASC )
GO

alter table ResponsibilityTypeRelationRule add LastRunOn datetime null
GO

alter table reporting.Global_Resource add CreatedOn datetime null
GO

create procedure ResponsibilityRuleShouldRun
	@id int-- = 70
as
begin
	set nocount on;

	--update ResponsibilityTypeRelationRule set LastRunOn = '7/20/2018 9:00:00 PM' where ID = 70
	declare @shouldRun bit = 0 ,
			@lastRunOn datetime,
			@o varchar(50),
			@oid int--,
			--@ruleUpdatedOn datetime

	select	@lastRunOn = coalesce(LastRunOn, '1/1/2000'),
			@o = Object,
			@oid = ObjectID--,
	--		@ruleUpdatedOn = UpdatedOn
	from	ResponsibilityTypeRelationRule
	where	ID = @id

	declare @assetMaxDate datetime,
			@assetFieldMaxDate datetime,
			@newUsers bit = 0,
			@newAssets bit = 0--,
	--		@ruleUpdated bit = 0

	--if @ruleUpdatedOn > @lastRunOn
	--begin
	--	set	@ruleUpdated = 1
	--end
	select	@newUsers = IIF(count(1) > 0, 1, 0)
	from	reporting.Global_Resource
	where	CreatedOn > @lastRunOn

	select	@assetMaxDate = max(A.CreatedOn)
	from	Asset A
			inner join AssetType T on T.ID = A.AssetTypeID and T.Object = @o and T.ObjectID = @oid 

	select	@assetMaxDate = IIF(max(A.UpdatedOn) > @assetMaxDate, max(A.UpdatedOn), @assetMaxDate)
	from	Asset A
			inner join AssetType T on T.ID = A.AssetTypeID and T.Object = @o and T.ObjectID = @oid 

	if @assetMaxDate > @lastRunOn
	begin
		set @newAssets = 1
	end

	if @newAssets = 0
	begin
		declare @fIDs table (FieldTypeID int)
		insert into @fIDs
			select	WF.FieldTypeID
			from	ResponsibilityTypeRelationRule R
					cross apply OPENJSON(R.Definition, '$.When') D--with ([When] nvarchar(max) '$.When', [Then] nvarchar(max) '$.Then') D
					cross apply OPENJSON(D.value) with (
							CheckType nvarchar(1) '$.CheckType',
							FieldTypeID int '$.FieldTypeID'--,
							--FieldTypeName nvarchar(250) '$.FieldTypeName' 
						) WF
			where	R.ID = @id
					and WF.CheckType = 'F'

		if exists(select 1 from @fIDs)
		begin
			select	@assetFieldMaxDate = max(F.EffectiveStartDate)
			from	Field F 
					inner join @fIDs FT on FT.FieldTypeID = F.FieldTypeID
					inner join Asset A on A.ID = F.AssetID
					inner join AssetType T on T.ID = A.AssetTypeID and T.Object = @o and T.ObjectID = @oid 
		end
		else
		begin
			select	@assetFieldMaxDate = max(F.EffectiveStartDate)
			from	Field F 
					inner join Asset A on A.ID = F.AssetID
					inner join AssetType T on T.ID = A.AssetTypeID and T.Object = @o and T.ObjectID = @oid 
		end

		if @assetFieldMaxDate > @lastRunOn
		begin
			set @newAssets = 1
		end	
	end

	if @newUsers = 1 or @newAssets = 1
	begin
		set @shouldRun = 1
	end
	--select	--@assetMaxDate as AssetMaxDate,
	--		--@assetFieldMaxDate as AssetFieldMaxDate,
	--		@newUsers as NewUser,
	--		@newAssets as NewAsset--,
	--	--	@ruleUpdated as RuleUpdated

	select @shouldRun
end
GO


-- GOV-4943 Remove dead/orphaned nav items ------------
delete nav
from sitenav nav
where nav.objectid is not null and nav.object is not null
and not exists (select 1 from assettype t where t.object = nav.object and t.objectid = nav.objectid);
GO
--------------------------------------------------------