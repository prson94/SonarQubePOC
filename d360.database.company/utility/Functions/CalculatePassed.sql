CREATE FUNCTION [utility].[CalculatePassed]
(
	@PassFraction decimal(4,3),
	@RuleID int
)
RETURNS bit
AS
BEGIN
	DECLARE @Passed bit

	select	top 1
			@Passed = case 
						when @PassFraction >= Threshold then cast(1 as bit)
						else cast(0 as bit)
					end
	from	[Rule] 
	where	ID = @RuleID

	RETURN @Passed
END