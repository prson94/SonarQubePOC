create view ArtifactView
as

	select 
		a.id,
		a.parentid,
		a.artifacttypeid,
		a.updatedon,
		a.updatedby,
		a.createdon,
		a.visible,
		a.sourceid,
		a.createdby,
		([utility].[GetObjectDisplayValueWrapper]('Artifact',a.[ID],a.[ArtifactTypeID])) as DisplayValue
		--(select [utility].[GetObjectDisplayValueDeterministic]('Artifact',a.id,'ArtifactType', att.displayformat,(select name,[value] from utility.objectfields('Artifact',a.id)) )	)
	from 
		artifact a
		inner join artifacttype att on a.artifacttypeid = att.id;