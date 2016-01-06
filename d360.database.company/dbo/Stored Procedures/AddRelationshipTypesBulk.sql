create procedure [dbo].[AddRelationshipTypesBulk]
	@unresolvedrelations RelationshipTypeTable readonly
as
begin
	set nocount on;

	if exists(select 1 from @unresolvedrelations)
	begin
			-- Relationship does not yet exist, so CREATE.
			Declare @UnResIDList Table(IntersectTypeID int,UnresID Int);
			
			--insert intersect records and save there id's
			-- trick is to use merge to keep the sequence id and staging row ids
			-- http://stackoverflow.com/questions/15614261/using-output-clause-to-insert-value-not-in-inserted
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

		
		INSERT INTO IntersectTypeNode	(IntersectTypeID, ObjectType, ObjectID, [Order]) 
			select il.IntersectTypeID, ur.startpromotedobjecttype, ur.startpromotedobjecttypeid, 1 from @unresolvedrelations ur inner join @UnResIDList il on (ur.ID = il.UnresID);
				

		
		INSERT INTO IntersectTypeNode	(IntersectTypeID, ObjectType, ObjectID, [Order]) 
			select il.IntersectTypeID, ur.endpromotedobjecttype, ur.endpromotedobjecttypeid, 2 from @unresolvedrelations ur inner join @UnResIDList il on (ur.ID = il.UnresID);
				
	end

end