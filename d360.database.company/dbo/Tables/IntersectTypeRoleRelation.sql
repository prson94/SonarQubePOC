CREATE TABLE [dbo].[IntersectTypeRoleRelation] (
    [IntersectTypeID]     INT            NOT NULL,
    [IntersectTypeRoleID] INT            NOT NULL,
    [Side1Label]          NVARCHAR (250) NOT NULL,
    [Side2Label]          NVARCHAR (250) NOT NULL,
    CONSTRAINT [PK_IntersectTypeRoleRelation] PRIMARY KEY CLUSTERED ([IntersectTypeID] ASC, [IntersectTypeRoleID] ASC),
    CONSTRAINT [FK_IntersectTypeRoleRelation_IntersectType] FOREIGN KEY ([IntersectTypeID]) REFERENCES [dbo].[IntersectType] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_IntersectTypeRoleRelation_IntersectTypeRole] FOREIGN KEY ([IntersectTypeRoleID]) REFERENCES [dbo].[IntersectTypeRole] ([ID]) ON DELETE CASCADE
);

