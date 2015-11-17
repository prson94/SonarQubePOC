CREATE TABLE [dbo].[WorkflowTypeRelation] (
    [WorkflowType]         INT          NOT NULL,
    [Object]               VARCHAR (50) NOT NULL,
    [ObjectID]             INT          NOT NULL,
    [Enabled]              BIT          CONSTRAINT [DF_WorkflowTypeRelation_Enabled] DEFAULT ((0)) NOT NULL,
    [Fields]               XML          CONSTRAINT [DF_WorkflowTypeRelation_Fields] DEFAULT ('<fields />') NOT NULL,
    [ID]                   INT          IDENTITY (1, 1) NOT NULL,
    [Parent]               VARCHAR (50) NULL,
    [ParentID]             INT          NULL,
    [ResponsibilityTypeID] INT          NOT NULL,
    CONSTRAINT [PK_WorkflowTypeRelation] PRIMARY KEY CLUSTERED ([ID] ASC)
);

