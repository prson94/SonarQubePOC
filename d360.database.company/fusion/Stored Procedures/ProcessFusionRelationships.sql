CREATE PROCEDURE [fusion].[ProcessFusionRelationships]
	@executionID int	
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	set NOCOUNT, ANSI_PADDING ON;
	SET ANSI_WARNINGS ON;

	declare @Intersects IDTable;
	declare @objectType varchar(50) = 'FusionAttribute';

    -- delete any relations we already have that was already added from stagingrelation table so we dont duplicate
			delete from fusion.stagingrelation where 
				executionid = @executionID
					and
				id in(
					select 
						sr.id
					from
						intersectnode inode1
						inner join intersectnode inode2 on(inode1.IntersectID = inode2.IntersectID)
						inner join fusion.stagingrelation sr on(inode1.ObjectID = sr.startfusionattributeid and inode2.ObjectID = sr.endfusionattributeid and inode1.IntersectTypeNodeID = sr.startintersecttypenodeid and inode2.IntersectTypeNodeID = sr.endintersecttypenodeid)
					where 
						inode1.objecttype = @objectType
								and
						inode2.objecttype = @objectType);
					
			Declare @IDList Table(IntersectID int,StageID Int);
			
			--insert intersect records and save there id's
			-- trick is to use merge to keep the sequence id and staging row ids
			-- http://stackoverflow.com/questions/15614261/using-output-clause-to-insert-value-not-in-inserted
			MERGE
				INTO    [Intersect] d
				USING   (
						SELECT sr.IntersectTypeID isectid , 2 as class,sr.ID as srID
							FROM [fusion].stagingrelation sr
							where sr.ExecutionID = @executionID and sr.IntersectID is null
						) s
				ON      (1 = 0)
				WHEN NOT MATCHED THEN
				INSERT  (IntersectTypeID, Classification, Description)
				VALUES  (isectid, class, NULL)
				OUTPUT  INSERTED.ID, s.srID into @IDList;

			--insert start records into intersect node
			INSERT INTO IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
					select sr.StartIntersectTypeNodeID, il.IntersectID, 'FusionAttribute',sr.StartFusionAttributeID from [fusion].[StagingRelation] sr inner join @IDList il on (sr.ID = il.StageID)
						where	sr.ExecutionID = @executionID and sr.IntersectID is null;

			--insert end records into intersect node
			INSERT INTO IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
					select sr.EndIntersectTypeNodeID, il.IntersectID, 'FusionAttribute',sr.EndFusionAttributeID from [fusion].[StagingRelation] sr inner join @IDList il on (sr.ID = il.StageID)
						where	sr.ExecutionID = @executionID and sr.IntersectID is null;
	
			--update staginrelation to have the id's we used in intersect table
			UPDATE	[fusion].[StagingRelation]
					SET		IntersectID = idl.intersectid
					from @IDList idl
					WHERE	ExecutionID = @executionID and ID = idl.stageid;
										
			insert into @Intersects select idl.intersectid from @IDList idl;
			
			declare @IntersectCount int
			select @IntersectCount = count(1) from @Intersects
			if @IntersectCount > 0 
			begin
				EXEC cache.SynchronizeRelationships @Intersects
			end

END
GO
