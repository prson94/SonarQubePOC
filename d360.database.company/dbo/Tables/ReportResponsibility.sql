CREATE TABLE [dbo].[ReportResponsibility] (
    [ID]                INT            IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    [ReportID]          INT            NOT NULL,
    [ResponsibilityTypeID] INT		   NOT NULL,
	CONSTRAINT [FK_ReportResponsibility_Report] FOREIGN KEY ([ReportID]) REFERENCES [dbo].[Report] ([ID]) ON DELETE CASCADE,
);