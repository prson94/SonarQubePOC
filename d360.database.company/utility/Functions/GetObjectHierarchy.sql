CREATE FUNCTION utility.GetObjectHierarchy
(
	@ObjectType varchar(25),
	@ObjectID int
)
RETURNS @tbl TABLE
(
	ObjectType varchar(25),
	ObjectID int, 
	AncestryType varchar(1)
)
AS
BEGIN
	if @ObjectType in ('Domain', 'EventType', 'FusionAttribute', 'Intersect')
	begin
		insert into @tbl values (@ObjectType, @ObjectID, 'O')
	end

	if @ObjectType = 'Artifact'
	begin
		with cteA as 
		(
			select	ID,
					ParentID,
					'O' as AncestryType
			from	Artifact
			where	ID = @ObjectID
			union all
			select	P.ID,
					P.ParentID,
					'A' as AncestryType
			from	Artifact P
					inner join cteA C on C.ParentID = P.ID
		)
		/*, cteD as 
		(
			select	ID,
					ParentID,
					'O' as AncestryType
			from	Artifact
			where	ID = @ObjectID
			union all
			select	C.ID,
					C.ParentID,
					'D' as AncestryType
			from	Artifact C
					inner join cteD P on C.ParentID = P.ID
		)*/
		insert into @tbl
			select	distinct 
					*
			from	(
					select @ObjectType as ObjectType, ID as ObjectID, AncestryType from cteA
					--union
					--select @ObjectType as ObjectType, ID as ObjectID, AncestryType from cteD		
					) H
	end

	if @ObjectType = 'Taxonomy'
	begin
		with cteA as 
		(
			select	ID,
					ParentID,
					'O' as AncestryType
			from	Taxonomy
			where	ID = @ObjectID
			union all
			select	P.ID,
					P.ParentID,
					'A' as AncestryType
			from	Taxonomy P
					inner join cteA C on C.ParentID = P.ID
		)/*, cteD as 
		(
			select	ID,
					ParentID,
					'O' as AncestryType
			from	Taxonomy
			where	ID = @ObjectID
			union all
			select	C.ID,
					C.ParentID,
					'D' as AncestryType
			from	Taxonomy C
					inner join cteD P on C.ParentID = P.ID
		)*/
		insert into @tbl
			select	distinct 
					*
			from	(
					select @ObjectType as ObjectType, ID as ObjectID, AncestryType from cteA
					--union
					--select @ObjectType as ObjectType, ID as ObjectID, AncestryType from cteD			
					) H
	end
	RETURN
END