CREATE procedure [utility].[GetArtifactsUpForCertification]
as
begin
	set nocount on;
	declare @artifactTypes table (RowID int identity, ID int)
	declare @vocabularies table (RowID int identity, ID int)

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
			@minDate datetime = '1900-01-01 00:00:00.000',
			@DateFieldExists bit = 0

	-- 1. CHECK ARTIFACT TYPES -------------------------------------
	-- get the artifact types that need to be checked
	insert into @artifactTypes
		select	T.ID
		from	ArtifactType T
				inner join WorkflowTypeRelation R on R.[Object] = 'ArtifactType' and R.ObjectID = T.ID and R.WorkflowType = @wt and R.[Enabled] = 1
--update Artifact set DateLastCertified = null, Status = 'Draft' where ID = 16109
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
				@calculationDate = Fields.value('(/fields/DateForScheduleCalculation)[1]', 'datetime')
		from	WorkflowTypeRelation
		where	[Object] = 'ArtifactType' and ObjectID = @id and WorkflowType = @wt
		/*
		select	Fields.value('(/fields/CertificationStartDate)[1]', 'datetime'),
				Fields.value('(/fields/CertificationEndDate)[1]', 'datetime'),
				Fields.value('(/fields/MonthsUntilCertification)[1]', 'int'),
				Fields.value('(/fields/DaysGivenToCompleteCertification)[1]', 'int'),
				Fields.value('(/fields/DateForScheduleCalculation)[1]', 'datetime')
		from	WorkflowTypeRelation
		where	ObjectType = 'ArtifactType' and ObjectID = 1 and WorkflowType = 2
		*/

		if @end is null
		begin
			set @end = @minDate
		end

