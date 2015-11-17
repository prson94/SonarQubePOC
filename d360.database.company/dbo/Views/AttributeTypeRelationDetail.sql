
CREATE VIEW [dbo].[AttributeTypeRelationDetail]
AS
	SELECT	R.AttributeTypeID,
			R.ObjectID,
			coalesce(D.Name, R.ObjectType) AS ObjectName, 
			R.ObjectType,
			cast(0 as bit) as Required,
			R.AllowMultipleEntries
	FROM	AttributeTypeRelation R
			left join cache.ObjectDetails D on D.[Object] = R.ObjectType and D.ObjectID = R.ObjectID
