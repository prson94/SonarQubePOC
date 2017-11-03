CREATE TABLE [dbo].[FusionQueryAttributeType] (
    [ID]            INT            IDENTITY (1, 1) NOT NULL,
    [FusionID]      INT            NOT NULL,
    [Name]          NVARCHAR (250) NOT NULL,
    [Query]         NVARCHAR (MAX) NOT NULL,
    [CreatedOn]     DATETIME       NULL,
    [CreatedBy]     INT            NULL,
    [UpdatedOn]     DATETIME       NULL,
    [UpdatedBy]     INT            NULL,
    [DisplayFormat] NVARCHAR (250) CONSTRAINT [DF_FusionQueryAttributeType_DisplayFormat] DEFAULT ('{ID}') NOT NULL,
    CONSTRAINT [PK_FusionQueryAttributeType] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FusionQueryAttributeType_Fusion] FOREIGN KEY ([FusionID]) REFERENCES [dbo].[Fusion] ([ID]) ON DELETE CASCADE
);




GO

GO
