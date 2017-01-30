CREATE FUNCTION dbo.GetWorkflowArtifactID(@Data XML)
RETURNS INT
WITH SCHEMABINDING
AS BEGIN
  DECLARE @ArtifactID INT

  SELECT  
    @ArtifactID = @Data.value('(fields/ArtifactID/text())[1]', 'int')

  RETURN @ArtifactID
END

GO

-- ADD THIS FUNCTION AS A PERSISTED COLUMN ON THE WORKFLOW TABLE
ALTER TABLE dbo.workflow ADD ArtifactID AS dbo.GetWorkflowArtifactID([Data]) PERSISTED
GO