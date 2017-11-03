CREATE PROCEDURE [dbo].[GetScoreHistoryByObject]-- 'Artifact', 733
--declare
	@type varchar(50),-- = 'Artifact',
	@id int-- = 4651
AS
begin
	--declare @DateStart date, 
	--		@DateEnd date

	--select	@DateEnd = max(Date),
	--		@DateStart = DATEADD(d, -30, max(Date))
	--from	Score
	--where	Object = @type 
	--		and ObjectID = @id
	--		and ScoreTypeID = 1
	
	select	EffectiveStartDate as [Date],
			cast(Value * 100 as int) as Score
	from	metrics.Score
	where	Object = @type 
			and ObjectID = @id
	union
	select	cast(getutcdate() as date) as [Date],
			cast(Value * 100 as int) as Score
	from	metrics.Score
	where	getutcdate() between EffectiveStartDate and EffectiveEndDate
			and Object = @type and ObjectID = @id
end

