CREATE PROCEDURE utility.AddOrUpdateFieldType
(
	@u int,
	@d datetime,
	@id int,
	@name nvarchar(250),
	@friendly nvarchar(250),
	@type varchar(25)
)
AS
BEGIN
	IF EXISTS(SELECT 1 from FieldType where ID = @id)
	begin
		UPDATE	FieldType
		SET		Name = @name,
				FriendlyName = @friendly,
				[Type] = @type
		WHERE	ID = @id
	end
	ELSE
	begin
		SET IDENTITY_INSERT FieldType ON
		INSERT INTO FieldType  ( ID, Name, FriendlyName, [Type] )
		VALUES ( @id, @name, @friendly, @type )
		SET IDENTITY_INSERT FieldType OFF
	end
END