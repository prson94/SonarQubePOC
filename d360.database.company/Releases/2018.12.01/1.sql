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

------------------------------------------------------------------
-- GOV-5959
-- clean up unused code / objects
------------------------------------------------------------------
drop table dbo.[language]
------------------------------------------------------------------

CREATE NONCLUSTERED INDEX [IX_FusionAttribute_TypeID_SourceID] ON [dbo].[FusionAttribute] ([FusionAttributeTypeID], [SourceID]) WITH (ONLINE = ON)
GO;

create FUNCTION [dbo].[GetAssetTypeUrlById]
(	
	@AssetTypeID int
)
RETURNS TABLE 
AS
RETURN 
(
	-- Add the SELECT statement with parameter references here
	SELECT CASE T.Object
		WHEN 'ArtifactType' THEN 'artifact/' + CAST(T.ObjectID as varchar)
		WHEN 'ReferenceItem' THEN 'reference/' +  + CAST(T.ObjectID as varchar)-- + '/' +  + CAST(@ObjectID as varchar)
		WHEN 'ReferenceItemType' THEN 'reference/' + CAST(T.ObjectID as varchar)
		WHEN 'FusionType' THEN 'fusion/' + CAST(T.ObjectID as varchar)
		WHEN 'PolicyType' THEN 'policy/' + CAST(T.ObjectID as varchar) + '/structure'		
		WHEN 'ResourceType' THEN 'resource/list/' + CAST(T.ObjectID as varchar)
		WHEN 'RuleType' THEN 'quality/rule/' + CAST(T.ObjectID as varchar)	
		WHEN 'TaxonomyType' THEN 'model/' + CAST(T.ObjectID as varchar) + '/structure'	
	end as Url
	from AssetType T
	where T.ID = @AssetTypeID
)
GO;

create FUNCTION [dbo].[GenerateAssetTypeUrl] 
(
	@AssetTypeID int
)
RETURNS varchar(500)
AS
BEGIN
	DECLARE @Type varchar(50), @TypeID int;
	DECLARE @Url varchar(500) = '';

	select 
		@Type = T.[Object],
		@TypeID = T.ObjectID
	from
		AssetType T
	where T.ID = @AssetTypeID;

	SET @Url = CASE @Type
		WHEN 'ArtifactType' THEN 'artifact/' + CAST(@TypeID as varchar)
		WHEN 'ReferenceItemType' THEN 'reference/' + CAST(@TypeID as varchar)	
		WHEN 'FusionType' THEN 'fusion/' + CAST(@TypeID as varchar)
		WHEN 'PolicyType' THEN 'policy/' + CAST(@TypeID as varchar) + '/structure'		
		WHEN 'ResourceType' THEN 'resource/list/' + CAST(@TypeID as varchar)
		WHEN 'RuleType' THEN 'quality/rule/' + CAST(@TypeID as varchar)	
		WHEN 'TaxonomyType' THEN 'model/' + CAST(@TypeID as varchar) + '/structure'				
	END

	RETURN @Url
END
GO;

create FUNCTION [dbo].[GenerateAssetUrl] 
(
	@AssetID bigint
)
RETURNS varchar(500)
AS
BEGIN
	declare @Url varchar(500) = '';
	declare @Type varchar(50), @TypeID int, @ObjectID int = 0;


	select
		@ObjectID = A.ObjectID,
		@Type = A.[Object],
		@TypeID = T.ObjectID
	from
		Asset A
		inner join AssetType T on T.ID = A.AssetTypeID
	where
		A.ID = @AssetID;


	SET @Url = CASE @Type
		WHEN 'Artifact' THEN 'artifact/' +  + CAST(@TypeID as varchar) + '/' + CAST(@ObjectID as varchar)
		WHEN 'ReferenceItem' THEN 'reference/' +  + CAST(@TypeID as varchar)-- + '/' +  + CAST(@ObjectID as varchar)
		WHEN 'FusionAttribute' THEN 'fusion/fusionattribute/' + CAST(@TypeID as varchar) + '/' + CAST(@ObjectID as varchar)		
		WHEN 'Fusion' THEN 'fusion/' + CAST(@TypeID as varchar) + '/' + + CAST(@ObjectID as varchar)
		WHEN 'Group' THEN 'groups/' + CAST(@ObjectID as varchar)	
		WHEN 'Policy' THEN 'policy/' + CAST(@TypeID as varchar(15)) + '/id/' + CAST(@ObjectID as varchar)	
		WHEN 'Resource' THEN 'resource/' + CAST(@ObjectID as varchar)
		WHEN 'Rule' THEN 'quality/rule/' + CAST(@TypeID as varchar) + '/' + CAST(@ObjectID as varchar)
		WHEN 'Taxonomy' THEN 'model/' + CAST(@TypeID as varchar) + '/id/' + CAST(@ObjectID as varchar)	
	END

	RETURN @Url
END
GO;

create FUNCTION [dbo].[GenerateUrlByTypeName] 
(
	@Type varchar(50),
	@TypeID int,
	@ObjectID int = 0
)
RETURNS varchar(500)
AS
BEGIN
	DECLARE @Url varchar(500) = ''

	SET @Url = CASE @Type
		WHEN 'Lookup' THEN 'admin/lookups/' + CAST(@TypeID as varchar) + '/' + CAST(@ObjectID as varchar)
		WHEN 'LookupType' THEN 'admin/lookups/' + CAST(@TypeID as varchar)
		WHEN 'ShoppingCartType' THEN 'cart/' + CAST(@ObjectID as varchar)	
	END

	RETURN @Url
END
GO;

ALTER view [cache].[ObjectDetails]
as
select
	T.Object,
	T.ObjectID,
	T.Name,
	T.Name as TextPath,
	cast(null as nvarchar) as Description,		
	T.Url,
	T.Url as NgUrl,
	cast(null as varchar) as Parent,
	cast(null as int) as ParentID,
	cast(null as nvarchar) as ParentName,
	T.ObjectType,
	T.ObjectTypeID,
	T.ObjectTypeName,
	T.IconBackColor,
	T.IconForeColor,
	T.IconText
from
	( select	A.Object as Object,
		A.ObjectID as ObjectID,
		AName.DisplayValue as Name,						
		AUrl.[Url] as [Url],
		AST.Object as ObjectType,
		AST.ObjectID as ObjectTypeID,
		AST.Name as ObjectTypeName,
		coalesce(S.IconBackColor, '#000') as IconBackColor,
		coalesce(S.IconForeColor, '#fff') as IconForeColor,
		coalesce(S.IconText, 'leaf') as IconText
	from	AssetType AST
		left join ObjectStyle S on S.ObjectType = AST.Object and S.ObjectID = AST.ObjectID
		left join Asset A on A.AssetTypeID = AST.ID
		cross apply [dbo].[GetAssetUrlById](A.ID) AUrl
		cross apply [dbo].[GetAssetDisplayValueById](A.ID) AName
			) T		
union -- types
select
	T_t.Object,
	T_t.ObjectID,
	T_t.Name,
	T_t.Name as TextPath,
	cast(null as nvarchar) as Description,		
	T_t.Url,
	T_t.Url as NgUrl,
	cast(null as varchar) as Parent,
	cast(null as int) as ParentID,
	cast(null as nvarchar) as ParentName,
	T_t.ObjectType,
	T_t.ObjectTypeID,
	T_t.ObjectTypeName,
	T_t.IconBackColor,
	T_t.IconForeColor,
	T_t.IconText
from
( select	AST.Object as Object,
		AST.ObjectID as ObjectID,
		AST.Name as Name,						
		AUrl.[Url] as [Url],
		AST.Object as ObjectType,
		AST.ObjectID as ObjectTypeID,
		null as ObjectTypeName,
		coalesce(S.IconBackColor, '#000') as IconBackColor,
		coalesce(S.IconForeColor, '#fff') as IconForeColor,
		coalesce(S.IconText, 'leaf') as IconText
	from	AssetType AST
		left join ObjectStyle S on S.ObjectType = AST.Object and S.ObjectID = AST.ObjectID		
		cross apply [dbo].[GetAssetTypeUrlByID](AST.ID) AUrl
			) T_t
union -- intersects
select	'Intersect' as Object,
		I.ID as ObjectID,
		IName.Name as Name,
		IName.Name as TextPath,		
		cast(null as nvarchar) as Description,
		null as Url,
		null as NgUrl,
		cast(null as varchar) as Parent,
		cast(null as int) as ParentID,
		cast(null as nvarchar) as ParentName,
		'IntersectType' as ObjectType,
		IT.ID as ObjectTypeID,
		ITypeName.Name as ObjectTypeName,
		coalesce(S.IconBackColor, '#000') as IconBackColor,
		coalesce(S.IconForeColor, '#fff') as IconForeColor,
		coalesce(S.IconText, 'leaf') as IconText
from	IntersectType IT		
		inner join [Intersect] I on I.IntersectTypeID = IT.ID		
		left join ObjectStyle S on S.ObjectType = 'IntersectType' and S.ObjectID = IT.ID		
		cross apply dbo.GetIntersectNames(I.ID) IName	
		cross apply dbo.GetIntersectTypeNames(IT.ID) ITypeName

union -- intersect types
select	'IntersectType' as Object,
		I_T.ID as ObjectID,
		ITypeName.Name as Name,
		ITypeName.Name as TextPath,		
		cast(null as nvarchar) as Description,
		null as Url,
		null as NgUrl,
		cast(null as varchar) as Parent,
		cast(null as int) as ParentID,
		cast(null as nvarchar) as ParentName,
		'IntersectType' as ObjectType,
		0 as ObjectTypeID,
		null as ObjectTypeName,
		coalesce(S.IconBackColor, '#000') as IconBackColor,
		coalesce(S.IconForeColor, '#fff') as IconForeColor,
		coalesce(S.IconText, 'leaf') as IconText
from	IntersectType I_T				
		left join ObjectStyle S on S.ObjectType = 'IntersectType' and S.ObjectID = I_T.ID				
		cross apply dbo.GetIntersectTypeNames(I_T.ID) ITypeName
GO;


ALTER VIEW [dbo].[AssetWithFieldInfo]   
AS   
SELECT   
     ss.ID
     ,ss.[Object]
	 ,ss.ObjectID
	 ,cast(null as datetime) as EffectiveStartDate
	 ,(gr.firstname + ' ' + gr.lastname) as ResourceName
	 ,ft.id as FieldTypeID
	 ,ft.FriendlyName  
	 ,f.FormattedValue
FROM [dbo].[asset] ss   
	inner join [dbo].[assettype] sst on (sst.id = ss.assettypeid)
	inner JOIN [dbo].[fieldtype] ft on (ft.[object] = sst.[object] and ft.[objectid] = sst.[objectid])
	left join [dbo].[field] f on(ss.[object] = f.objecttype and ss.[objectid] = f.objectid and f.fieldtypeid= ft.id)
	left join reporting.global_resource gr on (ss.updatedby = gr.resourceid)
GO;

ALTER VIEW [dbo].[FieldWithRelation]
AS
	SELECT	F.FieldTypeID,
			T.Name,
			T.FriendlyName,
			T.Category,
			T.Description,
			T.DisplayDescription,
			T.FormDescription,
			T.ValidationDescription,
			T.Type,
			T.LookupObjectType,
			T.LookupObjectID,
			T.LookupDisplayFormat,
			T.MinimumLength,
			T.MaximumLength,
			T.Length,
			T.Pattern,
			T.IsDisplayable,
			T.IsEditable,
			T.IsListable,
			T.IsRequired,
			T.SortOrder,
			T.AllowMultipleValues,
			F.ObjectType,
			F.ObjectID,
			F.Value,
			F.FormattedValue,
			case  
				when (T.AllowMultipleValues = 0 and T.LookupObjectType = 'ReferenceItemType') then 
					[dbo].GenerateAssetTypeUrl((select ID from AssetType Where Object = 'ReferenceItemType' and ObjectID = T.LookupObjectID))
				when (T.AllowMultipleValues = 0 and T.LookupObjectType = 'ReferenceItem') then 
					[dbo].GenerateAssetUrl((select ID from AssetType Where Object = 'ReferenceItem' and ObjectID = T.LookupObjectID))
				when (T.AllowMultipleValues = 0 and T.LookupObjectType = 'Resource') then 
					[dbo].GenerateAssetUrl((select ID from Asset WHere Object = 'Resource' and ObjectID = T.LookupObjectID))
				else null
			end as LookupUrl
	FROM	FieldType T
			left join Field F on F.FieldTypeID = T.ID
	WHERE	(F.Value is not null OR T.DefaultValue is not null)
GO;

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
				dbo.GenerateAssetUrl((select ID from Asset where Object ='Resource' and ObjectID = F.ResourceID)) as FollowerUrl,
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
GO;

alter view [dbo].[IntersectDetail]
as
	
	select	I.IntersectID as ID,
			I.IntersectTypeID,
			I.State,
			I.Subject,
			I.SubjectID,
			S.Name as SubjectName,
			S.Name as SubjectShortName,
			case when S.AssetID is not null then
				dbo.GenerateAssetUrl(S.AssetID)
			else
				dbo.GenerateAssetTypeUrl(S.AssetTypeID)
			end as SubjectUrl,
			S.Type as SubjectType,
			S.TypeID as SubjectTypeID,
			S.TypeName as SubjectTypeName,
			S.BackColor as SubjectIconBackColor,
			S.ForeColor as SubjectIconForeColor,
			S.Icon as SubjectIconText,

			I.Object,
			I.ObjectID,
			O.Name as ObjectName,
			O.Name as ObjectShortName,
			case when O.AssetID is not null then
				dbo.GenerateAssetUrl(O.AssetID)
			else
				dbo.GenerateAssetTypeUrl(O.AssetTypeID)
			end as ObjectUrl,
			O.Type as ObjectType,
			O.TypeID as ObjectTypeID,
			O.TypeName as ObjectTypeName,
			O.BackColor as ObjectIconBackColor,
			O.ForeColor as ObjectIconForeColor,
			O.Icon as ObjectIconText,

			I.PredicateID,
			I.PredicateType,
			case I.PredicateType
				when 1 then 'DataLineage'
				when 2 then 'ReferenceLineage'
				when 3 then 'InterTypeHierarchy'
				when 4 then 'IntraTypeHierarchy'
				when 5 then 'UserOwnership'
				when 6 then 'Grammar'
				when 7 then 'Simple'
				when 8 then 'FusionMapping'
				when 9 then 'SeeAlso'
				when 10 then 'Usage'
				when 11 then 'ObjectOwnerhip'
			end as PredicateTypeName,
			I.PredicateName,
			I.PredicateInverse
	from	PredicateIntersect I with(nolock)
			inner join (
				select A.ID as AssetID, A.AssetTypeID as AssetTypeID, coalesce(FA.TextPath,DisplayValue) as Name, Object, ObjectID, ForeColor, BackColor, Icon, Type, TypeID, TypeName from AssetDetail A
				left join FusionAttribute FA on FA.ID = A.ObjectID and A.Object = 'FusionAttribute'
				union all
				select null as AssetID, null as AssetTypeID, NI.Name as Name, 'Intersect' as Object, I.ID as ObjectID, null as ForeColor, null as BackColor, null as Icon, 'IntersectType' as Type, IntersectTypeID as TypeID, NIT.Name as TypeName from [Intersect] I
				inner join IntersectType T on T.ID = I.IntersectTypeID
				cross apply dbo.GetIntersectNames(I.ID) NI	
				cross apply dbo.GetIntersectTypeNames(T.ID) NIT
				union all
				select null as AssetID, TA.ID as AssetTypeID, TA.Name, TA.Object, TA.ObjectID, null as ForeColor, null as BackColor, null as Icon, 'ReferenceItemType' as Type, 0 as TypeID, TA.Name as TypeName from AssetType TA
				where TA.Object = 'ReferenceItemType'
			) S on S.Object = I.Subject and S.ObjectID = I.SubjectID
			inner join (
				select A.ID as AssetID, A.AssetTypeID as AssetTypeID, coalesce(FA.TextPath,DisplayValue) as Name, Object, ObjectID, ForeColor, BackColor, Icon, Type, TypeID, TypeName from AssetDetail A
				left join FusionAttribute FA on FA.ID = A.ObjectID and A.Object = 'FusionAttribute'
				union all
				select null as AssetID, null as AssetTypeID, NI.Name as Name, 'Intersect' as Object, I.ID as ObjectID, null as ForeColor, null as BackColor, null as Icon, 'IntersectType' as Type, IntersectTypeID as TypeID, NIT.Name as TypeName from [Intersect] I
				inner join IntersectType T on T.ID = I.IntersectTypeID
				cross apply dbo.GetIntersectNames(I.ID) NI	
				cross apply dbo.GetIntersectTypeNames(T.ID) NIT
				union all
				select null as AssetID, TA.ID as AssetTypeID, TA.Name, TA.Object, TA.ObjectID, null as ForeColor, null as BackColor, null as Icon, 'ReferenceItemType' as Type, 0 as TypeID, TA.Name as TypeName from AssetType TA
				where TA.Object = 'ReferenceItemType'
			) O on O.Object = I.Object and O.ObjectID = I.ObjectID
