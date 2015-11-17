CREATE TABLE [reporting].[Global_FieldAudit] (
    [AuditID]     BIGINT         NOT NULL,
    [FieldTypeID] INT            NOT NULL,
    [FieldName]   NVARCHAR (250) NOT NULL,
    [Version]     INT            NOT NULL,
    [Value]       NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_ReportingFieldAudit] PRIMARY KEY CLUSTERED ([AuditID] ASC, [FieldTypeID] ASC, [FieldName] ASC, [Version] DESC)
);

