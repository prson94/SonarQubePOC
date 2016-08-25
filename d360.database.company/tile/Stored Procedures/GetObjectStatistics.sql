create procedure [tile].[GetObjectStatistics]
	@type varchar(50),
	@id int
AS
BEGIN
declare @table table (Name nvarchar(250), Value varchar(250), [Group] varchar(25), Url varchar(250), MostRecent datetime)
	
	insert into @table
		select NULL, count(1), 'Followers', '/overlays/' + @type + '/' + cast(@id as varchar(10)) + '/followers', max(datecreated)
		from	Follow F
		inner join reporting.Global_Resource R on R.ResourceID = F.ResourceID
		where	F.ObjectType = @type and F.ObjectID = @id
	
	insert into @table
		select	NULL, count(1), 'Comments', '/overlays/' + @type + '/' + cast(@id as varchar(10)) + '/comments', max(datecreated)
		from	Comment C
				inner join CommentRelation R	on R.CommentID = C.ID and C.ParentID is null
												and R.ObjectType = @type and R.ObjectID = @id
                                                and C.ParentID is null
												and C.IsDeleted = 0
	insert into @table
		select NULL, count(1), 'Events', '/overlays/' + @type + '/' + cast(@id as varchar(10)) + '/events', max([date])
			FROM	    [Event] E
					    INNER JOIN EventGroup G ON E.EventGroupID = G.ID and E.Status in ('Active', 'Open')
					    INNER JOIN [Rule] R on R.ID = G.RuleID
					    inner join cache.Relationships CR on CR.SourceObject = @type and CR.SourceObjectID = @id and CR.TargetObject = 'Rule' and CR.TargetObjectID = R.ID

	insert into @table values (null, dbo.[GetObjectStatisticScore](@type, @id) * 100, 'Score', '/overlays/' + @type + '/' + cast(@id as varchar(10)) + '/score', null)

	if @type = 'Artifact'
	begin
		insert into @table 
			select		lower(T.Name),
						count(1),
						'Children',
						'/overlays/' + cast(@id as varchar(10)) + '/' + cast(T.ID as varchar(10)) + '/ChildArtifacts',
						max(A.createdon)
			from		Artifact A
						inner join ArtifactType T on T.ID = A.ArtifactTypeID and A.ParentID = @id
			group by	T.Name,
						T.ID
			order by	T.Name


		insert into @table
			select	
				'Issue',
				count(1),
				'Issues',
				'/overlays/Artifact/' + cast(@id as varchar(10)) + '/Issues',
				max(w.datestarted)
			from	
					workflow w
					inner join Comment C on C.ID = w.data.value('(fields/CommentID)[1]', 'int')
					inner join CommentRelation CR on CR.CommentID = C.ID and CR.ObjectType = 'Artifact'
					inner join Artifact A on w.workflowtype = 3 and w.datecompleted is null and A.ID = cr.objectid
			where 
				a.id = @id			
	end


	select * from @table

END


GO