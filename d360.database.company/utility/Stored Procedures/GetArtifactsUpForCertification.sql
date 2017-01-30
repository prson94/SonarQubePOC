CREATE procedure [utility].[GetArtifactsUpForCertification]
as
begin
	set nocount on;
	declare @artifactTypes table (RowID int identity, ID int)
	declare @subjectAreas table (RowID int identity, ID int)

	-- loop control variables
	declare @current int,
			@max int

	-- certification loop instance variables
	declare @wt int = 2,
			@id int,
			@start datetime,
			@end datetime,
			@months int,
			@days int,
			@calculationDate datetime,
			@difMonths int,
			@calculationDateMinusDaysBefore date,
			@lastStartDate datetime,
			@minDate datetime = '1900-01-01 00:00:00.000',
			@DateFieldExists bit = 0,
			@currentDate datetime = CAST(CAST(year(getutcdate()) AS varchar) + '-' + CAST(month(getutcdate()) AS varchar) + '-' + CAST(day(getutcdate()) AS varchar) AS DATETIME)

	-- 1. CHECK ARTIFACT TYPES -------------------------------------
	-- get the artifact types that need to be checked
	insert into @artifactTypes
		select	T.ID
		from	ArtifactType T
				inner join WorkflowTypeRelation R on R.[Object] = 'ArtifactType' and R.ObjectID = T.ID and R.WorkflowType = @wt and R.[Enabled] = 1

--select * from @artifactTypes

	set @current = 1
	select @max = MAX(RowID) from @artifactTypes
	while @current <= @max
	begin
		-- set to default
		set @start = null
		set @end = null
		set @months = null
		set @days = null

		select @id = ID from @artifactTypes where RowID = @current

		select	@start = Fields.value('(/fields/CertificationStartDate)[1]', 'datetime'),
				@end = Fields.value('(/fields/CertificationEndDate)[1]', 'datetime'),
				@months = Fields.value('(/fields/MonthsUntilCertification)[1]', 'int'),
				@days = Fields.value('(/fields/DaysGivenToCompleteCertification)[1]', 'int'),
				@calculationDate = Fields.value('(/fields/DateForScheduleCalculation)[1]', 'datetime'),
				@lastStartDate = Fields.value('(/fields/CertificationStartDate)[1]', 'datetime')
		from	WorkflowTypeRelation
		where	[Object] = 'ArtifactType' and ObjectID = @id and WorkflowType = @wt

		if @end is null
		begin
			set @end = @minDate
		end

--select DATEADD(d, -60, '2015-07-31 00:00:00.000')

		set @calculationDateMinusDaysBefore = DATEADD(d, -@days, @calculationDate)
		select @difMonths = DATEDIFF(mm, @calculationDateMinusDaysBefore, getutcdate())

--select	@id as ArtifactTypeID,
--		@calculationDate as CalculationDate,
--		@calculationDateMinusDaysBefore as CalculationDateMinusDaysBefore,
--		@difMonths as NumMonthsSinceLastCertification,
--		@months as NumMonthsBetweenCertifications,
--		@lastStartDate as LastStartDate

		if ((@difMonths >= @months) and (DATEDIFF(mm, @end, getutcdate()) >= @months) OR @lastStartDate is null) --or (@difMonths % @months = 0)
		begin
			set @start = CAST(CAST(year(getutcdate()) AS varchar) + '-' + CAST(month(getutcdate()) AS varchar) + '-' + CAST(day(@calculationDateMinusDaysBefore) AS varchar) AS DATETIME) --CONVERT(date, getutcdate())
			set @end = CAST(CAST(year(getutcdate()) AS varchar) + '-' + CAST(month(getutcdate()) AS varchar) + '-' + CAST(day(@calculationDate) AS varchar) AS DATETIME) --DATEADD(d, @days, CONVERT(date, getutcdate()))
--select @start, @end, DATEDIFF(d, @start, @end)

			if DATEDIFF(d, @start, @end) < @days
			begin
				set @start = @currentDate
				set @end = DATEADD(d, @days, @currentDate)
			end

			select	@DateFieldExists = Fields.exist('fields/CertificationStartDate')
			from	WorkflowTypeRelation
			where	[Object] = 'ArtifactType' and ObjectID = @id and WorkflowType = @wt

