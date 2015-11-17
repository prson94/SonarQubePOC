CREATE procedure tile.GetChildArtifactStatisticsByObject
--declare
	@id int
--set @id = 733
as
begin
	select		T.Name as [Name],
				T.ID,
				count(1) as [Count]
	from		Artifact A
				inner join ArtifactType T on T.ID = A.ArtifactTypeID and A.ParentID = @id
	group by	T.Name,
				T.ID
	order by	T.Name
end