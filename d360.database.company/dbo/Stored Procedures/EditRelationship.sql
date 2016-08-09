CREATE procedure [dbo].[EditRelationship]
--declare
	@ResourceID int,
	@Date datetime,
	@ID int,						-- The Intersect ID.
	@Classification int,
	@IntersectRole int,
	@Description nvarchar(4000)
--set @ResourceID = 1
--set @Date = getutcdate()
--set @ID = 40902
--set @Classification = 1
--set @Description = NULL
as
begin
	set nocount on;

	declare @Intersects IDTable

	update	[Intersect]
	set		Classification = @Classification,
			Description = @Description
	where	ID = @ID

	exec utility.AddAuditEntry 'Intersect', @ID, @ResourceID, @Date, 'Updated', 'Intersect', @ID

	insert into @Intersects VALUES (@ID)
	exec cache.SynchronizeRelationships @Intersects
end