CREATE TABLE [dbo].[Survey] (
    [ID]           INT          IDENTITY (1, 1) NOT NULL,
    [SurveyTypeID] INT          NOT NULL,
    [ObjectType]   VARCHAR (25) NULL,
    [ObjectID]     INT          NOT NULL,
    [ResourceID]   INT          NOT NULL,
    CONSTRAINT [PK_Survey] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Survey_SurveyType] FOREIGN KEY ([SurveyTypeID]) REFERENCES [dbo].[SurveyType] ([ID]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_Survey_ObjectType-ObjectID]
    ON [dbo].[Survey]([ObjectType] ASC, [ObjectID] ASC);


GO
CREATE TRIGGER Survey_OnAfterInsert
   ON  dbo.Survey
   AFTER INSERT
AS 
BEGIN
	SET NOCOUNT ON;
	
	DECLARE @objectID int,
			@surveyTypeID int,
			@objectType varchar(25)
	
	SELECT	@surveyTypeID = SurveyTypeID,
			@objectType = ObjectType,
			@objectID = ObjectID
	FROM	INSERTED;

	IF NOT EXISTS(SELECT 1 FROM SurveyObjectCache WHERE SurveyTypeID = @surveyTypeID AND ObjectType = @objectType AND ObjectID = @objectID)
	BEGIN
		INSERT INTO SurveyObjectCache (SurveyTypeID, ObjectType, ObjectID) VALUES (@surveyTypeID, @objectType, @objectID)
	END
END