--select @start, @end
--select @DateFieldExists as DateFieldExists

			if @DateFieldExists = 1
			begin
				update	WorkflowTypeRelation
				set		Fields.modify('delete (/fields/CertificationStartDate)')
				where	[Object] = 'ArtifactType' and ObjectID = @id and WorkflowType = @wt
			end

			update	WorkflowTypeRelation
			set		Fields.modify('insert <CertificationStartDate>{sql:variable("@start")}</CertificationStartDate> into (/fields)[1]')
			where	[Object] = 'ArtifactType' and ObjectID = @id and WorkflowType = @wt

			select	@DateFieldExists = Fields.exist('fields/CertificationEndDate')
			from	WorkflowTypeRelation
			where	[Object] = 'ArtifactType' and ObjectID = @id and WorkflowType = @wt

			if @DateFieldExists = 1
			begin
				update	WorkflowTypeRelation
				set		Fields.modify('delete (/fields/CertificationEndDate)')
				where	[Object] = 'ArtifactType' and ObjectID = @id and WorkflowType = @wt
			end

			update	WorkflowTypeRelation
			set		Fields.modify('insert <CertificationEndDate>{sql:variable("@end")}</CertificationEndDate> into (/fields)[1]')
			where	[Object] = 'ArtifactType' and ObjectID = @id and WorkflowType = @wt
		end

		-- Increment
		set @current = @current + 1
	end

	-- 2. CHECK VOCABULARIES ---------------------------------------
	-- get the vocabularies that need to be checked
	insert into @subjectAreas
		select	T.ID
		from	TaxonomyType T
				inner join WorkflowTypeRelation R on R.[Object] = 'TaxonomyType' and R.ObjectID = T.ID and R.WorkflowType = @wt and R.[Enabled] = 1

	set @current = 1
	select @max = MAX(RowID) from @subjectAreas
	while @current <= @max
	begin
	--	-- set to default
		set @start = null
		set @end = null
		set @months = null
		set @days = null
	
		select @id = ID from @subjectAreas where RowID = @current

		select	@start = Fields.value('(/fields/CertificationStartDate)[1]', 'datetime'),
				@end = Fields.value('(/fields/CertificationEndDate)[1]', 'datetime'),
				@months = Fields.value('(/fields/MonthsUntilCertification)[1]', 'int'),
				@days = Fields.value('(/fields/DaysGivenToCompleteCertification)[1]', 'int'),
				@calculationDate = Fields.value('(/fields/DateForScheduleCalculation)[1]', 'datetime'),
				@lastStartDate = Fields.value('(/fields/CertificationStartDate)[1]', 'datetime')
		from	WorkflowTypeRelation
		where	[Object] = 'TaxonomyType' and ObjectID = @id and WorkflowType = @wt
	
		if @months is not null and @days is not null
		begin
			if @end is null
			begin
				set @end = @minDate
			end

		set @calculationDateMinusDaysBefore = DATEADD(d, -@days, @calculationDate)
		select @difMonths = DATEDIFF(mm, @calculationDateMinusDaysBefore, getutcdate())
		
		if ((@difMonths >= @months) and (DATEDIFF(mm, @end, getutcdate()) >= @months) OR @lastStartDate is null)
			begin
				set @start = CAST(CAST(year(getutcdate()) AS varchar) + '-' + CAST(month(getutcdate()) AS varchar) + '-' + CAST(day(@calculationDateMinusDaysBefore) AS varchar) AS DATETIME)
				set @end = CAST(CAST(year(getutcdate()) AS varchar) + '-' + CAST(month(getutcdate()) AS varchar) + '-' + CAST(day(@calculationDate) AS varchar) AS DATETIME)

				if DATEDIFF(d, @start, @end) < @days
				begin
					set @start = @currentDate
					set @end = DATEADD(d, @days, @currentDate)
				end

				select	@DateFieldExists = Fields.exist('fields/CertificationStartDate')
				from	WorkflowTypeRelation
				where	[Object] = 'TaxonomyType' and ObjectID = @id and WorkflowType = @wt

				if @DateFieldExists = 1
				begin
					update	WorkflowTypeRelation
					set		Fields.modify('delete (/fields/CertificationStartDate)')
					where	[Object] = 'TaxonomyType' and ObjectID = @id and WorkflowType = @wt
				end

				update	WorkflowTypeRelation
				set		Fields.modify('insert <CertificationStartDate>{sql:variable("@start")}</CertificationStartDate> into (/fields)[1]')
				where	[Object] = 'TaxonomyType' and ObjectID = @id and WorkflowType = @wt

				select	@DateFieldExists = Fields.exist('fields/CertificationEndDate')
				from	WorkflowTypeRelation
				where	[Object] = 'TaxonomyType' and ObjectID = @id and WorkflowType = @wt

				if @DateFieldExists = 1
				begin
					update	WorkflowTypeRelation
					set		Fields.modify('delete (/fields/CertificationEndDate)')
					where	[Object] = 'TaxonomyType' and ObjectID = @id and WorkflowType = @wt
				end

				update	WorkflowTypeRelation
				set		Fields.modify('insert <CertificationEndDate>{sql:variable("@end")}</CertificationEndDate> into (/fields)[1]')
				where	[Object] = 'TaxonomyType' and ObjectID = @id and WorkflowType = @wt
			end
		end

		-- Increment
		set @current = @current + 1
	end

	-- 3. CHECK ARTIFACTS ------------------------------------------
--declare @wt int =2
	select	A.ID as ArtifactID,
