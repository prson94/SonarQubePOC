CREATE procedure [dbo].[GetAllowedAndUnallocatedResponsibilityTypesByObject]
--declare
	@type varchar(50),
	@id int
--set @type = 'Artifact'
--set @id = 733
as
begin
	declare @TypeID int
	SELECT	@TypeID = ObjectTypeID
	from	cache.ObjectDetails 
	where	[Object] = @type and ObjectID = @id--utility.ObjectDetail(@type, @id)

	SELECT	RT.*
	FROM	ResponsibilityType RT
			inner join ResponsibilityTypeRelation RTR	on RTR.ResponsibilityTypeID = RT.ID 
														and RTR.ObjectType = @type + 'Type'
														and RTR.ObjectID = @TypeID
			and RT.ID not in (SELECT ResponsibilityTypeID from Responsibility where ObjectType = @type and ObjectID = @id)
end
