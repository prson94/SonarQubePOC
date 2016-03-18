/*
--------------------------------------------------------------------------------------
 This file contains a list of SQL files that need to be executed when releasing 
 to production in the next cycle.
--------------------------------------------------------------------------------------
*/


--Add CreatedOn column to Artifact

-- add column CreatedOn to artifact table not nullable default to current_timestamp
alter table [Artifact] add CreatedOn datetime not null constraint DF_Artifact_CreatedOn default(CURRENT_TIMESTAMP)
go

-- DISABLE TRIGGER SO WE DONT ADD A TON OF RECORDS TO UPDATE THINGS IN THE QUEUE
ALTER TABLE [Artifact] DISABLE TRIGGER Artifact_AfterUpdate
go

-- update all created on to 1/1/2011 so they all dont show up as new
update [Artifact] set CreatedOn = '1/1/2011';

-- update all createdon dates with the updatedon date if the exist
update [Artifact] set CreatedOn = UpdatedOn where UpdatedOn is not null;

-- go to audit table and get items created date and use this.
UPDATE
	artifact
SET
    artifact.CreatedOn = a.[date]
FROM
    [dbo].[artifact] at
INNER JOIN
    [reporting].[Global_Audit] a
ON 
    (a.[object] = 'Artifact' and a.actionobject = 'Artifact' and a.actionobjectid = at.id and a.objectid = at.id and a.[action] = 'Created');


-- REENABLE TRIGGER AFTER UPDATES

ALTER TABLE [Artifact] ENABLE TRIGGER Artifact_AfterUpdate
go
