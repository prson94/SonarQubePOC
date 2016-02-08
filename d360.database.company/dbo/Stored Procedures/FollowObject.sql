CREATE PROCEDURE [dbo].[FollowObject]
	@id int,
	@type varchar(50),
	@resourceID int,
	@followTypeID int = 1,
	@includeChildren bit = 0
AS
BEGIN


	insert into Follow (ResourceID, ObjectType, ObjectID, DateCreated, FollowTypeID) values ( @resourceID, @type, @id, getutcdate(), @followTypeID)

	IF @followTypeID = 3 OR @id = 0 --Parent
	BEGIN
		delete	F
		from	Follow F
				inner join FollowDetail FD on FD.ObjectType = F.ObjectType and FD.ObjectID = FD.ObjectID and FD.HardFollow = 0 and F.ResourceID = FD.ResourceID and F.ResourceID = @resourceID and FD.Type = @type and FD.TypeID = @id
		--exec [SetChildrenByFollowID] @@identity, @includeChildren;
	END
END

