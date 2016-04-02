
CREATE VIEW [dbo].[FieldTypeWithRelation]
AS
	SELECT	T.ID,
			T.Name,
			T.FriendlyName,
			T.Category,
			T.Description,
			T.DisplayDescription,
			T.FormDescription,
			T.ValidationDescription,
			T.Type,
			T.LookupObjectType,
			T.LookupObjectID ,
			T.LookupDisplayFormat,
			T.Length,
			T.MinimumLength,
			T.MaximumLength,
			T.Pattern,
			T.[Object],
			T.ObjectID,
			D.Name as ObjectName,
			T.IsListable,
			T.IsRequired,
			T.SortOrder
	FROM	FieldType T
			inner join cache.ObjectDetails D on D.[Object] = T.[Object] and D.ObjectID = T.ObjectID
