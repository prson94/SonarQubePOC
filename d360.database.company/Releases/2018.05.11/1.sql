ALTER FUNCTION [utility].[GetBreadcrumbString]
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

	/*IF (@Type = 'Artifact')
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
					INNER JOIN H AS C ON C.ParentID = P.ID and c.[level] < 6
		)

		SELECT @breadcrumb =	COALESCE(@breadcrumb + @Delimiter, '') + H.DisplayValue
								FROM	H
								ORDER BY H.level DESC

	END*/

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
					INNER JOIN H AS C ON C.ParentID = P.ID and c.[level] < 6
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
					INNER JOIN H AS C ON C.ParentID = P.ID and c.[level] < 6
		)
	
		SELECT @breadcrumb =	COALESCE(@breadcrumb + @Delimiter, '') + H.Name
								FROM	H
								ORDER BY H.level DESC

		SELECT	@breadcrumb = FT.Name + @Delimiter + @breadcrumb
		FROM	FusionAttributeType FAT
				inner join FusionType FT on FAT.FusionTypeID = FT.ID and FAT.ID = @ID
	END

	/*IF (@Type = 'Policy')
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
					INNER JOIN H AS C ON C.ParentID = P.ID and c.[level] < 6
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
					INNER JOIN H AS C ON C.ParentID = P.ID and c.[level] < 6
		)
	
		SELECT @breadcrumb =	COALESCE(@breadcrumb + @Delimiter, '') + H.DisplayValue
								FROM	H
								ORDER BY H.level DESC

		SELECT	@breadcrumb = T.Name + @Delimiter +  @breadcrumb
		FROM	TaxonomyType T 
				INNER JOIN Taxonomy O ON T.ID = O.TaxonomyTypeID WHERE O.ID = @ID 
	END*/

	RETURN @breadcrumb
END
GO

alter table Artifact drop constraint DF_Artifact_DisplayValue
GO

alter table Artifact drop column DisplayValue
GO

