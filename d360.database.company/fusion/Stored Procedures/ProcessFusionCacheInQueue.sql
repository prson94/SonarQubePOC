CREATE PROCEDURE [fusion].[ProcessFusionCacheInQueue]
--declare
	@FusionID int
--set @FusionID = 15
as
begin
	SET NOCOUNT, ANSI_PADDING ON;
	SET ANSI_WARNINGS ON;

	UPDATE  FusionAttribute
	SET		TextPath = utility.GetBreadcrumbStringWrapper('FusionAttribute', ID, '.')
	FROM	FusionAttribute 
	WHERE	FusionID = @FusionID and deleted = 0

	-- do any fusion specific processing
	declare @fusionTypeId int;

	select 
		@fusionTypeId = fusiontypeid
	from
		fusion
	where
		id = @FusionID;

	if @fusionTypeId = 13
	begin		
		exec fusion.GenerateMarkitMapLineageData @FusionID
	end
	else if @fusionTypeId = 16
	begin
		exec [fusion].[GenerateEagleLineageData] @FusionID
	end
end