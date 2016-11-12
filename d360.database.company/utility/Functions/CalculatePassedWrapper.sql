
create FUNCTION [utility].[CalculatePassedWrapper]
(
	@PassFraction decimal(4,3),
	@RuleID int
)
RETURNS bit
AS
BEGIN
	RETURN [utility].CalculatePassed(@PassFraction, @RuleID)
END