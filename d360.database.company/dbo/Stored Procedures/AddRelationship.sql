CREATE procedure [dbo].[AddRelationship]
	@ResourceID int,
	@Date datetime,
	@Type varchar(50),				-- The start object type.
	@ID int,						-- The start object ID.
	@Classification int,
	@IntersectRole int,
	@Description nvarchar(4000),
	@TargetType varchar(50),				-- The end object type.
	@TargetID int						-- The end object ID.
as
begin
	declare @Objects ObjectsTable 
	insert into @Objects VALUES (@TargetType, @TargetID)
	declare @d datetime
	exec AddRelationships @ResourceID, @Date, @Type, @ID, @Classification, @IntersectRole, @Description, @Objects
end
