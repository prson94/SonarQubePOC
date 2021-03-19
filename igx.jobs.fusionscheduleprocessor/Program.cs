using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace igx.jobs.fusionscheduleprocessor
{
    class Program
    {
		static async Task Main()
		{
			var builder = CoreFunction.JobHostConfigBuilder();
			builder.ConfigureWebJobs(c =>
			{
				c.AddAzureStorageCoreServices()
				.AddAzureStorage()
				.AddTimers();
			});

			using (var host = builder.Build())
			{
				await host.RunAsync();
			}
		}
	}

    public static class FusionScheduleProcessor
    {
        const string functionName = "Fusion_ProcessSchedule";
        const string timerSettings = "0 */1 * * * *";
        //const string timerSettings = "*/10 * * * * *";

        public static void Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log)
        {
            try
            {
                var companies = CoreFunction.GetCompaniesByCurrentSlot(true);

                #region Sql

                var sql = @"
	declare @FusionIDs table (ID int identity, FusionID int)
	insert into @FusionIDs
		select ID from Fusion WHERE Enabled = 1 and Manual = 0

	declare @current int,
			@max int,
			@FusionID int,
			@DateStarted datetime,
			@DateCompleted datetime,
			@LastRunComplete bit,
			@IntervalType int,
			@Interval int,
            @FullRefresh bit,
			@MinDateJobMustStartNext datetime,
			@ShouldTriggerJob bit

	select	@current = 1,
			@max = MAX(ID)
	from	@FusionIDs

	delete FusionStatusLog where Success = 0 and DateStarted < DATEADD(hh, -6, getutcdate()) and MachineQueuedOn is not null

	while	@current <= @max
	begin
		select	@FusionID = F.ID,
                @FullRefresh = coalesce(F.ForceRefresh, 0),
				@IntervalType = F.IntervalType,
				@DateStarted = S.DateStarted,
				@DateCompleted = C.DateCompleted,
				@Interval = F.Interval
		from	Fusion F
				inner join @FusionIDs I on I.FusionID = F.ID and I.ID = @current
				outer apply (
							select	MAX(DateStarted) as DateStarted
							from	FusionStatusLog 
							where	FusionID = F.ID
							) S
				outer apply (
							select	DateCompleted
							from	FusionStatusLog 
							where	FusionID = F.ID
									and DateStarted = S.DateStarted
							) C
			set @LastRunComplete = case 
									when @DateStarted is not null and @DateCompleted is not null then 1
									else 0
								   end
	
		if (@DateStarted is null or @LastRunComplete = 1)
		begin
			if @DateCompleted is not null
			begin
				-- Get the next date when the job should run, based on the previous completed date, plus the interval.
				set @MinDateJobMustStartNext = case @IntervalType
													when 4 then DATEADD(s, @Interval, @DateCompleted)		-- SECOND
													when 3 then DATEADD(n, @Interval, @DateCompleted)		-- MINUTE
													when 2 then DATEADD(hh, @Interval, @DateCompleted)		-- HOUR
													else DATEADD(d, @Interval, @DateCompleted)				-- DAY = 1
												end
				set @ShouldTriggerJob = case 
											when DATEDIFF(s, @MinDateJobMustStartNext, getutcdate()) > 0 then 1
											else 0
										end
			end
		
			if @DateStarted is null
			begin
				-- Job has never been triggered, so force an execution immediately.
				set @ShouldTriggerJob = 1
			end
			
			if @ShouldTriggerJob = 1
			begin
				select	@FusionID, @IntervalType, @DateStarted, @DateCompleted, @Interval, @LastRunComplete
				insert into		FusionStatusLog
								(ID,		FusionID,		DateStarted,	Success,    FullRefresh)
				values			(newid(),	@FusionID,		getutcdate(),	0,          @FullRefresh)
			end
		end

		set @current = @current + 1
	end";

                #endregion

                companies.ForEach(c =>
                {
                    try
                    {
                        var company = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password);
                        company.Open();
						company.Execute(sql);
                        company.Close();
                        company.Dispose();
                    }
                    catch (Exception ex)
                    {
                        CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                        CoreFunction.AIFlush();
                        //log.Error($"Company [{c.CompanyID}]: [{ex.GetFullExceptionData()}]");
                    }
                });
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
                CoreFunction.AIFlush();
                //log.Error($"General Exception: {ex.GetFullExceptionData()}");
            }
        }
    }
}
