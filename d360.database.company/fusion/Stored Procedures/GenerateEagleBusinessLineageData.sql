create procedure [fusion].[GenerateEagleBusinessLineageData]
	@eagleOwnerArtifact int = 974209
as
begin
	SET NOCOUNT, ANSI_PADDING ON;
	SET ANSI_WARNINGS ON;

	declare @bloombergFusionTypeId int = 8;
	--declare @eagleOwnerArtifact int = 974209;

	IF OBJECT_ID('tempdb..#maps') IS NOT NULL
		DROP TABLE #maps;

	create table #maps (	
		ID int identity primary key,
		MapItemID int,	
		MapRuleItemID int,		
		FusionAttributeID int,
		FusionAttributeTypeID int,
		Object varchar(50),
		ObjectID int,
		ObjectArtifactTypeID int,
		ObjectFusionID int,
		ObjectOwnerArtifactID int,
		SourceIntersectID int,
		TargetIntersectID int,
		IsRelatedToBloomberg int
	);


	insert into #maps
	(FusionAttributeID, ObjectID, Object, ObjectFusionID, FusionAttributeTypeID, MapRuleItemID)
	select 
		mri.SourceFusionAttributeID as 'FusionAttributeID', 
		i.ObjectID as 'ObjectID',
		i.Object as 'Object'
		,f.FusionID
		,f.FusionAttributeTypeID
		,mri.id
	from 
		mapruleitem  mri
		inner join [intersect] i on (mri.SourceFusionAttributeID = i.SubjectID and i.Subject = 'FusionAttribute' and i.Object = 'Artifact')		
		inner join [dbo].[fusionattribute] f on f.id = mri.SourceFusionAttributeID and f.deleted = 0
	where 
		mri.[owner] = 'EAGLE LINEAGE'
	union
	select 
		mri.SourceFusionAttributeID as 'FusionAttributeID', 
		i.SubjectID as 'ObjectID',
		i.Subject as 'Object'
		,f.FusionID
		,f.FusionAttributeTypeID
		,mri.id
	from 
		mapruleitem  mri
		inner join [intersect] i on (mri.SourceFusionAttributeID = i.ObjectID and i.Object = 'FusionAttribute' and i.Subject = 'Artifact')
		inner join [dbo].[fusionattribute] f on f.id = mri.SourceFusionAttributeID and f.deleted = 0
	where 
		mri.[owner] = 'EAGLE LINEAGE'
	union
	select 
		mri.TargetFusionAttributeID as 'FusionAttributeID', 
		i.ObjectID as 'objectID',
		i.Object as 'Object'
		,f.FusionID
		,f.FusionAttributeTypeID
		,mri.id
	from 
		mapruleitem  mri
		inner join [intersect] i on (mri.TargetFusionAttributeID = i.SubjectID and i.Subject = 'FusionAttribute' and i.Object = 'Artifact')
		inner join [dbo].[fusionattribute] f on f.id = mri.TargetFusionAttributeID and f.deleted = 0
	where 
		mri.[owner] = 'EAGLE LINEAGE'
	union
	select 
		mri.TargetFusionAttributeID as 'FusionAttributeID', 
		i.SubjectID as 'ObjectID',
		i.Subject as 'Object'
		,f.FusionID
		,f.FusionAttributeTypeID
		,mri.id
	from 
		mapruleitem  mri
		inner join [intersect] i on (mri.TargetFusionAttributeID = i.ObjectID and i.Object = 'FusionAttribute' and i.Subject = 'Artifact')
		inner join [dbo].[fusionattribute] f on f.id = mri.TargetFusionAttributeID and f.deleted = 0
	where 
		mri.[owner] = 'EAGLE LINEAGE';

	
	-- set the owner artifact of the fusion ids
	update T
	 set T.ObjectOwnerArtifactID = f.ArtifactID
	 from #maps T
		inner join [dbo].[fusionowner] f on f.fusionid = T.objectfusionid


	update #maps set IsRelatedToBloomberg = 1 where fusionattributetypeid = 301;
	--delete the items that start with bloomberg
	--delete from #maps where fusionattributetypeid = 301;
	--update #maps set IsRelatedToBloomberg = 1 where fusionattributetypeid = 301;
	-- for owners objects that are not fusionattributetypeid 301 we need to see if they connect to bloomberg
	-- use source to target until we find end or 301

	declare @tFusionPoints table (	ID int, IsBB int);
	-- backward items
				with cte as (
					select		m.ID,                                           
                                I.SourceFusionAttributeID,
                                I.TargetFusionAttributeID,
                                1 as [Level],
								1 as SourceFusionAttributeTypeID,
								1 as TargetFusionAttributeTypeID								
                    from   MapRuleItem I                                
								inner join #maps m on (m.FusionAttributeID = I.SourceFusionAttributeID or m.FusionAttributeID = I.TargetFusionAttributeID) and m.FusionAttributeTypeID != 301
                    						   
					union all
					select	T.ID,
							S.SourceFusionAttributeID,
							S.TargetFusionAttributeID,
							T.[Level] + 1 as [Level],							
							SFA.FusionAttributeTypeID as SourceFusionAttributeTypeID,
							TFA.FusionAttributeTypeID as TargetFusionAttributeTypeID
					from	MapRuleItem S
							inner join cte T on T.SourceFusionAttributeID = S.TargetFusionAttributeID and S.ID <> T.ID
							inner join FusionAttribute SFA on SFA.ID = S.SourceFusionAttributeID and SFA.Deleted = 0
                            inner join FusionAttribute TFA on TFA.ID = S.TargetFusionAttributeID and TFA.Deleted = 0
					where	T.[Level] <= 25
				)
				insert into @tFusionPoints
					select distinct	ID, 							
							1
					from	cte 
					where	cte.SourceFusionAttributeTypeID = 301 or cte.TargetFusionAttributeTypeID = 301;

	update T
		 set T.IsRelatedToBloomberg = 1
		 from #maps T
			inner join @tFusionPoints f on f.ID = T.ID;

	update T
		set T.ObjectArtifactTypeID = A.ArtifactTypeID
		from #maps T
		inner join artifact A on (A.id = T.ObjectID);

	declare @bloombergOwnerArtifactId int = 0;

	select 
		top 1 @bloombergOwnerArtifactId = fo.artifactid
	from
		fusion f
		inner join fusionowner fo on (f.fusiontypeid = @bloombergFusionTypeId and fo.fusionid = f.id);


	--------------------------------------------------------------------------
	-- Eagle -> Bloomberg - Source is bloomberg Target is eagle
	--------------------------------------------------------------------------

	update T
	set T.[targetintersectid] = OI.ID
	from #maps T		
		inner join [IntersectDetail] OI on (OI.[Object] = T.[Object] and OI.ObjectID = T.[ObjectID] and OI.[Subject] = 'Artifact' and OI.[SubjectID] = T.ObjectOwnerArtifactID and T.TargetIntersectID is null  and T.fusionAttributeTypeId != 301);

	-- add any missing relations to source / object
	insert into [intersect] (IntersectTypeID, Classification, [Subject], SubjectID, [Object], ObjectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
		select distinct
			(select top 1 i_t.ID from [intersecttype] i_t where (i_t.[object] = c_s.objecttype and i_t.[subject] = c_t.objecttype and i_t.objectid = c_s.objecttypeid and i_t.subjectid = c_t.objecttypeid) /*or 
				(i_t.[subject] = c_s.objecttype and i_t.[object] = c_t.objecttype and i_t.subjectid = c_s.objecttypeid and i_t.objectid = c_t.objecttypeid)*/)
			,2			
			,'Artifact'
			,T.ObjectOwnerArtifactID
			,T.[object]
			,T.[objectID]			
			,0,getutcdate(),0,getutcdate(),'EAGLE BUSINESS LINEAGE'
		from #maps T		
		inner join [cache].[objectdetails] c_s on (c_s.[object] = T.[object] and c_s.[objectid] = T.[objectid])
		inner join [cache].[objectdetails] c_t on (c_t.[object] = 'Artifact' and c_t.[objectid] = T.ObjectOwnerArtifactID)
		where T.targetIntersectID is null  and T.fusionAttributeTypeId != 301;

	update T
	set T.[targetintersectid] = OI.ID
	from #maps T		
		inner join [IntersectDetail] OI on (OI.[Object] = T.[Object] and OI.ObjectID = T.[ObjectID] and OI.[Subject] = 'Artifact' and OI.[SubjectID] = T.ObjectOwnerArtifactID and T.TargetIntersectID is null  and T.fusionAttributeTypeId != 301);


	-- source intersects for eagle use bloomberg default
	update T
	set T.[sourceintersectid] = OI.ID
	from #maps T		
		inner join [IntersectDetail] OI on (OI.[Object] = T.[Object] and OI.ObjectID = T.[ObjectID] and OI.[Subject] = 'Artifact' and OI.[SubjectID] = @bloombergOwnerArtifactId and T.sourceIntersectID is null and T.fusionAttributeTypeId != 301);

	-- add any missing relations to source / object
	insert into [intersect] (IntersectTypeID, Classification, [Subject], SubjectID, [Object], ObjectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
		select distinct
			(select top 1 i_t.ID from [intersecttype] i_t where (i_t.[object] = c_s.objecttype and i_t.[subject] = c_t.objecttype and i_t.objectid = c_s.objecttypeid and i_t.subjectid = c_t.objecttypeid))
			,2			
			,'Artifact'
			,@bloombergOwnerArtifactId
			,T.[object]
			,T.[objectID]			
			,0,getutcdate(),0,getutcdate(),'EAGLE BUSINESS LINEAGE'
		from #maps T		
		inner join [cache].[objectdetails] c_s on (c_s.[object] = T.[object] and c_s.[objectid] = T.[objectid])
		inner join [cache].[objectdetails] c_t on (c_t.[object] = 'Artifact' and c_t.[objectid] = @bloombergOwnerArtifactId)
		where T.sourceIntersectID is null and T.IsRelatedToBloomberg = 1 and T.fusionAttributeTypeId != 301;

	update T
	set T.[sourceintersectid] = OI.ID
	from #maps T		
		inner join [IntersectDetail] OI on (OI.[Object] = T.[Object] and OI.ObjectID = T.[ObjectID] and OI.[Subject] = 'Artifact' and OI.[SubjectID] = @bloombergOwnerArtifactId and T.sourceIntersectID is null and T.fusionAttributeTypeId != 301);
		
	--------------------------------------------------------------------------
	-- Bloomberg -> Eagle - Source is bloomberg Target is eagle
	--------------------------------------------------------------------------
	
	update T
	set T.[sourceintersectid] = OI.ID
	from #maps T		
		inner join [IntersectDetail] OI on (OI.[Object] = T.[Object] and OI.ObjectID = T.[ObjectID] and OI.[Subject] = 'Artifact' and OI.[SubjectID] = T.ObjectOwnerArtifactID and T.sourceIntersectID is null and T.fusionAttributeTypeId = 301);

	-- add any missing relations to source / object
	insert into [intersect] (IntersectTypeID, Classification, [Subject], SubjectID, [Object], ObjectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
		select distinct
			(select top 1 i_t.ID from [intersecttype] i_t where (i_t.[object] = c_s.objecttype and i_t.[subject] = c_t.objecttype and i_t.objectid = c_s.objecttypeid and i_t.subjectid = c_t.objecttypeid))
			,2			
			,'Artifact'
			,T.ObjectOwnerArtifactID
			,T.[object]
			,T.[objectID]			
			,0,getutcdate(),0,getutcdate(),'EAGLE BUSINESS LINEAGE'
		from #maps T		
		inner join [cache].[objectdetails] c_s on (c_s.[object] = T.[object] and c_s.[objectid] = T.[objectid])
		inner join [cache].[objectdetails] c_t on (c_t.[object] = 'Artifact' and c_t.[objectid] = T.ObjectOwnerArtifactID)
		where T.sourceIntersectID is null and T.IsRelatedToBloomberg = 1 and T.fusionAttributeTypeId = 301;

	update T
	set T.[sourceintersectid] = OI.ID
	from #maps T		
		inner join [IntersectDetail] OI on (OI.[Object] = T.[Object] and OI.ObjectID = T.[ObjectID] and OI.[Subject] = 'Artifact' and OI.[SubjectID] = T.ObjectOwnerArtifactID and T.sourceIntersectID is null and T.fusionAttributeTypeId != 301);

	--target 

	update T
	set T.[targetintersectid] = OI.ID
	from #maps T		
		inner join [IntersectDetail] OI on (OI.[Object] = T.[Object] and OI.ObjectID = T.[ObjectID] and OI.[Subject] = 'Artifact' and OI.[SubjectID] = @eagleOwnerArtifact and T.TargetIntersectID is null  and T.fusionAttributeTypeId = 301);
		

	-- add any missing relations to source / object
	insert into [intersect] (IntersectTypeID, Classification, [Subject], SubjectID, [Object], ObjectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
		select distinct
			(select top 1 i_t.ID from [intersecttype] i_t where (i_t.[object] = c_s.objecttype and i_t.[subject] = c_t.objecttype and i_t.objectid = c_s.objecttypeid and i_t.subjectid = c_t.objecttypeid) /*or 
				(i_t.[subject] = c_s.objecttype and i_t.[object] = c_t.objecttype and i_t.subjectid = c_s.objecttypeid and i_t.objectid = c_t.objecttypeid)*/)
			,2			
			,'Artifact'
			,@eagleOwnerArtifact
			,T.[object]
			,T.[objectID]			
			,0,getutcdate(),0,getutcdate(),'EAGLE BUSINESS LINEAGE'
		from #maps T		
		inner join [cache].[objectdetails] c_s on (c_s.[object] = T.[object] and c_s.[objectid] = T.[objectid])
		inner join [cache].[objectdetails] c_t on (c_t.[object] = 'Artifact' and c_t.[objectid] = @eagleOwnerArtifact)
		where T.targetIntersectID is null  and T.fusionAttributeTypeId = 301;

	update T
	set T.[targetintersectid] = OI.ID
	from #maps T		
		inner join [IntersectDetail] OI on (OI.[Object] = T.[Object] and OI.ObjectID = T.[ObjectID] and OI.[Subject] = 'Artifact' and OI.[SubjectID] = @eagleOwnerArtifact and T.TargetIntersectID is null  and T.fusionAttributeTypeId = 301);

		
	---------------------------------------------------------------------------------------------------------------
	-- Insert Piece
	---------------------------------------------------------------------------------------------------------------

	Declare @MapItemIDList Table(MapItemID int, sourceintersectid int, targetintersectid int);
	Declare @MapRuleItemIDList Table(MapRuleItemID int, MapID Int);

	-- insert the map item records 
	-- load any existing map item instances
	update T
	set T.MapItemID = mi.ID
	from #maps T
		inner join mapitem mi on(T.sourceintersectid = mi.SourceIntersectID and T.targetintersectid = mi.TargetIntersectID and mi.[Owner] = 'EAGLE BUSINESS LINEAGE'); 

	-- insert map records
	MERGE
	INTO    mapitem mi
	USING   (			
			select distinct sourceintersectid, targetintersectid FROM #maps where (sourceintersectid is not null and targetintersectid is not null) and sourceintersectid != targetintersectid and mapitemid is null
			) S
	ON      (1 = 0)
	WHEN NOT MATCHED THEN
	INSERT  (SourceIntersectID, TargetIntersectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
	VALUES  (S.sourceintersectid, S.targetintersectid, 0, getutcdate(), 0, getutcdate(), 'EAGLE BUSINESS LINEAGE')
	OUTPUT  INSERTED.ID, S.sourceintersectid, S.targetintersectid into @MapItemIDList;

	--update map item id from main temp table
	update T
	set T.mapitemid = MI.MapItemID
	from #maps T
		inner join @MapItemIDList MI on (MI.sourceintersectid = T.sourceintersectid and MI.targetintersectid = T.targetintersectid)
		
	-- delete any mapitem records that are not in objectmap that are markit lineage
	delete from mapitem where [owner] = 'EAGLE BUSINESS LINEAGE' and id not in (select mapitemid from #maps);


	delete from mapruleitemmapitem where [owner] = 'EAGLE BUSINESS LINEAGE';

	insert into mapruleitemmapitem
		(mapruleitemid, mapitemid, [owner])
		select MapRuleItemID, MapItemID, 'EAGLE BUSINESS LINEAGE' from #maps;
	
	--select * from #maps;


end
