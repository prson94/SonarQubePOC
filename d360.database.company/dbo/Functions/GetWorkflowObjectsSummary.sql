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
join Asset a on a.object = i.object and a.objectid = i.objectid
where versionid = @versionId;

return (
	select string_agg(x.Name, ', ') + 
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
		utility.GetAssetDisplayValue(a.ID) as name, i.object, i.objectid from workflow.item i
		inner join Asset a on a.object = i.object and a.objectid = i.objectid
		where versionid = @versionId and ((@filteredObjectId is not null and (i.object = @filteredObject and i.objectId = @filteredObjectId)) or (@filteredObjectId is null))
		order by 1
	) x 
)
END
GO


