create FUNCTION [utility].[GetIntersectTypesByType]
(	
	@type varchar(50),
	@id int
)
RETURNS TABLE 
AS
RETURN 
(
	select	'I' as type,
			cast(I.ID as varchar) + '|' +
			case 
				when (Subject = @type and SubjectID = @id) then I.Object + '|' + cast(I.ObjectID as varchar)
				else I.Subject + '|' + cast(I.SubjectID as varchar)
			end as value,
			case 
				when (Subject = @type and SubjectID = @id) then I.ObjectName + ' [' + coalesce(P.Name, 'relates') + '] ' + I.SubjectName
				else I.SubjectName + ' [' + coalesce(P.Inverse, 'related') + '] ' + I.ObjectName
			end as title
	from	IntersectTypeDetail I
			left join [Predicate] P on P.ID = I.PredicateID
	where	(Subject = @type and SubjectID = @id) or 
			(Object = @type and ObjectID = @id)
)
