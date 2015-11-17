CREATE PROCEDURE [utility].[StartJob] (
    @name nvarchar(250),
	@minuteInterval int,
    @id uniqueidentifier OUTPUT)
AS

BEGIN TRANSACTION

SELECT	@id = ID
FROM	utility.JobActivity
WHERE	DATEDIFF(mi, DateStarted, GetDate()) < @minuteInterval
		AND Name = @name

IF (@@ROWCOUNT=0)
BEGIN
    -- Has Not Been Started
    SET @id = NewId()
    INSERT INTO utility.JobActivity	(	ID,		Name,	DateStarted	)
	VALUES							(	@id,	@name,	GetDate()	)
END
ELSE
BEGIN 
	SET @id = NULL
END

COMMIT TRAN
