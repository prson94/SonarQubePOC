CREATE VIEW [dbo].[RelationshipAggregate]
AS
	select		R.SourceObject as ObjectType,
				R.SourceObjectID as ObjectID,
				R.TargetTypeName as TypeName,
				R.TargetTypeID as TypeID,
				R.TargetType as Type,
				coalesce(S.IconBackColor, '#333333') as IconBackColor,
				coalesce(S.IconForeColor, '#ffffff') as IconForeColor,
				coalesce(S.IconText, 'leaf') as IconText,
				coalesce(count(1), 0) as [Count]
	from		cache.Relationships R
				left join ObjectStyle S on S.ObjectType = R.TargetType and S.ObjectID = R.TargetTypeID
	group by	R.SourceObject,
				R.SourceObjectID,
				R.TargetTypeName,
				R.TargetTypeID,
				R.TargetType,
				coalesce(S.IconBackColor, '#333333'),
				coalesce(S.IconForeColor, '#ffffff'),
				coalesce(S.IconText, 'leaf')
