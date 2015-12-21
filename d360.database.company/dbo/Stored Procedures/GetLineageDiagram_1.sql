
CREATE procedure [dbo].[GetLineageDiagram]
--declare
	@type varchar(50),
	@id int
--set @object = 'Artifact'
--set @id = 972861--972859
as
begin

	declare @rows table (IntersectID int, ID int, [Subject] varchar(50), [SubjectID] int, [Object] varchar(50), ObjectID int, PredicatePhraseID int, [Level] int, [MapID] int, IntersectMapID int);

	with u as (
		select	O_N.IntersectID,
				IM.ID,
				S_N.[ObjectType] as [Subject],
				S_N.ObjectID as SubjectID,
				O_N.[ObjectType] as [Object],
				O_N.ObjectID,
				IM.PredicatePhraseID,
				0 as [Level],
				IM.MapID,
				IM.ID as IntersectMapID
		from	IntersectNode O_N
				inner join IntersectMap IM on O_N.ID = IM.ObjectIntersectNodeID and IM.[Type] = 1 and O_N.ObjectType = @type and O_N.ObjectID = @id
				inner join IntersectNode S_N on S_N.ID = IM.SubjectIntersectNodeID
		union all
		select	O_N.IntersectID,
				IM.ID,
				S_N.[ObjectType] as [Subject],
				S_N.ObjectID as SubjectID,
				O_N.[ObjectType] as [Object],
				O_N.ObjectID,
				IM.PredicatePhraseID,
				U.[Level] - 1 as [Level],
				IM.MapID,
				IM.ID as IntersectMapID
		from	IntersectNode O_N
				inner join u on u.[Subject] = O_N.ObjectType and u.SubjectID = O_N.ObjectID and u.IntersectID <> O_N.IntersectID
				inner join IntersectMap IM on O_N.ID = IM.ObjectIntersectNodeID and IM.[Type] = 1
				inner join IntersectNode S_N on S_N.ID = IM.SubjectIntersectNodeID
		where	U.[Level] > -15
	)


	insert into @rows
		select * from u;

	with d as (
		select	S_N.IntersectID,
				IM.ID,
				S_N.[ObjectType] as [Subject],
				S_N.ObjectID as SubjectID,
				O_N.[ObjectType] as [Object],
				O_N.ObjectID,
				IM.PredicatePhraseID,
				0 as [Level],
				IM.MapID,
				IM.ID as IntersectMapID
		from	IntersectNode S_N
				inner join IntersectMap IM on S_N.ID = IM.SubjectIntersectNodeID and IM.[Type] = 1 and S_N.ObjectType = @type and S_N.ObjectID = @id
				inner join IntersectNode O_N on O_N.ID = IM.ObjectIntersectNodeID
		union all
		select	S_N.IntersectID,
				IM.ID,
				S_N.[ObjectType] as [Subject],
				S_N.ObjectID as SubjectID,
				O_N.[ObjectType] as [Object],
				O_N.ObjectID,
				IM.PredicatePhraseID,
				d.[Level] + 1 as [Level],
				IM.MapID,
				IM.ID as IntersectMapID
		from	IntersectNode S_N
				inner join d on d.[Object] = S_N.ObjectType and d.ObjectID = S_N.ObjectID and d.IntersectID <> S_N.IntersectID
				inner join IntersectMap IM on S_N.ID = IM.SubjectIntersectNodeID and IM.[Type] = 1
				inner join IntersectNode O_N on O_N.ID = IM.ObjectIntersectNodeID
		where	d.[Level] < 15
	)

	insert into @rows
		select * from d

	select	R.ID,
			R.MapID,
			R.IntersectMapID,
			R.[Level],
			S.[Object] as Sub,
			S.ObjectID as SubID,
			--cast(R.IntersectMapID as varchar) +  
			S.[Object] + cast(S.ObjectID as varchar) as SubjectID,
			case S.[Object] when 'FusionAttribute' then S.TextPath else S.Name end as [Subject],
			S.ObjectTypeName as SubjectType,
			S.IconBackColor as SubjectBackColor,
			S.IconForeColor as SubjectForeColor,
			O.[Object] as Obj,
			O.ObjectID as ObjID,
			--cast(R.IntersectMapID as varchar) +  
			O.[Object] + cast(O.ObjectID as varchar) as ObjectID,
			case O.[Object] when 'FusionAttribute' then O.TextPath else O.Name end as [Object],
			O.ObjectTypeName as ObjectType,
			O.IconBackColor as ObjectBackColor,
			O.IconForeColor as ObjectForeColor,
			PP.Phrase as Predicate,
			case when X.IntersectMapIDToExclude is null then
				0
			else
				1
			end as Exclude
	from	@rows R
			inner join cache.ObjectDetails S on S.[Object] = R.[Subject] and S.ObjectID = R.SubjectID
			inner join cache.ObjectDetails O on O.[Object] = R.[Object] and O.ObjectID = R.ObjectID
			inner join PredicatePhrase PP on PP.ID = R.PredicatePhraseID
			inner join Predicate P on P.ID = PP.PredicateID
			left join IntersectMapExclusion X on X.IntersectMapIDToExclude = R.IntersectMapID
end