--select DATEADD(d, -60, '2015-07-31 00:00:00.000')

		set @calculationDateMinusDaysBefore = DATEADD(d, -@days, @calculationDate)
		select @difMonths = DATEDIFF(mm, @calculationDateMinusDaysBefore, getutcdate())

		if (@difMonths >= @months) and (@difMonths % @months = 0) and (DATEDIFF(mm, @end, getutcdate()) >= @months)
		begin
			set @start = CAST(CAST(year(getutcdate()) AS varchar) + '-' + CAST(month(getutcdate()) AS varchar) + '-' + CAST(day(@calculationDateMinusDaysBefore) AS varchar) AS DATETIME) --CONVERT(date, getutcdate())
			set @end = CAST(CAST(year(getutcdate()) AS varchar) + '-' + CAST(month(getutcdate()) AS varchar) + '-' + CAST(day(@calculationDate) AS varchar) AS DATETIME) --DATEADD(d, @days, CONVERT(date, getutcdate()))

			select	@DateFieldExists = Fields.exist('fields/CertificationStartDate')
			from	WorkflowTypeRelation
			where	[Object] = 'ArtifactType' and ObjectID = @id and WorkflowType = @wt

			if @DateFieldExists = 1
			begin
				update	WorkflowTypeRelation
				set		Fields.modify('delete (/fields/CertificationStartDate)[1]')
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
				set		Fields.modify('delete (/fields/CertificationEndDate)[1]')
				where	[Object] = 'ArtifactType' and ObjectID = @id and WorkflowType = @wt
			end

			update	WorkflowTypeRelation
			set		Fields.modify('insert <CertificationEndDate>{sql:variable("@end")}</CertificationEndDate> into (/fields)[1]')
			where	[Object] = 'ArtifactType' and ObjectID = @id and WorkflowType = @wt
		end

		-- Increment
		set @current = @current + 1
	end

	-- clear the artifact types to prepare for vocabulary loop

	--update	WorkflowTypeRelation
	--set		Fields.modify('insert <CertificationStartDate>2015-06-01T00:00:00</CertificationStartDate> into (/fields)[1]')
	--where	[Object] = 'ArtifactType' and ObjectID = 1 and WorkflowType = 2

	--update	WorkflowTypeRelation
	--set		Fields.modify('insert <CertificationEndDate>2015-07-31T00:00:00</CertificationEndDate> into (/fields)[1]')
	--where	[Object] = 'ArtifactType' and ObjectID = 1 and WorkflowType = 2

	-- 2. CHECK VOCABULARIES ---------------------------------------
	-- get the vocabularies that need to be checked
	insert into @vocabularies
		select	ID
		from	TaxonomyType

	set @current = 1
	select @max = MAX(RowID) from @vocabularies
	while @current <= @max
	begin
		-- set to default
		set @start = null
		set @end = null
		set @months = null
		set @days = null
	
		select @id = ID from @vocabularies where RowID = @current

		select	@start = Fields.value('(/fields/CertificationStartDate)[1]', 'datetime'),
				@end = Fields.value('(/fields/CertificationEndDate)[1]', 'datetime'),
				@months = Fields.value('(/fields/MonthsUntilCertification)[1]', 'int'),
				@days = Fields.value('(/fields/DaysGivenToCompleteCertification)[1]', 'int'),
				@calculationDate = Fields.value('(/fields/DateForScheduleCalculation)[1]', 'datetime')
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
		
		if (@difMonths >= @months) and (@difMonths % @months = 0) and (DATEDIFF(mm, @end, getutcdate()) >= @months)
			begin
				set @start = CAST(CAST(year(getutcdate()) AS varchar) + '-' + CAST(month(getutcdate()) AS varchar) + '-' + CAST(day(@calculationDateMinusDaysBefore) AS varchar) AS DATETIME)
				set @end = CAST(CAST(year(getutcdate()) AS varchar) + '-' + CAST(month(getutcdate()) AS varchar) + '-' + CAST(day(@calculationDate) AS varchar) AS DATETIME)

				select	@DateFieldExists = Fields.exist('fields/CertificationStartDate')
				from	WorkflowTypeRelation
				where	[Object] = 'TaxonomyType' and ObjectID = @id and WorkflowType = @wt

				if @DateFieldExists = 1
				begin
					update	WorkflowTypeRelation
					set		Fields.modify('delete (/fields/CertificationStartDate)[1]')
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
					set		Fields.modify('delete (/fields/CertificationEndDate)[1]')
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
	select	A.ID as ArtifactID,
			coalesce(V.Fields.value('(/fields/CertificationStartDate)[1]', 'datetime'), T.Fields.value('(/fields/CertificationStartDate)[1]', 'datetime')) as CertificationStartDate,
			coalesce(V.Fields.value('(/fields/CertificationEndDate)[1]', 'datetime'), T.Fields.value('(/fields/CertificationEndDate)[1]', 'datetime')) as CertificationEndDate
	from	Artifact A
			inner join WorkflowTypeRelation T on T.[Object] = 'ArtifactType' and T.ObjectID = A.ArtifactTypeID and T.WorkflowType = @wt and T.[Enabled] = 1  and T.Parent is null and T.ParentID is null
			left join WorkflowTypeRelation V on V.[Object] = 'ArtifactType' and V.ObjectID = A.ArtifactTypeID and V.WorkflowType = @wt and V.[Enabled] = 1 and V.Parent = 'TaxonomyType' and V.ParentID = A.TaxonomyTypeID
	where	(
			A.DateLastCertified is null 
			or A.DateLastCertified < coalesce(V.Fields.value('(/fields/CertificationStartDate)[1]', 'datetime'), T.Fields.value('(/fields/CertificationStartDate)[1]', 'datetime'))
			)
			and A.Status <> 'Archived'
			and coalesce(V.Fields.value('(/fields/CertificationStartDate)[1]', 'datetime'), T.Fields.value('(/fields/CertificationStartDate)[1]', 'datetime')) is not null
			and A.ID not in (
						select	Data.value('(/fields/ArtifactID)[1]', 'int')
						from	Workflow
						where	WorkflowType = @wt 
								and Data.value('(/fields/StartDate)[1]', 'datetime') between 
									coalesce(V.Fields.value('(/fields/CertificationStartDate)[1]', 'datetime'), T.Fields.value('(/fields/CertificationStartDate)[1]', 'datetime'))
									and coalesce(V.Fields.value('(/fields/CertificationEndDate)[1]', 'datetime'), T.Fields.value('(/fields/CertificationEndDate)[1]', 'datetime'))
								 --and DateCompleted is null
						)
			and A.ID in (
						select	RD.ObjectID 
						from	ResponsibilityDetail RD
								inner join WorkflowTypeRelationResponsibilityType WTR on RD.[ObjectType] = 'Artifact' 
																						and WTR.ObjectType = 'ArtifactType' 
																						and WTR.ObjectID = RD.ObjectTypeID
																						and WTR.WorkflowType = @wt 
																						and WTR.ResponsibilityTypeID = RD.ResponsibilityTypeID
						)

end
