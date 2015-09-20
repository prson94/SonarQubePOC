DISABLE TRIGGER LookupType_OnBeforeInsert ON LookupType
go

go
DISABLE TRIGGER Lookup_OnBeforeInsert ON [Lookup]
go

DECLARE @TranName VARCHAR(20);
SELECT @TranName = 'MyTransaction';

BEGIN TRANSACTION @TranName;
declare @sourceCompanyID int
declare @targetCompanyID int
set @sourceCompanyID = 2 --the company that has your source lookup
set @targetCompanyID = 4 -- ACI


ENABLE TRIGGER LookupType_OnBeforeInsert ON LookupType

insert into LookupType
select 4,
              ID,
              Name,
              DateCreated,
              CreatingResourceID,
              DateUpdated,
              UpdatingResourceID
from   LookupType 
where  CompanyID = 2
              and Name in ('Contact Type')



declare @sourceCompanyID int
declare @targetCompanyID int
set @sourceCompanyID = 2 --the company that has your source lookup
set @targetCompanyID = 4 -- ACI
ENABLE TRIGGER FieldType_OnBeforeInsert ON FieldType

insert into FieldType
SELECT 4
              ,[ID]
              ,[Name]
              ,[FriendlyName]
              ,[Description]
              ,[Type]
              ,[LookupObjectType]
              ,[LookupObjectID]
              ,[LookupDisplayFormat]
              ,[MinimumLength]
              ,[MaximumLength]
              ,[Length]
              ,[Pattern]
              ,[DateCreated]
              ,[CreatingResourceID]
              ,[DateUpdated]
              ,[UpdatingResourceID]
from   FieldType F
              inner join    (
                                  select CompanyID,
                                                FieldTypeID
                                  from   FieldTypeRelation R
                                                inner join    (
                                                                     select 'LookupType' as ObjectType,
                                                                            ID as ObjectID
                                                                     from   LookupType
                                                                     where  CompanyID = 2 
                                                                                  and  Name in ('Contact Type')              
                                                                     ) L on L.ObjectType = R.ObjectType and L.ObjectID = R.ObjectID
                                  where  R.CompanyID = 2
                                  ) R on R.CompanyID = F.CompanyID and R.FieldTypeID = F.ID




INSERT INTO FieldTypeRelation
           ([CompanyID]
           ,[FieldTypeID]
           ,[ObjectType]
           ,[ObjectID]
           ,[SortOrder]
           ,[IsRequired]
           ,[IsListable]
           ,[Description]
                 )
       select 4,
                     FieldTypeID,
                     R.ObjectType,
                     R.ObjectID,
                     SortOrder,
                     IsRequired,
                     IsListable,
                     Description
       from   FieldTypeRelation R
                     inner join    (
                                         select 'LookupType' as ObjectType,
                                                       ID as ObjectID
                                         from   LookupType
                                         where  CompanyID = 2 
                                                       and  Name in ('Contact Type')               
                                         ) L on L.ObjectType = R.ObjectType and L.ObjectID = R.ObjectID
       where  R.CompanyID = 2



ENABLE TRIGGER Lookup_OnBeforeInsert ON [Lookup]

insert into [Lookup]
select 4,
              ID,
              LookupTypeID,
              DateCreated,
              CreatingResourceID,
              DateUpdated,
              UpdatingResourceID
from   [Lookup]
where  CompanyID = 2
              and LookupTypeID in (
                                                select ID
                                                from   LookupType
                                                where  CompanyID = 2
                                                              and Name in ('Contact Type')
                                                )


ENABLE TRIGGER Field_InsteadOfInsert ON [Field]

INSERT INTO Field
       select 4,
                     ObjectType,
                     ObjectID,
                     FieldTypeID,
                     Value
       from   Field
       where  CompanyID = 2
                     and ObjectType = 'Lookup'
                     and ObjectID IN (
                                                select ID
                                                FROM   [Lookup]
                                                where  CompanyID = 2
                                                              and LookupTypeID in (
                                                                                                select ID
                                                                                                from       LookupType
                                                                                                where       CompanyID = 2
                                                                                                              and Name in ('Contact Type')
                                                                                                )
                                                )
COMMIT TRANSACTION @TranName;
GO

ENABLE TRIGGER FieldType_OnBeforeInsert ON FieldType
ENABLE TRIGGER LookupType_OnBeforeInsert ON LookupType
ENABLE TRIGGER Lookup_OnBeforeInsert ON [Lookup]


select * from FusionAttribute where CompanyID = 4