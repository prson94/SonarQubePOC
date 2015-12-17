create procedure [dbo].[FindExcludeMapIntersect]
	@type varchar(50),
	@id int
as
begin

	declare @rows table (ID int, [Subject] varchar(50), [SubjectID] int, [Object] varchar(50), ObjectID int, PredicatePhraseID int, [Level] int, [MapID] int, IntersectMapID int);

	with u as (
		select	IM.ID,
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
		select	IM.ID,
				S_N.[ObjectType] as [Subject],
				S_N.ObjectID as SubjectID,
				O_N.[ObjectType] as [Object],
				O_N.ObjectID,
				IM.PredicatePhraseID,
				U.[Level] - 1 as [Level],
				IM.MapID,
				IM.ID as IntersectMapID
		from	IntersectNode O_N
				inner join u on u.[Subject] = O_N.ObjectType and u.SubjectID = O_N.ObjectID
				inner join IntersectMap IM on O_N.ID = IM.ObjectIntersectNodeID and IM.[Type] = 1
				inner join IntersectNode S_N on S_N.ID = IM.SubjectIntersectNodeID
	)

	insert into @rows
		select * from u;

	with d as (
		select	IM.ID,
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
		select	IM.ID,
				S_N.[ObjectType] as [Subject],
				S_N.ObjectID as SubjectID,
				O_N.[ObjectType] as [Object],
				O_N.ObjectID,
				IM.PredicatePhraseID,
				d.[Level] + 1 as [Level],
				IM.MapID,
				IM.ID as IntersectMapID
		from	IntersectNode S_N
				inner join d on d.[Object] = S_N.ObjectType and d.ObjectID = S_N.ObjectID
				inner join IntersectMap IM on S_N.ID = IM.SubjectIntersectNodeID and IM.[Type] = 1
				inner join IntersectNode O_N on O_N.ID = IM.ObjectIntersectNodeID
	)

	insert into @rows
		select * from d;


	--	select * from @rows;

	with p as
	(
		select 
			[subject],
			subjectid,
			[object],
			objectid,
			IntersectMapID
		from @rows
		where objectid = @id and [object] = @type
		union all
		select
			r.[subject],
			r.subjectid,
			r.[object],
			r.objectid,
			r.IntersectMapID
		from p
		join @rows r on r.objectid = p.subjectid and r.[object] = p.[subject]
	)

	select distinct
		intersectmapid 
	from 
		p
	where 
	 not exists
		(select * from IntersectMapExclusion where IntersectMapID = p.intersectmapid and IntersectMapIDToExclude = p.intersectmapid)
	
	union all

	select
		intersectmapid
	from @rows r
	where
		[subject] = @type and subjectid = @id
	and not exists 
		(select * from IntersectMapExclusion where IntersectMapID = r.intersectmapid and IntersectMapIDToExclude = r.intersectmapid);


end
