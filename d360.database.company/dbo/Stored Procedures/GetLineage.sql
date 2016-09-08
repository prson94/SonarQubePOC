CREATE procedure [dbo].[GetLineage]
--declare 
	@type varchar(50),
	@id int,
	@view int = 1

--set @type = 'Artifact'
--set @id = 2554--19
--set @view = 3
as
begin
	set nocount on;

	declare @links table ([from] varchar(250), [to] varchar(250), category varchar(50));
	declare @nodes table (
		[key] varchar(250), 
		obj varchar(50), [objid] int, [type] varchar(50), typeName nvarchar(250), name nvarchar(500), 
		back varchar(7), fore varchar(7), template varchar(50), other varchar(500),

		HasSourceRules bit
	);

	declare @objects table (Type varchar(50), ID int);
	insert into @objects values (@type, @id)

	IF OBJECT_ID('tempdb..#points') IS NOT NULL
		DROP TABLE #points

	create table #points ( ID int, SourceIntersectID int, TargetIntersectID int, [Level] int )
	CREATE CLUSTERED INDEX CIX_#points ON #points ([ID])
	CREATE NONCLUSTERED INDEX IX_#points_Level ON #points ([Level])
	CREATE NONCLUSTERED INDEX IX_#points_SourceIntersectID ON #points ([SourceIntersectID])

	declare @counter int = 1,
			@max int = 10


	if @type <> 'FusionAttribute'
	begin
		-- Get synonyms for this empty item, to get their lineages
		if not exists(
			select	MI.ID
			from	MapItem MI
					inner join [Intersect] SI on SI.ID = MI.SourceIntersectID
					inner join [Intersect] TI ON TI.ID = MI.TargetIntersectID
			where 	( (SI.Subject = @type and SI.SubjectID = @id) OR (SI.Object = @type and SI.ObjectID = @id)  )
					OR ( (TI.Subject = @type and TI.SubjectID = @id) OR (TI.Object = @type and TI.ObjectID = @id)  )
		)
		begin
			insert into @objects
				select	case 
							when I.Subject = @type and I.SubjectID = @id then I.Object
							else I.Subject
						end,
						case 
							when I.Subject = @type and I.SubjectID = @id then I.ObjectID 
							else I.SubjectID 
						end
				from	[Intersect] I
						inner join IntersectType T on T.ID = I.IntersectTypeID 
						inner join Predicate P on P.ID = T.PredicateID and P.Type = 6
				where	(I.Subject = @type and I.SubjectID = @id) or (I.Object = @type and I.ObjectID = @id)
		end

		-- get all items directly tied to the focal object.
		insert into #points
			select	MI.ID, MI.SourceIntersectID, MI.TargetIntersectID, 0
			from	MapItem MI
					inner join [Intersect] SI on SI.ID = MI.SourceIntersectID
					inner join [Intersect] TI ON TI.ID = MI.TargetIntersectID
					inner join @objects O on	( (SI.Subject = O.Type and SI.SubjectID = O.ID) OR (SI.Object = O.Type and SI.ObjectID = O.ID)  ) OR 
												( (TI.Subject = O.Type and TI.SubjectID = O.ID) OR (TI.Object = O.Type and TI.ObjectID = O.ID)  )

		-- get all items not directly tied to the focal object, but still tied to maps involved above.
		insert into #points
			select	MI.ID, MI.SourceIntersectID, MI.TargetIntersectID, 0
			from	MapItem MI
					inner join	(
								select	ID.MapItemID
								from	MapItemMap DM
										inner join #points D on D.ID = DM.MapItemID
										inner join MapItemMap ID on ID.MapID = DM.MapID and ID.MapItemID not in (
																												select ID from #points
																												)
								) O on O.MapItemID = MI.ID;

		--backward-facing lineage
		while exists(select 1 from #points where [Level] = @counter-1) AND @counter <= @max
		begin
			insert into #points
				select	S.ID,
						S.SourceIntersectID,
						S.TargetIntersectID,
						@counter
				from	MapItem S
						inner join #points T on T.SourceIntersectID = S.TargetIntersectID and S.ID <> T.ID and [Level] = @counter-1
			set @counter = @counter + 1
		end

		--forward-facing lineage
		set @counter = -1

		while exists(select 1 from #points where [Level] = @counter+1) AND @counter >= -@max
		begin
			insert into #points
				select	S.ID,
						S.SourceIntersectID,
						S.TargetIntersectID,
						@counter
				from	MapItem S
						inner join #points T on T.TargetIntersectID = S.SourceIntersectID and S.ID <> T.ID and [Level] = @counter+1
			set @counter = @counter - 1
		end
	end

	if @view = 1 OR @view = 2
	begin
		declare @items table (
			ID int,
			SourceIntersectID int, 
			SourceSubjectTypeName nvarchar(500), SourceSubjectName nvarchar(500), SourceSubject varchar(50), SourceSubjectID int, SourceSubjectIconBackColor varchar(7), SourceSubjectIconForeColor varchar(7), 
			SourceObjectTypeName nvarchar(500), SourceObjectName nvarchar(500), SourceObject varchar(50), SourceObjectID int, SourceObjectIconBackColor varchar(7), SourceObjectIconForeColor varchar(7),
			
			TargetIntersectID int, 
			TargetSubjectTypeName nvarchar(500), TargetSubjectName nvarchar(500), TargetSubject varchar(50), TargetSubjectID int, TargetSubjectIconBackColor varchar(7), TargetSubjectIconForeColor varchar(7), 
			TargetObjectTypeName nvarchar(500), TargetObjectName nvarchar(500), TargetObject varchar(50), TargetObjectID int, TargetObjectIconBackColor varchar(7), TargetObjectIconForeColor varchar(7),

			HasSourceRules bit
		)

		insert into @items
			select	O.ID,
				
					O.SourceIntersectID,
					SI.SubjectTypeName,
					SI.SubjectName,
					SI.Subject,
					SI.SubjectID,
					SI.SubjectIconBackColor,
					SI.SubjectIconForeColor,
					SI.ObjectTypeName,
					SI.ObjectName,
					SI.Object,
					SI.ObjectID,
					SI.ObjectIconBackColor,
					SI.ObjectIconForeColor,

					O.TargetIntersectID,
					TI.SubjectTypeName,
					TI.SubjectName,
					TI.Subject,
					TI.SubjectID,
					TI.SubjectIconBackColor,
					TI.SubjectIconForeColor,
					TI.ObjectTypeName,
					TI.ObjectName,
					TI.Object,
					TI.ObjectID,
					TI.ObjectIconBackColor,
					TI.ObjectIconForeColor,

					case 
						when HSR.C > 0 then cast(1 as bit)
						else cast(0 as bit)
					end as HasSourceRules
			from	#points O
					inner join IntersectDetail SI on SI.ID = O.SourceIntersectID
					inner join IntersectDetail TI ON TI.ID = O.TargetIntersectID
					cross apply (
								select	count(1) as C
								from	MapItem MI 
										inner join MapSequence MS on MS.MapItemID = MI.ID and MI.TargetIntersectID = TI.ID
								) HSR

		if @view = 1
		begin
			insert into @links
					select	distinct
							S.SourceSubject + '.' + cast(S.SourceSubjectID as varchar) as [from],
							S.TargetSubject + '.' + cast(S.TargetSubjectID as varchar) as [to],
							'' as category
					from	@items S
			insert into @nodes
					select	distinct
							I.SourceSubject + '.' + cast(I.SourceSubjectID as varchar) as [key],
							I.SourceSubject as [obj],
							I.SourceSubjectID as [objid], 
							I.SourceSubject as [type],
							I.SourceSubjectTypeName as typeName,
							I.SourceSubjectName as name,
							I.SourceSubjectIconBackColor as back,
							I.SourceSubjectIconForeColor as fore,
							case 
								when I.SourceSubject = @type and I.SourceSubjectID = @id then 'Focal'
								else 'Normal'
							end as template,
							null as other,
							I.HasSourceRules
					from	@items I
			insert into @nodes
					select	distinct
							I.TargetSubject + '.' + cast(I.TargetSubjectID as varchar) as [key],
							I.TargetSubject as [obj],
							I.TargetSubjectID as [objid], 
							I.TargetSubject as [type],
							I.TargetSubjectTypeName as typeName,
							I.TargetSubjectName as name,
							I.TargetSubjectIconBackColor as back,
							I.TargetSubjectIconForeColor as fore,
							case 
								when I.TargetSubject = @type and I.TargetSubjectID = @id then 'Focal'
								else 'Normal'
							end as template,
							null as other,
							I.HasSourceRules
					from	@items I
					where	I.TargetSubject + '.' + cast(I.TargetSubjectID as varchar) not in (select [key] from @nodes)

--select	* from	@links
--select	* from	@nodes

			select	(
					select	*
					from	@links O
					for json path			
					) as 'links',
					(
					select	I.*,
							C.challenges,
							E.issues
					from	@nodes I
							cross apply (
											select count(1) as challenges   
											from Workflow W            			                          
											where W.WorkflowType = 4 and W.Data.exist('/fields/ArtifactID[text() = sql:column("I.objid")]') = 1 and W.DateCompleted is null   
										) C
							cross apply (
											select count(1) as issues   
											from Workflow W            			                          
											where W.WorkflowType = 3 and W.Data.exist('/fields/ArtifactID[text() = sql:column("I.objid")]') = 1 and W.DateCompleted is null   
										) E
					for json path
					) as 'nodes'
			for json path, WITHOUT_ARRAY_WRAPPER
		end --view 1

		if @view = 2
		begin
			insert into @links
				select	distinct
						SourceSubject + '.' + cast(SourceSubjectID as varchar) as 'from',
						cast(SourceIntersectID as varchar) + '.S' as 'to',
						'Support' as category
				from	@items
				union
				select	distinct
						cast(SourceIntersectID as varchar) + '.S' as 'from',
						TargetSubject + '.' + cast(TargetSubjectID as varchar) as 'to',
						'' as category
				from	@items O
				where	(SourceObject + cast(SourceObjectID as varchar)) = (TargetObject + cast(TargetObjectID as varchar))
				union
				select	distinct
						cast(SourceIntersectID as varchar) + '.S' as 'from',
						cast(TargetIntersectID as varchar) + '.T' as 'to',
						'' as category
				from	@items O
				where	(SourceObject + cast(SourceObjectID as varchar)) <> (TargetObject + cast(TargetObjectID as varchar))
				--where	TargetIntersectID in (select SourceIntersectID from @items)
				union
				select	distinct
						cast(TargetIntersectID as varchar) + '.T' as 'from',
						TargetSubject + '.' + cast(TargetSubjectID as varchar) as 'to',
						'Support' as category
				from	@items
				where	(SourceObject + cast(SourceObjectID as varchar)) <> (TargetObject + cast(TargetObjectID as varchar))

			insert into @nodes
				select	distinct
						SourceSubject + '.' + cast(SourceSubjectID as varchar) as [key],
						SourceSubject as [obj],
						SourceSubjectID as [objid], 
						SourceSubject as [type],
						SourceSubjectTypeName as typeName,
						SourceSubjectName as name,
						SourceSubjectIconBackColor as back,
						SourceSubjectIconForeColor as fore,
						case 
							when SourceSubject = @type and SourceSubjectID = @id then 'Focal'
							else 'Normal'
						end as template,
						null as other,
						HasSourceRules
				from	@items 

			insert into @nodes
				select	distinct
						cast(SourceIntersectID as varchar) + '.S' as [key],
						SourceObject as [obj],
						SourceObjectID as [objid], 
						SourceObject as [type],
						SourceObjectTypeName as typeName,
						SourceObjectName as name,
						SourceObjectIconBackColor as back,
						SourceObjectIconForeColor as fore,
						case 
							when SourceObject = @type and SourceObjectID = @id then 'SupportFocal'
							else 'SupportNormal'
						end as template,
						null as other,
						HasSourceRules
				from	@items

			insert into @nodes
				select	distinct
						cast(TargetIntersectID as varchar) + '.T' as [key],
						TargetObject as [obj],
						TargetObjectID as [objid], 
						TargetObject as [type],
						TargetObjectTypeName as typeName,
						TargetObjectName as name,
						TargetObjectIconBackColor as back,
						TargetObjectIconForeColor as fore,
						case 
							when TargetObject = @type and TargetObjectID = @id then 'SupportFocal'
							else 'SupportNormal'
						end as template,
						null as other,
						HasSourceRules
				from	@items
				where	(SourceObject + cast(SourceObjectID as varchar)) <> (TargetObject + cast(TargetObjectID as varchar))
				--where	TargetIntersectID in (select SourceIntersectID from @items)

			insert into @nodes
				select	distinct
						TargetSubject + '.' + cast(TargetSubjectID as varchar) as [key],
						TargetSubject as [obj],
						TargetSubjectID as [objid], 
						TargetSubject as [type],
						TargetSubjectTypeName as typeName,
						TargetSubjectName as name,
						TargetSubjectIconBackColor as back,
						TargetSubjectIconForeColor as fore,
						case 
							when TargetSubject = @type and TargetSubjectID = @id then 'Focal'
							else 'Normal'
						end as template,
						null as other,
						HasSourceRules
				from	@items
				where	TargetSubject + '.' + cast(TargetSubjectID as varchar) not in (select [key] from @nodes)

			--select	* from	@links
			--select	* from	@nodes

			select	(
					select	*
					from	@links O
					for json path			
					) as 'links',
					(
					select	I.*,
							C.challenges,
							E.issues
					from	@nodes I
							cross apply (
											select count(1) as challenges   
											from Workflow W            			                          
											where W.WorkflowType = 4 and W.Data.exist('/fields/ArtifactID[text() = sql:column("I.objid")]') = 1 and W.DateCompleted is null   
										) C
							cross apply (
											select count(1) as issues   
											from Workflow W            			                          
											where W.WorkflowType = 3 and W.Data.exist('/fields/ArtifactID[text() = sql:column("I.objid")]') = 1 and W.DateCompleted is null   
										) E
					for json path			
					) as 'nodes'
			for json path, WITHOUT_ARRAY_WRAPPER
		end --view 2
	end

	if @view = 3
	begin
		IF OBJECT_ID('tempdb..#tFusionPoints') IS NOT NULL
			DROP TABLE #tFusionPoints

		create table #tFusionPoints ( ID int, MapItemID int, SourceFusionAttributeID int, TargetFusionAttributeID int, [Level] int )

		CREATE CLUSTERED INDEX CIX_#tFusionPoints ON #tFusionPoints ([ID])
		CREATE NONCLUSTERED INDEX IX_#tFusionPoints_Level ON #tFusionPoints ([Level])
		CREATE NONCLUSTERED INDEX IX_#tFusionPoints_SourceFusionAttributeID ON #tFusionPoints ([SourceFusionAttributeID])
		CREATE NONCLUSTERED INDEX IX_#tFusionPoints_TargetFusionAttributeID ON #tFusionPoints ([TargetFusionAttributeID])

		declare @tItems table (
			MapItemID int, --MapID int,

			SourceIntersectID int, 
			SourceSubjectTypeName nvarchar(500), SourceSubjectName nvarchar(500), SourceSubject varchar(50), SourceSubjectID int,
			SourceObjectTypeName nvarchar(500), SourceObjectName nvarchar(500), SourceObject varchar(50), SourceObjectID int, 
		
			TargetIntersectID int, 
			TargetSubjectTypeName nvarchar(500), TargetSubjectName nvarchar(500), TargetSubject varchar(50), TargetSubjectID int, 
			TargetObjectTypeName nvarchar(500), TargetObjectName nvarchar(500), TargetObject varchar(50), TargetObjectID int
		)
	
		if @type = 'FusionAttribute'
			begin
				insert into #tFusionPoints
					select	I.ID,
							NULL,
							I.SourceFusionAttributeID,
							I.TargetFusionAttributeID,
							0
					from	MapRuleItem I
					where	I.SourceFusionAttributeID = @id or I.TargetFusionAttributeID = @id;

				--backward-facing lineage
				set @counter = 1

				while exists(select 1 from #tFusionPoints where [Level] = @counter-1) AND @counter <= @max
				begin
					insert into #tFusionPoints
						select	S.ID,
								NULL,
								S.SourceFusionAttributeID,
								S.TargetFusionAttributeID,
								@counter
						from	MapRuleItem S
								inner join #tFusionPoints T on T.SourceFusionAttributeID = S.TargetFusionAttributeID and S.ID <> T.ID and [Level] = @counter-1
					set @counter = @counter + 1
				end

				--forward-facing lineage
				set @counter = -1

				while exists(select 1 from #tFusionPoints where [Level] = @counter+1) AND @counter >= -@max
				begin
					insert into #tFusionPoints
						select	S.ID,
								NULL,
								S.SourceFusionAttributeID,
								S.TargetFusionAttributeID,
								@counter
						from	MapRuleItem S
								inner join #tFusionPoints T on T.TargetFusionAttributeID = S.SourceFusionAttributeID and S.ID <> T.ID and [Level] = @counter+1

					set @counter = @counter - 1
				end


				-- get all items directly tied to the focal object.
				insert into @tItems
					select	MI.ID,
					
							MI.SourceIntersectID,
							SI.SubjectTypeName,
							SI.SubjectName,
							SI.Subject,
							SI.SubjectID,
							SI.ObjectTypeName,
							SI.ObjectName,
							SI.Object,
							SI.ObjectID,

							MI.TargetIntersectID,
							TI.SubjectTypeName,
							TI.SubjectName,
							TI.Subject,
							TI.SubjectID,
							TI.ObjectTypeName,
							TI.ObjectName,
							TI.Object,
							TI.ObjectID

					from	#tFusionPoints F
							inner join MapRuleItemMapItem J on J.MapRuleItemID = F.ID
							inner join MapItem MI on MI.ID = J.MapItemID
							inner join IntersectDetail SI on SI.ID = MI.SourceIntersectID
							inner join IntersectDetail TI ON TI.ID = MI.TargetIntersectID


				-- get all items not directly tied to the focal object, but still tied to maps involved above.
				insert into @tItems
					select	MI.ID,
							--NULL,
					
							MI.SourceIntersectID,
							SI.SubjectTypeName,
							SI.SubjectName,
							SI.Subject,
							SI.SubjectID,
							SI.ObjectTypeName,
							SI.ObjectName,
							SI.Object,
							SI.ObjectID,

							MI.TargetIntersectID,
							TI.SubjectTypeName,
							TI.SubjectName,
							TI.Subject,
							TI.SubjectID,
							TI.ObjectTypeName,
							TI.ObjectName,
							TI.Object,
							TI.ObjectID

					from	MapItem MI
							inner join	(
										select	ID.MapItemID
										from	MapItemMap DM
												inner join @tItems D on D.MapItemID = DM.MapItemID
												inner join MapItemMap ID on ID.MapID = DM.MapID and ID.MapItemID not in (
																														select MapItemID from @tItems
																														)
										) O on O.MapItemID = MI.ID
							inner join [IntersectDetail] SI on SI.ID = MI.SourceIntersectID
							inner join [IntersectDetail] TI on TI.ID = MI.TargetIntersectID
			end
		else
			begin
				insert into @tItems
					select	O.ID,
							--NULL,
					
							O.SourceIntersectID,
							SI.SubjectTypeName,
							SI.SubjectName,
							SI.Subject,
							SI.SubjectID,
							SI.ObjectTypeName,
							SI.ObjectName,
							SI.Object,
							SI.ObjectID,

							O.TargetIntersectID,
							TI.SubjectTypeName,
							TI.SubjectName,
							TI.Subject,
							TI.SubjectID,
							TI.ObjectTypeName,
							TI.ObjectName,
							TI.Object,
							TI.ObjectID

					from	#points O
							inner join IntersectDetail SI on SI.ID = O.SourceIntersectID
							inner join IntersectDetail TI ON TI.ID = O.TargetIntersectID

				insert into #tFusionPoints
					select	J.MapRuleItemID,
							J.MapItemID,
							T.SourceFusionAttributeID,
							T.TargetFusionAttributeID,
							0
					from	@tItems I
							inner join MapRuleItemMapItem J on J.MapItemID = I.MapItemID
							inner join MapRuleItem T on T.ID = J.MapRuleItemID
			end

			--Load tables we will return to caller.
			insert into @links
				select	distinct
						cast(S.SourceFusionAttributeID as varchar) + '.' + coalesce(B.SourceSubject, '0') + '.' + coalesce(cast(B.SourceSubjectID as varchar), '0') as [from],
						cast(S.TargetFusionAttributeID as varchar) + '.' + coalesce(B.TargetSubject, '0') + '.' + coalesce(cast(B.TargetSubjectID as varchar), '0') as [to],
						'' as category
				from	#tFusionPoints S
						left join MapRuleItemMapItem J on J.MapRuleItemID = S.ID
						left join @tItems B on B.MapItemID = J.MapItemID
			insert into @nodes
				select	distinct
						cast(S.SourceFusionAttributeID as varchar) + '.' + coalesce(B.SourceSubject, '0') + '.' + coalesce(cast(B.SourceSubjectID as varchar), '0') as [key],
						'FusionAttribute' as [obj],
						SourceFusionAttributeID as [objid], 
						'FusionAttribute' as [type],
						T.Name as typeName,
						A.TextPath as name,
						'#000' as back,
						'#fff' as fore,
						'Fusion' as template,
						B.SourceSubjectTypeName + ' : ' + B.SourceSubjectName as other,
						null
				from	#tFusionPoints S
						inner join FusionAttribute A on A.ID = S.SourceFusionAttributeID
						inner join FusionAttributeType T on T.ID = A.FusionAttributeTypeID
						left join MapRuleItemMapItem J on J.MapRuleItemID = S.ID
						left join @tItems B on B.MapItemID = J.MapItemID
			insert into @nodes
				select	distinct
						cast(S.TargetFusionAttributeID as varchar) + '.' + coalesce(B.TargetSubject, '0') + '.' + coalesce(cast(B.TargetSubjectID as varchar), '0') as [key],
						'FusionAttribute' as [obj],
						TargetFusionAttributeID as [objid], 
						'FusionAttribute' as [type],
						T.Name as typeName,
						A.TextPath as name,
						'#000' as back,
						'#fff' as fore,
						'Fusion' as template,
						B.TargetSubjectTypeName + ' : ' + B.TargetSubjectName as other,
						null
				from	#tFusionPoints S
						inner join FusionAttribute A on A.ID = S.TargetFusionAttributeID
						inner join FusionAttributeType T on T.ID = A.FusionAttributeTypeID
						left join MapRuleItemMapItem J on J.MapRuleItemID = S.ID
						left join @tItems B on B.MapItemID = J.MapItemID
				where	cast(S.TargetFusionAttributeID as varchar) + '.' + coalesce(B.TargetSubject, '0') + '.' + coalesce(cast(B.TargetSubjectID as varchar), '0') not in (select [key] from @nodes)

			delete @nodes where objid in (select objid from @nodes where other is not null) and other is null 
			delete @links where [from] not in (select [key] from @nodes) or [to] not in (select [key] from @nodes)
--select	* from	@links
--select	* from	@nodes

		select	(
				select	*
				from	@links O
				for json path			
				) as 'links',
				(
				select	*
				from	@nodes
				for json path			
				) as 'nodes'
		for json path, WITHOUT_ARRAY_WRAPPER
	end --view 3
end