CREATE PROCEDURE [dbo].[GetAllowedIntersectionTypes]
(
--declare 
	@SourceType varchar(250),
	@SourceTypeID int,
	@IntersectID int = 0
--set @SourceType = 'ArtifactType'--'FusionAttributeType'
--set @SourceTypeID = 1--213
--set @IntersectID = 32
)
AS
BEGIN
	SET NOCOUNT ON;

	declare @tbl table (IntersectTypeID int, TargetType varchar(50), TargetTypeID int, TargetName nvarchar(500), ParentIntersectID int);

	insert into @tbl
		SELECT	RT.IntersectTypeID,
				RT.[TargetObjectType] AS TargetType,
				RT.[TargetObjectID] AS TargetTypeID, 
				(
				case RT.[TargetObjectType]
					when 'TaxonomyType' then 'Model: '
					when 'DomainType' then 'Reference: '
					when 'FusionType' then 'Fusion: '
					when 'FusionAttributeType' then 'Fusion: '
					when 'ArtifactType' then 'Glossary: '
					when 'RuleType' then 'Rules: '
					when 'PolicyType' then 'Policies: '
					else ''
				end + 
				case 
					when RT.[TargetMenuDisplayText] is null then coalesce(RTD.Name, RT.[TargetObjectType])
					when RT.[TargetMenuDisplayText] = '' then coalesce(RTD.Name, RT.[TargetObjectType])
					else RT.[TargetMenuDisplayText]
				end 
				) AS TargetName,
				NULL
		FROM	[utility].[RelationshipTypes] RT
				left join cache.ObjectDetails RTD on RTD.[Object] = RT.TargetObjectType and RTD.ObjectID = RT.TargetObjectID
		WHERE	RT.SourceObjectType = @SourceType and RT.SourceObjectID = @SourceTypeID 
		ORDER BY (
				case RT.[TargetObjectType]
					when 'TaxonomyType' then 'Model: '
					when 'DomainType' then 'Reference: '
					when 'FusionType' then 'Fusion: '
					when 'FusionAttributeType' then 'Fusion: '
					when 'ArtifactType' then 'Glossary: '
					when 'RuleType' then 'Rules: '
					when 'PolicyType' then 'Policies: '
					else ''
				end + 
				case 
					when RT.[TargetMenuDisplayText] is null then coalesce(RTD.Name, RT.[TargetObjectType])
					when RT.[TargetMenuDisplayText] = '' then coalesce(RTD.Name, RT.[TargetObjectType])
					else RT.[TargetMenuDisplayText]
				end 
				)

	if @IntersectID > 0
	begin
		select	@SourceType = 'IntersectType',
				@SourceTypeID = IntersectTypeID
		from	[Intersect]
		where	ID = @IntersectID;

		insert into @tbl
			SELECT	RT.IntersectTypeID,
					RT.[TargetObjectType] AS TargetType,
					RT.[TargetObjectID] AS TargetTypeID, 
					case 
						when RT.[TargetMenuDisplayText] is null then coalesce(RTD.Name, RT.[TargetObjectType])
						when RT.[TargetMenuDisplayText] = '' then coalesce(RTD.Name, RT.[TargetObjectType])
						else RT.[TargetMenuDisplayText]
					end AS TargetName,
					NULL
			FROM	[utility].[RelationshipTypes] RT
					left join cache.ObjectDetails RTD on RTD.[Object] = RT.TargetObjectType and RTD.ObjectID = RT.TargetObjectID
			WHERE	RT.SourceObjectType = @SourceType and RT.SourceObjectID = @SourceTypeID
			ORDER BY case 
						when RT.[TargetMenuDisplayText] is null then coalesce(RTD.Name, RT.[TargetObjectType])
						when RT.[TargetMenuDisplayText] = '' then coalesce(RTD.Name, RT.[TargetObjectType])
						else RT.[TargetMenuDisplayText]
					end

		-- Now figure out if we need to remove any fusion relationship types based on ownership.
		select	top 1
				@SourceType = ObjectType,
				@SourceTypeID = ObjectID
		from	IntersectNode N
				inner join Artifact A	on A.ID = N.ObjectID and N.ObjectType = 'Artifact'
										and N.IntersectID = @IntersectID
				inner join ArtifactType AT on AT.ID = A.ArtifactTypeID and AT.CanOwnFusion = 1

		delete	@tbl
		where	TargetType = 'FusionAttributeType'
				and TargetTypeID not in (
										select	ObjectID
										from	FusionAttributeOwnerRule
										where	ObjectType = 'FusionAttributeType' 
												and RelationshipOwnerObjectType = @SourceType 
												and RelationshipOwnerObjectID = @SourceTypeID
										)
	end

	select * from @tbl order by TargetName
END

