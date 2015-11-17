CREATE TABLE [plugin].[EventTypeFieldType] (
    [EventTypeID] INT NOT NULL,
    [FieldTypeID] INT NOT NULL,
    CONSTRAINT [PK_Plugin_EventTypeFieldType] PRIMARY KEY CLUSTERED ([EventTypeID] ASC, [FieldTypeID] ASC)
);

