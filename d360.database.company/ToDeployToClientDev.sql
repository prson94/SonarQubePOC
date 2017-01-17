--update RuleItem to use objectid/type instead of FusionAttributeID
sp_RENAME 'fusion.RuleItem.FusionAttributeID' , 'ObjectID', 'COLUMN'
alter table fusion.RuleItem add ObjectType nvarchar(250);
update fusion.RuleItem
set ObjectType = 'FusionAttribute' where ObjectType is null;


--add attribute type column to RulePromotion
sp_RENAME 'fusion.RulePromotion.FusionAttributeID' , 'AttributeID', 'COLUMN';
alter table fusion.RulePromotion drop constraint FK_FusionRulePromotion_FusionAttribute;
alter table fusion.RulePromotion add AttributeType varchar(25);
update fusion.RulePromotion set AttributeType = 'FusionAttribute' where AttributeType is null;
alter table fusion.RulePromotion alter column AttributeType varchar(25) not null;