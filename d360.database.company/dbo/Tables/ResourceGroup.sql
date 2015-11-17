CREATE TABLE [dbo].[ResourceGroup] (
    [ResourceID] INT NOT NULL,
    [GroupID]    INT NOT NULL,
    [IsOwner]    BIT CONSTRAINT [DF_ResourceGroup_IsOwner] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_ResourceGroup] PRIMARY KEY CLUSTERED ([ResourceID] ASC, [GroupID] ASC),
    CONSTRAINT [FK_ResourceGroup_Group] FOREIGN KEY ([GroupID]) REFERENCES [dbo].[Group] ([ID]) ON DELETE CASCADE
);

