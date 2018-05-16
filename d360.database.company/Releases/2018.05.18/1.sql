create proc integration.ProcessUnresolvedRelationships
as
begin
	delete	[integration].[UnresolvedRelationItem]
	where	ID in	(
					select  U.ID
					from    [integration].[UnresolvedRelationItem] U
							inner join IntersectType IT on IT.ID = U.IntersectTypeID
							inner join AssetType ST on ST.Object = IT.Subject and ST.ObjectID = IT.SubjectID
							inner join AssetType OT on OT.Object = IT.Object and OT.ObjectID = IT.ObjectID
							inner join Asset S on S.AssetTypeID = ST.ID and S.SourceID = U.SubjectSourceID
							inner join Asset O on O.AssetTypeID = OT.ID and O.SourceID = U.ObjectSourceID
							inner join [Intersect] I on I.IntersectTypeID = IT.ID and I.Subject = S.Object and I.SubjectID = S.ObjectID and I.Object = O.Object and I.ObjectID = O.ObjectID		
					);

	merge into  [Intersect] T
	using       (
				select  U.IntersectTypeID,
						S.Object as Subject, 
						S.ObjectID as SubjectID, 
						O.Object, 
						O.ObjectID 
				from    [integration].[UnresolvedRelationItem] U
						inner join IntersectType IT on IT.ID = U.IntersectTypeID
						inner join AssetType ST on ST.Object = IT.Subject and ST.ObjectID = IT.SubjectID
						inner join AssetType OT on OT.Object = IT.Object and OT.ObjectID = IT.ObjectID
						inner join Asset S on S.AssetTypeID = ST.ID and S.SourceID = U.SubjectSourceID
						inner join Asset O on O.AssetTypeID = OT.ID and O.SourceID = U.ObjectSourceID
				) S
	on          (
					T.IntersectTypeID = S.IntersectTypeID and 
					T.Subject = S.Subject and 
					T.SubjectID = S.SubjectID and 
					T.Object = S.Object and 
					T.ObjectID = S.ObjectID
				)
	when not matched then
		insert  (IntersectTypeID, Subject, SubjectID, Object, ObjectID, CreatedBy, UpdatedBy)
		values  (S.IntersectTypeID, S.Subject, S.SubjectID, S.Object, S.ObjectID, 0, 0);
end
GO