

CREATE procedure [dbo].[GetLineageDiagramData]
--declare
	@IntersectID int
--set @IntersectID = 289
as
begin
	set nocount on;
	declare	-- This must NOT be the business term, it can only be the application or license service.  Calculate by getting the object that is first in the order of the intersect type.
			@SourceType varchar(50),
			@SourceID int
	declare -- This MUST be the other side of the relationship.
			@topType varchar(50),
			@topID int

	select		top 1
				@SourceType = N.ObjectType,
				@SourceID = N.ObjectID
	from		IntersectNode N 
				inner join IntersectTypeNode TN on N.IntersectTypeNodeID = TN.ID and N.IntersectID = @IntersectID
	order by	TN.[Order] asc

	select		top 1
				@topType = N.ObjectType,
				@topID = N.ObjectID
	from		IntersectNode N 
				inner join IntersectTypeNode TN on N.IntersectTypeNodeID = TN.ID and N.IntersectID = @IntersectID
	order by	TN.[Order] desc

	-- Stores the sources we have identified through the loop below.
	declare @tbl table (IntersectID int, ID int, ParentID int, SourceObjectType varchar(50), SourceObjectID int, ResponsibilityTypeID int, ResponsibleObjectType varchar(50), ResponsibleObjectID int, [Role] nvarchar(250))

	--Seed initial tables values
	insert into @tbl
		select	R.IntersectID,
				T.ID,
				0,
				R.SourceObject,
				R.SourceObjectID,
				T.ResponsibilityTypeID,
				T.ResponsibleObjectType,
				T.ResponsibleObjectID,
				R.[Role]
		from	Responsibility T
				inner join cache.Relationships R on 
					R.SourceObject = T.ResponsibleObjectType and R.SourceObjectID = T.ResponsibleObjectID 
					and R.TargetObject = @topType and R.TargetObjectID = @topID
					and T.ObjectType = 'Intersect' and T.ObjectID = @IntersectID

	-- follow trail all the way back.
	while exists(
			select	1 
			from	Responsibility R
					inner join cache.Relationships CR on 
						CR.SourceObject = R.ResponsibleObjectType and CR.SourceObjectID = R.ResponsibleObjectID 
						and CR.TargetObject = @topType and CR.TargetObjectID = @topID
					inner join @tbl T on R.ObjectType = 'Intersect' and T.IntersectID = R.ObjectID and CR.IntersectID not in (select IntersectID from @tbl)
	)
	begin
		insert into @tbl
			select	CR.IntersectID,
					R.ID,
					T.ID,
					CR.SourceObject,
					CR.SourceObjectID,
					R.ResponsibilityTypeID,
					R.ResponsibleObjectType,
					R.ResponsibleObjectID,
					CR.[Role]
			from	Responsibility R
					inner join cache.Relationships CR on 
						CR.SourceObject = R.ResponsibleObjectType and CR.SourceObjectID = R.ResponsibleObjectID 
						and CR.TargetObject = @topType and CR.TargetObjectID = @topID
					inner join @tbl T on R.ObjectType = 'Intersect' and T.IntersectID = R.ObjectID and CR.IntersectID not in (select IntersectID from @tbl)
	end

	--final result to caller
	select	@IntersectID as IntersectID,
			0 as ID,
			NULL as ParentID,
			'Intersect' as ObjectType,
			@IntersectID as ObjectID,
			I.Name,
			I.ObjectTypeName as [Type],
			I.IconBackColor as BackColor,
			I.IconForeColor as ForeColor,
			I.Url,
			T.TechnicalRelationships,
			NULL as Contexts,
			NULL as Transformations,
			NULL as [Role]
	from	cache.ObjectDetails I
			outer apply (
						select (
								select	CR.TargetObject as "@type",
										CR.TargetObjectID as "@id",
										CR.TargetTypeName as "@attribute",
									 	coalesce(F.Name, CR.TargetObjectName, '') "@fusion",
										TD.TextPath as "@name",
										TD.Url as "@url"
								from	cache.Relationships CR
										inner join cache.ObjectDetails TD on TD.[Object] = CR.TargetObject and TD.ObjectID = CR.TargetObjectID
										left join FusionAttribute FA on FA.ID = TD.ObjectID
										left join Fusion F on F.ID = FA.FusionID
								where	CR.SourceObject = 'Intersect' and CR.SourceObjectID = @IntersectID
								for xml path('relationship'), root('relationships')
							) as TechnicalRelationships
						) T
	where	I.[Object] = 'Intersect' 
			and I.ObjectID = @IntersectID
	union
	select	R.IntersectID,
			R.ID,
			R.ParentID,
			R.SourceObjectType as ObjectType,
			R.SourceObjectID as ObjectID,
			D.Name,
			D.ObjectTypeName as [Type],
			D.IconBackColor as BackColor,
			D.IconForeColor as ForeColor,
			D.Url,
			T.TechnicalRelationships,
			C.Contexts,
			X.Transformations,
			R.[Role]
	from	@tbl R
			inner join cache.ObjectDetails D on D.[Object] = R.ResponsibleObjectType and D.ObjectID = R.ResponsibleObjectID
			outer apply (
						select (
								select	CR.TargetObject as "@type",
										CR.TargetObjectID as "@id",
										CR.TargetTypeName as "@attribute",
									 	coalesce(F.Name, CR.TargetObjectName, '') "@fusion", --F.Name, 
										TD.TextPath as "@name",
										TD.Url as "@url"
								from	cache.Relationships CR
										inner join cache.ObjectDetails TD on TD.[Object] = CR.TargetObject and TD.ObjectID = CR.TargetObjectID
										left join FusionAttribute FA on FA.ID = TD.ObjectID
										left join Fusion F on F.ID = FA.FusionID
								where	CR.SourceObject = 'Intersect' and CR.SourceObjectID = R.IntersectID
								for xml path('relationship'), root('relationships')
							) as TechnicalRelationships
						) T
			outer apply (
						select (
								select	case ResponsibilityTransformationType
											when 1 then 'Business Transformation'
											else 'Technical Transformation'
										end as "@type",
										ID as "@id",
										Description as "description"
								from	ResponsibilityTransformation
								where	ResponsibilityID = R.ID
								for xml path('transformation'), root('transformations')
							) as Transformations
						) X
			outer apply (
						select (
								select	LT.Name as "@lookup",
										L.Name as "@name",
										L.Code as "@code"
								from	ResponsibilityContextItem RCI
										inner join DomainItem L on RCI.ObjectType = 'DomainItem' and L.ID = RCI.ObjectID and RCI.ResponsibilityID = R.ID
										inner join Domain LT on LT.ID = L.DomainID
								for xml path('context'), root('contexts')
							) as Contexts
						) C
end
GO