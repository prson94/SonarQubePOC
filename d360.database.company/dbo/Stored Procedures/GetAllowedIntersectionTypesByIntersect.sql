CREATE PROCEDURE [dbo].[GetAllowedIntersectionTypesByIntersect]
--declare 
	@IntersectID int
--set @IntersectID = 40954
AS
BEGIN
	SET NOCOUNT ON;

	declare @intersectTypeID int

	select @intersectTypeID = IntersectTypeID from [Intersect] where ID = @intersectID

	declare @tbl table (IntersectTypeID int, TargetType varchar(50), TargetTypeID int, TargetName nvarchar(500), ParentIntersectID int);

	insert into @tbl
		select	ID,
				Object,
				ObjectID,
				ObjectName,
				@IntersectID
		from	IntersectTypeDetail
		where	Subject = 'IntersectType' 
				and SubjectID = @intersectTypeID 

	-- Now figure out if we need to remove any fusion relationship types based on ownership.

	declare @OwnerSourceType varchar(50),
			@OwnerSourceID int

	select	top 1
			@OwnerSourceType = I.Subject,
			@OwnerSourceID = I.SubjectID
	from	[Intersect] I
			inner join Artifact A on I.Subject = 'Artifact' and A.ID = I.SubjectID and I.ID = @IntersectID
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
									select		T.ID
									from		FusionOwner O
												inner join @h h on h.ID = O.ArtifactID
												inner join Fusion F on F.ID = O.FusionID
												inner join FusionAttributeType T on T.FusionTypeID = F.FusionTypeID
									)

	select * from @tbl order by TargetName
END
