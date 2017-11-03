CREATE procedure [tile].[GetObjectStatistics]
	@type varchar(50),
	@id int
AS
BEGIN
	declare @table table (Name nvarchar(250), Value varchar(250), [Group] varchar(25), Url varchar(250), MostRecent datetime, TypeID int)
	
	declare @ObjectScore varchar(250)

	insert into @table
		select NULL, count(1), 'Followers', '/overlays/' + @type + '/' + cast(@id as varchar(10)) + '/followers', max(datecreated),null
		from	Follow F
		inner join reporting.Global_Resource R on R.ResourceID = F.ResourceID
		where	F.ObjectType = @type and F.ObjectID = @id
	
	insert into @table
		select	NULL, count(1), 'Comments', '/overlays/' + @type + '/' + cast(@id as varchar(10)) + '/comments', max(datecreated),null
		from	Comment C
				inner join CommentRelation R	on R.CommentID = C.ID and C.ParentID is null
												and R.ObjectType = @type and R.ObjectID = @id
                                                and C.ParentID is null
												and C.IsDeleted = 0

	select	@ObjectScore = cast(Value * 100 as int)
	from	metrics.Score
	where	getutcdate() between EffectiveStartDate and EffectiveEndDate
			and Object = @type and ObjectID = @id

	insert into @table values (null, @ObjectScore, 'Score', '/overlays/' + @type + '/' + cast(@id as varchar(10)) + '/score', null, null)

	if @type = 'Artifact'
	begin
		insert into @table 
			select		lower(T.Name),
						count(1),
						'Children',
						'/overlays/' + cast(@id as varchar(10)) + '/' + cast(T.ID as varchar(10)) + '/ChildArtifacts',
						max(A.createdon),
						T.ID
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
				'',
				max(wi.CreatedOn),
				null
			from
				Issue wi                
				inner join Artifact A on wi.Object ='Artifact' and A.ID = wi.objectid and A.ID = @id;-- and wi.iscompleted = 0;
				
	end


	select * from @table

END