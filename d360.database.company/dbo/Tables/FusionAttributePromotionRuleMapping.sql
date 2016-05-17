CREATE TABLE [dbo].[FusionAttributePromotionRuleMapping] (
    [ID]                             INT            IDENTITY (1, 1) NOT NULL,
    [FusionAttributePromotionRuleID] INT            NOT NULL,
    [SourceFieldName]                NVARCHAR (250) NULL,
    [SourceFieldTypeID]              INT            NOT NULL,
    [TargetFieldName]                NVARCHAR (250) NULL,
    [TargetFieldTypeID]              INT            NOT NULL,
    [IsConstantValue]                BIT            DEFAULT ((0)) NOT NULL,
    [ConstantValue]                  NVARCHAR (250) NULL,
    CONSTRAINT [PK_FusionAttributePromotionRuleMapping] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FusionAttributePromotionRuleMapping_FusionAttributePromotionRule] FOREIGN KEY ([FusionAttributePromotionRuleID]) REFERENCES [dbo].[FusionAttributePromotionRule] ([ID]) ON DELETE CASCADE
);



go



create TRIGGER [dbo].[FusionAttributePromotionRuleMapping_AfterUpsertOrDelete]
	ON [dbo].[FusionAttributePromotionRuleMapping]
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
CREATE CLUSTERED INDEX [CIX_FusionAttributePromotionRuleMapping]
    ON [dbo].[FusionAttributePromotionRuleMapping]([FusionAttributePromotionRuleID] ASC);