ALTER FUNCTION [utility].[GetFormattedFieldLookupValueWithMultiple]
(
	@Type varchar(25),
	@DisplayFormat nvarchar(250),
	@LookupObjectType varchar(25),
	@LookupObjectID int,
	@Value nvarchar(max),
	@SupportsMultipleValues bit	
)
RETURNS nvarchar(max)
AS
BEGIN
	declare @formattedValue nvarchar(max)
	
	if @Value is null
	begin
		return null
	end

	if @LookupObjectType is null
	begin
		set @formattedValue  = @Value

		if @Type = 'Link' OR @Type = 'UncLink'
		begin
			declare @linkName nvarchar(max),
					@linkUrl nvarchar(max)

			if charindex('|', @Value, 1) > 1
				begin
					SELECT @linkName = SUBSTRING(@Value, 1, PATINDEX('%|%', @Value)-1)
					SELECT @linkUrl = SUBSTRING(@Value, PATINDEX('%|%', @Value)+1, LEN(@Value))

					set @formattedValue = '<a href="' + @linkUrl + '" target="_blank">' + @linkName + '</a>'
				end
			else
				begin
					if @Value <> '' AND @Value <> '|' AND @Value IS NOT NULL
						begin
							if LEFT(@Value, 1) = '|'
								begin
									--no name, default to url
									set @formattedValue = '<a href="' + SUBSTRING(@Value,2, LEN(@Value)) + '" target="_blank">' + SUBSTRING(@Value,2, LEN(@Value)) + '</a>'
								end
							else
								begin
									set @formattedValue = '<a href="' + @Value + '" target="_blank">' + @Value + '</a>'
								end
						end
					else
						begin
							set @formattedValue = null
						end
				end
		end

	end	
	else
	begin
		if @SupportsMultipleValues = 1
		begin	
			set @formattedValue =  utility.GenerateFormattedMultipleValue (@DisplayFormat, @LookupObjectType, @LookupObjectID, @Value)
		end
		else if @LookupObjectType = 'ReferenceItemType'
		begin
			select @formattedValue = Name from ReferenceItemType where id = @Value;		
		end
		else if @LookupObjectType = 'TaxonomyType'
		begin
			select @formattedValue = Name from TaxonomyType where id = @Value;		
		end
		else
		begin
			declare @tokens table(ID int identity(1,1), Token nvarchar(100), Field nvarchar(100))
			declare @fieldValues table(Field nvarchar(100), Value nvarchar(max), LookupObjectType nvarchar(250), LookupObjectID int, LookupDisplayFormat nvarchar(250))

			set @formattedValue = @DisplayFormat
	
			while patindex('%{%',@formattedValue) > 0
			 begin
				declare @txt nvarchar(100) = SUBSTRING(@formattedValue, patindex('%{%',@formattedValue), PATINDEX('%}%', @formattedValue))
				insert into @tokens Values (@txt, REPLACE(REPLACE(@txt,'{',''),'}',''))
				set @formattedValue = replace(@formattedValue, @txt, '')
			end

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
								'Lookup' as ObjectType
						FROM	[LookupType]
						WHERE	@LookupObjectType = 'Lookup' and ID = @LookupObjectID
						UNION
						SELECT	ID,
								Name,
								'ReferenceItem' as ObjectType
						FROM	[ReferenceItemType]
						WHERE	@LookupObjectType = 'ReferenceItem' and ID = @LookupObjectID
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
																	/*and [IF].ObjectID = case 
																							when dbo.IsInteger(@Value) = 1 then @Value
																							else 0
																						end*/
																	and [IF].ObjectID = case 
																							when TRY_CAST(@Value AS int) IS NULL  then 0 --not an int
																							else @Value -- int
																						end

								
									UNION

									SELECT	P.FieldName as Name,
											p.FieldValue as Value,
											NULL as LookupObjectType,
											NULL as LookupObjectID,
											NULL as LookupDisplayFormat
									FROM	(
											SELECT	A.ObjectID as AID,
													CAST(A.ObjectID as nvarchar(max)) as ID,
													CAST(TP.TextPath as nvarchar(max)) as TextPath
											FROM	asset A
													cross apply dbo.GetAssetTextPathById(A.ID, '/') TP 
											WHERE	A.ObjectID = CAST(@Value as int) and A.[Object] = 'Artifact' and L.ObjectType = 'Artifact'
											/*SELECT	ID as AID,
													CAST(ID as nvarchar(max)) as ID,
													CAST(DisplayValue as nvarchar(max)) as TextPath
											FROM	Artifact A
											WHERE	A.ID = CAST(@Value as int)
													and L.ObjectType = 'Artifact'*/
											) A
											unpivot	(
													FieldValue for FieldName in (ID, TextPath)
													) p

									UNION

									SELECT	P.FieldName as Name,
											p.FieldValue as Value,
											NULL as LookupObjectType,
											NULL as LookupObjectID,
											NULL as LookupDisplayFormat
									FROM	(
											SELECT	ID,
													CAST(TextPath as nvarchar(max)) as TextPath
											FROM	Taxonomy A
											WHERE	A.ID = CAST(@Value as int)
													and L.ObjectType = 'Taxonomy'
											) A
											unpivot	(
													FieldValue for FieldName in (TextPath)
													) p

									UNION

									SELECT	P.FieldName as Name,
											p.FieldValue as Value,
											NULL as LookupObjectType,
											NULL as LookupObjectID,
											NULL as LookupDisplayFormat
									FROM	(
											SELECT	ID,
													CAST(Code as nvarchar(max)) as Code
											FROM	ReferenceItem A
											WHERE	A.ID = @Value
													and L.ObjectType = 'ReferenceItem'
											) A
											unpivot	(
													FieldValue for FieldName in (Code)
													) p

									UNION

									SELECT	P.FieldName as Name,
											p.FieldValue as Value,
											NULL as LookupObjectType,
											NULL as LookupObjectID,
											NULL as LookupDisplayFormat
									FROM	(
											SELECT	ID,
													CAST(Name as nvarchar(max)) as Name,
													CAST(Description as nvarchar(max)) as Description
											FROM	ReferenceItemType A
											WHERE	A.ID = @Value
													and L.ObjectType = 'ReferenceItemType'
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
											SELECT	ResourceID as ID,
													CAST(FirstName as nvarchar(max)) as FirstName,
													CAST(LastName as nvarchar(max)) as LastName,
													CAST(Email as nvarchar(max)) as Email
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

			set @formattedValue = @DisplayFormat

			while(@current <= @max)
			begin
				declare @currentToken nvarchar(100) = null,
						@currentField nvarchar(100) = null,
						@currentValue nvarchar(max) = null,
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
						select @currentValue = utility.GetFormattedFieldLookupValueWithMultiple(@Type, @lkpFormat, @lkpType, @lkpID, @currentValue, @SupportsMultipleValues)
					end

					SET @formattedValue = REPLACE(@formattedValue, @currentToken, @currentValue)
				end
				else
				begin
					SET @formattedValue = REPLACE(@formattedValue, @currentToken, '')
				end

				SET @current = @current + 1
			end
		end
	end

	return @formattedValue
END
GO


ALTER procedure [dbo].[DeleteObject]
	@ObjTemp varchar(50),
	@ObjectIDTemp int,
	@ResourceIDTemp int
as
begin
	set nocount on

	-- Weird StackOverflow about SQL Server using parameter sniffing, which can potentially slow down executing of procs from an application. See GOV-3316 for more details.
	declare
		@Obj varchar(50) = @ObjTemp,
		@ObjectID int = @ObjectIDTemp,
		@ResourceID int = @ResourceIDTemp

	
	declare @Object varchar(50) = @Obj,
			@CurrentDate datetime = getutcdate(),
			@predicateType int = 0,
			@trans varchar(25) = 'Trans',
			@current int = 1,
			@max int,
			@IsType bit = 0

	declare @h table (IntersectID int, ID bigint, ObjectID int, Processed bit null)
	declare @ht table (IntersectTypeID int, ID int, ObjectID int, Processed bit null)

	declare @ClearAttributes bit = 0,
			@ClearComments bit = 0,
			@ClearIntersects bit = 0,
			@ClearFavorites bit = 0,
			@ClearFields bit = 0,
			@ClearFollows bit = 0,
			@ClearIssues bit = 0,
			@ClearNyms bit = 0,
			@ClearResponsibilities bit = 0,
			@ClearSiteNav bit = 0

	if charindex('Type', @Object) > 0
	begin
		set @IsType = 1
	end

	begin try
		begin transaction @trans

		if @Obj = 'Artifact' or @Obj = 'ArtifactType' or @Obj = 'FusionAttribute' or @Obj = 'FusionAttributeType' or @Obj = 'ReferenceItem' or @Obj = 'ReferenceItemType'
		begin
			set @predicateType = 3
		end
		if @Obj = 'Policy' or @Obj = 'PolicyType' or @Obj = 'Taxonomy' or @Obj = 'TaxonomyType'
		begin
			set @predicateType = 4
		end

		if @predicateType > 0
		begin
			if @IsType = 1
				begin
					insert into @ht
						select	null,
								ID,
								ObjectID,
								0
						from	AssetType
						where	[Object] = @Obj and ObjectID = @ObjectID

					while exists(select 1 from @ht where Processed = 0)
					begin
						insert into @ht
							select	I.ID,
									C.ID,
									C.ObjectID,
									null
							from	AssetType C
									inner join IntersectType I on I.Subject = @Obj and I.Object = @Obj and I.ObjectID = C.ObjectID
									inner join [Predicate] PR on PR.ID = I.PredicateID and PR.[Type] = @predicateType
									inner join AssetType P on P.Object = I.Subject and P.ObjectID = I.SubjectID
									inner join @ht T on T.ID = P.ID and T.Processed = 0

						update	@ht set Processed = 1 where Processed = 0
						update	@ht set Processed = 0 where Processed is null
					end

					-- Get all assets based on the types found above.
					insert into @h 
						select null, ID, ObjectID, 1 from Asset where AssetTypeID in (select ID from @ht)
				end
			else
				begin
					insert into @h
						select	null,
								ID,
								ObjectID,
								0
						from	Asset
						where	[Object] = @Obj and ObjectID = @ObjectID

					while exists(select 1 from @h where Processed = 0)
					begin
						insert into @h
							select	I.IntersectID,
									C.ID,
									C.ObjectID,
									null
							from	Asset C
									inner join PredicateIntersect I on I.PredicateType = @predicateType and I.Subject = @Obj and I.Object = @Obj and I.ObjectID = C.ObjectID
									inner join Asset P on P.Object = I.Subject and P.ObjectID = I.SubjectID
									inner join @h T on T.ID = P.ID and T.Processed = 0

						update	@h set Processed = 1 where Processed = 0
						update	@h set Processed = 0 where Processed is null
					end
				end
		end
		
		-- INDEX
		INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
			select	'ObjectIndex', 
					'D',
					O.Object, 
					O.ObjectID
			from	Asset O
					inner join @h I on O.ID = I.ID

		-- AUDIT
		insert into reporting.Global_Audit (Object, ObjectID, ObjectName, ResourceID, Date, Action, ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, ActionDescription)
			select	O.Object, 
					O.ObjectID, 
					O.DisplayValue, 
					@ResourceID, 
					@CurrentDate, 
					'Deleted', 
					O.Object, 
					O.ObjectID, 
					O.TypeName, 
					O.DisplayValue, 
					'This asset has been removed.' 
			from	AssetDetail O
					inner join @h I on O.ID = I.ID
			union
			select	O.Object, 
					O.ObjectID, 
					O.Name, 
					@ResourceID, 
					@CurrentDate, 
					'Deleted', 
					O.Object, 
					O.ObjectID, 
					O.Name, 
					O.Name, 
					'This asset type has been removed.' 
			from	AssetType O
					inner join @ht I on O.ID = I.ID

		-- WORKFLOW

		if @Object = 'Artifact'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1

			delete Artifact where ID in (select ObjectID from @h)
		end

		if @Object = 'ArtifactType'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1
			set @ClearSiteNav = 1
			
			delete	T
			from	ArtifactTypeExportTemplate T
					inner join @ht h on h.ObjectID = T.ID

			delete	Artifact
			where	ID in (select ObjectID from @h)

			delete	ArtifactType			
			where	ID in (select ObjectID from @ht)
		end

		if @Object = 'AttributeType'
		begin
			declare @at table (ID int)
			declare @a table (ID int);

			with ht as	(
						select	ID, 
								ParentID
						from	AttributeType
						where	ID = @ObjectID
						union all
						select	C.ID,
								C.ParentID
						from	AttributeType C
								inner join ht P on P.ID = C.ParentID
						)

			insert into @at 
				select ID from ht

			insert into @a
				select ID from Attribute where AttributeTypeID in (select ID from @at)

			-- AUDIT
			insert into reporting.Global_Audit (Object, ObjectID, ObjectName, ResourceID, Date, Action, ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, ActionDescription)
				select	A.Object, 
						A.ObjectID, 
						A.DisplayValue, 
						@ResourceID, 
						@CurrentDate, 
						'Deleted', 
						'Attribute', 
						O.ID, 
						O.Name, 
						O.FormattedValue, 
						'This attribute has been removed.' 
				from	AttributeDetail O
						inner join AssetDetail A on A.Object = O.ObjectType and A.ObjectID = O.ObjectID
						inner join @a I on O.ID = I.ID
				union
				select	A.Object, 
						A.ObjectID, 
						A.Name, 
						@ResourceID, 
						@CurrentDate, 
						'Deleted', 
						'AttributeType', 
						O.ID, 
						'Attribute Type', 
						O.Name, 
						'This attribute type has been removed.' 
				from	AttributeType O
						inner join @at I on O.ID = I.ID
						inner join AttributeTypeRelation R on R.AttributeTypeID = O.ID
						inner join AssetType A on A.Object = R.ObjectType and A.ObjectID = R.ObjectID

			update	T
			set		T.UpdatedBy = @ResourceID,
					T.UpdatedOn = @CurrentDate
			from	Asset T
					inner join [Attribute] S on S.ObjectType = T.Object and S.ObjectID = T.ObjectID and S.ID in (select ID from @a)

			update	T
			set		T.UpdatedBy = @ResourceID,
					T.UpdatedOn = @CurrentDate
			from	AssetType T
					inner join AttributeTypeRelation S on S.ObjectType = T.Object and S.ObjectID = T.ObjectID 
					inner join AttributeType A on A.ID = S.AttributeTypeID and A.ID in (select ID from @at)

			delete Field					where ObjectType = 'Attribute' and ObjectID in (select ID from @a)
			delete Attribute				where ID in (select ID from @a)
			delete FieldType				where Object = 'AttributeType' and ObjectID in (select ID from @at)
			delete AttributeTypeRelation	where AttributeTypeID in (select ID from @at)
			delete AttributeType			where ID in (select ID from @at)
		end

		if @Object = 'FieldType'
		begin
			-- AUDIT
			insert into reporting.Global_Audit (Object, ObjectID, ObjectName, ResourceID, Date, Action, ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, ActionDescription)
				select	A.Object, 
						A.ObjectID, 
						A.DisplayValue, 
						@ResourceID, 
						@CurrentDate, 
						'Deleted', 
						A.Object, 
						A.ObjectID, 
						T.Name, 
						O.FormattedValue, 
						'This field has been removed.' 
				from	Field O
						inner join FieldType T on T.ID = O.FieldTypeID and T.ID = @ObjectID
						inner join AssetDetail A on A.Object = O.ObjectType and A.ObjectID = O.ObjectID
				union
				select	A.Object, 
						A.ObjectID, 
						A.Name, 
						@ResourceID, 
						@CurrentDate, 
						'Deleted', 
						'FieldType', 
						O.ID, 
						'Field Type', 
						O.Name, 
						'This field type has been removed.' 
				from	FieldType O
						inner join AssetType A on A.Object = O.Object and A.ObjectID = O.ObjectID and O.ID = @ObjectID

			update	T
			set		T.UpdatedBy = @ResourceID,
					T.UpdatedOn = @CurrentDate
			from	Asset T
					inner join Field S on S.ObjectType = T.Object and S.ObjectID = T.ObjectID and S.FieldTypeID = @ObjectID

			update	T
			set		T.UpdatedBy = @ResourceID,
					T.UpdatedOn = @CurrentDate
			from	AssetType T
					inner join FieldType S on S.Object = T.Object and S.ObjectID = T.ObjectID and S.ID = @ObjectID

			delete	Field 
			where	FieldTypeID = @ObjectID
			
			update	FieldType 
			set		ParentFieldTypeID = null 
			where	ParentFieldTypeID = @ObjectID

			delete	FieldType 
			where	ID = @ObjectID
		end

		if @Object = 'FusionAttributeType'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1

			delete FusionAttribute		where ID in (select ObjectID from @h)
			delete FusionAttributeType	where ID in (select ObjectID from @ht)
		end

		if @Object = 'Fusion'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1

			insert into @h
				select	I.ID, null, F.ID, null 
				from	[IntersectDetail] I
						inner join FusionAttribute F on I.Subject = 'FusionAttribute' 
														and I.Object = 'FusionAttribute' 
														and (F.ID = I.SubjectID OR F.ID = I.ObjectID) 
														and F.FusionID = @ObjectID
														and I.PredicateType = 3

			delete FusionAttribute where FusionID = @ObjectID
			delete Fusion where ID = @ObjectID
		end

		if @Object = 'FusionType'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1

			insert into @ht
				select	ID, null, null, null
				from	IntersectType
				where	Subject = 'FusionAttributeType' 
						and Object = 'FusionAttributeType' 
						and (
							SubjectID in (select ID from FusionAttributeType where FusionTypeID = @ObjectID)
							or ObjectID in (select ID from FusionAttributeType where FusionTypeID = @ObjectID)
							)

			insert into @h
				select ID, null, null, null from [Intersect] where IntersectTypeID in (select IntersectTypeID from @ht)

			delete FusionAttribute where FusionAttributeTypeID in (select ID from FusionAttributeType where FusionTypeID = @ObjectID)
			delete Fusion where FusionTypeID = @ObjectID
			delete FusionAttributeType where FusionTypeID = @ObjectID
			delete FusionType where ID = @ObjectID
		end

		if @Object = 'IntersectType'
		begin
			set @ClearAttributes = 1
			set @ClearFields = 1

			delete [Intersect] where IntersectTypeID = @ObjectID
			delete IntersectType where ID = @ObjectID
		end

		if @Object = 'LookupType'
		begin
			set @ClearFields = 1

			delete [Lookup] where LookupTypeID = @ObjectID
			delete  LookupType where ID=@ObjectID
		end

		if @Object = 'Policy'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1

			delete [Policy] where ID in (select ObjectID from @h)
		end

		if @Object = 'PolicyType'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1
			set @ClearSiteNav = 1

			delete [Policy] where PolicyTypeID in (select ObjectID from @ht)
			delete PolicyTypeLevel where PolicyTypeID in (select ObjectID from @ht)
			delete PolicyType where ID in (select ObjectID from @ht)
		end

		if @Object = 'ReferenceItem'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1

			delete ReferenceItem where ID = @ObjectID			
		end

		if @Object = 'ReferenceItemType'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1

			delete ReferenceItem where ReferenceItemTypeID = @ObjectID
			delete ReferenceItemType where ID = @ObjectID
		end

		if @Object = 'ResponsibilityType'
		begin
			delete ResponsibilityTypeRelationOverrideItem where ResponsibilityTypeID = @ObjectID
			delete ResponsibilityTypeRelationItem where ResponsibilityTypeID = @ObjectID
			delete ResponsibilityTypeRelationRule where ResponsibilityTypeID = @ObjectID
			delete ResponsibilityType where ID = @ObjectID
		end

		if @Object = 'Rule'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1

			-- DELETE
			delete T
			from   RuleResultQualifierType T
				   inner join RuleImplementation I on I.ID = T.RuleImplementationID and I.RuleID = @ObjectID

			delete	Q
			from	RuleResultQualifier Q 
					inner join RuleResult S on S.ID = Q.RuleResultID
					inner join RuleImplementation I on I.ID = S.RuleImplementationID and I.RuleID = @ObjectID

			delete	F
			from	RuleResultFusionAttribute F
					inner join RuleResult S on S.ID = F.RuleResultID
					inner join RuleImplementation I on I.ID = S.RuleImplementationID and I.RuleID = @ObjectID

			delete	RuleImplementation where RuleID = @ObjectID

			delete	[Rule] where ID = @ObjectID
		end

		if @Object = 'RuleType'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1

			delete	Q
			from	RuleResultQualifier Q 
					inner join RuleResult S on S.ID = Q.RuleResultID
					inner join RuleImplementation I on I.ID = S.RuleImplementationID
					inner join [Rule] R on R.ID = I.RuleID and R.RuleTypeID = @ObjectID

			delete	F
			from	RuleResultQualifierType F
					inner join RuleImplementation I on I.ID = F.RuleImplementationID
					inner join [Rule] R on R.ID = I.RuleID and R.RuleTypeID = @ObjectID

			delete	F
			from	RuleResultFusionAttribute F
					inner join RuleResult S on S.ID = F.RuleResultID
					inner join RuleImplementation I on I.ID = S.RuleImplementationID
					inner join [Rule] R on R.ID = I.RuleID and R.RuleTypeID = @ObjectID

			delete	S
			from	RuleResult S
					inner join RuleImplementation I on I.ID = S.RuleImplementationID
					inner join [Rule] R on R.ID = I.RuleID and R.RuleTypeID = @ObjectID

			delete [RuleImplementation] where RuleID in (select ID from [Rule] where RuleTypeID = @ObjectID)

			delete [Rule] where RuleTypeID = @ObjectID

			delete RuleType where ID = @ObjectID
		end

		if @Object = 'SurveyType'
		begin
			delete Survey where SurveyTypeID = @ObjectID
			delete SurveyType where ID = @ObjectID
		end

		if @Object = 'Taxonomy'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1

			delete Taxonomy where ID in (select ObjectID from @h)
		end

		if @Object = 'TaxonomyType'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1
			set @ClearSiteNav = 1

			delete Taxonomy where TaxonomyTypeID = @ObjectID
			delete TaxonomyTypeLevel where TaxonomyTypeID = @ObjectID
			delete TaxonomyType where ID = @ObjectID
		end

		-- DELETE (Supporting Tables) ---------------------------------------------------------

		-- Attribute deletion
		IF @ClearAttributes = 1 AND @IsType = 0
		BEGIN
			delete Field where ObjectType = 'Attribute' and ObjectID in (select ID from Attribute where ObjectType = @Object and ObjectID in (select ObjectID from @h))
			delete Attribute where ObjectType = @Object and ObjectID in (select ObjectID from @h)
		END

		-- Intersect deletion
		IF @ClearIntersects = 1
		BEGIN
			DECLARE @tblIntersectIDs table (ID int)

			INSERT INTO @tblIntersectIDs
				SELECT	ID
				FROM	[Intersect]
				WHERE	(Subject = @Object and SubjectID in (select ObjectID from @h)) OR (Object = @Object and ObjectID in (select ObjectID from @h))

			--delete	MapItem 
			--where	SourceIntersectID in (select ID from @tblIntersectIDs) OR
			--		TargetIntersectID in (select ID from @tblIntersectIDs)

			delete [Intersect] where ID in (select ID from @tblIntersectIDs)
		END

		-- Comment deletion
		IF @ClearComments = 1 AND @IsType = 0
		BEGIN
			delete	CommentRelation
			where	ObjectType = @Object 
					and ObjectID in (select ObjectID from @h)

			delete	CommentVote
			where	CommentID in (
								select	ID
								from	Comment
								where	OwnerObjectType = @Object 
										and OwnerObjectID in (select ObjectID from @h)			
								)

			delete	Comment
			where	OwnerObjectType = @Object 
					and OwnerObjectID in (select ObjectID from @h)
		END

		-- Site menu deletion
		IF @ClearSiteNav = 1
		BEGIN
			delete sitenav where objectid = @ObjectID and [object] = @Object
		END

		-- Favorite deletion
		IF @ClearFavorites = 1
		BEGIN
			IF @IsType = 1
				BEGIN
					delete	Favorite
					where	Object = @Object 
							and ObjectID in (select ObjectID from @ht)
				END
			ELSE
				BEGIN 
					delete	Favorite
					where	Object = @Object
							and ObjectID in (select ObjectID from @h)
				END
		END

		-- Field deletion
		IF @ClearFields = 1
		BEGIN
			IF @IsType = 1
				BEGIN
					delete	FieldType
					where	[Object] = @Object 
							and ObjectID in (select ObjectID from @ht)
				END
			ELSE
				BEGIN
					delete	Field
					where	ObjectType = @Object 
							and ObjectID in (select ObjectID from @h)
				END
		END

		-- Follow deletion
		IF @ClearFollows = 1
		BEGIN
			IF @IsType = 1
				BEGIN
					delete	Follow
					where	ObjectType = @Object 
							and ObjectID in (select ObjectID from @ht)
				END
			ELSE
				BEGIN 
					delete	Follow
					where	ObjectType = @Object
							and ObjectID in (select ObjectID from @h)
				END
		END

		-- Issue deletion
		IF @ClearIssues = 1 AND @IsType = 0
		BEGIN
			delete	Issue
			where	Object = @Object 
					and ObjectID in (select ObjectID from @h)
		END

		-- Nym deletion
		IF @ClearNyms = 1 AND @IsType = 0
		BEGIN
			IF @IsType = 1
				BEGIN 
					delete	NymRelation
					where	Object = @Object 
							and ObjectID in (select ObjectID from @ht)			
				END
			ELSE
				BEGIN
					delete	Nym
					where	Object = @Object 
							and ObjectID in (select ObjectID from @h)
				END
		END

		-- Responsibility deletion
		IF @ClearResponsibilities = 1 AND @IsType = 0
		BEGIN
			IF @IsType = 1
				BEGIN
					delete ResponsibilityTypeRelation		where ObjectType = @Obj and ObjectID in (select ObjectID from @ht)
					delete ResponsibilityTypeObjectClaim	where ObjectType = @Obj and ObjectID in (select ObjectID from @ht)
				END
			ELSE
				BEGIN
					delete	T
					from	ResponsibilityTypeRelationOverrideItem T
							inner join Asset A on A.ID = T.AssetID and A.ID in (select ID from @h)

					delete	T
					from	ResponsibilityTypeRelationItem T
							inner join Asset A on A.ID = T.AssetID and A.ID in (select ID from @h)
				END
		END
		---------------------------------------------------------------------------------------

		if @IsType = 1
		begin
			delete AttributeTypeRelation			where ObjectType = @Obj AND ObjectID in (select ObjectID from @ht)
			delete IntersectType					where (Object = @Obj and ObjectID in (select ObjectID from @ht)) OR (Subject = @Obj and SubjectID in (select ObjectID from @ht))
			delete ObjectStyle						where ObjectType = @Obj AND ObjectID in (select ObjectID from @ht)
		end
		
		commit transaction @trans
	end try
	begin catch
		DECLARE @ErrorMessage NVARCHAR(4000)
		DECLARE @ErrorSeverity INT
	    DECLARE @ErrorState INT

		SELECT 
			@ErrorMessage = ERROR_MESSAGE(),
			@ErrorSeverity = ERROR_SEVERITY(),
			@ErrorState = ERROR_STATE()

		-- Use RAISERROR inside the CATCH block to return error
		-- information about the original error that caused
		-- execution to jump to the CATCH block.
		RAISERROR (@ErrorMessage, -- Message text.
				   @ErrorSeverity, -- Severity.
				   @ErrorState -- State.
				   )

		rollback transaction @trans
	end catch
end
GO

ALTER FUNCTION [dbo].[GetAssetDisplayValueById]
(
	@Id bigint
)
RETURNS TABLE 
AS
RETURN 
(
	select		top 1
				string_agg(coalesce(D.FormattedValue, D.value), '') as DisplayValue
	from		dbo.Asset A
				inner join dbo.AssetType T on T.ID = A.AssetTypeID 
				outer apply (
							select	TF.value,
									coalesce(case when TF.Value = 'FirstName' then R.FirstName + ' ' else R.LastName end, F.FormattedValue, RI.Code, FA.Name) as FormattedValue
							from	string_split(replace(replace(T.DisplayFormat, '{', '|'),'}','|'), '|') TF									
									left join dbo.FieldType FT on FT.AssetTypeID = T.ID and FT.Name = TF.Value
									left join dbo.Field F on F.FieldTypeID = FT.ID and F.AssetID = A.ID
									left join dbo.ReferenceItem RI on TF.Value = 'Code' and A.Object = 'ReferenceItem' and RI.ID = A.ObjectID
									left join dbo.FusionAttribute FA on TF.Value = 'Name' and A.Object = 'FusionAttribute' and FA.ID = A.ObjectID
									left join reporting.Global_resource R on TF.Value in ('FirstName', 'LastName') and A.Object = 'Resource' and R.ResourceID = A.ObjectID
							where	RTRIM(TF.value) <> ''									
							) D
	where A.ID = @Id
)
GO

ALTER TRIGGER [dbo].[FieldType_AfterUpsert]
   ON  [dbo].[FieldType] 
   AFTER INSERT, UPDATE
AS 
		UPDATE	F
		set		F.FormattedValue = utility.GetFormattedFieldLookupValueWithMultiple(FT.Type, FT.LookupDisplayFormat, FT.LookupObjectType, FT.LookupObjectID, F.Value, FT.AllowMultipleValues)
		FROM	Field F
				inner join inserted FT on FT.ID = F.FieldTypeID and FT.LookupObjectType is not null

		update	FT	
		set		FT.defaultformattedvalue  = [utility].[GetFormattedFieldLookupValueWrapper](FT.[Type],FT.[LookupDisplayFormat],FT.[LookupObjectType],FT.[LookupObjectID],FT.[DefaultValue])
		from	FieldType FT
				inner join inserted ins on ins.ID = FT.ID and ins.LookupObjectType is not null

		--check insert vs update
		IF EXISTS (SELECT * FROM DELETED)
		begin
			INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
				select 'Update', [queue].WriteIndexXml('', Object, ObjectID, UpdatedBy), 'FieldType', ID from inserted;
		end
		ELSE
		BEGIN
			INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
				select 'Add', [queue].WriteIndexXml('', Object, ObjectID, UpdatedBy), 'FieldType', ID from inserted;
		END
GO

CREATE NONCLUSTERED INDEX [IX_Asset_AssetType_Include] ON [dbo].[Asset] ([AssetTypeID]) INCLUDE ([ObjectID], [SourceID]) WITH (ONLINE = ON)
GO

CREATE NONCLUSTERED INDEX [IX_Intersect_Object_Type_Subject_Include] ON [dbo].[Intersect] ([Object], [ObjectID], [IntersectTypeID], [SubjectID]) INCLUDE ([ID]) WITH (ONLINE = ON)
GO

CREATE NONCLUSTERED INDEX [IX_Artifact_ArtifactTypeID_SourceID] ON [dbo].[Artifact] ( ArtifactTypeID ASC, SourceID ASC )
GO

CREATE NONCLUSTERED INDEX [IX_ReferenceItem_ReferenceItemTypeID_SourceID] ON [dbo].[ReferenceItem] ( ReferenceItemTypeID ASC, SourceID ASC )
GO

CREATE NONCLUSTERED INDEX [IX_Policy_PolicyTypeID_SourceID] ON [dbo].[Policy] ( PolicyTypeID ASC, SourceID ASC )
GO

CREATE NONCLUSTERED INDEX [IX_Rule_RuleTypeID_SourceID] ON [dbo].[Rule] ( RuleTypeID ASC, SourceID ASC )
GO

CREATE NONCLUSTERED INDEX [IX_Taxonomy_TaxonomyTypeID_SourceID] ON [dbo].[Taxonomy] ( TaxonomyTypeID ASC, SourceID ASC )
GO

CREATE NONCLUSTERED INDEX [IX_Asset_SourceID_Object_ObjectID] ON [dbo].[Asset] ( SourceID ASC, Object ASC, ObjectID ASC )
GO

CREATE SCHEMA responsibility AUTHORIZATION dbo
GO

CREATE INDEX IX_ResponsibilityTypeRelationTypeItem_RuleID ON [dbo].[ResponsibilityTypeRelationTypeItem] ( RuleID ASC )
GO
--DROP INDEX [IX_ResponsibilityTypeRelationTypeItem_SecurityAssetID] ON [dbo].[ResponsibilityTypeRelationTypeItem]
--GO
CREATE INDEX IX_ResponsibilityTypeRelationTypeItem_SecurityAsset ON [dbo].[ResponsibilityTypeRelationTypeItem] ( SecurityAsset ASC, SecurityAssetID ASC ) --INCLUDE ( SecurityAsset )
GO

alter view [dbo].[AssetWithType]
as
	select	A.ID,
			A.AssetTypeID,
			A.State,
			A.Object,
			A.ObjectID,
			A.SourceID,
			A.CreatedOn,
			A.CreatedBy,
			A.UpdatedOn,
			A.UpdatedBy,
			T.Class as AssetTypeClass,
			T.Description as AssetTypeDescription,
			T.Name as TypeName,
			T.Object as Type,
			T.ObjectID as TypeID,
			coalesce(S.IconBackColor, '#000') as BackColor,
			coalesce(S.IconForeColor, '#fff') as ForeColor,
			coalesce(S.IconText, 'leaf') as Icon
	from	Asset A
			inner join AssetType T on T.ID = A.AssetTypeID
			left join ObjectStyle S on S.ObjectType = T.Object and S.ObjectID = T.ObjectID
GO

create view responsibility.Core
as
select	R.ID as RuleID,
		R.ResponsibilityTypeID,
		A.ID as AssetID,
		A.Object,
		A.ObjectID,
		A.AssetTypeID,
		T.Object as Type,
		T.ObjectID as TypeID,
		coalesce(ATT.SecurityAsset, X.SecurityAsset, ATI.SecurityAsset) as SecurityAsset,
		coalesce(ATT.SecurityAssetID, X.SecurityAssetID, ATI.SecurityAssetID) as SecurityAssetID,
		coalesce(X.Context, R.Context) as Context,
		R.ApplyToType,
		case 
			when X.ID is not null then cast(1 as bit)
			else R.IsVisible
		end as IsVisible,
		case 
			when R.ApplyToType = 0 and X.SecurityAssetID is not null then cast(1 as bit) 
			else cast(0 as bit)
		end as  Overriden
from	Asset A
		inner join AssetType T on T.ID = A.AssetTypeID
		inner join ResponsibilityTypeRelationRule R on R.Object = T.Object and R.ObjectID = T.ObjectID
		left join ResponsibilityTypeRelationTypeItem ATT on ATT.RuleID = R.ID and R.ApplyToType = 1
		left join ResponsibilityTypeRelationItem ATI on ATI.RuleID = R.ID and ATI.AssetID = A.ID and R.ApplyToType = 0
		left join dbo.ResponsibilityTypeRelationOverrideItem X on X.ResponsibilityTypeID = ATI.ResponsibilityTypeID and x.AssetID = ATI.AssetID
where	ATT.RuleID is not null or ATI.RuleID is not null
go

create view responsibility.ClaimCore
as
select	O.AssetID,
		--coalesce(ReGr.ResourceID, OrRe.ResourceID, O.SecurityAssetID) as ResourceID,
		case O.SecurityAsset
			when 'G' then ReGr.ResourceID
			when 'O' then OrRe.ResourceID
			when 'R' then O.SecurityAssetID
			else null
		end as ResourceID,
		RTC.Claim,
		RTC.ClaimObject
from	responsibility.Core O
		inner join ResponsibilityTypeObjectClaim RTC	on RTC.ResponsibilityTypeID = O.ResponsibilityTypeID 
																and RTC.ObjectType = O.Type 
																and RTC.ObjectID = O.TypeID
		left join dbo.OrganizationResource OrRe on O.SecurityAsset = 'O' and OrRe.OrganizationID = O.SecurityAssetID
		left join dbo.ResourceGroup ReGr on O.SecurityAsset = 'G' and ReGr.GroupID = O.SecurityAssetID
group by O.AssetID,
		--coalesce(ReGr.ResourceID, OrRe.ResourceID, O.SecurityAssetID),
		case O.SecurityAsset
			when 'G' then ReGr.ResourceID
			when 'O' then OrRe.ResourceID
			when 'R' then O.SecurityAssetID
			else null
		end,
		RTC.Claim,
		RTC.ClaimObject
go

ALTER VIEW [dbo].[ResponsibilityDetails]
AS 
select	O.AssetID,
		O.Object,
		O.ObjectID,
		O.Type,
		O.TypeID,
		O.Context,
		O.ResponsibilityTypeID,
		RT.Name as ResponsibilityTypeName,
		GrRe.FirstName,
		GrRe.LastName,
		case O.SecurityAsset
			when 'G' then ReGr.ResourceID
			when 'O' then OrRe.ResourceID
			when 'R' then O.SecurityAssetID
			else null
		end as ResourceID,
		O.SecurityAsset,
		O.SecurityAssetID,
		case O.SecurityAsset
			when 'G' then Gr.Name
			when 'O' then Org.Name
			when 'R' then GrRe.LastName + ', ' + GrRe.FirstName
			else null
		end as SecurityAssetName,
		O.IsVisible,
		O.ApplyToType
from	responsibility.Core O
		inner join dbo.ResponsibilityType RT on RT.ID = O.ResponsibilityTypeID
		left join dbo.OrganizationResource OrRe on O.SecurityAsset = 'O' and OrRe.OrganizationID = O.SecurityAssetID
		left join dbo.Organization Org on O.SecurityAsset = 'O' and Org.ID = OrRe.OrganizationID
		left join dbo.ResourceGroup ReGr on O.SecurityAsset = 'G' and ReGr.GroupID = O.SecurityAssetID
		left join dbo.[Group] Gr on O.SecurityAsset = 'G' and Gr.ID = ReGr.GroupID
		inner join reporting.Global_Resource GrRe on GrRe.ResourceID =	case O.SecurityAsset
																			when 'G' then ReGr.ResourceID
																			when 'O' then OrRe.ResourceID
																			when 'R' then O.SecurityAssetID
																			else null
																		end
GO

create table cache.NoRead (
	AssetID bigint not null,
	Object varchar(50) not null,
	ObjectID int not null,
	ResourceID int not null,
	CONSTRAINT [PK_CacheNoRead] PRIMARY KEY CLUSTERED ( [AssetID] ASC, [ResourceID] ASC )
)
GO
CREATE INDEX IX_CacheNoRead_Resource_Include ON [cache].NoRead ( ResourceID ASC ) INCLUDE ( AssetID, Object, ObjectID )
GO
CREATE INDEX IX_CacheNoRead_Object_Include ON [cache].NoRead ( Object ASC, ObjectID ASC ) INCLUDE ( AssetID, ResourceID )
GO

CREATE TABLE [cache].[AssetDelete] (
    [AssetID]    BIGINT NOT NULL,
    [ResourceID] INT    NOT NULL,
    CONSTRAINT [PK_CacheAssetDelete] PRIMARY KEY CLUSTERED ([AssetID] ASC, [ResourceID] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_CacheAssetDelete_Resource_Include] ON [cache].[AssetDelete]([ResourceID] ASC) INCLUDE([AssetID])
GO

CREATE TABLE [cache].[AssetEdit] (
    [AssetID]    BIGINT NOT NULL,
    [ResourceID] INT    NOT NULL,
    CONSTRAINT [PK_CacheAssetEdit] PRIMARY KEY CLUSTERED ([AssetID] ASC, [ResourceID] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_CacheAssetEdit_Resource_Include] ON [cache].[AssetEdit]([ResourceID] ASC) INCLUDE([AssetID]);
GO

drop view responsibility.RestrictedCore
GO

CREATE FULLTEXT CATALOG FieldCatalog AS DEFAULT 
GO

CREATE TABLE [integration].[Execution] (
	ID bigint IDENTITY(1,1) NOT NULL,
	StartedOn datetime NOT NULL,
	CompletedOn datetime null,
	CONSTRAINT [PK_IntegrationExecution] PRIMARY KEY NONCLUSTERED ( [ID] DESC )
)
GO

CREATE TABLE [integration].[ExecutionAssetType] (
	ExecutionID bigint NOT NULL,
	SynchedAssetTypeID int NOT NULL,
	CurrentSourceAssetCount int NOT NULL,
	CurrentTargetAssetCount int NOT NULL,
	StartedOn datetime NOT NULL,
	CompletedOn datetime NULL,
	CONSTRAINT [PK_IntegrationExecutionAssetType] PRIMARY KEY NONCLUSTERED ( ExecutionID DESC, SynchedAssetTypeID ASC )
)
GO

ALTER TABLE [integration].[ExecutionAssetType]  WITH CHECK ADD  CONSTRAINT [FK_IntegrationExecutionAssetType_IntegrationExecution] FOREIGN KEY([ExecutionID]) REFERENCES [integration].[Execution] ([ID])
ALTER TABLE [integration].[ExecutionAssetType] CHECK CONSTRAINT [FK_IntegrationExecutionAssetType_IntegrationExecution]
GO

ALTER TABLE [integration].[ExecutionAssetType]  WITH CHECK ADD  CONSTRAINT [FK_IntegrationExecutionAssetType_IntegrationSynchedAssetType] FOREIGN KEY([SynchedAssetTypeID]) REFERENCES [integration].[SynchedAssetType] ([ID])
ALTER TABLE [integration].[ExecutionAssetType] CHECK CONSTRAINT [FK_IntegrationExecutionAssetType_IntegrationSynchedAssetType]
GO

CREATE TABLE [integration].[ExecutionAsset] (
	ExecutionID bigint NOT NULL,
	SynchedAssetTypeID int NOT NULL,
	SourceID varchar(100) NOT NULL,
	RawObject nvarchar(max) NULL,
	ErrorMessages nvarchar(max) NULL,
	CONSTRAINT [PK_IntegrationExecutionAsset] PRIMARY KEY NONCLUSTERED ( ExecutionID DESC, SynchedAssetTypeID ASC, SourceID ASC )
)
GO

ALTER TABLE [integration].[ExecutionAsset]  WITH CHECK ADD  CONSTRAINT [FK_IntegrationExecutionAsset_IntegrationExecution] FOREIGN KEY([ExecutionID]) REFERENCES [integration].[Execution] ([ID])
ALTER TABLE [integration].[ExecutionAsset] CHECK CONSTRAINT [FK_IntegrationExecutionAsset_IntegrationExecution]
GO

ALTER TABLE [integration].[ExecutionAsset]  WITH CHECK ADD  CONSTRAINT [FK_IntegrationExecutionAsset_IntegrationSynchedAssetType] FOREIGN KEY([SynchedAssetTypeID]) REFERENCES [integration].[SynchedAssetType] ([ID])
ALTER TABLE [integration].[ExecutionAsset] CHECK CONSTRAINT [FK_IntegrationExecutionAsset_IntegrationSynchedAssetType]
GO

CREATE CLUSTERED INDEX CIX_IntegrationExecutionAsset ON [integration].[ExecutionAsset] ( ExecutionID ASC, SynchedAssetTypeID, SourceID ASC )
GO

ALTER VIEW [dbo].[FieldDetail]
AS
	SELECT	T.ID as FieldTypeID,
			T.Name,
			T.FriendlyName,
			A.AssetTypeID,
			A.ID as AssetID,
			A.Object,
			A.ObjectID,
			T.Type,
			coalesce(F.Value, T.DefaultValue) as Value,
			case
				when T.AllowAllValue = 1 and F.FormattedValue = '0' then cast(T.AllowAllLabel as nvarchar(max))
				when F.FormattedValue is not null then F.FormattedValue
				when T.DefaultFormattedValue is not null then cast(T.DefaultFormattedValue as nvarchar(max))
				else null
			end as FormattedValue
	FROM	Asset A
			inner join FieldType T on T.AssetTypeID = A.AssetTypeID
			left join Field F on F.FieldTypeID = T.ID and F.AssetID = A.ID
	WHERE	(F.Value is not null OR T.DefaultValue is not null)
GO

ALTER procedure [bulkload].[Promotions]
--declare
	@id int
--set @id = 84
as
begin
	set nocount on;

	declare @levels table (rowIndex int, [level] int, processed bit);

	declare @Object varchar(50),
			@ObjectID int,
			@Action varchar(1),
			@UpdatedOn datetime = getutcdate(),
			@UpdatedBy int = 0
				
	select	@Object = [Object], 
			@ObjectID = ObjectID,
			@Action = [Action],
			@UpdatedBy = UpdatedBy
	from	[Load]
	where	ID = @id;

	update	LoadItem
	set		Object = null, 
			ObjectID = null, 
			Status = null,
			StatusMessage = null
	where	LoadID = @id;

	-- Process hashes for Load Items
	if @Object = 'ReferenceItemType'
	begin
		update	T
		set		T.KeyHash = CONVERT(
									varchar(32), 
									SUBSTRING(HASHBYTES('SHA1', substring(ltrim(rtrim(IC.Value)), 1, 250)), 3, 32), 
									2),
				T.FieldHash = V.FieldHash
		from	LoadItem T
				inner join LoadColumn C on C.LoadID = T.LoadID and C.Name = 'Code'
				inner join LoadItemColumn IC on IC.LoadID = C.LoadID and IC.RowIndex = T.RowIndex and IC.ColumnIndex = C.ColumnIndex
				inner join	(
							select		RowIndex,
										CONVERT(
											varchar(32), 
											SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
											2) as FieldHash
							from		(
										select		top 100 percent
													I.RowIndex,
													FT.ID as FieldTypeID,
													coalesce(IC.Value, '') as Value
										from		LoadItem I
													inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id
													inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex = IC.ColumnIndex
													inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name
										order by	I.RowIndex,
													FT.ID
										) A
							group by	A.RowIndex	
							) V on V.RowIndex = T.RowIndex
		where	T.LoadID = @id;
	end
	else if @Object = 'TaxonomyType'
	begin
		declare @currRow int, @maxRow int, @currLevel int;
		set @currRow = 1;
		set @currLevel = 0;
		set @maxRow = (select max(RowIndex) from LoadItem where LoadID = @id);	

		while @currRow < @maxRow
		begin
			set @currRow = @currRow + 1;

			--get level for current row
			select		@currLevel = coalesce(max(L.[Level]), 1) 
			from		TaxonomyTypeLevel L
						inner join LoadColumn LC on LC.LoadID = @id and L.Name = substring(LC.[Name], 1, len(LC.[Name]) - charindex(' ', reverse(LC.[Name])))
						inner join LoadItemColumn LI on LI.LoadID = @id and LI.RowIndex = @currRow and LI.ColumnIndex = LC.ColumnIndex and LI.[Value] is not null
			where		L.TaxonomyTypeID = @ObjectID

			insert into @levels (rowIndex, level, processed) values (@currRow, @currLevel, 0);

			--update the key hash based on the current level
			update	T
			set		T.KeyHash = K.KeyHash,
					T.FieldHash = V.FieldHash
			from	LoadItem T
					left join	(
								select		RowIndex,
											CONVERT(
												varchar(32), 
												SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
												2) as KeyHash
								from		(
												select top 100 percent
													IC.RowIndex, 
													FT.ID as FieldTypeID, 
													coalesce(IC.[Value],'') as [Value] 
												from LoadColumn LC
												inner join LoadItemColumn IC on IC.LoadID = @id and IC.RowIndex = @currRow and IC.ColumnIndex = LC.ColumnIndex
												inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.IsPartOfKey = 1 and FT.Name = reverse(substring(reverse(LC.[Name]), 0, charindex(' ',reverse(LC.[Name]))))			
												where LC.LoadID = @id and LC.ColumnIndex in (
			 										select		LC.ColumnIndex 
													from		TaxonomyTypeLevel L
																inner join LoadColumn LC on LC.LoadID = @id and L.Name = substring(LC.[Name], 1, len(LC.[Name]) - charindex(' ', reverse(LC.[Name])))
																inner join LoadItemColumn LI on LI.LoadID = @id and LI.RowIndex = @currRow and LI.ColumnIndex = LC.ColumnIndex and LI.[Value] is not null
													where		L.TaxonomyTypeID = @ObjectID and L.[Level] = @currLevel
													)
											) A
								group by	A.RowIndex
								) K on K.RowIndex = T.RowIndex
					inner join	(
								select		RowIndex,
											CONVERT(
												varchar(32), 
												SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
												2) as FieldHash
								from		(
											select		top 100 percent
														I.RowIndex,
														FT.ID as FieldTypeID,
														coalesce(IC.Value, '') as Value
											from		LoadItem I
														inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id
														inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex = IC.ColumnIndex
														inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name
											order by	I.RowIndex,
														FT.ID
											) A
								group by	A.RowIndex	
								) V on V.RowIndex = T.RowIndex
			where	T.LoadID = @id and T.RowIndex = @currRow;
		end
	end
	else
	begin
		update	T
		set		T.KeyHash = K.KeyHash,
				T.FieldHash = V.FieldHash
		from	LoadItem T
				inner join	(
							select		RowIndex,
										CONVERT(
											varchar(32), 
											SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
											2) as KeyHash
							from		(
										select		top 1000000000 --percent
													I.RowIndex,
													FT.ID as FieldTypeID,
													coalesce(IC.Value, '') as Value
										from		LoadItem I
													inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id and IC.Value is not null
													inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex = IC.ColumnIndex
													inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.IsPartOfKey = 1 and FT.Name = C.Name
										order by	I.RowIndex,
													FT.ID
										) A
							group by	A.RowIndex
							) K on K.RowIndex = T.RowIndex
				inner join	(
							select		RowIndex,
										CONVERT(
											varchar(32), 
											SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
											2) as FieldHash
							from		(
										select		top 100 percent
													I.RowIndex,
													FT.ID as FieldTypeID,
													coalesce(IC.Value, '') as Value
										from		LoadItem I
													inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id
													inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex = IC.ColumnIndex
													inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name
										order by	I.RowIndex,
													FT.ID
										) A
							group by	A.RowIndex	
							) V on V.RowIndex = T.RowIndex
		where	T.LoadID = @id;
	end
	-- -----------------------------
	
	-- Resolve Single-value LOOKUP fields
	exec [bulkload].[UpdateDynamicLookupFieldColumns] @id

	-- Resolve Multi-value LOOKUP fields
	update	IC
	set		IC.LookupObject = MV.LookupObject,
			IC.LookupValue = MV.LookupValue
	from	LoadItemColumn IC
			inner join	(
						select		IC.LoadID,
									IC.RowIndex,
									IC.ColumnIndex,
									'ReferenceItem' as LookupObject,
									string_agg(AD.ID, ',') as LookupValue
						from		LoadItem LI
									inner join LoadItemColumn IC on LI.LoadID = @id and LI.LoadID = IC.LoadID and IC.RowIndex = LI.RowIndex
									inner join LoadColumn C on C.LoadID = IC.LoadID and C.ColumnIndex = IC.ColumnIndex
									inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and FT.AllowMultipleValues = 1
									cross apply string_split(IC.Value, ',') VS									
									left join ReferenceItem AD on AD.ReferenceITemTypeID = FT.LookupObjectID
									CROSS APPLY [dbo].[GetReferenceItemDisplayValue](AD.ID, FT.ID) GRIDV
						where GRIDV.DisplayValue = ltrim(rtrim(VS.Value))
						group by	IC.LoadID,
									IC.RowIndex,
									IC.ColumnIndex			
						) MV on MV.LoadID = IC.LoadID and MV.RowIndex = IC.RowIndex and MV.ColumnIndex = IC.ColumnIndex

	-- Log error messages for reference list resolution.
	update	LI
	set		LI.StatusMessage = coalesce(LI.StatusMessage,'') + FT.Name + ' could not be resolved to an existing reference item.' 
	from	LoadItem LI
			inner join LoadItemColumn IC on LI.LoadID = @id and IC.LoadID = LI.LoadID and IC.RowIndex = LI.RowIndex
			inner join LoadColumn C on C.ColumnIndex = IC.ColumnIndex and C.LoadID = IC.LoadID
			inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
										and FT.Name = C.Name 
										and FT.Type = 'Lookup' 
										and FT.LookupObjectType = 'ReferenceItem' 
										and (FT.IsRequired = 1 or FT.IsPartOfKey = 1) 
										and ( 
												(FT.AllowMultipleValues = 0 AND IC.LookupObjectID is null) OR 
												(FT.AllowMultipleValues = 1 AND IC.LookupValue is null)
											);

	-- Resolve Allow All LOOKUP field values
	update	IC
	set		IC.LookupObject = REPLACE(FT.LookupObjectType, 'Type', ''),
			IC.LookupObjectID = 0,
			IC.LookupValue = 0
	from	LoadItemColumn IC
			inner join LoadColumn C on C.ColumnIndex = IC.ColumnIndex and C.LoadID = IC.LoadID and C.LoadID = @id
			inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and FT.Type = 'Lookup' and FT.AllowAllValue = 1 and IC.Value = FT.AllowAllLabel;

	-- Resolve RELATIONSHIP fields
	declare @relFieldLookups table (LoadID int, RowIndex int, ColumnIndex int, Object varchar(50), ObjectID int )

	insert into @relFieldLookups
		select	IC.LoadID,
				Ic.RowIndex,
				IC.ColumnIndex,
				D.Object,
				D.ObjectID
		from	LoadItemColumn IC
				inner join LoadColumn C on C.ColumnIndex = IC.ColumnIndex and C.LoadID = IC.LoadID and C.LoadID = @id
				inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and FT.Type = 'Relationship'
				inner join IntersectType IT on FT.LookupObjectType = 'IntersectType' and FT.LookupObjectID = IT.ID
				inner join AssetType DT on DT.Object = case when IT.Subject = @Object and IT.SubjectID = @ObjectID then IT.Object else IT.Subject end
											and DT.ObjectID = case when IT.Subject = @Object and IT.SubjectID = @ObjectID then IT.ObjectID else IT.SubjectID end
				inner join dbo.GetAssetDisplayValue() D on D.AssetTypeID = DT.ID and D.DisplayValue = ltrim(rtrim(IC.Value));

	update	T
	set		T.LookupObject = S.Object,
			T.LookupObjectID = S.ObjectID
	from	LoadItemColumn T
			inner join @relFieldLookups S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex;


	-- Capture changes for logging purposes.
	--declare @tbl table (ObjectID int, RowIndex int, [Action] varchar(1), [FieldsLoaded] bit null, [RelationshipsLoaded] bit null);

	IF OBJECT_ID('tempdb..#tbl') IS NOT NULL
			DROP TABLE #tbl;

	create table #tbl (ObjectID int, RowIndex int, [Action] varchar(1), [FieldsLoaded] bit null, [RelationshipsLoaded] bit null);
	
	CREATE CLUSTERED INDEX PK_tempTbl ON #tbl ([RowIndex] ASC,[Action] ASC);

	--declare @insertToPerform table (RowID int identity, KeyHash varchar(250));
	IF OBJECT_ID('tempdb..#insertToPerform') IS NOT NULL
			DROP TABLE #insertToPerform;

	create table #insertToPerform (RowID int identity, KeyHash varchar(250));
	
	CREATE CLUSTERED INDEX PK_tempinsertToPerform ON #insertToPerform ([KeyHash] ASC);

	--declare @insertOutputID table (RowID int identity, ObjectID int);
	IF OBJECT_ID('tempdb..#insertOutputID') IS NOT NULL
			DROP TABLE #insertOutputID;

	create table #insertOutputID (RowID int identity, ObjectID int);
	
	-- COMMON ------------------
	-- Identify which load items already exist based on key hash.
	-- oddly wonky
	/*update	T
	set		T.Object = A.Object,
			T.ObjectID = A.ObjectID
	from	LoadItem T
			inner join AssetType ST on ST.Object = @Object and ST.ObjectID = @ObjectID
			inner join GetAssetKeyHash() S on S.AssetTypeID = ST.ID and S.KeyHash = T.KeyHash and T.LoadID = @id
			inner join Asset A on A.ID = S.ID;*/

	/*update	T
	set		T.Object = A.Object,
			T.ObjectID = A.ObjectID
	from	LoadItem T
			inner join AssetType ST on ST.Object = @Object and ST.ObjectID = @ObjectID
			cross apply GetAssetKeyHashById(ST.ID) S 
			inner join Asset A on A.ID = S.ID
	where S.KeyHash = T.KeyHash and T.LoadID = @id*/

	update	T
	set		T.Object = A.Object,
			T.ObjectID = A.ObjectID
	from	LoadItem T
			inner join AssetType ST on ST.Object = @Object and ST.ObjectID = @ObjectID			
			inner join Asset A on A.AssetTypeID = ST.ID
			cross apply GetAssetKeyHashById(A.ID) S 
	where S.KeyHash = T.KeyHash and T.LoadID = @id
	
	-- ARTIFACTS ---------------
	if @Object = 'ArtifactType'
	begin
		-- Mark the existing artifacts as being updated.
		update	T
		set		T.UpdatedBy = @UpdatedBy,
				T.UpdatedOn = @UpdatedOn
		from	Artifact T
				inner join LoadItem S on S.LoadID = @id and S.Object = 'Artifact' and S.ObjectID = T.ID and T.ArtifactTypeID = @ObjectID;

		-- Insert the updated records into temp table for logging.
		insert into #tbl 
			select	ObjectID,
					RowIndex,
					'U', null, null
			from	LoadItem
			where	LoadID = @id 
					and ObjectID is not null;

		-- Insert new items into the Artifact table.
		insert into #insertToPerform
			select	distinct
					KeyHash
			from	LoadItem
			where	LoadID = @id
					and ObjectID is null
					and KeyHash is not null;

		--declare @insertOutputID table (RowID int identity, ObjectID int);
		insert Artifact (ArtifactTypeID, UpdatedOn, UpdatedBy, CreatedOn, CreatedBy)
		output inserted.ID into #insertOutputID
			select	@ObjectID, 
					@UpdatedOn, 
					@UpdatedBy, 
					@UpdatedOn, 
					@UpdatedBy
			from	#insertToPerform;

		-- Insert the added records into temp table for logging.
		insert into #tbl 
			select	N.ObjectID,
					I.RowIndex,
					'A', null, null
			from	LoadItem I
					inner join #insertToPerform P on P.KeyHash = I.KeyHash and I.LoadID = @id 
					inner join #insertOutputID N on N.RowID = P.RowID;

		-- Update the LoadItem table with the Object and ObjectID generated from the insert above.
		update	T
		set		T.Object = 'Artifact',
				T.ObjectID = S.ObjectID
		from	LoadItem T
				inner join #tbl S on T.LoadID = @id and S.RowIndex = T.RowIndex and S.[Action] = 'A';
	end
	-------------------------

	-- MODEL ----------------
   if @Object = 'TaxonomyType'
   begin
		declare 
			@row int, 
			@level int, 
			@rows int, 
			@rowObject varchar(50), 
			@rowObjectId int, 
			@parentKeyHash varchar(50),
			@intersectTypeid int,
			@parentObjectId int;

		declare @ids table (id int);

		set @row = 0;
		set @level = 0;

		while (select count(*) from @levels where processed = 0) > 0
		begin
			set @parentKeyHash = null;
			set @parentObjectId = null;
			delete from @ids;

			--need to process rows in order of level (low to high) to make sure parent items are added or exist
			select		top 1
						@row = L.RowIndex, 
						@level = L.[Level], 
						@rowObject = LC.[Object], 
						@rowObjectId = LC.ObjectID 
			from		@levels L
						inner join LoadItem LC on LC.RowIndex = L.RowIndex and LC.LoadID = @id
			where		L.processed = 0
			order by	L.[Level] asc;
			
			if @rowObjectId is not null
			begin
				update	Taxonomy
				set		UpdatedOn = @UpdatedOn,
						UpdatedBy = @UpdatedBy
				where	ID = @rowObjectId;
			end
			else
			begin
				if @level > 1
				begin
					--hash key fields at (level - 1) and check against asset or LoadItem
					select @parentKeyHash = CONVERT(
									varchar(32), 
									SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
									2)
					from		(
									select		top 100 percent
												FT.ID as FieldTypeID, 
												coalesce(IC.[Value],'') as [Value] 
									from		LoadColumn LC
												inner join LoadItemColumn IC on IC.LoadID = @id and IC.RowIndex = @row and IC.ColumnIndex = LC.ColumnIndex
												inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.IsPartOfKey = 1 
													and FT.Name = reverse(substring(reverse(LC.[Name]), 0, charindex(' ',reverse(LC.[Name]))))			
									where		LC.LoadID = @id and LC.ColumnIndex in (
			 										select	LC.ColumnIndex 
													from	TaxonomyTypeLevel L
															inner join LoadColumn LC on LC.LoadID = @id and L.Name = substring(LC.[Name], 1, len(LC.[Name]) - charindex(' ', reverse(LC.[Name])))
															inner join LoadItemColumn LI on LI.LoadID = @id and LI.RowIndex = @row and LI.ColumnIndex = LC.ColumnIndex and LI.[Value] is not null
													where	L.TaxonomyTypeID = @ObjectID and L.[Level] = (@level-1)
													)
								) A;

					select @parentObjectId = coalesce(
							(
							select		top 1 
										a.ObjectID 
							from		Asset A
										inner join AssetType T on T.Object = @Object and T.ObjectID = @ObjectID and A.AssetTypeID = T.ID
										inner join GetAssetKeyHash() H on H.ID = A.ID
							where		H.KeyHash = @parentKeyHash
							),
							(
							select		top 1 
										a.ObjectID 
							from		LoadItem L
										inner join Asset A on A.[Object] = L.[Object] and A.ObjectID = L.ObjectID
							where		LoadID = @id and L.KeyHash = @parentKeyHash
							)
						);
					
					if @parentObjectId is not null
					begin
						insert Taxonomy (TaxonomyTypeID, UpdatedOn, UpdatedBy)
						output inserted.ID into @ids
							select	@ObjectID, 
									@UpdatedOn, 
									@UpdatedBy;

						insert into #tbl
						select	id,
								@row,
								'A', null, null
						from	@ids
					
						select  @intersectTypeId = id 
						from	intersecttypedetail 
						where	[subject] = @Object and subjectid = @ObjectID 
								and [object] = @Object and objectid = @objectID
								and predicatetype = 4;
						
						if @intersectTypeId is not null 
							and not exists (
								select		1 
								from		[Intersect] 
								where		IntersectTypeID = @intersectTypeId 
											and ObjectID = (select id from @ids) 
											and SubjectID = @parentObjectId)
						begin						
							insert into [Intersect] (IntersectTypeId, [Subject], [Object], SubjectID, ObjectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
							select	@intersectTypeId as IntersectTypeId,
									'Taxonomy' as [Subject],
									'Taxonomy' as [Object],
									@parentObjectId as SubjectID,
									(select id from @ids) as ObjectID,
									@UpdatedBy as CreatedBy,
									@UpdatedOn as CreatedOn,
									@UpdatedBy as UpdatedBy,
									@UpdatedOn as UpdatedOn,
									'BulkLoad' as [Owner];
						end
					end
				end
				else --root item
				begin			
					insert Taxonomy (TaxonomyTypeID, UpdatedOn, UpdatedBy)
					output inserted.ID into @ids
						select	@ObjectID, 
								@UpdatedOn, 
								@UpdatedBy;

					insert into #tbl
					select	id,
							@row,
							'A', null, null
					from	@ids;									
				end
			end

			update	@levels 
			set		processed = 1 
			where	rowIndex = @row 
					and [level] = @level;

			update	T
			set		T.Object = 'Taxonomy',
					T.ObjectID = S.ObjectID
			from	LoadItem T
					inner join #tbl S on T.LoadID = @id and S.RowIndex = T.RowIndex and S.[Action] = 'A';
		end
	
	end
	--------------------------

	-- REFERENCE ------------
	if @Object = 'ReferenceItemType'
	begin
		declare @ri_insertToPerform table (RowID int identity, Code nvarchar(250), KeyHash varchar(250));
		declare @ri_insertOutputID table (RowID int identity, ObjectID int);

		-- Mark the existing items as being updated.
		update	T
		set		T.UpdatedBy = @UpdatedBy,
				T.UpdatedOn = @UpdatedOn
		from	ReferenceItem T
				inner join LoadItem S on S.LoadID = @id and S.Object = 'ReferenceItem' and S.ObjectID = T.ID and T.ReferenceItemTypeID = @ObjectID;

		-- Insert the updated records into temp table for logging.
		insert into #tbl 
			select	ObjectID,
					RowIndex,
					'U', null, null
			from	LoadItem
			where	LoadID = @id 
					and ObjectID is not null;

		-- Insert new items into the ReferenceItem table.
		insert into @ri_insertToPerform
			select	distinct
					substring(ltrim(rtrim(IC.Value)), 1, 250),
					I.KeyHash
			from	LoadItem I
					inner join LoadColumn C on C.LoadID = I.LoadID and C.Name = 'Code'
					inner join LoadItemColumn IC on C.ColumnIndex = IC.ColumnIndex and IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex 
			where	I.LoadID = @id
					and I.ObjectID is null
					and I.KeyHash is not null;

		insert ReferenceItem (ReferenceItemTypeID, Code, UpdatedOn, UpdatedBy, CreatedOn, CreatedBy)
		output inserted.ID into @ri_insertOutputID
			select	@ObjectID, 
					Code,
					@UpdatedOn, 
					@UpdatedBy, 
					@UpdatedOn, 
					@UpdatedBy
			from	@ri_insertToPerform;

		-- Insert the added records into temp table for logging.
		insert into #tbl 
			select	N.ObjectID,
					I.RowIndex,
					'A', null, null
			from	LoadItem I
					inner join @ri_insertToPerform P on P.KeyHash = I.KeyHash and I.LoadID = @id 
					inner join @ri_insertOutputID N on N.RowID = P.RowID;

		-- Update the LoadItem table with the Object and ObjectID generated from the insert above.
		update	T
		set		T.Object = 'ReferenceItem',
				T.ObjectID = S.ObjectID
		from	LoadItem T
				inner join #tbl S on T.LoadID = @id and S.RowIndex = T.RowIndex and S.[Action] = 'A';
	end
	-------------------------
	

	-- Capture field logs	
	IF OBJECT_ID('tempdb..#fields') IS NOT NULL
			DROP TABLE #fields;

	create table #fields (RowIndex int, ColumnIndex int, [Action] varchar(25));

	--CREATE CLUSTERED INDEX PK_tempFields ON #fields ([RowIndex] ASC,[ColumnIndex] ASC);

	-- Non-relationship fields
	merge	Field as T
	using	(
			select	I.FieldTypeID,
					I.Type,
					I.AllowMultipleValues,
					I.Object,
					I.ObjectID,
					case 
						when I.Type = 'Lookup' and I.AllowMultipleValues = 0 then cast(C.LookupObjectID as nvarchar)
						when I.Type = 'Lookup' and I.AllowMultipleValues = 1 then C.LookupValue
						else C.Value
					end as [Value],
					C.RowIndex,
					C.ColumnIndex
			from	(
					select		I.LoadID,
								FT.ID as FieldTypeID,
								FT.Type,
								FT.AllowMultipleValues,
								I.Object,
								I.ObjectID,
								min(I.RowIndex) as RowIndex,
								C.ColumnIndex
					from		LoadItem I
								inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id
								inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex
								inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
														and  (
															FT.Name = LC.Name or
																(
																	@Object = 'TaxonomyType'
																	 and LC.ColumnIndex in (
																		select LC2.ColumnIndex from TaxonomyTypeLevel L2
																		inner join LoadColumn LC2 on LC2.LoadID = @id and L2.[Name] = substring(LC2.[Name], 1, len(LC2.[Name]) - charindex(' ', reverse(LC2.[Name])))
																		inner join LoadItemColumn LI2 on LI2.LoadID = @id and LI2.RowIndex = C.RowIndex and LI2.ColumnIndex = LC2.ColumnIndex and LI2.[Value] is not null
																		where L2.TaxonomyTypeID = @ObjectID and L2.[Level] = (select [level] from @levels where rowIndex = C.RowIndex)
																	 )
																	 and FT.Name = reverse(substring(reverse(LC.[Name]), 0, charindex(' ',reverse(LC.[Name])))) 
																)
															)
														and FT.Type <> 'Relationship' 
														and ( 
																(FT.Type <> 'Lookup' and C.Value is not null) OR 
																(FT.Type = 'Lookup' and FT.AllowMultipleValues = 0 and C.LookupObjectID is not null) OR
																(FT.Type = 'Lookup' and FT.AllowMultipleValues = 1 and C.LookupValue is not null)
															)
					where		I.ObjectID is not null
					group by	I.LoadID,
								FT.ID,
								FT.Type,
								FT.AllowMultipleValues,
								I.Object,
								I.ObjectID,
								C.ColumnIndex
					) I
					inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and C.ColumnIndex = I.ColumnIndex
			) S on (T.FieldTypeID = S.FieldTypeID and S.Object = T.ObjectType and S.ObjectID = T.ObjectID)
	when matched then
		update	set
				Value = S.Value
	when not matched then
		insert (FieldTypeID, ObjectType, ObjectID, Value)
		values (S.FieldTypeID, S.Object, S.ObjectID, S.Value)
	output S.RowIndex, S.ColumnIndex, $action into #fields;

	delete	T
	from	FieldValue T
			left join (
				select		FT.ID as FieldTypeID,
							I.Object,
							I.ObjectID,
							VS.Value
				from		LoadItem I
							inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id and I.ObjectID is not null and C.LookupValue is not null
							cross apply string_split(C.LookupValue, ',') VS
							inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex
							inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
													and FT.Name = LC.Name 
													and FT.Type = 'Lookup' 
													and FT.AllowMultipleValues = 1
			) S on S.FieldTypeID = T.FieldTypeID and S.Object = T.ObjectType and S.ObjectID = T.ObjectID and S.value = T.Value
			inner join (	--LIMITS THE IMPACT OF THIS STATEMENT
				select		FT.ID as FieldTypeID,
							I.Object,
							I.ObjectID
				from		LoadItem I
							inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id and I.ObjectID is not null and C.LookupValue is not null
							inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex
							inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
													and FT.Name = LC.Name 
													and FT.Type = 'Lookup' 
													and FT.AllowMultipleValues = 1			
			) L on L.FieldTypeID = T.FieldTypeID and L.Object = T.ObjectType and L.ObjectID = T.ObjectID
	where	S.FieldTypeID is null;

	insert into FieldValue (FieldTypeID, ObjectType, ObjectID, Value)
		select		FT.ID,
					I.Object,
					I.ObjectID,
					VS.Value
		from		LoadItem I
					inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id and I.ObjectID is not null and C.LookupValue is not null
					cross apply string_split(C.LookupValue, ',') VS
					inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex
					inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
											and FT.Name = LC.Name 
											and FT.Type = 'Lookup' 
											and FT.AllowMultipleValues = 1
					left join FieldValue FV on FV.FieldTypeID = FT.ID and FV.ObjectType = I.Object and FV.ObjectID = I.ObjectID and FV.Value = VS.Value
		where		FV.ID is null;

	update	T
	set		T.FieldsLoaded = 1
	from	#tbl T
			inner join	(
						select		RowIndex,
									[Action]
						from		#fields
						group by	RowIndex, 
									[Action]
						) S on S.RowIndex = T.RowIndex;

	truncate table #fields;

	-- Parent fields
	declare @parentTypeID int = null,
			@parentTypeName nvarchar(250) = null;
	declare @parentIntersectTypeId int = null;

	select 
		@parentTypeID = I.SubjectID,
		@parentTypeName = I.SubjectName,
		@parentIntersectTypeId = I.ID
	from 
		intersecttypedetail I                
	where I.[PredicateType] = 3 and [Object] = @Object and ObjectID = @ObjectId;
	
	if @parentTypeID is not null
	begin
	
		-- look for column with the parent type name this contains the parent 
		merge	[Intersect] as T
		using	(
				select	distinct
						AD.ObjectID as ParentObjectID,
						AD.[Object] as ParentObject,
						AD.[TypeID] as ParentTypeID,
						AD.[Type] as ParentType,
						@parentIntersectTypeId as IntersectTypeID,
						LI.[Object] as ItemObject,
						LI.ObjectID as ItemObjectID
				from	LoadItem I
						inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id
						inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex and LC.Name = @parentTypeName
						inner join AssetDetail AD on AD.TypeID = @parentTypeID and AD.DisplayValue = C.Value and AD.[Type] = @Object	
						inner join LoadItem LI on LI.RowIndex = C.RowIndex and LI.LoadID = C.LoadID					
				where	I.ObjectID is not null
				) S on (T.IntersectTypeID = S.IntersectTypeID and T.Subject = S.ParentObject and T.SubjectID = S.ParentObjectID and T.Object = S.ItemObject and T.ObjectID = S.ItemObjectID)

		when matched then
			update	set
					T.Subject	= S.ParentObject,
					T.SubjectID = S.ParentObjectID,
					T.Object	= S.ItemObject,
					T.ObjectID	= S.ItemObjectID
		when not matched then
			insert (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner], Visible)
			values (
					S.IntersectTypeID,
					S.ParentObject, 
					S.ParentObjectID,
					S.ItemObject, 
					S.ItemObjectID, 
					0, @UpdatedBy, @UpdatedOn, @UpdatedBy, @UpdatedOn, 'BulkLoad', 1
					);

	end

	-- Relationship fields
	merge	[Intersect] as T
	using	(
			select	distinct
					FT.LookupObjectID as IntersectTypeID,
					case 
						when IT.Subject = @Object and IT.SubjectID = @ObjectID then cast(1 as bit)
						else cast(0 as bit)
					end as IsSubject,
					case 
						when IT.Subject = @Object and IT.SubjectID = @ObjectID then I.Object
						else C.LookupObject
					end as Subject,
					case 
						when IT.Subject = @Object and IT.SubjectID = @ObjectID then I.ObjectID
						else C.LookupObjectID
					end as SubjectID,
					case 
						when IT.Subject = @Object and IT.SubjectID = @ObjectID then C.LookupObject
						else I.Object
					end as Object,
					case 
						when IT.Subject = @Object and IT.SubjectID = @ObjectID then C.LookupObjectID
						else I.ObjectID
					end as ObjectID
			from	LoadItem I
					inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id
					inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex
					inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
											and FT.Name = LC.Name and FT.Type = 'Relationship' 
											and C.LookupObject is not null and C.LookupObjectID is not null
					inner join IntersectType IT on FT.LookupObjectType = 'IntersectType' and FT.LookupObjectID = IT.ID
			where	I.ObjectID is not null
			) S on (
					T.IntersectTypeID = S.IntersectTypeID 
					and (
							(S.IsSubject = 1 and S.Subject = T.Subject and S.SubjectID = T.SubjectID) OR
							(S.IsSubject = 0 and S.Object = T.Object and S.ObjectID = T.ObjectID)
						)
					)
	when matched then
		update	set
				T.Subject	= S.Subject,
				T.SubjectID = S.SubjectID,
				T.Object	= S.Object,
				T.ObjectID	= S.ObjectID
	when not matched then
		insert (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner], Visible)
		values (
				S.IntersectTypeID,
				S.Subject, 
				S.SubjectID,
				S.Object, 
				S.ObjectID, 
				0, @UpdatedBy, @UpdatedOn, @UpdatedBy, @UpdatedOn, 'BulkLoad', 1
				);
	
	-- Capture logs and update load status. -----
	update	T
	set		T.Status = 1,
			T.StatusMessage = 'Item successfully ' + case S.[Action] when 'A' then 'added' else 'updated' end + '.'
	from	LoadItem T
			inner join #tbl S on T.LoadID = @id and S.RowIndex = T.RowIndex and T.[Object] is not null and T.ObjectID is not null;

	update	LoadItem
	set		Status = 0,
			StatusMessage = 'Item load failed. ' + coalesce(StatusMessage, '')
	where	([Object] is null or ObjectID is null)
			and LoadID = @id;

	----Finally, close out the Load.
	update	[Load] 
	set		DateCompleted = getutcdate()
	where	ID = @id
	---------------------------------------------
end
GO

alter procedure [fusion].[GenerateMarkitMapLineageData]
	@fusionID int
as
begin
	SET NOCOUNT, ANSI_PADDING ON;
	SET ANSI_WARNINGS ON;

	declare @databaseName varchar(100);
	declare @sourceFieldTypeID int;
	declare @targetFieldTypeID int;		
	declare @mapFusionAttributeTypeID int = 710; -- this is fixed for all clients
	declare @viewColumnFusionAttributeTypeID int = 715; -- this is fixed for all clients
	
	-- load the field ids for the source / target from mappings
	select @sourceFieldTypeID = id from fieldtype where [object] = 'FusionAttributeType' and [objectid] = @mapFusionAttributeTypeID and name = 'source';
	select @targetFieldTypeID = id from fieldtype where [object] = 'FusionAttributeType' and [objectid] = @mapFusionAttributeTypeID and name = 'target';
	
	IF @sourceFieldTypeID IS NULL
	begin		
		raiserror('ERROR - Cannot find the Markit Fusion Map Source Field.  Please make sure the latest markit fusion attribute types have been pushed to this environment', 16, -1);
		return;
	end

	IF @targetFieldTypeID IS NULL
	begin		
		raiserror('ERROR - Cannot find the Markit Fusion Map Target Field.  Please make sure the latest markit fusion attribute types have been pushed to this environment', 16, -1);
		return;
	end

	-- determine the database name
	select top 1 @databaseName = replace(sourceid, name,'') from fusionattribute where fusionid = @fusionID and fusionattributetypeid = 711 and sourceid like '%.%';	

	if @databaseName is null
	begin
		raiserror('ERROR - Cannot determine the database name to strip from markit fusion attribute data', 16, -1);
		return;
	end

	-- dont run if this is not a markit fusion
	declare @fusionTypeId int;
	select @fusionTypeId = FusionTypeID from [dbo].[Fusion] where ID = @fusionID;
	if @fusionTypeId != 13
	begin
		raiserror('ERROR - The fusion lineage generation process may only be run for the Markit Fusion Type', 16, -1);
		return;
	end

	-- dont run if no map records exist for this fusion
	if not exists( select 1 from fusionattribute where fusionid = @fusionID and fusionattributetypeid = @mapFusionAttributeTypeID )
	begin
		raiserror('ERROR - No Markit Fusion Map records exist for the specified Fusion ID', 16, -1);
		return;
	end

	-- figure out the database prefix from some markit data

	-- some logging
	declare @fusionName nvarchar(250);
	select @fusionName = name from [dbo].[fusion] where id = @fusionID;

	begin
		print 'Running For Fusion:' + @fusionName;
		print 'Using Target Field ID:' + cast(@targetFieldTypeID as varchar(100));
		print 'Using Source Field ID:' + cast(@sourceFieldTypeID as varchar(100));
		print 'Using Database prefix:' + @databaseName;
	end
	-- end logging

	-- get the intersecttypeid for view -> table intersects
	declare @viewTableIntersectTypeId int;
	select @viewTableIntersectTypeId = id from intersecttype where [object] = 'FusionAttributeType' and [Subject] = 'FusionAttributeType' and [subjectid] = 714 and [objectid] = 712
	if @viewTableIntersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for markit view/table relations', 16, -1);
		return;
	end

	-- get the intersecttypeid for view -> view intersects
	declare @viewViewIntersectTypeId int;
	select @viewViewIntersectTypeId = id from intersecttype where [object] = 'FusionAttributeType' and [Subject] = 'FusionAttributeType' and [subjectid] = 714 and [objectid] = 714
	if @viewViewIntersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for markit view/view relations', 16, -1);
		return;
	end

	IF OBJECT_ID('tempdb..#maps') IS NOT NULL
		DROP TABLE #maps;

	create table #maps (	
		ID int identity primary key,			
		MapRuleItemID int,
		[ParentID] int,
		[UltimateParentID] int,
		[Level] int,
		SourceFusionAttributeID int,
		SourceFusionAttributeTypeID int,
		SourceObject nvarchar(500),		
		SourceParentObject nvarchar(max),
		SourceParentObjectFusionAttributeID int,
		SourceParentObjectFusionAttributeTypeID int,
		TargetFusionAttributeID int,
		TargetFusionAttributeTypeID int,
		TargetObject nvarchar(500),
		TargetParentObject nvarchar(max),
		TargetParentObjectFusionAttributeID int,
		TargetParentObjectFusionAttributeTypeID int,					
		[Source] varchar(50),
		[SourceID] int,	
		[Target] varchar(50),
		[TargetID] int,
	);

	CREATE NONCLUSTERED INDEX [CIX_TempMaps] ON #maps ( SourceFusionAttributeID ASC, TargetFusionAttributeID ASC );

	IF OBJECT_ID('tempdb..#objectmap') IS NOT NULL
		DROP TABLE #objectmap;

	create table #objectmap (
		MapID int,
		MapItemID int,
		[Object] varchar(50),
		[ObjectID] int,	
		[SourceIntersectID] int,		
		[TargetIntersectID] int		
	)

	CREATE NONCLUSTERED INDEX [CIX_TempObjectMap] ON #objectmap ( MapID ASC, [Object] ASC, [ObjectID] ASC );
	
	insert into #maps
		(SourceObject, TargetObject)
		select distinct
			replace(cast(F_source.formattedValue as nvarchar(500)), @databaseName, '') as SourceObject						
			, replace(cast(F_target.formattedValue as nvarchar(500)), @databaseName, '') as TargetObject			
		from 
			FusionAttribute FA
			inner join Field F_source on F_source.ObjectType = 'FusionAttribute' and F_source.ObjectID = FA.ID and F_source.FieldTypeID = @sourceFieldTypeID -- MAP SOURCE FIELD VALUE
			inner join Field F_target on F_target.ObjectType = 'FusionAttribute' and F_target.ObjectID = FA.ID and F_target.FieldTypeID = @targetFieldTypeID -- TARGET SOURCE FIELD VALUE
		where 
			FA.FusionID = @fusionID
				and
			FA.FusionAttributeTypeID = @mapFusionAttributeTypeID
			--	and
			--F_source.formattedValue like '%.cusip' or F_source.formattedValue like '%.ticker' or F_source.formattedValue like '%.cntry_of%' -- **for testing to limit to just cusip**;
	
	-- check how many map records we have
	declare @mapRecordCount int;
	select @mapRecordCount = count(1) from #maps
	if @fusionTypeId > 0
		begin
			print 'Loaded [' + cast(@mapRecordCount as varchar) + '] map records';			
		end
	else
		begin
			raiserror('ERROR - Could not load any map records this is most likely because there are no corresponding fusionattributes for the markit source/target mappings.', 16, -1);
			return;
		end

			
	--set the Source objects 
	update	T
	set		T.SourceFusionAttributeID = S.ID, T.SourceFusionAttributeTypeID = S.FusionAttributeTypeID
	from	#maps T			
			inner join fusionattribute S on (S.TextPath = T.SourceObject and S.FusionID = @fusionID)

	--set the Target Objects
	update	T
	set		T.TargetFusionAttributeID = S.ID, T.TargetFusionAttributeTypeID = S.FusionAttributeTypeID
	from	#maps T			
			inner join fusionattribute S on (S.TextPath = T.TargetObject and S.FusionID = @fusionID)

	--remove any source objects that we cant find the fusion attribute for
	delete from #maps where SourceFusionAttributeID is null or TargetFusionAttributeID is null		
	
	--set the source parent objects
	update T
	set T.SourceParentObject = FA_p.TextPath, T.SourceParentObjectFusionAttributeID = FA_p.ID, T.SourceParentObjectFusionAttributeTypeID = FA_p.FusionAttributeTypeID
	from #maps T
		inner join fusionattribute FA on (FA.ID = T.SourceFusionAttributeID)
		inner join fusionattribute FA_p on (FA_p.ID = FA.ParentID)

	--set the target parent objects
	update T
	set T.TargetParentObject = FA_p.TextPath, T.TargetParentObjectFusionAttributeID = FA_p.ID, T.TargetParentObjectFusionAttributeTypeID = FA_p.FusionAttributeTypeID
	from #maps T
		inner join fusionattribute FA on (FA.ID = T.TargetFusionAttributeID)
		inner join fusionattribute FA_p on (FA_p.ID = FA.ParentID)

	-- remove any maps that reference same fusionattribute both sides
	delete from #maps where SourceFusionAttributeID = TargetFusionAttributeID;
	
	--this query adds in the view to table mapings
	-- add in any view column to table column records
	-- table / view maps for targets that are missing connection
	insert into #maps
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, SourceParentObjectFusionAttributeID, SourceParentObject, SourceParentObjectFusionAttributeTypeID, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject, TargetParentObjectFusionAttributeID, TargetParentObject, TargetParentObjectFusionAttributeTypeID)
		select 	distinct
			m.TargetFusionAttributeID as SourceFusionAttributeID,
			m.TargetFusionAttributeTypeID as SourceFusionAttributeTypeID,
			m.TargetObject as SourceObject,
			m.TargetParentObjectFusionAttributeID as SourceParentObjectFusionAttributeID,
			m.TargetParentObject as SourceParentObject,
			m.TargetParentObjectFusionAttributeTypeID as SourceParentObjectFusionAttributeTypeID,
			T.id as TargetFusionAttributeID,
			T.fusionattributetypeid as TargetFusionAttributeTypeID,
			T.textpath as TargetObject,
			i.objectid as TargetParentObjectFusionAttributeID,
			T_p.name as TargetParentObject,
			T_p.fusionattributetypeid as TargetParentObjectFusionAttributeTypeID			
		 from 
			#maps m			
			inner join [intersect] i on (i.subjectid = m.TargetParentObjectFusionAttributeID and i.[subject] = 'FusionAttribute')	
			inner join fusionattribute T_p on (T_p.id = i.objectid)
			inner join fusionattribute T on(T.parentid = T_p.id and T.Textpath = T_p.TextPath + replace(m.TargetObject,m.TargetParentObject,'')) -- we are doing this to avoid messing with the name column that doesnt have an index
		where 
			m.TargetFusionAttributeTypeID = @viewColumnFusionAttributeTypeID
				and
			i.intersecttypeid = @viewTableIntersectTypeId
				and
			m.id not in(select m_2.id from #maps m_2 where (m_2.SourceFusionAttributeID = m.TargetFusionAttributeID and m_2.TargetFusionAttributeID = T.Id) or (m_2.TargetFusionAttributeID = m.TargetFusionAttributeID and m_2.SourceFusionAttributeID = T.id)) -- dont insert duplicates
	
	-- table / view maps for sources that are missing connection
	insert into #maps
		(TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject, TargetParentObjectFusionAttributeID, TargetParentObject, TargetParentObjectFusionAttributeTypeID, SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, SourceParentObjectFusionAttributeID, SourceParentObject, SourceParentObjectFusionAttributeTypeID)
		select 	distinct
			m.SourceFusionAttributeID as TargetFusionAttributeID,
			m.SourceFusionAttributeTypeID as TargetFusionAttributeTypeID,
			m.SourceObject as TargetObject,
			m.SourceParentObjectFusionAttributeID as TargetParentObjectFusionAttributeID,
			m.SourceParentObject as TargetParentObject,
			m.SourceParentObjectFusionAttributeTypeID as TargetParentObjectFusionAttributeTypeID,
			T.id as SourceFusionAttributeID,
			T.fusionattributetypeid as SourceFusionAttributeTypeID,
			T.textpath as SourceObject,
			i.objectid as SourceParentObjectFusionAttributeID,
			T_p.name as SourceParentObject,
			T_p.fusionattributetypeid as SourceParentObjectFusionAttributeTypeID			
		 from 
			#maps m			
			inner join [intersect] i on (i.subjectid = m.SourceParentObjectFusionAttributeID and i.[subject] = 'FusionAttribute')	
			inner join fusionattribute T_p on (T_p.id = i.objectid)
			inner join fusionattribute T on(T.parentid = T_p.id and T.Textpath = T_p.TextPath + replace(m.SourceObject,m.SourceParentObject,'')) -- we are doing this to avoid messing with the name column that doesnt have an index
		where 
			m.SourceFusionAttributeTypeID = @viewColumnFusionAttributeTypeID
				and
			i.intersecttypeid = @viewTableIntersectTypeId
				and
			m.id not in(select m_2.id from #maps m_2 where (m_2.SourceFusionAttributeID = T.Id and m_2.TargetFusionAttributeID = m.SourceFusionAttributeID) or (m_2.TargetFusionAttributeID = T.id  and m_2.SourceFusionAttributeID = m.SourceFusionAttributeID)) -- dont insert duplicates
					
	-- end table / view maps

	

	--this query adds in the view to view mapings
	-- add in any view column to view column records
	-- view / view maps for targets that are missing connection
	/*insert into #maps
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, SourceParentObjectFusionAttributeID, SourceParentObject, SourceParentObjectFusionAttributeTypeID, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject, TargetParentObjectFusionAttributeID, TargetParentObject, TargetParentObjectFusionAttributeTypeID)
		select 	distinct
			m.TargetFusionAttributeID as SourceFusionAttributeID,
			m.TargetFusionAttributeTypeID as SourceFusionAttributeTypeID,
			m.TargetObject as SourceObject,
			m.TargetParentObjectFusionAttributeID as SourceParentObjectFusionAttributeID,
			m.TargetParentObject as SourceParentObject,
			m.TargetParentObjectFusionAttributeTypeID as SourceParentObjectFusionAttributeTypeID,
			T.id as TargetFusionAttributeID,
			T.fusionattributetypeid as TargetFusionAttributeTypeID,
			T.textpath as TargetObject,
			i.objectid as TargetParentObjectFusionAttributeID,
			T_p.name as TargetParentObject,
			T_p.fusionattributetypeid as TargetParentObjectFusionAttributeTypeID			
		 from 
			#maps m			
			inner join [intersect] i on (i.subjectid = m.TargetParentObjectFusionAttributeID and i.[subject] = 'FusionAttribute')	
			inner join fusionattribute T_p on (T_p.id = i.objectid)
			inner join fusionattribute T on(T.parentid = T_p.id and T.Textpath = T_p.TextPath + replace(m.TargetObject,m.TargetParentObject,'')) -- we are doing this to avoid messing with the name column that doesnt have an index
		where 
			m.TargetFusionAttributeTypeID = @viewColumnFusionAttributeTypeID
				and
			i.intersecttypeid = @viewViewIntersectTypeId
				and
			m.id not in(select m_2.id from #maps m_2 where (m_2.SourceFusionAttributeID = m.TargetFusionAttributeID and m_2.TargetFusionAttributeID = T.Id) or (m_2.TargetFusionAttributeID = m.TargetFusionAttributeID and m_2.SourceFusionAttributeID = T.id)) -- dont insert duplicates
	*/
	insert into #maps
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, SourceParentObjectFusionAttributeID, SourceParentObject, SourceParentObjectFusionAttributeTypeID, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject, TargetParentObjectFusionAttributeID, TargetParentObject, TargetParentObjectFusionAttributeTypeID)
		select 	distinct
			m.TargetFusionAttributeID as SourceFusionAttributeID,
			m.TargetFusionAttributeTypeID as SourceFusionAttributeTypeID,
			m.TargetObject as SourceObject,
			m.TargetParentObjectFusionAttributeID as SourceParentObjectFusionAttributeID,
			m.TargetParentObject as SourceParentObject,
			m.TargetParentObjectFusionAttributeTypeID as SourceParentObjectFusionAttributeTypeID,
			T.id as TargetFusionAttributeID,
			T.fusionattributetypeid as TargetFusionAttributeTypeID,
			T.textpath as TargetObject,
			i.objectid as TargetParentObjectFusionAttributeID,
			T_p.name as TargetParentObject,
			T_p.fusionattributetypeid as TargetParentObjectFusionAttributeTypeID			
		 from 
			#maps m			
			inner join [intersect] i on (i.objectid = m.TargetParentObjectFusionAttributeID and i.[object] = 'FusionAttribute')	
			inner join fusionattribute T_p on (T_p.id = i.subjectid)
			inner join fusionattribute T on(T.FusionId = @fusionId and T.deleted = 0 and T.parentid = T_p.id and T.Textpath = T_p.TextPath + replace(m.TargetObject,m.TargetParentObject,'')) -- we are doing this to avoid messing with the name column that doesnt have an index
		where 
			m.TargetFusionAttributeTypeID = @viewColumnFusionAttributeTypeID
				and
			i.intersecttypeid = @viewViewIntersectTypeId
				and
			m.id not in(select m_2.id from #maps m_2 where (m_2.SourceFusionAttributeID = m.TargetFusionAttributeID and m_2.TargetFusionAttributeID = T.Id) or (m_2.TargetFusionAttributeID = m.TargetFusionAttributeID and m_2.SourceFusionAttributeID = T.id)) -- dont insert duplicates

	-- view / view maps for sources that are missing connection
	insert into #maps
		(TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject, TargetParentObjectFusionAttributeID, TargetParentObject, TargetParentObjectFusionAttributeTypeID, SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, SourceParentObjectFusionAttributeID, SourceParentObject, SourceParentObjectFusionAttributeTypeID)
		select 	distinct
			m.SourceFusionAttributeID as TargetFusionAttributeID,
			m.SourceFusionAttributeTypeID as TargetFusionAttributeTypeID,
			m.SourceObject as TargetObject,
			m.SourceParentObjectFusionAttributeID as TargetParentObjectFusionAttributeID,
			m.SourceParentObject as TargetParentObject,
			m.SourceParentObjectFusionAttributeTypeID as TargetParentObjectFusionAttributeTypeID,
			T.id as SourceFusionAttributeID,
			T.fusionattributetypeid as SourceFusionAttributeTypeID,
			T.textpath as SourceObject,
			i.objectid as SourceParentObjectFusionAttributeID,
			T_p.name as SourceParentObject,
			T_p.fusionattributetypeid as SourceParentObjectFusionAttributeTypeID			
		 from 
			#maps m			
			inner join [intersect] i on (i.subjectid = m.SourceParentObjectFusionAttributeID and i.[subject] = 'FusionAttribute')	
			inner join fusionattribute T_p on (T_p.id = i.objectid)
			inner join fusionattribute T on(T.FusionId = @fusionId and T.deleted = 0 and T.parentid = T_p.id and T.Textpath = T_p.TextPath + replace(m.SourceObject,m.SourceParentObject,'')) -- we are doing this to avoid messing with the name column that doesnt have an index
		where 
			m.SourceFusionAttributeTypeID = @viewColumnFusionAttributeTypeID
				and
			i.intersecttypeid = @viewViewIntersectTypeId
				and
			m.id not in(select m_2.id from #maps m_2 where (m_2.SourceFusionAttributeID = T.Id and m_2.TargetFusionAttributeID = m.SourceFusionAttributeID) or (m_2.TargetFusionAttributeID = T.id  and m_2.SourceFusionAttributeID = m.SourceFusionAttributeID)) -- dont insert duplicates

	/*	insert into #maps
		(TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject, TargetParentObjectFusionAttributeID, TargetParentObject, TargetParentObjectFusionAttributeTypeID, SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, SourceParentObjectFusionAttributeID, SourceParentObject, SourceParentObjectFusionAttributeTypeID)
		select 	distinct
			m.SourceFusionAttributeID as TargetFusionAttributeID,
			m.SourceFusionAttributeTypeID as TargetFusionAttributeTypeID,
			m.SourceObject as TargetObject,
			m.SourceParentObjectFusionAttributeID as TargetParentObjectFusionAttributeID,
			m.SourceParentObject as TargetParentObject,
			m.SourceParentObjectFusionAttributeTypeID as TargetParentObjectFusionAttributeTypeID,
			T.id as SourceFusionAttributeID,
			T.fusionattributetypeid as SourceFusionAttributeTypeID,
			T.textpath as SourceObject,
			i.objectid as SourceParentObjectFusionAttributeID,
			T_p.name as SourceParentObject,
			T_p.fusionattributetypeid as SourceParentObjectFusionAttributeTypeID			
		 from 
			#maps m			
			inner join [intersect] i on (i.objectid = m.SourceParentObjectFusionAttributeID and i.[object] = 'FusionAttribute')	
			inner join fusionattribute T_p on (T_p.id = i.subjectid)
			inner join fusionattribute T on(T.parentid = T_p.id and T.Textpath = T_p.TextPath + replace(m.SourceObject,m.SourceParentObject,'')) -- we are doing this to avoid messing with the name column that doesnt have an index
		where 
			m.SourceFusionAttributeTypeID = @viewColumnFusionAttributeTypeID
				and
			i.intersecttypeid = @viewViewIntersectTypeId
				and
			m.id not in(select m_2.id from #maps m_2 where (m_2.SourceFusionAttributeID = T.Id and m_2.TargetFusionAttributeID = m.SourceFusionAttributeID) or (m_2.TargetFusionAttributeID = T.id  and m_2.SourceFusionAttributeID = m.SourceFusionAttributeID)) -- dont insert duplicates
		*/				
	-- end view / view maps


	-- populate the previous step id this also duplicates items that have multiple paths and is very important
	update m_S
	set m_S.ParentID = m_T.ID
	from #maps m_T
	left outer join #maps m_S on (m_T.TargetFusionAttributeID = m_S.SourceFusionAttributeID)

	IF OBJECT_ID('tempdb..#levelMap') IS NOT NULL
		DROP TABLE #levelMap;
	
	;with C as
			(
			  select
				ID,
				SourceFusionAttributeID as SourceID,
				TargetFusionAttributeID as TargetID,
				ID as [UltimateParentID],
				0 as [level] 
			  from 
					#maps
			  where ParentID is null
			  union all
			  select 
					T.ID,
					T.SourceFusionAttributeID as SourceID,			 
					 T.TargetFusionAttributeID as TargetID,
					 C.[UltimateParentID] as [UltimateParentID],
					 C.[level] + 1
			  from #maps as T
				inner join C  
					on T.ParentID = C.ID				  
			)
			select C.ID, C.[level], C.[UltimateParentID]
			into #levelMap
			from C
			OPTION (MAXRECURSION 25) 

	update T
	set T.[level] = S.[level], T.[UltimateParentID] = S.[UltimateParentID]
	from #maps T
	inner join #levelMap S on S.ID = T.ID;
	
	--remove any that we cant find the level for
	--delete from #maps where [level] is null		


	-- find any object related to column as the object	
	insert into #objectmap (MapID, [Object], [ObjectID])
		select T.ID, OI.[subject], OI.[subjectid]
		from #maps T
		inner join [IntersectDetail] OI on OI.Subject <> 'FusionAttribute' and OI.Object = 'FusionAttribute' and OI.ObjectID in (T.SourceFusionAttributeID, T.TargetFusionAttributeID)  and OI.PredicateType = 8-- look for relation between non fusion object and source/target column

	-- find any business terms related to source
	update T
	set T.[source] = OI.[subject], T.[sourceid] = OI.[subjectid]--, T.sourceintersectid = OI.ID
	from #maps T
		inner join [IntersectDetail] OI on OI.Subject <> 'FusionAttribute' and OI.Object = 'FusionAttribute' and OI.ObjectID = T.SourceParentObjectFusionAttributeID  and OI.PredicateType = 8 

	
	-- find any business terms related to target
	update T
	set T.[target] = OI.[subject], T.[targetid] = OI.[subjectid]--, T.targetintersectid = OI.ID
	from #maps T
		inner join [IntersectDetail] OI on OI.Subject <> 'FusionAttribute' and OI.Object = 'FusionAttribute' and OI.ObjectID = T.TargetParentObjectFusionAttributeID and OI.PredicateType = 8
		
	-- update the objects for each path to be the same	
	insert into #objectmap (MapID, [Object], [ObjectID])
		select T.ID, SO.[object], SO.[objectID]
		from #maps T		
		inner join #maps S on T.UltimateParentID = S.UltimateParentID
		inner join #objectmap SO on S.ID = SO.MapID
		left join #objectmap T_O on (T.ID = T_O.MapID and T_O.[object] is null);
	
	
	--take any sources with null targets find the next target

	WITH hierarchy (id, [target], [targetid], [source], [sourceid]) AS
	(
		SELECT id, [target], [targetid], [source], [sourceid]
		FROM #maps
		WHERE [parentid] is null

		UNION ALL

		SELECT mc.id, coalesce(mc.[target], mc.[source], gps.[target]) as [target], coalesce(mc.targetid, mc.sourceid, gps.targetid) as [targetid], coalesce(mc.[source], gps.[target], gps.[source]) as [source], coalesce(mc.sourceid, gps.targetid, gps.sourceid) as [targetid]
		FROM #maps mc
		JOIN hierarchy gps ON gps.id = mc.parentid
	)
	UPDATE T
	set T.[target] = cte.[target], T.[targetid] = cte.[targetid], T.[source] = cte.[source], T.[sourceid] = cte.[sourceid]
	from #maps T
	inner join 
		hierarchy cte
	on cte.id = T.id
	OPTION (MAXRECURSION 50)
			
	-- generate relationships for each unique object / source that dont exist

	update T
	set T.[sourceintersectid] = OI.ID
	from #objectmap T
		inner join #maps M on (T.MapID = M.ID)
		inner join [IntersectDetail] OI on OI.[Subject] = M.[Source] and OI.SubjectID = M.[SourceID] and OI.[Object] = T.[Object] and OI.[ObjectID] = T.[ObjectID];

	update T
	set T.[sourceintersectid] = OI.ID
	from #objectmap T
		inner join #maps M on (T.MapID = M.ID)
		inner join [IntersectDetail] OI on OI.[Object] = M.[Source] and OI.ObjectID = M.[SourceID] and OI.[Subject] = T.[Object] and OI.[SubjectID] = T.[ObjectID] and T.sourceintersectid is null
	
	-- add any missing relations to source / object
	insert into [intersect] (IntersectTypeID, [Subject], SubjectID, [Object], ObjectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
		select distinct
			(select top 1 i_t.ID from [intersecttype] i_t where (i_t.[object] = c_s.objecttype and i_t.[subject] = c_t.objecttype and i_t.objectid = c_s.objecttypeid and i_t.subjectid = c_t.objecttypeid))				
			,T.[Source]
			,T.[SourceID]
			,OM.[Object]
			,OM.[ObjectID]
			,0,getutcdate(),0,getutcdate(),'MARKIT LINEAGE'
		from #maps T
		inner join #objectmap OM on (T.ID = OM.MapID)
		inner join [cache].[objectdetails] c_s on (c_s.[object] = OM.[object] and c_s.[objectid] = OM.[objectid])
		inner join [cache].[objectdetails] c_t on (c_t.[object] = T.[source] and c_t.[objectid] = T.[sourceid])		
		where OM.sourceIntersectID is null;
	
	update OM
	set OM.[sourceintersectid] = OI.ID
	from #objectmap OM
		inner join #maps T on (OM.MapID = T.ID)		
		inner join [IntersectDetail] OI on OI.[Subject] = T.[Source] and OI.SubjectID = T.[SourceID] and OI.[Object] = OM.[Object] and OI.[ObjectID] = OM.[ObjectID] and OM.sourceintersectid is null;

	
	-- generate relationships for each unique object / target that dont exist	
	update OM
	set OM.[targetintersectid] = OI.ID
	from #objectmap OM
		inner join #maps T on (OM.MapID = T.ID)
		inner join [IntersectDetail] OI on OI.[Subject] = T.[Target] and OI.SubjectID = T.[TargetID] and OI.[Object] = OM.[Object] and OI.[ObjectID] = OM.[ObjectID]
		
	update OM
	set OM.[targetintersectid] = OI.ID
	from #objectmap OM
		inner join #maps T on (OM.MapID = T.ID)
		inner join [IntersectDetail] OI on OI.[Object] = T.[Target] and OI.ObjectID = T.[TargetID] and OI.[Subject] = OM.[Object] and OI.[SubjectID] = OM.[ObjectID] and OM.targetintersectid is null;

	-- add any missing relations to source / object
	insert into [intersect] (IntersectTypeID, [Subject], SubjectID, [Object], ObjectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
		select distinct
			(select top 1 i_t.ID from [intersecttype] i_t where (i_t.[object] = c_s.objecttype and i_t.[subject] = c_t.objecttype and i_t.objectid = c_s.objecttypeid and i_t.subjectid = c_t.objecttypeid))			
			,T.[target]
			,T.[targetID]
			,OM.[Object]
			,OM.[ObjectID]
			,0,getutcdate(),0,getutcdate(),'MARKIT LINEAGE'
		from #maps T
		inner join #objectmap OM on (T.ID = OM.MapID)
		inner join [cache].[objectdetails] c_s on (c_s.[object] = OM.[object] and c_s.[objectid] = OM.[objectid])
		inner join [cache].[objectdetails] c_t on (c_t.[object] = T.[target] and c_t.[objectid] = T.[targetid])		
		where OM.targetintersectid is null;
		
	update OM
	set OM.[targetintersectid] = OI.ID
	from #objectmap OM
		inner join #maps T on (OM.MapID = T.ID)
		inner join [IntersectDetail] OI on OI.[Subject] = T.[Target] and OI.SubjectID = T.[TargetID] and OI.[Object] = OM.[Object] and OI.[ObjectID] = OM.[ObjectID] and OM.targetintersectid is null;
	

	/*testing only!!*/			
--	select * from #maps order by [ultimateparentid], [level]
	/*end testing only*/

	print 'Removing any prior generated Markit Lineage map records';

	-- clear any previous values from map rule item map item table
	--delete from mapitem where [owner] = 'MARKIT LINEAGE';
	--delete from mapruleitem where [owner] = 'MARKIT LINEAGE';
	delete from mapruleitemmapitem where [owner] = 'MARKIT LINEAGE';

	print 'Inserting new map records';
	-- insert mapping data
	
	Declare @MapItemIDList Table(MapItemID int, sourceintersectid int, targetintersectid int);
	Declare @MapRuleItemIDList Table(MapRuleItemID int, MapID Int);
	
	-- load any existing map item instances
	update T
	set T.MapItemID = mi.ID
	from #objectmap T
		inner join mapitem mi on(T.sourceintersectid = mi.SourceIntersectID and T.targetintersectid = mi.TargetIntersectID and mi.[Owner] = 'MARKIT LINEAGE'); 

	-- insert map records
	MERGE
	INTO    mapitem mi
	USING   (			
			select distinct sourceintersectid, targetintersectid FROM #objectmap where (sourceintersectid is not null and targetintersectid is not null) and sourceintersectid != targetintersectid and mapitemid is null
			) S
	ON      (1 = 0)
	WHEN NOT MATCHED THEN
	INSERT  (SourceIntersectID, TargetIntersectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
	VALUES  (S.sourceintersectid, S.targetintersectid, 0, getutcdate(), 0, getutcdate(), 'MARKIT LINEAGE')
	OUTPUT  INSERTED.ID, S.sourceintersectid, S.targetintersectid into @MapItemIDList;

	--update map item id from main temp table
	update T
	set T.mapitemid = MI.MapItemID
	from #objectmap T
		inner join @MapItemIDList MI on (MI.sourceintersectid = T.sourceintersectid and MI.targetintersectid = T.targetintersectid)
		
	-- delete any mapitem records that are not in objectmap that are markit lineage
	delete from mapitem where [owner] = 'MARKIT LINEAGE' and id not in (select mapitemid from #objectmap);
	
	-- load id's of existing mapruleitems
	update T
	set T.mapruleitemid = S.id
	from #maps T
		inner join [dbo].[mapruleitem] S on (S.[owner] = 'MARKIT LINEAGE' and S.SourceFusionAttributeID = T.SourceFusionAttributeID and S.TargetFusionAttributeID = T.TargetFusionAttributeID);
	
	-- insert the mapruleitem records
	MERGE
	INTO    mapruleitem mri
	USING   (
			select SourceFusionAttributeID, TargetFusionAttributeID, ID from #maps where mapruleitemid is null
			) S
	ON      (1 = 0)
	WHEN NOT MATCHED THEN
	INSERT  (SourceFusionAttributeID, TargetFusionAttributeID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
	VALUES  (S.SourceFusionAttributeID, S.TargetFusionAttributeID, 0, getutcdate(), 0, getutcdate(), 'MARKIT LINEAGE')
	OUTPUT  INSERTED.ID, S.ID into @MapRuleItemIDList;
	
	--update map rule item id from main temp table
	update T
	set T.MapRuleItemID = MI.MapRuleItemID
	from #maps T
		inner join @MapRuleItemIDList MI on (MI.MapID = T.ID);

	-- delete any mapitem records that are not in objectmap that are markit lineage
	delete from mapruleitem where [owner] = 'MARKIT LINEAGE' and id not in (select MapRuleItemID from #maps);
			
	--insert mapruleitemmapitem records
	insert into mapruleitemmapitem 
		(MapRuleItemID, MapItemID, [Owner])
		SELECT distinct M.MapRuleItemID, OM.MapItemID , 'MARKIT LINEAGE'
		FROM #maps M 
		inner join #objectmap OM on(M.ID = OM.MapID)
		where M.MapRuleItemID is not null and OM.MapItemID is not null;	

	declare @mapruleitemmapitemCount int;
	select @mapruleitemmapitemCount = count(1) from mapruleitemmapitem where [owner] = 'MARKIT LINEAGE'
	print 'Inserted [' + cast(@mapruleitemmapitemCount as varchar) + '] mapruleitemmapitem records';			

	declare @mapruleitemCount int;
	select @mapruleitemCount = count(1) from mapruleitem where [owner] = 'MARKIT LINEAGE'
	print 'Inserted [' + cast(@mapruleitemCount as varchar) + '] mapruleitem records';			

	declare @mapitemCount int;
	select @mapitemCount = count(1) from mapitem where [owner] = 'MARKIT LINEAGE'
	print 'Inserted [' + cast(@mapitemCount as varchar) + '] mapitem records';
			
end
GO

alter proc [dbo].[GetPageInformation]
--declare 
	@o varchar(50),-- = 'Artifact',
	@oid int,-- = 23450,
	@rid int --= 1
as
begin
	declare @breadcrumbsRaw table ([Level] int, [TypeName] nvarchar(500), [Name] nvarchar(max), [TypeUrl] nvarchar(2500), [Url] nvarchar(2500));
	declare @breadcrumbs table ([Name] nvarchar(max), [Url] nvarchar(2500), Active bit, IsType bit);

	with h as
		(
		select	A.ID,
				A.[ObjectID], 
				A.AssetTypeID,
				I.SubjectID as [ParentID], 
				0 as [Level]
		from	Asset A
				left join PredicateIntersect I on I.Object = A.Object and I.ObjectID = A.ObjectID and I.PredicateType = 3
		where	A.[Object] = @o and A.ObjectID = @oid
		union all
		select	P.ID,
				P.[ObjectID] as ID, 
				P.AssetTypeID,
				I.SubjectID as ParentID, 
				h.[Level]-1 as [Level]
		from	Asset P
				inner join h on P.[Object] = @o and P.ObjectID = h.ParentID
				outer apply (
							select	SubjectID
							from	PredicateIntersect 
							where	Object = P.Object 
									and ObjectID = P.ObjectID 
									and PredicateType = 3
							) I
		)

	insert into @breadcrumbsRaw
		select		distinct	
					[Level],
					ltrim(rtrim(T.Name)),
					ltrim(rtrim(D.DisplayValue)),
					UT.Url,
					U.Url
		from		h 
					inner join AssetType T on T.ID = h.AssetTypeID
					left join dbo.GetAssetDisplayValue() D on D.ID = h.ID
					cross apply dbo.GetAssetUrl(@o, T.ObjectID, h.ObjectID) U
					cross apply dbo.GetAssetUrl(T.Object, T.ObjectID, T.ObjectID) UT
		where		ltrim(rtrim(T.Name)) is not null
					and ltrim(rtrim(D.DisplayValue)) is not null
		order by	[Level]

	declare @max int = 0,
			@min int
	select	@min = min([Level]) from @breadcrumbsRaw

	insert into @breadcrumbs values ('Glossary', null, 0, 0)

	while @min <= @max
	begin
		insert into @breadcrumbs
			select	TypeName, TypeUrl, 0, 1 from @breadcrumbsRaw where [Level] = @min

		insert into @breadcrumbs
			select	Name, 
					Url, 
					case @min when 0 then 1 else 0 end, 
					0 
			from	@breadcrumbsRaw 
			where	[Level] = @min

		set @min = @min + 1
	end

	select	distinct
			A.ID,
			O.ID as AssetID,
			OD.DisplayValue,
			T.Name as [TypeName],
			case 
				when Dash.[Count] > 0 then cast(1 as bit)
				else cast(0 as bit)
			end as HasDashboards,
			case 
				when Work.[Count] > 0 then cast(1 as bit)
				else cast(0 as bit)
			end as HasWorkflow,
			case 
				when Child.[Count] > 0 then cast(1 as bit)
				else cast(0 as bit)
			end as HasChildArtifacts,
			case 
				when Attr.[Count] > 0 then cast(1 as bit)
				else cast(0 as bit)
			end as AllowAttributes,
			case 
				when Hier.[Count] > 0 then cast(1 as bit)
				else cast(0 as bit)
			end as AllowPredicateHierarchies,
			(
			select	*
			from	(
					select	P.ID as [ID],
							P.Name as [Name]
					from	[Predicate] P
					where	exists(SELECT * FROM IntersectType IT WHERE P.[type] = 6 and P.ID = IT.PredicateID and ((IT.Subject = T.Object and IT.SubjectID = T.ObjectID) OR (IT.Object = T.Object and IT.ObjectID =T.ObjectID)))
					union	
					select	P.ID as [ID], 
							P.Name as [Name] 
					from	[NymRelation] R 
							inner join [dbo].[predicate] P on P.ID = R.PredicateID where R.[Object] = T.Object and R.ObjectID = T.ObjectID
					) NMT
			for		json path
			)
			as NymTypes,
			(
			select	* 
			from	@breadcrumbs
			for		json path
			) as Breadcrumbs
	from	Artifact A 
			inner join Asset O on O.Object = @o and O.ObjectID = A.ID 
			inner join AssetType T on T.ID = O.AssetTypeID
			left join dbo.GetAssetDisplayValue() OD on OD.ID = O.ID
			--cross apply [dbo].GetAssetDisplayValueById(O.ID) as OD
			cross apply (
						select	count(1) as [Count]
						from	Report
						where	ObjectType = O.Object
								and ObjectID = T.ObjectID
						) Dash
			cross apply (
						select	count(1) as [Count]
						from	workflow.EventRegistration WER
								inner join workflow.Type WT on WER.TypeID = WT.ID and WT.PublishedVersionID is not null and WT.[State] = 1 and WER.ChangeType = 8 --ACTIVE
						where	WER.Object = T.Object
								and WER.ObjectID = T.ObjectID
						) Work
			cross apply (
						select	count(1) as [Count]
						from	[PredicateIntersect]
						where	Subject = O.Object
								and SubjectID = O.ObjectID
								and PredicateType = 3
						) Child
			cross apply (
						select	count(1) as [Count]
						from	AttributeTypeRelation
						where	ObjectType = T.Object and ObjectID = T.ObjectID
						) Attr
			cross apply (
						select	count(1) as [Count]
						from	IntersectType IT
								inner join [Predicate] P on P.ID = IT.PredicateID and P.[Type] = 3 -- TypeOf
						where	((IT.Subject = T.Object and IT.SubjectID = T.ObjectID) OR (IT.Object = T.Object and IT.ObjectID = T.ObjectID))
						) Hier
	where   A.ID = @oid 
			and A.[Visible] = 1 
			and A.ID not in (select AssetID from cache.NoRead where ResourceID = @rid)
	for json path, WITHOUT_ARRAY_WRAPPER
end
GO

ALTER PROCEDURE [dbo].[GetRenderedTemplateBodyNg]-- 'Tooltip', 'Resource', 2, 'Preview'
--declare
	@TemplateType varchar(25),
	@Type varchar(50),
	@ID int,
	@Action varchar(50),
	@SubjectName VARCHAR (200) = 'Governing Domain',
	@resourceId int = -1
--set @TemplateType = 'Lookup'
--set @Type = 'Artifact'
--set @ID = 7004--16435
--set @Action = 'Preview'--'Certificate'
AS
BEGIN
	SET NOCOUNT ON;

	declare @html nvarchar(max),
			@link nvarchar(2500),
			@icon nvarchar(250),
			@hasDynamicFields bit = 0,
			@hasStats bit = 0,
			@typeID int,

			@showIcon bit = 1,

			@current int,
			@max int,
			@name nvarchar(250),
			@value nvarchar(max);

	declare @tbl table (ID int identity, Name nvarchar(250), Value nvarchar(max));

	if @TemplateType = 'Tooltip'
	begin
		select	@html = TemplateBody
		from	TooltipTemplate
		where	Name = @Type
				and [Action] = @Action
	end

	-- Get the static tokens, depending on the type.
	declare @n nvarchar(250), @t nvarchar(250), @s nvarchar(25), @v int, @dc datetime, @du datetime, @d nvarchar(4000);
		
	-- Get common fields
	select	@typeID = C_D.ObjectTypeID,
			@icon = '<div title=''' + C_D.Name + ''' class=''tooltip-icon'' style=''background-color: ' + C_D.IconBackColor + '; color: ' + C_D.IconForeColor + '''><i class=''fa fa-' + C_D.IconText + '''></i></div>',
			@n = C_D.Name,
			@t = C_D.ObjectTypeName,
			@d = f.formattedvalue,
			@link = C_D.Url
	from	cache.objectdetails C_D			
			left join fieldtype ft on (ft.[object] = C_D.[objecttype] and ft.objectid = C_D.objecttypeid and ft.name = 'Description')
			left join field f on (f.fieldtypeid = ft.id and f.[objecttype] = C_D.[object] and f.objectid = C_D.objectid)
	where	C_D.[Object] = @Type
			and C_D.ObjectID = @ID;

	--fusion attributes arent in cache
	if @Type = 'FusionAttribute'
	begin		
		select 
			@typeID = fa.fusionattributetypeid,
			@n = fa.name,
			@t = fat.Name,
			@link = dbo.GenerateNgObjectUrl('FusionAttribute', fat.id, fa.id) 
		from fusionattribute fa 
			inner join fusionattributetype fat on (fa.fusionattributetypeid = fat.id) 
		where fa.id = @ID
	end

	if @n is not null
	begin
		if @link is null
		begin
			insert into @tbl values ('Name', @n)
		end
		else
		begin
			insert into @tbl values ('Name', '<a routerLink="/' + @link + '">' + @n + '</a>')
		end
		insert into @tbl values ('Description', @d)
	end
	insert into @tbl values ('Type', @t)

	if @Action = 'AssigningItemPreview'
	begin
		set @html = '<h3>{Name}</h3>'
	end

	
	if @Action = 'LookupPreview'
	begin
		set @html = '{Items}'
		
		if @Type = 'FusionAttribute'
		begin
			-- BUILD LIST HTML -----------------------------------------
			declare @fusionAttributeItemsHtml nvarchar(max)

			set @fusionAttributeItemsHtml = '<div style="height: 200px; overflow-y: scroll"><table class="hoverable bordered striped" style="width:100%"><thead>'
			set @fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '<th style="margin-right: 15px">Name</th>'
			set @fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '</thead><tbody>'

			select		--top 10 
						@fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '<tr>' 
											+ '<td>' + Name + '</td>'
											+ '</tr>'
			from		FusionAttribute
			where		ParentID = @ID
			order by	Name asc

			set @fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '</tbody>'
			set @fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '</table></div>'
 
			insert into @tbl values ('Items', @fusionAttributeItemsHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'LookupType' OR @Type = 'Lookup'
		begin
			-- BUILD LOOKUP LIST HTML -----------------------------------------
			declare @lookups table (RowID int identity, ID int)

			declare @MyLookupTypeID int
			if @Type = 'Lookup'
				begin
					select @MyLookupTypeID = LookupTypeID from [Lookup] where ID = @ID 
				end
			else
				begin
					set @MyLookupTypeID = @ID
				end

			insert into @lookups 
				select top 10 ID from [Lookup] where LookupTypeID = @MyLookupTypeID order by ID desc
		
			declare @lookupFieldTypes table (ID int identity, Name nvarchar(250))
			insert into @lookupFieldTypes
				select FriendlyName from FieldType where [Object] = 'LookupType' and ObjectID = @MyLookupTypeID order by SortOrder asc

			declare @lookupHtml nvarchar(max)

			set @lookupHtml = '<table class="hoverable bordered striped" style="width:100%">'

			-- Loop through field name list ---------
			set @lookupHtml = @lookupHtml + '<thead>'
			set		@current = 1
			select	@max = max(ID) from @lookupFieldTypes
			while @current <= @max
			begin
				select	@name = Name
				from	@lookupFieldTypes
				where	ID = @current

				set @lookupHtml = @lookupHtml + '<th style="margin-right: 15px">' + @name  + '</th>'

				set @current = @current + 1
			end
			set @lookupHtml = @lookupHtml + '</thead>'
			-----------------------------------------

			set @lookupHtml = @lookupHtml + '<tbody>'

			-- Loop through event list --------------
			select	@current = min(RowID) from @lookups
			select	@max = max(RowID) from @lookups

			while @current <= @max
			begin
				set @lookupHtml = @lookupHtml + '<tr>'	-- Open row for selected event.

				declare @lookupFields table (Name nvarchar(250), Value nvarchar(4000))
			
				declare @lookupID int

				select	@lookupID = ID from @lookups where RowID = @current

				insert into @lookupFields
					select		FriendlyName,
								FormattedValue
					from		FieldWithRelation
					where		ObjectType = 'Lookup' 
								and ObjectID = @lookupID

					-- Loop through each field for this selected event --
					declare @lfCurrent int,
							@lfMax int
					set		@lfCurrent = 1
					select	@lfMax = max(ID) from @lookupFieldTypes
					while @lfCurrent <= @lfMax
					begin
						select	@name = Name from @lookupFieldTypes where ID = @lfCurrent

						select @lookupHtml = @lookupHtml + '<td>' + coalesce(Value, '') + '</td>' from @lookupFields where Name = @name

						set @lfCurrent = @lfCurrent + 1
					end
					-----------------------------------------------------

				delete @lookupFields

				set @lookupHtml = @lookupHtml + '</tr>'	-- Close off row for selected lookup.

				set @current = @current + 1
			end
			-----------------------------------------

			set @lookupHtml = @lookupHtml + '</tbody>'

			set @lookupHtml = @lookupHtml + '</table>'

			insert into @tbl values ('Items', @lookupHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'ReferenceItemType' OR @Type = 'ReferenceItem'
		begin
			-- BUILD LOOKUP LIST HTML -----------------------------------------
			declare @refs table (RowID int identity, ID int)
			declare @isHierarchy bit = 0;

			declare @MyRefTypeID int
			if @Type = 'ReferenceItem'
				begin
					select @MyRefTypeID = ReferenceItemTypeID from ReferenceItem where ID = @ID 

					-- check if this item is in a hierarchy if so set the flag as true
					select  @isHierarchy = count(1) from intersecttypedetail where [object] = 'ReferenceItemType' and [objectid] = @MyRefTypeID and predicatetype = 3
				end
			else
				begin
					set @MyRefTypeID = @ID
				end

			if @isHierarchy = 1 
				begin
				insert into @refs 					
					select	top 500 
							ri.ID 
					from	[ReferenceItem] ri
							inner join Asset ast on (ri.id = ast.objectid and ast.[object] = 'ReferenceItem')
							inner join [intersect] id on (id.objectid = ri.id and id.[object] = 'ReferenceItem')
							inner join [intersect] id_2 on (id_2.[object] = 'ReferenceItem' and id_2.[objectid] = @id and id_2.subjectid = id.subjectid)
							inner join [intersecttypedetail] it on (it.id = id.intersecttypeid and it.id = id_2.intersecttypeid and it.[object]='ReferenceItemType' and it.predicatetype = 3)
					where	ri.ReferenceItemTypeID = @MyRefTypeID 
							and ast.[State] = 1 
							and ast.ID not in (select AssetID from cache.NoRead where ResourceID = @resourceId)
					order by DisplayValue asc
				end
			else
				begin
				insert into @refs 
					select	top 500 
							ri.ID 
					from	[ReferenceItem] ri
							inner join Asset ast on (ri.id = ast.objectid and ast.[object] = 'ReferenceItem')
					where	ri.ReferenceItemTypeID = @MyRefTypeID 
							and ast.[State] = 1 
							and ast.ID not in (select AssetID from cache.NoRead where ResourceID = @resourceId)
					order by DisplayValue asc
				end
		
			declare @refFieldTypes table (ID int identity, Name nvarchar(250))
			insert into @refFieldTypes values ('Code')
			insert into @refFieldTypes
				select FriendlyName from FieldType where [Object] = 'ReferenceItemType' and ObjectID = @MyRefTypeID order by SortOrder asc

			declare @refHtml nvarchar(max)

			set @refHtml = '<table class="hoverable bordered striped" style="width:100%; min-width: 400px">'

			-- Loop through field name list ---------
			set @refHtml = @refHtml + '<thead>'
			set		@current = 1
			select	@max = max(ID) from @refFieldTypes
			while @current <= @max
			begin
				select	@name = Name
				from	@refFieldTypes
				where	ID = @current

				set @refHtml = @refHtml + '<th style="margin-right: 15px">' + @name  + '</th>'

				set @current = @current + 1
			end
			set @refHtml = @refHtml + '</thead>'
			-----------------------------------------

			set @refHtml = @refHtml + '<tbody>'

			-- Loop through event list --------------
			select	@current = min(RowID) from @refs
			select	@max = max(RowID) from @refs

			while @current <= @max
			begin
				set @refHtml = @refHtml + '<tr>'	-- Open row for selected event.

				declare @refFields table (Name nvarchar(250), Value nvarchar(4000))
			
				declare @refID int

				select	@refID = ID from @refs where RowID = @current

				insert into @refFields
					select	'Code', Code from ReferenceItem where ID = @refID

				insert into @refFields
					select		FriendlyName,
								FormattedValue
					from		FieldWithRelation
					where		ObjectType = 'ReferenceItem' 
								and ObjectID = @refID

					-- Loop through each field for this selected event --
					declare @rfCurrent int,
							@rfMax int,
							@rfCurrentVal nvarchar(max);

					set		@rfCurrent = 1
					select	@rfMax = max(ID) from @refFieldTypes
					while @rfCurrent <= @rfMax
					begin
						select	@name = Name from @refFieldTypes where ID = @rfCurrent

						if exists (select 1 from @refFields where Name = @name)
						begin
							select @refHtml = @refHtml + '<td>' + coalesce(Value, '1') + '</td>' from @refFields where Name = @name;
						end
						else
						begin
							set @refHtml = @refHtml + '<td>&nbsp;</td>';
						end

						set @rfCurrent = @rfCurrent + 1
					end
					-----------------------------------------------------

				delete @refFields

				set @refHtml = @refHtml + '</tr>'	-- Close off row for selected lookup.

				set @current = @current + 1
			end
						
			-----------------------------------------

			set @refHtml = @refHtml + '</tbody>'

			set @refHtml = @refHtml + '</table>'

			if @max >= 500
			begin
				set @refHtml = @refHtml + '<div style="font-weight:bold;padding-top:10px">Showing top 500 items</div>'	
			end

			insert into @tbl values ('Items', @refHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'Resource' OR @Type = 'ResourceType'
		begin
			-- BUILD Resource LIST HTML -----------------------------------------
			declare @resourceItemsHtml nvarchar(max)

			set @resourceItemsHtml = '<table class="hoverable bordered striped" style="width:100%"><thead>'
			set @resourceItemsHtml = @resourceItemsHtml + '<th style="margin-right: 15px">First Name</th><th style="margin-right: 15px">Last Name</th><th>Email</th>'
			set @resourceItemsHtml = @resourceItemsHtml + '</thead><tbody>'

			select		top 10 
						@resourceItemsHtml = @resourceItemsHtml + '<tr>' + 
											'<td>' + FirstName + '</td>' + 
											'<td>' + LastName + '</td>' + 
											'<td>' + Email + '</td>'
											+ '</tr>'
			from		reporting.Global_Resource
			order by	LastName, FirstName asc

			set @resourceItemsHtml = @resourceItemsHtml + '</tbody>'
			set @resourceItemsHtml = @resourceItemsHtml + '</table>'
 
			insert into @tbl values ('Items', @resourceItemsHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'ReferenceItem'
		begin

			declare @myReferenceListID int

			select	@myReferenceListID = ReferenceItemTypeID from ReferenceItem where ID = @ID
			-- BUILD LIST HTML -----------------------------------------
			declare @referenceItemHtml nvarchar(max)

			set @referenceItemHtml = '<table class="hoverable bordered striped" style="width:100%">'
			set @referenceItemHtml = @referenceItemHtml + '<thead><th style="margin-right: 15px">Name</th></thead>'
			set @referenceItemHtml = @referenceItemHtml + '<tbody>'



			select		top 10 
						@referenceItemHtml = @referenceItemHtml + '<tr>' + '<td>' + DisplayValue + '</td>' + '</tr>'             
			from		ReferenceItem
			where		ReferenceItemTypeID = @myReferenceListID
			order by	DisplayValue desc

			set @referenceItemHtml = @referenceItemHtml + '</tbody>'
			set @referenceItemHtml = @referenceItemHtml + '</table>'
 
			insert into @tbl values ('Items', @referenceItemHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'ReferenceItemType'
		begin

		--	declare @myReferenceListID int

			--select	@myReferenceListID = ReferenceItemTypeID from ReferenceItem where ID = @ID
			-- BUILD LIST HTML -----------------------------------------
			declare @referenceItemTypeHtml nvarchar(max)

			set @referenceItemTypeHtml = '<table class="hoverable bordered striped" style="width:100%">'
			set @referenceItemTypeHtml = @referenceItemTypeHtml + '<thead><th style="margin-right: 15px">Display Value</th></thead>'
			set @referenceItemTypeHtml = @referenceItemTypeHtml + '<tbody>'



			select		top 10 
						@referenceItemTypeHtml = @referenceItemTypeHtml + '<tr>' + '<td>' + DisplayValue + '</td>' + '</tr>'             
			from		ReferenceItem
			where		ReferenceItemTypeID = @ID
			order by	DisplayValue desc

			set @referenceItemTypeHtml = @referenceItemTypeHtml + '</tbody>'
			set @referenceItemTypeHtml = @referenceItemTypeHtml + '</table>'
 
			insert into @tbl values ('Items', @referenceItemTypeHtml)
			------------------------------------------------------------------
		end;

	end
	
	if @Action = 'None'
	begin
		set @html = '<h3>{Name}</h3><div>'
	end

	if @Action = 'Preview'
	begin
		set @html = '<h3 style="positon: relative">{Name} <small style="background-color: #fff; float:right;font-size:65%;">{Type}</small></h3><div>{Description}</div>'
		set @showIcon = 0

		if @Type = 'Artifact'
		begin
			declare @artifactPathHtml nvarchar(2500) = '<table>';
			declare @artLevelResult table(ID int identity, LevelName nvarchar(250), DisplayValue nvarchar(250), Url varchar(1000));

			with ap as (
				select	O.ID,
						O.ParentID,
						'INVALID' as DisplayValue,
						L.Name as LevelName,
						dbo.GenerateNgObjectUrl('Artifact', L.ID, O.ID) as Url,
						1 as [Level]
				from	Artifact O
						inner join ArtifactType L on L.ID = O.ArtifactTypeID
				where	O.ID = @ID
				union all
				select	O.ID,
						O.ParentID,
						'INVALID' as DisplayValue,
						L.Name as LevelName,
						dbo.GenerateNgObjectUrl('Artifact', L.ID, O.ID) as Url,
						C.[Level] + 1 as [Level]
				from	Artifact O
						inner join ArtifactType L on L.ID = O.ArtifactTypeID
						inner join ap as C on C.ParentID = O.ID
			)

			insert into @artLevelResult
				select LevelName, DisplayValue, Url from ap order by [Level] desc

			select		@artifactPathHtml = coalesce(@artifactPathHtml + '', '') + '<tr><td style="width: 15px">' +  cast([ID] as varchar) + '</td><td>' +  LevelName + '</td><td><b><a href="' + Url + '">' + DisplayValue + '</a></b>' + '</td></tr>'
			from		@artLevelResult
			
			set @artifactPathHtml =  @artifactPathHtml + '</table>'

			set @html = @html + '<div><b>Path:</b></div><div>' + coalesce(@artifactPathHtml,'') + '</div>'

			set @hasDynamicFields = 1
		end;

		if @Type = 'FusionAttribute'
		begin
			declare @faPathHtml nvarchar(2500) = '<table>';
			declare @faLevelResult table(ID int identity, LevelName nvarchar(250), Name nvarchar(250));

			with fap as (
				select	O.ID,
						O.ParentID,
						O.Name,
						L.Name as LevelName,
						1 as [Level]
				from	FusionAttribute O
						inner join FusionAttributeType L on L.ID = O.FusionAttributeTypeID
				where	O.ID = @ID
				union all
				select	O.ID,
						O.ParentID,
						O.Name,
						L.Name as LevelName,
						C.[Level] + 1 as [Level]
				from	FusionAttribute O
						inner join FusionAttributeType L on L.ID = O.FusionAttributeTypeID
						inner join fap as C on C.ParentID = O.ID
			)

			insert into @faLevelResult
				select LevelName, Name from fap order by [Level] desc

			select		@faPathHtml = @faPathHtml + 
						'<tr><td colspan="2">Configuration</td><td><b><a href="/fusion/' + cast(F.ID as nvarchar) + '">' + coalesce(F.Name,'') + '</a></b></td></tr>' 
			from		Fusion F 
						inner join FusionAttribute A on A.FusionID = F.ID and A.ID = @ID

			select		@faPathHtml = coalesce(@faPathHtml + '', '') + '<tr><td style="width: 15px">' +  cast(ID as varchar) + '</td><td>' +  LevelName + '</td><td><b>' + Name + '</b>' + '</td></tr>'
			from		@faLevelResult

			set @faPathHtml =  @faPathHtml + '</table>'

			set @html = @html + '<div><b>Path:</b></div><div>' + coalesce(@faPathHtml,'') + '</div>'

			set @hasDynamicFields = 1
		end

		if @Type = 'Intersect'
		begin
			set @hasDynamicFields = 1
		end;

		
		if @Type = 'Issue'
		begin
			insert into @tbl values('Name', '')
			insert into @tbl values('Description', '')
					
			if exists (select id from issue where id = @ID)
			begin			
				set @html = @html + '<div><b>Issue Type:</b> {IssueType}</div>'
				set @html = @html + '<div><b>Criticality:</b> {Criticality}</div>'
						
				insert into @tbl 
					select 'IssueType', it.name 
					from issuetype it inner join issue i on(i.issuetypeid = it.id) 
					where i.id = @ID

				insert into @tbl 
					select 'Criticality', case when i.Criticality = 0 then 'Negligible' when i.Criticality = 1 then 'Low' when i.Criticality = 2 then 'Medium' when i.Criticality = 3 then 'High'  when i.Criticality = 4 then 'Critical' else 'N/A' end
					from issuetype it inner join issue i on(i.issuetypeid = it.id) 
					where i.id = @ID

				set @hasDynamicFields = 1
			end			
		end;

		if @Type = 'Resource'
		begin
			--declare @e nvarchar(500), @fn nvarchar(250), @ln nvarchar(250)
			--select	@e = Email, @fn = FirstName, @ln = LastName
			--from	reporting.Global_Resource
			--where	ResourceID = @ID

			--insert into @tbl values ('Email', @e)
			--insert into @tbl values ('FirstName', @fn)
			--insert into @tbl values ('LastName', @ln)
			--insert into @tbl values ('Role', '')

			--set @html = @html + '<div><b>Email:</b> {Email}</div>'
			--set @html = @html + '<div><b>First Name:</b> {FirstName}</div>'
			--set @html = @html + '<div><b>Last Name:</b> {LastName}</div>'

			set @hasDynamicFields = 1
		end;

		if @Type = 'Rule'
		begin
			insert into @tbl
				select	'Name', DisplayValue
				from	[Rule] O
				where	ID = @ID

			set @hasDynamicFields = 1
		end;

		if @Type = 'RuleDimension'
		begin
			insert into @tbl
				select	'Description', [Description]
				from	RuleDimension
				where	ID = @ID
			insert into @tbl
				select	'Name', [Name]
				from	RuleDimension
				where	ID = @ID

			--set @html = @html + '<div><b>Path:</b> {Description}</div>'
						
		end;

		if @Type = 'Taxonomy'
		begin
			declare @taxonomyPathHtml nvarchar(2500) = '<table>';

			with tp as (
				select	O.ID,
						O.ParentID,
						O.DisplayValue,
						coalesce(L.Name, 'Level ' + cast(O.[Level] as varchar)) as LevelName,
						O.[Level]
				from	[Taxonomy] O
						left join TaxonomyTypeLevel L on L.TaxonomyTypeID = O.TaxonomyTypeID and L.[Level] = O.[Level]
				where	O.ID = @ID
				union all
				select	O.ID,
						O.ParentID,
						O.DisplayValue,
						coalesce(L.Name, 'Level ' + cast(O.[Level] as varchar)) as LevelName,
						O.[Level]
				from	[Taxonomy] O
						outer apply (
									select Name from TaxonomyTypeLevel where TaxonomyTypeID = O.TaxonomyTypeID and [Level] = O.[Level]
									) L
						--left join TaxonomyTypeLevel L on L.TaxonomyTypeID = O.TaxonomyTypeID and L.[Level] = O.[Level]
						inner join tp as C on C.ParentID = O.ID
			)

			select		@taxonomyPathHtml = coalesce(@taxonomyPathHtml + '', '') + '<tr><td style="width: 15px">' +  cast([Level] as varchar) + '</td><td>' +  LevelName + '</td><td><b>' + DisplayValue + '</b>' + '</td></tr>'
			from		tp
			order by	[Level]
			
			set @taxonomyPathHtml =  @taxonomyPathHtml + '</table>'

			set @html = @html + '<div><b>Path:</b></div><div>' + coalesce(@taxonomyPathHtml,'') + '</div>'

			set @hasDynamicFields = 1
		end;

		if @Type = 'TaxonomyType'
		begin
			insert into @tbl
				select	'Name', Name
				from	TaxonomyType O
				where	ID = @ID

			set @hasDynamicFields = 1
		end;
		
		-- If required, get dynamic fields to add to list.
		if @hasDynamicFields = 1
		begin
			select	@html = @html + '<div><b>' + FriendlyName + '</b>: ' + '{' + Name + '}' + '</div>' 
			from	FieldWithRelation
			where	ObjectType = @Type
					and ObjectID = @ID
					and Name not in (select Name from @tbl)

			insert into @tbl
				select	Name,
						FormattedValue
				from	FieldWithRelation
				where	ObjectType = @Type
						and ObjectID = @ID
						and Name not in (select Name from @tbl)
		end;
	end

	if @Action = 'Statistics'
	begin
		set @html = '<h3>{Name}</h3><div>{Statistics}</div>'

		set @hasStats = case @Type
							when 'Artifact' then 1
							when 'Taxonomy' then 1
							else 0
						end

		-- If required, build statistics table
		if @hasStats = 1
		begin
			-- BUILD STATS LIST HTML -----------------------------------------
			declare @statsHtml nvarchar(max)

			declare @stats table (ID int identity, Name nvarchar(250), Score bit)

			insert into @stats 
				select		G.Name + ': ' + I.Name,
							MR.Value
				from		metrics.Score S
							inner join metrics.MapResult MR on MR.ScoreID = S.ID and S.EffectiveEndDate = '12/31/9999' and S.Object = @Type and S.ObjectID = @ID
							inner join metrics.Map M on M.ID = MR.MapID
							inner join metrics.[Group] G on G.ID = M.GroupID
							inner join metrics.Item I on I.ID = M.ItemID
				order by	G.Name + ': ' + I.Name

			set @statsHtml = '<table class="hoverable bordered striped" style="width:100%">'

			-- Loop through field name list ---------
			set @statsHtml = @statsHtml + '<tbody>'
			set		@current = 1
			select	@max = max(ID) from @stats
			while @current <= @max
			begin
				select	@statsHtml = @statsHtml + '<tr><td>' + Name  + '</td>' + '<td>' + case when Score = 1 then 'Pass' else 'Fail' end  + ' </td></tr>'
				from	@stats
				where	ID = @current

				set @current = @current + 1
			end
			set @statsHtml = @statsHtml + '</tbody>'
			-----------------------------------------

			insert into @tbl values ('Statistics', @statsHtml)

			------------------------------------------------------------------
		end;
	end

	if exists (select 1 from cache.NoRead arp where arp.resourceid = @resourceId and arp.[object] = @Type and arp.objectid = @ID)
	begin
		set @html = 'This item either does not exist or you do not have access to its details.';

		-- Return the properly formatted values.
		select	'' as Title,
				@html as Body;
	end
	else
	begin
		-- Replace the fields in the template with the appropriate text value.
		set		@current = 1
		select	@max = max(ID) from @tbl

		while @current <= @max
		begin
			select	@name = '{' + Name + '}',
					@value = COALESCE(Value, '')
			from	@tbl 
			where	ID = @current

			if @showIcon = 1
			begin
				if @name = '{Name}' and @icon is not null
				begin
					update	@tbl 
					set		Value = '<div class="pull-left" style="width: 30px">' + @icon + '</div>' + '<div class="pull-right">' + @value + '</div>'
					where	ID = @current
					--set @usedIconAlready = 1
				end
			end

			set @html = REPLACE(@html, @name, @value)

			set @current = @current + 1
		end

		--if @showIcon = 1 and @icon is not null
		--begin
		--	set @html = @icon + '<br/>' + @html
		--end

		set @html = '<div style="max-height: 500px; min-width: 400px; overflow-y: auto">' + @html + '</div>'

		-- Return the properly formatted values.
		select	'' as Title,
				@html as Body;
	end
END
GO

ALTER TABLE [dbo].[Field] SET ( SYSTEM_VERSIONING = OFF  )
alter table Field add ID bigint identity(1,1) not null
alter table Field_History add ID bigint null

update	T
set		T.ID = S.ID
from	Field_History T
		inner join Field S on S.FieldTypeID = T.FieldTypeID and S.AssetID = T.AssetID

update	T
set		T.ID = S.ID--,
		--T.AssetID = S.AssetID
from	Field_History T
		inner join Field S on S.AssetID = T.AssetID and S.ID is not null and T.ID is null --S.ObjectType = T.ObjectType and S.ObjectID = T.ObjectID and T.ID is null  --S.FieldTypeID = T.FieldTypeID and 

update Field_History set ID = 0 where ID is null
select count(1) from Field_History where ID is null


alter table Field_History alter column ID bigint not null
GO
ALTER TABLE [dbo].[Field] SET ( SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[Field_History], DATA_CONSISTENCY_CHECK = ON)  )
GO



CREATE UNIQUE INDEX UQ_Field_ID ON dbo.Field(ID);  
GO
CREATE FULLTEXT INDEX ON dbo.Field (Value, FormattedValue) KEY INDEX UQ_Field_ID on FieldCatalog; --KEY INDEX CIX_Field WITH STOPLIST = SYSTEM;  
GO 

drop view [dbo].[AssetWithoutReadPermission]
go

drop view [dbo].[StatisticTypeCheckOption]
GO

ALTER PROCEDURE [dbo].[GetReferenceItemValues]	
	@listid int,
	@resourceID int	= 0,
	@useApiName bit = 0
AS
BEGIN
	SET NOCOUNT ON;
	
	create table #fieldtypes (ID int, Name nvarchar(250))
	create table #parentTypes (IntersectTypeID int, Name nvarchar(250), ReferenceListTypeID int, ParentLevel int)

	-- load the fields for this item
	if @useApiName = 1
		begin
			insert into #fieldtypes
				select ID, [Name] from fieldtype where object = 'ReferenceItemType' and objectid = @listid order by ColumnOrder
		end
	else
		begin
			insert into #fieldtypes
				select ID, 'Field' + cast(id as varchar(100)) as [Name] from fieldtype where object = 'ReferenceItemType' and objectid = @listid order by ColumnOrder
		end

	declare @parentLevel int = 0;
	declare @currentReferenceListID int = @listid;	
	-- load the parents for this reference item type
	while exists (select 1 from intersecttypedetail where [object] = 'ReferenceItemType' and objectid = @currentReferenceListID and predicatetype = 3 and @parentLevel < 20)
	begin
		-- need to loop through parent / child relations till we get to the lowest one or loop to many times
		insert into #parentTypes 
			select id, subjectname, subjectid, @parentLevel from intersecttypedetail where [object] = 'ReferenceItemType' and objectid = @currentReferenceListID and predicatetype = 3;

		select @currentReferenceListID =subjectid from intersecttypedetail where [object] = 'ReferenceItemType' and objectid = @currentReferenceListID and predicatetype = 3;
		
		set @parentLevel = @parentLevel +1;
	end
	
	
	DECLARE @tsqlSelect nvarchar(max);
	DECLARE @tsqlFrom nvarchar(max);
	DECLARE @tsqlWhere nvarchar(max);
	DECLARE @tsql nvarchar(max);

	set @tsqlSelect = 'select ri.id as [ID] ,ri.code as [Code],o.id as [AssetID]';
	set @tsqlFrom = ' from [dbo].[referenceitem] ri  inner join Asset O on O.Object = ''ReferenceItem'' and O.ObjectID = ri.ID ';
	set @tsqlWhere = ' where ri.visible = 1 and ri.referenceitemtypeid = ' + cast(@listid as nvarchar(20));
	if @resourceID > 0
	begin
		set @tsqlFrom = @tsqlFrom  + ' left join cache.NoRead RP on RP.ResourceID = ' +  cast(@resourceID as varchar) + ' and RP.AssetID = O.ID ';
		set @tsqlWhere = @tsqlWhere + ' and RP.AssetID is null ';
	end	

	DECLARE @name nvarchar(250);
	DECLARE @id int = 0;
	DECLARE @intersectTypeId int;
	DECLARE @parentName nvarchar(250);
	DECLARE @parentListTypeID int = 0;	
	DECLARE @index int = 0;
	DECLARE @previousRelation varchar(200) = 'ri.ID';

	-- generate dynamic sql for each relationship
	DECLARE relCur CURSOR FOR SELECT IntersectTypeId, Name, ReferenceListTypeID, ParentLevel FROM #parentTypes
	OPEN relCur

	FETCH NEXT FROM relCur INTO @intersectTypeId, @parentName, @parentListTypeID, @parentLevel

	WHILE @@FETCH_STATUS = 0 BEGIN
	
		SET @tsqlSelect = @tsqlSelect + ',REL_' + cast(@index as nvarchar(10)) + '.DisplayValue as [Rel' + cast(@parentListTypeID as varchar(20)) + ']';
        SET @tsqlFrom = @tsqlFrom +' outer apply (
				    select	ID.DisplayValue, I.SubjectID                            
				    from	[PredicateIntersect] I
                            inner join Asset IA on I.Object = ''ReferenceItem'' and I.ObjectID = ' + @previousRelation + ' and IA.Object = ''ReferenceItem'' and IA.ObjectID = I.SubjectID and I.PredicateType = 3
                            inner join AssetType IAT on IAT.ID = IA.AssetTypeID
                            cross apply dbo.GetAssetDisplayValueById(IA.ID) ID
				    ) REL_' + cast(@index as nvarchar(10));

		set @previousRelation = 'REL_' + cast(@index as nvarchar(10)) + '.SubjectID';
		SET @index = @index + 1;
		FETCH NEXT FROM relCur INTO @intersectTypeId, @parentName, @parentListTypeID, @parentLevel
	END

	CLOSE relCur    
	DEALLOCATE relCur

	set @index = 0;
	-- generate dynamic sql for each field
	DECLARE cur CURSOR FOR SELECT id, name FROM #fieldtypes
	OPEN cur

	FETCH NEXT FROM cur INTO @id, @name

	WHILE @@FETCH_STATUS = 0 BEGIN
		
		SET @tsqlSelect = @tsqlSelect + ',f'+ cast(@index as nvarchar(10)) + '.formattedvalue as [' + @name + ']';
		SET @tsqlFrom = @tsqlFrom + ' left outer join [dbo].[field] f' + cast(@index as nvarchar(10)) + ' on (ri.id = f' + cast(@index as nvarchar(10)) + '.objectid and f' + cast(@index as nvarchar(10)) + '.[objecttype] = ''ReferenceItem'' and f' + cast(@index as nvarchar(10)) + '.fieldtypeid = ' + cast(@id as nvarchar(20)) + ')';

		SET @index = @index + 1;
		FETCH NEXT FROM cur INTO @id, @name
	END

	CLOSE cur    
	DEALLOCATE cur

	SET @tsql = @tsqlSelect + @tsqlFrom + @tsqlWhere;
	print @tsql
	EXEC sp_executesql @tsql;

END
GO