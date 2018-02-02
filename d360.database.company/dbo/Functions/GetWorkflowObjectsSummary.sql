CREATE FUNCTION [dbo].[GetWorkflowObjectsSummary]
(
	@versionId int,
	@filteredObject varchar(50) = null,
	@filteredObjectId int = null
)
RETURNS varchar(max)
AS
BEGIN

declare @itemCount int;

select @itemCount = count(*) from workflow.item i
inner join workflow.version v on v.id = @versionId and i.versionid = @versionid;

return (
	select string_agg(utility.GetAssetDisplayValue(x.id), ', ') + 
	case when @filteredObjectId is not null then
		case when @itemCount > 1 then
			' and ' + cast((@itemCount - 1) as varchar) + ' more...'
		else
			''
		end
	else
		case when @itemCount > 5 then
			' and ' + cast((@itemCount - 5) as varchar) + ' more...'
		else
			''
		end
	end from 
	(
		select distinct top 5 
		coalesce(a2.id, a.id) as id, coalesce(a.object,a2.object) as object, coalesce(a.objectid,a2.objectid) as objectid from workflow.item i
		left join Asset a on i.object != 'Issue' and a.object = i.object and a.objectid = i.objectid
		left join Issue s on i.object = 'Issue' and s.id = i.objectid
		left join Asset a2 on i.object = 'Issue' and a2.object = s.object and a2.objectid = s.objectid
		inner join workflow.version v on i.versionid = v.id and v.id = @versionId
		inner join workflow.type t on t.id = v.typeid
		where ((@filteredObjectId is not null and (coalesce(a2.object, a.object) = @filteredObject and coalesce(a2.objectId, a.objectId) = @filteredObjectId)) or (@filteredObjectId is null))
		order by 1
	) x 
)
END
