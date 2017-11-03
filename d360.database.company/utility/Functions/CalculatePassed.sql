CREATE FUNCTION [utility].[CalculatePassed]
(
	@PassFraction decimal(4,3),
	@RuleImplementationID int
)
RETURNS bit
AS
BEGIN
	DECLARE @Passed bit

	select	top 1
			@Passed = case 
						when @PassFraction >= R.Threshold then cast(1 as bit)
						else cast(0 as bit)
					end
	from	RuleImplementation I
			inner join [Rule] R on I.ID = @RuleImplementationID and I.RuleID = R.ID

	RETURN @Passed
END