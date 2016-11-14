CREATE PROCEDURE [dbo].[GetAllowedIntersectionTypes]
--declare 
	@SourceType varchar(250),
	@SourceTypeID int,
	@IntersectID int = 0
--set @SourceType = 'ArtifactType'--'FusionAttributeType'
--set @SourceTypeID = 1--213
--set @IntersectID = 40859
AS
BEGIN
	SET NOCOUNT ON;

	declare @tbl table (IntersectTypeID int, TargetType varchar(50), TargetTypeID int, TargetName nvarchar(500), ParentIntersectID int, PredicateName nvarchar(100));

	insert into @tbl
		SELECT	RT.ID,
				case 
					when RT.Subject = @SourceType and RT.SubjectID = @SourceTypeID then RT.Object 
					else RT.Subject
				end AS TargetType,
				case 
					when RT.Subject = @SourceType and RT.SubjectID = @SourceTypeID then RT.ObjectID
					else RT.SubjectID
				end AS TargetTypeID,
				case 
					when RT.Subject = @SourceType and RT.SubjectID = @SourceTypeID then RT.ObjectName
					else RT.SubjectName
				end AS TargetName,
				NULL,
				RT.PredicateName
		FROM	IntersectTypeDetail RT
		WHERE	(RT.Subject = @SourceType and RT.SubjectID = @SourceTypeID) OR 
				(RT.Object = @SourceType and RT.ObjectID = @SourceTypeID)

	if @IntersectID > 0
	begin
		select	@SourceType = 'IntersectType',
				@SourceTypeID = IntersectTypeID
		from	[Intersect]
		where	ID = @IntersectID;

		insert into @tbl
			SELECT		RT.ID,
						case 
							when RT.Subject = @SourceType and RT.SubjectID = @SourceTypeID then RT.Object 
							else RT.Subject
						end AS TargetType,
						case 
							when RT.Subject = @SourceType and RT.SubjectID = @SourceTypeID then RT.ObjectID
							else RT.SubjectID
						end AS TargetTypeID,
						case 
							when RT.Subject = @SourceType and RT.SubjectID = @SourceTypeID then RT.ObjectName
							else RT.SubjectName
						end AS TargetName,
						NULL,
						RT.PredicateName
			FROM		IntersectTypeDetail RT
			WHERE	(RT.Subject = @SourceType and RT.SubjectID = @SourceTypeID) OR 
					(RT.Object = @SourceType and RT.ObjectID = @SourceTypeID)

		-- Now figure out if we need to remove any fusion relationship types based on ownership.
		select	top 1
				@SourceType = 'Artifact',
				@SourceTypeID = A.ID
		from	[Intersect] I
				inner join Artifact A on ( (A.ID = I.SubjectID and I.Subject = 'Artifact') OR (A.ID = I.ObjectID and I.Object = 'Artifact') ) and I.ID = @IntersectID
				inner join ArtifactType AT on AT.ID = A.ArtifactTypeID and AT.CanOwnFusion = 1

		delete	@tbl
		where	TargetType = 'FusionAttributeType'
				and TargetTypeID not in (
										select	T.ID
										from	FusionOwner FO
												inner join Fusion F on F.ID = FO.FusionID
												inner join FusionAttributeType T on T.FusionTypeID = F.FusionTypeID
										where	@SourceType = 'Artifact' and FO.ArtifactID = @SourceTypeID
										)
	end

	select		distinct
				IntersectTypeID, 
				TargetType, 
				TargetTypeID, 
				case TargetType
					when 'TaxonomyType' then 'Model: '
					when 'DomainType' then 'Reference: '
					when 'FusionType' then 'Fusion: '
					when 'FusionAttributeType' then 'Fusion: '
					when 'ArtifactType' then 'Glossary: '
					when 'RuleType' then 'Rules: '
					when 'PolicyType' then 'Policies: '
					else ''
				end + ' : ' + TargetName as TargetName, 
				ParentIntersectID,
				PredicateName
	from		@tbl 
	order by	case TargetType
					when 'TaxonomyType' then 'Model: '
					when 'DomainType' then 'Reference: '
					when 'FusionType' then 'Fusion: '
					when 'FusionAttributeType' then 'Fusion: '
					when 'ArtifactType' then 'Glossary: '
					when 'RuleType' then 'Rules: '
					when 'PolicyType' then 'Policies: '
					else ''
				end + ' : ' + TargetName
END




