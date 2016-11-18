--purge any existing reference records
--delete from field where objecttype = 'ReferenceItem'
--delete from fieldtype where object = 'ReferenceItemType'
--delete from referenceitem;
--delete from referenceitemtype;


--add column to keep track of old domain id
alter table referenceitemtype add oldid int;
go

--insert domain records into referenceitemtype
insert into referenceitemtype (oldid, name, displayformat, description, createdon, createdby, updatedon, updatedby)
select ID as oldid, Name, '{Code}' as DisplayFormat, Description, getdate() as CreatedOn, 0 as CreatedBy, UpdatedOn, UpdatedBy
from domain d;

--add column to keep track of old domain item id
alter table referenceitem add oldid int;
go

--insert domainitem records into referenceitem
insert into referenceitem (oldid, referenceitemtypeid, createdon, createdby, updatedon, updatedby, code)
select di.id as oldid, t.id as referenceitemtypeid, getdate() as CreatedOn, 0 as CreatedBy,  di.UpdatedOn, di.UpdatedBy, code as Code from domainitem di
join referenceitemtype t on t.oldid = di.domainid;
go

--create name/description field types for the reference items
insert into fieldtype (name, friendlyname, type, object, objectid)
select 'Name' as Name, 'Name' as FriendlyName, 'Text' as type, 'ReferenceItemType' as object, id as objectid
from referenceitemtype;

insert into fieldtype (name, friendlyname, type, object, objectid)
select 'Description' as Name, 'Description' as FriendlyName, 'Html' as type, 'ReferenceItemType' as object, id as objectid
from referenceitemtype;

--insert values for the name/description fields where applicable
insert into field (fieldtypeid, objecttype, objectid, value, formattedvalue)
select ft.id as fieldtypeid, 'ReferenceItem' as objecttype, ri.id as objectid, di.name as value, di.name as formattedvalue from domainitem di
inner join referenceitem ri on ri.oldid = di.id
inner join referenceitemtype rt on rt.id = ri.referenceitemtypeid
inner join fieldtype ft on ft.object = 'ReferenceItemType' and ft.objectid = rt.id and ft.name = 'Name'
where di.Name is not null and di.Name != '';

insert into field (fieldtypeid, objecttype, objectid, value, formattedvalue)
select ft.id as fieldtypeid, 'ReferenceItem' as objecttype, ri.id as objectid, di.Description as value, di.Description as formattedvalue from domainitem di
inner join referenceitem ri on ri.oldid = di.id
inner join referenceitemtype rt on rt.id = ri.referenceitemtypeid
inner join fieldtype ft on ft.object = 'ReferenceItemType' and ft.objectid = rt.id and ft.name = 'Description'
where di.Description is not null and di.Description != '';

--update existing domain list field types to reference list and update field values
update fieldtype
set LookupObjectType = 'ReferenceItemType',
LookupObjectID = 0,
LookupDisplayFormat = '{Name}'
where LookupObjectType = 'Domain' and Type = 'Lookup';

update f
set f.value = rt.id,
f.formattedvalue = null
from field f
inner join fieldtype ft on ft.id = f.fieldtypeid and ft.LookupObjectType = 'ReferenceItemType' and ft.Type = 'Lookup'
inner join referenceitemtype rt on rt.oldid = f.value;

--update existing domain item field types to reference item and update field values
update ft
set ft.LookupObjectType = 'ReferenceItem',
ft.LookupObjectID = rt.ID,
ft.LookupDisplayFormat = '{Name}'
from fieldtype ft
inner join referenceitemtype rt on ft.lookupobjectid = rt.oldid
inner join domain d on d.id = rt.oldid
where ft.LookupObjectType = 'DomainItem';

update f
set f.formattedvalue = null,
f.value = ri.id
from field f
inner join fieldtype ft on ft.id = f.fieldtypeid and ft.Type = 'Lookup' and ft.LookupObjectType = 'ReferenceItem'
inner join referenceitemtype rt on rt.id = ft.LookupObjectID
inner join referenceitem ri on ri.oldid = f.Value;

go

--remove tracking columns for old domain records
alter table referenceitemtype drop column oldid;
go

alter table referenceitem drop column oldid;
go
