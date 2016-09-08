CREATE PROCEDURE [dbo].[GetNonIntersections]
--declare
	@SourceID int,
	@TargetTypeID int,
	@SourceType varchar(250),
	@TargetType varchar(250),
	@Prefix varchar(250),
	@IntersectTypeID int
--set @SourceID = 261537
--set @TargetTypeID = 155
--set @SourceType = 'Intersect'
--set @TargetType = 'FusionAttribute'
--set @Prefix = ''
--set @IntersectTypeID = 72
AS
BEGIN
	SET NOCOUNT ON;

	declare @owners table (ObjectType varchar(25), ObjectID int, FusionAttributeID int)

	DECLARE @IDs TABLE (
						ID int
						)

	DECLARE @tbl TABLE (
						TargetUrl nvarchar(2500), 
						TargetID int, 
						TargetName nvarchar(2500), 
						TargetType nvarchar(250)
						)

	IF @TargetType = 'Event'
	BEGIN
		SET @TargetType = 'EventType'
	END

	INSERT INTO @IDs
		select TargetObjectID from cache.Relationships where SourceObject = @SourceType and SourceObjectID = @SourceID and TargetObject = @TargetType
		
	IF (@TargetType = 'Artifact')
	BEGIN
		INSERT INTO @tbl
			SELECT	dbo.GenerateObjectUrl(@TargetType, O.ArtifactTypeID, O.ID),
					O.ID,
					coalesce(O.TextPath, O.Name),
					T.Name
			FROM	Artifact O
					INNER JOIN ArtifactType T ON	O.ArtifactTypeID = T.ID
													AND O.ArtifactTypeID = @TargetTypeID
													AND O.Name LIKE '%' + @Prefix + '%'
													AND O.ID NOT IN (SELECT	ID FROM	@IDs)
	END

	IF (@TargetType = 'Domain')
	BEGIN
		INSERT INTO @tbl
			SELECT	dbo.GenerateObjectUrl(@TargetType, D.DomainTypeID, D.ID),
					D.ID,
					D.Name,
					T.Name
			FROM	Domain D
					INNER JOIN DomainType T ON	T.ID = D.DomainTypeID
													AND D.DomainTypeID = @TargetTypeID
													AND D.Name LIKE '%' + @Prefix + '%'
													AND D.ID NOT IN (SELECT	ID FROM	@IDs)
	END

	IF (@TargetType = 'FusionAttribute')
	BEGIN
		declare @OwnerSourceType varchar(50),
				@OwnerSourceID int
		IF @SourceType = 'Intersect'
		BEGIN
			select	top 1
					@OwnerSourceType = I.Subject,
					@OwnerSourceID = I.SubjectID
			from	[Intersect] I
					inner join Artifact A on I.Subject = 'Artifact' and A.ID = I.SubjectID and I.ID = @SourceID
					inner join ArtifactType AT on AT.ID = A.ArtifactTypeID and AT.CanOwnFusion = 1
		END
		ELSE
		BEGIN
			set @OwnerSourceType = @SourceType
			set @OwnerSourceID = @SourceID
		END

		declare @h table (ID int);

		if @OwnerSourceType = 'Artifact'
			begin
				with h as	(
							select	ID,
									ParentID
							from	Artifact
							where	ID = @OwnerSourceID
							union all
							select	P.ID,
									P.ParentID
							from	Artifact P
									inner join h as C on C.ParentID = P.ID
							)
				insert into @h
					select ID from h;
			end
		else
			begin
				insert into @h values (@OwnerSourceID)
			end;


		declare @fatName nvarchar(250)
		select @fatName = Name from FusionAttributeType where ID = @TargetTypeID;

		with fa as	(
					select		A.ID
					from		FusionOwner O
								inner join @h H on H.ID = O.ArtifactID
								inner join FusionAttribute A on A.FusionID = O.FusionID and A.FusionAttributeTypeID = @TargetTypeID
					group by	A.ID
					)

		INSERT INTO @tbl
			SELECT	dbo.GenerateObjectUrl(@TargetType, O.FusionAttributeTypeID, O.ID),
					O.ID,
					O.TextPath,
					@fatName
			FROM	FusionAttribute O
					INNER JOIN fa on fa.ID = O.ID 
	END

	IF (@TargetType = 'Group')
	BEGIN
		INSERT INTO @tbl
			SELECT	dbo.GenerateObjectUrl(@TargetType, ID, ID),
					ID,
					Name,
					Name
			FROM	[Group]
			WHERE	Name LIKE '%' + @Prefix + '%' 
					 AND ID NOT IN (SELECT	ID FROM	@IDs)
	END

	IF (@TargetType = 'Policy')
	BEGIN
		INSERT INTO @tbl
			SELECT	dbo.GenerateObjectUrl(@TargetType, ID, ID),
					ID,
					TextPath,
					Name
			FROM	[Policy]
			WHERE	Name LIKE '%' + @Prefix + '%' 
					 AND ID NOT IN (SELECT	ID FROM	@IDs)
	END

	IF (@TargetType = 'Resource')
	BEGIN
		INSERT INTO @tbl
			SELECT	dbo.GenerateObjectUrl('ResourceType', ResourceID, ResourceID),
					ResourceID,
					LastName + ', ' + FirstName,
					LastName + ', ' + FirstName
			FROM	reporting.[Global_Resource]
			WHERE	ResourceID > 0
					AND LastName LIKE '%' + @Prefix + '%' 
					AND ResourceID NOT IN (SELECT	ID FROM	@IDs)
	END

	IF (@TargetType = 'Rule')
	BEGIN
		INSERT INTO @tbl
			SELECT	dbo.GenerateObjectUrl(@TargetType, ID, ID),
					ID,
					Name,
					Name
			FROM	[Rule]
			WHERE	Name LIKE '%' + @Prefix + '%' 
					 AND ID NOT IN (SELECT	ID FROM	@IDs)
	END

	IF (@TargetType = 'Taxonomy')
	BEGIN
		INSERT INTO @tbl
			SELECT	dbo.GenerateObjectUrl(@TargetType, B.TaxonomyTypeID, B.ID),
					B.ID,
					B.TextPath,
					C.Name
			FROM	Taxonomy B
					INNER JOIN TaxonomyType C ON	C.ID = B.TaxonomyTypeID
													AND B.TaxonomyTypeID = @TargetTypeID
													AND B.Name LIKE '%' + @Prefix + '%'
													AND B.ID NOT IN (SELECT	ID FROM	@IDs)
	END

	SELECT * FROM @tbl
END