CREATE procedure [dbo].[GetTechnicalRelationshipsByIntersect]
	@IntersectID int
as
begin
	select	distinct 
			I.Object as [Type],
			FT.Name as Attribute,
			coalesce(F.Name, '') Fusion,
			coalesce(FA.TextPath, FA.Name) as Name,
			'#/fusion/' + CAST(FT.FusionTypeID as varchar(15)) + '/' + + CAST(FA.FusionID as varchar(15)) as URL
	from	[Intersect] I
			inner join FusionAttribute FA on I.Subject = 'Intersect' and I.Object = 'FusionAttribute' and I.SubjectID = @IntersectID and FA.ID = I.ObjectID
			inner join Fusion F on F.ID = FA.FusionID
			inner join FusionAttributeType FT on FT.ID = FA.FusionAttributeTypeID;
end