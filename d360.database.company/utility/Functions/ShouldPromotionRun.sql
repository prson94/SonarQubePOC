CREATE FUNCTION [utility].[ShouldPromotionRun]
(
)
RETURNS bit
AS
BEGIN	
	DECLARE @lastPromotionRun datetime;
	
	-- if there are no enabled rules then say no
	if not exists(select 1 from [dbo].[fusionattributepromotionrule] where [Enabled] = 1)
	begin
		return 0;
	end;

	-- GET LAST RUN OF THE PROMOTON PROCESS FROM DBO.FUSIONATTRIBUTEPROMOTIONLOGSUMMARY
	select @lastPromotionRun = max(DateStarted) from [DBO].[FusionAttributePromotionLogSummary]
		
	if(@lastPromotionRun is null)
	begin
	 set @lastPromotionRun = '1970-01-01';
	end;

	-- promotion should not run if there is a current job out there that has not completed and this job was started within the last day
	if exists (select 1 from fusionattributepromotionlogsummary where datecompleted is null and datestarted > DATEADD(day,-1,CURRENT_TIMESTAMP))
	begin
		return 0; --should not run already running 
	end;

	--PROMOTION ONLY NEEDS TO RUN IF
	-- FUSION HAS COMPLETED ON A FUSION ID THAT HAS RULES SETUP AGAINST IT	
	if exists (select 1 from [fusion].[execution] fe inner join [dbo].[fusionattributepromotionrule] fapr on (fapr.fusionid = fe.fusionid)	where fapr.[enabled] = 1 and fe.datecompleted > @lastPromotionRun)
	begin		
		RETURN 1;		
	end;

	-- OR THE PROMOTION RULES HAVE BEEN MODIFIED, ADDED OR DELETED SINCE LAST RUN OF PROMOTION	
	if exists (select 1 from [dbo].[fusionattributepromotionrule] where updatedon > @lastPromotionRun)
	begin
		return 1;
	end;
		
	RETURN 0;
END
