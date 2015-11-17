
CREATE procedure [cache].[SynchronizeRelationships]
	@Intersects IDTable READONLY
as
begin
	declare @count int
	select @count = count(1) from @Intersects

	if @count = 0
	begin
		--REFRESH ENTIRE TABLE
		merge cache.Relationships as T
		using (
				select	distinct
						I.IntersectTypeID,
						S.IntersectID,
						case 
							when I.Classification is null then 2
							when I.Classification = 0 then 2
							else I.Classification
						end as Classification,
						I.Description,
						R.Name as [Role],
						S.IntersectTypeNodeID as SourceIntersectTypeNodeID, 
						S.ObjectType as SourceObject,
						S.ObjectID as SourceObjectID,
						SD.Name as SourceObjectName,
						SD.ObjectType as SourceType,
						SD.ObjectTypeID as SourceTypeID,
						SD.ObjectTypeName as SourceTypeName,
						T.IntersectTypeNodeID as TargetIntersectTypeNodeID,
						T.ObjectType as TargetObject,
						T.ObjectID as TargetObjectID,
						TD.Name as TargetObjectName,
						TD.ObjectType as TargetType,
						TD.ObjectTypeID as TargetTypeID,
						TD.ObjectTypeName as TargetTypeName
				from	dbo.IntersectNode S
						inner join dbo.IntersectNode T on T.IntersectID = S.IntersectID and T.ID <> S.ID
						inner join dbo.[Intersect] I on I.ID = S.IntersectID
						left join IntersectTypeRole R on R.ID = I.IntersectTypeRoleID
						inner join cache.ObjectDetails SD on SD.[Object] = S.ObjectType and SD.ObjectID = S.ObjectID
						inner join cache.ObjectDetails TD on TD.[Object] = T.ObjectType and TD.ObjectID = T.ObjectID
			  ) as S (
					IntersectTypeID, IntersectID, Classification, Description, [Role], 
					SourceIntersectTypeNodeID, SourceObject, SourceObjectID, SourceObjectName, SourceType, SourceTypeID, SourceTypeName,
					TargetIntersectTypeNodeID, TargetObject, TargetObjectID, TargetObjectName, TargetType, TargetTypeID, TargetTypeName
					)
		on    (T.IntersectID = S.IntersectID and T.SourceObject = S.SourceObject and T.SourceObjectID = S.SourceObjectID)
		when matched then
			update  
			set T.Classification = S.Classification,
				T.Description = S.Description,
				T.[Role] = S.[Role],
				T.SourceObjectName = S.SourceObjectName,
				T.TargetObjectName = S.TargetObjectName,
				T.SourceTypeName = S.SourceTypeName,
				T.TargetTypeName = S.TargetTypeName
		when not matched then
			insert (
					IntersectTypeID, IntersectID, Classification, Description, [Role],
					SourceIntersectTypeNodeID, SourceObject, SourceObjectID, SourceObjectName, SourceType, SourceTypeID, SourceTypeName,
					TargetIntersectTypeNodeID, TargetObject, TargetObjectID, TargetObjectName, TargetType, TargetTypeID, TargetTypeName
					)
			values (
					S.IntersectTypeID, S.IntersectID, S.Classification, S.Description, S.[Role], 
					S.SourceIntersectTypeNodeID, S.SourceObject, S.SourceObjectID, S.SourceObjectName, S.SourceType, S.SourceTypeID, S.SourceTypeName,
					S.TargetIntersectTypeNodeID, S.TargetObject, S.TargetObjectID, S.TargetObjectName, S.TargetType, S.TargetTypeID, S.TargetTypeName
					);
	end
	else
	begin
		--REFRESH SINGLE INTERSECT ENTRIES (2)
		merge cache.Relationships as T
		using (
				select	distinct
						I.IntersectTypeID,
						S.IntersectID,
						case 
							when I.Classification is null then 2
							when I.Classification = 0 then 2
							else I.Classification
						end as Classification,
						I.Description,
						R.Name as [Role],
						S.IntersectTypeNodeID as SourceIntersectTypeNodeID, 
						S.ObjectType as SourceObject,
						S.ObjectID as SourceObjectID,
						SD.Name as SourceObjectName,--coalesce(SA.Name, ST.Name, SD.Name, SF.TextPath, SI.Name, SP.Name, SR.Name, SG.Name, SRE.LastName + ', ' + SRE.FirstName) as SourceObjectName,
						SD.ObjectType as SourceType, --case 
						--	when S.ObjectType = 'Policy' then T.ObjectType
						--	when S.ObjectType = 'Rule' then T.ObjectType
						--	else S.ObjectType + 'Type' 
						--end as SourceType,
						SD.ObjectTypeID as SourceTypeID, --coalesce(SA.ArtifactTypeID, ST.TaxonomyTypeID, SD.DomainTypeID, SF.FusionAttributeTypeID, SI.IntersectTypeID, 0) as SourceTypeID,
						SD.ObjectTypeName as SourceTypeName, --coalesce(SAT.Name, STT.Name, SDT.Name, SFT.Name, SIT.Name, IIF(SP.Name is not null, 'Policy', null), IIF(SR.Name is not null, 'Rule', null), S.ObjectType) as SourceTypeName,
						T.IntersectTypeNodeID as TargetIntersectTypeNodeID,
						T.ObjectType as TargetObject,
						T.ObjectID as TargetObjectID,
						TD.Name as TargetObjectName, --coalesce(TA.Name, TT.Name, TD.Name, TF.TextPath, TI.Name, TP.Name, TR.Name, TG.Name, TRE.LastName + ', ' + TRE.FirstName) as TargetObjectName,
						TD.ObjectType as TargetType,--case 
						--	when T.ObjectType = 'Policy' then T.ObjectType
						--	when T.ObjectType = 'Rule' then T.ObjectType
						--	else T.ObjectType + 'Type' 
						--end as TargetType,
						TD.ObjectTypeID as TargetTypeID, --coalesce(TA.ArtifactTypeID, TT.TaxonomyTypeID, TD.DomainTypeID, TF.FusionAttributeTypeID, TI.IntersectTypeID, 0) as TargetTypeID,
						TD.ObjectTypeName as TargetTypeName --coalesce(TAT.Name, TTT.Name, TDT.Name, TFT.Name, TIT.Name, IIF(TP.Name is not null, 'Policy', null), IIF(TR.Name is not null, 'Rule', null), T.ObjectType) as TargetTypeName
				from	dbo.IntersectNode S
						inner join dbo.IntersectNode T on T.IntersectID = S.IntersectID and T.ID <> S.ID
						inner join dbo.[Intersect] I on I.ID = S.IntersectID
						inner join @Intersects C on C.ObjectID = I.ID

						inner join cache.ObjectDetails SD on SD.[Object] = S.ObjectType and SD.ObjectID = S.ObjectID
						inner join cache.ObjectDetails TD on TD.[Object] = T.ObjectType and TD.ObjectID = T.ObjectID
						
						left join IntersectTypeRole R on R.ID = I.IntersectTypeRoleID
			  ) as S (
					IntersectTypeID, IntersectID, Classification, Description, [Role], 
					SourceIntersectTypeNodeID, SourceObject, SourceObjectID, SourceObjectName, SourceType, SourceTypeID, SourceTypeName,
					TargetIntersectTypeNodeID, TargetObject, TargetObjectID, TargetObjectName, TargetType, TargetTypeID, TargetTypeName
					)
		on    (T.IntersectID = S.IntersectID and T.SourceObject = S.SourceObject and T.SourceObjectID = S.SourceObjectID)
		when matched then
			update  
			set T.Classification = S.Classification,
				T.Description = S.Description,
				T.[Role] = S.[Role],
				T.SourceObjectName = S.SourceObjectName,
				T.TargetObjectName = S.TargetObjectName,
				T.SourceTypeName = S.SourceTypeName,
				T.TargetTypeName = S.TargetTypeName
		when not matched then
			insert (
					IntersectTypeID, IntersectID, Classification, Description, [Role], 
					SourceIntersectTypeNodeID, SourceObject, SourceObjectID, SourceObjectName, SourceType, SourceTypeID, SourceTypeName,
					TargetIntersectTypeNodeID, TargetObject, TargetObjectID, TargetObjectName, TargetType, TargetTypeID, TargetTypeName
					)
			values (
					S.IntersectTypeID, S.IntersectID, S.Classification, S.Description, S.[Role],
					S.SourceIntersectTypeNodeID, S.SourceObject, S.SourceObjectID, S.SourceObjectName, S.SourceType, S.SourceTypeID, S.SourceTypeName,
					S.TargetIntersectTypeNodeID, S.TargetObject, S.TargetObjectID, S.TargetObjectName, S.TargetType, S.TargetTypeID, S.TargetTypeName
					);
	end
end
