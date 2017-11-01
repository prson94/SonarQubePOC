CREATE FUNCTION [utility].[DeriveIntersectTypeName] 
(
--declare
	@id int
--set @id = 17
)
RETURNS nvarchar(500)
AS
BEGIN
	DECLARE @result nvarchar(500)

	SET @result =	(
					SELECT	COALESCE(SA.Name, SD.Name, SF.TextPath, SM.Name, SP.Name, SR.Name, ST.Name, SI.Name, SQF.Name, '') + 
							' [' + coalesce(P.Name,'/') + '] ' + 
							COALESCE(OA.Name, OD.Name, [OF].TextPath, OM.Name, OP.Name, [OR].Name, OT.Name, OQF.Name, '')
					FROM	[IntersectType] I
							left join ArtifactType SA on I.Subject = 'ArtifactType' and SA.ID = I.SubjectID
							left join ArtifactType OA on I.Object = 'ArtifactType' and OA.ID = I.ObjectID

							left join ReferenceItemType SD on I.Subject = 'ReferenceItemType' and SD.ID = I.SubjectID
							left join ReferenceItemType OD on I.Object = 'ReferenceItemType' and OD.ID = I.ObjectID

							left join [FusionAttributeType] SF on I.Subject = 'FusionAttributeType' and SF.ID = I.SubjectID
							left join [FusionAttributeType] [OF] on I.Object = 'FusionAttributeType' and [OF].ID = I.ObjectID

							left join [FusionQueryAttributeType] SQF on I.Subject = 'FusionQueryAttributeType' and SQF.ID = I.SubjectID
							left join [FusionQueryAttributeType] [OQF] on I.Object = 'FusionQueryAttributeType' and [OQF].ID = I.ObjectID

							left join [IntersectType] SI on I.Subject = 'IntersectType' and SI.ID = I.SubjectID

							left join [MapType] SM on I.Subject = 'MapType' and SM.ID = I.SubjectID
							left join [MapType] OM on I.Object = 'MapType' and OM.ID = I.ObjectID

							left join [PolicyType] SP on I.Subject = 'PolicyType' and SP.ID = I.SubjectID
							left join [PolicyType] OP on I.Object = 'PolicyType' and OP.ID = I.ObjectID

							left join [RuleType] SR on I.Subject = 'RuleType' and SR.ID = I.SubjectID
							left join [RuleType] [OR] on I.Object = 'RuleType' and [OR].ID = I.ObjectID

							left join [TaxonomyType] ST on I.Subject = 'TaxonomyType' and ST.ID = I.SubjectID
							left join [TaxonomyType] OT on I.Object = 'TaxonomyType' and OT.ID = I.ObjectID

							left join [Predicate] P on P.ID = I.PredicateID
					WHERE	I.ID = @id					
					)

	RETURN @result
END