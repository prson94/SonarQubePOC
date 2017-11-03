CREATE FUNCTION [utility].[GetBreadcrumbString]
(
	@Type varchar(50),
	@ID int,
	@Delimiter varchar(10)
)
RETURNS nvarchar(1000)
AS
BEGIN
	-- Declare the return variable here
	DECLARE @breadcrumb nvarchar(1000)

	IF (@Type = 'Artifact')
	BEGIN
		WITH H
		AS
		(
			SELECT	DisplayValue, 
					ParentID, 
					ID, 
					0 as [Level]
			FROM	Artifact
			WHERE	ID = @ID		
			UNION ALL
			SELECT	P.DisplayValue, 
					P.ParentID, 
					P.ID, 
					C.[level] + 1
			FROM	Artifact	P
					INNER JOIN H AS C ON C.ParentID = P.ID and @@NESTLEVEL < 6
		)

		SELECT @breadcrumb =	COALESCE(@breadcrumb + @Delimiter, '') + H.DisplayValue
								FROM	H
								ORDER BY H.level DESC

	END

	IF (@Type = 'FusionAttribute')
	BEGIN
		WITH H (Name, ParentID, ID, [level])
		AS
		(
			SELECT	Name, 
					ParentID, 
					ID, 
					0
			FROM	FusionAttribute
			WHERE	ID = @ID		
			UNION ALL
			SELECT	P.Name, 
					P.ParentID, 
					P.ID, 
					C.[level] + 1
			FROM	FusionAttribute	P
					INNER JOIN H AS C ON C.ParentID = P.ID and @@NESTLEVEL < 6
		)
	
		SELECT @breadcrumb =	COALESCE(@breadcrumb + @Delimiter, '') + H.Name
								FROM	H
								ORDER BY H.level DESC
	END

	IF (@Type = 'FusionAttributeType')
	BEGIN
		WITH H (Name, ParentID, ID, [level])
		AS
		(
			SELECT	Name, 
					ParentID, 
					ID, 
					0
			FROM	FusionAttributeType
			WHERE	ID = @ID		
			UNION ALL
			SELECT	P.Name, 
					P.ParentID, 
					P.ID, 
					C.[level] + 1
			FROM	FusionAttributeType	P
					INNER JOIN H AS C ON C.ParentID = P.ID and @@NESTLEVEL < 6
		)
	
		SELECT @breadcrumb =	COALESCE(@breadcrumb + @Delimiter, '') + H.Name
								FROM	H
								ORDER BY H.level DESC

		SELECT	@breadcrumb = FT.Name + @Delimiter + @breadcrumb
		FROM	FusionAttributeType FAT
				inner join FusionType FT on FAT.FusionTypeID = FT.ID and FAT.ID = @ID
	END

	IF (@Type = 'Policy')
	BEGIN
		WITH H
		AS
		(
			SELECT	DisplayValue, 
					ParentID, 
					ID, 
					0 as [Level]
			FROM	Policy
			WHERE	ID = @ID		
			UNION ALL
			SELECT	P.DisplayValue, 
					P.ParentID, 
					P.ID, 
					C.[level] + 1
			FROM	Policy	P
					INNER JOIN H AS C ON C.ParentID = P.ID and @@NESTLEVEL < 6
		)

		SELECT @breadcrumb =	COALESCE(@breadcrumb + @Delimiter, '') + H.DisplayValue
								FROM	H
								ORDER BY H.level DESC

	END

	IF (@Type = 'Taxonomy')
	BEGIN
		WITH H (DisplayValue, CatalogID, ParentID, ID, [level])
		AS
		(
			SELECT	DisplayValue, 
					TaxonomyTypeID, 
					ParentID, 
					ID, 
					0
			FROM	Taxonomy
			WHERE	ID = @ID		
			UNION ALL
			SELECT	P.DisplayValue, 
					P.TaxonomyTypeID, 
					P.ParentID, 
					P.ID, 
					C.[level] + 1
			FROM	Taxonomy	P
					INNER JOIN H AS C ON C.ParentID = P.ID and @@NESTLEVEL < 6
		)
	
		SELECT @breadcrumb =	COALESCE(@breadcrumb + @Delimiter, '') + H.DisplayValue
								FROM	H
								ORDER BY H.level DESC

		SELECT	@breadcrumb = T.Name + @Delimiter +  @breadcrumb
		FROM	TaxonomyType T 
				INNER JOIN Taxonomy O ON T.ID = O.TaxonomyTypeID WHERE O.ID = @ID 
	END

	RETURN @breadcrumb
END
