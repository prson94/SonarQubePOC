CREATE FUNCTION utility.GetVerticalResponsibilityList
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

	if @Object = 'ArtifactType' OR @Object = 'Artifact'
		begin
			insert into @tbl
				select	'Artifact Vertical' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						'ArtifactType' as AssigningItemType,
						T.ID as AssigningItemID,
						'Artifact' as ObjectType,
						A.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	ArtifactType T 
						inner join Responsibility R on R.ObjectType = 'ArtifactType' and R.ObjectID = T.ID
						inner join Artifact A on A.ArtifactTypeID = T.ID 
													and (
															(
																(
																(@Object = 'ArtifactType' and A.ArtifactTypeID = @ObjectID) OR 
																(@Object = 'Artifact' and A.ID = @ObjectID)
																)
																and @ObjectID is not null 
															)
															OR @ObjectID is null 
														);

			insert into @tbl
				select	'Taxonomy Vertical' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						'TaxonomyType' as AssigningItemType,
						T.ID as AssigningItemID,
						'Taxonomy' as ObjectType,
						A.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority+1 as [Priority]
				from	TaxonomyType T 
						inner join Responsibility R on R.ObjectType = 'TaxonomyType' and R.ObjectID = T.ID
						inner join Taxonomy A on A.TaxonomyTypeID = T.ID
						inner join Artifact AR on AR.TaxonomyTypeID = T.ID
												  and	(
															(
																(
																(@Object = 'ArtifactType' and AR.ArtifactTypeID = @ObjectID) OR 
																(@Object = 'Artifact' and AR.ID = @ObjectID)
																)
																and @ObjectID is not null 
															)
															OR @ObjectID is null 
														)
						inner join cache.Relationship RE on RE.SourceObject = 'Taxonomy' and RE.SourceObjectID = A.ID and RE.TargetObject = 'Artifact' and RE.TargetObjectID = AR.ID;
		end
	if @Object = 'DomainType' OR @Object = 'Domain'
		begin
			insert into @tbl
				select	'Domain Vertical' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						'DomainType' as AssigningItemType,
						T.ID as AssigningItemID,
						'Domain' as ObjectType,
						A.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	DomainType T 
						inner join Responsibility R on R.ObjectType = 'DomainType' and R.ObjectID = T.ID
						inner join Domain A on A.DomainTypeID = T.ID 
												and (
														(
															(
															(@Object = 'DomainType' and T.ID = @ObjectID) 
															OR (@Object = 'Domain' and A.ID = @ObjectID) 
															)
															and @ObjectID is not null
														)
														or (@ObjectID is null)
													);
		end
	if @Object = 'FusionType' OR @Object = 'Fusion'
		begin
			insert into @tbl
				select	'Fusion Vertical' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						'FusionType' as AssigningItemType,
						T.ID as AssigningItemID,
						'Fusion' as ObjectType,
						A.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	FusionType T 
						inner join Responsibility R on R.ObjectType = 'FusionType' and R.ObjectID = T.ID
						inner join Fusion A on A.FusionTypeID = T.ID 
												and (
														(
															(
															(@Object = 'FusionType' and T.ID = @ObjectID) 
															OR (@Object = 'Fusion' and A.ID = @ObjectID) 
															)
															and @ObjectID is not null
														)
														or (@ObjectID is null)
													);																		 
		end
	if @Object = 'TaxonomyType' OR @Object = 'Taxonomy'
		begin
			insert into @tbl
				select	'Taxonomy Vertical' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						'TaxonomyType' as AssigningItemType,
						T.ID as AssigningItemID,
						'Taxonomy' as ObjectType,
						A.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	TaxonomyType T 
						inner join Responsibility R on R.ObjectType = 'TaxonomyType' and R.ObjectID = T.ID
						inner join Taxonomy A on A.TaxonomyTypeID = T.ID 
												and (
														(
															(
															(@Object = 'TaxonomyType' and T.ID = @ObjectID) 
															OR (@Object = 'Taxonomy' and A.ID = @ObjectID)
															)
															and @ObjectID is not null
														)
														or (@ObjectID is null)
													);
		end
	RETURN 
END