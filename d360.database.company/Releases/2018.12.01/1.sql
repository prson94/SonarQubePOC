CREATE NONCLUSTERED INDEX [IX_FieldType_AssetTypeID-Name] ON [dbo].[FieldType] ( [AssetTypeID] ASC, Name ASC )
GO;


------------------------------------------------------------------
-- GOV-5886
-- issue deleting a user then adding them back
------------------------------------------------------------------

-- fix busted trigger

-- fix busted trigger

ALTER TRIGGER [reporting].[ReportingGlobalResource_AfterDelete]
	ON [reporting].[Global_Resource]
	FOR DELETE
AS
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'Resource', ResourceID, 0), 'Resource', ResourceID from deleted;


	delete Asset
	where Object = 'Resource' and ObjectID in (select ResourceID from deleted);

go

-- delete partially deleted users
delete from field where [objecttype] = 'Resource' and objectid not in (select resourceid from reporting.global_resource)
go

delete from asset where [object] = 'Resource' and objectid not in (select resourceid from reporting.global_resource)
go

------------------------------------------------------------------


------------------------------------------------------------------
-- GOV-5891
-- Workflow Assignment duplication issue when workflow has multiple forms assigned to multiple users
------------------------------------------------------------------

-- clear out any duplicated workflow assignments
;WITH cte AS (SELECT *,ROW_NUMBER() OVER(PARTITION BY itemid, resourceobject,resourceobjectid ORDER BY id DESC) AS RN 
              FROM workflow.itemassignment where stepid is null
              )
delete cte
WHERE RN > 1
	
GO

------------------------------------------------------------------

