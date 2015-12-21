CREATE TABLE [fusion].[StagingItem] (
    [RowID]                   INT             NOT NULL,
    [Name]                    NVARCHAR (250)  NULL,
    [Value]                   NVARCHAR (4000) NULL,
    [FusionAttributeTypeID]   INT             NULL,
    [FusionAttributeID]       INT             NULL,
    [SourceID]                NVARCHAR (250)  NULL,
    [ParentFusionAttributeID] INT             NULL,
    [ParentSourceID]          NVARCHAR (250)  NULL,
    [FieldTypeID]             INT             NULL,
    [OldValue]                NVARCHAR (4000) NULL,
    [Action]                  VARCHAR (1)     NULL,
    [Message]                 NVARCHAR (4000) NULL,
    [FieldExists]             BIT             NULL,
    [ExecutionID]             INT             NOT NULL,
    [ValueAsShortChar]        VARCHAR (250)   NULL,
    [ValueAsInt]              INT             NULL
);




GO
CREATE CLUSTERED INDEX [CIX_FusionStagingItem]
    ON [fusion].[StagingItem]([ExecutionID] DESC, [RowID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_FusionStagingItem_FusionAttributeTypeID]
    ON [fusion].[StagingItem]([ExecutionID] DESC, [FusionAttributeTypeID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_FusionStagingItem_Name]
    ON [fusion].[StagingItem]([ExecutionID] DESC, [Name] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_FusionStagingItem_ExecutionID]
    ON [fusion].[StagingItem]([ExecutionID] DESC);


GO
CREATE NONCLUSTERED INDEX [IX_FusionStagingItem_FusionAttributeTypeID_SourceID_ExecutionID]
    ON [fusion].[StagingItem]([FusionAttributeTypeID] ASC, [SourceID] ASC, [ExecutionID] ASC)
    INCLUDE([RowID]);


GO
CREATE NONCLUSTERED INDEX [IX_FusionStagingItem_ParentSourceID_ExecutionID]
    ON [fusion].[StagingItem]([ParentSourceID] ASC, [ExecutionID] ASC)
    INCLUDE([RowID]);


GO
CREATE NONCLUSTERED INDEX [IX_FusionStagingItem_Name_Execution]
    ON [fusion].[StagingItem]([Name] ASC, [ExecutionID] ASC)
    INCLUDE([Value], [FusionAttributeTypeID], [SourceID], [Action]);


GO
CREATE NONCLUSTERED INDEX [IX_FusionStagingItem_Execution-Name]
    ON [fusion].[StagingItem]([Name] ASC, [ExecutionID] ASC)
    INCLUDE([Value], [FusionAttributeTypeID], [SourceID], [Action]);


GO
CREATE NONCLUSTERED INDEX [IX_FusionStagingItem_FusionAttributeType_Source]
    ON [fusion].[StagingItem]([FusionAttributeTypeID] ASC, [SourceID] ASC);

