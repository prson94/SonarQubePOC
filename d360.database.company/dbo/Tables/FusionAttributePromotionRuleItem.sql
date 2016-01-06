CREATE TABLE [dbo].[FusionAttributePromotionRuleItem] (
    [ID]                             INT IDENTITY (1, 1) NOT NULL,
    [FusionAttributePromotionRuleID] INT NOT NULL,
    [FusionAttributeID]              INT NULL,
    CONSTRAINT [PK_FusionAttributePromotionRuleItem] PRIMARY KEY CLUSTERED ([ID] ASC)
);


go

create TRIGGER [dbo].[FusionAttributePromotionRuleItem_AfterUpsertOrDelete]
	ON [dbo].[fusionattributepromotionruleitem]
	FOR INSERT, UPDATE, DELETE
AS
	SET NOCOUNT ON;
		
	IF EXISTS(SELECT * FROM DELETED)
    BEGIN
        UPDATE	T
		SET		T.UpdatedOn = CURRENT_TIMESTAMP
		FROM	fusionattributepromotionrule T 
				inner join deleted F on F.FusionAttributePromotionRuleID = T.ID;
    END
	else
	begin
		--modify the modify date of the corresponding rule in fusionattributepromotionrule
		UPDATE	T
		SET		T.UpdatedOn = CURRENT_TIMESTAMP
		FROM	fusionattributepromotionrule T 
				inner join inserted F on F.FusionAttributePromotionRuleID = T.ID;
	end;
GO