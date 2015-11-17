CREATE VIEW [dbo].[LookupAllocation] AS
	SELECT	FT.ID as FieldTypeID,
			FT.Name as FieldTypeName,
			COALESCE(AT.ID, DT.ID, LT.ID) as LookupTypeID,
			COALESCE(AT.Name, DT.Name, LT.Name) as LookupTypeName,
			FT.LookupObjectType,
			D.ObjectID,
			D.Name as ObjectName,
			D.ObjectType,
			D.ObjectTypeName,
			D.Url as ObjectUrl
	FROM	FieldType FT
			left JOIN ArtifactType AT ON	FT.LookupObjectType = 'Artifact' and AT.ID = FT.LookupObjectID
			left JOIN DomainType DT ON		FT.LookupObjectType = 'Domain' and DT.ID = FT.LookupObjectID
			left JOIN LookupType LT ON		FT.LookupObjectType = 'Lookup' and LT.ID = FT.LookupObjectID
			inner join cache.ObjectDetails D on D.[Object] = FT.[Object] and D.ObjectID = FT.ObjectID
	WHERE	FT.LookupObjectType is not null
			AND FT.LookupObjectID is not null
