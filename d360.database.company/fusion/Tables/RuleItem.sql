CREATE TABLE [fusion].[RuleItem] (
    [ID]         INT            IDENTITY (1, 1) NOT NULL,
    [RuleID]     INT            NULL,
    [ObjectID]   INT            NULL,
    [ObjectType] NVARCHAR (250) NULL,
    CONSTRAINT [PK_RuleItem] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FusionRuleItem_FusionRule] FOREIGN KEY ([RuleID]) REFERENCES [fusion].[Rule] ([ID]) ON DELETE CASCADE
);






GO
CREATE TRIGGER [fusion].FusionRuleItem_OnAfter
   ON  fusion.RuleItem
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