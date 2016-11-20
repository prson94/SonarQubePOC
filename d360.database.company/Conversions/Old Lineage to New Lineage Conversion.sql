insert into MapItem
	select	SI.ID as SourceID,
			SI.Name,
			TI.ID as TargetID,
			--TI.Name--,
			0,
			getutcdate(),
			0,
			getutcdate()
	from	IntersectMap M
			inner join IntersectNode S on S.ID = M.SubjectIntersectNodeID
			inner join cache.ObjectDetails SD on SD.Object = S.ObjectType and SD.ObjectID = S.ObjectID
			inner join IntersectNode T on T.ID = M.ObjectIntersectNodeID
			inner join cache.ObjectDetails TD on SD.Object = T.ObjectType and TD.ObjectID = T.ObjectID
			inner join [Intersect] SI on (
											(SI.Subject = SD.Object and SI.SubjectID = SD.ObjectID and (SI.Object + cast(SI.ObjectID as varchar) <> SD.Object + cast(SD.ObjectID as varchar))) or 
											(SI.Object = SD.Object and SI.ObjectID = SD.ObjectID and (SI.Subject + cast(SI.SubjectID as varchar) <> SD.Object + cast(SD.ObjectID as varchar)))
										)
			inner join [Intersect] TI on (
											(TI.Subject = TD.Object and TI.SubjectID = TD.ObjectID and (TI.Object + cast(TI.ObjectID as varchar) <> TD.Object + cast(TD.ObjectID as varchar))) or 
											(TI.Object = TD.Object and TI.ObjectID = TD.ObjectID and (TI.Object + cast(TI.ObjectID as varchar) <> TD.Object + cast(TD.ObjectID as varchar)))
										)
			--left join [dbo].[IntersectMapSourceRule] SR on SR.IntersectMapID = M.ID
	where	(SI.ObjectID = TI.ObjectID) --OR (SI.SubjectID = TI.ObjectID) OR (SI.ObjectID = TI.ObjectID) OR (SI.ObjectID = TI.SubjectID)
	--order by TI.ID, SR.SortOrder