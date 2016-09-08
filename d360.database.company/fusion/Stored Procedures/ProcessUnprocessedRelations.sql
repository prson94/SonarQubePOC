CREATE procedure [fusion].[ProcessUnprocessedRelations]
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
				select	distinct 
						@unprocessedRelationsExeId,
						R.StartID,
						R.EndID,
						S.ID,
						E.ID,
						S.FusionAttributeTypeID,
						E.FusionAttributeTypeID,
						IT.ID,
						null
				from	(
						select	srm.StartID,
								srm.EndID
						from	[fusion].[StagingRelationUnresolved] srm													
						) R
						inner join FusionAttribute S on S.SourceID = R.StartID
						inner join FusionAttribute E on E.SourceID = R.EndID
						inner join IntersectType IT on	IT.Subject = 'FusionAttributeType' and 
														IT.Object = 'FusionAttributeType' and 
														(
															( IT.SubjectID = S.FusionAttributeTypeID and IT.ObjectID = E.FusionAttributeTypeID ) OR
															( IT.SubjectID = E.FusionAttributeTypeID and IT.ObjectID = S.FusionAttributeTypeID )
														)
				where	NOT EXISTS	(
									select	* 
									from	[Intersect] I
									where	I.Subject = 'FusionAttribute' and 
											I.Object = 'FusionAttribute' and
											(
												(I.SubjectID = S.ID and I.ObjectID = E.ID ) OR
												(I.SubjectID = E.ID and I.ObjectID = S.ID )
											)
									)

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
GO

