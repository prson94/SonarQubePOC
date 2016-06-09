CREATE procedure [dbo].[GetTypeHierarchyByObject]
--declare 
	@type varchar(50),
	@id int
--set @type = 'Artifact'
--set @id = 16441--11808
as
begin
	declare @predicateType int = 3

	declare @rawResults table (
		ID int, 
		[Subject] varchar(50), SubjectID int, SubjectLevel int,
		[Object] varchar(50), ObjectID int, ObjectLevel int
	);

	declare @results table (
		--ID int, 
		[Object] varchar(50), 
		ObjectID int, 
		[Level] int,
		GroupNumber int
	);

	with u as		--Get parent hierarchy from current item.
	(
	select	I.ID,
			I.[Subject], 
			I.[SubjectID],
			1 as [SubjectLevel],
			I.[Object],
			I.[ObjectID],
			0 as [ObjectLevel]
	from	[Intersect] I
			inner join IntersectType IT on IT.ID = I.IntersectTypeID and I.Object = @type and I.ObjectID = @id
			inner join [Predicate] P on P.ID = IT.PredicateID and P.Type = @predicateType

	union all

	select	I.ID,
			I.[Subject], 
			I.[SubjectID],
			u.[SubjectLevel] + 1 as [SubjectLevel],
			I.[Object],
			I.[ObjectID], 
			u.[ObjectLevel] + 1 as [ObjectLevel]
	from	[Intersect] I
			inner join IntersectType IT on IT.ID = I.IntersectTypeID 
			inner join [Predicate] P on P.ID = IT.PredicateID and P.[Type] = @predicateType
			inner join u on u.[Subject] = I.Object and u.[SubjectID] = I.ObjectID
	),
	d as		--Get child hierarchy from current item.
	(
	select	I.ID,
			I.[Subject], 
			I.[SubjectID],
			0 as [SubjectLevel],
			I.[Object],
			I.[ObjectID],
			-1 as [ObjectLevel]
	from	[Intersect] I
			inner join IntersectType IT on IT.ID = I.IntersectTypeID and I.Subject = @type and I.SubjectID = @id
			inner join [Predicate] P on P.ID = IT.PredicateID and P.Type = @predicateType

	union all

	select	I.ID,
			I.[Subject], 
			I.[SubjectID],
			d.SubjectLevel - 1 as [SubjectLevel],
			I.[Object],
			I.[ObjectID], 
			d.[ObjectLevel] - 1 as [ObjectLevel]
	from	[Intersect] I
			inner join IntersectType IT on IT.ID = I.IntersectTypeID 
			inner join [Predicate] P on P.ID = IT.PredicateID and P.[Type] = @predicateType
			inner join d on d.[Object] = I.Subject and d.[ObjectID] = I.SubjectID
	)

	insert into @rawResults
		select * from u
		union
		select * from d;



	insert into @results
		select	distinct
				--ID, 
				Subject, 
				SubjectID, 
				SubjectLevel,
				NULL
		from	@rawResults

	insert into @results
		select	distinct
				--ID, 
				Object, 
				ObjectID, 
				ObjectLevel,
				NULL
		from	@rawResults
		where	cast(ObjectLevel as varchar) + [Object] + cast(ObjectID as varchar) 
					not in (
							select	cast([Level] as varchar) + [Object] + cast(ObjectID as varchar)
							from @results
							)

	select		R.Object,
				R.ObjectID,
				D.ObjectType,
				D.ObjectTypeID,
				D.Name,
				D.Url,
				D.ObjectTypeName,
				R.[Level],
				R.GroupNumber
	from		@results R
				inner join cache.ObjectDetails D on D.Object = R.Object and D.ObjectID = R.ObjectID
	order by	R.[Level] desc
end