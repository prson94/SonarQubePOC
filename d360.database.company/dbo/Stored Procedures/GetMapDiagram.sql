CREATE procedure [dbo].[GetMapDiagram]
--declare
	@mapID int
--set @mapID = 1
as
begin
	select	IM.ID,
			IM.MapID,
			SD.[Object] as Sub,
			SD.ObjectID as SubID,
			SD.[Object] + cast(SD.ObjectID as varchar) as SubjectID,
			case SD.[Object] when 'FusionAttribute' then SD.TextPath else SD.Name end as [Subject],
			SD.ObjectTypeName as SubjectType,
			SD.IconBackColor as SubjectBackColor,
			SD.IconForeColor as SubjectForeColor,
			ED.[Object] as Obj,
			ED.ObjectID as ObjID,
			ED.[Object] + cast(ED.ObjectID as varchar) as ObjectID,
			case ED.[Object] when 'FusionAttribute' then ED.TextPath else ED.Name end as [Object],
			ED.ObjectTypeName as ObjectType,
			ED.IconBackColor as ObjectBackColor,
			ED.IconForeColor as ObjectForeColor,
			PP.Phrase as Predicate 
	from	IntersectMap IM
			inner join IntersectNode SN on SN.ID = IM.SubjectIntersectNodeID
			inner join cache.ObjectDetails SD on SD.[Object] = SN.ObjectType and SD.ObjectID = SN.ObjectID
			inner join IntersectNode EN on EN.ID = IM.ObjectIntersectNodeID
			inner join cache.ObjectDetails ED on ED.[Object] = EN.ObjectType and ED.ObjectID = EN.ObjectID
			inner join PredicatePhrase PP on PP.ID = IM.PredicatePhraseID
			inner join Predicate P on P.ID = PP.PredicateID
	where	IM.MapID = @mapID
end
GO