CREATE TABLE [dbo].[AssetDataQualityImplementationResult] (
    [ID]                               BIGINT   IDENTITY (1, 1) NOT NULL,
    [AssetDataQualityImplementationID] BIGINT   NOT NULL,
    [SourceID]                         INT      NULL,
    [EffectiveDate]                    DATETIME NOT NULL,
    [RunDate]                          DATETIME NOT NULL,
    [RowsPassed]                       INT      NOT NULL,
    [RowsFailed]                       INT      NOT NULL,
    [PassFraction]                     AS       (case when [RowsPassed]=(0) AND [RowsFailed]=(0) then (0) else CONVERT([decimal](4,3),CONVERT([decimal](18,3),[RowsPassed],(0))/(CONVERT([decimal](18,3),[RowsPassed],(0))+CONVERT([decimal](18,3),[RowsFailed],(0))),(0)) end),
    [FailFraction]                     AS       (case when [RowsPassed]=(0) AND [RowsFailed]=(0) then (0) when [RowsPassed]=(0) AND [RowsFailed]<>(0) then (1) else CONVERT([decimal](4,3),CONVERT([decimal](3,3),CONVERT([decimal](18,3),(1),(0))-CONVERT([decimal](18,3),[RowsPassed],(0))/(CONVERT([decimal](18,3),[RowsPassed],(0))+CONVERT([decimal](18,3),[RowsFailed],(0))),(0)),(0)) end),
    [Passed]                           AS       ([utility].[CalculatePassedWrapper](case when [RowsPassed]=(0) AND [RowsFailed]=(0) then (0) else CONVERT([decimal](4,3),CONVERT([decimal](18,3),[RowsPassed],(0))/(CONVERT([decimal](18,3),[RowsPassed],(0))+CONVERT([decimal](18,3),[RowsFailed],(0))),(0)) end,[AssetDataQualityImplementationID])),
    [CreatedOn]                        DATETIME NULL,
    [CreatedBy]                        INT      NULL,
    [UpdatedOn]                        DATETIME NULL,
    [UpdatedBy]                        INT      NULL,
    CONSTRAINT [PK_AssetDataQualityImplementationResult] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_AssetDataQualityImplementationResult_AssetDataQualityImplementation] FOREIGN KEY ([AssetDataQualityImplementationID]) REFERENCES [dbo].[AssetDataQualityImplementation] ([ID]) ON DELETE CASCADE
);

