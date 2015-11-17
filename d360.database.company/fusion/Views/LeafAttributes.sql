CREATE VIEW [fusion].[LeafAttributes]
as
	with H as (
		select	ID,
				ParentID,
				Name,
				TextPath,
				Tab
		from	FusionAttributeType
		where	ParentID is null
		union all
		select	C.ID,
				C.ParentID,
				C.Name,
				C.TextPath,
				H.Tab
		from	FusionAttributeType C
				inner join H on H.ID = C.ParentID
	)

	select	FA.ID,
			FA.FusionID,
			FA.Name as AttributeName,
			FA.TextPath as AttributePath,
			H.Name as TypeName,
			H.TextPath as TypePath,
			H.Tab,
			F.Name as FusionName,
			COALESCE(FAD.Url, '#') as Url
	from	FusionAttribute FA
			inner join Fusion F on  F.ID = FA.FusionID
			inner join H on H.ID = FA.FusionAttributeTypeID
			cross apply (
						SELECT	COUNT(1) as ChildCount 
						FROM	FusionAttribute 
						where	ParentID = FA.ID
						) CFA
			inner join cache.ObjectDetails FAD on FAD.[Object] = 'FusionAttribute' and FAD.ObjectID = FA.ID--cross apply utility.ObjectDetail('FusionAttribute', FA.ID) FAD
	where	CFA.ChildCount = 0
