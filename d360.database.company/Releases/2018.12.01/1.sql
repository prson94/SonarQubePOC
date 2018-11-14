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

------------------------------------------------------------------
-- GOV-5926
-- markit lineage using parent predicate types 3,4 if available
------------------------------------------------------------------

ALTER procedure [fusion].[GenerateMarkitMapLineageData]
	@fusionID int
as
begin
	SET NOCOUNT, ANSI_PADDING ON;
	SET ANSI_WARNINGS ON;

	declare @databaseName varchar(100);
	declare @sourceFieldTypeID int;
	declare @targetFieldTypeID int;		
	declare @mapFusionAttributeTypeID int = 710; -- this is fixed for all clients
	declare @viewColumnFusionAttributeTypeID int = 715; -- this is fixed for all clients
	
	-- load the field ids for the source / target from mappings
	select @sourceFieldTypeID = id from fieldtype where [object] = 'FusionAttributeType' and [objectid] = @mapFusionAttributeTypeID and name = 'source';
	select @targetFieldTypeID = id from fieldtype where [object] = 'FusionAttributeType' and [objectid] = @mapFusionAttributeTypeID and name = 'target';
	
	IF @sourceFieldTypeID IS NULL
	begin		
		raiserror('ERROR - Cannot find the Markit Fusion Map Source Field.  Please make sure the latest markit fusion attribute types have been pushed to this environment', 16, -1);
		return;
	end

	IF @targetFieldTypeID IS NULL
	begin		
		raiserror('ERROR - Cannot find the Markit Fusion Map Target Field.  Please make sure the latest markit fusion attribute types have been pushed to this environment', 16, -1);
		return;
	end

	-- determine the database name
	--select top 1 @databaseName = replace(sourceid, name,'') from fusionattribute where fusionid = @fusionID and fusionattributetypeid = 711 and sourceid like '%.%';	
	--substring(sourceid, 0,charindex('.',sourceid))
	select top 1 @databaseName = substring(sourceid, 0,charindex('.',sourceid)+1) from fusionattribute where fusionid = @fusionID and fusionattributetypeid = 711 and sourceid like '%.%';	

	if @databaseName is null
	begin
		raiserror('ERROR - Cannot determine the database name to strip from markit fusion attribute data', 16, -1);
		return;
	end

	-- dont run if this is not a markit fusion
	declare @fusionTypeId int;
	select @fusionTypeId = FusionTypeID from [dbo].[Fusion] where ID = @fusionID;
	if @fusionTypeId != 13
	begin
		raiserror('ERROR - The fusion lineage generation process may only be run for the Markit Fusion Type', 16, -1);
		return;
	end

	-- dont run if no map records exist for this fusion
	if not exists( select 1 from fusionattribute where fusionid = @fusionID and fusionattributetypeid = @mapFusionAttributeTypeID )
	begin
		raiserror('ERROR - No Markit Fusion Map records exist for the specified Fusion ID', 16, -1);
		return;
	end

	-- figure out the database prefix from some markit data

	-- some logging
	declare @fusionName nvarchar(250);
	select @fusionName = name from [dbo].[fusion] where id = @fusionID;

	begin
		print 'Running For Fusion:' + @fusionName;
		print 'Using Target Field ID:' + cast(@targetFieldTypeID as varchar(100));
		print 'Using Source Field ID:' + cast(@sourceFieldTypeID as varchar(100));
		print 'Using Database prefix:' + @databaseName;
	end
	-- end logging

	-- get the intersecttypeid for view -> table intersects
	declare @viewTableIntersectTypeId int;
	select @viewTableIntersectTypeId = id from intersecttype where [object] = 'FusionAttributeType' and [Subject] = 'FusionAttributeType' and [subjectid] = 714 and [objectid] = 712
	if @viewTableIntersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for markit view/table relations', 16, -1);
		return;
	end

	-- get the intersecttypeid for view -> view intersects
	declare @viewViewIntersectTypeId int;
	select @viewViewIntersectTypeId = id from intersecttype where [object] = 'FusionAttributeType' and [Subject] = 'FusionAttributeType' and [subjectid] = 714 and [objectid] = 714
	if @viewViewIntersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for markit view/view relations', 16, -1);
		return;
	end

	IF OBJECT_ID('tempdb..#maps') IS NOT NULL
		DROP TABLE #maps;

	create table #maps (	
		ID int identity primary key,			
		MapRuleItemID int,
		[ParentID] int,
		[UltimateParentID] int,
		[Level] int,
		SourceFusionAttributeID int,
		SourceFusionAttributeTypeID int,
		SourceObject nvarchar(500),		
		SourceParentObject nvarchar(max),
		SourceParentObjectFusionAttributeID int,
		SourceParentObjectFusionAttributeTypeID int,
		TargetFusionAttributeID int,
		TargetFusionAttributeTypeID int,
		TargetObject nvarchar(500),
		TargetParentObject nvarchar(max),
		TargetParentObjectFusionAttributeID int,
		TargetParentObjectFusionAttributeTypeID int,					
		[Source] varchar(50),
		[SourceID] int,	
		[Target] varchar(50),
		[TargetID] int,
	);

	CREATE NONCLUSTERED INDEX [CIX_TempMaps] ON #maps ( SourceFusionAttributeID ASC, TargetFusionAttributeID ASC );

	IF OBJECT_ID('tempdb..#objectmap') IS NOT NULL
		DROP TABLE #objectmap;

	create table #objectmap (
		MapID int,
		MapItemID int,
		[Object] varchar(50),
		[ObjectID] int,	
		[SourceIntersectID] int,		
		[TargetIntersectID] int		
	)

	CREATE NONCLUSTERED INDEX [CIX_TempObjectMap] ON #objectmap ( MapID ASC, [Object] ASC, [ObjectID] ASC );
	
	insert into #maps
		(SourceObject, TargetObject)
		select distinct
			replace(cast(F_source.formattedValue as nvarchar(500)), @databaseName, '') as SourceObject						
			, replace(cast(F_target.formattedValue as nvarchar(500)), @databaseName, '') as TargetObject			
		from 
			FusionAttribute FA
			inner join Field F_source on F_source.ObjectType = 'FusionAttribute' and F_source.ObjectID = FA.ID and F_source.FieldTypeID = @sourceFieldTypeID -- MAP SOURCE FIELD VALUE
			inner join Field F_target on F_target.ObjectType = 'FusionAttribute' and F_target.ObjectID = FA.ID and F_target.FieldTypeID = @targetFieldTypeID -- TARGET SOURCE FIELD VALUE
		where 
			FA.FusionID = @fusionID
				and
			FA.FusionAttributeTypeID = @mapFusionAttributeTypeID
			--	and
			--F_source.formattedValue like '%.cusip' or F_source.formattedValue like '%.ticker' or F_source.formattedValue like '%.cntry_of%' -- **for testing to limit to just cusip**;
	
	-- check how many map records we have
	declare @mapRecordCount int;
	select @mapRecordCount = count(1) from #maps
	if @fusionTypeId > 0
		begin
			print 'Loaded [' + cast(@mapRecordCount as varchar) + '] map records';			
		end
	else
		begin
			raiserror('ERROR - Could not load any map records this is most likely because there are no corresponding fusionattributes for the markit source/target mappings.', 16, -1);
			return;
		end

			
	--set the Source objects 
	update	T
	set		T.SourceFusionAttributeID = S.ID, T.SourceFusionAttributeTypeID = S.FusionAttributeTypeID
	from	#maps T			
			inner join fusionattribute S on (S.TextPath = T.SourceObject and S.FusionID = @fusionID)

	--set the Target Objects
	update	T
	set		T.TargetFusionAttributeID = S.ID, T.TargetFusionAttributeTypeID = S.FusionAttributeTypeID
	from	#maps T			
			inner join fusionattribute S on (S.TextPath = T.TargetObject and S.FusionID = @fusionID)

	--remove any source objects that we cant find the fusion attribute for
	delete from #maps where SourceFusionAttributeID is null or TargetFusionAttributeID is null		
	
	--set the source parent objects
	update T
	set T.SourceParentObject = FA_p.TextPath, T.SourceParentObjectFusionAttributeID = FA_p.ID, T.SourceParentObjectFusionAttributeTypeID = FA_p.FusionAttributeTypeID
	from #maps T
		inner join fusionattribute FA on (FA.ID = T.SourceFusionAttributeID)
		inner join fusionattribute FA_p on (FA_p.ID = FA.ParentID)

	--set the target parent objects
	update T
	set T.TargetParentObject = FA_p.TextPath, T.TargetParentObjectFusionAttributeID = FA_p.ID, T.TargetParentObjectFusionAttributeTypeID = FA_p.FusionAttributeTypeID
	from #maps T
		inner join fusionattribute FA on (FA.ID = T.TargetFusionAttributeID)
		inner join fusionattribute FA_p on (FA_p.ID = FA.ParentID)

	-- remove any maps that reference same fusionattribute both sides
	delete from #maps where SourceFusionAttributeID = TargetFusionAttributeID;
	
	--this query adds in the view to table mapings
	-- add in any view column to table column records
	-- table / view maps for targets that are missing connection
	insert into #maps
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, SourceParentObjectFusionAttributeID, SourceParentObject, SourceParentObjectFusionAttributeTypeID, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject, TargetParentObjectFusionAttributeID, TargetParentObject, TargetParentObjectFusionAttributeTypeID)
		select 	distinct
			m.TargetFusionAttributeID as SourceFusionAttributeID,
			m.TargetFusionAttributeTypeID as SourceFusionAttributeTypeID,
			m.TargetObject as SourceObject,
			m.TargetParentObjectFusionAttributeID as SourceParentObjectFusionAttributeID,
			m.TargetParentObject as SourceParentObject,
			m.TargetParentObjectFusionAttributeTypeID as SourceParentObjectFusionAttributeTypeID,
			T.id as TargetFusionAttributeID,
			T.fusionattributetypeid as TargetFusionAttributeTypeID,
			T.textpath as TargetObject,
			i.objectid as TargetParentObjectFusionAttributeID,
			T_p.name as TargetParentObject,
			T_p.fusionattributetypeid as TargetParentObjectFusionAttributeTypeID			
		 from 
			#maps m			
			inner join [intersect] i on (i.subjectid = m.TargetParentObjectFusionAttributeID and i.[subject] = 'FusionAttribute')	
			inner join fusionattribute T_p on (T_p.id = i.objectid)
			inner join fusionattribute T on(T.parentid = T_p.id and T.Textpath = T_p.TextPath + replace(m.TargetObject,m.TargetParentObject,'')) -- we are doing this to avoid messing with the name column that doesnt have an index
		where 
			m.TargetFusionAttributeTypeID = @viewColumnFusionAttributeTypeID
				and
			i.intersecttypeid = @viewTableIntersectTypeId
				and
			m.id not in(select m_2.id from #maps m_2 where (m_2.SourceFusionAttributeID = m.TargetFusionAttributeID and m_2.TargetFusionAttributeID = T.Id) or (m_2.TargetFusionAttributeID = m.TargetFusionAttributeID and m_2.SourceFusionAttributeID = T.id)) -- dont insert duplicates
	
	-- table / view maps for sources that are missing connection
	insert into #maps
		(TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject, TargetParentObjectFusionAttributeID, TargetParentObject, TargetParentObjectFusionAttributeTypeID, SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, SourceParentObjectFusionAttributeID, SourceParentObject, SourceParentObjectFusionAttributeTypeID)
		select 	distinct
			m.SourceFusionAttributeID as TargetFusionAttributeID,
			m.SourceFusionAttributeTypeID as TargetFusionAttributeTypeID,
			m.SourceObject as TargetObject,
			m.SourceParentObjectFusionAttributeID as TargetParentObjectFusionAttributeID,
			m.SourceParentObject as TargetParentObject,
			m.SourceParentObjectFusionAttributeTypeID as TargetParentObjectFusionAttributeTypeID,
			T.id as SourceFusionAttributeID,
			T.fusionattributetypeid as SourceFusionAttributeTypeID,
			T.textpath as SourceObject,
			i.objectid as SourceParentObjectFusionAttributeID,
			T_p.name as SourceParentObject,
			T_p.fusionattributetypeid as SourceParentObjectFusionAttributeTypeID			
		 from 
			#maps m			
			inner join [intersect] i on (i.subjectid = m.SourceParentObjectFusionAttributeID and i.[subject] = 'FusionAttribute')	
			inner join fusionattribute T_p on (T_p.id = i.objectid)
			inner join fusionattribute T on(T.parentid = T_p.id and T.Textpath = T_p.TextPath + replace(m.SourceObject,m.SourceParentObject,'')) -- we are doing this to avoid messing with the name column that doesnt have an index
		where 
			m.SourceFusionAttributeTypeID = @viewColumnFusionAttributeTypeID
				and
			i.intersecttypeid = @viewTableIntersectTypeId
				and
			m.id not in(select m_2.id from #maps m_2 where (m_2.SourceFusionAttributeID = T.Id and m_2.TargetFusionAttributeID = m.SourceFusionAttributeID) or (m_2.TargetFusionAttributeID = T.id  and m_2.SourceFusionAttributeID = m.SourceFusionAttributeID)) -- dont insert duplicates
					
	-- end table / view maps

	

	--this query adds in the view to view mapings
	-- add in any view column to view column records
	-- view / view maps for targets that are missing connection
	/*insert into #maps
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, SourceParentObjectFusionAttributeID, SourceParentObject, SourceParentObjectFusionAttributeTypeID, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject, TargetParentObjectFusionAttributeID, TargetParentObject, TargetParentObjectFusionAttributeTypeID)
		select 	distinct
			m.TargetFusionAttributeID as SourceFusionAttributeID,
			m.TargetFusionAttributeTypeID as SourceFusionAttributeTypeID,
			m.TargetObject as SourceObject,
			m.TargetParentObjectFusionAttributeID as SourceParentObjectFusionAttributeID,
			m.TargetParentObject as SourceParentObject,
			m.TargetParentObjectFusionAttributeTypeID as SourceParentObjectFusionAttributeTypeID,
			T.id as TargetFusionAttributeID,
			T.fusionattributetypeid as TargetFusionAttributeTypeID,
			T.textpath as TargetObject,
			i.objectid as TargetParentObjectFusionAttributeID,
			T_p.name as TargetParentObject,
			T_p.fusionattributetypeid as TargetParentObjectFusionAttributeTypeID			
		 from 
			#maps m			
			inner join [intersect] i on (i.subjectid = m.TargetParentObjectFusionAttributeID and i.[subject] = 'FusionAttribute')	
			inner join fusionattribute T_p on (T_p.id = i.objectid)
			inner join fusionattribute T on(T.parentid = T_p.id and T.Textpath = T_p.TextPath + replace(m.TargetObject,m.TargetParentObject,'')) -- we are doing this to avoid messing with the name column that doesnt have an index
		where 
			m.TargetFusionAttributeTypeID = @viewColumnFusionAttributeTypeID
				and
			i.intersecttypeid = @viewViewIntersectTypeId
				and
			m.id not in(select m_2.id from #maps m_2 where (m_2.SourceFusionAttributeID = m.TargetFusionAttributeID and m_2.TargetFusionAttributeID = T.Id) or (m_2.TargetFusionAttributeID = m.TargetFusionAttributeID and m_2.SourceFusionAttributeID = T.id)) -- dont insert duplicates
	*/
	insert into #maps
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, SourceParentObjectFusionAttributeID, SourceParentObject, SourceParentObjectFusionAttributeTypeID, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject, TargetParentObjectFusionAttributeID, TargetParentObject, TargetParentObjectFusionAttributeTypeID)
		select 	distinct
			m.TargetFusionAttributeID as SourceFusionAttributeID,
			m.TargetFusionAttributeTypeID as SourceFusionAttributeTypeID,
			m.TargetObject as SourceObject,
			m.TargetParentObjectFusionAttributeID as SourceParentObjectFusionAttributeID,
			m.TargetParentObject as SourceParentObject,
			m.TargetParentObjectFusionAttributeTypeID as SourceParentObjectFusionAttributeTypeID,
			T.id as TargetFusionAttributeID,
			T.fusionattributetypeid as TargetFusionAttributeTypeID,
			T.textpath as TargetObject,
			i.objectid as TargetParentObjectFusionAttributeID,
			T_p.name as TargetParentObject,
			T_p.fusionattributetypeid as TargetParentObjectFusionAttributeTypeID			
		 from 
			#maps m			
			inner join [intersect] i on (i.objectid = m.TargetParentObjectFusionAttributeID and i.[object] = 'FusionAttribute')	
			inner join fusionattribute T_p on (T_p.id = i.subjectid)
			inner join fusionattribute T on(T.FusionId = @fusionId and T.deleted = 0 and T.parentid = T_p.id and T.Textpath = T_p.TextPath + replace(m.TargetObject,m.TargetParentObject,'')) -- we are doing this to avoid messing with the name column that doesnt have an index
		where 
			m.TargetFusionAttributeTypeID = @viewColumnFusionAttributeTypeID
				and
			i.intersecttypeid = @viewViewIntersectTypeId
				and
			m.id not in(select m_2.id from #maps m_2 where (m_2.SourceFusionAttributeID = m.TargetFusionAttributeID and m_2.TargetFusionAttributeID = T.Id) or (m_2.TargetFusionAttributeID = m.TargetFusionAttributeID and m_2.SourceFusionAttributeID = T.id)) -- dont insert duplicates

	-- view / view maps for sources that are missing connection
	insert into #maps
		(TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject, TargetParentObjectFusionAttributeID, TargetParentObject, TargetParentObjectFusionAttributeTypeID, SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, SourceParentObjectFusionAttributeID, SourceParentObject, SourceParentObjectFusionAttributeTypeID)
		select 	distinct
			m.SourceFusionAttributeID as TargetFusionAttributeID,
			m.SourceFusionAttributeTypeID as TargetFusionAttributeTypeID,
			m.SourceObject as TargetObject,
			m.SourceParentObjectFusionAttributeID as TargetParentObjectFusionAttributeID,
			m.SourceParentObject as TargetParentObject,
			m.SourceParentObjectFusionAttributeTypeID as TargetParentObjectFusionAttributeTypeID,
			T.id as SourceFusionAttributeID,
			T.fusionattributetypeid as SourceFusionAttributeTypeID,
			T.textpath as SourceObject,
			i.objectid as SourceParentObjectFusionAttributeID,
			T_p.name as SourceParentObject,
			T_p.fusionattributetypeid as SourceParentObjectFusionAttributeTypeID			
		 from 
			#maps m			
			inner join [intersect] i on (i.subjectid = m.SourceParentObjectFusionAttributeID and i.[subject] = 'FusionAttribute')	
			inner join fusionattribute T_p on (T_p.id = i.objectid)
			inner join fusionattribute T on(T.FusionId = @fusionId and T.deleted = 0 and T.parentid = T_p.id and T.Textpath = T_p.TextPath + replace(m.SourceObject,m.SourceParentObject,'')) -- we are doing this to avoid messing with the name column that doesnt have an index
		where 
			m.SourceFusionAttributeTypeID = @viewColumnFusionAttributeTypeID
				and
			i.intersecttypeid = @viewViewIntersectTypeId
				and
			m.id not in(select m_2.id from #maps m_2 where (m_2.SourceFusionAttributeID = T.Id and m_2.TargetFusionAttributeID = m.SourceFusionAttributeID) or (m_2.TargetFusionAttributeID = T.id  and m_2.SourceFusionAttributeID = m.SourceFusionAttributeID)) -- dont insert duplicates

	/*	insert into #maps
		(TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject, TargetParentObjectFusionAttributeID, TargetParentObject, TargetParentObjectFusionAttributeTypeID, SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, SourceParentObjectFusionAttributeID, SourceParentObject, SourceParentObjectFusionAttributeTypeID)
		select 	distinct
			m.SourceFusionAttributeID as TargetFusionAttributeID,
			m.SourceFusionAttributeTypeID as TargetFusionAttributeTypeID,
			m.SourceObject as TargetObject,
			m.SourceParentObjectFusionAttributeID as TargetParentObjectFusionAttributeID,
			m.SourceParentObject as TargetParentObject,
			m.SourceParentObjectFusionAttributeTypeID as TargetParentObjectFusionAttributeTypeID,
			T.id as SourceFusionAttributeID,
			T.fusionattributetypeid as SourceFusionAttributeTypeID,
			T.textpath as SourceObject,
			i.objectid as SourceParentObjectFusionAttributeID,
			T_p.name as SourceParentObject,
			T_p.fusionattributetypeid as SourceParentObjectFusionAttributeTypeID			
		 from 
			#maps m			
			inner join [intersect] i on (i.objectid = m.SourceParentObjectFusionAttributeID and i.[object] = 'FusionAttribute')	
			inner join fusionattribute T_p on (T_p.id = i.subjectid)
			inner join fusionattribute T on(T.parentid = T_p.id and T.Textpath = T_p.TextPath + replace(m.SourceObject,m.SourceParentObject,'')) -- we are doing this to avoid messing with the name column that doesnt have an index
		where 
			m.SourceFusionAttributeTypeID = @viewColumnFusionAttributeTypeID
				and
			i.intersecttypeid = @viewViewIntersectTypeId
				and
			m.id not in(select m_2.id from #maps m_2 where (m_2.SourceFusionAttributeID = T.Id and m_2.TargetFusionAttributeID = m.SourceFusionAttributeID) or (m_2.TargetFusionAttributeID = T.id  and m_2.SourceFusionAttributeID = m.SourceFusionAttributeID)) -- dont insert duplicates
		*/				
	-- end view / view maps


	-- populate the previous step id this also duplicates items that have multiple paths and is very important
	update m_S
	set m_S.ParentID = m_T.ID
	from #maps m_T
	left outer join #maps m_S on (m_T.TargetFusionAttributeID = m_S.SourceFusionAttributeID)

	IF OBJECT_ID('tempdb..#levelMap') IS NOT NULL
		DROP TABLE #levelMap;
	
	;with C as
			(
			  select
				ID,
				SourceFusionAttributeID as SourceID,
				TargetFusionAttributeID as TargetID,
				ID as [UltimateParentID],
				0 as [level] 
			  from 
					#maps
			  where ParentID is null
			  union all
			  select 
					T.ID,
					T.SourceFusionAttributeID as SourceID,			 
					 T.TargetFusionAttributeID as TargetID,
					 C.[UltimateParentID] as [UltimateParentID],
					 C.[level] + 1
			  from #maps as T
				inner join C  
					on T.ParentID = C.ID				  
			)
			select C.ID, C.[level], C.[UltimateParentID]
			into #levelMap
			from C
			OPTION (MAXRECURSION 25) 

	update T
	set T.[level] = S.[level], T.[UltimateParentID] = S.[UltimateParentID]
	from #maps T
	inner join #levelMap S on S.ID = T.ID;
	
	--remove any that we cant find the level for
	--delete from #maps where [level] is null		


	-- find any object related to column as the object	
	insert into #objectmap (MapID, [Object], [ObjectID])
		select T.ID, OI.[subject], OI.[subjectid]
		from #maps T
		inner join [IntersectDetail] OI on OI.Subject <> 'FusionAttribute' and OI.Object = 'FusionAttribute' and OI.ObjectID in (T.SourceFusionAttributeID, T.TargetFusionAttributeID)  and OI.PredicateType = 8-- look for relation between non fusion object and source/target column

	-- find any business terms related to source
	update T
	set T.[source] = OI.[subject], T.[sourceid] = OI.[subjectid]--, T.sourceintersectid = OI.ID
	from #maps T
		inner join [IntersectDetail] OI on OI.Subject <> 'FusionAttribute' and OI.Object = 'FusionAttribute' and OI.ObjectID = T.SourceParentObjectFusionAttributeID  and OI.PredicateType = 8 

	
	-- find any business terms related to target
	update T
	set T.[target] = OI.[subject], T.[targetid] = OI.[subjectid]--, T.targetintersectid = OI.ID
	from #maps T
		inner join [IntersectDetail] OI on OI.Subject <> 'FusionAttribute' and OI.Object = 'FusionAttribute' and OI.ObjectID = T.TargetParentObjectFusionAttributeID and OI.PredicateType = 8
		
	-- update the objects for each path to be the same	
	insert into #objectmap (MapID, [Object], [ObjectID])
		select T.ID, SO.[object], SO.[objectID]
		from #maps T		
		inner join #maps S on T.UltimateParentID = S.UltimateParentID
		inner join #objectmap SO on S.ID = SO.MapID
		left join #objectmap T_O on (T.ID = T_O.MapID and T_O.[object] is null);
	
	
	--take any sources with null targets find the next target

	WITH hierarchy (id, [target], [targetid], [source], [sourceid]) AS
	(
		SELECT id, [target], [targetid], [source], [sourceid]
		FROM #maps
		WHERE [parentid] is null

		UNION ALL

		SELECT mc.id, coalesce(mc.[target], mc.[source], gps.[target]) as [target], coalesce(mc.targetid, mc.sourceid, gps.targetid) as [targetid], coalesce(mc.[source], gps.[target], gps.[source]) as [source], coalesce(mc.sourceid, gps.targetid, gps.sourceid) as [targetid]
		FROM #maps mc
		JOIN hierarchy gps ON gps.id = mc.parentid
	)
	UPDATE T
	set T.[target] = cte.[target], T.[targetid] = cte.[targetid], T.[source] = cte.[source], T.[sourceid] = cte.[sourceid]
	from #maps T
	inner join 
		hierarchy cte
	on cte.id = T.id
	OPTION (MAXRECURSION 50)
			
	-- generate relationships for each unique object / source that dont exist

	update T
	set T.[sourceintersectid] = OI.ID
	from #objectmap T
		inner join #maps M on (T.MapID = M.ID)
		inner join [IntersectDetail] OI on OI.[Subject] = M.[Source] and OI.SubjectID = M.[SourceID] and OI.[Object] = T.[Object] and OI.[ObjectID] = T.[ObjectID];

	update T
	set T.[sourceintersectid] = OI.ID
	from #objectmap T
		inner join #maps M on (T.MapID = M.ID)
		inner join [IntersectDetail] OI on OI.[Object] = M.[Source] and OI.ObjectID = M.[SourceID] and OI.[Subject] = T.[Object] and OI.[SubjectID] = T.[ObjectID] and T.sourceintersectid is null
	
	-- add any missing relations to source / object
	insert into [intersect] (IntersectTypeID, [Subject], SubjectID, [Object], ObjectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
		select distinct
			(select top 1 i_t.ID from [intersecttype] i_t inner join [predicate] p on i_t.[predicateid] = p.id where (p.[type] not in(3,4) and i_t.[object] = c_s.objecttype and i_t.[subject] = c_t.objecttype and i_t.objectid = c_s.objecttypeid and i_t.subjectid = c_t.objecttypeid))				
			,T.[Source]
			,T.[SourceID]
			,OM.[Object]
			,OM.[ObjectID]
			,0,getutcdate(),0,getutcdate(),'MARKIT LINEAGE'
		from #maps T
		inner join #objectmap OM on (T.ID = OM.MapID)
		inner join [cache].[objectdetails] c_s on (c_s.[object] = OM.[object] and c_s.[objectid] = OM.[objectid])
		inner join [cache].[objectdetails] c_t on (c_t.[object] = T.[source] and c_t.[objectid] = T.[sourceid])		
		where OM.sourceIntersectID is null;
	
	update OM
	set OM.[sourceintersectid] = OI.ID
	from #objectmap OM
		inner join #maps T on (OM.MapID = T.ID)		
		inner join [IntersectDetail] OI on OI.[Subject] = T.[Source] and OI.SubjectID = T.[SourceID] and OI.[Object] = OM.[Object] and OI.[ObjectID] = OM.[ObjectID] and OM.sourceintersectid is null;

	
	-- generate relationships for each unique object / target that dont exist	
	update OM
	set OM.[targetintersectid] = OI.ID
	from #objectmap OM
		inner join #maps T on (OM.MapID = T.ID)
		inner join [IntersectDetail] OI on OI.[Subject] = T.[Target] and OI.SubjectID = T.[TargetID] and OI.[Object] = OM.[Object] and OI.[ObjectID] = OM.[ObjectID]
		
	update OM
	set OM.[targetintersectid] = OI.ID
	from #objectmap OM
		inner join #maps T on (OM.MapID = T.ID)
		inner join [IntersectDetail] OI on OI.[Object] = T.[Target] and OI.ObjectID = T.[TargetID] and OI.[Subject] = OM.[Object] and OI.[SubjectID] = OM.[ObjectID] and OM.targetintersectid is null;

	-- add any missing relations to source / object
	insert into [intersect] (IntersectTypeID, [Subject], SubjectID, [Object], ObjectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
		select distinct
			(select top 1 i_t.ID from [intersecttype] i_t  inner join [predicate] p on i_t.[predicateid] = p.id where (p.[type] not in(3,4) and i_t.[object] = c_s.objecttype and i_t.[subject] = c_t.objecttype and i_t.objectid = c_s.objecttypeid and i_t.subjectid = c_t.objecttypeid))			
			,T.[target]
			,T.[targetID]
			,OM.[Object]
			,OM.[ObjectID]
			,0,getutcdate(),0,getutcdate(),'MARKIT LINEAGE'
		from #maps T
		inner join #objectmap OM on (T.ID = OM.MapID)
		inner join [cache].[objectdetails] c_s on (c_s.[object] = OM.[object] and c_s.[objectid] = OM.[objectid])
		inner join [cache].[objectdetails] c_t on (c_t.[object] = T.[target] and c_t.[objectid] = T.[targetid])		
		where OM.targetintersectid is null;
		
	update OM
	set OM.[targetintersectid] = OI.ID
	from #objectmap OM
		inner join #maps T on (OM.MapID = T.ID)
		inner join [IntersectDetail] OI on OI.[Subject] = T.[Target] and OI.SubjectID = T.[TargetID] and OI.[Object] = OM.[Object] and OI.[ObjectID] = OM.[ObjectID] and OM.targetintersectid is null;
	

	/*testing only!!*/			
--	select * from #maps order by [ultimateparentid], [level]
	/*end testing only*/

	print 'Removing any prior generated Markit Lineage map records';

	-- clear any previous values from map rule item map item table
	--delete from mapitem where [owner] = 'MARKIT LINEAGE';
	--delete from mapruleitem where [owner] = 'MARKIT LINEAGE';
	delete from mapruleitemmapitem where [owner] = 'MARKIT LINEAGE';

	print 'Inserting new map records';
	-- insert mapping data
	
	Declare @MapItemIDList Table(MapItemID int, sourceintersectid int, targetintersectid int);
	Declare @MapRuleItemIDList Table(MapRuleItemID int, MapID Int);
	
	-- load any existing map item instances
	update T
	set T.MapItemID = mi.ID
	from #objectmap T
		inner join mapitem mi on(T.sourceintersectid = mi.SourceIntersectID and T.targetintersectid = mi.TargetIntersectID and mi.[Owner] = 'MARKIT LINEAGE'); 

	-- insert map records
	MERGE
	INTO    mapitem mi
	USING   (			
			select distinct sourceintersectid, targetintersectid FROM #objectmap where (sourceintersectid is not null and targetintersectid is not null) and sourceintersectid != targetintersectid and mapitemid is null
			) S
	ON      (1 = 0)
	WHEN NOT MATCHED THEN
	INSERT  (SourceIntersectID, TargetIntersectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
	VALUES  (S.sourceintersectid, S.targetintersectid, 0, getutcdate(), 0, getutcdate(), 'MARKIT LINEAGE')
	OUTPUT  INSERTED.ID, S.sourceintersectid, S.targetintersectid into @MapItemIDList;

	--update map item id from main temp table
	update T
	set T.mapitemid = MI.MapItemID
	from #objectmap T
		inner join @MapItemIDList MI on (MI.sourceintersectid = T.sourceintersectid and MI.targetintersectid = T.targetintersectid)
		
	-- delete any mapitem records that are not in objectmap that are markit lineage
	delete from mapitem where [owner] = 'MARKIT LINEAGE' and id not in (select mapitemid from #objectmap);
	
	-- load id's of existing mapruleitems
	update T
	set T.mapruleitemid = S.id
	from #maps T
		inner join [dbo].[mapruleitem] S on (S.[owner] = 'MARKIT LINEAGE' and S.SourceFusionAttributeID = T.SourceFusionAttributeID and S.TargetFusionAttributeID = T.TargetFusionAttributeID);
	
	-- insert the mapruleitem records
	MERGE
	INTO    mapruleitem mri
	USING   (
			select SourceFusionAttributeID, TargetFusionAttributeID, ID from #maps where mapruleitemid is null
			) S
	ON      (1 = 0)
	WHEN NOT MATCHED THEN
	INSERT  (SourceFusionAttributeID, TargetFusionAttributeID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
	VALUES  (S.SourceFusionAttributeID, S.TargetFusionAttributeID, 0, getutcdate(), 0, getutcdate(), 'MARKIT LINEAGE')
	OUTPUT  INSERTED.ID, S.ID into @MapRuleItemIDList;
	
	--update map rule item id from main temp table
	update T
	set T.MapRuleItemID = MI.MapRuleItemID
	from #maps T
		inner join @MapRuleItemIDList MI on (MI.MapID = T.ID);

	-- delete any mapitem records that are not in objectmap that are markit lineage
	delete from mapruleitem where [owner] = 'MARKIT LINEAGE' and id not in (select MapRuleItemID from #maps);
			
	--insert mapruleitemmapitem records
	insert into mapruleitemmapitem 
		(MapRuleItemID, MapItemID, [Owner])
		SELECT distinct M.MapRuleItemID, OM.MapItemID , 'MARKIT LINEAGE'
		FROM #maps M 
		inner join #objectmap OM on(M.ID = OM.MapID)
		where M.MapRuleItemID is not null and OM.MapItemID is not null;	

	declare @mapruleitemmapitemCount int;
	select @mapruleitemmapitemCount = count(1) from mapruleitemmapitem where [owner] = 'MARKIT LINEAGE'
	print 'Inserted [' + cast(@mapruleitemmapitemCount as varchar) + '] mapruleitemmapitem records';			

	declare @mapruleitemCount int;
	select @mapruleitemCount = count(1) from mapruleitem where [owner] = 'MARKIT LINEAGE'
	print 'Inserted [' + cast(@mapruleitemCount as varchar) + '] mapruleitem records';			

	declare @mapitemCount int;
	select @mapitemCount = count(1) from mapitem where [owner] = 'MARKIT LINEAGE'
	print 'Inserted [' + cast(@mapitemCount as varchar) + '] mapitem records';
			
end

GO

------------------------------------------------------------------

------------------------------------------------------------------
-- GOV-5672
-- bulk load doesnt correctly handle when item has same name different parent
------------------------------------------------------------------

create FUNCTION [dbo].[GetArtifactKeyHashByIdWithParent](
	@Id bigint
)
RETURNS TABLE 
AS
RETURN 
(

		select		
											CONVERT(
												varchar(32), 
												SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(A.FieldTypeID as nvarchar) + ':' +A.Value, char(59))), 3, 32), 
												2) as KeyHash
								from		(
								
											select		top 1000000000 --percent
														A.AssetTypeID,
														A.ID,														
														FT.ID as FieldTypeID,
														F.Value as [Value]
											from		Asset A
											inner join Field F on F.ObjectType = A.Object and F.ObjectID = A.ObjectID and A.Object != 'ReferenceItem'
											inner join FieldType FT on FT.ID = F.FieldTypeID 
																	and FT.AssetTypeID = A.AssetTypeID
																	and FT.IsPartOfKey = 1
											where a.id = @id
													
											union
											select
												top 1
												A.AssetTypeID,
												A.ID,
												-1 as FieldTypeID,
												AD.DisplayValue as [Value]												
											from Asset A
											inner join [utility].IntersectAsset IAD on A.ID = IAD.ObjectAssetID and IAD.[Object] = 'Artifact'							
											inner join dbo.Asset IA on IA.Object = 'Artifact' and IA.ObjectID = IAD.SubjectID and IAD.PredicateType = 3
											inner join dbo.AssetType IAT on IAT.ID = IA.AssetTypeID
											inner join AssetDisplayValue AD on  AD.AssetID = IA.ID
									where a.id = @id	
											order by fieldtypeid
							) A

)



go


ALTER procedure [bulkload].[Promotions]
--declare
	@id int
--set @id = 84
as
begin
	set nocount on;

	declare @levels table (rowIndex int, [level] int, processed bit);

	declare @Object varchar(50),
			@ObjectID int,
			@Action varchar(1),
			@UpdatedOn datetime = getutcdate(),
			@UpdatedBy int = 0,
			@parentTypeID int = null,
			@parentTypeName nvarchar(250) = null,
			@parentIntersectTypeId int = null;

	select	@Object = [Object], 
			@ObjectID = ObjectID,
			@Action = [Action],
			@UpdatedBy = UpdatedBy
	from	[Load]
	where	ID = @id;

	-- Load Parent type info
	select 
		@parentTypeID = I.SubjectID,
		@parentTypeName = I.SubjectName,
		@parentIntersectTypeId = I.ID
	from 
		intersecttypedetail I                
	where I.[PredicateType] = 3 and [Object] = @Object and ObjectID = @ObjectId;

	print 'Parent TypeID is:';
	print @ObjectId;

	update	LoadItem
	set		Object = null, 
			ObjectID = null, 
			Status = null,
			StatusMessage = null
	where	LoadID = @id;


	print '(starting resolve lookup) ' 
	print getdate() 
	-- resolve lookups first as we need the id to generate the hash correctly

	-- Resolve Single-value LOOKUP fields
	exec [bulkload].[UpdateDynamicLookupFieldColumns] @id

	print '(completed resolve lookup) ' 
	print getdate() 


	if exists (select 1 from LoadItem LI
						inner join LoadColumn C on C.LoadID = LI.LoadID
						inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name
				where
					FT.AllowMultipleValues = 1 and LI.LoadID = @id )
	begin
		-- Resolve Multi-value LOOKUP fields
		update	IC
		set		IC.LookupObject = MV.LookupObject,
				IC.LookupValue = MV.LookupValue
		from	LoadItemColumn IC
				inner join	(
							select		IC.LoadID,
										IC.RowIndex,
										IC.ColumnIndex,
										'ReferenceItem' as LookupObject,
										string_agg(AD.ID, ',') as LookupValue
							from		LoadItem LI
										inner join LoadItemColumn IC on LI.LoadID = @id and LI.LoadID = IC.LoadID and IC.RowIndex = LI.RowIndex
										inner join LoadColumn C on C.LoadID = IC.LoadID and C.ColumnIndex = IC.ColumnIndex
										inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and FT.AllowMultipleValues = 1
										cross apply string_split(IC.Value, ',') VS									
										left join ReferenceItem AD on AD.ReferenceITemTypeID = FT.LookupObjectID
										CROSS APPLY [dbo].[GetReferenceItemDisplayValue](AD.ID, FT.ID) GRIDV
							where GRIDV.DisplayValue = ltrim(rtrim(VS.Value))
							group by	IC.LoadID,
										IC.RowIndex,
										IC.ColumnIndex			
							) MV on MV.LoadID = IC.LoadID and MV.RowIndex = IC.RowIndex and MV.ColumnIndex = IC.ColumnIndex
	end



	-- Log error messages for reference list resolution.
	update	LI
	set		LI.StatusMessage = coalesce(LI.StatusMessage,'') + FT.Name + ' could not be resolved to an existing reference item.' 
	from	LoadItem LI
			inner join LoadItemColumn IC on LI.LoadID = @id and IC.LoadID = LI.LoadID and IC.RowIndex = LI.RowIndex
			inner join LoadColumn C on C.ColumnIndex = IC.ColumnIndex and C.LoadID = IC.LoadID
			inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
										and FT.Name = C.Name 
										and FT.Type = 'Lookup' 
										and FT.LookupObjectType = 'ReferenceItem' 
										and (FT.IsRequired = 1 or FT.IsPartOfKey = 1) 
										and ( 
												(FT.AllowMultipleValues = 0 AND IC.LookupObjectID is null) OR 
												(FT.AllowMultipleValues = 1 AND IC.LookupValue is null)
											);

	-- Resolve Allow All LOOKUP field values
	update	IC
	set		IC.LookupObject = REPLACE(FT.LookupObjectType, 'Type', ''),
			IC.LookupObjectID = 0,
			IC.LookupValue = 0
	from	LoadItemColumn IC
			inner join LoadColumn C on C.ColumnIndex = IC.ColumnIndex and C.LoadID = IC.LoadID and C.LoadID = @id
			inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and FT.Type = 'Lookup' and FT.AllowAllValue = 1 and IC.Value = FT.AllowAllLabel;

	-- Process hashes for Load Items needs to be after lookup, lookup
	if @Object = 'ReferenceItemType'
	begin		
		update	T
		set		T.KeyHash = CONVERT(
									varchar(32), 
									SUBSTRING(HASHBYTES('SHA1', substring(ltrim(rtrim(IC.Value)), 1, 250)), 3, 32), 
									2),
				T.FieldHash = V.FieldHash
		from	LoadItem T
				inner join LoadColumn C on C.LoadID = T.LoadID and C.Name = 'Code'
				inner join LoadItemColumn IC on IC.LoadID = C.LoadID and IC.RowIndex = T.RowIndex and IC.ColumnIndex = C.ColumnIndex
				inner join	(
							select		RowIndex,
										CONVERT(
											varchar(32), 
											SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
											2) as FieldHash
							from		(
										select		top 100 percent
													I.RowIndex,
													FT.ID as FieldTypeID,
													coalesce(cast(IC.LookupObjectID as varchar(100)), IC.Value, '') as Value
										from		LoadItem I
													inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id
													inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex = IC.ColumnIndex													
													left join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and C.Name !='Code'
													left join dbo.ReferenceItem RI on C.Name = 'Code' and RI.ID = @ObjectID
										order by	I.RowIndex,
													FT.ID
										) A
							group by	A.RowIndex	
							) V on V.RowIndex = T.RowIndex
		where	T.LoadID = @id;
	end
	else if @Object = 'TaxonomyType'
	begin
		declare @currRow int, @maxRow int, @currLevel int;
		set @currRow = 1;
		set @currLevel = 0;
		set @maxRow = (select max(RowIndex) from LoadItem where LoadID = @id);	

		while @currRow < @maxRow
		begin
			set @currRow = @currRow + 1;

			--get level for current row
			select		@currLevel = coalesce(max(L.[Level]), 1) 
			from		TaxonomyTypeLevel L
						inner join LoadColumn LC on LC.LoadID = @id and L.Name = substring(LC.[Name], 1, len(LC.[Name]) - charindex(' ', reverse(LC.[Name])))
						inner join LoadItemColumn LI on LI.LoadID = @id and LI.RowIndex = @currRow and LI.ColumnIndex = LC.ColumnIndex and LI.[Value] is not null
			where		L.TaxonomyTypeID = @ObjectID

			insert into @levels (rowIndex, level, processed) values (@currRow, @currLevel, 0);

			--update the key hash based on the current level
			update	T
			set		T.KeyHash = K.KeyHash,
					T.FieldHash = V.FieldHash
			from	LoadItem T
					left join	(
								select		RowIndex,
											CONVERT(
												varchar(32), 
												SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
												2) as KeyHash
								from		(
												select top 100 percent
													IC.RowIndex, 
													FT.ID as FieldTypeID, 
													coalesce(cast(IC.LookupObjectID as varchar(100)), IC.[Value],'') as [Value] 
												from LoadColumn LC
												inner join LoadItemColumn IC on IC.LoadID = @id and IC.RowIndex = @currRow and IC.ColumnIndex = LC.ColumnIndex
												inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.IsPartOfKey = 1 and FT.Name = reverse(substring(reverse(LC.[Name]), 0, charindex(' ',reverse(LC.[Name]))))			
												where LC.LoadID = @id and LC.ColumnIndex in (
			 										select		LC.ColumnIndex 
													from		TaxonomyTypeLevel L
																inner join LoadColumn LC on LC.LoadID = @id and L.Name = substring(LC.[Name], 1, len(LC.[Name]) - charindex(' ', reverse(LC.[Name])))
																inner join LoadItemColumn LI on LI.LoadID = @id and LI.RowIndex = @currRow and LI.ColumnIndex = LC.ColumnIndex and LI.[Value] is not null
													where		L.TaxonomyTypeID = @ObjectID and L.[Level] = @currLevel
													)
											) A
								group by	A.RowIndex
								) K on K.RowIndex = T.RowIndex
					inner join	(
								select		RowIndex,
											CONVERT(
												varchar(32), 
												SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
												2) as FieldHash
								from		(
											select		top 100 percent
														I.RowIndex,
														FT.ID as FieldTypeID,
														coalesce(IC.Value, '') as Value
											from		LoadItem I
														inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id
														inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex = IC.ColumnIndex
														inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name
											order by	I.RowIndex,
														FT.ID
											) A
								group by	A.RowIndex	
								) V on V.RowIndex = T.RowIndex
			where	T.LoadID = @id and T.RowIndex = @currRow;
		end
	end
	else
	begin
		-- if Object is an artifact type with a parent we need to take into consideration the parent and use the 
		-- parent key hash value instead. Leave existing logic untouched to minimize impact
		if @Object = 'ArtifactType' and @parentTypeID is not null
		begin
			print 'Generating keyhash including parent'			
			update	T
			set		T.KeyHash = K.KeyHash,
					T.FieldHash = V.FieldHash
			from	LoadItem T
					inner join	(
								select		RowIndex,
											CONVERT(
												varchar(32), 
												SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' +Value, char(59))), 3, 32), 
												2) as KeyHash
								from		(
								
											select		top 1000000000 --percent
														I.RowIndex,
														FT.ID as FieldTypeID,
														coalesce(cast(IC.LookupObjectID as varchar(100)), IC.Value, '') as Value
											from		LoadItem I
														inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id and IC.Value is not null
														inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex = IC.ColumnIndex
														inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.IsPartOfKey = 1 and FT.Name = C.Name
													
											union
											select
												top 1000000000
												I.RowIndex,
												-1 as FieldTypeID,
												coalesce(cast(IC.LookupObjectID as varchar(100)), IC.Value, '') as Value
											from
												LoadItem I
												inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id and IC.Value is not null
												inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex = IC.ColumnIndex
												inner join ASSETTYPE ATT on ATT.Object = @Object and ATT.ObjectID = @parentTypeID 
												inner join ASSET A on A.AssetTypeID = ATT.ID
												inner join AssetDisplayValue AD on A.ID = AD.AssetID and AD.DisplayValue = IC.Value
											order by	RowIndex,
														FieldTypeID
											) A								
								group by	A.RowIndex
								) K on K.RowIndex = T.RowIndex
					inner join	(
								select		RowIndex,
											CONVERT(
												varchar(32), 
												SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
												2) as FieldHash
								from		(
											select		top 100 percent
														I.RowIndex,
														FT.ID as FieldTypeID,
														coalesce(IC.Value, '') as Value
											from		LoadItem I
														inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id
														inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex = IC.ColumnIndex
														inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name
											order by	I.RowIndex,
														FT.ID
											) A
								group by	A.RowIndex	
								) V on V.RowIndex = T.RowIndex
			where	T.LoadID = @id;

		end
		else
		begin
			update	T
			set		T.KeyHash = K.KeyHash,
					T.FieldHash = V.FieldHash
			from	LoadItem T
					inner join	(
								select		RowIndex,
											CONVERT(
												varchar(32), 
												SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' +Value, char(59))), 3, 32), 
												2) as KeyHash
								from		(
											select		top 1000000000 --percent
														I.RowIndex,
														FT.ID as FieldTypeID,
														coalesce(cast(IC.LookupObjectID as varchar(100)), IC.Value, '') as Value
											from		LoadItem I
														inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id and IC.Value is not null
														inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex = IC.ColumnIndex
														inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.IsPartOfKey = 1 and FT.Name = C.Name
											order by	I.RowIndex,
														FT.ID
											) A
								group by	A.RowIndex
								) K on K.RowIndex = T.RowIndex
					inner join	(
								select		RowIndex,
											CONVERT(
												varchar(32), 
												SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
												2) as FieldHash
								from		(
											select		top 100 percent
														I.RowIndex,
														FT.ID as FieldTypeID,
														coalesce(IC.Value, '') as Value
											from		LoadItem I
														inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id
														inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex = IC.ColumnIndex
														inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name
											order by	I.RowIndex,
														FT.ID
											) A
								group by	A.RowIndex	
								) V on V.RowIndex = T.RowIndex
			where	T.LoadID = @id;
		end
	end
	-- -----------------------------


	-- Resolve RELATIONSHIP fields
	if exists (select 1 from LoadColumn C
					inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and FT.Type = 'Relationship' where C.LoadID = @id )
	begin
		declare @relFieldLookups table (LoadID int, RowIndex int, ColumnIndex int, Object varchar(50), ObjectID int )

		insert into @relFieldLookups
			select	IC.LoadID,
					Ic.RowIndex,
					IC.ColumnIndex,
					D.Object,
					D.ObjectID
			from	LoadItemColumn IC
					inner join LoadColumn C on C.ColumnIndex = IC.ColumnIndex and C.LoadID = IC.LoadID and C.LoadID = @id
					inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and FT.Type = 'Relationship'
					inner join IntersectType IT on FT.LookupObjectType = 'IntersectType' and FT.LookupObjectID = IT.ID
					inner join AssetType DT on DT.Object = case when IT.Subject = @Object and IT.SubjectID = @ObjectID then IT.Object else IT.Subject end
												and DT.ObjectID = case when IT.Subject = @Object and IT.SubjectID = @ObjectID then IT.ObjectID else IT.SubjectID end
					inner join dbo.GetAssetDisplayValue() D on D.AssetTypeID = DT.ID and D.DisplayValue = ltrim(rtrim(IC.Value));

		update	T
		set		T.LookupObject = S.Object,
				T.LookupObjectID = S.ObjectID
		from	LoadItemColumn T
				inner join @relFieldLookups S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex;
	end


	-- Capture changes for logging purposes.
	--declare @tbl table (ObjectID int, RowIndex int, [Action] varchar(1), [FieldsLoaded] bit null, [RelationshipsLoaded] bit null);

	IF OBJECT_ID('tempdb..#tbl') IS NOT NULL
			DROP TABLE #tbl;

	create table #tbl (ObjectID int, RowIndex int, [Action] varchar(1), [FieldsLoaded] bit null, [RelationshipsLoaded] bit null);

	CREATE CLUSTERED INDEX PK_tempTbl ON #tbl ([RowIndex] ASC,[Action] ASC);

	--declare @insertToPerform table (RowID int identity, KeyHash varchar(250));
	IF OBJECT_ID('tempdb..#insertToPerform') IS NOT NULL
			DROP TABLE #insertToPerform;

	create table #insertToPerform (RowID int identity, KeyHash varchar(250));

	CREATE CLUSTERED INDEX PK_tempinsertToPerform ON #insertToPerform ([KeyHash] ASC);

	--declare @insertOutputID table (RowID int identity, ObjectID int);
	IF OBJECT_ID('tempdb..#insertOutputID') IS NOT NULL
			DROP TABLE #insertOutputID;

	create table #insertOutputID (RowID int identity, ObjectID int);

	-- COMMON ------------------
	-- Identify which load items already exist based on key hash.	

	if @Object = 'ArtifactType' and @parentTypeID is not null
	begin
		print 'comparing bulk load hash to KeyHash With Parent'
		update	T
		set		T.Object = A.Object,
				T.ObjectID = A.ObjectID
		from	LoadItem T
				inner join AssetType ST on ST.Object = @Object and ST.ObjectID = @ObjectID			
				inner join Asset A on A.AssetTypeID = ST.ID
				cross apply [GetArtifactKeyHashByIdWithParent](A.ID) S 
		where S.KeyHash = T.KeyHash and T.LoadID = @id
	end
	else
	begin
		print 'comparing bulk load hash to normal AssetKeyHash'

		update	T
		set		T.Object = A.Object,
				T.ObjectID = A.ObjectID
		from	LoadItem T
				inner join AssetType ST on ST.Object = @Object and ST.ObjectID = @ObjectID			
				inner join Asset A on A.AssetTypeID = ST.ID
				cross apply GetAssetKeyHashById(A.ID) S 
		where S.KeyHash = T.KeyHash and T.LoadID = @id
	end


	--BEGIN TRANSACTION;
    --SAVE TRANSACTION PromotionCreationTrans;

	--BEGIN Try 
			-- ARTIFACTS ---------------
			if @Object = 'ArtifactType'
			begin
				-- Mark the existing artifacts as being updated.
				update	T
				set		T.UpdatedBy = @UpdatedBy,
						T.UpdatedOn = @UpdatedOn
				from	Artifact T
						inner join LoadItem S on S.LoadID = @id and S.Object = 'Artifact' and S.ObjectID = T.ID and T.ArtifactTypeID = @ObjectID;

				-- Insert the updated records into temp table for logging.
				insert into #tbl 
					select	ObjectID,
							RowIndex,
							'U', null, null
					from	LoadItem
					where	LoadID = @id 
							and ObjectID is not null;

				-- Insert new items into the Artifact table.
				insert into #insertToPerform
					select	distinct
							KeyHash
					from	LoadItem
					where	LoadID = @id
							and ObjectID is null
							and KeyHash is not null;

				--declare @insertOutputID table (RowID int identity, ObjectID int);
				insert Artifact (ArtifactTypeID, UpdatedOn, UpdatedBy, CreatedOn, CreatedBy)
				output inserted.ID into #insertOutputID
					select	@ObjectID, 
							@UpdatedOn, 
							@UpdatedBy, 
							@UpdatedOn, 
							@UpdatedBy
					from	#insertToPerform;

				-- Insert the added records into temp table for logging.
				insert into #tbl 
					select	N.ObjectID,
							I.RowIndex,
							'A', null, null
					from	LoadItem I
							inner join #insertToPerform P on P.KeyHash = I.KeyHash and I.LoadID = @id 
							inner join #insertOutputID N on N.RowID = P.RowID;

				-- Update the LoadItem table with the Object and ObjectID generated from the insert above.
				update	T
				set		T.Object = 'Artifact',
						T.ObjectID = S.ObjectID
				from	LoadItem T
						inner join #tbl S on T.LoadID = @id and S.RowIndex = T.RowIndex and S.[Action] = 'A';
			end
			-------------------------

			-- MODEL ----------------
		   if @Object = 'TaxonomyType'
		   begin
				declare 
					@row int, 
					@level int, 
					@rows int, 
					@rowObject varchar(50), 
					@rowObjectId int, 
					@parentKeyHash varchar(50),
					@intersectTypeid int,
					@parentObjectId int;

				declare @ids table (id int);

				set @row = 0;
				set @level = 0;

				-- Insert the updated records into temp table for logging.
				insert into #tbl 
					select	ObjectID,
							RowIndex,
							'U', null, null
					from	LoadItem
					where	LoadID = @id 
							and ObjectID is not null;

				while (select count(*) from @levels where processed = 0) > 0
				begin
					set @parentKeyHash = null;
					set @parentObjectId = null;
					delete from @ids;

					--need to process rows in order of level (low to high) to make sure parent items are added or exist
					select		top 1
								@row = L.RowIndex, 
								@level = L.[Level], 
								@rowObject = LC.[Object], 
								@rowObjectId = LC.ObjectID 
					from		@levels L
								inner join LoadItem LC on LC.RowIndex = L.RowIndex and LC.LoadID = @id
					where		L.processed = 0
					order by	L.[Level] asc;

					if @rowObjectId is not null
					begin
						update	Taxonomy
						set		UpdatedOn = @UpdatedOn,
								UpdatedBy = @UpdatedBy
						where	ID = @rowObjectId;
					end
					else
					begin
						if @level > 1
						begin
							--hash key fields at (level - 1) and check against asset or LoadItem
							select @parentKeyHash = CONVERT(
											varchar(32), 
											SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
											2)
							from		(
											select		top 100 percent
														FT.ID as FieldTypeID, 
														coalesce(IC.[Value],'') as [Value] 
											from		LoadColumn LC
														inner join LoadItemColumn IC on IC.LoadID = @id and IC.RowIndex = @row and IC.ColumnIndex = LC.ColumnIndex
														inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.IsPartOfKey = 1 
															and FT.Name = reverse(substring(reverse(LC.[Name]), 0, charindex(' ',reverse(LC.[Name]))))			
											where		LC.LoadID = @id and LC.ColumnIndex in (
			 												select	LC.ColumnIndex 
															from	TaxonomyTypeLevel L
																	inner join LoadColumn LC on LC.LoadID = @id and L.Name = substring(LC.[Name], 1, len(LC.[Name]) - charindex(' ', reverse(LC.[Name])))
																	inner join LoadItemColumn LI on LI.LoadID = @id and LI.RowIndex = @row and LI.ColumnIndex = LC.ColumnIndex and LI.[Value] is not null
															where	L.TaxonomyTypeID = @ObjectID and L.[Level] = (@level-1)
															)
										) A;

							select @parentObjectId = coalesce(
									(
									select		top 1 
												a.ObjectID 
									from		Asset A
												inner join AssetType T on T.Object = @Object and T.ObjectID = @ObjectID and A.AssetTypeID = T.ID
												inner join GetAssetKeyHash() H on H.ID = A.ID
									where		H.KeyHash = @parentKeyHash
									),
									(
									select		top 1 
												a.ObjectID 
									from		LoadItem L
												inner join Asset A on A.[Object] = L.[Object] and A.ObjectID = L.ObjectID
									where		LoadID = @id and L.KeyHash = @parentKeyHash
									)
								);

							if @parentObjectId is not null
							begin
								insert Taxonomy (TaxonomyTypeID, UpdatedOn, UpdatedBy)
								output inserted.ID into @ids
									select	@ObjectID, 
											@UpdatedOn, 
											@UpdatedBy;

								insert into #tbl
								select	id,
										@row,
										'A', null, null
								from	@ids

								select  @intersectTypeId = id 
								from	intersecttypedetail 
								where	[subject] = @Object and subjectid = @ObjectID 
										and [object] = @Object and objectid = @objectID
										and predicatetype = 4;

								if @intersectTypeId is not null 
									and not exists (
										select		1 
										from		[Intersect] 
										where		IntersectTypeID = @intersectTypeId 
													and ObjectID = (select id from @ids) 
													and SubjectID = @parentObjectId)
								begin						
									insert into [Intersect] (IntersectTypeId, [Subject], [Object], SubjectID, ObjectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
									select	@intersectTypeId as IntersectTypeId,
											'Taxonomy' as [Subject],
											'Taxonomy' as [Object],
											@parentObjectId as SubjectID,
											(select id from @ids) as ObjectID,
											@UpdatedBy as CreatedBy,
											@UpdatedOn as CreatedOn,
											@UpdatedBy as UpdatedBy,
											@UpdatedOn as UpdatedOn,
											'BulkLoad' as [Owner];
								end
							end
						end
						else --root item
						begin			
							insert Taxonomy (TaxonomyTypeID, UpdatedOn, UpdatedBy)
							output inserted.ID into @ids
								select	@ObjectID, 
										@UpdatedOn, 
										@UpdatedBy;

							insert into #tbl
							select	id,
									@row,
									'A', null, null
							from	@ids;									
						end
					end

					update	@levels 
					set		processed = 1 
					where	rowIndex = @row 
							and [level] = @level;

					update	T
					set		T.Object = 'Taxonomy',
							T.ObjectID = S.ObjectID
					from	LoadItem T
							inner join #tbl S on T.LoadID = @id and S.RowIndex = T.RowIndex and S.[Action] = 'A';
				end

			end
			--------------------------

			-- REFERENCE ------------
			if @Object = 'ReferenceItemType'
			begin
				declare @ri_insertToPerform table (RowID int identity, Code nvarchar(250), KeyHash varchar(250));
				declare @ri_insertOutputID table (RowID int identity, ObjectID int);

				-- Mark the existing items as being updated.
				update	T
				set		T.UpdatedBy = @UpdatedBy,
						T.UpdatedOn = @UpdatedOn
				from	ReferenceItem T
						inner join LoadItem S on S.LoadID = @id and S.Object = 'ReferenceItem' and S.ObjectID = T.ID and T.ReferenceItemTypeID = @ObjectID;

				-- Insert the updated records into temp table for logging.
				insert into #tbl 
					select	ObjectID,
							RowIndex,
							'U', null, null
					from	LoadItem
					where	LoadID = @id 
							and ObjectID is not null;

				-- Insert new items into the ReferenceItem table.
				insert into @ri_insertToPerform
					select	distinct
							substring(ltrim(rtrim(IC.Value)), 1, 250),
							I.KeyHash
					from	LoadItem I
							inner join LoadColumn C on C.LoadID = I.LoadID and C.Name = 'Code'
							inner join LoadItemColumn IC on C.ColumnIndex = IC.ColumnIndex and IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex 
					where	I.LoadID = @id
							and I.ObjectID is null
							and I.KeyHash is not null;

				insert ReferenceItem (ReferenceItemTypeID, Code, UpdatedOn, UpdatedBy, CreatedOn, CreatedBy)
				output inserted.ID into @ri_insertOutputID
					select	@ObjectID, 
							Code,
							@UpdatedOn, 
							@UpdatedBy, 
							@UpdatedOn, 
							@UpdatedBy
					from	@ri_insertToPerform;

				-- Insert the added records into temp table for logging.
				insert into #tbl 
					select	N.ObjectID,
							I.RowIndex,
							'A', null, null
					from	LoadItem I
							inner join @ri_insertToPerform P on P.KeyHash = I.KeyHash and I.LoadID = @id 
							inner join @ri_insertOutputID N on N.RowID = P.RowID;

				-- Update the LoadItem table with the Object and ObjectID generated from the insert above.
				update	T
				set		T.Object = 'ReferenceItem',
						T.ObjectID = S.ObjectID
				from	LoadItem T
						inner join #tbl S on T.LoadID = @id and S.RowIndex = T.RowIndex and S.[Action] = 'A';
			end
			-------------------------


			-- Capture field logs	
			IF OBJECT_ID('tempdb..#fields') IS NOT NULL
					DROP TABLE #fields;

			create table #fields (RowIndex int, ColumnIndex int, [Action] varchar(25));


			--CREATE CLUSTERED INDEX PK_tempFields ON #fields ([RowIndex] ASC,[ColumnIndex] ASC);

			-- Non-relationship fields
			print '(starting merge fields) ' 
			print getdate() 

				merge	Field as T
				using	(
						select	I.FieldTypeID,
								I.Type,
								I.AllowMultipleValues,
								I.Object,
								I.ObjectID,
								case 
									when I.Type = 'Lookup' and I.AllowMultipleValues = 0 then cast(C.LookupObjectID as nvarchar)
									when I.Type = 'Lookup' and I.AllowMultipleValues = 1 then C.LookupValue
									else C.Value
								end as [Value],
								C.RowIndex,
								C.ColumnIndex
						from	(
								select		I.LoadID,
											FT.ID as FieldTypeID,
											FT.Type,
											FT.AllowMultipleValues,
											I.Object,
											I.ObjectID,
											min(I.RowIndex) as RowIndex,
											C.ColumnIndex
								from		LoadItem I
											inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id
											inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex
											inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
																	and  (
																		FT.Name = LC.Name or
																			(
																				@Object = 'TaxonomyType'
																				 and LC.ColumnIndex in (
																					select LC2.ColumnIndex from TaxonomyTypeLevel L2
																					inner join LoadColumn LC2 on LC2.LoadID = @id and L2.[Name] = substring(LC2.[Name], 1, len(LC2.[Name]) - charindex(' ', reverse(LC2.[Name])))
																					inner join LoadItemColumn LI2 on LI2.LoadID = @id and LI2.RowIndex = C.RowIndex and LI2.ColumnIndex = LC2.ColumnIndex and LI2.[Value] is not null
																					where L2.TaxonomyTypeID = @ObjectID and L2.[Level] = (select [level] from @levels where rowIndex = C.RowIndex)
																				 )
																				 and FT.Name = reverse(substring(reverse(LC.[Name]), 0, charindex(' ',reverse(LC.[Name])))) 
																			)
																		)
																	and FT.Type <> 'Relationship' 
																	and ( 
																			(FT.Type <> 'Lookup' and C.Value is not null) OR 
																			(FT.Type = 'Lookup' and FT.AllowMultipleValues = 0 and C.LookupObjectID is not null) OR
																			(FT.Type = 'Lookup' and FT.AllowMultipleValues = 1 and C.LookupValue is not null)
																		)
								where		I.ObjectID is not null
								group by	I.LoadID,
											FT.ID,
											FT.Type,
											FT.AllowMultipleValues,
											I.Object,
											I.ObjectID,
											C.ColumnIndex
								) I
								inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and C.ColumnIndex = I.ColumnIndex
						) S on (T.FieldTypeID = S.FieldTypeID and S.Object = T.ObjectType and S.ObjectID = T.ObjectID)
				when matched then
					update	set
							Value = S.Value
				when not matched then
					insert (FieldTypeID, ObjectType, ObjectID, Value)
					values (S.FieldTypeID, S.Object, S.ObjectID, S.Value)
				output S.RowIndex, S.ColumnIndex, $action into #fields;

	print '(end merge fields) ' 
	print getdate() 

	--END TRY
    /*BEGIN CATCH
        IF @@TRANCOUNT > 0
        BEGIN
            ROLLBACK TRANSACTION PromotionCreationTrans; -- rollback to PromotionCreationTrans
        END
    END CATCH
    COMMIT TRANSACTION */

	delete	T
	from	FieldValue T
			left join (
				select		FT.ID as FieldTypeID,
							I.Object,
							I.ObjectID,
							VS.Value
				from		LoadItem I
							inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id and I.ObjectID is not null and C.LookupValue is not null
							cross apply string_split(C.LookupValue, ',') VS
							inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex
							inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
													and FT.Name = LC.Name 
													and FT.Type = 'Lookup' 
													and FT.AllowMultipleValues = 1
			) S on S.FieldTypeID = T.FieldTypeID and S.Object = T.ObjectType and S.ObjectID = T.ObjectID and S.value = T.Value
			inner join (	--LIMITS THE IMPACT OF THIS STATEMENT
				select		FT.ID as FieldTypeID,
							I.Object,
							I.ObjectID
				from		LoadItem I
							inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id and I.ObjectID is not null and C.LookupValue is not null
							inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex
							inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
													and FT.Name = LC.Name 
													and FT.Type = 'Lookup' 
													and FT.AllowMultipleValues = 1			
			) L on L.FieldTypeID = T.FieldTypeID and L.Object = T.ObjectType and L.ObjectID = T.ObjectID
	where	S.FieldTypeID is null;

	insert into FieldValue (FieldTypeID, ObjectType, ObjectID, Value)
		select		FT.ID,
					I.Object,
					I.ObjectID,
					VS.Value
		from		LoadItem I
					inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id and I.ObjectID is not null and C.LookupValue is not null
					cross apply string_split(C.LookupValue, ',') VS
					inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex
					inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
											and FT.Name = LC.Name 
											and FT.Type = 'Lookup' 
											and FT.AllowMultipleValues = 1
					left join FieldValue FV on FV.FieldTypeID = FT.ID and FV.ObjectType = I.Object and FV.ObjectID = I.ObjectID and FV.Value = VS.Value
		where		FV.ID is null;



	update	T
	set		T.FieldsLoaded = 1
	from	#tbl T
			inner join	(
						select		RowIndex,
									[Action]
						from		#fields
						group by	RowIndex, 
									[Action]
						) S on S.RowIndex = T.RowIndex;

	truncate table #fields;
		

	if @parentTypeID is not null
	begin

		-- look for column with the parent type name this contains the parent 
		merge	[Intersect] as T
		using	(
				select	distinct
						AD.ObjectID as ParentObjectID,
						AD.[Object] as ParentObject,
						AD.[TypeID] as ParentTypeID,
						AD.[Type] as ParentType,
						@parentIntersectTypeId as IntersectTypeID,
						LI.[Object] as ItemObject,
						LI.ObjectID as ItemObjectID
				from	LoadItem I
						inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id
						inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex and LC.Name = @parentTypeName
						inner join AssetDetail AD on AD.TypeID = @parentTypeID and AD.DisplayValue = C.Value and AD.[Type] = @Object	
						inner join LoadItem LI on LI.RowIndex = C.RowIndex and LI.LoadID = C.LoadID					
				where	I.ObjectID is not null
				) S on (T.IntersectTypeID = S.IntersectTypeID and T.Subject = S.ParentObject and T.SubjectID = S.ParentObjectID and T.Object = S.ItemObject and T.ObjectID = S.ItemObjectID)

		when matched then
			update	set
					T.Subject	= S.ParentObject,
					T.SubjectID = S.ParentObjectID,
					T.Object	= S.ItemObject,
					T.ObjectID	= S.ItemObjectID
		when not matched then
			insert (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner], Visible)
			values (
					S.IntersectTypeID,
					S.ParentObject, 
					S.ParentObjectID,
					S.ItemObject, 
					S.ItemObjectID, 
					0, @UpdatedBy, @UpdatedOn, @UpdatedBy, @UpdatedOn, 'BulkLoad', 1
					);

	end

	print '(starting merge relationship fields) ' 
	print getdate() 
	-- Relationship fields
	merge	[Intersect] as T
	using	(
			select	distinct
					FT.LookupObjectID as IntersectTypeID,
					case 
						when IT.Subject = @Object and IT.SubjectID = @ObjectID then cast(1 as bit)
						else cast(0 as bit)
					end as IsSubject,
					case 
						when IT.Subject = @Object and IT.SubjectID = @ObjectID then I.Object
						else C.LookupObject
					end as Subject,
					case 
						when IT.Subject = @Object and IT.SubjectID = @ObjectID then I.ObjectID
						else C.LookupObjectID
					end as SubjectID,
					case 
						when IT.Subject = @Object and IT.SubjectID = @ObjectID then C.LookupObject
						else I.Object
					end as Object,
					case 
						when IT.Subject = @Object and IT.SubjectID = @ObjectID then C.LookupObjectID
						else I.ObjectID
					end as ObjectID
			from	LoadItem I
					inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id
					inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex
					inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
											and FT.Name = LC.Name and FT.Type = 'Relationship' 
											and C.LookupObject is not null and C.LookupObjectID is not null
					inner join IntersectType IT on FT.LookupObjectType = 'IntersectType' and FT.LookupObjectID = IT.ID
			where	I.ObjectID is not null
			) S on (
					T.IntersectTypeID = S.IntersectTypeID 
					and (
							(S.IsSubject = 1 and S.Subject = T.Subject and S.SubjectID = T.SubjectID) OR
							(S.IsSubject = 0 and S.Object = T.Object and S.ObjectID = T.ObjectID)
						)
					)
	when matched then
		update	set
				T.Subject	= S.Subject,
				T.SubjectID = S.SubjectID,
				T.Object	= S.Object,
				T.ObjectID	= S.ObjectID
	when not matched then
		insert (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner], Visible)
		values (
				S.IntersectTypeID,
				S.Subject, 
				S.SubjectID,
				S.Object, 
				S.ObjectID, 
				0, @UpdatedBy, @UpdatedOn, @UpdatedBy, @UpdatedOn, 'BulkLoad', 1
				);

	print '(done merge relationship fields) ' 
	print getdate()

	-- Capture logs and update load status. -----
	update	T
	set		T.Status = 1,
			T.StatusMessage = 'Item successfully ' + case S.[Action] when 'A' then 'added' else 'updated' end + '.'
	from	LoadItem T
			inner join #tbl S on T.LoadID = @id and S.RowIndex = T.RowIndex and T.[Object] is not null and T.ObjectID is not null;

	update	LoadItem
	set		Status = 0,
			StatusMessage = 'Item load failed. ' + coalesce(StatusMessage, '')
	where	([Object] is null or ObjectID is null)
			and LoadID = @id;



	----Finally, close out the Load.
	update	[Load] 
	set		DateCompleted = getutcdate()
	where	ID = @id
	---------------------------------------------
end

go



-- GOV-5945 - Delete Asset API -----------------------------------
create procedure [asset].[BulkDelete]
--declare 
	@uid uniqueidentifier,
	@r int
as
begin
	set nocount on;
/*
	--TESTING LOGIC
	declare @uid uniqueidentifier = 'A9B94F4B-14F6-474F-9572-80F954C8FC59', @r int = 1

	drop table if exists #AssetTable;
	create table #AssetTable (
		ItemNumber int not null,

		Uid uniqueidentifier null,
		AssetID bigint null,

		[Message] nvarchar(2500) null,
		Success bit null
	);
	
	insert into #AssetTable (ItemNumber, [Uid]) values (1, null);--'AC8AE7C0-8CD0-482D-AC44-DB05502150B3');
*/
	update	T
	set		T.AssetID = S.ID
	from	#AssetTable T
			inner join Asset S on S.Uid = T.Uid
			inner join AssetType ST on ST.Uid = @uid and ST.ID = S.AssetTypeID;

	-- Validation checks
	update	#AssetTable
	set		Success = 0,
			[Message] = coalesce([Message] + '; ', '') + 'You must provide a valid Uid for this asset when you are attempting to delete it'
	where	[Uid] is null or [Uid] = CAST(CAST(0 AS BINARY) AS UNIQUEIDENTIFIER); -- (empty guid)

	update	#AssetTable
	set		Success = 0,
			[Message] = coalesce([Message] + '; ', '') + 'Not found based on Uid provided'
	where	AssetID is null;
	--------------------

	-- Now upsert the valid assets.
	drop table if exists #ObjectMergeTableResult;
	create table #ObjectMergeTableResult (ID int, ItemNumber int, [Action] nvarchar(10));
	CREATE NONCLUSTERED INDEX IX_TempObjectMergeTableResult ON #ObjectMergeTableResult ( ItemNumber ASC );

	declare @current int = 1,	-- to track which ItemNumber row you are on.
			@max int = 0

	select @max = max(ItemNumber) from #AssetTable

	while @current <= @max
	begin
		if exists(select ItemNumber from #AssetTable where ItemNumber = @current and Success is null)
		begin
			declare @assetId bigint;
			select @assetId = AssetID from #AssetTable where ItemNumber = @current
			exec DeleteAssetById @assetId, @r
		end
		set @current = @current + 1
	end

	update	#AssetTable
	set		Success = 1
	where	Success is null
			and AssetID is not null;

	select * from #AssetTable
end
GO
------------------------------------------------------------------


------------------------------------------------------------------
-- GOV-5959
-- update DateLastLoggedIn column in Complex Lookup json
------------------------------------------------------------------
update FieldTypeLookup
set Definition = replace(Definition, '"FieldTypeName":"DateLastLoggedIn"', '"FieldTypeName":"LastLoggedInOn"')
where Definition like '%DatelastLoggedIn%';
GO
------------------------------------------------------------------
