-- add a constraint on name to reference item type to prevent duplicate names
ALTER TABLE [dbo].[ReferenceItemType] ADD CONSTRAINT CONST_Reference_Item_Type_Name UNIQUE (Name); 

-- updates to favorites table to allow names to update and join to cache object details
ALTER TABLE [dbo].[Favorite] ALTER COLUMN name varchar(250) NULL -- make name optional
alter table [dbo].[favorite] add [Object] varchar(50)
alter table [dbo].[favorite] add ObjectID int  