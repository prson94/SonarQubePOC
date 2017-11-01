CREATE FUNCTION [utility].[DeriveIntersectName] 
(	
	@id int
)
RETURNS nvarchar(500)
AS
BEGIN
	DECLARE @result nvarchar(500)

	SET @result =	(
					SELECT	COALESCE(SA.DisplayValue, SD.Name, SF.TextPath, 'Map', SP.TextPath, SR.DisplayValue, ST.TextPath, SI.Name, '') + ' / ' + COALESCE(OA.DisplayValue, OD.Name, [OF].TextPath, 'Map', OP.TextPath, [OR].DisplayValue, OT.TextPath, '')
					FROM	[Intersect] I
							left join Artifact SA on I.Subject = 'Artifact' and SA.ID = I.SubjectID
							left join Artifact OA on I.Object = 'Artifact' and OA.ID = I.ObjectID

							left join ReferenceItemType SD on I.Subject = 'ReferenceItemType' and SD.ID = I.SubjectID
							left join ReferenceItemType OD on I.Object = 'ReferenceItemType' and OD.ID = I.ObjectID

							left join [FusionAttribute] SF on I.Subject = 'FusionAttribute' and SF.ID = I.SubjectID
							left join [FusionAttribute] [OF] on I.Object = 'FusionAttribute' and [OF].ID = I.ObjectID


							left join [Intersect] SI on I.Subject = 'Intersect' and SI.ID = I.SubjectID

							left join [Map] SM on I.Subject = 'Map' and SM.ID = I.SubjectID
							left join [Map] OM on I.Object = 'Map' and OM.ID = I.ObjectID

							left join [Policy] SP on I.Subject = 'Policy' and SP.ID = I.SubjectID
							left join [Policy] OP on I.Object = 'Policy' and OP.ID = I.ObjectID

							left join [Rule] SR on I.Subject = 'Rule' and SR.ID = I.SubjectID
							left join [Rule] [OR] on I.Object = 'Rule' and [OR].ID = I.ObjectID

							left join [Taxonomy] ST on I.Subject = 'Taxonomy' and ST.ID = I.SubjectID
							left join [Taxonomy] OT on I.Object = 'Taxonomy' and OT.ID = I.ObjectID

					WHERE	I.ID = @id					
					)

	RETURN @result
END
