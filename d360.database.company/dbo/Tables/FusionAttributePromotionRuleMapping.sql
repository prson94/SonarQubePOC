CREATE TABLE [dbo].[FusionAttributePromotionRuleMapping] (
    [ID]                             INT            IDENTITY (1, 1) NOT NULL,
    [FusionAttributePromotionRuleID] INT            NOT NULL,
    [SourceFieldName]                NVARCHAR (250) NULL,
    [SourceFieldTypeID]              INT            NOT NULL,
    [TargetFieldName]                NVARCHAR (250) NULL,
    [TargetFieldTypeID]              INT            NOT NULL,
    [IsConstantValue]				 BIT NOT NULL DEFAULT 0, 
    [ConstantValue]					 NVARCHAR(250) NULL, 
    CONSTRAINT [PK_FusionAttributePromotionRuleMapping] PRIMARY KEY CLUSTERED ([ID] ASC)
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