CREATE procedure [dbo].[ProcessSchedule]
as
begin
	set nocount on;

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
			@MinDateJobMustStartNext datetime,
			@ShouldTriggerJob bit

	select	@current = 1,
			@max = MAX(ID)
	from	@FusionIDs

	delete FusionStatusLog where Success = 0 and DateStarted < DATEADD(hh, -6, getutcdate()) and MachineQueuedOn is not null

	while	@current <= @max
	begin
		select	@FusionID = F.ID,
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
	
		if @DateStarted is null or @LastRunComplete = 1
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
								(ID,		FusionID,		DateStarted,	Success)
				values			(newid(),	@FusionID,		getutcdate(),	0)
			end
		end

		set @current = @current + 1
	end
end

