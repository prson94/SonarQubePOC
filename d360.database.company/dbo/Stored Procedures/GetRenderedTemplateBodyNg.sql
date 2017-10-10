CREATE PROCEDURE [dbo].[GetRenderedTemplateBodyNg]-- 'Tooltip', 'Resource', 2, 'Preview'
--declare
	@TemplateType varchar(25),
	@Type varchar(50),
	@ID int,
	@Action varchar(50),
	@SubjectName VARCHAR (200) = 'Governing Domain'
--set @TemplateType = 'Lookup'
--set @Type = 'Artifact'
--set @ID = 7004--16435
--set @Action = 'Preview'--'Certificate'
AS
BEGIN
	SET NOCOUNT ON;

	declare @html nvarchar(max),
			@link nvarchar(2500),
			@icon nvarchar(250),
			@hasDynamicFields bit = 0,
			@hasStats bit = 0,
			@typeID int,

			@showIcon bit = 1,

			@current int,
			@max int,
			@name nvarchar(250),
			@value nvarchar(max);

	declare @tbl table (ID int identity, Name nvarchar(250), Value nvarchar(max));

	if @TemplateType = 'Tooltip'
	begin
		select	@html = TemplateBody
		from	TooltipTemplate
		where	Name = @Type
				and [Action] = @Action
	end

	-- Get the static tokens, depending on the type.
	declare @n nvarchar(250), @t nvarchar(250), @s nvarchar(25), @v int, @dc datetime, @du datetime, @d nvarchar(4000);
		
	-- Get common fields
	select	@typeID = ObjectTypeID,
			@icon = '<div title=''' + ObjectTypeName + ''' class=''tooltip-icon'' style=''background-color: ' + IconBackColor + '; color: ' + IconForeColor + '''><i class=''fa fa-' + IconText + '''></i></div>',
			@n = Name,
			@t = ObjectTypeName,
			@d = Description,
			@link = NgUrl
	from	cache.ObjectDetails
	where	[Object] = @Type
			and ObjectID = @ID;

	--fusion attributes arent in cache
	if @Type = 'FusionAttribute'
	begin		
		select 
			@typeID = fa.fusionattributetypeid,
			@n = fa.name,
			@t = fat.Name,
			@link = dbo.GenerateNgObjectUrl('FusionAttribute', fat.id, fa.id) 
		from fusionattribute fa 
			inner join fusionattributetype fat on (fa.fusionattributetypeid = fat.id) 
		where fa.id = @ID
	end

	if @n is not null
	begin
		if @link is null
		begin
			insert into @tbl values ('Name', @n)
		end
		else
		begin
			insert into @tbl values ('Name', '<a routerLink="/' + @link + '">' + @n + '</a>')
		end
		insert into @tbl values ('Description', @d)
	end
	insert into @tbl values ('Type', @t)

	if @Action = 'AssigningItemPreview'
	begin
		set @html = '<h3>{Name}</h3>'
	end

	
	if @Action = 'LookupPreview'
	begin
		set @html = '{Items}'
		
		if @Type = 'FusionAttribute'
		begin
			-- BUILD LIST HTML -----------------------------------------
			declare @fusionAttributeItemsHtml nvarchar(max)

			set @fusionAttributeItemsHtml = '<div style="height: 200px; overflow-y: scroll"><table class="hoverable bordered striped" style="width:100%"><thead>'
			set @fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '<th style="margin-right: 15px">Name</th>'
			set @fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '</thead><tbody>'

			select		--top 10 
						@fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '<tr>' 
											+ '<td>' + Name + '</td>'
											+ '</tr>'
			from		FusionAttribute
			where		ParentID = @ID
			order by	Name asc

			set @fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '</tbody>'
			set @fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '</table></div>'
 
			insert into @tbl values ('Items', @fusionAttributeItemsHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'LookupType' OR @Type = 'Lookup'
		begin
			-- BUILD LOOKUP LIST HTML -----------------------------------------
			declare @lookups table (RowID int identity, ID int)

			declare @MyLookupTypeID int
			if @Type = 'Lookup'
				begin
					select @MyLookupTypeID = LookupTypeID from [Lookup] where ID = @ID 
				end
			else
				begin
					set @MyLookupTypeID = @ID
				end

			insert into @lookups 
				select top 10 ID from [Lookup] where LookupTypeID = @MyLookupTypeID order by ID desc
		
			declare @lookupFieldTypes table (ID int identity, Name nvarchar(250))
			insert into @lookupFieldTypes
				select FriendlyName from FieldType where [Object] = 'LookupType' and ObjectID = @MyLookupTypeID order by SortOrder asc

			declare @lookupHtml nvarchar(max)

			set @lookupHtml = '<table class="hoverable bordered striped" style="width:100%">'

			-- Loop through field name list ---------
			set @lookupHtml = @lookupHtml + '<thead>'
			set		@current = 1
			select	@max = max(ID) from @lookupFieldTypes
			while @current <= @max
			begin
				select	@name = Name
				from	@lookupFieldTypes
				where	ID = @current

				set @lookupHtml = @lookupHtml + '<th style="margin-right: 15px">' + @name  + '</th>'

				set @current = @current + 1
			end
			set @lookupHtml = @lookupHtml + '</thead>'
			-----------------------------------------

			set @lookupHtml = @lookupHtml + '<tbody>'

			-- Loop through event list --------------
			select	@current = min(RowID) from @lookups
			select	@max = max(RowID) from @lookups

			while @current <= @max
			begin
				set @lookupHtml = @lookupHtml + '<tr>'	-- Open row for selected event.

				declare @lookupFields table (Name nvarchar(250), Value nvarchar(4000))
			
				declare @lookupID int

				select	@lookupID = ID from @lookups where RowID = @current

				insert into @lookupFields
					select		FriendlyName,
								FormattedValue
					from		FieldWithRelation
					where		ObjectType = 'Lookup' 
								and ObjectID = @lookupID

					-- Loop through each field for this selected event --
					declare @lfCurrent int,
							@lfMax int
					set		@lfCurrent = 1
					select	@lfMax = max(ID) from @lookupFieldTypes
					while @lfCurrent <= @lfMax
					begin
						select	@name = Name from @lookupFieldTypes where ID = @lfCurrent

						select @lookupHtml = @lookupHtml + '<td>' + coalesce(Value, '') + '</td>' from @lookupFields where Name = @name

						set @lfCurrent = @lfCurrent + 1
					end
					-----------------------------------------------------

				delete @lookupFields

				set @lookupHtml = @lookupHtml + '</tr>'	-- Close off row for selected lookup.

				set @current = @current + 1
			end
			-----------------------------------------

			set @lookupHtml = @lookupHtml + '</tbody>'

			set @lookupHtml = @lookupHtml + '</table>'

			insert into @tbl values ('Items', @lookupHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'ReferenceItemType' OR @Type = 'ReferenceItem'
		begin
			-- BUILD LOOKUP LIST HTML -----------------------------------------
			declare @refs table (RowID int identity, ID int)

			declare @MyRefTypeID int
			if @Type = 'ReferenceItem'
				begin
					select @MyRefTypeID = ReferenceItemTypeID from ReferenceItem where ID = @ID 
				end
			else
				begin
					set @MyRefTypeID = @ID
				end

			insert into @refs 
				select top 500 ID from [ReferenceItem] where ReferenceItemTypeID = @MyRefTypeID order by DisplayValue asc
		
			declare @refFieldTypes table (ID int identity, Name nvarchar(250))
			insert into @refFieldTypes values ('Code')
			insert into @refFieldTypes
				select FriendlyName from FieldType where [Object] = 'ReferenceItemType' and ObjectID = @MyRefTypeID order by SortOrder asc

			declare @refHtml nvarchar(max)

			set @refHtml = '<table class="hoverable bordered striped" style="width:100%; min-width: 400px">'

			-- Loop through field name list ---------
			set @refHtml = @refHtml + '<thead>'
			set		@current = 1
			select	@max = max(ID) from @refFieldTypes
			while @current <= @max
			begin
				select	@name = Name
				from	@refFieldTypes
				where	ID = @current

				set @refHtml = @refHtml + '<th style="margin-right: 15px">' + @name  + '</th>'

				set @current = @current + 1
			end
			set @refHtml = @refHtml + '</thead>'
			-----------------------------------------

			set @refHtml = @refHtml + '<tbody>'

			-- Loop through event list --------------
			select	@current = min(RowID) from @refs
			select	@max = max(RowID) from @refs

			while @current <= @max
			begin
				set @refHtml = @refHtml + '<tr>'	-- Open row for selected event.

				declare @refFields table (Name nvarchar(250), Value nvarchar(4000))
			
				declare @refID int

				select	@refID = ID from @refs where RowID = @current

				insert into @refFields
					select	'Code', Code from ReferenceItem where ID = @refID

				insert into @refFields
					select		FriendlyName,
								FormattedValue
					from		FieldWithRelation
					where		ObjectType = 'ReferenceItem' 
								and ObjectID = @refID

					-- Loop through each field for this selected event --
					declare @rfCurrent int,
							@rfMax int,
							@rfCurrentVal nvarchar(max);

					set		@rfCurrent = 1
					select	@rfMax = max(ID) from @refFieldTypes
					while @rfCurrent <= @rfMax
					begin
						select	@name = Name from @refFieldTypes where ID = @rfCurrent

						if exists (select 1 from @refFields where Name = @name)
						begin
							select @refHtml = @refHtml + '<td>' + coalesce(Value, '1') + '</td>' from @refFields where Name = @name;
						end
						else
						begin
							set @refHtml = @refHtml + '<td>&nbsp;</td>';
						end

						set @rfCurrent = @rfCurrent + 1
					end
					-----------------------------------------------------

				delete @refFields

				set @refHtml = @refHtml + '</tr>'	-- Close off row for selected lookup.

				set @current = @current + 1
			end
						
			-----------------------------------------

			set @refHtml = @refHtml + '</tbody>'

			set @refHtml = @refHtml + '</table>'

			if @max >= 500
			begin
				set @refHtml = @refHtml + '<div style="font-weight:bold;padding-top:10px">Showing top 500 items</div>'	
			end

			insert into @tbl values ('Items', @refHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'Resource' OR @Type = 'ResourceType'
		begin
			-- BUILD Resource LIST HTML -----------------------------------------
			declare @resourceItemsHtml nvarchar(max)

			set @resourceItemsHtml = '<table class="hoverable bordered striped" style="width:100%"><thead>'
			set @resourceItemsHtml = @resourceItemsHtml + '<th style="margin-right: 15px">First Name</th><th style="margin-right: 15px">Last Name</th><th>Email</th>'
			set @resourceItemsHtml = @resourceItemsHtml + '</thead><tbody>'

			select		top 10 
						@resourceItemsHtml = @resourceItemsHtml + '<tr>' + 
											'<td>' + FirstName + '</td>' + 
											'<td>' + LastName + '</td>' + 
											'<td>' + Email + '</td>'
											+ '</tr>'
			from		reporting.Global_Resource
			order by	LastName, FirstName asc

			set @resourceItemsHtml = @resourceItemsHtml + '</tbody>'
			set @resourceItemsHtml = @resourceItemsHtml + '</table>'
 
			insert into @tbl values ('Items', @resourceItemsHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'ReferenceItem'
		begin

			declare @myReferenceListID int

			select	@myReferenceListID = ReferenceItemTypeID from ReferenceItem where ID = @ID
			-- BUILD LIST HTML -----------------------------------------
			declare @referenceItemHtml nvarchar(max)

			set @referenceItemHtml = '<table class="hoverable bordered striped" style="width:100%">'
			set @referenceItemHtml = @referenceItemHtml + '<thead><th style="margin-right: 15px">Name</th></thead>'
			set @referenceItemHtml = @referenceItemHtml + '<tbody>'



			select		top 10 
						@referenceItemHtml = @referenceItemHtml + '<tr>' + '<td>' + DisplayValue + '</td>' + '</tr>'             
			from		ReferenceItem
			where		ReferenceItemTypeID = @myReferenceListID
			order by	DisplayValue desc

			set @referenceItemHtml = @referenceItemHtml + '</tbody>'
			set @referenceItemHtml = @referenceItemHtml + '</table>'
 
			insert into @tbl values ('Items', @referenceItemHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'ReferenceItemType'
		begin

		--	declare @myReferenceListID int

			--select	@myReferenceListID = ReferenceItemTypeID from ReferenceItem where ID = @ID
			-- BUILD LIST HTML -----------------------------------------
			declare @referenceItemTypeHtml nvarchar(max)

			set @referenceItemTypeHtml = '<table class="hoverable bordered striped" style="width:100%">'
			set @referenceItemTypeHtml = @referenceItemTypeHtml + '<thead><th style="margin-right: 15px">Display Value</th></thead>'
			set @referenceItemTypeHtml = @referenceItemTypeHtml + '<tbody>'



			select		top 10 
						@referenceItemTypeHtml = @referenceItemTypeHtml + '<tr>' + '<td>' + DisplayValue + '</td>' + '</tr>'             
			from		ReferenceItem
			where		ReferenceItemTypeID = @ID
			order by	DisplayValue desc

			set @referenceItemTypeHtml = @referenceItemTypeHtml + '</tbody>'
			set @referenceItemTypeHtml = @referenceItemTypeHtml + '</table>'
 
			insert into @tbl values ('Items', @referenceItemTypeHtml)
			------------------------------------------------------------------
		end;

	end
	
	if @Action = 'None'
	begin
		set @html = '<h3>{Name}</h3><div>'
	end

	if @Action = 'Preview'
	begin
		set @html = '<h3 style="positon: relative">{Name} <small style="background-color: #fff; float:right;font-size:65%;">{Type}</small></h3><div>{Description}</div>'
		set @showIcon = 0

		if @Type = 'Artifact'
		begin
			declare @artifactPathHtml nvarchar(2500) = '<table>';
			declare @artLevelResult table(ID int identity, LevelName nvarchar(250), DisplayValue nvarchar(250), Url varchar(1000));

			with ap as (
				select	O.ID,
						O.ParentID,
						O.DisplayValue,
						L.Name as LevelName,
						dbo.GenerateNgObjectUrl('Artifact', L.ID, O.ID) as Url,
						1 as [Level]
				from	Artifact O
						inner join ArtifactType L on L.ID = O.ArtifactTypeID
				where	O.ID = @ID
				union all
				select	O.ID,
						O.ParentID,
						O.DisplayValue,
						L.Name as LevelName,
						dbo.GenerateNgObjectUrl('Artifact', L.ID, O.ID) as Url,
						C.[Level] + 1 as [Level]
				from	Artifact O
						inner join ArtifactType L on L.ID = O.ArtifactTypeID
						inner join ap as C on C.ParentID = O.ID
			)

			insert into @artLevelResult
				select LevelName, DisplayValue, Url from ap order by [Level] desc

			select		@artifactPathHtml = coalesce(@artifactPathHtml + '', '') + '<tr><td style="width: 15px">' +  cast([ID] as varchar) + '</td><td>' +  LevelName + '</td><td><b><a href="' + Url + '">' + DisplayValue + '</a></b>' + '</td></tr>'
			from		@artLevelResult
			
			set @artifactPathHtml =  @artifactPathHtml + '</table>'

			set @html = @html + '<div><b>Path:</b></div><div>' + coalesce(@artifactPathHtml,'') + '</div>'

			set @hasDynamicFields = 1
		end;

		if @Type = 'FusionAttribute'
		begin
			declare @faPathHtml nvarchar(2500) = '<table>';
			declare @faLevelResult table(ID int identity, LevelName nvarchar(250), Name nvarchar(250));

			with fap as (
				select	O.ID,
						O.ParentID,
						O.Name,
						L.Name as LevelName,
						1 as [Level]
				from	FusionAttribute O
						inner join FusionAttributeType L on L.ID = O.FusionAttributeTypeID
				where	O.ID = @ID
				union all
				select	O.ID,
						O.ParentID,
						O.Name,
						L.Name as LevelName,
						C.[Level] + 1 as [Level]
				from	FusionAttribute O
						inner join FusionAttributeType L on L.ID = O.FusionAttributeTypeID
						inner join fap as C on C.ParentID = O.ID
			)

			insert into @faLevelResult
				select LevelName, Name from fap order by [Level] desc

			select		@faPathHtml = @faPathHtml + 
						'<tr><td colspan="2">Configuration</td><td><b><a href="/fusion/' + cast(F.ID as nvarchar) + '">' + coalesce(F.Name,'') + '</a></b></td></tr>' 
			from		Fusion F 
						inner join FusionAttribute A on A.FusionID = F.ID and A.ID = @ID

			select		@faPathHtml = coalesce(@faPathHtml + '', '') + '<tr><td style="width: 15px">' +  cast(ID as varchar) + '</td><td>' +  LevelName + '</td><td><b>' + Name + '</b>' + '</td></tr>'
			from		@faLevelResult

			set @faPathHtml =  @faPathHtml + '</table>'

			set @html = @html + '<div><b>Path:</b></div><div>' + coalesce(@faPathHtml,'') + '</div>'

			set @hasDynamicFields = 1
		end

		if @Type = 'Intersect'
		begin
			set @hasDynamicFields = 1
		end;

		
		if @Type = 'Issue'
		begin
			insert into @tbl values('Name', '')
			insert into @tbl values('Description', '')
					
			if exists (select id from issue where id = @ID)
			begin			
				set @html = @html + '<div><b>Issue Type:</b> {IssueType}</div>'
				set @html = @html + '<div><b>Criticality:</b> {Criticality}</div>'
						
				insert into @tbl 
					select 'IssueType', it.name 
					from issuetype it inner join issue i on(i.issuetypeid = it.id) 
					where i.id = @ID

				insert into @tbl 
					select 'Criticality', case when i.Criticality = 0 then 'Negligible' when i.Criticality = 1 then 'Low' when i.Criticality = 2 then 'Medium' when i.Criticality = 3 then 'High'  when i.Criticality = 4 then 'Critical' else 'N/A' end
					from issuetype it inner join issue i on(i.issuetypeid = it.id) 
					where i.id = @ID

				set @hasDynamicFields = 1
			end			
		end;

		if @Type = 'Responsibility'
		begin
			select	@n = T.Name, 
					@t = T.Name,
					@d = T.[Description]
			from	Responsibility O
					inner join ResponsibilityType T on T.ID = O.ResponsibilityTypeID
			where	O.ID = @ID
			
			declare @contextsHtml nvarchar(max)

			set @contextsHtml = '<table class="hoverable bordered striped" style="width:100%">' + 
								'<thead><th>List</th><th>Code</th></thead>' + 
								'<tbody>' + 
								(
								select		(select D.Name as 'td' for xml path(''), type),
											(select I.Code as 'td' for xml path(''), type)
								from		ResponsibilityContextItem R
											inner join ReferenceItem I on R.ResponsibilityID = @ID and R.ObjectType = 'ReferenceItem' and I.ID = R.ObjectID
											inner join ReferenceItemType D on D.ID = I.ReferenceItemTypeID
								FOR XML RAW('tr'), ELEMENTS
								) +
								'</tbody>' + 
								'</table>'

			insert into @tbl values ('Name', @n)
			insert into @tbl values ('Type', @t)
			insert into @tbl values ('Description', @d)
			insert into @tbl values ('Contexts', @contextsHtml)

			set @html = @html + '<div><b>Contexts:</b> {Contexts}</div>'
		end;

		if @Type = 'Resource'
		begin
			--declare @e nvarchar(500), @fn nvarchar(250), @ln nvarchar(250)
			--select	@e = Email, @fn = FirstName, @ln = LastName
			--from	reporting.Global_Resource
			--where	ResourceID = @ID

			--insert into @tbl values ('Email', @e)
			--insert into @tbl values ('FirstName', @fn)
			--insert into @tbl values ('LastName', @ln)
			--insert into @tbl values ('Role', '')

			--set @html = @html + '<div><b>Email:</b> {Email}</div>'
			--set @html = @html + '<div><b>First Name:</b> {FirstName}</div>'
			--set @html = @html + '<div><b>Last Name:</b> {LastName}</div>'

			set @hasDynamicFields = 1
		end;

		if @Type = 'Rule'
		begin
			insert into @tbl
				select	'Name', DisplayValue
				from	[Rule] O
				where	ID = @ID

			set @hasDynamicFields = 1
		end;

		if @Type = 'RuleDimension'
		begin
			insert into @tbl
				select	'Description', [Description]
				from	RuleDimension
				where	ID = @ID
			insert into @tbl
				select	'Name', [Name]
				from	RuleDimension
				where	ID = @ID

			--set @html = @html + '<div><b>Path:</b> {Description}</div>'
						
		end;

		if @Type = 'Taxonomy'
		begin
			declare @taxonomyPathHtml nvarchar(2500) = '<table>';

			with tp as (
				select	O.ID,
						O.ParentID,
						O.DisplayValue,
						coalesce(L.Name, 'Level ' + cast(O.[Level] as varchar)) as LevelName,
						O.[Level]
				from	[Taxonomy] O
						left join TaxonomyTypeLevel L on L.TaxonomyTypeID = O.TaxonomyTypeID and L.[Level] = O.[Level]
				where	O.ID = @ID
				union all
				select	O.ID,
						O.ParentID,
						O.DisplayValue,
						coalesce(L.Name, 'Level ' + cast(O.[Level] as varchar)) as LevelName,
						O.[Level]
				from	[Taxonomy] O
						outer apply (
									select Name from TaxonomyTypeLevel where TaxonomyTypeID = O.TaxonomyTypeID and [Level] = O.[Level]
									) L
						--left join TaxonomyTypeLevel L on L.TaxonomyTypeID = O.TaxonomyTypeID and L.[Level] = O.[Level]
						inner join tp as C on C.ParentID = O.ID
			)

			select		@taxonomyPathHtml = coalesce(@taxonomyPathHtml + '', '') + '<tr><td style="width: 15px">' +  cast([Level] as varchar) + '</td><td>' +  LevelName + '</td><td><b>' + DisplayValue + '</b>' + '</td></tr>'
			from		tp
			order by	[Level]
			
			set @taxonomyPathHtml =  @taxonomyPathHtml + '</table>'

			set @html = @html + '<div><b>Path:</b></div><div>' + coalesce(@taxonomyPathHtml,'') + '</div>'

			set @hasDynamicFields = 1
		end;

		if @Type = 'TaxonomyType'
		begin
			insert into @tbl
				select	'Name', Name
				from	TaxonomyType O
				where	ID = @ID

			set @hasDynamicFields = 1
		end;
		
		-- If required, get dynamic fields to add to list.
		if @hasDynamicFields = 1
		begin
			select	@html = @html + '<div><b>' + FriendlyName + '</b>: ' + '{' + Name + '}' + '</div>' 
			from	FieldWithRelation
			where	ObjectType = @Type
					and ObjectID = @ID
					and Name not in (select Name from @tbl)

			insert into @tbl
				select	Name,
						FormattedValue
				from	FieldWithRelation
				where	ObjectType = @Type
						and ObjectID = @ID
						and Name not in (select Name from @tbl)
		end;
	end

	if @Action = 'Statistics'
	begin
		set @html = '<h3>{Name}</h3><div>{Statistics}</div>'

		set @hasStats = case @Type
							when 'Artifact' then 1
							when 'Taxonomy' then 1
							else 0
						end

		-- If required, build statistics table
		if @hasStats = 1
		begin
			-- BUILD STATS LIST HTML -----------------------------------------
			declare @statsHtml nvarchar(max)

			declare @stats table (ID int identity, Name nvarchar(250), Score int)
			insert into @stats 
				select	T.Name,
						coalesce(S.SCore, 0) as Score
				from	StatisticType T
						outer apply (
									select	top 1
											*
									from	Statistic
									where	StatisticTypeID = T.ID
											and ObjectType = @Type
											and ObjectID = @ID
									order by DateStart desc
									) S
				where	T.[Object] = @Type + 'Type' 
						and T.ObjectID = @typeID
						and T.PartOfScore = 1

			set @statsHtml = '<table class="hoverable bordered striped" style="width:100%">'

			-- Loop through field name list ---------
			set @statsHtml = @statsHtml + '<tbody>'
			set		@current = 1
			select	@max = max(ID) from @stats
			while @current <= @max
			begin
				select	@statsHtml = @statsHtml + '<tr><td>' + Name  + '</td>' + '<td>' + cast(Score as varchar(5))  + ' Points</td></tr>'
				from	@stats
				where	ID = @current

				set @current = @current + 1
			end
			set @statsHtml = @statsHtml + '</tbody>'
			-----------------------------------------

			insert into @tbl values ('Statistics', @statsHtml)

			------------------------------------------------------------------
		end;
	end

	-- Replace the fields in the template with the appropriate text value.
	set		@current = 1
	select	@max = max(ID) from @tbl

	while @current <= @max
	begin
		select	@name = '{' + Name + '}',
				@value = COALESCE(Value, '')
		from	@tbl 
		where	ID = @current

		if @showIcon = 1
		begin
			if @name = '{Name}' and @icon is not null
			begin
				update	@tbl 
				set		Value = '<div class="pull-left" style="width: 30px">' + @icon + '</div>' + '<div class="pull-right">' + @value + '</div>'
				where	ID = @current
				--set @usedIconAlready = 1
			end
		end

		set @html = REPLACE(@html, @name, @value)

		set @current = @current + 1
	end

	--if @showIcon = 1 and @icon is not null
	--begin
	--	set @html = @icon + '<br/>' + @html
	--end

	set @html = '<div style="max-height: 500px; min-width: 400px; overflow-y: auto">' + @html + '</div>'

	-- Return the properly formatted values.
	select	'' as Title,
			@html as Body;
END
