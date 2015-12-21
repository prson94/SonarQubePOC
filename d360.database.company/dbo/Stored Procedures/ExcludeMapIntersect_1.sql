CREATE procedure [dbo].[ExcludeMapIntersect]
--declare
	@mapId int,
	@type varchar(50),
	@id int
--set @object = 'Artifact'
--set @id = 972861--972859
as
begin

declare @rows table (id int);

insert into @rows 
exec FindExcludeMapIntersect @type, @id;

insert into IntersectMapExclusion (MapID, IntersectMapIDToExclude)
select 
	@mapId
	,id 
from @rows r
where
	not exists (select * from IntersectMap where mapid = @mapid and id = r.id);


end