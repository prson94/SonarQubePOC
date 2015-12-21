-- CONVERT ALL SOURCE ID'S FOR SQL SERVER THAT DONT CONTAIN DB NAME / SCHEMA NAME TO CONTAIN THEM
CREATE procedure [fusion].[ConvertSQLServerFusionIDs]
	@infusionID int
as
begin
	set NOCOUNT, ANSI_PADDING ON;
	SET ANSI_WARNINGS ON;
--LOAD DB NAME FOR CURRENT FUSION
--LOAD SCHEMAS MAPPING	
	declare @dbName varchar(250);
	declare @fusionID int;
-- DETERMINE IF THIS SCRIPT SHOULD BE RUN

--fusion attribute source id's to update
-- 1 - 18

	select @fusionID = [id] from fusion where fusiontypeid = 1 and [id] = @infusionID


	if @fusionID is null
	begin
		raiserror('Input fusion id is not a valid sql server fusion id',16,1);
		return;
	end;
	
	select @dbName =upper(substring(value,charindex('database=',value)+len('database='), (charindex(';',value,charindex('database=',value)) - (charindex('database=',value)+len('database='))) ) )
	from field where 
		objecttype = 'fusion' and 
		fieldtypeid = (select ID from fieldtype where Name = 'ConnectionString' and [object] = 'FusionType' and ObjectID = 1) and 
		charindex('database=',value) > 0 and
		objectid = @fusionID

	--if dbname is null then try initial catalog
	if @dbName is null
	begin
		select @dbName = upper(substring(value,charindex('initial catalog=',value)+len('initial catalog='), (charindex(';',value,charindex('initial catalog=',value)) - (charindex('initial catalog=',value)+len('initial catalog='))) ) )
			from field where 
				objecttype = 'fusion' 
				and fieldtypeid = (select ID from fieldtype where Name = 'ConnectionString' and [object] = 'FusionType' and ObjectID = 1) 
				and charindex('initial catalog=',value) > 0
				and objectid = @fusionID
	end;

	-- throw error if we cant figure out the db name
	if @dbName is null
	begin
		 raiserror('cannot find db name from fusion connection string',16,1);
		 return;
	end;	

	-- 1 = DB Schema's
	--	 Old format id = <schema_id>
	--	 New format id = <db name>.<schema name>
	update 
		fusionattribute 
	set 
		sourceid =  @dbName + '.' + upper(Name)
	where 
		fusionattributetypeid = 1 
			and 
		isnumeric(sourceid) = 1 
			and 
		fusionid = @fusionID;

	-- 2 = DB Tables
	--	 Old format id = <schema_id>_<Table_name>
	--	 New format id = <db name>.<schema name>.<table_name>
	-- 7 = DB Procs	
	--	 Old format id = <schema_id>_<proc_name>
	--	 New format id = <db name>.<schema name>.<proc_name>
	-- 8 = DB Functions
	--	 Old format id = <schema_id>_<function_name>
	--	 New format id = <db name>.<schema name>.<function_name>
	-- 11 = DB View
	--	 Old format id = <schema_id>_<view_name>
	--	 New format id = <db name>.<schema name>.<view_name>
	-- 12 = DB Trigger
	--	 Old format id = <schema_id>_<trigger_name>
	--	 New format id = <db name>.<schema name>.<trigger_name>
	update 
		fa
	set 
		fa.sourceid = @dbName + '.' + upper(fa2.Name) + '.' + fa.Name
	from 
		fusionattribute fa 
		inner join fusionattribute fa2 on (fa.parentID = fa2.id)
	where fa.fusionattributetypeid in(2,7,8,11,12)
		and fa.sourceid not like @dbName + '.%'
		and fa.fusionid = @fusionID;

			
	-- 3 = DB Table Columns
	--	 Old format id = <schema_id>_<tablename>.<col#>
	--	 New format id = <db name>.<schema name>.<tablename>.<col#>
	-- 15 = DB View Columns
	--	 Old format id = <schema_id>_<view_name>.<col#>
	--	 New format id = <db name>.<schema name>.<view_name>.<col#>
	-- 16 = Function Columns
	--	 Old format id = <schema_id>_<function_name>.<col#>
	--	 New format id = <db name>.<schema name>.<function_name>.<col#>
	-- 17 = DB Procedure Parameters
	--	 Old format id = <schema_id>_<table_name>_<proc_name>.<col#>
	--	 New format id = <db name>.<schema name>.<proc_name>.<col#>
	-- 18 = DB Function Parameters
	--	 Old format id = <schema_id>_<function_name>.<col#>
	--	 New format id = <db name>.<schema name>.<function_name>.<col#>
	update 
		fa	
	set 
		fa.sourceid = @dbName + '.' + upper(fa3.name) + '.' + fa2.name + '.' + substring(fa.SourceID,charindex('.',fa.SourceID)+1, len(fa.SourceID) - charindex('.',fa.SourceID)+1)
	from 
		fusionattribute fa 
		inner join fusionattribute fa2 on (fa.parentID = fa2.id)
		inner join fusionattribute fa3 on (fa2.parentID = fa3.id)
	where fa.fusionattributetypeid in(3,15,16,17,18)
		and charindex('.',fa.SourceID) > 0
		and fa.sourceid not like @dbName + '.%'
		and fa.fusionid = @fusionID;

	-- 4 = DB Primary Keys
	--	 Old format id = <schema_id>_<tablename>_PK
	--	 New format id = <db name>.<schema name>.<tablename>_PK
	
	update
		fa
	set
		fa.sourceid = @dbName +'.' + upper(fa3.name) + '.' + fa2.name + '_PK'	
	from 
		fusionattribute fa 
		inner join fusionattribute fa2 on (fa.parentID = fa2.id)
		inner join fusionattribute fa3 on (fa2.parentID = fa3.id)
	where fa.fusionattributetypeid = 4		
		and fa.sourceid not like @dbName + '.%'
		and fa.fusionid = @fusionID;

	-- 5 = DB Foreign Keys / Check that these should be unique across db?
	-- left as is
	-- 6 = DB Permissions
	-- left as is
	
	-- 13 = DB Constraints
	--	 Old format id = <schema_id>_<constraint_name>
	--	 New format id = <db name>.<schema name>.<constraint_name>
	-- parent is table not schema
	update
		fa
	set 
		fa.sourceid = upper(@dbName) + '.' + upper(fa3.name) + '_' + fa.name
	from 
		fusionattribute fa 
		inner join fusionattribute fa2 on (fa.parentID = fa2.id)
		inner join fusionattribute fa3 on (fa2.parentID = fa3.id)
	where fa.fusionattributetypeid in(13)		
		and fa.sourceid not like @dbName + '.%'
		and fa.fusionid = @fusionID;


	-- 14 = DB Indexes
	--	 Old format id = <schema_id>_<table_name>_<index#>.IX
	--	 New format id = <db name>.<schema name>.<table_name>_<index#>.IX
	update
		fa
	set 
		fa.sourceid =upper('RULES') + '.' + upper(fa3.name) + '.' + fa2.name + '_' + replace(right(fa.sourceid, len(fa.sourceid) - (charindex(fa2.name,fa.sourceid) + len(fa2.name))),'.IX','') + '.IX'		 
	from 
		fusionattribute fa 
		inner join fusionattribute fa2 on (fa.parentID = fa2.id)
		inner join fusionattribute fa3 on (fa2.parentID = fa3.id)
	where fa.fusionattributetypeid in(14)		
		and fa.sourceid not like @dbName + '.%'
		and fa.fusionid = @fusionID;
	
end;

