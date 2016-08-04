CREATE FUNCTION GetFusionAttributesByOwningArtifact
(
	@ArtifactID int
)
RETURNS 
@tbl TABLE 
(
	ID int
)
AS
BEGIN
		declare @h table (ID int);

		with h as	(
					select	ID,
							ParentID
					from	Artifact
					where	ID = @ArtifactID
					union all
					select	P.ID,
							P.ParentID
					from	Artifact P
							inner join h as C on C.ParentID = P.ID
					)
		insert into @h
			select ID from h;
	
		--with fa as	(
		--			select	A.ID,
		--					A.ParentID
		--			from	FusionAttributeOwnerRule R
		--					inner join FusionAttributeOwnerRuleItem RI on RI.FusionAttributeOwnerRuleID = R.ID and R.RelationshipOwnerObjectType = 'Artifact'
		--					inner join @h H on H.ID = R.RelationshipOwnerObjectID
		--					inner join FusionAttribute A on (
		--													(RI.FusionAttributeID is not null and A.ID = RI.FusionAttributeID) OR 
		--													(RI.FusionAttributeID is null and A.FusionAttributeTypeID = R.ObjectID)
		--													)
		--													AND A.FusionID = R.FusionID
		--			union all
		--			select	C.ID,
		--					C.ParentID
		--			from	FusionAttribute C
		--					inner join fa P on C.ParentID = P.ID
		--			)

		with f as	(
					select	R.FusionID
					from	FusionAttributeOwnerRule R
							inner join @h H on H.ID = R.RelationshipOwnerObjectID and R.RelationshipOwnerObjectType = 'Artifact'
					)

		--INSERT INTO @tbl
		--	SELECT	ID
		--	FROM	fa

		INSERT INTO @tbl
			SELECT	distinct
					FusionID
			FROM	f
	
	RETURN 
END