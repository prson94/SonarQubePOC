CREATE TABLE [fusion].[RuleStep] (
    [ID]          INT             IDENTITY (1, 1) NOT NULL,
    [RuleID]      INT             NOT NULL,
    [Step]        INT             NOT NULL,
    [Action]      VARCHAR (25)    NOT NULL,
    [Description] NVARCHAR (4000) NULL,
    PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FusionRuleStep_FusionRule] FOREIGN KEY ([RuleID]) REFERENCES [fusion].[Rule] ([ID]) ON DELETE CASCADE
);



GO

CREATE TRIGGER [fusion].FusionRuleStep_OnAfter
   ON  fusion.RuleStep
   AFTER INSERT,DELETE,UPDATE
AS 
BEGIN
	SET NOCOUNT ON;
	update	T
	set		T.UpdatedOn = getutcdate()
	from	fusion.[Rule] T
			inner join inserted S on S.RuleID = T.ID;

	update	T
	set		T.UpdatedOn = getutcdate()
	from	fusion.[Rule] T
			inner join deleted S on S.RuleID = T.ID;
END