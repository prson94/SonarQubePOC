CREATE VIEW [dbo].[FieldWithRelation]
AS
	SELECT	F.FieldTypeID,
			T.Name,
			T.FriendlyName,
			T.Category,
			T.Description,
			T.DisplayDescription,
			T.FormDescription,
			T.ValidationDescription,
			T.Type,
			T.LookupObjectType,
			T.LookupObjectID,
			T.LookupDisplayFormat,
			T.MinimumLength,
			T.MaximumLength,
			T.Length,
			T.Pattern,
			T.IsDisplayable,
			T.IsEditable,
			T.IsListable,
			T.IsRequired,
			T.SortOrder,
			T.AllowMultipleValues,
			F.ObjectType,
			F.ObjectID,
			coalesce(F.Value, T.DefaultValue) as Value,
			case
				when T.AllowAllValue = 1 and F.FormattedValue = '0' then T.AllowAllLabel
				when F.FormattedValue is not null then F.FormattedValue
				when T.DefaultFormattedValue is not null then T.DefaultFormattedValue
				else null
			end as FormattedValue,
			case  
				when (T.AllowMultipleValues = 0 and T.LookupObjectType = 'ReferenceItemType') then [dbo].GenerateObjectUrl('ReferenceItemType', coalesce(F.Value, T.DefaultValue), T.LookupObjectID)
				when (T.AllowMultipleValues = 0 and T.LookupObjectType = 'ReferenceItem') then [dbo].GenerateObjectUrl('ReferenceItemType', T.LookupObjectID, coalesce(F.Value, T.DefaultValue))
				when (T.AllowMultipleValues = 0 and T.LookupObjectType = 'Resource') then [dbo].GenerateObjectUrl('ResourceType', 0, T.LookupObjectID)
				else null
			end as LookupUrl
	FROM	FieldType T
			left join Field F on F.FieldTypeID = T.ID 
	WHERE	(F.Value is not null OR T.DefaultValue is not null)

GO