--A.ArtifactTypeID,
--W.DateStarted,
			coalesce(V.Fields.value('(/fields/CertificationStartDate/text())[1]', 'datetime'), T.Fields.value('(/fields/CertificationStartDate/text())[1]', 'datetime')) as CertificationStartDate,
			coalesce(V.Fields.value('(/fields/CertificationEndDate/text())[1]', 'datetime'), T.Fields.value('(/fields/CertificationEndDate/text())[1]', 'datetime')) as CertificationEndDate
	from	Artifact A
			left join WorkflowTypeRelation T on T.[Object] = 'ArtifactType' and T.ObjectID = A.ArtifactTypeID and T.WorkflowType = @wt and T.[Enabled] = 1  and T.Parent is null and T.ParentID is null
			left join WorkflowTypeRelation V on V.[Object] = 'ArtifactType' and V.ObjectID = A.ArtifactTypeID and V.WorkflowType = @wt and V.[Enabled] = 1 and V.Parent = 'TaxonomyType' and V.ParentID = A.TaxonomyTypeID
			outer apply (
						select	max(DateStarted) as DateStarted
						from	Workflow
						where	artifactID = A.ID
								--and DateCompleted is null
						) W
	where	(
				W.DateStarted is null
				or
				(
					W.DateStarted is not null 
					and
					DATEDIFF(m, W.DateStarted, 
						coalesce(V.Fields.value('(/fields/CertificationStartDate/text())[1]', 'datetime'), T.Fields.value('(/fields/CertificationStartDate/text())[1]', 'datetime'))
					) > 0
				)
			)
			and
			(
				A.DateLastCertified is null 
				--or A.DateLastCertified < coalesce(V.Fields.value('(/fields/CertificationStartDate)[1]', 'datetime'), T.Fields.value('(/fields/CertificationStartDate)[1]', 'datetime'))
				or 
				(
					A.DateLastCertified is not null
					and DATEDIFF(m, 
						A.DateLastCertified, 
						coalesce(V.Fields.value('(/fields/CertificationStartDate/text())[1]', 'datetime'), T.Fields.value('(/fields/CertificationStartDate/text())[1]', 'datetime'))
					) > coalesce(V.Fields.value('(/fields/MonthsUntilCertification/text())[1]', 'int'), T.Fields.value('(/fields/MonthsUntilCertification/text())[1]', 'int'))
					and A.Status = 'Certified'
				)
				or A.Status <> 'Certified'
			)
			and A.Status <> 'Archived'
			and coalesce(V.Fields.value('(/fields/CertificationStartDate/text())[1]', 'datetime'), T.Fields.value('(/fields/CertificationStartDate/text())[1]', 'datetime')) is not null
			and A.ID not in (
							select	artifactid
							from	Workflow
							where	WorkflowType = @wt 
									and Data.value('(/fields/StartDate/text())[1]', 'datetime') between 
											coalesce(V.Fields.value('(/fields/CertificationStartDate/text())[1]', 'datetime'), T.Fields.value('(/fields/CertificationStartDate/text())[1]', 'datetime'))
											and coalesce(V.Fields.value('(/fields/CertificationEndDate/text())[1]', 'datetime'), T.Fields.value('(/fields/CertificationEndDate/text())[1]', 'datetime'))
							)
			and A.ID not in (
							select	ArtifactID
							from	Workflow
							where	WorkflowType = @wt 
									and DateCompleted is null
							)
			and A.ID not in (
							select	ArtifactID
							from	Workflow
							where	WorkflowType = @wt 
									and DATEDIFF(m, 
											DateStarted, 
											coalesce(V.Fields.value('(/fields/CertificationStartDate/text())[1]', 'datetime'), T.Fields.value('(/fields/CertificationStartDate/text())[1]', 'datetime'))
										) > coalesce(V.Fields.value('(/fields/MonthsUntilCertification/text())[1]', 'int'), T.Fields.value('(/fields/MonthsUntilCertification/text())[1]', 'int'))
							)
			and A.ID in (
						select	RD.ObjectID 
						from	[cache].[Responsibilities] RD
								left join WorkflowTypeRelation WTR_V on WTR_V.[Object] = 'ArtifactType' and WTR_V.ObjectID = RD.ObjectTypeID and WTR_V.WorkflowType = @wt and WTR_V.[Enabled] = 1 and WTR_V.ResponsibilityTypeID = RD.ResponsibilityTypeID and WTR_V.Parent = 'TaxonomyType' and WTR_V.ParentID = A.TaxonomyTypeID
								left join WorkflowTypeRelation WTR_T on WTR_T.[Object] = 'ArtifactType' and WTR_T.ObjectID = RD.ObjectTypeID and WTR_T.WorkflowType = @wt and WTR_T.[Enabled] = 1 and WTR_T.ResponsibilityTypeID = RD.ResponsibilityTypeID and WTR_T.Parent is null and WTR_T.ParentID is null
						where	RD.[Object] = 'Artifact' 
								and coalesce(WTR_V.ID, WTR_T.ID) is not null
						)

end