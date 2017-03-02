CREATE PROCEDURE [dbo].[GetAllowedIntersectionTypes]
	@SourceType varchar(50),
	@SourceTypeID int,
	@IntersectID int = 0
AS
BEGIN
	SET NOCOUNT ON;

	declare @tbl table (IntersectTypeID int, TargetType varchar(50), TargetTypeID int, TargetName nvarchar(500), ParentIntersectID int, PredicateName nvarchar(100), SourceName nvarchar(500), SourceTypeID int, SourceType varchar(50));
	
	insert into @tbl
		(IntersectTypeID, TargetType, TargetTypeID, TargetName, ParentIntersectID, PredicateName, SourceName, SourceTypeID, SourceType)
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
				RT.PredicateName,
				case 
					when RT.Subject = @SourceType and RT.SubjectID = @SourceTypeID then RT.SubjectName
					else RT.ObjectName
				end AS SourceName,
				case 
					when RT.Subject = @SourceType and RT.SubjectID = @SourceTypeID then RT.SubjectID
					else RT.ObjectID
				end AS SourceTypeID,
				case 
					when RT.Subject = @SourceType and RT.SubjectID = @SourceTypeID then RT.Subject
					else RT.Object
				end AS SourceType
		FROM	IntersectTypeDetail RT
		WHERE	(RT.Subject = @SourceType and RT.SubjectID = @SourceTypeID) OR 
				(RT.Object = @SourceType and RT.ObjectID = @SourceTypeID)
				
	-- load any map types for this object
			insert into @tbl
			(IntersectTypeID, TargetType, TargetTypeID, TargetName, ParentIntersectID, PredicateName, SourceName, SourceTypeID, SourceType)
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
					RT.PredicateName,
					case 
						when RT.Subject = @SourceType and RT.SubjectID = @SourceTypeID then RT.SubjectName
						else RT.ObjectName
					end AS SourceName,
					case 
						when RT.Subject = @SourceType and RT.SubjectID = @SourceTypeID then RT.SubjectID
						else RT.ObjectID
					end AS SourceTypeID,
					case 
						when RT.Subject = @SourceType and RT.SubjectID = @SourceTypeID then RT.Subject
						else RT.Object
				end AS SourceType
			FROM	IntersectTypeDetail RT
					inner join @tbl t on (t.TargetType = 'MapType'  and (RT.Subject = 'MapType' and RT.SubjectID = t.TargetTypeID) );

	--delete the map type associated directly with this type					
	delete from @tbl where TargetType = 'MapType' and SourceTypeID = @SourceTypeID and SourceType = @SourceType;
	
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
					when 'Maptype' then 'Map: ' + SourceName
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
					when 'Maptype' then 'Map: ' + SourceName
					else ''
				end + ' : ' + TargetName
END