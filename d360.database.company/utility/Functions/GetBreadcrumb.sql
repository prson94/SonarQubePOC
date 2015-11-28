CREATE FUNCTION [utility].[GetBreadcrumb]
(
	@Type varchar(50),
	@ID int
)
RETURNS XML
AS
BEGIN
	-- Declare the return variable here
	DECLARE @breadcrumb xml
	SET @breadcrumb = '<root/>'

	IF (@Type = 'Artifact')
	BEGIN
		WITH H (Name, ParentID, ID, [level])
		AS
		(
			SELECT	Name, 
					ParentID, 
					ID, 
					0
			FROM	Artifact
			WHERE	ID = @ID		
			UNION ALL
			SELECT	P.Name, 
					P.ParentID, 
					P.ID, 
					C.[level] + 1
			FROM	Artifact	P
					INNER JOIN H AS C ON C.ParentID = P.ID and @@NESTLEVEL < 6
		)
	
		SELECT @breadcrumb =	(
								SELECT (
										SELECT	H.ID as "node/@id",
												H.Name as "node/@name"
										FROM	H
										ORDER BY H.level DESC
										FOR XML PATH(''), type
										) AS hierachy
								FOR XML PATH('')
								)
	END

	IF (@Type = 'Domain')
	BEGIN
		WITH H (Name, ParentID, ID, [level])
		AS
		(
			SELECT	Name, 
					ParentID, 
					ID, 
					0
			FROM	Domain
			WHERE	ID = @ID		
			UNION ALL
			SELECT	P.Name, 
					P.ParentID, 
					P.ID, 
					C.[level] + 1
			FROM	Domain	P
					INNER JOIN H AS C ON C.ParentID = P.ID and @@NESTLEVEL < 6
		)
	
		SELECT @breadcrumb =	(
								SELECT (
										SELECT	H.ID as "node/@id",
												H.Name as "node/@name"
										FROM	H
										ORDER BY H.level DESC
										FOR XML PATH(''), type
										) AS hierachy
								FOR XML PATH('')
								)
	END


	IF (@Type = 'FusionAttribute')
	BEGIN
		WITH H
		AS
		(
			SELECT	A.Name, 
					T.Name as [Type],
					A.ParentID, 
					A.ID, 
					0 as [Level]
			FROM	FusionAttribute A
					inner join FusionAttributeType T on T.ID = A.FusionAttributeTypeID
			WHERE	A.ID = @ID		
			UNION ALL
			SELECT	P.Name, 
					T.Name as [Type],
					P.ParentID, 
					P.ID, 
					C.[level] + 1 as [Level]
			FROM	FusionAttribute	P
					inner join FusionAttributeType T on T.ID = P.FusionAttributeTypeID and @@NESTLEVEL < 6
					INNER JOIN H AS C ON C.ParentID = P.ID
		)
	
		SELECT @breadcrumb =	(
								SELECT (
										SELECT	H.ID as "node/@id",
												H.Name as "node/@name",
												H.Type as "node/@type"
										FROM	H
										ORDER BY H.level DESC
										FOR XML PATH(''), type
										) AS hierachy
								FOR XML PATH('')
								)
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
	
		SELECT @breadcrumb =	(
								SELECT (
										SELECT	H.ID as "node/@id",
												H.Name as "node/@name"
										FROM	H
										ORDER BY H.level DESC
										FOR XML PATH(''), type
										) AS hierachy
								FOR XML PATH('')
								)
	END

	IF (@Type = 'Taxonomy')
	BEGIN
		WITH H (Name, CatalogID, ParentID, ID, [level])
		AS
		(
			SELECT	Name, 
					TaxonomyTypeID, 
					ParentID, 
					ID, 
					0
			FROM	Taxonomy
			WHERE	ID = @ID		
			UNION ALL
			SELECT	P.Name, 
					P.TaxonomyTypeID, 
					P.ParentID, 
					P.ID, 
					C.[level] + 1
			FROM	Taxonomy	P
					INNER JOIN H AS C ON C.ParentID = P.ID and @@NESTLEVEL < 6
		)
	
		SELECT @breadcrumb =	(
								SELECT (
										SELECT	H.ID as "node/@id",
												H.Name as "node/@name"
										FROM	H
										ORDER BY H.level DESC
										FOR XML PATH(''), type
										) AS hierachy
								FOR XML PATH('')
								)

		DECLARE @cName nvarchar(250)
		DECLARE @cID int
		SELECT	@cID = TT.ID,
				@cName = TT.Name
		FROM	TaxonomyType TT
				INNER JOIN Taxonomy T ON T.TaxonomyTypeID = TT.ID
		WHERE	T.ID = @ID
		SET @breadcrumb.modify('insert <catalog id="" name="" /> as first into (/hierachy)[1]') 
		SET @breadcrumb.modify(
		'replace value of (//catalog/@id)[1] 
		 with sql:variable("@cID")'
		)
		SET @breadcrumb.modify(
		'replace value of (//catalog/@name)[1] 
		 with sql:variable("@cName")'
		)
	END

	RETURN @breadcrumb
END
GO