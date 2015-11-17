CREATE view [fusion].[AttributeOwner]
as
	select	R.FusionID,
			R.ID,
			R.ObjectID,
			R.ObjectType,
			OBJ.Name + ' (' + OBJ.ObjectTypeName + ')' as ObjectName,
			R.ParentObjectID,
			R.ParentObjectType,
			PAR.Name + ' (' + PAR.ObjectTypeName + ')' as ParentName,
			R.RelationshipOwnerObjectID,
			R.RelationshipOwnerObjectType,
			REL.Name + ' (' + REL.ObjectTypeName + ')' as RelationshipOwnerName
	from	FusionAttributeOwnerRule R
			left join cache.ObjectDetails OBJ on OBJ.[Object] = R.ObjectType and OBJ.ObjectID = R.ObjectID--outer apply utility.ObjectDetail(R.ObjectType, R.ObjectID) OBJ
			left join cache.ObjectDetails PAR on PAR.[Object] = R.ParentObjectType and PAR.ObjectID = R.ParentObjectID--outer apply utility.ObjectDetail(R.ParentObjectType, R.ParentObjectID) PAR
			left join cache.ObjectDetails REL on REL.[Object] = R.RelationshipOwnerObjectType and REL.ObjectID = R.RelationshipOwnerObjectID--outer apply utility.ObjectDetail(R.RelationshipOwnerObjectType, R.RelationshipOwnerObjectID) REL
