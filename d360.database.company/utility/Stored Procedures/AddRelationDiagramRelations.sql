CREATE procedure [utility].[AddRelationDiagramRelations]
	@diagramRelations [utility].[DiagramRelationshipTable] readonly,
	@NumberOfIntersectsAdded int OUTPUT,
	@NumberOfObjectsUpdated int OUTPUT
as
begin
	set nocount on;

	If EXISTS (SELECT 1 FROM @diagramRelations)		
			begin
				Declare @IDList Table(IntersectID int,RelID Int);
				Declare @SourceIntersectNodeList Table(IntersectNodeID int,Item Int);
				Declare @TargetIntersectNodeList Table(IntersectNodeID int,Item Int);
				Declare @IntersectMapTemp Table(SubjectIntersectNode int, ObjectIntersectNode int, PredicateID int, [Type] int);
				Declare @Intersects IDTable;

				select @NumberOfIntersectsAdded = @NumberOfIntersectsAdded + (select count(1) from @diagramRelations);

				--insert intersect records and save there id's
				-- trick is to use merge to keep the sequence id and staging row ids
				-- http://stackoverflow.com/questions/15614261/using-output-clause-to-insert-value-not-in-inserted
				MERGE
							INTO    [Intersect] d
							USING   (
										select	rel.IntersectTypeID, 
												2 as Classification,
												rel.SourceObject,
												rel.SourceObjectID,
												rel.TargetObject,
												rel.TargetObjectID,
												rel.ItemID as srID 
										from	@diagramRelations rel						
									) s
							ON      (1 = 0)
							WHEN NOT MATCHED THEN
							INSERT  (IntersectTypeID, Classification, Description, Subject, SubjectID, Object, ObjectID)
							VALUES  (s.IntersectTypeID, s.Classification, NULL, s.SourceObject, s.SourceObjectID, s.TargetObject, s.TargetObjectID)
							OUTPUT  INSERTED.ID, s.srID into @IDList;
							
				--insert start records into intersect node track the id that it gets 
				MERGE
							INTO    IntersectNode d
							USING   (
										select	sr.SourceIntersectTypeNodeID, 
												il.IntersectID, 
												sr.SourceObject,
												sr.SourceObjectID, 
												sr.ItemID as itemID 
										from	@diagramRelations sr 
												inner join @IDList il on (sr.ItemID = il.RelID)											
									) s
							ON      (1 = 0)
							WHEN NOT MATCHED THEN
							INSERT  (IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
							VALUES  (s.SourceIntersectTypeNodeID, s.IntersectID, s.SourceObject, s.SourceObjectID)
							OUTPUT  INSERTED.ID, s.itemID into @SourceIntersectNodeList;
					
				MERGE
							INTO    IntersectNode d
							USING   (
										select	sr.TargetIntersectTypeNodeID, 
												il.IntersectID, 
												sr.TargetObject,
												sr.TargetObjectID, 
												sr.ItemID as itemID 
										from	@diagramRelations sr 
												inner join @IDList il on (sr.ItemID = il.RelID)											
									) s
							ON      (1 = 0)
							WHEN NOT MATCHED THEN
							INSERT  (IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
							VALUES  (s.TargetIntersectTypeNodeID, s.IntersectID, s.TargetObject, s.TargetObjectID)
							OUTPUT  INSERTED.ID, s.itemID into @TargetIntersectNodeList;
					
				--add record for each to intersectmap table
				insert into intersectmap
					select 
						sList.IntersectNodeID as SubjectIntersectNode,
						tList.IntersectNodeID as ObjectIntersectNode,
						itemList.[predicateid] as PredicateID,
						itemList.[type] as [Type]
					from
						@diagramRelations itemList
						inner join @SourceIntersectNodeList sList on (itemList.ItemID = sList.Item)
						inner join @TargetIntersectNodeList tList on (itemList.ItemID = tList.Item)
					where 
						itemList.needsMapRecord = 1
					
				select @NumberOfObjectsUpdated = @NumberOfObjectsUpdated + 1;
					
				insert into @Intersects select idl.intersectid from @IDList idl;

				if exists (select 1 from @Intersects)
				begin
					EXEC cache.SynchronizeRelationships @Intersects		
				end
			end	-- end if intersects are needed

end