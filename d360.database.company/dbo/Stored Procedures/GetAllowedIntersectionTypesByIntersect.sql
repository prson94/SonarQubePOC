CREATE PROCEDURE [dbo].[GetAllowedIntersectionTypesByIntersect]
--declare 
	@IntersectID int
--set @IntersectID = 261502--261625
AS
BEGIN
	SET NOCOUNT ON;

	declare @intersectTypeID int

	select @intersectTypeID = IntersectTypeID from [Intersect] where ID = @intersectID

	declare @tbl table (IntersectTypeID int, TargetType varchar(50), TargetTypeID int, TargetName nvarchar(500), ParentIntersectID int);

	insert into @tbl
		select	RT.IntersectTypeID,
				RT.TargetObjectType,
				RT.TargetObjectID,
				case 
					when RT.TargetMenuDisplayText is null then coalesce(RTD.TextPath, RTD.Name)
					when RT.TargetMenuDisplayText = '' then coalesce(RTD.TextPath, RTD.Name)
					else RT.TargetMenuDisplayText
				end,
				@IntersectID
		from	[utility].[RelationshipTypes] RT 
				inner join cache.ObjectDetails RTD on RTD.[Object] = RT.TargetObjectType and RTD.ObjectID = RT.TargetObjectID
		where	RT.SourceObjectType = 'IntersectType' 
				and RT.SourceObjectID = @intersectTypeID 

	-- Now figure out if we need to remove any fusion relationship types based on ownership.

	declare @OwnerSourceType varchar(50),
			@OwnerSourceID int

	select	top 1
			@OwnerSourceType = ObjectType,
			@OwnerSourceID = ObjectID
	from	IntersectNode N
			inner join Artifact A on A.ID = N.ObjectID and N.ObjectType = 'Artifact' and N.IntersectID = @IntersectID
			inner join ArtifactType AT on AT.ID = A.ArtifactTypeID and AT.CanOwnFusion = 1

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


	delete	@tbl
	where	TargetType = 'FusionAttributeType'
			and TargetTypeID not in (
									select		R.ObjectID
									from		FusionAttributeOwnerRule R
												inner join @h h on 
													R.ObjectType = 'FusionAttributeType' 
													and R.RelationshipOwnerObjectType = 'Artifact'
													and h.ID = R.RelationshipOwnerObjectID

									group by	R.ObjectID
									)

	select * from @tbl order by TargetName
END
