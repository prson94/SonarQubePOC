CREATE procedure [fusion].[GetFusionOwnerOptions]
as
begin
	select	T.Name as [Type],
			A.ID,
			T.Name + ' : ' + A.TextPath as Name
	from	Artifact A 
			inner join ArtifactType T	on T.ID = A.ArtifactTypeID 
										and T.CanOwnFusion = 1
	order by	T.Name + ' : ' + A.TextPath
end