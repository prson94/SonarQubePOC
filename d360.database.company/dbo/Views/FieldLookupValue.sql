


CREATE VIEW [dbo].[FieldLookupValue]
AS
	SELECT	T.ID as FieldTypeID,
			T.LookupObjectType,
			T.LookupObjectID,
			COALESCE(A.ID, D.ID, DI.ID, R.ResourceID, L.ID) as Value,
			utility.GetFormattedFieldLookupValue(T.Type, T.LookupDisplayFormat, T.LookupObjectType, T.LookupObjectID, COALESCE(A.ID, D.ID, DI.ID, R.ResourceID, L.ID)) AS Text
	FROM	FieldType T 
			LEFT JOIN Artifact A ON T.LookupObjectType = 'Artifact' AND T.LookupObjectID = A.ArtifactTypeID
			LEFT JOIN Domain D ON T.LookupObjectType = 'Domain' AND T.LookupObjectID = D.DomainTypeID
			LEFT JOIN DomainItem DI ON T.LookupObjectType = 'DomainItem' AND T.LookupObjectID = DI.DomainID
			LEFT JOIN reporting.Global_Resource R ON T.LookupObjectType = 'Resource' --AND T.LookupObjectID = R.ResourceTypeID
			LEFT JOIN Lookup L ON T.LookupObjectType = 'Lookup' AND T.LookupObjectID = L.LookupTypeID
	WHERE	T.LookupObjectType is not null
			AND COALESCE(A.ID, D.ID, DI.ID, R.ResourceID, L.ID) IS NOT NULL
