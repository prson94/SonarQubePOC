CREATE FUNCTION [utility].[ShouldPromotionRun]
(
)
RETURNS bit
AS
BEGIN	
	DECLARE @lastPromotionRun datetime;
	
	-- if there are no enabled rules then say no
	if not exists(select 1 from [fusion].[Rule] where [Enabled] = 1)
	begin
		return 0;
	end;

	-- GET LAST RUN OF THE PROMOTON PROCESS FROM DBO.FUSIONATTRIBUTEPROMOTIONLOGSUMMARY
	select @lastPromotionRun = max(DateStarted) from fusion.RuleLog
		
	if(@lastPromotionRun is null)
	begin
	 set @lastPromotionRun = '1970-01-01';
	end;

	-- promotion should not run if there is a current job out there that has not completed and this job was started within the last day
	if exists (select 1 from fusion.RuleLog where DateCompleted is null and DateStarted > DATEADD(day,-1,CURRENT_TIMESTAMP))
	begin
		return 0; --should not run already running 
	end;

	--PROMOTION ONLY NEEDS TO RUN IF FUSION HAS COMPLETED ON A FUSION ID THAT HAS RULES SETUP AGAINST IT.
	if exists	(
				select	1 
				from	fusion.Execution E 
						inner join [fusion].[Rule] R on R.fusionid = E.fusionid	
				where	R.[enabled] = 1 
						and E.datecompleted > @lastPromotionRun
						and (E.Adds + E.Updates + E.Deletes) > 0
				)

	begin		
		RETURN 1;		
	end;

	-- OR THE PROMOTION RULES HAVE BEEN MODIFIED, ADDED OR DELETED SINCE LAST RUN OF PROMOTION	
	if exists (select 1 from fusion.[Rule] where UpdatedOn > @lastPromotionRun)
	begin
		return 1;
	end;
		
	RETURN 0;
END