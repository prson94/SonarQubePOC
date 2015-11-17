CREATE TABLE [dbo].[ResponseTypeOption] (
    [ID]             INT            IDENTITY (50000, 1) NOT NULL,
    [ResponseTypeID] INT            NOT NULL,
    [Name]           NVARCHAR (250) NOT NULL,
    [Value]          INT            NULL,
    CONSTRAINT [PK_ResponseTypeOption] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_ResponseTypeOption_ResponseType] FOREIGN KEY ([ResponseTypeID]) REFERENCES [dbo].[ResponseType] ([ID]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_ResponseTypeOption_ResponseTypeID]
    ON [dbo].[ResponseTypeOption]([ResponseTypeID] ASC);

