create procedure [fusion].[ProcessFusionCacheInQueue]
--declare
	@FusionID int
--set @FusionID = 15
as
begin
	SET NOCOUNT, ANSI_PADDING ON;
	SET ANSI_WARNINGS ON;

	UPDATE  FusionAttribute
	SET		[Path] = utility.GetBreadcrumbWrapper('FusionAttribute', ID),
			TextPath = utility.GetBreadcrumbStringWrapper('FusionAttribute', ID, '.')
	FROM	FusionAttribute 
	WHERE	FusionID = @FusionID

	-- upsert the individual object into the cache table.
	merge	cache.ObjectDetails as T
	using	(
			SELECT	'FusionAttribute' as [Object], O.ID, O.Name, O.TextPath,
					case 
						when P.ID is not null then 'FusionAttribute'
						else NULL
					end as Parent, O.ParentID,	P.Name as ParentName,
					'#/fusion/' + CAST(FT.ID as varchar(15)) + '/' + + CAST(O.FusionID as varchar(15)) + '/' + CAST(O.ID as varchar(15)) as Url,
					'FusionAttributeType' as ObjectType, O.FusionAttributeTypeID as ObjectTypeID, T.Name as ObjectTypeName,
					coalesce(S.IconBackColor, '#000000') as IconBackColor,
					coalesce(S.IconForeColor, '#ffffff') as IconForeColor,
					coalesce(S.IconText, 'leaf') as IconText
			FROM	FusionAttribute O
					LEFT JOIN FusionAttribute P on P.ID = O.ParentID
					INNER JOIN FusionAttributeType T ON O.FusionAttributeTypeID = T.ID and O.FusionID = @FusionID
					INNER JOIN FusionType FT ON T.FusionTypeID = FT.ID
					left join ObjectStyle S on S.ObjectType = 'FusionType' and S.ObjectID = FT.ID
			) as S
	on		(
			T.[Object] = S.[Object] and T.[ObjectID] = S.[ID]
			)
	when matched then
			update	
			set		T.Name = S.Name,
					T.TextPath = S.TextPath,
					T.Parent = S.Parent,
					T.ParentID = S.ParentID,
					T.ParentName = S.ParentName,
					T.Url = S.Url,
					T.ObjectType = S.ObjectType,
					T.ObjectTypeID = S.ObjectTypeID,
					T.ObjectTypeName = S.ObjectTypeName,
					T.IconBackColor = S.IconBackColor,
					T.IconForeColor = S.IconForeColor,
					T.IconText = S.IconText
	when not matched then
			insert (
					[Object], ObjectID, Name, TextPath, 
					Parent, ParentID, ParentName, 
					Url, 
					ObjectType, ObjectTypeID, ObjectTypeName, 
					IconBackColor, IconForeColor, IconText)
			values (
					S.[Object], S.ID, S.Name, S.TextPath,
					S.Parent, S.ParentID, S.ParentName, 
					S.Url, 
					S.ObjectType, S.ObjectTypeID, S.ObjectTypeName, 
					S.IconBackColor, S.IconForeColor, S.IconText
					);

	UPDATE	R
	SET		R.SourceObjectName = S.Name
	FROM	cache.Relationships R INNER JOIN FusionAttribute S ON R.SourceObject = 'FusionAttribute' and R.SourceObjectID = S.ID

	UPDATE	R
	SET		R.TargetObjectName = S.Name
	FROM	cache.Relationships R INNER JOIN FusionAttribute S ON R.TargetObject = 'FusionAttribute' and R.TargetObjectID = S.ID
end

