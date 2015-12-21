CREATE procedure [fusion].[ProcessFusionCacheInQueue]
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
	merge	cache.[Object] as T
	using	(
			SELECT	'FusionAttribute' as [Object], 
					ID,
					'FusionAttributeType' as ObjectType, 
					FusionAttributeTypeID as ObjectTypeID
			FROM	FusionAttribute
			) as S
	on		(
			T.[Object] = S.[Object] and T.[ObjectID] = S.[ID]
			)
	when matched then
			update	
			set		T.ObjectType = S.ObjectType,
					T.ObjectTypeID = S.ObjectTypeID
	when not matched then
			insert ([Object], ObjectID, ObjectType, ObjectTypeID)
			values (S.[Object], S.ID, S.ObjectType, S.ObjectTypeID);
end

