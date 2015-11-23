CREATE procedure [tile].[GetObjectStatistics]
	@type varchar(50),
	@id int
AS
BEGIN
declare @table table (Name nvarchar(250), Value varchar(250), [Group] varchar(25), Url varchar(250))
	
	insert into @table
		select NULL, count(1), 'Followers', '/overlays/' + @type + '/' + cast(@id as varchar(10)) + '/followers'
		from	Follow
		where	ObjectType = @type and ObjectID = @id
	
	insert into @table
		select	NULL, count(1), 'Comments', '/overlays/' + @type + '/' + cast(@id as varchar(10)) + '/comments'
		from	Comment C
				inner join CommentRelation R	on R.CommentID = C.ID and C.ParentID is null
												and R.ObjectType = @type and R.ObjectID = @id
                                                and C.ParentID is null
												and C.IsDeleted = 0
	insert into @table
		select NULL, count(1), 'Events', '/overlays/' + @type + '/' + cast(@id as varchar(10)) + '/events'
			FROM	    [Event] E
					    INNER JOIN EventGroup G ON E.EventGroupID = G.ID and E.Status in ('Active', 'Open')
					    INNER JOIN [Rule] R on R.ID = G.RuleID
					    inner join cache.Relationships CR on CR.SourceObject = @type and CR.SourceObjectID = @id and CR.TargetObject = 'Rule' and CR.TargetObjectID = R.ID

	insert into @table values (null, dbo.[GetObjectStatisticScore](@type, @id) * 100, 'Score', '/overlays/' + @type + '/' + cast(@id as varchar(10)) + '/score')

	if @type = 'Artifact'
	begin
		insert into @table 
			select		lower(T.Name),
						count(1),
						'Children',
						'/overlays/' + cast(@id as varchar(10)) + '/' + cast(T.ID as varchar(10)) + '/ChildArtifacts'
			from		Artifact A
						inner join ArtifactType T on T.ID = A.ArtifactTypeID and A.ParentID = @id
			group by	T.Name,
						T.ID
			order by	T.Name
	end

	select * from @table

END