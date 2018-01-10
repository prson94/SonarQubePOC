CREATE FUNCTION GetWorkflowConditionLabels
(
	@conditions xml
)
RETURNS xml
AS
BEGIN
	declare @recordCount int;

	declare @results table (id int, FieldTypeID int, ValueType varchar(max), [Value] nvarchar(max), Operator varchar(max), VersionStepID int, FormInputID varchar(max), ValueLabel varchar(max));

	select 
		 @recordCount = count(*)
	from 
		@conditions.nodes('/Conditions/Condition') c(x);

		insert into @results (id, FieldTypeID, VersionStepID, FormInputID, ValueType, [Value], Operator, ValueLabel)
			select
			row_number() over (order by x.value('@FieldTypeID', 'int'), x.value('@VersionStepID', 'int'), x.value('@FormInputID', 'varchar(max)')) as id,
			 x.value('@FieldTypeID', 'int') as FieldTypeID
			,x.value('@VersionStepID', 'int') as VersionStepID  
			,x.value('@FormInputID', 'varchar(max)') as FormInputID
			,x.value('@ValueType', 'varchar(max)') as ValueType  
			,x.value('@Value', 'varchar(max)') as [Value]  
			,x.value('@Operator', 'varchar(max)') as [Operator] 
			,null as ValueLabel
		from 
			@conditions.nodes('/Conditions/Condition') c(x)
		left join FieldType FT on FT.ID = x.value('@FieldTypeID', 'int')
		left join workflow.VersionStep VS on VS.ID = x.value('@VersionStepID', 'int')

		
	while(@recordCount > 0)
	begin
		if (select top 1 ValueType from @results where id = @recordCount) in ('U', 'L')
		begin
		
			if ((select FieldTypeID from @results where id = @recordCount) is not null)
			begin
				declare @valueLabel varchar(max);

				select @valueLabel = coalesce(RI.DisplayValue, R.[Value])
				from 
					FieldType FT
				inner join @results R on R.id = @recordCount and FT.ID = R.FieldTypeID
				left join LookupType LT on FT.LookupObjectType = 'Lookup' and LT.ID = FT.LookupObjectID
				left join [Lookup] L on L.ID = (select top 1 [Value] from @results where id = @recordCount)
				left join ReferenceItem RI on FT.LookupObjectType = 'ReferenceItem' and FT.LookupObjectID = RI.ReferenceItemTypeID and RI.ID = R.[Value]

				update r
				set r.ValueLabel = @valueLabel
				from @results r
				where r.id = @recordCount;

			end
			
			if ((select FormInputID from @results where id = @recordCount) is not null)
			begin
				declare @fields xml, @valueLabel2 varchar(max);

				select @fields = VS.fields from 
				workflow.VersionStep VS
				inner join @results R on R.id = @recordCount and VS.ID = R.VersionStepID;


				select 
					@valueLabel2 = coalesce(RI.DisplayValue, R.[Value])
				from @fields.nodes('fields/form/field') f(x)
				inner join @results R on R.id = @recordCount
				inner join FieldType FT on FT.ID = x.value('@referenceFieldId', 'int')
				left join LookupType LT on FT.LookupObjectType = 'Lookup' and LT.ID = FT.LookupObjectID
				left join [Lookup] L on L.ID = (select top 1 [Value] from @results where id = @recordCount)
				left join ReferenceItem RI on FT.LookupObjectType = 'ReferenceItem' and FT.LookupObjectID = RI.ReferenceItemTypeID and RI.ID = R.[Value]
				where x.value('@id', 'varchar(max)') = R.FormInputID;


				update r
				set r.ValueLabel = @valueLabel2
				from @results r
				where r.id = @recordCount;


			end
		end	
		else
		begin
			update r
			set r.ValueLabel = r.[Value]
			from @results r
			where r.id = @recordCount;
		end


		set @recordCount = @recordCount - 1;
	end

	RETURN 
		coalesce(
		 (select 
			r.FieldTypeID as 'Condition/@FieldTypeID',
			r.VersionStepID as 'Condition/@VersionStepID',
			r.FormInputID as 'Condition/@FormInputID',
			r.ValueType as 'Condition/@ValueType',
			r.[Value] as 'Condition/@Value',
			r.Operator as 'Condition/@Operator',
			r.ValueLabel as 'Condition/@ValueLabel' 
		from @results r
		for xml path(''), root('Conditions'))
		,
		'<Conditions />');
END
GO

