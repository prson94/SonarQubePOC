CREATE FUNCTION [utility].[CalculatePassed]
(
	@PassFraction decimal(4,3),
	@RuleID int
)
RETURNS bit
AS
BEGIN
	DECLARE @Passed bit--,
			--@Threshold decimal(3,3)

	--SELECT @Threshold = Threshold from quality.[Rule] where ID = @QualityRuleID

	select	top 1
			@Passed = case 
						when @PassFraction >= Threshold then cast(1 as bit)
						else cast(0 as bit)
					end
	from	[Rule] 
	where	ID = @RuleID

	RETURN @Passed
END