CREATE FUNCTION [utility].[GetFormattedFieldLookupValue]
(
	@Type varchar(25),
	@DisplayFormat nvarchar(250),
	@LookupObjectType varchar(25),
	@LookupObjectID int,
	@Value nvarchar(4000)
)
RETURNS nvarchar(4000)
AS
BEGIN
	declare @formattedValue nvarchar(4000)

	if @LookupObjectType is null
	begin
		set @formattedValue  = @Value

		if @Type = 'Link' OR @Type = 'UncLink'
		begin
			declare @linkName nvarchar(4000),
					@linkUrl nvarchar(4000)
					
			SELECT @linkName = SUBSTRING(@Value, 1, PATINDEX('%|%', @Value)-1)
			SELECT @linkUrl = SUBSTRING(@Value, PATINDEX('%|%', @Value)+1, LEN(@Value))

			set @formattedValue = '<a href="' + @linkUrl + '" target="_blank">' + @linkName + '</a>'
		end

	end
	else
	begin

		declare @tDisplayFormat nvarchar(250)
		declare @tokens table(ID int identity(1,1), pos int, Token nvarchar(100), Field nvarchar(100))
		declare @fieldValues table(Field nvarchar(100), Value nvarchar(4000), LookupObjectType nvarchar(250), LookupObjectID int, LookupDisplayFormat nvarchar(250))

		set @formattedValue = @DisplayFormat
		SET @tDisplayFormat = @DisplayFormat
	

		declare @pos int
		declare @oldpos int
		select @oldpos = 0
		select @pos=patindex('%{%',@DisplayFormat) 
		while @pos > 0 and @oldpos<>@pos
		 begin
			declare @txt nvarchar(100)
			SELECT @txt = SUBSTRING(@tDisplayFormat, @pos, PATINDEX('%}%', @tDisplayFormat))

			insert into @tokens Values (@pos, @txt, SUBSTRING(@txt, 2, LEN(@txt)-2))
			Select @oldpos = @pos
			select @pos = patindex('%{%',Substring(@DisplayFormat, @pos + 1, len(@DisplayFormat))) + @pos
		end
		--WHILE(PATINDEX('%{%', @tDisplayFormat) > 0)
		--BEGIN
		--	declare @txt nvarchar(100)
		--	SELECT @txt = SUBSTRING(@tDisplayFormat, PATINDEX('%{%', @tDisplayFormat), PATINDEX('%}%', @tDisplayFormat))
		--	IF NOT EXISTS(SELECT 1 FROM @tokens WHERE Token = @txt)
		--	BEGIN
		--		INSERT INTO @tokens VALUES (
		--			@txt,
		--			(select SUBSTRING(@txt, 2, LEN(@txt)-2))
		--		)
		--	END
		--	SET @tDisplayFormat = stuff(@tDisplayFormat, charindex(@txt, @tDisplayFormat), len(@txt), '') --REPLACE(SUBSTRING(@tDisplayFormat, 1, PATINDEX('%}%', @tDisplayFormat)), @txt, '') + SUBSTRING(@tDisplayFormat, PATINDEX('%}%', @tDisplayFormat), LEN(@tDisplayFormat))
		--END

		insert into @fieldValues
			select	distinct
					V.Name,
					V.Value,
					V.LookupObjectType,
					V.LookupObjectID,
					V.LookupDisplayFormat
			from	(
					SELECT	ID,
							Name,
							'Artifact' as ObjectType
					FROM	ArtifactType
					WHERE	@LookupObjectType = 'Artifact' and ID = @LookupObjectID
					UNION
					SELECT	ID,
							Name,
							'Domain' as ObjectType
					FROM	DomainType
					WHERE	@LookupObjectType = 'Domain' and ID = @LookupObjectID
					UNION
					SELECT	ID,
							Name,
							'DomainItem' as ObjectType
					FROM	Domain
					WHERE	@LookupObjectType = 'DomainItem' and ID = @LookupObjectID
					UNION
					SELECT	ID,
							Name,
							'Lookup' as ObjectType
					FROM	[LookupType]
					WHERE	@LookupObjectType = 'Lookup' and ID = @LookupObjectID
					UNION
					SELECT	1 as ID,
							'User' as Name,
							'Resource' as ObjectType
					WHERE	@LookupObjectType = 'Resource'-- and ID = @LookupObjectID
					UNION
					SELECT	ID,
							Name,
							'Taxonomy' as ObjectType
					FROM	TaxonomyType
					WHERE	@LookupObjectType = 'Taxonomy' and ID = @LookupObjectID
					) L
					outer apply (

								SELECT	IT.Name,
										[IF].Value,
										[IT].LookupObjectType,
										COALESCE([IT].LookupObjectID, 0) as LookupObjectID,
										[IT].LookupDisplayFormat
								FROM	Field [IF]
										inner join FieldType IT ON [IF].FieldTypeID = IT.ID 
																and [IF].ObjectType = L.ObjectType
																and [IF].ObjectID = case 
																						when dbo.IsInteger(@Value) = 1 then @Value
																						else 0
																					end
								
								UNION

								SELECT	P.FieldName as Name,
										p.FieldValue as Value,
										NULL as LookupObjectType,
										NULL as LookupObjectID,
										NULL as LookupDisplayFormat
								FROM	(
										SELECT	ID,
												CAST(Name as nvarchar(4000)) as Name,
												Description
										FROM	Artifact A
										WHERE	A.ID = CAST(@Value as int)
												and L.ObjectType = 'Artifact'
										) A
										unpivot	(
												FieldValue for FieldName in (Name, Description)
												) p

								UNION

								SELECT	P.FieldName as Name,
										p.FieldValue as Value,
										NULL as LookupObjectType,
										NULL as LookupObjectID,
										NULL as LookupDisplayFormat
								FROM	(
										SELECT	ID,
												CAST(Name as nvarchar(4000)) as Name,
												Description
										FROM	Domain A
										WHERE	A.ID = @Value
												and L.ObjectType = 'Domain'
										) A
										unpivot	(
												FieldValue for FieldName in (Name, Description)
												) p

								UNION

								SELECT	P.FieldName as Name,
										p.FieldValue as Value,
										NULL as LookupObjectType,
										NULL as LookupObjectID,
										NULL as LookupDisplayFormat
								FROM	(
										SELECT	ID,
												CAST(Code as nvarchar(4000)) as Code,
												CAST(Name as nvarchar(4000)) as Name,
												Description
										FROM	DomainItem A
										WHERE	A.ID = @Value
												and L.ObjectType = 'DomainItem'
										) A
										unpivot	(
												FieldValue for FieldName in (Code, Name, Description)
												) p

								UNION

								SELECT	P.FieldName as Name,
										p.FieldValue as Value,
										NULL as LookupObjectType,
										NULL as LookupObjectID,
										NULL as LookupDisplayFormat
								FROM	(
										SELECT	ResourceID as ID,
												CAST(FirstName as nvarchar(4000)) as FirstName,
												CAST(LastName as nvarchar(4000)) as LastName,
												CAST(Email as nvarchar(4000)) as Email
										FROM	reporting.Global_Resource A
										WHERE	A.ResourceID = @Value
												and L.ObjectType = 'Resource'
										) A
										unpivot	(
												FieldValue for FieldName in (FirstName, LastName, Email)
												) p
								) V

		declare @current int,
				@max int

		set @current = 1
		select @max = Max(ID) from @tokens

		while(@current <= @max)
		begin
			declare @currentToken nvarchar(100) = null,
					@currentField nvarchar(100) = null,
					@currentValue nvarchar(4000) = null,
					@lkpType nvarchar(250) = null, 
					@lkpID int = null, 
					@lkpFormat nvarchar(250) = null

			select	@currentField = Field, 
					@currentToken = Token 
			from	@tokens
			where	ID = @current

			select	@currentValue = Value,
					@lkpType = LookupObjectType,
					@lkpID = LookupObjectID,
					@lkpFormat = LookupDisplayFormat
			from	@fieldValues 
			where	Field = @currentField

			if @currentValue is not null
			begin
				if @lookupObjectType is not null and @lkpID is not null
				begin
					select @currentValue = utility.GetFormattedFieldLookupValue(@Type, @lkpFormat, @lkpType, @lkpID, @currentValue)
				end

				SET @formattedValue = REPLACE(@formattedValue, @currentToken, @currentValue)
			end

			SET @current = @current + 1
		end
	end

	return @formattedValue
END
