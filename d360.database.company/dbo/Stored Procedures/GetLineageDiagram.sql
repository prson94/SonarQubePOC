create procedure [dbo].[GetLineageDiagram]
--declare
	@type varchar(50),
	@id int
--set @type = 'Artifact'
--set @id = 4651
as
begin
	
	declare @rows table (ID int, [Subject] varchar(50), [SubjectID] int, [Object] varchar(50), ObjectID int, PredicateID int, [Level] int);
	
	-- Process upstream
	declare @level int = 1

	insert into @rows
		select	M.ID,
				SR.SourceObject,
				SR.SourceObjectID,
				@type as TargetObject,
				@id as TargetObjectID,
				M.[PredicateID],
				@level as [Level]
		from	IntersectMap M
				inner join cache.Relationship SR on SR.TargetIntersectNodeID = M.ObjectIntersectNodeID and M.[Type] = 1 and SR.TargetObject = @type and SR.TargetObjectID = @id
				--inner join cache.Relationship TR on TR.TargetIntersectNodeID = M.ObjectIntersectNodeID and M.[Type] = 1 and TR.TargetObject = @type and TR.TargetObjectID = @id;

	while exists(
			select	1
			from	@rows as d
					inner join cache.Relationship R on R.TargetObject = d.[Subject] and R.TargetObjectID = d.[SubjectID]
					inner join IntersectMap M on M.ID not in (select ID from @rows) and M.[Type] = 1 and R.TargetIntersectNodeID = M.ObjectIntersectNodeID
			where	exists(select 1 from cache.Relationship where SourceObject = @type and SourceObjectID = @id and TargetObject = R.SourceObject and TargetObjectID = R.SourceObjectID)
					--and d.[Level] between 1 and (@level-1)
		)
	begin
		set @level = @level + 1
		insert into @rows
			select	M.ID,
					R.SourceObject,
					R.SourceObjectID,
					d.[Subject] as TargetObject,
					d.SubjectID as TargetObjectID,
					M.[PredicateID],
					@level
			from	@rows as d
					inner join cache.Relationship R on R.TargetObject = d.[Subject] and R.TargetObjectID = d.[SubjectID]
					inner join IntersectMap M on M.ID not in (select ID from @rows) and M.[Type] = 1 and R.TargetIntersectNodeID = M.ObjectIntersectNodeID
			where	exists(select 1 from cache.Relationship where SourceObject = @type and SourceObjectID = @id and TargetObject = R.SourceObject and TargetObjectID = R.SourceObjectID)
					--and d.[Level] between 1 and (@level-1)
	end

	-- Process downstream
	set @level = -1
	
	insert into @rows
		select	M.ID,
				@type as SourceObject,
				@id as SourceObjectID,
				R.TargetObject,
				R.TargetObjectID,
				M.[PredicateID],
				@level as [Level]
		from	IntersectMap M
				inner join cache.Relationship R on R.SourceIntersectNodeID = M.SubjectIntersectNodeID and M.[Type] = 1 and R.SourceObject = @type and R.SourceObjectID = @id;


	while exists(
			select	1
			from	@rows as d
					inner join cache.Relationship R on R.SourceObject = d.[Object] and R.SourceObjectID = d.[ObjectID]
					inner join IntersectMap M on M.ID not in (select ID from @rows) and M.[Type] = 1 and R.SourceIntersectNodeID = M.SubjectIntersectNodeID
			where	exists(select 1 from cache.Relationship where SourceObject = @type and SourceObjectID = @id and TargetObject = R.TargetObject and TargetObjectID = R.TargetObjectID)
					and d.[Level] between -1 and (@level+1)
		)
	begin
		set @level = @level - 1
		insert into @rows
			select	M.ID,
					d.[Object] as SourceObject,
					d.[ObjectID] as SourceObjectID,
					R.TargetObject as TargetObject,
					R.TargetObjectID as TargetObjectID,
					M.[PredicateID],
					@level
			from	@rows as d
					inner join cache.Relationship R on R.SourceObject = d.[Object] and R.SourceObjectID = d.[ObjectID]
					inner join IntersectMap M on M.ID not in (select ID from @rows) and M.[Type] = 1 and R.SourceIntersectNodeID = M.SubjectIntersectNodeID
			where	exists(select 1 from cache.Relationship where SourceObject = @type and SourceObjectID = @id and TargetObject = R.TargetObject and TargetObjectID = R.TargetObjectID)
					and d.[Level] between -1 and (@level+1)
	end

	select	R.ID as IntersectMapID,
			R.[Level],
			S.[Object] as Sub,
			S.ObjectID as SubID,
			case R.[Level] when -1 then '0' else cast(R.[Level] as varchar) end + S.[Object] + cast(S.ObjectID as varchar) as SubjectID,
			S.TextPath as [Subject],
			S.ObjectTypeName as SubjectType,
			S.IconBackColor as SubjectBackColor,
			S.IconForeColor as SubjectForeColor,
			O.[Object] as Obj,
			O.ObjectID as ObjID,
			cast(R.[Level]-1 as varchar) + O.[Object] + cast(O.ObjectID as varchar) as ObjectID,
			O.TextPath as [Object],
			O.ObjectTypeName as ObjectType,
			O.IconBackColor as ObjectBackColor,
			O.IconForeColor as ObjectForeColor,
			P.Name as Predicate,
			0 as Exclude
	from	@rows R
			inner join cache.ObjectDetails S on S.[Object] = R.[Subject] and S.ObjectID = R.SubjectID
			inner join cache.ObjectDetails O on O.[Object] = R.[Object] and O.ObjectID = R.ObjectID
			inner join Predicate P on P.ID = R.PredicateID
end
