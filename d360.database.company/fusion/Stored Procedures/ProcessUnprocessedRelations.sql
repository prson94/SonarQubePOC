create procedure [fusion].[ProcessUnprocessedRelations]
as
begin
	set NOCOUNT, ANSI_PADDING ON;
	SET ANSI_WARNINGS ON;

	declare @unprocessedRelationsExeId int;
	
	set @unprocessedRelationsExeId = -99;

	-- delete any unprocessed relations older than 3 days
	delete from [fusion].[StagingRelationUnresolved] where DATEDIFF(day,getdate(),CreatedOn) < -3
			
	-- load the unprocessed relations for now across all fusion types /ids

	insert into [fusion].[StagingRelation]
				select	@unprocessedRelationsExeId,
						R.StartID,
						R.EndID,
						S.ID,
						E.ID,
						S.FusionAttributeTypeID,
						E.FusionAttributeTypeID,
						RT.SourceIntersectTypeNodeID,
						RT.TargetIntersectTypeNodeID,
						RT.IntersectTypeID,
						V.IntersectID
				from	(
						select	srm.StartID,
								srm.EndID
						from	[fusion].[StagingRelationUnresolved] srm													
						) R
						inner join FusionAttribute S on S.SourceID = R.StartID
						inner join FusionAttribute E on E.SourceID = R.EndID
						cross apply (
									select	IntersectTypeID,
											SourceIntersectTypeNodeID,
											TargetIntersectTypeNodeID
									from	utility.RelationshipTypes
									where	SourceObjectType = 'FusionAttributeType' and SourceObjectID = S.FusionAttributeTypeID 
											and TargetObjectType = 'FusionAttributeType' and TargetObjectID = E.FusionAttributeTypeID
									) RT
						left join cache.Relationships V on V.SourceObject = 'FusionAttribute' and V.TargetObject = 'FusionAttribute' and V.SourceObjectID = S.ID and V.TargetObjectID = E.ID						
				where	V.IntersectID is null --only get non-existent relationships

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