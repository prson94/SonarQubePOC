CREATE FUNCTION [lineage].[GetTrailForObject]
(	
	@Object varchar(50), 
	@ObjectID int,
	@Forward bit
)
RETURNS @tbl TABLE
(
	IntersectID int, 
	IntersectTypeID int, 
	[Subject] varchar(50), 
	SubjectID int, 
	[Object] varchar(50), 
	ObjectID int, 
	[State] int, 
	PredicateID int, 
	PredicateName varchar(max), 
	PredicateInverse varchar(max), 
	PredicateType int, 
	Visited bit
)
AS
BEGIN


	--TESTING---------------------
	--declare @tbl table (IntersectID int, IntersectTypeID int, Subject varchar(50), SubjectID int, Object varchar(50), ObjectID int, State int, PredicateID int, PredicateName varchar(max), PredicateInverse varchar(max), PredicateType int, Visited bit);
	--declare @Object varchar(50);
	--declare @ObjectID int;
	--declare @Forward bit;

	--select @Object = 'Artifact',
	--	   @ObjectID = 973683,
	--	   @Forward = 1;
	-------------------------------


	insert into @tbl
	select 
		P.*,
		0 as Visited 
	from PredicateIntersect P
	where 
		((@Forward = 1 and [Subject] = @Object and SubjectID = @ObjectID) OR
		(@Forward = 0 and [Object] = @Object and ObjectID = @ObjectID)) AND
		PredicateType = 1;
		

	declare @i int;
	select @i = count(*) from @tbl where Visited = 0;

	while @i != 0
	begin
		declare @intersectId int;
		select top 1 @intersectId = IntersectID from @tbl where Visited = 0; 

		update @tbl
		set Visited = 1
		where IntersectID = @intersectId;

		insert into @tbl
		select 
			P.*,
			0 as Visited 
		from PredicateIntersect P
		cross apply (select * from @tbl where IntersectID = @intersectId) I
		where 
			((@Forward = 1 and P.[Subject] = I.[Object] and P.SubjectID = I.ObjectID) OR
			(@Forward = 0 and P.[Object] = I.[Subject] and P.ObjectID = I.SubjectID)) AND
			P.PredicateType = 1 AND P.IntersectID not in (select IntersectID from @tbl);

		select @i = count(*) from @tbl where Visited = 0;
	end

	RETURN
END
