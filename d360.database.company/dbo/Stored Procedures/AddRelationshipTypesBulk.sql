CREATE procedure [dbo].[AddRelationshipTypesBulk]
	@unresolvedrelations RelationshipTypeTable readonly
as
begin
	set nocount on;

	if exists(select 1 from @unresolvedrelations)
	begin
			
			-- Relationship does not yet exist, so CREATE.
			Declare @UnResIDList Table(IntersectTypeID int,UnresID Int);
			
			MERGE
				INTO    [IntersectType] d
				USING   (
						SELECT distinct ur.startpromotedobjecttype, ur.startpromotedobjecttypeid, ur.endpromotedobjecttype, ur.endpromotedobjecttypeid ,ur.ID as srID
							FROM @unresolvedrelations ur							
						) s
				ON      (1 = 0)
				WHEN NOT MATCHED THEN
				INSERT  (UpdatedOn, UpdatedBy)
				VALUES  (getutcdate(),0)
				OUTPUT  INSERTED.ID, s.srID into @UnResIDList;
				
	end

end
GO

