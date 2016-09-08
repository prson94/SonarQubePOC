create FUNCTION [quality].[CalculatePassedWrapper]
(
	@PassFraction decimal(4,3),
	@QualityRuleID int
)
RETURNS bit
AS
BEGIN
	RETURN [quality].CalculatePassed(@PassFraction, @QualityRuleID)
END
GO

