CREATE TABLE [dbo].[IntersectType] (
    [ID]                 INT          IDENTITY (1, 1) NOT NULL,
    [Name]               AS           ([utility].[DeriveIntersectTypeNameWrapper]([ID])),
    [UpdatedOn]          DATETIME     NULL,
    [UpdatedBy]          INT          NULL,
    [Subject]            VARCHAR (50) NULL,
    [SubjectID]          INT          NULL,
    [Object]             VARCHAR (50) NULL,
    [ObjectID]           INT          NULL,
    [IsSystem]           BIT          NULL,
    [CreatedBy]          INT          NULL,
    [CreatedOn]          DATETIME     NULL,
    [PredicateID]        INT          NULL,
    [SubjectCardinality] INT          CONSTRAINT [DF_IntersectType_SubjectCardinality] DEFAULT ((2)) NOT NULL,
    [ObjectCardinality]  INT          CONSTRAINT [DF_IntersectType_ObjectCardinality] DEFAULT ((2)) NOT NULL,
    CONSTRAINT [PK_IntersectType] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [UQ_IntersectType] UNIQUE NONCLUSTERED ([Subject] ASC, [SubjectID] ASC, [Object] ASC, [ObjectID] ASC, [PredicateID] ASC)
);












GO


GO

CREATE TRIGGER [dbo].[IntersectType_AfterDelete]
   ON  [dbo].[IntersectType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'IntersectType', ID, coalesce(UpdatedBy, 0)), 'IntersectType', ID from deleted

GO

