declare @it int = 100; --the intersecttype id for the see also predicate

declare @tbl table (SubjectID int, ObjectID int)
declare @maxGroup int = 43,
		@groupID int = 1,
		@current int = 0

while @groupID <= @maxGroup
begin
	declare @iTable table (ID int)
	delete @iTable

	insert into @iTable
		select	ArtifactID
		from	RelatedArtifact
		where	GroupID = @groupID
		order by ArtifactID asc

	declare @count int = 0
	select	@count = count(1)
	from	@iTable

	while @count > 0
	begin
		select	top 1
				@current = ID
		from	@iTable
	
		insert into @tbl
			select	@current,
					ID
			from	@iTable
			where ID >  @current
		
		delete @iTable where ID = @current
		set @count = @count - 1
	end

	set @groupID = @groupID + 1
end

INSERT INTO [dbo].[Intersect] ([IntersectTypeID], [Classification],[Subject],[SubjectID],[Object],[ObjectID],[Deleted],[CreatedBy],[CreatedOn],[UpdatedBy],[UpdatedOn])
	select @it, 2, 'Artifact', SubjectID, 'Artifact', ObjectID, 0, 0, getutcdate(), 0, getutcdate() from @tbl


