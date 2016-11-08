-- add a constraint on name to reference item type to prevent duplicate names
ALTER TABLE [dbo].[ReferenceItemType] ADD CONSTRAINT CONST_Reference_Item_Type_Name UNIQUE (Name); 