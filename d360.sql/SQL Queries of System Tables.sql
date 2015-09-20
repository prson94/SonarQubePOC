/* OBJECTS ***************************/
select	* 
from	(
		select	O.Name,
				O.object_id as ID,
				O.schema_id as SchemaID,
				S.Name as [Schema],
				case  
					when O.parent_object_id = 0 and O.schema_id is null then null
					when O.parent_object_id = 0 and O.schema_id is not null then O.schema_id
					else O.parent_object_id
				end as ParentID,
				OP.Name as Parent,
				case O.type
					when 'C'	then 'Check Constraint'
					when 'D'	then 'Default Constraint'
					when 'UQ'	then 'Unique Constraint'
					--when 'F'	then NULL
					when 'FN'	then 'Scalar Function'
					when 'IF'	then 'Inline Table Function'
					when 'TF'	then 'Table Function'
					when 'L'	then NULL
					when 'P'	then 'User-defined Procedure'
					when 'PK'	then NULL
					when 'S'	then 'System Table'
					when 'RF'	then 'Replication Filter Procedure'
					when 'TR'	then NULL
					when 'U'	then 'User Table'
					when 'V'	then NULL
					when 'X'	then 'Extended Procedure'
					else O.type_desc
				end as SubType,
				case O.type
					when 'C'	then 13
					when 'D'	then 13
					when 'UQ'	then 13
					--when 'F'	then 5
					when 'FN'	then 8
					when 'IF'	then 8
					when 'TF'	then 8
					when 'L'	then 10
					when 'P'	then 7
					when 'PK'	then 4
					when 'S'	then 2
					when 'RF'	then 7
					when 'TR'	then 12
					when 'U'	then 2
					when 'V'	then 11
					when 'X'	then 7
					else O.type_desc
				end as FusionAttributeTypeID,
				NULL as ParentFusionAttributeID,
				NULL as FusionAttributeID,
				case 
					when o.parent_object_id is null then 0 
					when o.parent_object_id = 0 then 0
					else 1 
				end as HasParent
		from	sys.objects O
				inner join sys.schemas S ON O.schema_id = S.schema_id
				left join sys.objects OP on O.parent_object_id <> 0 and O.parent_object_id = OP.object_id
		where	O.is_ms_shipped = 0
				and O.type <> 'F'
		union 
		select	Name,
				schema_id as ID,
				NULL as SchemaID,
				NULL as [Schema],
				NULL as ParentID,
				NULL as Parent,
				NULL as SubType,
				1 as FusionAttributeTypeID,
				NULL as ParentFusionAttributeID,
				NULL as FusionAttributeID,
				0 as HasParent
		from	sys.schemas
		) O
order by	o.HasParent, O.FusionAttributeTypeID
/*************************************/

/* INDEXES ***************************/
select		Name,
			cast(object_id as varchar(25)) + '.' + cast(index_id as varchar(15)) as ID,
			object_id AS ParentID,
			type_desc as SubType,
			fill_factor as [FillFactor],
			is_padded as IsPadded,
			is_primary_key as IsPrimaryKey,
			is_unique_constraint as IsUnique,
			14 as FusionAttributeTypeID,
			NULL as ParentFusionAttributeID,
			NULL as FusionAttributeID
from		[sys].[indexes] 
where		[type] <> 0
			and [object_id] > 200
order by	[object_id], index_id
/*************************************/

/* COLUMNS ***************************/
select	s.schema_id as SchemaID,
		c.object_id as ParentID,
		cast(c.object_id as varchar(25)) + '.' + cast(c.column_id as varchar(25)) as ID,
		c.Name,
		t.name as [Type],
		c.max_length as MaxLength,
		c.Precision,
		c.Scale,
		c.is_nullable as Nullable,
		c.is_computed as Computed,
		3 as FusionAttributeTypeID,
		NULL as ParentFusionAttributeID,
		NULL as FusionAttributeID
from	sys.columns c
		inner join sys.objects o on c.object_id = o.object_id 
		inner join sys.schemas s on o.schema_id = s.schema_id
		inner join sys.types t on c.user_type_id = t.user_type_id and c.object_id > 100
/*************************************/

/* PERMISSIONS ***********************/
SELECT		cast(p.major_id as varchar(25)) + '.' + cast(p.grantee_principal_id as varchar(25)) as ID,
			p.major_id as ParentID,
			p.state_desc as [Action],
			p.permission_name as [Permission],
			USER_NAME(p.grantee_principal_id) as [User],
			p.permission_name + 
			' to ' +
			USER_NAME(p.grantee_principal_id) +
			' on ' + 
			CASE p.class 
				WHEN 0 THEN DB_NAME()
				WHEN 1 THEN OBJECT_NAME(major_id)
				WHEN 3 THEN SCHEMA_NAME(major_id) 
			END as Name,
			6 as FusionAttributeTypeID,
			NULL as ParentFusionAttributeID,
			NULL as FusionAttributeID
FROM		sys.database_permissions p
			left join sys.objects o on p.major_id = o.[object_id]
where		p.grantee_principal_id <> 0 
			and p.major_id <> 0
order by	p.major_id, 
			p.grantee_principal_id
/*************************************/


/* FOREIGN KEY TABLE REFERENCES ******/
select	fk.Name,
		fk.object_id as ID,
		fk.Schema_ID as SchemaID,
		fk.delete_referential_action_desc as DeleteAction,
		fk.update_referential_action_desc as UpdateAction,
		p.object_id as SourceTableID,
		--p.name as SourceTableName,
		t.object_id as TargetTableID,
		--t.name as TargetTableName
		5 as FusionAttributeTypeID,
		NULL as ParentFusionAttributeID,
		NULL as FusionAttributeID
from	sys.foreign_keys fk
		inner join sys.objects p on fk.parent_object_id = p.object_id
		inner join sys.objects t on fk.referenced_object_id = t.object_id
where	fk.is_ms_shipped = 0
/*************************************/

/* FOREIGN KEY COLUMN REFERENCES *****/
select	fkc.constraint_object_id as ForeignKeyID,
		--c.name as ForeignKeyName,
		cast(pp.object_id as varchar(25)) + '.' + cast(pp.column_id as varchar(25)) as SourceColumnID,
		--pp.name as SourceColumnName,
		cast(rr.object_id as varchar(25)) + '.' + cast(rr.column_id as varchar(25)) as TargetColumnID--,
		--rr.name as TargetColumnName
from	[sys].[foreign_key_columns] fkc
		inner join sys.objects c on fkc.constraint_object_id = c.object_id
		inner join sys.columns pp on pp.object_id = fkc.parent_object_id and fkc.parent_column_id = pp.column_id and pp.object_id > 100
		inner join sys.columns rr on rr.object_id = fkc.referenced_object_id and fkc.referenced_column_id = rr.column_id
order by fkc.constraint_object_id
/*************************************/


--exec sp_sproc_columns 'LoadCachedSecurity', 'dbo', 'D3S'
select * from [sys].[sysreferences]
select * from [sys].[column_type_usages]