GO;

alter view [dbo].[SiteNavAvailable] as
	select
		u.ID,
		u.ObjectID as ObjectID,
		u.[Name],
		u.[url] as [Route],
		u.[Object],
		null as SortOrder,
		null as ParentID
	from
	(

		select
			A.ID,
			A.ObjectID,
			A.[Object],
			A.[Name],
			dbo.GenerateAssetTypeUrl(A.ID) As [url]
		from AssetType A
		where A.[Object] in ('ArtifactType', 'TaxonomyType', 'PolicyType', 'RuleType')
	) u
	left join SiteNav v on v.Object = u.Object and v.ObjectID = u.ObjectID
	where v.ObjectID is null and u.ID not in (
		select distinct 
			C.ChildAssetTypeID 
		from SiteNav n
		inner join AssetType T on T.Object = n.Object and T.ObjectID = n.Objectid
		cross apply dbo.GetAssetTypeChildrenByID(T.ID) C
	);
GO;

alter view [dbo].[SiteNavFlat] as
	select
		u.ID as ObjectID,
		u.Name,
		u.url as Route,
		u.Object,
		null as SortOrder,
		u.ParentID as ParentID
	from
	(
		select
		A.ID,
		A.ParentID,
		A.Name,
		dbo.GenerateAssetTYpeUrl(T.ID) As url,
		'ArtifactType' as [Object]
		FROM ArtifactType A
		inner join AssetType T on T.Object = 'ArtifactType' and T.ObjectID = A.ID
		
		UNION ALL
		
		SELECT
		A.ID,
		null as ParentID,
		A.Name,
		dbo.GenerateAssetTypeUrl(T.ID)  As url,
		'TaxonomyType' as [Object]
		FROM TaxonomyType A
		inner join AssetType T on T.Object = 'TaxonomyType' and T.ObjectID = A.ID

		UNION ALL
		
		SELECT
		A.ID,
		null as ParentID,
		A.Name,
		dbo.GenerateAssetTypeUrl(T.ID)  As url,
		'PolicyType' as [Object]
		FROM PolicyType A
		inner join AssetType T on T.Object = 'PolicyType' and T.ObjectID = A.ID
	) u
GO;

ALTER PROCEDURE [dbo].[GetAvailableSiteNavigation]
AS
BEGIN
	SET NOCOUNT ON;

	select
		u.ID as ObjectID,
		u.Name,
		u.url as Route,
		u.Object,
		null as SortOrder,
		null as ParentID
	from
	(
		select
		A.ID,
		A.Name,
		dbo.GenerateAssetTypeUrl(T.ID) As url,
		'ArtifactType' as [Object]
		FROM ArtifactType A
		inner join AssetType T on T.Object = 'ArtifactType' and T.ObjectID = A.ID

		UNION ALL
		
		SELECT
		A.ID,
		A.Name,
		dbo.GenerateAssetTypeUrl(T.ID)  As url,
		'TaxonomyType' as [Object]
		FROM TaxonomyType A
		inner join AssetType T on T.Object = 'TaxonomyType' and T.ObjectID = A.ID
		
		UNION ALL
		
		SELECT
		A.ID,
		A.Name,
		dbo.GenerateAssetTypeUrl(T.ID)  As url,
		'PolicyType' as [Object]
		FROM PolicyType A
		inner join AssetType T on T.Object = 'PolicyType' and T.ObjectID = A.ID
	) u
	left join SiteNav v on v.Object = u.Object and v.ObjectID = u.ID
	where v.ObjectID is null 
END
GO

ALTER PROCEDURE [dbo].[GetCommentDetailByID]
	@id int
AS
BEGIN
	with i (ResourceID) 
	as
	(
		select	r.ResourceID
		from	ResponsibilityDetail r
				inner join Comment c on c.OwnerObjectType = r.Object and c.OwnerObjectID = r.ObjectID and c.ID = @id
	),
	P (ID, ParentID)
	AS
	(
		SELECT		C.ID,
					C.ParentID
		FROM		Comment C
		WHERE		ID = @id
	)

	SELECT		C.*,
				C.CreatingResourceID,
				O.DisplayValue as ObjectName,				
				AUrl.Url as ObjectUrl,
				case
					WHEN C.ParentID IS NULL THEN C.OwnerObjectType
					ELSE 'Resource'
				end as ObjectType,
				O.DisplayValue as ResourceName,
				case 
					WHEN C.ParentID IS NULL THEN C.OwnerObjectID
					ELSE C.CreatingResourceID
				end as ObjectID,
				(
				select	CRD.Object,
						CRD.ObjectID,
						CRD.TextPath,
						CRD.ObjectTypeName,
						CRD.Url,
						CRD.ForeColor as IconForeColor,
						CRD.BackColor as IconBackColor,
						CRD.NgUrl
				from	CommentRelation CR
				inner join (
					select Object, ObjectID, ForeColor, BackColor, TypeName as ObjectTypeName, AUrl.Url as Url, AUrl.Url as NgUrl, DisplayValue as TextPath from AssetDetail A
					cross apply [dbo].[GetAssetUrlById](A.ID) AUrl
					union all
					select T.Object, T.ObjectID, OS.IconForeColor as ForeColor, OS.IconBackColor as BackColor, null as ObjectTypeName, TUrl.Url as Url, TUrl.Url as NgUrl, Name as TextPath from AssetType T
					cross apply [dbo].[GetAssetTypeUrlById](T.ID) TUrl
					left join ObjectStyle OS on OS.ObjectType = T.Object and OS.ObjectID = T.ObjectID
				) CRD on CR.CommentID = C.ID 
					and CR.ObjectType = CRD.[Object] 
					and CR.ObjectID = CRD.ObjectID
				where Object != 'Resource'
					and TextPath != 'FirstNameLastName'
				for xml path('tag'), root('tags'), type
				) as TagsXml,
				(
				select CommentID,
						ResourceID,
						vote as VoteValue
				from commentvote
				where commentid = p.ID
					for xml path('vote'), root('votes'), type
			) as VotesXML,
			CASE WHEN (select count(*) from i where ResourceID = C.CreatingResourceID) > 0  THEN
				cast(1 as bit)
			WHEN (select count(*) from i where ResourceID = C.CreatingResourceID) > 0  THEN
				cast(1 as bit)
			ELSE
				cast(0 as bit)
			END as CreatorIsOwner
	FROM		Comment C
				left join AssetDetail O on O.[Object] = C.OwnerObjectType and O.ObjectID = C.OwnerObjectID
				outer apply [dbo].[GetAssetUrlById](O.ID) AUrl
				INNER JOIN P ON C.ID = P.ID
	ORDER BY	C.ParentID, C.DateCreated DESC
END
GO;

ALTER FUNCTION [utility].[GetIntersectNames]
(	
	@id int
)
RETURNS TABLE 
AS
RETURN 
(
	SELECT	SA.DisplayValue + ' / ' + OA.DisplayValue as Name
	FROM	[Intersect] I
			left join AssetDetail SA on SA.Object = I.Subject and SA.ObjectID = I.SubjectID
			left join AssetDetail OA on OA.Object = I.Object and OA.ObjectID = I.ObjectID
	WHERE	I.ID = @id					
)
GO;

ALTER FUNCTION [utility].[ObjectDetail]
(
--declare
	@type varchar(50), 
	@id int
--set @type = 'Domain'
--set @id = 1
)
RETURNS @tbl TABLE 
(
	ID int,
	AssetID bigint,
	UID uniqueidentifier,
	AssetTypeID int,
	Name nvarchar(max),
	TextPath nvarchar(2500),
	Description nvarchar(max),
	ParentID int null,
	ParentType nvarchar(250),
	Url nvarchar(2500),
	TypeID int,
	[Type] varchar(25),
	[TypeName] nvarchar(250),
	IconBackColor varchar(15),
	IconForeColor varchar(15),
	IconText varchar(15),
	Status nvarchar(25) null
) 
AS
BEGIN
	if @type = 'Artifact' or @type = 'Attribute' or @type = 'Fusion' or @type = 'FusionAttribute' or @type = 'Policy' or @type = 'ReferenceItem' or @type = 'Rule' or @type = 'Taxonomy'
	begin
		insert into @tbl (	ID,		UID,	AssetID,	AssetTypeID, Name,			TextPath,		[Description],	ParentID,	ParentType, Url,											TypeID,	[Type],	TypeName, Status)
			SELECT			ObjectID,	UID, ID, 		AssetTypeID, DisplayValue,	DisplayValue,	NULL,			null,		null,		dbo.GenerateAssetUrl(ID),	TypeID,	Type,	TypeName, NULL
			FROM	AssetDetail
			where	Object = @type 
					and ObjectID = @id
	end

	if @type = 'ArtifactType' or @type = 'AttributeType' or @type = 'FusionType' or @type = 'FusionAttributeType' or @type = 'PolicyType' or @type = 'ReferenceItemType' or @type = 'RuleType' or @type = 'TaxonomyType'
	begin
		insert into @tbl (	ID,		UID, Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ObjectID, UID,		Name,	Name,		Description,	NULL,		NULL,		turl.[url] as Url,	ObjectID,		@type,	'Asset Type'
			FROM	AssetType O
			cross apply [dbo].GetAssetTypeUrlById(o.id) turl
			WHERE	Object = @type
					and ObjectID = @id
	end

	if @type = 'Group'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			G.ID,		G.Name,	G.Name,		G.Description,	NULL,		NULL,		dbo.GenerateAssetUrl(A.ID),	0,		@type,	'Group'
			FROM	[Group] G
			inner join Asset A on A.Object = 'Group' and A.ObjectID = G.ID
			WHERE	G.ID = @id
	end

	if @type = 'Intersect'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,													TypeID,				[Type],				TypeName)
			SELECT			O.ID,	IName.Name,	IName.Name,		'',				NULL,		@type,		null,	O.IntersectTypeID,	'IntersectType', ITN.Name	
			FROM	[Intersect] O
					INNER JOIN IntersectType T ON O.IntersectTypeID = T.ID and O.ID = @id
					CROSS APPLY dbo.GetIntersectNames(O.ID) IName	
					CROSS APPLY dbo.GetIntersectTypeNames(T.ID) ITN
	end

	if @type = 'IntersectType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		T.Name,	T.Name,		'',				NULL,		NULL,		null,	ID,		@type,	'Intersect Type'
			FROM	IntersectType 
			CROSS APPLY dbo.GetIntersectTypeNames(@id) T	
			WHERE	ID = @id
	end

	if @type = 'Issue'
	begin
		insert into @tbl (	ID,		Name,				TextPath,	[Description],	ParentID,	ParentType, Url,												TypeID,			[Type],			TypeName)
			SELECT			O.ID,	'',	'',		'',				NULL,		NULL,		null,	O.IssueTypeID,	'IssueType',	T.Name
			FROM	Issue O
					INNER JOIN IssueType T ON O.IssueTypeID = T.ID AND O.ID = @id
	end

	if @type = 'IssueType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,													TypeID,				[Type],				TypeName)
			SELECT			O.ID,	O.Name,	O.Name,		O.Description,				NULL,		@type,		NULL,	O.ID,	'IssueType',	'Issue Type'
			FROM	IssueType O
			WHERE	ID = @id
	end

	if @type = 'Lookup'
	begin
		insert into @tbl (	ID,		Name,				TextPath,	[Description],	ParentID,	ParentType, Url,												TypeID,			[Type],			TypeName)
			SELECT			O.ID,	T.Name + ' Item',	T.Name,		'',				NULL,		NULL,		dbo.GenerateUrlByTypeName(@type, O.LookupTypeID, O.ID),	O.LookupTypeID,	'LookupType',	T.Name
			FROM	[Lookup] O
					INNER JOIN LookupType T ON O.LookupTypeID = T.ID AND O.ID = @id
	end

	if @type = 'LookupType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		Name,	Name,		'',				0,			@type,		dbo.GenerateUrlByTypeName(@type, ID, 0),	ID,		@type,	'Lookup Type'
			FROM	LookupType O
			WHERE	ID = @id
	end

	if @type = 'FusionQueryAttribute'
	begin
		insert into @tbl (	ID,		Name,		TextPath,	[Description],	ParentID,	ParentType, Url,	TypeID,						[Type],					TypeName)
			SELECT			O.ID,	O.DisplayValue,	O.DisplayValue,	'',				NULL,	@type,		null,
																											O.FusionQueryAttributeTypeID,	'FusionQueryAttributeType',	T.Name
			FROM	FusionQueryAttribute O
					INNER JOIN FusionQueryAttributeType T ON O.FusionQueryAttributeTypeID = T.ID and O.ID = @id					
	end
	
	if @type = 'FusionQueryAttributeType'
	begin
		insert into @tbl (	ID, Name,		TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,	O.Name,	O.Name,	'',				NULL,		NULL,		null,	ID,		@type,	'Fusion Query Attribute Type'
			FROM	FusionQueryAttributeType O
			WHERE	ID = @id
	end

	if @type = 'Report'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,	TypeID,				[Type],			TypeName)
			SELECT			O.ID,	O.Name,	O.Name,	O.Description,	NULL,		@type,		'#',	0,	'Report',	'Report'
			FROM	Report O
			WHERE	O.ID = @id
	end

	if @type = 'Resource'
	begin
		insert into @tbl (ID, Name, Url, TypeID, [Type], TypeName)
			select	ResourceID, FirstName + ' ' + LastName, dbo.GenerateAssetUrl(A.ID), 1, 'ResourceType', 'Employee'
			from	reporting.Global_Resource R
			inner join Asset A on A.Object = 'Resource' and A.objectID = R.ResourceID
			where	ResourceID = @id
	end

	if @type = 'ResponsibilityType'
	begin
		insert into @tbl (	ID, Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,	O.Name,	NULL,		Description,	NULL,		NULL,		null,	ID,		@type,	'Responsibility Type'
			FROM	ResponsibilityType O
			WHERE	ID = @id
	end

	if @type = 'ResourceType'
	begin
		insert into @tbl (ID, Name, Url, TypeID, [Type], TypeName)
		values			(@id, 'Resource Type', '#/resources/administration', @id, @type, 'Resource Type')
	end

	if @type = 'RuleImplementation'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,	TypeID,				[Type],			TypeName, Status)
			SELECT			O.ID,	coalesce(O.Name,'Implementation ' + cast(o.id as nvarchar)) ,	coalesce(O.Name,'Implementation ' + cast(o.id as nvarchar)),	null,	T.ID,		'Rule',		null,	T.RuleTypeID,	'RuleType',	T.DisplayValue, 'Active'
			FROM	[RuleImplementation] O
					inner join [Rule] T on T.ID = O.RuleID
			WHERE	O.ID = @id
	end

	if @type = 'ShoppingCart'
	begin
			insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			O.ID,		Name,	Name,		NULL,	NULL,		NULL,		dbo.GenerateUrlByTypeName('ShoppingCartType', O.ShoppingCartTypeID, O.ID),	O.ID,		@type,	T.Name
			FROM	ShoppingCart O
			inner join ShoppingCartType T on O.ShoppingCartTypeID = T.ID
			WHERE	O.ID = @id
	end

	update	T
	set		T.IconBackColor = coalesce(S.IconBackColor, '#000000'),
			T.IconForeColor = coalesce(S.IconForeColor, '#ffffff'),
			T.IconText =	--case @type
							--	when 'Taxonomy' then 'IM'
							--	when 'TaxonomyType' then 'IM'
								--else 
								COALESCE(S.IconText, 'leaf') 
							--end
	from	@tbl T
			left join ObjectStyle S ON S.ObjectType = T.[Type] and S.ObjectID = T.TypeID

	RETURN
