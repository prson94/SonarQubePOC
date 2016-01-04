create procedure [dbo].[GetTechnicalRelationshipsByIntersect]
	@IntersectID int
as
begin

select	distinct TN.ObjectType as [Type],
		FT.Name as Attribute,
		coalesce(F.Name, '') Fusion,
		coalesce(FA.TextPath, FA.Name) as Name,
		'#/fusion/' + CAST(FT.FusionTypeID as varchar(15)) + '/' + + CAST(FA.FusionID as varchar(15)) as URL
from	IntersectNode SN
		inner join IntersectNode TN on 
									TN.IntersectID = SN.IntersectID and TN.ID <> SN.ID
									and TN.IntersectID = @IntersectID
		inner join IntersectNode SFN on 
									SFN.ObjectType = 'Intersect' and SFN.ObjectID = TN.IntersectID
		inner join IntersectNode TFN on 
									TFN.IntersectID = SFN.IntersectID and TFN.ID <> SFN.ID
									and SFN.ObjectType = 'Intersect' and SFN.ObjectID = TN.IntersectID
									and TFN.ObjectType = 'FusionAttribute'
		inner join FusionAttribute FA on FA.ID = TFN.ObjectID
		inner join Fusion F on F.ID = FA.FusionID
		inner join FusionAttributeType FT on FT.ID = FA.FusionAttributeTypeID;

end


