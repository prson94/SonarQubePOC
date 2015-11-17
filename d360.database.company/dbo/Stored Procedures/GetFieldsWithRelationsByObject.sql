CREATE PROCEDURE dbo.GetFieldsWithRelationsByObject
	@type varchar(25),
	@id int
as
begin
	select	* 
	from	FieldWithRelation 
	WHERE	ObjectType = @type
			and ObjectID = @id
end