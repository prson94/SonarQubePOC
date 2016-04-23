CREATE FUNCTION [utility].[GetDirectlyAssignedResponsibilityList]
(
	@Object varchar(50),
	@ObjectID int,
	@Priority int
)
RETURNS 
@tbl TABLE 
(
	[Source] varchar(50), 
	Visible bit,
	ResponsibilityID int,
	ResponsibilityTypeID int,
	AssigningItem varchar(50),
	AssigningItemID int,
	[Object] varchar(50),
	ObjectID int,
	ContextHash varchar(50),
	[Priority] int
)
AS
BEGIN

	if @Object = 'Artifact'
		begin
			insert into @tbl
				select	'Artifact Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						T.ID as AssigningItemID,
						@Object as ObjectType,
						T.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	Artifact T 
						inner join Responsibility R on R.ObjectType = @Object and R.ObjectID = T.ID 
							and (
									(T.ID = @ObjectID and @ObjectID is not null)
									or (@ObjectID is null)
								);
		end
	if @Object = 'ArtifactType'
		begin
			insert into @tbl
				select	'Artifact Type Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						T.ID as AssigningItemID,
						@Object as ObjectType,
						T.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	ArtifactType T 
						inner join Responsibility R on R.ObjectType = @Object and R.ObjectID = T.ID 
							and (
									(T.ID = @ObjectID and @ObjectID is not null)
									or (@ObjectID is null)
								);
		end
	if @Object = 'Domain'
		begin
			insert into @tbl
				select	'Domain Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						T.ID as AssigningItemID,
						@Object as ObjectType,
						T.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	Domain T 
						inner join Responsibility R on R.ObjectType = @Object and R.ObjectID = T.ID
							and (
									(T.ID = @ObjectID and @ObjectID is not null)
									or (@ObjectID is null)
								);
		end
	if @Object = 'DomainType'
		begin
			insert into @tbl
				select	'Domain Type Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						T.ID as AssigningItemID,
						@Object as ObjectType,
						T.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	DomainType T 
						inner join Responsibility R on R.ObjectType = @Object and R.ObjectID = T.ID
							and (
									(T.ID = @ObjectID and @ObjectID is not null)
									or (@ObjectID is null)
								);
		end
	if @Object = 'Fusion'
		begin
			insert into @tbl
				select	'Fusion Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						T.ID as AssigningItemID,
						@Object as ObjectType,
						T.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	Fusion T 
						inner join Responsibility R on R.ObjectType = @Object and R.ObjectID = T.ID
							and (
									(T.ID = @ObjectID and @ObjectID is not null)
									or (@ObjectID is null)
								);
		end
	if @Object = 'FusionType'
		begin
			insert into @tbl
				select	'Fusion Type Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						T.ID as AssigningItemID,
						@Object as ObjectType,
						T.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	FusionType T 
						inner join Responsibility R on R.ObjectType = @Object and R.ObjectID = T.ID
							and (
									(T.ID = @ObjectID and @ObjectID is not null)
									or (@ObjectID is null)
								);
		end
	if @Object = 'Rule'
		begin
			insert into @tbl
				select	'Rule Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						RU.ID as AssigningItemID,
						@Object as ObjectType,
						RU.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	[Rule] RU 
						inner join Responsibility R on R.ObjectType = @Object and R.ObjectID = RU.ID
							and (
								(RU.ID = @ObjectID and @ObjectID is not null) or (@ObjectID is null)
								);
		end
	if @Object = 'RuleType'
		begin
			insert into @tbl
				select	'Rule Type Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						@ObjectID as AssigningItemID,
						@Object as ObjectType,
						@ObjectID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	Responsibility R where R.ObjectType = @Object and R.ObjectID = @ObjectID;				
		end
	if @Object = 'Taxonomy'
		begin
			insert into @tbl
				select	'Taxonomy Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						T.ID as AssigningItemID,
						@Object as ObjectType,
						T.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	Taxonomy T 
						inner join Responsibility R on R.ObjectType = @Object and R.ObjectID = T.ID
							and (
								(T.ID = @ObjectID and @ObjectID is not null) or (@ObjectID is null)
								)
		end
	if @Object = 'TaxonomyType'
		begin
			insert into @tbl
				select	'Taxonomy Type Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						T.ID as AssigningItemID,
						@Object as ObjectType,
						T.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	TaxonomyType T 
						inner join Responsibility R on R.ObjectType = @Object and R.ObjectID = T.ID
							and (
								(T.ID = @ObjectID and @ObjectID is not null) or (@ObjectID is null)
								)
		end
	RETURN 
END