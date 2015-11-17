CREATE PROCEDURE [utility].[StopJob]
(
    @id uniqueidentifier,
	@status varchar(8000)
)
AS
	UPDATE	utility.JobActivity
	SET		DateStopped = GetDate(),
			[Status] = @status
	WHERE	ID = @id
