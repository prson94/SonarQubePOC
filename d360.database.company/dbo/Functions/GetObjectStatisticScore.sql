CREATE FUNCTION [dbo].[GetObjectStatisticScore]
(
--declare
	@type varchar(25) = 'Resource',
	@id int = 1
)
RETURNS int
AS
BEGIN
	declare @score int

	select	@score = cast(Value * 100 as int)
	from	metrics.Score
	where	getutcdate() between EffectiveStartDate and EffectiveEndDate
			and Object = @type and ObjectID = @id
	return @score
END
