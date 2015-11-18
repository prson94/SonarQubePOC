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
					@OwnerSourceType = ObjectType,
					@OwnerSourceID = ObjectID
			from	IntersectNode N
					inner join Artifact A on A.ID = N.ObjectID and N.ObjectType = 'Artifact' and N.IntersectID = @SourceID
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

		with fa as	(
					select	A.ID,
							A.ParentID,
							A.FusionAttributeTypeID
					from	FusionAttributeOwnerRule R
							inner join FusionAttributeOwnerRuleItem RI on RI.FusionAttributeOwnerRuleID = R.ID and R.RelationshipOwnerObjectType = 'Artifact'
							inner join @h H on H.ID = R.RelationshipOwnerObjectID
							inner join FusionAttribute A on (
															(RI.FusionAttributeID is not null and A.ID = RI.FusionAttributeID) OR 
															(RI.FusionAttributeID is null and A.FusionAttributeTypeID = R.ObjectID)
															)
															AND A.FusionID = R.FusionID
					union all
					select	C.ID,
							C.ParentID,
							C.FusionAttributeTypeID
					from	FusionAttribute C
							inner join fa P on C.ParentID = P.ID --and P.ID <> C.ID
					)

		INSERT INTO @tbl
			SELECT	dbo.GenerateObjectUrl(@TargetType, B.FusionAttributeTypeID, B.ID),
					B.ID,
					B.TextPath,
					C.Name
			FROM	FusionAttribute B
					INNER JOIN FusionAttributeType C ON	C.ID = B.FusionAttributeTypeID
													AND B.FusionAttributeTypeID = @TargetTypeID
													AND B.ID NOT IN (SELECT	ID FROM	@IDs)
					INNER JOIN fa on fa.ID = B.ID and fa.FusionAttributeTypeID = @TargetTypeID
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

	IF (@TargetType = 'Intersect')
	BEGIN
		IF @SourceType = 'FusionAttribute'
		BEGIN
			declare @fusionID int
			select @fusionID = FusionID from FusionAttribute where ID = @SourceID
			insert into @owners
				select	RelationshipOwnerObjectType, 
						RelationshipOwnerObjectID, 
						FusionAttributeID 
				from	GetFusionOwnershipHierarchy(@fusionID, '', 0)
		END

		INSERT INTO @tbl
			SELECT	dbo.GenerateObjectUrl(@TargetType, O.IntersectTypeID, O.ID),
					O.ID,
					O.Name,
					T.Name
			FROM	[Intersect] O
					INNER JOIN IntersectType T ON	O.IntersectTypeID = T.ID
													AND T.ID = @TargetTypeID
													AND T.Name LIKE '%' + @Prefix + '%'
													AND O.ID NOT IN (SELECT	ID FROM	@IDs)
			WHERE	@SourceType <> 'FusionAttribute'
					OR	(
						@SourceType = 'FusionAttribute' and
						O.ID in (
								SELECT	I.ID
								FROM	[Intersect]	I
										INNER JOIN IntersectNode N on N.IntersectID = I.ID
										INNER JOIN @owners FO on FO.ObjectType = N.ObjectType and FO.ObjectID = N.ObjectID and FO.FusionAttributeID = @SourceID
								)
						)
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