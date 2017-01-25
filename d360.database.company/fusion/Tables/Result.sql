CREATE TABLE [fusion].[Result] (
    [ExecutionID]       INT              NOT NULL,
    [FusionAttributeID] INT              NOT NULL,
    [Body]              NVARCHAR (MAX)   NULL,
    [ID]                UNIQUEIDENTIFIER NULL,
    [FieldTypeID]       INT              NULL,
    [FieldName]         NVARCHAR (250)   CONSTRAINT [DF_FusionResult_FieldName] DEFAULT ('Name') NOT NULL,
    [Action]            VARCHAR (1)      NULL,
    [OldValue]          NVARCHAR (MAX)   NULL,
    [NewValue]          NVARCHAR (MAX)   NULL    
);




GO
CREATE NONCLUSTERED INDEX [IX_FusionResult_ExecutionID]
    ON [fusion].[Result]([ExecutionID] DESC);


GO
CREATE NONCLUSTERED INDEX [IX_FusionResult_Execution_FusionAttribute]
    ON [fusion].[Result]([ExecutionID] DESC, [FusionAttributeID] DESC);


GO
CREATE NONCLUSTERED INDEX [IX_FusionResult_Execution_FieldName]
    ON [fusion].[Result]([ExecutionID] DESC, [FieldName] DESC);


GO
CREATE NONCLUSTERED INDEX [IX_FusionResult_ExecutionID_Action]
    ON [fusion].[Result]([ExecutionID] ASC, [Action] ASC);

