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
					SELECT	COALESCE(SA.Name, SD.Name, SF.TextPath, SP.Name, ST.Name, SI.Name, case I.Subject when 'RuleType' then 'Rule' else '' end) + 
							' ' + coalesce(P.Name,'/') + ' ' + 
							COALESCE(OA.Name, OD.Name, [OF].TextPath, OP.Name, OT.Name, case I.Object when 'RuleType' then 'Rule' else '' end)
					FROM	[IntersectType] I
							left join ArtifactType SA on I.Subject = 'ArtifactType' and SA.ID = I.SubjectID
							left join ArtifactType OA on I.Object = 'ArtifactType' and OA.ID = I.ObjectID

							left join DomainType SD on I.Subject = 'DomainType' and SD.ID = I.SubjectID
							left join DomainType OD on I.Object = 'DomainType' and OD.ID = I.ObjectID

							left join [FusionAttributeType] SF on I.Subject = 'FusionAttributeType' and SF.ID = I.SubjectID
							left join [FusionAttributeType] [OF] on I.Object = 'FusionAttributeType' and [OF].ID = I.ObjectID


							left join [IntersectType] SI on I.Subject = 'IntersectType' and SI.ID = I.SubjectID

							left join [PolicyType] SP on I.Subject = 'PolicyType' and SP.ID = I.SubjectID
							left join [PolicyType] OP on I.Object = 'PolicyType' and OP.ID = I.ObjectID

							left join [TaxonomyType] ST on I.Subject = 'TaxonomyType' and ST.ID = I.SubjectID
							left join [TaxonomyType] OT on I.Object = 'TaxonomyType' and OT.ID = I.ObjectID

							left join [Predicate] P on P.ID = I.PredicateID
					WHERE	I.ID = @id
					FOR XML PATH('')
					)

	RETURN @result
END