------------------------------------------------------------------
-- GOV-5901
-- Following broken by scoring changes
------------------------------------------------------------------
ALTER VIEW [dbo].[FollowDetail]
AS
	with ArtifactTypes as
	(
	select	ID as FollowID,
			ObjectType as [Object],
			ObjectID,
			ResourceID,
			1 as HardFollow
	from	Follow
	where	ObjectType = 'ArtifactType' and FollowTypeID = 3
	union all
	select	P.ID as FollowID,
			cast('Artifact' as varchar(50)) as [Object],
			C.ID as ObjectID,
			P.ResourceID,
			0 as HardFollow
	from	Artifact C
			inner join Follow P on P.ObjectType = 'ArtifactType' and P.ObjectID = C.ArtifactTypeID and P.FollowTypeID = 3
	),
	DomainTypes as
	(
	select	ID as FollowID,
			ObjectType as [Object],
			ObjectID,
			ResourceID,
			1 as HardFollow
	from	Follow
	where	ObjectType = 'ReferenceItemType' and FollowTypeID = 3
	),
	Groups as
	(
	select	ID as FollowID,
			ObjectType as [Object],
			ObjectID,
			ResourceID,
			1 as HardFollow
	from	Follow
	where	ObjectType = 'Group' and ObjectID = 0 and FollowTypeID = 3
	union all
	select	P.ID as FollowID,
			P.ObjectType as [Object],
			C.ID as ObjectID,
			P.ResourceID,
			0 as HardFollow
	from	[Group] C
			inner join Follow P on P.ObjectType = 'Group' and P.ObjectID = 0 and P.FollowTypeID = 3
	),
	PolicyTypes as
	(
	select	ID as FollowID,
			ObjectType as [Object],
			ObjectID,
			ResourceID,
			1 as HardFollow
	from	Follow
	where	ObjectType = 'PolicyType' and FollowTypeID = 3
	union all
	select	P.ID as FollowID,
			cast('Policy' as varchar(50)) as [Object],
			C.ID as ObjectID,
			P.ResourceID,
			0 as HardFollow
	from	Policy C
			inner join Follow P on P.ObjectType = 'PolicyType' and P.ObjectID = C.PolicyTypeID and P.FollowTypeID = 3
	),
	PolicyParents as
	(
	select	F.ID as FollowID,
			T.ID,
			T.ParentID,
			F.ResourceID,
			1 as HardFollow
	from	Policy T
			inner join Follow F on F.ObjectType = 'Policy' and F.ObjectID = T.ID and F.FollowTypeID = 3
	union all
	select	P.FollowID,
			C.ID,
			C.ParentID,
			P.ResourceID,
			0 as HardFollow
	from	Policy C
			inner join PolicyParents P on P.ID = C.ParentID
	),
	Resources as
	(
	select	ID as FollowID,
			ObjectType as [Object],
			ObjectID,
			ResourceID,
			1 as HardFollow
	from	Follow
	where	ObjectType = 'ResourceType' and FollowTypeID = 3
	union all
	select	P.ID as FollowID,
			cast('Resource' as varchar(50)) as [Object],
			C.ResourceID as ObjectID,
			P.ResourceID,
			0 as HardFollow
	from	reporting.Global_Resource C
			inner join Follow P on P.ObjectType = 'ResourceType' and P.FollowTypeID = 3
	where	C.ResourceID > 0
	),
	TaxonomyParents as
	(
	select	F.ID as FollowID,
			T.ID,
			T.ParentID,
			F.ResourceID,
			1 as HardFollow
	from	Taxonomy T
			inner join Follow F on F.ObjectType = 'Taxonomy' and F.ObjectID = T.ID and F.FollowTypeID = 3
	union all
	select	P.FollowID,
			C.ID,
			C.ParentID,
			P.ResourceID,
			0 as HardFollow
	from	Taxonomy C
			inner join TaxonomyParents P on P.ID = C.ParentID
	)

	SELECT		F.FollowID,
				F.ResourceID,
				R.Email,
				R.Email as FollowerEmail,
				R.FirstName + ' ' + R.LastName as FollowerName,
				R.FirstName as FollowerFirstName,
				R.LastName as FollowerLastName,
				'Resource' as FollowerObjectType,
				F.ResourceID as FollowerObjectID,
				dbo.GenerateObjectUrl('Resource', 1, F.ResourceID) as FollowerUrl,
				F.ObjectID,
				F.[Object] as ObjectType,
				O.ObjectID as ID,
				O.Name,
				O.TextPath,
				O.Description,
				O.ParentID,
				O.Parent as ParentType,
				O.Url,
				O.ObjectTypeID as TypeID,
				O.ObjectType as [Type],
				case O.ObjectType
					when 'ResourceType' then 'User'
					when 'Group' then 'Group'
					else O.ObjectTypeName
				end as [TypeName],
				O.IconBackColor,
				O.IconForeColor,
				O.IconText,
				0 AS OpenEventCount,
				0 as CurrentScore,
				cast(F.HardFollow as bit) as HardFollow
	FROM		(
				select	FollowID,
						[Object], 
						ObjectID, 
						ResourceID, 
						HardFollow 
				from	ArtifactTypes
				union
				select	FollowID,
						[Object], 
						ObjectID, 
						ResourceID, 
						HardFollow 
				from	DomainTypes
				union
				select	FollowID,
						[Object], 
						ObjectID, 
						ResourceID, 
						HardFollow 
				from	Groups
				union
				select	FollowID,
						[Object], 
						ObjectID, 
						ResourceID, 
						HardFollow 
				from	PolicyTypes
				union
				select	FollowID,
						'Policy', 
						ID as ObjectID, 
						ResourceID, 
						HardFollow 
				from	PolicyParents
				union
				select	FollowID,
						[Object], 
						ObjectID, 
						ResourceID, 
						HardFollow 
				from	Resources
				union
				select	FollowID,
						'Taxonomy' as [Object], 
						ID as ObjectID, 
						ResourceID, 
						HardFollow 
				from	TaxonomyParents
				union
				select	ID as FollowID,
						ObjectType as [Object], 
						ObjectID, 
						ResourceID, 
						1 as HardFollow 
				from	Follow
				where	FollowTypeID = 1	
				) F
				inner join reporting.Global_Resource R on R.ResourceID = F.ResourceID
				inner join cache.ObjectDetails O on O.[Object] = F.[Object] and O.ObjectID = F.ObjectID

GO


------------------------------------------------------------------

------------------------------------------------------------------
-- GOV-5760
-- missing asset soft delete for fusion attribute and clean-up
------------------------------------------------------------------

ALTER TRIGGER [dbo].[FusionAttribute_AfterUpdate]
   ON  [dbo].[FusionAttribute] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON
	update	T
	set		T.UpdatedBy = 0,
			T.[State] = ~S.Deleted,
			T.UpdatedOn = getutcdate()
	from	Asset T
			inner join inserted S on T.Object = 'FusionAttribute' and T.ObjectID = S.ID
GO

--clean-up existing records
UPDATE A
set A.[State] = 0
from Asset A
inner join FusionAttribute F on F.ID = A.ObjectID and A.[Object] = 'FusionAttribute' and F.Deleted = 1
GO

------------------------------------------------------------------