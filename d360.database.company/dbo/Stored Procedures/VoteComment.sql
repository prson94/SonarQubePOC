CREATE PROCEDURE [dbo].[VoteComment]
	@CommentID int,
	@ResourceID int,
	@Vote int
AS
BEGIN

	IF (select count(*) from commentvote where commentid = @CommentID and Resourceid = @ResourceID) > 0
	BEGIN
	IF @Vote = (select vote from commentvote where commentid = @CommentID and ResourceId = @ResourceID)
	BEGIN
		--removing vote
		delete from CommentVote
		where commentid = @CommentID AND
		ResourceID = @ResourceID
	END
	ELSE
	BEGIN
		--changing vote
		update CommentVote
		SET Vote = @Vote
		Where CommentID = @CommentID AND ResourceID = @ResourceID
	END

	END
	else
	BEGIN
		--voting for the first time
		INSERT INTO CommentVote (CommentID, ResourceID, Vote) values (@CommentID, @ResourceID, @Vote);
	END

	select * from CommentVote where CommentID = @CommentID;
END