END
GO;

ALTER PROCEDURE [dbo].[GetRenderedTemplateBodyNg]
--declare
	@TemplateType varchar(25),
	@Type varchar(50),
	@ID int,
	@Action varchar(50),
	@SubjectName VARCHAR (200) = 'Governing Domain',
	@resourceId int = -1
--set @TemplateType = 'Lookup'
--set @Type = 'Artifact'
--set @ID = 7004--16435
--set @Action = 'Preview'--'Certificate'
AS
BEGIN
	SET NOCOUNT ON;

	declare @html nvarchar(max),
			@link nvarchar(2500),
			@icon nvarchar(250),
			@hasDynamicFields bit = 0,
			@hasStats bit = 0,
			@typeID int,

			@showIcon bit = 1,

			@current int,
			@max int,
			@name nvarchar(250),
			@value nvarchar(max);

	declare @tbl table (ID int identity, Name nvarchar(250), Value nvarchar(max));

	if @TemplateType = 'Tooltip'
	begin
		select	@html = TemplateBody
		from	TooltipTemplate
		where	Name = @Type
				and [Action] = @Action
	end

	-- Get the static tokens, depending on the type.
	declare @n nvarchar(250), @t nvarchar(250), @s nvarchar(25), @v int, @dc datetime, @du datetime, @d nvarchar(4000);

	-- Get common fields
	select	@typeID = C_D.TypeID,
			@icon = '<div title=''' + C_D.DisplayValue + ''' class=''tooltip-icon'' style=''background-color: ' + C_D.BackColor + '; color: ' + C_D.ForeColor + '''><i class=''fa fa-' + C_D.Icon + '''></i></div>',
			@n = C_D.DisplayValue,
			@t = C_D.TypeName,
			@d = f.formattedvalue,
			@link = AUrl.Url
	from	AssetDetail C_D	
			cross apply [dbo].[GetAssetUrlById](C_D.ID) AUrl
			left join fieldtype ft on (ft.[object] = C_D.[type] and ft.objectid = C_D.typeid and ft.name = 'Description')
			left join field f on (f.fieldtypeid = ft.id and f.[objecttype] = C_D.[object] and f.objectid = C_D.objectid)
	where	C_D.[Object] = @Type
			and C_D.ObjectID = @ID;

	--fusion attributes arent in cache
	if @Type = 'FusionAttribute'
	begin		
		select 
			@typeID = fa.fusionattributetypeid,
			@n = fa.name,
			@t = fat.Name,
			@link = dbo.GenerateAssetUrl(a.ID) 
		from fusionattribute fa 
		inner join Asset A on A.Object = 'FusionAttribute' and A.ObjectID = fa.ID
			inner join fusionattributetype fat on (fa.fusionattributetypeid = fat.id) 
		where fa.id = @ID
	end

	if @n is not null
	begin
		if @link is null
		begin
			insert into @tbl values ('Name', @n)
		end
		else
		begin
			insert into @tbl values ('Name', '<a routerLink="/' + @link + '">' + @n + '</a>')
		end
		insert into @tbl values ('Description', @d)
	end
	insert into @tbl values ('Type', @t)

	if @Action = 'AssigningItemPreview'
	begin
		set @html = '<h3>{Name}</h3>'
	end


	if @Action = 'LookupPreview'
	begin
		set @html = '{Items}'

		if @Type = 'FusionAttribute'
		begin
			-- BUILD LIST HTML -----------------------------------------
			declare @fusionAttributeItemsHtml nvarchar(max)

			set @fusionAttributeItemsHtml = '<div style="height: 200px; overflow-y: scroll"><table class="hoverable bordered striped" style="width:100%"><thead>'
			set @fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '<th style="margin-right: 15px">Name</th>'
			set @fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '</thead><tbody>'

			select		--top 10 
						@fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '<tr>' 
											+ '<td>' + Name + '</td>'
											+ '</tr>'
			from		FusionAttribute
			where		ParentID = @ID
			order by	Name asc

			set @fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '</tbody>'
			set @fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '</table></div>'

			insert into @tbl values ('Items', @fusionAttributeItemsHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'LookupType' OR @Type = 'Lookup'
		begin
			-- BUILD LOOKUP LIST HTML -----------------------------------------
			declare @lookups table (RowID int identity, ID int)

			declare @MyLookupTypeID int
			if @Type = 'Lookup'
				begin
					select @MyLookupTypeID = LookupTypeID from [Lookup] where ID = @ID 
				end
			else
				begin
					set @MyLookupTypeID = @ID
				end

			insert into @lookups 
				select top 10 ID from [Lookup] where LookupTypeID = @MyLookupTypeID order by ID desc

			declare @lookupFieldTypes table (ID int identity, Name nvarchar(250))
			insert into @lookupFieldTypes
				select FriendlyName from FieldType where [Object] = 'LookupType' and ObjectID = @MyLookupTypeID order by ColumnOrder asc

			declare @lookupHtml nvarchar(max)

			set @lookupHtml = '<table class="hoverable bordered striped" style="width:100%">'

			-- Loop through field name list ---------
			set @lookupHtml = @lookupHtml + '<thead>'
			set		@current = 1
			select	@max = max(ID) from @lookupFieldTypes
			while @current <= @max
			begin
				select	@name = Name
				from	@lookupFieldTypes
				where	ID = @current

				set @lookupHtml = @lookupHtml + '<th style="margin-right: 15px">' + @name  + '</th>'

				set @current = @current + 1
			end
			set @lookupHtml = @lookupHtml + '</thead>'
			-----------------------------------------

			set @lookupHtml = @lookupHtml + '<tbody>'

			-- Loop through event list --------------
			select	@current = min(RowID) from @lookups
			select	@max = max(RowID) from @lookups

			while @current <= @max
			begin
				set @lookupHtml = @lookupHtml + '<tr>'	-- Open row for selected event.

				declare @lookupFields table (Name nvarchar(250), Value nvarchar(4000))

				declare @lookupID int

				select	@lookupID = ID from @lookups where RowID = @current

				insert into @lookupFields
					select		FriendlyName,
								FormattedValue
					from		FieldWithRelation
					where		ObjectType = 'Lookup' 
								and ObjectID = @lookupID

					-- Loop through each field for this selected event --
					declare @lfCurrent int,
							@lfMax int
					set		@lfCurrent = 1
					select	@lfMax = max(ID) from @lookupFieldTypes
					while @lfCurrent <= @lfMax
					begin
						select	@name = Name from @lookupFieldTypes where ID = @lfCurrent

						select @lookupHtml = @lookupHtml + '<td>' + coalesce(Value, '') + '</td>' from @lookupFields where Name = @name

						set @lfCurrent = @lfCurrent + 1
					end
					-----------------------------------------------------

				delete @lookupFields

				set @lookupHtml = @lookupHtml + '</tr>'	-- Close off row for selected lookup.

				set @current = @current + 1
			end
			-----------------------------------------

			set @lookupHtml = @lookupHtml + '</tbody>'

			set @lookupHtml = @lookupHtml + '</table>'

			insert into @tbl values ('Items', @lookupHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'ReferenceItemType' OR @Type = 'ReferenceItem'
		begin
			-- BUILD LOOKUP LIST HTML -----------------------------------------
			declare @refs table (RowID int identity, ID int)
			declare @isHierarchy bit = 0;

			declare @MyRefTypeID int
			if @Type = 'ReferenceItem'
				begin
					select @MyRefTypeID = ReferenceItemTypeID from ReferenceItem where ID = @ID 

					-- check if this item is in a hierarchy if so set the flag as true
					select  @isHierarchy = count(1) from intersecttypedetail where [object] = 'ReferenceItemType' and [objectid] = @MyRefTypeID and predicatetype = 3
				end
			else
				begin
					set @MyRefTypeID = @ID
				end

			if @isHierarchy = 1 
				begin
				insert into @refs 					
					select	top 500 
							ri.ID 
					from	[ReferenceItem] ri
							inner join Asset ast on (ri.id = ast.objectid and ast.[object] = 'ReferenceItem')
							inner join [intersect] id on (id.objectid = ri.id and id.[object] = 'ReferenceItem')
							inner join [intersect] id_2 on (id_2.[object] = 'ReferenceItem' and id_2.[objectid] = @id and id_2.subjectid = id.subjectid)
							inner join [intersecttypedetail] it on (it.id = id.intersecttypeid and it.id = id_2.intersecttypeid and it.[object]='ReferenceItemType' and it.predicatetype = 3)
					where	ri.ReferenceItemTypeID = @MyRefTypeID 
							and ast.[State] = 1 
							and ast.ID not in (select AssetID from ResponsibilityDetail where ((PermissionsBitMask & 1) = 0) and ResourceID = @resourceId)
					order by DisplayValue asc
				end
			else
				begin
				insert into @refs 
					select	top 500 
							ri.ID 
					from	[ReferenceItem] ri
							inner join Asset ast on (ri.id = ast.objectid and ast.[object] = 'ReferenceItem')
					where	ri.ReferenceItemTypeID = @MyRefTypeID 
							and ast.[State] = 1 
							and ast.ID not in (select AssetID from ResponsibilityDetail where ((PermissionsBitMask & 1) = 0) and ResourceID = @resourceId)
					order by DisplayValue asc
				end

			declare @refFieldTypes table (ID int identity, Name nvarchar(250))
			insert into @refFieldTypes values ('Code')
			insert into @refFieldTypes
				select FriendlyName from FieldType where [Object] = 'ReferenceItemType' and ObjectID = @MyRefTypeID order by ColumnOrder asc

			declare @refHtml nvarchar(max)

			set @refHtml = '<table class="hoverable bordered striped" style="width:100%; min-width: 400px">'

			-- Loop through field name list ---------
			set @refHtml = @refHtml + '<thead>'
			set		@current = 1
			select	@max = max(ID) from @refFieldTypes
			while @current <= @max
			begin
				select	@name = Name
				from	@refFieldTypes
				where	ID = @current

				set @refHtml = @refHtml + '<th style="margin-right: 15px">' + @name  + '</th>'

				set @current = @current + 1
			end
			set @refHtml = @refHtml + '</thead>'
			-----------------------------------------

			set @refHtml = @refHtml + '<tbody>'

			-- Loop through event list --------------
			select	@current = min(RowID) from @refs
			select	@max = max(RowID) from @refs

			while @current <= @max
			begin
				set @refHtml = @refHtml + '<tr>'	-- Open row for selected event.

				declare @refFields table (Name nvarchar(250), Value nvarchar(4000))

				declare @refID int

				select	@refID = ID from @refs where RowID = @current

				insert into @refFields
					select	'Code', Code from ReferenceItem where ID = @refID

				insert into @refFields
					select		FriendlyName,
								FormattedValue
					from		FieldWithRelation
					where		ObjectType = 'ReferenceItem' 
								and ObjectID = @refID

					-- Loop through each field for this selected event --
					declare @rfCurrent int,
							@rfMax int,
							@rfCurrentVal nvarchar(max);

					set		@rfCurrent = 1
					select	@rfMax = max(ID) from @refFieldTypes
					while @rfCurrent <= @rfMax
					begin
						select	@name = Name from @refFieldTypes where ID = @rfCurrent

						if exists (select 1 from @refFields where Name = @name)
						begin
							select @refHtml = @refHtml + '<td>' + coalesce(Value, '1') + '</td>' from @refFields where Name = @name;
						end
						else
						begin
							set @refHtml = @refHtml + '<td>&nbsp;</td>';
						end

						set @rfCurrent = @rfCurrent + 1
					end
					-----------------------------------------------------

				delete @refFields

				set @refHtml = @refHtml + '</tr>'	-- Close off row for selected lookup.

				set @current = @current + 1
			end

			-----------------------------------------

			set @refHtml = @refHtml + '</tbody>'

			set @refHtml = @refHtml + '</table>'

			if @max >= 500
			begin
				set @refHtml = @refHtml + '<div style="font-weight:bold;padding-top:10px">Showing top 500 items</div>'	
			end

			insert into @tbl values ('Items', @refHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'Resource' OR @Type = 'ResourceType'
		begin
			-- BUILD Resource LIST HTML -----------------------------------------
			declare @resourceItemsHtml nvarchar(max)

			set @resourceItemsHtml = '<table class="hoverable bordered striped" style="width:100%"><thead>'
			set @resourceItemsHtml = @resourceItemsHtml + '<th style="margin-right: 15px">First Name</th><th style="margin-right: 15px">Last Name</th><th>Email</th>'
			set @resourceItemsHtml = @resourceItemsHtml + '</thead><tbody>'

			select		top 10 
						@resourceItemsHtml = @resourceItemsHtml + '<tr>' + 
											'<td>' + FirstName + '</td>' + 
											'<td>' + LastName + '</td>' + 
											'<td>' + Email + '</td>'
											+ '</tr>'
			from		reporting.Global_Resource
			order by	LastName, FirstName asc

			set @resourceItemsHtml = @resourceItemsHtml + '</tbody>'
			set @resourceItemsHtml = @resourceItemsHtml + '</table>'

			insert into @tbl values ('Items', @resourceItemsHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'ReferenceItem'
		begin

			declare @myReferenceListID int

			select	@myReferenceListID = ReferenceItemTypeID from ReferenceItem where ID = @ID
			-- BUILD LIST HTML -----------------------------------------
			declare @referenceItemHtml nvarchar(max)

			set @referenceItemHtml = '<table class="hoverable bordered striped" style="width:100%">'
			set @referenceItemHtml = @referenceItemHtml + '<thead><th style="margin-right: 15px">Name</th></thead>'
			set @referenceItemHtml = @referenceItemHtml + '<tbody>'



			select		top 10 
						@referenceItemHtml = @referenceItemHtml + '<tr>' + '<td>' + DisplayValue + '</td>' + '</tr>'             
			from		ReferenceItem
			where		ReferenceItemTypeID = @myReferenceListID
			order by	DisplayValue desc

			set @referenceItemHtml = @referenceItemHtml + '</tbody>'
			set @referenceItemHtml = @referenceItemHtml + '</table>'

			insert into @tbl values ('Items', @referenceItemHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'ReferenceItemType'
		begin

		--	declare @myReferenceListID int

			--select	@myReferenceListID = ReferenceItemTypeID from ReferenceItem where ID = @ID
			-- BUILD LIST HTML -----------------------------------------
			declare @referenceItemTypeHtml nvarchar(max)

			set @referenceItemTypeHtml = '<table class="hoverable bordered striped" style="width:100%">'
			set @referenceItemTypeHtml = @referenceItemTypeHtml + '<thead><th style="margin-right: 15px">Display Value</th></thead>'
			set @referenceItemTypeHtml = @referenceItemTypeHtml + '<tbody>'



			select		top 10 
						@referenceItemTypeHtml = @referenceItemTypeHtml + '<tr>' + '<td>' + DisplayValue + '</td>' + '</tr>'             
			from		ReferenceItem
			where		ReferenceItemTypeID = @ID
			order by	DisplayValue desc

			set @referenceItemTypeHtml = @referenceItemTypeHtml + '</tbody>'
			set @referenceItemTypeHtml = @referenceItemTypeHtml + '</table>'

			insert into @tbl values ('Items', @referenceItemTypeHtml)
			------------------------------------------------------------------
		end;

	end

	if @Action = 'None'
	begin
		set @html = '<h3>{Name}</h3><div>'
	end

	if @Action = 'Preview'
	begin
		set @html = '<h3 style="positon: relative">{Name} <small style="background-color: #fff; float:right;font-size:65%;">{Type}</small></h3><div>{Description}</div>'
		set @showIcon = 0

		if @Type = 'Artifact'
		begin
			declare @artifactPathHtml nvarchar(2500) = '<table>';
			declare @artLevelResult table(ID int identity, LevelName nvarchar(250), DisplayValue nvarchar(250), Url varchar(1000));

			with ap as (
				select	O.ID,
						O.ParentID,
						'INVALID' as DisplayValue,
						L.Name as LevelName,
						dbo.GenerateAssetUrl(A.ID) as Url,
						1 as [Level]
				from	Artifact O
						inner join Asset A on A.Object = 'Artifact' and A.ObjectID = O.ID
						inner join ArtifactType L on L.ID = O.ArtifactTypeID
				where	O.ID = @ID
				union all
				select	O.ID,
						O.ParentID,
						'INVALID' as DisplayValue,
						L.Name as LevelName,
						dbo.GenerateAssetUrl(A.ID) as Url,
						C.[Level] + 1 as [Level]
				from	Artifact O
						inner join Asset A on A.Object = 'Artifact' and A.ObjectID = O.ID
						inner join ArtifactType L on L.ID = O.ArtifactTypeID
						inner join ap as C on C.ParentID = O.ID
			)

			insert into @artLevelResult
				select LevelName, DisplayValue, Url from ap order by [Level] desc

			select		@artifactPathHtml = coalesce(@artifactPathHtml + '', '') + '<tr><td style="width: 15px">' +  cast([ID] as varchar) + '</td><td>' +  LevelName + '</td><td><b><a href="' + Url + '">' + DisplayValue + '</a></b>' + '</td></tr>'
			from		@artLevelResult

			set @artifactPathHtml =  @artifactPathHtml + '</table>'

			set @html = @html + '<div><b>Path:</b></div><div>' + coalesce(@artifactPathHtml,'') + '</div>'

			set @hasDynamicFields = 1
		end;

		if @Type = 'FusionAttribute'
		begin
			declare @faPathHtml nvarchar(2500) = '<table>';
			declare @faLevelResult table(ID int identity, LevelName nvarchar(250), Name nvarchar(250));

			with fap as (
				select	O.ID,
						O.ParentID,
						O.Name,
						L.Name as LevelName,
						1 as [Level]
				from	FusionAttribute O
						inner join FusionAttributeType L on L.ID = O.FusionAttributeTypeID
				where	O.ID = @ID
				union all
				select	O.ID,
						O.ParentID,
						O.Name,
						L.Name as LevelName,
						C.[Level] + 1 as [Level]
				from	FusionAttribute O
						inner join FusionAttributeType L on L.ID = O.FusionAttributeTypeID
						inner join fap as C on C.ParentID = O.ID
			)

			insert into @faLevelResult
				select LevelName, Name from fap order by [Level] desc

			select		@faPathHtml = @faPathHtml + 
						'<tr><td colspan="2">Configuration</td><td><b><a href="/fusion/' + cast(F.ID as nvarchar) + '">' + coalesce(F.Name,'') + '</a></b></td></tr>' 
			from		Fusion F 
						inner join FusionAttribute A on A.FusionID = F.ID and A.ID = @ID

			select		@faPathHtml = coalesce(@faPathHtml + '', '') + '<tr><td style="width: 15px">' +  cast(ID as varchar) + '</td><td>' +  LevelName + '</td><td><b>' + Name + '</b>' + '</td></tr>'
			from		@faLevelResult

			set @faPathHtml =  @faPathHtml + '</table>'

			set @html = @html + '<div><b>Path:</b></div><div>' + coalesce(@faPathHtml,'') + '</div>'

			set @hasDynamicFields = 1
		end

		if @Type = 'Intersect'
		begin
			set @hasDynamicFields = 1
		end;


		if @Type = 'Issue'
		begin
			insert into @tbl values('Name', '')
			insert into @tbl values('Description', '')

			if exists (select id from issue where id = @ID)
			begin			
				set @html = @html + '<div><b>Issue Type:</b> {IssueType}</div>'
				set @html = @html + '<div><b>Criticality:</b> {Criticality}</div>'

				insert into @tbl 
					select 'IssueType', it.name 
					from issuetype it inner join issue i on(i.issuetypeid = it.id) 
					where i.id = @ID

				insert into @tbl 
					select 'Criticality', case when i.Criticality = 0 then 'Negligible' when i.Criticality = 1 then 'Low' when i.Criticality = 2 then 'Medium' when i.Criticality = 3 then 'High'  when i.Criticality = 4 then 'Critical' else 'N/A' end
					from issuetype it inner join issue i on(i.issuetypeid = it.id) 
					where i.id = @ID

				set @hasDynamicFields = 1
			end			
		end;

		if @Type = 'Resource'
		begin
			--declare @e nvarchar(500)--, @fn nvarchar(250), @ln nvarchar(250)
			--select	@e = Email--, @fn = FirstName, @ln = LastName
			--from	reporting.Global_Resource
			--where	ResourceID = @ID

			--insert into @tbl values ('Email', @e)
			--insert into @tbl values ('FirstName', @fn)
			--insert into @tbl values ('LastName', @ln)
			--insert into @tbl values ('Role', '')

			--set @html = @html + '<div><b>Email:</b> {Email}</div>'
			--set @html = @html + '<div><b>First Name:</b> {FirstName}</div>'
			--set @html = @html + '<div><b>Last Name:</b> {LastName}</div>'

			set @hasDynamicFields = 1
		end;

		if @Type = 'Rule'
		begin
			insert into @tbl
				select	'Name', DisplayValue
				from	[Rule] O
				where	ID = @ID

			set @hasDynamicFields = 1
		end;

		if @Type = 'RuleDimension'
		begin
			insert into @tbl
				select	'Description', [Description]
				from	RuleDimension
				where	ID = @ID
			insert into @tbl
				select	'Name', [Name]
				from	RuleDimension
				where	ID = @ID

			--set @html = @html + '<div><b>Path:</b> {Description}</div>'

		end;

		if @Type = 'Taxonomy'
		begin
			declare @taxonomyPathHtml nvarchar(2500) = '<table>';

			with tp as (
				select	O.ID,
						O.ParentID,
						O.DisplayValue,
						coalesce(L.Name, 'Level ' + cast(O.[Level] as varchar)) as LevelName,
						O.[Level]
				from	[Taxonomy] O
						left join TaxonomyTypeLevel L on L.TaxonomyTypeID = O.TaxonomyTypeID and L.[Level] = O.[Level]
				where	O.ID = @ID
				union all
				select	O.ID,
						O.ParentID,
						O.DisplayValue,
						coalesce(L.Name, 'Level ' + cast(O.[Level] as varchar)) as LevelName,
						O.[Level]
				from	[Taxonomy] O
						outer apply (
									select Name from TaxonomyTypeLevel where TaxonomyTypeID = O.TaxonomyTypeID and [Level] = O.[Level]
									) L
						--left join TaxonomyTypeLevel L on L.TaxonomyTypeID = O.TaxonomyTypeID and L.[Level] = O.[Level]
						inner join tp as C on C.ParentID = O.ID
			)

			select		@taxonomyPathHtml = coalesce(@taxonomyPathHtml + '', '') + '<tr><td style="width: 15px">' +  cast([Level] as varchar) + '</td><td>' +  LevelName + '</td><td><b>' + DisplayValue + '</b>' + '</td></tr>'
			from		tp
			order by	[Level]

			set @taxonomyPathHtml =  @taxonomyPathHtml + '</table>'

			set @html = @html + '<div><b>Path:</b></div><div>' + coalesce(@taxonomyPathHtml,'') + '</div>'

			set @hasDynamicFields = 1
		end;

		if @Type = 'TaxonomyType'
		begin
			insert into @tbl
				select	'Name', Name
				from	TaxonomyType O
				where	ID = @ID

			set @hasDynamicFields = 1
		end;

		-- If required, get dynamic fields to add to list.
		if @hasDynamicFields = 1
		begin
			select	@html = @html + '<div><b>' + FriendlyName + '</b>: ' + '{' + Name + '}' + '</div>' 
			from	FieldWithRelation
			where	ObjectType = @Type
					and ObjectID = @ID
					and Name not in (select Name from @tbl)

			insert into @tbl
				select	Name,
						FormattedValue
				from	FieldWithRelation
				where	ObjectType = @Type
						and ObjectID = @ID
						and Name not in (select Name from @tbl)
		end;
	end

	if @Action = 'Statistics'
	begin
		set @html = '<h3>{Name}</h3><div>{Statistics}</div>'

		set @hasStats = case @Type
							when 'Artifact' then 1
							when 'Taxonomy' then 1
							else 0
						end

		-- If required, build statistics table
		if @hasStats = 1
		begin
			-- BUILD STATS LIST HTML -----------------------------------------
			declare @statsHtml nvarchar(max)

			declare @stats table (ID int identity, Name nvarchar(250), Score bit)

			--insert into @stats 
			--	select		G.Name + ': ' + I.Name,
			--				MR.Value
			--	from		metrics.ScoreItem S
			--				inner join metrics.MapResult MR on MR.ScoreID = S.ID and S.EffectiveEndDate = '12/31/9999' --and S.Object = @Type and S.ObjectID = @ID
			--				inner join metrics.Map M on M.ID = MR.MapID
			--				inner join metrics.[Group] G on G.ID = M.GroupID
			--				inner join metrics.Item I on I.ID = M.ItemID
			--	order by	G.Name + ': ' + I.Name

			set @statsHtml = '<table class="hoverable bordered striped" style="width:100%">'

			-- Loop through field name list ---------
			set @statsHtml = @statsHtml + '<tbody>'
			set		@current = 1
			select	@max = max(ID) from @stats
			while @current <= @max
			begin
				select	@statsHtml = @statsHtml + '<tr><td>' + Name  + '</td>' + '<td>' + case when Score = 1 then 'Pass' else 'Fail' end  + ' </td></tr>'
				from	@stats
				where	ID = @current

				set @current = @current + 1
			end
			set @statsHtml = @statsHtml + '</tbody>'
			-----------------------------------------

			insert into @tbl values ('Statistics', @statsHtml)

			------------------------------------------------------------------
		end;
	end

	if exists (select 1 from ResponsibilityDetail where ((PermissionsBitMask & 1) = 0) and resourceid = @resourceId and [object] = @Type and objectid = @ID)
	begin
		set @html = 'This item either does not exist or you do not have access to its details.';

		-- Return the properly formatted values.
		select	'' as Title,
				@html as Body;
	end
	else
	begin
		-- Replace the fields in the template with the appropriate text value.
		set		@current = 1
		select	@max = max(ID) from @tbl

		while @current <= @max
		begin
			select	@name = '{' + Name + '}',
					@value = COALESCE(Value, '')
			from	@tbl 
			where	ID = @current

			if @showIcon = 1
			begin
				if @name = '{Name}' and @icon is not null
				begin
					update	@tbl 
					set		Value = '<div class="pull-left" style="width: 30px">' + @icon + '</div>' + '<div class="pull-right">' + @value + '</div>'
					where	ID = @current
					--set @usedIconAlready = 1
				end
			end

			set @html = REPLACE(@html, @name, @value)

			set @current = @current + 1
		end

		--if @showIcon = 1 and @icon is not null
		--begin
		--	set @html = @icon + '<br/>' + @html
		--end

		set @html = '<div style="max-height: 500px; min-width: 400px; overflow-y: auto">' + @html + '</div>'

		-- Return the properly formatted values.
		select	'' as Title,
				@html as Body;
	end
END
GO;

ALTER FUNCTION [dbo].[ArtifactNgSiteNavigation](@id int)
RETURNS XML
WITH RETURNS NULL ON NULL INPUT
BEGIN 
	RETURN 
	(
	SELECT	name,
			url,
			'Menu_AT' + cast(id as varchar(15)) as menuID,
			0 as feature,
			case when @@NESTLEVEL > 24 then null else  dbo.ArtifactNgSiteNavigation(id) end as items
	FROM	(				
					select
						FAT.ID,
						FAT.Name,
						AUrl.[Url] as [Url]
					from	    ArtifactType FAT					
					inner join AssetType T on T.Object = 'ArtifactType' and T.ObjectID = FAT.ID
					outer apply (
							select	IT.SubjectID
							from	IntersectType IT 
									inner join [Predicate] P on IT.Object = T.Object and IT.ObjectID = FAT.ID and P.ID = IT.PredicateID and P.Type = 3
							) IT
					cross apply [dbo].[GetAssetTypeUrlById](T.ID) AUrl
					where IT.SubjectID = @id and FAT.ID <> @id
			--		) A
			) BG
			FOR XML PATH('nav'), TYPE
	)
END
GO;

ALTER FUNCTION [dbo].[GetArtifactParentByAssetID]
(
	@Id bigint
)
RETURNS TABLE 

AS
RETURN 
(	
	select	IAD.ObjectAssetID as ID,
			IAD.ObjectID as ObjectID,
			IAD.SubjectID as ParentID,
            ID.DisplayValue as ParentDisplayValue,						
			PUrl.Url as ParentUrl							
				    from	[utility].IntersectAsset IAD							
                            inner join dbo.Asset IA on IA.Object = 'Artifact' and IA.ObjectID = IAD.SubjectID and IAD.PredicateType = 3
                            inner join dbo.AssetType IAT on IAT.ID = IA.AssetTypeID
                            cross apply [dbo].[GetArtifactDisplayValue](IA.ID) ID
							cross apply dbo.GetAssetUrlById(IA.ID) PUrl
					where IAD.[Object] = 'Artifact' and IAD.ObjectAssetID = @Id
)
GO;

ALTER FUNCTION [dbo].[ArtifactSiteNavigation](@id int)
RETURNS XML
WITH RETURNS NULL ON NULL INPUT
BEGIN 
	RETURN 
	(
	SELECT	name,
			url,
			'Menu_AT' + cast(id as varchar(15)) as menuID,
			0 as feature,
			dbo.ArtifactSiteNavigation(id) as items
	FROM	(
			--SELECT	A.name,
			--		A.url,
			--		NULL AS items
			--FROM	(
					SELECT		TOP 1000
								a.id,
								a.name,
								dbo.GenerateAssetTypeurl(T.ID) As url
					FROM		ArtifactType  A
								inner join AssetType T on T.Object = 'ArtifactType' and T.ObjectID = A.ID
					WHERE		ParentID = @id
					ORDER BY	name
			--		) A
			) BG
			FOR XML PATH('nav'), TYPE
	)
END
GO;

ALTER PROCEDURE [dbo].[GetCommentDetailByID]
	@id int
AS
BEGIN
	with i (ResourceID) 
	as
	(
		select	r.ResourceID
		from	ResponsibilityDetail r
				inner join Comment c on c.OwnerObjectType = r.Object and c.OwnerObjectID = r.ObjectID and c.ID = @id
	),
	P (ID, ParentID)
	AS
	(
		SELECT		C.ID,
					C.ParentID
		FROM		Comment C
		WHERE		ID = @id
	)

	SELECT		C.*,
				C.CreatingResourceID,
				O.DisplayValue as ObjectName,				
				AUrl.Url as ObjectUrl,
				case
					WHEN C.ParentID IS NULL THEN C.OwnerObjectType
					ELSE 'Resource'
				end as ObjectType,
				O.DisplayValue as ResourceName,
				case 
					WHEN C.ParentID IS NULL THEN C.OwnerObjectID
					ELSE C.CreatingResourceID
				end as ObjectID,
				(
				select	CRD.Object,
						CRD.ObjectID,
						CRD.TextPath,
						CRD.ObjectTypeName,
						CRD.Url,
						CRD.ForeColor as IconForeColor,
						CRD.BackColor as IconBackColor,
						CRD.NgUrl
				from	CommentRelation CR
				inner join (
					select Object, ObjectID, ForeColor, BackColor, TypeName as ObjectTypeName, AUrl.Url as Url, AUrl.Url as NgUrl, DisplayValue as TextPath from AssetDetail A
					cross apply [dbo].[GetAssetUrlById](A.ID) AUrl
					union all
					select T.Object, T.ObjectID, OS.IconForeColor as ForeColor, OS.IconBackColor as BackColor, null as ObjectTypeName, TUrl.Url as Url, TUrl.Url as NgUrl, Name as TextPath from AssetType T
					cross apply [dbo].[GetAssetTypeUrlById](T.ID) TUrl
					left join ObjectStyle OS on OS.ObjectType = T.Object and OS.ObjectID = T.ObjectID
				) CRD on CR.CommentID = C.ID 
					and CR.ObjectType = CRD.[Object] 
					and CR.ObjectID = CRD.ObjectID
				where Object != 'Resource'
					and TextPath != 'FirstNameLastName'
				for xml path('tag'), root('tags'), type
				) as TagsXml,
				(
				select CommentID,
						ResourceID,
						vote as VoteValue
				from commentvote
				where commentid = p.ID
					for xml path('vote'), root('votes'), type
			) as VotesXML,
			CASE WHEN (select count(*) from i where ResourceID = C.CreatingResourceID) > 0  THEN
				cast(1 as bit)
			WHEN (select count(*) from i where ResourceID = C.CreatingResourceID) > 0  THEN
				cast(1 as bit)
			ELSE
				cast(0 as bit)
			END as CreatorIsOwner
	FROM		Comment C
				left join AssetDetail O on O.[Object] = C.OwnerObjectType and O.ObjectID = C.OwnerObjectID
				outer apply [dbo].[GetAssetUrlById](O.ID) AUrl
				INNER JOIN P ON C.ID = P.ID
	ORDER BY	C.ParentID, C.DateCreated DESC
END
GO;

ALTER PROCEDURE [dbo].[GetCommentDetailsByFollower]
--declare
	@resourceID int,
	@skip int,
	@take int,
	@dateStart datetime = null,
	@dateEnd datetime = null,
	@commentTypeID int = 0,
	@searchPhrase varchar(100) = ''
--set @resourceID = 1
--set @skip = 0
--set @take = 200
AS
BEGIN
	set nocount on;

	drop table if exists #commentIds;
	create table #commentIds (id int);

	insert into #commentIds
		select	CommentID as ID
		from	FollowDetail f
				inner join CommentRelation cr on cr.ObjectID = f.ObjectID and cr.ObjectType = f.ObjectType
		where	f.ResourceID = @resourceId
		union all
		select	ID 
		from	Comment 
		where	CreatingResourceID = @resourceid
		union all
		select	ID 
		from	comment c2
				inner join	ResponsibilityDetail o on o.ResourceID = @resourceID and o.Object = c2.OwnerObjectType and o.ObjectID = c2.OwnerObjectID;

	with p as
	(
	select	c.*,
			case 
				when c.CreatingResourceID = @resourceID then 1
				when c.VisibilityID = 2 then 1
				when c.VisibilityID = 3 then 1
				when coalesce(c.VisibilityID, 4) = 4  then 1
				else 0
			end as IsVisible
	from	Comment c
	inner join #commentIds S on S.ID = C.ID
	where   C.isdeleted = 0
			AND (
					coalesce(@commentTypeID,0) = 0 OR (C.CommentTypeID = @commentTypeID)
				) 
			AND (
					(C.DateCreated between @dateStart and @dateEnd and @dateStart is not null and @dateEnd is not null) or
					(@dateStart is null and @dateEnd is null)
				)
			AND C.ParentID is null
			AND (
				coalesce(ltrim(rtrim(@searchPhrase)),'')='' or 
				lower(Body) like lower('%'+@searchPhrase+'%')
				)
	order by c.datecreated DESC
	OFFSET		@skip ROWS 
	FETCH NEXT	@take ROWS ONLY
	)

	select	a.*,
			a.OwnerObjectType as ObjectType,
			a.OwnerObjectId as ObjectId,
			R.FirstName + ' ' + R.LastName as ResourceName,
			R.Email as ResourceEmail,
			D.DisplayValue as ObjectName,
			AUrl.Url as ObjectUrl,
			(
			select	CRD.Object,
					CRD.ObjectID,
					CRD.TextPath,
					CRD.ObjectTypeName,
					CRD.Url,
					CRD.BackColor as IconBackColor,
					CRD.ForeColor as IconForeColor,
					CRD.NgUrl
			from	CommentRelation CR
				inner join (
					select Object, ObjectID, ForeColor, BackColor, TypeName as ObjectTypeName, AUrl.Url as Url, AUrl.Url as NgUrl, DisplayValue as TextPath from AssetDetail A
					cross apply [dbo].[GetAssetUrlById](A.ID) AUrl
					union all
					select T.Object, T.ObjectID, OS.IconForeColor as ForeColor, OS.IconBackColor as BackColor, null as ObjectTypeName, TUrl.Url as Url, TUrl.Url as NgUrl, Name as TextPath from AssetType T
					cross apply [dbo].[GetAssetTypeUrlById](T.ID) TUrl
					left join ObjectStyle OS on OS.ObjectType = T.Object and OS.ObjectID = T.ObjectID
				) CRD on CR.CommentID = a.ID and a.ParentID is null and CR.ObjectType = CRD.[Object] and CR.ObjectID = CRD.ObjectID
			for xml path('tag'), root('tags'), type
			) as TagsXml,
			(
			select	CommentID,
					ResourceID,
					vote as VoteValue
			from	commentvote
			where	commentid = a.ID
			for		xml path('vote'), root('votes'), type
			) as VotesXML,
			0 as CreatorIsOwner
	from	(
			select	* 
			from	p
			union all
			select	r.*,
					1 as IsVisible 
			from	Comment r
					inner join p on r.ParentID = p.ID
			) a
			left join reporting.Global_Resource R on R.ResourceID = a.CreatingResourceID
			left join AssetDetail D on D.[Object] = a.OwnerObjectType and D.ObjectID = a.OwnerObjectID
			outer apply [dbo].[GetAssetUrlById](D.ID) AUrl
	where	IsVisible = 1;
END
GO;

ALTER PROCEDURE [dbo].[GetCommentDetailsByType]
--declare
	@type varchar(50), 
	@id int,
	@skip int,
	@take int,
	@dateStart datetime = null,
	@dateEnd datetime = null,
	@commentTypeID int = 0,
	@searchPhrase varchar(100) = ''
--set @type = 'Artifact'
--set @id = 733
--set @skip = 0
--set @take = 100
AS
BEGIN
	SET NOCOUNT ON;

	with i (ResourceID) 
	as
	(
		select	r.ResourceID
		from	ResponsibilityDetail r
				inner join Comment c on c.OwnerObjectType = r.Object and c.OwnerObjectID = r.ObjectID and c.ID = @id
	),
	 P
	AS
	(
		SELECT		C.*,
					CASE WHEN (select count(*) from i where ResourceID = C.CreatingResourceID) > 0  THEN
						1
					WHEN (select count(*) from i where ResourceID = C.CreatingResourceID) > 0  THEN
						1
					ELSE
						0
					END as CreatorIsOwner,
					coalesce(C.OwnerObjectType, CR.ObjectType) as ObjectType,
					coalesce(C.OwnerObjectID, CR.ObjectID) as ObjectID,
					(
					select	a.[Object],
							a.ObjectID,
							utility.getassetdisplayvalue(a.id) as TextPath,
							ast.Name as ObjectTypeName,							
							os.IconForeColor,
							os.IconBackColor,
							dbo.generateAsseturl(a.ID) as Url
					from	CommentRelation CR
							inner join asset a on (CR.CommentID = C.ID and a.[object] = CR.[ObjectType] and a.objectid = CR.ObjectID)
							inner join assettype ast on ( a.assettypeid = ast.id)
							inner join objectstyle os on (ast.[object] = os.[objecttype] and ast.[objectid] = os.[objectid])							
					for xml path('tag'), root('tags'), type
					) as TagsXml
		FROM		Comment C
					INNER JOIN CommentRelation CR	ON C.ID = CR.CommentID
													AND (
														coalesce(@commentTypeID,0) = 0 OR (C.CommentTypeID = @commentTypeID)
														) --in (1,2,3,7)
													AND CR.ObjectType = @type 
													AND CR.ObjectID = @id
													AND (
														(C.DateCreated between @dateStart and @dateEnd and @dateStart is not null and @dateEnd is not null) or
														(@dateStart is null and @dateEnd is null)
														)
													AND C.ParentID IS NULL	
													and c.isdeleted = 0			
		WHERE
			coalesce(ltrim(rtrim(@searchPhrase)),'')='' or (lower(Body) like lower('%'+@searchPhrase+'%')) 
		ORDER BY	C.DateCreated DESC
		OFFSET  @skip ROWS 
		FETCH NEXT @take ROWS ONLY 

		UNION ALL

		SELECT	C.*,
				0 as CreatorIsOwner, 
				cast('Resource' as varchar(50)) as ObjectType,
				C.CreatingResourceID as ObjectID,
				NULL as TagsXml
		FROM	P
				INNER JOIN Comment C ON C.ParentID = P.ID
	)

	select	P.*,
			R.FirstName + ' ' + R.LastName as ResourceName,
			R.Email as ResourceEmail,
			utility.getassetdisplayvalue(a.id),
			dbo.generateasseturl(a.id) as ObjectUrl,
			(
				select CommentID,
						ResourceID,
						vote as VoteValue
				from commentvote
				where commentid = p.ID
					for xml path('vote'), root('votes'), type
			) as VotesXML
	from	P
			left join reporting.Global_Resource R on R.ResourceID = P.CreatingResourceID			
			left join asset a on a.[object] = p.objecttype and a.objectid = p.objectid
			left join assettype ast on a.assettypeid = ast.id
	where
		isdeleted = 0;
END
GO;

ALTER proc [dbo].[GetPageInformation]
--declare 
	@o varchar(50),-- = 'Artifact',
	@oid int,-- = 23450,
	@rid int --= 1
as
begin
	declare @breadcrumbsRaw table ([Level] int, [TypeName] nvarchar(500), [Name] nvarchar(max), [TypeUrl] nvarchar(2500), [Url] nvarchar(2500));
	declare @breadcrumbs table ([Name] nvarchar(max), [Url] nvarchar(2500), Active bit, IsType bit);

	with h as
		(
		select	A.ID,
				A.[ObjectID], 
				A.AssetTypeID,
				I.SubjectID as [ParentID], 
				0 as [Level]
		from	Asset A
				left join PredicateIntersect I on I.Object = A.Object and I.ObjectID = A.ObjectID and I.PredicateType = 3
		where	A.[Object] = @o and A.ObjectID = @oid
		union all
		select	P.ID,
				P.[ObjectID] as ID, 
				P.AssetTypeID,
				I.SubjectID as ParentID, 
				h.[Level]-1 as [Level]
		from	Asset P
				inner join h on P.[Object] = @o and P.ObjectID = h.ParentID
				outer apply (
							select	SubjectID
							from	PredicateIntersect 
							where	Object = P.Object 
									and ObjectID = P.ObjectID 
									and PredicateType = 3
							) I
		)

	insert into @breadcrumbsRaw
		select		distinct	
					[Level],
					ltrim(rtrim(T.Name)),
					ltrim(rtrim(D.DisplayValue)),
					UT.Url,
					U.Url
		from		h 
					inner join AssetType T on T.ID = h.AssetTypeID
					left join dbo.GetAssetDisplayValue() D on D.ID = h.ID
					cross apply dbo.GetAssetUrlByID(h.ID) U
					cross apply dbo.GetAssetTypeUrlById(T.ID) UT
		where		ltrim(rtrim(T.Name)) is not null
					and ltrim(rtrim(D.DisplayValue)) is not null
		order by	[Level]

	declare @max int = 0,
			@min int
	select	@min = min([Level]) from @breadcrumbsRaw

	insert into @breadcrumbs values ('Glossary', null, 0, 0)

	while @min <= @max
	begin
		insert into @breadcrumbs
			select	TypeName, TypeUrl, 0, 1 from @breadcrumbsRaw where [Level] = @min

		insert into @breadcrumbs
			select	Name, 
					Url, 
					case @min when 0 then 1 else 0 end, 
					0 
			from	@breadcrumbsRaw 
			where	[Level] = @min

		set @min = @min + 1
	end

	select	distinct
			O.[Uid],
			A.ID,
			O.ID as AssetID,
			O.AssetTypeID,
			OD.DisplayValue,
			T.Name as [TypeName],
			case 
				when Dash.[Count] > 0 then cast(1 as bit)
				else cast(0 as bit)
			end as HasDashboards,
			case 
				when Work.[Count] > 0 then cast(1 as bit)
				else cast(0 as bit)
			end as HasWorkflow,
			case 
				when Child.[Count] > 0 then cast(1 as bit)
				else cast(0 as bit)
			end as HasChildArtifacts,
			case 
				when Attr.[Count] > 0 then cast(1 as bit)
				else cast(0 as bit)
			end as AllowAttributes,
			case 
				when Hier.[Count] > 0 then cast(1 as bit)
				else cast(0 as bit)
			end as AllowPredicateHierarchies,
			(
			select	*
			from	(
					select	P.ID as [ID],
							P.Name as [Name]
					from	[Predicate] P
					where	exists(SELECT * FROM IntersectType IT WHERE P.[type] = 6 and P.ID = IT.PredicateID and ((IT.Subject = T.Object and IT.SubjectID = T.ObjectID) OR (IT.Object = T.Object and IT.ObjectID =T.ObjectID)))
					union	
					select	P.ID as [ID], 
							P.Name as [Name] 
					from	[NymRelation] R 
							inner join [dbo].[predicate] P on P.ID = R.PredicateID where R.[Object] = T.Object and R.ObjectID = T.ObjectID
					) NMT
			for		json path
			)
			as NymTypes,
			(
			select	* 
			from	@breadcrumbs
			for		json path
			) as Breadcrumbs
	from	Artifact A 
			inner join Asset O on O.Object = @o and O.ObjectID = A.ID 
			inner join AssetType T on T.ID = O.AssetTypeID
			left join dbo.GetAssetDisplayValue() OD on OD.ID = O.ID
			--cross apply [dbo].GetAssetDisplayValueById(O.ID) as OD
			cross apply (
						select	count(1) as [Count]
						from	Report
						where	ObjectType = O.Object
								and ObjectID = T.ObjectID
						) Dash
			cross apply (
						select	count(1) as [Count]
						from	workflow.EventRegistration WER
								inner join workflow.Type WT on WER.TypeID = WT.ID and WT.PublishedVersionID is not null and WT.[State] = 1 and WER.ChangeType = 8 --ACTIVE
						where	WER.Object = T.Object
								and WER.ObjectID = T.ObjectID
						) Work
			cross apply (
						select	count(1) as [Count]
						from	[PredicateIntersect]
						where	Subject = O.Object
								and SubjectID = O.ObjectID
								and PredicateType = 3
						) Child
			cross apply (
						select	count(1) as [Count]
						from	AttributeTypeRelation
						where	ObjectType = T.Object and ObjectID = T.ObjectID
						) Attr
			cross apply (
						select	count(1) as [Count]
						from	IntersectType IT
								inner join [Predicate] P on P.ID = IT.PredicateID and P.[Type] = 3 -- TypeOf
						where	((IT.Subject = T.Object and IT.SubjectID = T.ObjectID) OR (IT.Object = T.Object and IT.ObjectID = T.ObjectID))
						) Hier
	where   A.ID = @oid 
			and A.[Visible] = 1 
			and not exists (select 1 from ResponsibilityDetail where PermissionsBitMask & 1 = 0 and ResourceID = @rid and ( (AssetID = O.ID) OR (AssetTypeID = O.AssetTypeID and AssetID = 0)))
	for json path, WITHOUT_ARRAY_WRAPPER
end
GO;

ALTER PROCEDURE [dbo].[GetSiteNavigation]
(
	@ResourceID int = 0
)
AS
BEGIN
	SET NOCOUNT ON;

SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		NULL AS Items	
FROM SiteNav n
WHERE n.Name = '#Monitor' AND dbo.HasSiteNavPermission(n.ID, @ResourceID) = 1
UNION ALL

SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		NULL AS Items		
FROM SiteNav n
WHERE n.Name = '#Home' AND dbo.HasSiteNavPermission(n.ID, @ResourceID) = 1
UNION ALL

SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		(		
			select
				FAT.name,
				AUrl.[Url] as [url],
				0 as feature,		
				dbo.ArtifactNgSiteNavigation(fat.id) as items
					from	    ArtifactType FAT					
						inner join AssetType T on T.Object = 'ArtifactType' and T.ObjectID = FAT.ID					
						cross apply [dbo].[GetAssetTypeUrlById](T.ID) AUrl
						left join SiteNav v on v.ObjectID = FAT.ID and v.Object = 'ArtifactType'
					where 
						not exists  (
							select	IT.SubjectID
							from	IntersectType IT 
									inner join [Predicate] P on IT.Object = T.Object and IT.ObjectID = FAT.ID and P.ID = IT.PredicateID and P.Type = 3
							) 	
							and v.ObjectID is null				
					FOR XML PATH('nav'), TYPE
		) AS Items
FROM SiteNav n
WHERE n.Name = '#Glossary' AND dbo.HasSiteNavPermission(n.ID, @ResourceID) = 1

UNION ALL


SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		(
			SELECT	name,
					url,
					0 as feature,
					null as items
			FROM	(
					SELECT		TOP 1000
								a.id,
								a.name,
								dbo.GenerateAssetTypeUrl(T.ID) As url
					FROM		TaxonomyType a
								inner join AssetType T on T.Object = 'TaxonomyType' and T.objectID = a.id
								left join SiteNav v on v.ObjectID = a.ID and v.Object = 'TaxonomyType'
					WHERE		v.ObjectID is null
					ORDER BY	a.name
					) BG
					FOR XML PATH('nav'), TYPE
		) AS Items
FROM SiteNav n
WHERE n.Name = '#Models' AND dbo.HasSiteNavPermission(n.ID, @ResourceID) = 1

UNION ALL

SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		(
			SELECT	name,
					url,
					0 as feature,
					null as items
			FROM	(
					SELECT		TOP 1000
								a.id,
								a.name,
								dbo.GenerateAssetTypeUrl(T.ID) As url
					FROM		PolicyType a
								inner join AssetType T on T.Object = 'PolicyType' and T.ObjectID = a.ID
								left join SiteNav v on v.ObjectID = a.ID and v.Object = 'PolicyType'
					WHERE		v.ObjectID is null
					ORDER BY	a.name
					) BG
					FOR XML PATH('nav'), TYPE
		) AS Items
FROM SiteNav n
WHERE n.Name = '#Policy' AND dbo.HasSiteNavPermission(n.ID, @ResourceID) = 1
		
UNION ALL

SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		null AS Items
FROM SiteNav n
WHERE n.Name = '#Reference' AND dbo.HasSiteNavPermission(n.ID, @ResourceID) = 1

UNION ALL

SELECT	n.Name as MenuID,
		n.SortOrder,
		2 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		(
		SELECT		FT.name, 
					dbo.GenerateAssetTypeUrl(T.ID)  As url,
					2 as feature,
					(
					SELECT		name, 
								dbo.GenerateAssetUrl(A.ID)  As url,
								'F' + cast(F.ID as varchar(15)) as menuID,
								2 as feature
					FROM		Fusion F
								inner join Asset A on A.Object = 'Fusion' and A.ObjectID = F.ID
					WHERE		F.FusionTypeID = FT.ID
					ORDER BY	name
					FOR XML PATH('nav'), TYPE
					) AS items	
		FROM		FusionType FT
					inner join AssetType T on T.Object = 'FusionType' and T.ObjectID = FT.ID
		ORDER BY	name
		FOR XML PATH('nav'), TYPE
		) AS Items	
	FROM SiteNav n
WHERE n.Name = '#Fusion' AND dbo.HasSiteNavPermission(n.ID, @ResourceID) = 1
		
UNION ALL

SELECT	n.Name as MenuID, 
		n.SortOrder,
		4 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		(
        SELECT	'People' AS name, --'#People' as MenuID,
                'community/groups' AS url, 		        
                0 as feature,
		        NULL AS Items
        FOR XML PATH('nav'), TYPE
        ) AS Items
FROM SiteNav n
WHERE n.Name = '#Community' AND dbo.HasSiteNavPermission(n.ID, @ResourceID) = 1
UNION ALL

SELECT	'#Admin' as MenuID,
		999 as SortOrder,
		0 as Feature,
		'fa-cogs' as Icon,
		'Administration' as Title,
		(
			select	*
			from	(
					SELECT	'Security' AS name, 
							'#/' AS url, 
							0 as feature,
							(
							select	*
							from	(
									SELECT	'Groups' AS name, 
											'#/groups/administration' AS url, 
											--'Menu_A_S_G' as menuID,
											0 as feature,
											NULL AS items
									union all
									SELECT	'Users' AS name, 
											'#/resources/administration' AS url, 
											--'Menu_A_S_R' as menuID,
											0 as feature,
											NULL AS items
									union all
									SELECT	'Responsibilities' AS name, 
											'#/governance/administration' AS url, 
											0 as feature,
											NULL AS items
                            ) bg
							FOR XML PATH('nav'), TYPE
							) AS items
						
					union all

					SELECT	'MetaModel' AS name, 
							'#/' AS url,
							0 as feature, 
							(
							select	*
							from	(
									SELECT	'Artifacts' AS name, 
											'#/artifacts/administration' AS url, 
											0 as feature,
											NULL AS items
									union all
									SELECT	'Attributes' AS name, 
											'#/attributes/administration' AS url, 
											0 as feature,
											NULL AS items
									union all
									SELECT	'Lookups' AS name, 
											'#/lookups/administration' AS url, 
											0 as feature,
											NULL AS items
									union all
									SELECT	'Models' AS name, 
											'#/catalogs/administration' AS url, 
											0 as feature,
											NULL AS items
                                    union all
									SELECT	'Policies' AS name, 
											'#/policies/administration' AS url, 
											1 as feature,
											NULL AS items
                                    union all
									SELECT	'Relationships' AS name, 
											'#/relations/administration' AS url, 
											0 as feature,
											NULL AS items
                                    union all
                                    SELECT	'Rules' AS name, 
											'#/rules/administration' AS url, 
											0 as feature,
											NULL AS items
									) bg
							FOR XML PATH('nav'), TYPE
							) AS items
						
					union all

					SELECT	'Metrics' AS name, 
							'#/' AS url,
							0 as feature, 
							(
							select	*
							from	(
									SELECT	'Scoring' AS name, 
											'#/analytics/administration' AS url, 
											5 as feature,
											NULL AS items
									union all
					                SELECT	'Dashboards' AS name, 
							                '#/reporting/administration' AS url, 
							                0 as feature,
							                NULL AS items
                                    union all
					                SELECT	'Surveys' AS name, 
							                '#/surveys/administration' AS url, 
							                7 as feature,
							                (
							                SELECT	'Response Types' AS name, 
									                '#/surveyresponsetypes/administration' AS url, 
									                7 as feature,
									                NULL AS items
							                FOR XML PATH('nav'), TYPE
							                ) AS items
									) bg
							FOR XML PATH('nav'), TYPE
							) AS items
						
					union all

					SELECT	'Reference' AS name, 
							'#/domains/administration' AS url, 
							0 as feature,
							NULL AS items

					union all

					SELECT	'Workflow' AS name, 
							'#/workflow/administration' AS url, 
							0 as feature,
							NULL AS items

                    union all

                    SELECT	'Templates' AS name, 
							'#/templates/administration' AS url, 
							0 as feature,
							NULL AS items

					union all

					SELECT	'Integration' AS name, 
							'#/' AS url, 
							0 as feature,
							(
							select	*
							from	(
									SELECT	'Bulk Loader' AS name, 
											'#/load' AS url, 
											0 as feature,
											NULL AS items
									union all
									SELECT	'Fusion' AS name, 
											'#/fusion/administration' AS url, 
											2 as feature,
											NULL AS items
									union all
									SELECT	'API' AS name, 
											'/swagger' AS url, 
											0 as feature,
											NULL AS items
									) bg
							FOR XML PATH('nav'), TYPE
							) AS items

                    union all

                    SELECT	'Settings' AS name, 
							'#/settings' AS url, 
							0 as feature,
							NULL AS items
            ) bg
			for xml path('nav'), type
		) as Items

	where 1 = 1

	UNION ALL

	SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		(
		SELECT	RT.name, 				
				dbo.GenerateAssetTypeUrl(T.ID) As url,
				0 as feature,
				null AS items	
		FROM	RuleType RT
				inner join AssetType T on T.Object = 'RuleType' and T.ObjectID = RT.ID
				LEFT JOIN SiteNav v on v.ObjectID = RT.ID and v.Object ='RuleType'
		WHERE	v.ObjectID IS NULL
		FOR XML PATH('nav'), TYPE
		) AS Items
	FROM SiteNav n
	WHERE n.Name = '#Data Quality' AND dbo.HasSiteNavPermission(n.ID, @ResourceID) = 1

	UNION ALL

	SELECT 
		'~' + Name AS MenuID,
		s.SortOrder,
		0 AS Feature,
		s.Icon as Icon,
		s.Title as Title,
		dbo.CustomSiteNavigation(ID) AS Items
	from SiteNav s
	where ParentID IS NULL and Name not like '#%' AND dbo.HasSiteNavPermission(s.ID, @ResourceID) = 1

	order by sortorder
END
GO;



------------------------------------------------------------------
-- GOV-6040
-- Bulk load users fails when it encounters a field with a null asset id
-- cause the assetid of resources that have null assetid's to get updated by the trigger
------------------------------------------------------------------

update field set updatedon = getutcdate() where assetid is null and objecttype = 'Resource'
go;

------------------------------------------------------------------
-- GOV-6013
-- Handle TaxonomyType lookup fields
------------------------------------------------------------------
ALTER procedure [asset].[BulkUpsert]
--declare 
	@isInsert bit,
	@uid uniqueidentifier,
	@r int
as
begin
	set nocount on;
/*
	-- test to set parameters
	declare @isInsert bit = 1, @uid uniqueidentifier = 'A9B94F4B-14F6-474F-9572-80F954C8FC59', @r int = 1

	--TESTING LOGIC

	drop table if exists #AssetTable;
	create table #AssetTable (
		ItemNumber int not null,

		Uid uniqueidentifier null,
		AssetID bigint null,
		Object varchar(50) null,
		ObjectID int null,
		KeyHash varchar(50) null,

		ParentUid uniqueidentifier null,
		ParentObject varchar(50) null,
		ParentObjectID int null,

		[Message] nvarchar(2500) null,
		Success bit null,
		IsNew bit null
	);
	drop table if exists #AssetFieldTable;
	create table #AssetFieldTable (
		ItemNumber int not null,
		FieldName nvarchar(250) not null,
		FieldValue nvarchar(max) null,
		FieldTypeID int null,
		LookupValue nvarchar(250) null
	);
	
	insert into #AssetTable (ItemNumber, [Uid]) values (1, null);--'AC8AE7C0-8CD0-482D-AC44-DB05502150B3');
	insert into #AssetTable (ItemNumber, ParentUid) values (1, null);--'AC8AE7C0-8CD0-482D-AC44-DB05502150B3');
	
	insert into #AssetFieldTable (ItemNumber, FieldName, FieldValue) values (1, 'Name', 'Pappas loads with asset.BulkUpsert');
	insert into #AssetFieldTable (ItemNumber, FieldName, FieldValue) values (1, 'PersonalDataFlag', 'true');
	insert into #AssetFieldTable (ItemNumber, FieldName, FieldValue) values (1, 'GDPRCompliant', 'false');
	insert into #AssetFieldTable (ItemNumber, FieldName, FieldValue) values (1, 'CDE', 'false');
	insert into #AssetFieldTable (ItemNumber, FieldName, FieldValue) values (1, 'SpecialData', 'true');
	insert into #AssetFieldTable (ItemNumber, FieldName, FieldValue) values (1, 'Status', 'In progress');
	insert into #AssetFieldTable (ItemNumber, FieldName, FieldValue) values (1, 'SubjectArea', 'Investments');
	select * from AssetType where Object = 'ArtifactType'
	s
	select	top 100 percent
			A.ItemNumber,
			coalesce(F.LookupValue, F.FieldValue) as [Value]
	from	#AssetTable A
			inner join FieldType FT on FT.AssetTypeID = @at 
			inner join #AssetFieldTable F on F.ItemNumber = A.ItemNumber 
											and F.FieldTypeID = FT.ID 
											and FT.IsPartOfKey = 1
											and A.Success is null -- We have not failed yet.
	order by FT.ColumnOrder	

*/
	declare @ot varchar(50),
			@otid int,
			@at int,
			@class int,
			@parentIntersectTypeUid uniqueidentifier,
			@parentIntersectTypeID int,
			@parentOt varchar(50),
			@parentOtId int
	select	@ot = Object,
			@otid = ObjectID,
			@at = ID,
			@class = [Class] 
	from	AssetType
	where	[uid] = @uid

	--Determine if there should be a parent present.
	select	@parentIntersectTypeUid = I.[Uid],
			@parentIntersectTypeID = I.ID,
			@parentOt = I.Subject,
			@parentOtId = I.SubjectID
	from	IntersectType I
			inner join [Predicate] P on P.ID = I.PredicateID 
									and I.Object = @ot
									and I.ObjectID = @otid
									and P.[Type] = case @ot
														when 'PolicyType' then 4
														when 'TaxonomyType' then 4
														else 3 --InterTypeHierarchy
													end

	-- Resolve the FieldTypeIDs for the fields you have added.
	update	T
	set		T.FieldTypeID = S.ID
	from	#AssetFieldTable T
			inner join FieldType S on S.AssetTypeID = @at and S.Name = T.FieldName
	----------------------------------------------------------

	BEGIN 
		-- Validation checks ----------

		-- 0. Did user pass any UIDs when this is an INSERT-only action?
		if @isInsert = 1
		begin
			update	#AssetTable
			set		Success = 0,
					[Message] = coalesce([Message] + '; ', '') + 'You may not provide a Uid for this asset when you are attempting to add it'
			where	[Uid] is not null 
		end;

		-- 0. Did user pass proper Uids when this is an UPDATE-only action?
		if @isInsert = 0
		begin
			update	#AssetTable
			set		Success = 0,
					[Message] = coalesce([Message] + '; ', '') + 'You must provide a valid Uid for this asset when you are attempting to update it'
			where	[Uid] is null or [Uid] = CAST(CAST(0 AS BINARY) AS UNIQUEIDENTIFIER) -- (empty guid)
		end;

		-- 0. Did user pass any Parent Uids when this is an UPDATE-only action?
		if @isInsert = 0
		begin
			update	#AssetTable
			set		Success = 0,
					[Message] = coalesce([Message] + '; ', '') + 'You may not provide a Parent Uid for this asset when you are attempting to update it'
			where	[ParentUid] is not null 
		end;

		-- 0. If parents required and this is an INSERT command, make sure there is a parentUid present and it is valid.
		IF @parentIntersectTypeID is not null and @isInsert = 1
		BEGIN
			update	#AssetTable
			set		Success = 0,
					[Message] = coalesce([Message] + '; ', '') + 'Asset is missing a required ParentUid value'
			where	ParentUid is null;

			update	T
			set		T.ParentObject = S.Object,
					T.ParentObjectID = S.ObjectID
			from	#AssetTable T
					inner join Asset S on S.[Uid] = T.ParentUid and T.ParentUid is not null
					inner join AssetType ST on ST.ID = S.AssetTypeID and ST.Object = @parentOt and ST.ObjectID = @parentOtId;

			update	#AssetTable
			set		Success = 0,
					[Message] = coalesce([Message] + '; ', '') + 'Asset does not contain a valid ParentUid value'
			where	ParentObjectID is null
					and ParentUid is not null;
		END;

		-- 1. Does asset have all the key fields defined?
		--if @isInsert = 1
		--begin
			update	T
			set		T.Success = 0,
					T.[Message] = coalesce(T.[Message] + '; ', '') + 'Asset is missing key field(s): [' + S.Names + ']'
			from	#AssetTable T
					inner join	(
								select	A.ItemNumber,
										STRING_AGG(FT.Name, ', ') as Names
								from	#AssetTable A
										inner join FieldType FT on FT.AssetTypeID = @at 
																	and FT.IsPartOfKey = 1
										left join #AssetFieldTable F on F.ItemNumber = A.ItemNumber and F.FieldTypeID = FT.ID
								where	F.ItemNumber is null
								group by A.ItemNumber
								) S on S.ItemNumber = T.ItemNumber;
		--end;

		-- 2. Does asset have all required fields defined?
		if @isInsert = 1
		begin
			update	T
			set		T.Success = 0,
					T.[Message] = coalesce(T.[Message] + '; ', '') + 'Asset is missing required field(s): [' + S.Names + ']'
			from	#AssetTable T
					inner join	(
								select	A.ItemNumber,
										STRING_AGG(FT.Name, ', ') as Names
								from	#AssetTable A
										inner join FieldType FT on FT.AssetTypeID = @at 
																	and FT.IsRequired = 1
										left join #AssetFieldTable F on F.ItemNumber = A.ItemNumber and F.FieldTypeID = FT.ID 
								where	F.ItemNumber is null
								group by A.ItemNumber
								) S on S.ItemNumber = T.ItemNumber
		end;

		-- 3. Are all lookup fields valid, based on field's LookupEditFormat, or LookupDisplayFormat?

		--- A. Get the valid lookup values.
		--the query below is SUPER slow, using the one below that just looks for reference list lookups for now.
		--update	T
		--set		T.LookupValue = S.[Value]
		--from	#AssetFieldTable T
		--		inner join FieldType F on F.ID = T.FieldTypeID and F.[Type] = 'Lookup'
		--		inner join FieldLookupValue S on S.FieldTypeID = F.ID and S.[Text] = T.FieldValue
		update	T
		set		T.LookupValue = RI.ID
		from	#AssetFieldTable T
				inner join FieldType F on F.ID = T.FieldTypeID and F.[Type] = 'Lookup'
				inner join ReferenceItem RI ON F.LookupObjectType = 'ReferenceItem' and F.LookupObjectID = RI.ReferenceItemTypeID 
					and T.FieldValue = utility.GetFormattedFieldLookupValue(F.Type, coalesce(F.LookupEditFormat, F.LookupDisplayFormat), F.LookupObjectType, F.LookupObjectID, RI.ID);
		
		update	T
		set		T.LookupValue = RI.ID
		from	#AssetFieldTable T
				inner join FieldType F on F.ID = T.FieldTypeID and F.[Type] = 'Lookup'
				inner join ReferenceItemType RI ON F.LookupObjectType = 'ReferenceItemType'
					and T.FieldValue = utility.GetFormattedFieldLookupValue(F.Type, coalesce(F.LookupEditFormat, F.LookupDisplayFormat), F.LookupObjectType, F.LookupObjectID, RI.ID);

		update	T
		set		T.LookupValue = RI.ID
		from	#AssetFieldTable T
				inner join FieldType F on F.ID = T.FieldTypeID and F.[Type] = 'Lookup'
				inner join TaxonomyType RI ON F.LookupObjectType = 'TaxonomyType'
					and T.FieldValue = utility.GetFormattedFieldLookupValue(F.Type, coalesce(F.LookupEditFormat, F.LookupDisplayFormat), F.LookupObjectType, F.LookupObjectID, RI.ID);

		--- B. Check which fields do not have a valid lookup value from query above.
		update	T
		set		T.Success = 0,
				T.[Message] = coalesce(T.[Message] + '; ', '') + 'Asset contains one or more fields with invalid lookup values: [' + S.Names + ']'
		from	#AssetTable T
				inner join	(
							select		A.ItemNumber,
										STRING_AGG(FT.Name+'='+F.FieldValue, ', ') as Names
							from		#AssetTable A
										inner join FieldType FT on FT.AssetTypeID = @at 
																	and FT.[Type] = 'Lookup'
										inner join #AssetFieldTable F on F.ItemNumber = A.ItemNumber and F.FieldTypeID = FT.ID and F.LookupValue is null
							group by	A.ItemNumber
							) S on S.ItemNumber = T.ItemNumber;

		-- 4. Are all values valid based on field's data type?
		update	T
		set		T.Success = 0,
				T.[Message] = coalesce(T.[Message] + '; ', '') + 'Asset contains one or more field that are invalid based on their data types: [' + S.Names + ']'
		from	#AssetTable T
				inner join	(
							select	A.ItemNumber,
									STRING_AGG(FT.Name + ' is ' + FT.[Type] + ' but has a value of ' + F.FieldValue, ', ') as Names
							from	#AssetTable A
									inner join FieldType FT on FT.AssetTypeID = @at 
									inner join #AssetFieldTable F on F.ItemNumber = A.ItemNumber 
																	and F.FieldTypeID = FT.ID 
																	and (
																		(FT.[Type] = 'Boolean' and LOWER(F.FieldValue)  not in ('false', 'true')) or 
																		(FT.[Type] = 'Date' and ISDATE(F.FieldValue) = 0) or 
																		(FT.[Type] = 'DateTime' and ISDATE(F.FieldValue) = 0) or 
																		(FT.[Type] = 'Number' and ISNUMERIC(F.FieldValue + '.e0') = 0) or 
																		(FT.[Type] = 'Decimal' and ISNUMERIC(F.FieldValue) = 0) or 
																		(FT.[Type] = 'Link' and (CHARINDEX('|', F.FieldValue, 0) = 0 OR CHARINDEX('|', F.FieldValue, 0) is null) ) or 
																		(FT.[Type] = 'Percentage' and ISDATE(F.FieldValue) = 0)
																	)
							group by A.ItemNumber
							) S on S.ItemNumber = T.ItemNumber;

		-- 5. Check if length populated, if so is the field's length valid?
		update	T
		set		T.Success = 0,
				T.[Message] = coalesce(T.[Message] + '; ', '') + 'Asset contains one or more field that have an invalid length: [' + S.Names + ']'
		from	#AssetTable T
				inner join	(
							select	A.ItemNumber,
									STRING_AGG(FT.Name + ' must have an exact length of ' + cast(FT.[Length] as nvarchar), ', ') as Names
							from	#AssetTable A
									inner join FieldType FT on FT.AssetTypeID = @at 
									inner join #AssetFieldTable F on F.ItemNumber = A.ItemNumber 
																	and F.FieldTypeID = FT.ID 
																	and FT.[Length] is not null
																	and FT.[Length] <> LEN(F.FieldValue)
							group by A.ItemNumber
							) S on S.ItemNumber = T.ItemNumber;

		-- 6. Check if minimum length populated, if so is the field's minimum length valid?
		update	T
		set		T.Success = 0,
				T.[Message] = coalesce(T.[Message] + '; ', '') + 'Asset contains one or more field that have an invalid minimum length: [' + S.Names + ']'
		from	#AssetTable T
				inner join	(
							select	A.ItemNumber,
									STRING_AGG(FT.Name + ' must have a minimum length of ' + cast(FT.[MinimumLength] as nvarchar), ', ') as Names
							from	#AssetTable A
									inner join FieldType FT on FT.AssetTypeID = @at 
									inner join #AssetFieldTable F on F.ItemNumber = A.ItemNumber 
																	and F.FieldTypeID = FT.ID 
																	and FT.[MinimumLength] is not null
																	and FT.[MinimumLength] > LEN(F.FieldValue)
							group by A.ItemNumber
							) S on S.ItemNumber = T.ItemNumber;

		-- 7. Check if maximum length populated, if so is the field's maximum length valid?
		update	T
		set		T.Success = 0,
				T.[Message] = coalesce(T.[Message] + '; ', '') + 'Asset contains one or more field that have an invalid maximum length: [' + S.Names + ']'
		from	#AssetTable T
				inner join	(
							select	A.ItemNumber,
									STRING_AGG(FT.Name + ' must have a maximum length of ' + cast(FT.[MaximumLength] as nvarchar), ', ') as Names
							from	#AssetTable A
									inner join FieldType FT on FT.AssetTypeID = @at 
									inner join #AssetFieldTable F on F.ItemNumber = A.ItemNumber 
																	and F.FieldTypeID = FT.ID 
																	and FT.[MaximumLength] is not null
																	and FT.[MaximumLength] < LEN(F.FieldValue)
							group by A.ItemNumber
							) S on S.ItemNumber = T.ItemNumber;

		-- 8. If regex defined, validate against the Pattern field as defined on FieldType.
		-- TODO: perhaps implement a CLR function here.
		-- https://stackoverflow.com/questions/194652/sql-server-regular-expressions-in-t-sql

		-- 9. If KeyHash matches an asset with a different UID than the one provided (IF provided), throw an error.
	
		--- A. First, figure out what the hash should be, if this is an insert
		--if @isInsert = 1
		--begin
			update	T
			set		T.KeyHash = S.KeyHash
			from	#AssetTable T
					inner join	(
								select	O.ItemNumber,
										utility.GetHash(coalesce(convert(varchar(36), O.ParentUid)+'|','') + STRING_AGG(O.Value, '|')) as KeyHash
								from	(
										select	top 100 percent
												A.ItemNumber,
												A.ParentUid,
												coalesce(F.LookupValue, F.FieldValue) as [Value]
										from	#AssetTable A
												inner join FieldType FT on FT.AssetTypeID = @at 
												inner join #AssetFieldTable F on F.ItemNumber = A.ItemNumber 
																				and F.FieldTypeID = FT.ID 
																				and FT.IsPartOfKey = 1
																				and A.Success is null -- We have not failed yet.
										order by FT.ColumnOrder						
										) O
								group by O.ItemNumber,
										O.ParentUid
								) S on S.ItemNumber = T.ItemNumber;
		--end

		--- B. Next, validate the hash against the object we are trying to update.
		if @isInsert = 1
		begin
			update	T
			set		T.Success = 0,
					T.[Message] = coalesce(T.[Message] + '; ', '') + 'Asset contains an error: [' + S.Error + ']'
			from	#AssetTable T
					inner join	(
								select	T.ItemNumber,
										'Key values match another asset under a different set of key fields.' as Error
								from	#AssetTable T
										inner join AssetKeyHash K on K.AssetTypeID = @at and K.KeyHash = T.KeyHash
								) S on S.ItemNumber = T.ItemNumber;
		end
		else
		begin 
			update	T
			set		T.Success = 0,
					T.[Message] = coalesce(T.[Message] + '; ', '') + 'Asset contains an error: [' + S.Error + ']'
			from	#AssetTable T
					inner join	(
								select	T.ItemNumber,
										'Key values match another asset under a different public uid.' as Error
								from	#AssetTable T
										inner join AssetKeyHash K on K.AssetTypeID = @at and K.KeyHash = T.KeyHash and T.[Uid] <> K.[Uid]
								) S on S.ItemNumber = T.ItemNumber;
		end


		-- 10. Check if there are duplicate nodes in the JSON. We want to make sure we only allow the first through, and fail the dupes.
		update	T
		set		T.Success = 0,
				T.[Message] = coalesce(T.[Message] + '; ', '') + 'Asset with matching key hash is already referenced previously. Nodes must be unique within a load.'
		from	#AssetTable T
				inner join	(
							select	min(ItemNumber) as ItemNumber,
									KeyHash
							from	#AssetTable
							group by KeyHash
							) S on S.KeyHash = T.KeyHash and S.ItemNumber < T.ItemNumber;

	END	-------------------------------

	-- Now upsert the valid assets.
	drop table if exists #ObjectMergeTableResult;
	create table #ObjectMergeTableResult (ID int, ItemNumber int, [Action] nvarchar(10));
	CREATE NONCLUSTERED INDEX IX_TempObjectMergeTableResult ON #ObjectMergeTableResult ( ItemNumber ASC );

	if @isInsert = 0
	begin
		update	T
		set		T.Object = S.Object,
				T.ObjectID = S.ObjectID,
				T.AssetID = S.ID
		from	#AssetTable T
				inner join Asset S on S.[Uid] = T.[Uid]
	end;

	declare @current int = 1,	-- to track which ItemNumber row you are on.
			@max int = 0,
			@objectId int

	select @max = max(ItemNumber) from #AssetTable

	IF @class = 1 --GLOSSARY
	BEGIN
		if @isInsert = 1
		begin
			while @current <= @max
			begin
				if exists(select ItemNumber from #AssetTable where ItemNumber = @current and Success is null and ObjectID is null)
				begin
					insert Artifact(ArtifactTypeID, CreatedOn, UpdatedBy, UpdatedOn, Visible)
					values (@otid, getutcdate(), @r, getutcdate(), 1);
				
					set	@objectId = SCOPE_IDENTITY()

					update	T
					set		T.Object ='Artifact',
							T.ObjectID = @objectId,
							T.AssetID = S.ID,
							T.[Uid] = S.[Uid],
							T.IsNew = 1
					from	#AssetTable T
							inner join Asset S on S.Object = 'Artifact' and S.ObjectID = @objectId and T.ItemNumber = @current; 
				end
				set @current = @current + 1
			end
		end
		else
		begin
			update	T
			set		T.UpdatedBy = @r,
					T.UpdatedOn = getutcdate()
			from	Artifact T
					inner join #AssetTable S on S.ObjectID = T.ID;

			update	#AssetTable
			set		IsNew = 0
			where	Success is null;
		end
	END;

	IF @class = 2 --MODEL
	BEGIN
		if @isInsert = 1
		begin
			while @current <= @max
			begin
				if exists(select ItemNumber from #AssetTable where ItemNumber = @current and Success is null and ObjectID is null)
				begin
					insert Taxonomy(TaxonomyTypeID, UpdatedBy, UpdatedOn, Visible)
					values (@otid, @r, getutcdate(), 1);
				
					set	@objectId = SCOPE_IDENTITY()

					update	T
					set		T.Object ='Taxonomy',
							T.ObjectID = @objectId,
							T.AssetID = S.ID,
							T.[Uid] = S.[Uid],
							T.IsNew = 1
					from	#AssetTable T
							inner join Asset S on S.Object = 'Taxonomy' and S.ObjectID = @objectId and T.ItemNumber = @current; 
				end
				set @current = @current + 1
			end
		end
		else
		begin
			update	T
			set		T.UpdatedBy = @r,
					T.UpdatedOn = getutcdate()
			from	Taxonomy T
					inner join #AssetTable S on S.ObjectID = T.ID;

			update	#AssetTable
			set		IsNew = 0
			where	Success is null;
		end
	END;

	IF @class = 4 --FUSION ATTRIBUTE
	BEGIN
		if @isInsert = 1
		begin
			while @current <= @max
			begin
				declare @fusionId int,
						@fusionName nvarchar(250)

				select	@fusionId = cast(F.FieldValue as int),
						@fusionName = N.FieldValue
				from	#AssetTable A
						inner join #AssetFieldTable N on N.ItemNumber = A.ItemNumber and N.FieldName = 'Name'
						inner join #AssetFieldTable F on F.ItemNumber = A.ItemNumber and F.FieldName = 'FusionID'
				where	A.Success is null -- no errors from validation
						and A.ObjectID is null
						and A.ItemNumber = @current;

				if @fusionId is not null and @fusionName is not null
				begin
					set	@objectId = NEXT VALUE FOR [dbo].[FusionAttribute_Seq]

					insert FusionAttribute(ID, FusionAttributeTypeID, Name, FusionID)
					values (@objectId, @otid, @fusionName, @fusionId);

					update	T
					set		T.Object ='FusionAttribute',
							T.ObjectID = @objectId,
							T.AssetID = S.ID,
							T.[Uid] = S.[Uid],
							T.IsNew = 1
					from	#AssetTable T
							inner join Asset S on S.Object = 'FusionAttribute' and S.ObjectID = @objectId and T.ItemNumber = @current; 
				end

				set @current = @current + 1
			end
		end
		else
		begin
			update	#AssetTable
			set		IsNew = 0
			where	Success is null;
		end
	END;

	IF @class = 6 --POLICY
	BEGIN
		if @isInsert = 1
		begin
			while @current <= @max
			begin
				if exists(select ItemNumber from #AssetTable where ItemNumber = @current and Success is null and ObjectID is null)
				begin
					insert [Policy](PolicyTypeID, UpdatedBy, UpdatedOn, Visible)
					values (@otid, @r, getutcdate(), 1);
				
					set	@objectId = SCOPE_IDENTITY()

					update	T
					set		T.Object ='Policy',
							T.ObjectID = @objectId,
							T.AssetID = S.ID,
							T.[Uid] = S.[Uid],
							T.IsNew = 1
					from	#AssetTable T
							inner join Asset S on S.Object = 'Policy' and S.ObjectID = @objectId and T.ItemNumber = @current;
				end
				set @current = @current + 1
			end
		end
		else
		begin
			update	T
			set		T.UpdatedBy = @r,
					T.UpdatedOn = getutcdate()
			from	[Policy] T
					inner join #AssetTable S on S.ObjectID = T.ID;

			update	#AssetTable
			set		IsNew = 0
			where	Success is null;
		end
	END;

	IF @class = 7 --RULE
	BEGIN
		if @isInsert = 1
		begin
			while @current <= @max
			begin
				if exists(select ItemNumber from #AssetTable where ItemNumber = @current and Success is null and ObjectID is null)
				begin

					insert [Rule](RuleTypeID, UpdatedBy, UpdatedOn, Visible)
					values (@otid, @r, getutcdate(), 1);
				
					set	@objectId = SCOPE_IDENTITY()

					update	T
					set		T.Object ='Rule',
							T.ObjectID = @objectId,
							T.AssetID = S.ID,
							T.[Uid] = S.[Uid],
							T.IsNew = 1
					from	#AssetTable T
							inner join Asset S on S.Object = 'Rule' and S.ObjectID = @objectId and T.ItemNumber = @current;

				end
				set @current = @current + 1 
			end
		end
		else
		begin
			update	T
			set		T.UpdatedBy = @r,
					T.UpdatedOn = getutcdate()
			from	[Rule] T
					inner join #AssetTable S on S.ObjectID = T.ID;

			update	#AssetTable
			set		IsNew = 0
			where	Success is null;
		end
	END;

	IF @class = 9 --REFERENCE
	BEGIN
		if @isInsert = 1
		begin
			--while @current > 0 and @current is not null
			while @current <= @max
			begin
				--set @current = 0

				declare @code nvarchar(250)

				select		top 1
							--@current = A.ItemNumber,
							@code = F.FieldValue-- + ' ' + cast(A.ItemNumber as nvarchar)
				from		#AssetTable A
							inner join #AssetFieldTable F on F.ItemNumber = A.ItemNumber and F.FieldName = 'Code'
				where		A.Success is null -- no errors from validation
							and A.ObjectID is null
							and A.ItemNumber = @current
				--order by	A.ItemNumber;
				
				if @code is not null
				begin
					insert ReferenceItem(ReferenceItemTypeID, Code, UpdatedBy, UpdatedOn, Visible)
					values (@otid, @code, @r, getutcdate(), 1);
				
					set	@objectId = SCOPE_IDENTITY()

					update	T
					set		T.Object ='ReferenceItem',
							T.ObjectID = @objectId,
							T.AssetID = S.ID,
							T.[Uid] = S.[Uid],
							T.IsNew = 1
					from	#AssetTable T
							inner join Asset S on S.Object = 'ReferenceItem' and S.ObjectID = @objectId and T.ItemNumber = @current
				end

				set @current = @current + 1
			end
		end
		else
		begin
			update	T
			set		T.UpdatedBy = @r,
					T.UpdatedOn = getutcdate()
			from	ReferenceItem T
					inner join #AssetTable S on S.ObjectID = T.ID;

			update	#AssetTable
			set		IsNew = 0
			where	Success is null;
		end
	END;

	/*
	-- testing
	declare @isInsert bit = 1, @uid uniqueidentifier = 'A9B94F4B-14F6-474F-9572-80F954C8FC59', @r int = 1
	declare @ot varchar(50),
			@otid int,
			@at int,
			@class int,
			@parentIntersectTypeUid uniqueidentifier,
			@parentIntersectTypeID int,
			@parentOt varchar(50),
			@parentOtId int
	select	@ot = Object,
			@otid = ObjectID,
			@at = ID,
			@class = [Class] 
	from	AssetType
	where	[uid] = @uid
	*/

	-- Merge the parent/child relationships if required.
	IF @parentIntersectTypeID is not null and @isInsert = 1
	BEGIN
		-- Remove parent/child records that are no longer valid for the assets we are loading.
		delete	T
		from	[Intersect] T
				inner join #AssetTable S on T.IntersectTypeID = @parentIntersectTypeID 
											and S.Object = T.Object 
											and S.ObjectID = T.ObjectID 
											and (S.ParentObject <> T.Subject OR S.ParentObjectID <> T.SubjectID)
											and S.Object is not null 
											and S.ObjectID is not null 
											and S.ParentObject is not null 
											and S.ParentObjectID is not null;

		-- Merge parent/child relationships.
		merge into  [Intersect] T
		using		(
					select      *
					from        #AssetTable
					where		Object is not null 
								and ObjectID is not null 
								and ParentObject is not null 
								and ParentObjectID is not null
								and Success is null	-- We have not failed in validation.
                ) S
		on      ( T.IntersectTypeID = @parentIntersectTypeID and S.Object = T.Object and S.ObjectID = T.ObjectID )
		when not matched by target then
			insert  (IntersectTypeID, Subject, SubjectID, Object, ObjectID, CreatedBy, UpdatedBy)
			values  (@parentIntersectTypeID, S.ParentObject, S.ParentObjectID, S.Object, S.ObjectID, @r, @r);
	END;

	-- Merge field data ---------------------------
	merge into  Field T
    using       (
                select  distinct 
                        A.AssetID,
						A.Object, 
                        A.ObjectID, 
                        F.FieldTypeID,
                        coalesce(F.LookupValue, F.FieldValue) as Value
                from    #AssetFieldTable F
                        inner join #AssetTable A on A.ItemNumber = F.ItemNumber 
                            and A.ObjectID is not null 
                            and F.FieldTypeID is not null
							and A.Success is null	-- We have not failed in validation.
                ) S
    on          (
                    T.FieldTypeID = S.FieldTypeID and 
                    T.AssetID = S.AssetID
                )
    when		matched then
	update		set
					T.Value = S.Value,
					T.FormattedValue = S.Value
    when		not matched by target then
	insert		(FieldTypeID, ObjectType, ObjectID, AssetID, Value, FormattedValue)
    values		(S.FieldTypeID, S.Object, S.ObjectID, S.AssetID, S.Value, S.Value);
	-----------------------------------------------

	update	#AssetTable
	set		Success = 1
	where	Success is null
			and Object is not null
			and ObjectID is not null;

	select * from #AssetTable
	--select * from #AssetFieldTable
	--update #AssetTable set Success = null
	--update #AssetFieldTable set LookupValue = null
end
GO;
go




------------------------------------------------------------------
-- GOV-5569
-- Update nav tooltip and icon (if it has not already been changed to something else)
------------------------------------------------------------------
update SiteNav
set Title = 'Workflow'
where [Name] = '#Monitor';
go

update SiteNav
set Icon = 'fa-usb'
where [Name] = '#Monitor' and Icon = 'fa-television'; --don't override customizations
go