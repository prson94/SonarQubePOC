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
			--left join cache.ObjectDetails D on D.[Object] = F.ObjectType and D.ObjectID = F.ObjectID
			--left join Attribute AD on F.ObjectType = 'Attribute' and AD.ID = F.ObjectID
			left join cache.ObjectDetails LD on 
				LD.[Object] = case when T.LookupObjectType = 'Lookup' then 'LookupType' when T.LookupObjectType = 'DomainItem' then 'Domain' else T.LookupObjectType end
				and LD.ObjectID = case when T.LookupObjectType = 'Lookup' then T.LookupObjectID when T.LookupObjectType = 'DomainItem' then T.LookupObjectID when T.LookupObjectType = 'Resource' then T.LookupObjectID when T.LookupObjectType is null then NULL else F.Value end
	--where	T.ObjectID = coalesce(D.ObjectTypeID, AD.AttributeTypeID)
	--		and coalesce(D.ObjectID, AD.ID) is not null
