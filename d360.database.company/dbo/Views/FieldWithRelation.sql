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
			T.IsListable,
			T.IsRequired,
			T.SortOrder,
			F.ObjectType,
			F.ObjectID,
			F.Value,
			F.FormattedValue,
			LD.Url as LookupUrl
	FROM	FieldType T
			inner join Field F on F.FieldTypeID = T.ID and ( 
															(F.ObjectType + 'Type' = T.[Object] and F.ObjectType <> 'Event') OR 
															(T.[Object] = 'Rule' and F.ObjectType = 'Event') 
														   )
			left join cache.ObjectDetails LD on 
				LD.[Object] = case T.LookupObjectType
									when 'ReferenceItem' then 'ReferenceItemType' 
									else T.LookupObjectType 
							  end
				and LD.ObjectID = case 
									when T.LookupObjectType = 'ReferenceItem' then T.LookupObjectID 
									when T.LookupObjectType = 'Resource' then T.LookupObjectID 
									when T.LookupObjectType is null then NULL 
									when dbo.IsInteger(F.Value) = 1 then F.Value
								end

GO