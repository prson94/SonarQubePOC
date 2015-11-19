CREATE PROCEDURE [dbo].[FollowObject]
	@id int,
	@type varchar(50),
	@resourceID int,
	@followTypeID int = 1,
	@includeChildren bit = 0
AS
BEGIN


	insert into Follow (ResourceID, ObjectType, ObjectID, DateCreated, FollowTypeID)
	select
		@resourceID,
		@type,
		@id,
		getdate(),
		@followTypeID


	IF @followTypeID = 3 OR @id = 0 --Parent
	BEGIN
			exec [SetChildrenByFollowID] @@identity, @includeChildren;
	END

END

