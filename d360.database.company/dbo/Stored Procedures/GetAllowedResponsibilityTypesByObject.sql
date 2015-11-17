CREATE procedure [dbo].[GetAllowedResponsibilityTypesByObject]
--declare
	@type varchar(50),
	@id int
--set @type = 'ArtifactType'
--set @id = 1
as
begin
	declare @useFilter bit
	set @useFilter = 1

	if @type not like '%Type'
	begin
		set @useFilter = 1
		SELECT	@id = ObjectTypeID
		from	cache.ObjectDetails where [Object] = @type and ObjectID = @id

		set @type = @type + 'Type'
	end

	if @useFilter = 1
		begin
			SELECT	RT.*
			FROM	ResponsibilityType RT
					inner join ResponsibilityTypeRelation RTR	on RTR.ResponsibilityTypeID = RT.ID 
																and RTR.ObjectType = @type
																and RTR.ObjectID = @id
		end
	else
		begin
			SELECT	*
			FROM	ResponsibilityType
			where	ID > 0
		end
end
