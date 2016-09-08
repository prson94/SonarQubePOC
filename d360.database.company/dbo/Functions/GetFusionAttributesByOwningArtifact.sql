CREATE FUNCTION [dbo].[GetFusionAttributesByOwningArtifact]
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
	
		with f as	(
					select	R.FusionID
					from	FusionOwner R
							inner join @h H on H.ID = R.ArtifactID
					)

		INSERT INTO @tbl
			SELECT	distinct
					FusionID
			FROM	f
	RETURN 
END