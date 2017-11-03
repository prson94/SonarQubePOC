CREATE TABLE [dbo].[ReportResponsibility] (
    [ID]                   INT IDENTITY (1, 1) NOT NULL,
    [ReportID]             INT NOT NULL,
    [ResponsibilityTypeID] INT NOT NULL,
    PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_ReportResponsibility_Report] FOREIGN KEY ([ReportID]) REFERENCES [dbo].[Report] ([ID]) ON DELETE CASCADE
);

