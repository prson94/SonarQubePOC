CREATE procedure [dbo].[GetRelationships]
--declare
	@ObjectType varchar(50),
	@ObjectID int
--set @ObjectType = 'Artifact'
--set @ObjectID = 4651
as
begin
	IF OBJECT_ID('tempdb..#Relates') IS NOT NULL
		DROP TABLE #Relates;

	create table #Relates (
		IntersectID int, 
		ObjectType varchar(50), 
		ObjectID int, 
		ObjectName nvarchar(1000),
		TypeName nvarchar(250),
		Url nvarchar(2000),
		ConcatValue varchar(65)
	);

	CREATE NONCLUSTERED INDEX IX_TempRelates ON #Relates (ConcatValue ASC);

	--Intersect loading
	insert into #Relates
		select	R.IntersectID,
				R.TargetObject as ObjectType,
				R.TargetObjectID as ObjectID,
				coalesce(D.TextPath, R.TargetObjectName) as Name,
				R.TargetTypeName as TypeName,
				dbo.GenerateObjectUrl(R.TargetObject, R.TargetTypeID, R.TargetObjectID) Url,
				R.TargetObject + cast(R.TargetObjectID as varchar(15))
		from	cache.Relationships R
				left join cache.ObjectDetails D on D.[Object] = R.TargetObject and D.ObjectID = R.TargetObjectID
		where	R.SourceObject = @ObjectType
				and R.SourceObjectID = @ObjectID
	
	if (@ObjectType <> 'Intersect')
	begin
		--Source loading
		insert into #Relates
			select	NULL as IntersectID,
					R.ResponsibleObjectType,
					R.ResponsibleObjectID,
					R.ResponsibleObjectName,
					ROD.ObjectTypeName as TypeName,
					ROD.Url,
					NULL
			from	SourcingResponsibilityDetail R
					inner join cache.ObjectDetails ROD on ROD.[Object] = R.ResponsibleObjectType and ROD.ObjectID = R.ResponsibleObjectID --cross apply utility.ObjectDetail(R.ResponsibleObjectType, R.ResponsibleObjectID) ROD
			where	R.ObjectType = @ObjectType 
					and R.ObjectID = @ObjectID
					and R.ResponsibleObjectType + cast(R.ResponsibleObjectID as varchar(15)) not in (select ObjectType + cast(ObjectID as varchar(15)) from #Relates)
	end

	-- Return the results to client.
	select		IntersectID, 
				ObjectType, 
				ObjectID, 
				ObjectName,
				TypeName,
				Url
	from		#Relates
	order by	TypeName,
				ObjectName
end
