CREATE procedure [lineage].[GetByObject]
--declare
	@Object varchar(50),-- = 'Artifact',
	@ObjectID int-- = 1101
as
begin
	--Hold the raw lineage records.
	declare @tbl table (IntersectID int, IntersectTypeID int, 
						Subject varchar(50), SubjectID int, Object varchar(50), ObjectID int, [State] int, 
						PredicateID int, PredicateName nvarchar(250), PredicateInverse nvarchar(250), PredicateType int, 
						IntersectGroupID int null
						)

	-- Get the direct lineage going backward from the provided object.
	insert into @tbl
		select	L.IntersectID,
				L.IntersectTypeID,
				L.[Subject],
				L.SubjectID,
				L.[Object],
				L.ObjectID,
				L.[State],
				L.PredicateID,
				L.PredicateName,
				L.PredicateInverse,
				L.PredicateType,
				G.IntersectGroupID 
		from	lineage.GetTrailForObject(@Object, @ObjectID, 0) L	
				left join IntersectGroupItem G on G.IntersectID = L.IntersectID

	-- Get the direct lineage going foreward from the provided object.
	insert into @tbl
		select	L.IntersectID,
				L.IntersectTypeID,
				L.[Subject],
				L.SubjectID,
				L.[Object],
				L.ObjectID,
				L.[State],
				L.PredicateID,
				L.PredicateName,
				L.PredicateInverse,
				L.PredicateType,
				G.IntersectGroupID 
		from	lineage.GetTrailForObject(@Object, @ObjectID, 1) L	
				left join IntersectGroupItem G on G.IntersectID = L.IntersectID

	-- Hold the intersect IDs that are part of an IntersectGroup from one of the retrieved intersects above.
	declare @groupIntersects table (IntersectGroupID int, IntersectID int)

	-- Get the intersects that are part of an IntersectGroup from one of intersects above, but not yet pulled back in the temp table (i.e. does not exist in the lineage)
	insert into @groupIntersects
		select	GI.IntersectGroupID,
				GI.IntersectID
		from	@tbl O
				inner join IntersectGroupItem GI on GI.IntersectGroupID = O.IntersectGroupID and GI.IntersectID not in (select IntersectID from @tbl)

	-- Get the intersect record itself, for each ID pulled back as part of the group query above.
	insert into @tbl
		select	P.IntersectID,
				P.IntersectTypeID,
				P.[Subject],
				P.SubjectID,
				P.[Object],
				P.ObjectID,
				P.[State],
				P.PredicateID,
				P.PredicateName,
				P.PredicateInverse,
				P.PredicateType,
				G.IntersectGroupID
		from	PredicateIntersect P
				inner join @groupIntersects G on G.IntersectID = P.IntersectID

	-- Go back for each group intersectID retrieved above and get backward-facing lineage, that is not already present in the lineage @tbl
	insert into @tbl
		select	Src.IntersectID,
				Src.IntersectTypeID,
				Src.[Subject],
				Src.SubjectID,
				Src.[Object],
				Src.ObjectID,
				Src.[State],
				Src.PredicateID,
				Src.PredicateName,
				Src.PredicateInverse,
				Src.PredicateType,
				null
		from	PredicateIntersect P
				inner join @groupIntersects G on G.IntersectID = P.IntersectID
				cross apply lineage.GetTrailForObject(P.Subject, P.SubjectID, 0) Src
		where	Src.IntersectID not in (select IntersectID from @tbl)


	-- Return the full results to the caller.
	select	distinct
			I.IntersectID,
			I.IntersectGroupID,
			T.IntersectTypeID,
			SA.ID as SubjectAssetID,
			I.Subject,
			I.SubjectID,
			SA.DisplayValue as SubjectName,
			SA.BackColor as SubjectBackColor,
			SA.ForeColor as SubjectForeColor,
			SA.TypeName as SubjectTypeName,
			SA.Type as SubjectType,
			SA.TypeID as SubjectTypeID,
			SA.AssetTypeID as SubjectAssetTypeID,

			OA.ID as ObjectAssetID,
			I.Object,
			I.ObjectID,
			OA.DisplayValue as ObjectName,
			OA.BackColor as ObjectBackColor,
			OA.ForeColor as ObjectForeColor,
			OA.TypeName as ObjectTypeName,
			OA.Type as ObjectType,
			OA.TypeID as ObjectTypeID,
			OA.AssetTypeID as ObjectAssetTypeID,

			I.[State],

			I.PredicateName as [Predicate]
	from	@tbl I
			inner join [Intersect] T on T.ID = I.IntersectID
			inner join AssetDetail SA on SA.Object = I.Subject and SA.ObjectID = I.SubjectID
			inner join AssetDetail OA on OA.Object = I.Object and OA.ObjectID = I.ObjectID
end