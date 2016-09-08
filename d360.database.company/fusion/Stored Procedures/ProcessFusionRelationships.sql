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
	delete	T
	from	fusion.StagingRelation T
			left join [Intersect] S on	S.Subject = @objectType and 
										S.Object = @objectType and
										(
											( S.SubjectID = T.StartFusionAttributeID and S.ObjectID = T.EndFusionAttributeID ) OR
											( S.SubjectID = T.EndFusionAttributeID and S.ObjectID = T.StartFusionAttributeID )
										)
	where	ExecutionID = @executionID and
			S.ID is null;
					
	Declare @IDList Table(IntersectID int, StageID Int);
			
	MERGE
		INTO    [Intersect] d
		USING   (
				SELECT	IntersectTypeID, 
						ID,
						StartFusionAttributeID,
						EndFusionAttributeID
				FROM	[fusion].stagingrelation
				where	ExecutionID = @executionID 
						and IntersectID is null
				) S
		ON      (1 = 0)
		WHEN NOT MATCHED THEN
		INSERT  (IntersectTypeID, Classification, Description, Subject, SubjectID, Object, ObjectID)
		VALUES  (S.IntersectTypeID, 2, NULL, @objectType, StartFusionAttributeID, @objectType, EndFusionAttributeID)
		OUTPUT  INSERTED.ID, S.ID into @IDList;
	
	--update StagingRelation to have the id's we used in intersect table.
	UPDATE	T
	SET		T.IntersectID = S.IntersectID
	from	[fusion].[StagingRelation] T
			inner join @IDList S on T.ExecutionID = @executionID and T.ID = S.StageID;

	insert into @Intersects 
		select	IntersectID 
		from	@IDList;
			
	declare @IntersectCount int
	select @IntersectCount = count(1) from @Intersects
	if @IntersectCount > 0 
	begin
		EXEC cache.SynchronizeRelationships @Intersects
	end
END
GO

