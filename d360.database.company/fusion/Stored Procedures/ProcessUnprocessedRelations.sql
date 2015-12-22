create procedure [fusion].[ProcessUnprocessedRelations]
as
begin
	set NOCOUNT, ANSI_PADDING ON;
	SET ANSI_WARNINGS ON;

	declare @unprocessedRelationsExeId int;
	
	set @unprocessedRelationsExeId = -99;

	-- delete any unprocessed relations older than 3 days
	delete from [fusion].[StagingRelationUnresolved] where DATEDIFF(day,getdate(),CreatedOn) < -3

	-- delete any unprocessed relations from any prior run that may be hanging around
	delete from [fusion].[StagingRelation] where executionid = @unprocessedRelationsExeId
			
	-- load the unprocessed relations for now across all fusion types /ids

	insert into [fusion].[StagingRelation]
				select	@unprocessedRelationsExeId,
						R.StartID,
						R.EndID,
						S.ID,
						E.ID,
						S.FusionAttributeTypeID,
						E.FusionAttributeTypeID,
						rel.SourceIntersectTypeNodeID,
						rel.TargetIntersectTypeNodeID,
						rel.IntersectTypeID,
						null
				from	(
						select	srm.StartID,
								srm.EndID
						from	[fusion].[StagingRelationUnresolved] srm													
						) R
						inner join FusionAttribute S on S.SourceID = R.StartID
						inner join FusionAttribute E on E.SourceID = R.EndID
						inner join utility.RelationshipTypes rel on (rel.SourceObjectID = S.FusionAttributeTypeID and rel.SourceObjectType = 'FusionAttributeType' and rel.TargetObjectType = 'FusionAttributeType' and rel.TargetObjectID = E.FusionAttributeTypeID)
				WHERE  NOT EXISTS (select * from [intersect] i
					inner join intersectnode inode on (i.id = inode.intersectid and inode.objectid = s.id and inode.objecttype = 'FusionAttribute')
					inner join intersectnode inode2 on (i.id = inode2.intersectid and inode2.objectid = e.id and inode2.objecttype = 'FusionAttribute'))

	-- process these relations as regular relations
	exec [fusion].[ProcessFusionRelationships] @unprocessedRelationsExeId

	--clean up

	-- delete any unprocessed relations that were processed from unprocessed table
	DELETE sru
		FROM [fusion].[StagingRelationUnresolved] sru 
		INNER JOIN [fusion].[StagingRelation] sr
		  ON sru.startid = sr.startid and sru.endid = sr.endid and sr.executionid = @unprocessedRelationsExeId
		
	-- delete from staging relation any relations added
	delete from [fusion].[StagingRelation] where executionid = @unprocessedRelationsExeId

end