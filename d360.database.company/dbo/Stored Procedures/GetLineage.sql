CREATE procedure [dbo].[GetLineage]
--declare 
	@type varchar(50),
	@id int,
	@view int = 1,
	@usageOnly bit = 0,
	@rows LineageTable readonly,
	@technicalRows LineageTechnicalTable readonly

--set @type = 'Artifact'
--set @id = 550
--set @view = 1
as
begin
	declare @links table ([from] varchar(250), [to] varchar(250), category varchar(50))
	declare @nodes table (
		[key] varchar(250), 
		obj varchar(50), [objid] int, [type] varchar(50), typeName nvarchar(250), name nvarchar(500), shortname nvarchar(500),
		back varchar(7), fore varchar(7), template varchar(50), other varchar(500),

		HasSourceRules bit
		)
	declare @objects table (Type varchar(50), ID int)
	declare @currentDepth int = 0;
	
	if @view in (0, 1, 2)
	begin
		insert into @objects values (@type, @id)

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

		declare @points table ( ID int, SourceIntersectID int, TargetIntersectID int, Depth int );
		declare @forwardPoints table ( ID int, SourceIntersectID int, TargetIntersectID int );

		-- get all items directly tied to the focal object.
		insert into @points
			select	MI.ID, MI.SourceIntersectID, MI.TargetIntersectID, 0
			from	MapItem MI
					inner join [Intersect] SI on SI.ID = MI.SourceIntersectID
					inner join [Intersect] TI ON TI.ID = MI.TargetIntersectID
					inner join @objects O on	( (SI.Subject = O.Type and SI.SubjectID = O.ID) OR (SI.Object = O.Type and SI.ObjectID = O.ID)  ) OR 
												( (TI.Subject = O.Type and TI.SubjectID = O.ID) OR (TI.Object = O.Type and TI.ObjectID = O.ID)  )
			where not exists (select 1 from @rows R where R.Deleting = 1 and R.ID = MI.ID)

		-- get all items not directly tied to the focal object, but still tied to maps involved above.
		insert into @points
			select	MI.ID, MI.SourceIntersectID, MI.TargetIntersectID, 0
			from	MapItem MI
					inner join	(
								select	ID.MapItemID
								from	MapItemMap DM
										inner join @points D on D.ID = DM.MapItemID
										inner join MapItemMap ID on ID.MapID = DM.MapID and ID.MapItemID not in (
																												select ID from @points
																												)
								) O on O.MapItemID = MI.ID
			where not exists (select 1 from @rows R where R.Deleting = 1 and R.ID = MI.ID);

		insert into @forwardPoints
			select ID,SourceIntersectID,TargetIntersectID from @points

			--join editor rows to any existing intersects
			if exists(select 1 from @rows)
			begin
				insert into @points
					select	R.ID,
							D1.ID as SourceIntersectID,
							D2.ID as TargetIntersectID,
							0
					from	@rows R
							inner join [Intersect] D1 on 
								R.SourceSubject = D1.[Subject] AND 
								R.SourceObject = D1.[Object] AND 
								R.SourceSubjectID = D1.SubjectID AND 
								R.SourceObjectID = D1.ObjectID
							inner join [Intersect] D2 on 
								R.TargetSubject = D2.[Subject] AND 
								R.TargetObject = D2.[Object] AND 
								R.TargetSubjectID = D2.SubjectID AND 
								R.TargetObjectID = D2.ObjectID
					where	R.Adding = 1 
			end;

		set @currentDepth = 0;

		while( exists(select 1 from @points ps where ps.depth = @currentDepth) and (@currentDepth < 13))
		begin

			insert into @points
				select	S.ID,
					S.SourceIntersectID,
					S.TargetIntersectID,
					@currentDepth
				from	MapItem S
						inner join @points T on T.TargetIntersectID = S.SourceIntersectID and S.ID <> T.ID and T.Depth = @currentDepth
				where	not exists (select 1 from @rows R where R.Deleting = 1 and R.ID = S.ID) and not exists (select ID from @points where ID = S.ID)

			insert into @points
				select	S.ID,
					S.SourceIntersectID,
					S.TargetIntersectID,
					@currentDepth
				from	MapItem S
						inner join @points T on T.SourceIntersectID = S.TargetIntersectID and S.ID <> T.ID and T.Depth = @currentDepth
				where	not exists (select 1 from @rows R where R.Deleting = 1 and R.ID = S.ID)
					and not exists (select ID from @points where ID = S.ID)

			set @currentDepth = @currentDepth + 1;
		end
				



		declare @items table (
			ID int,
			SourceIntersectID int, 
			SourceSubjectTypeName nvarchar(500), SourceSubjectName nvarchar(500), SourceSubjectShortName nvarchar(500), SourceSubject varchar(50), SourceSubjectID int, SourceSubjectIconBackColor varchar(7), SourceSubjectIconForeColor varchar(7), 
			SourceObjectTypeName nvarchar(500), SourceObjectName nvarchar(500), SourceObjectShortName nvarchar(500), SourceObject varchar(50), SourceObjectID int, SourceObjectIconBackColor varchar(7), SourceObjectIconForeColor varchar(7),
			
			TargetIntersectID int, 
			TargetSubjectTypeName nvarchar(500), TargetSubjectName nvarchar(500), TargetSubjectShortName nvarchar(500), TargetSubject varchar(50), TargetSubjectID int, TargetSubjectIconBackColor varchar(7), TargetSubjectIconForeColor varchar(7), 
			TargetObjectTypeName nvarchar(500), TargetObjectName nvarchar(500), TargetObjectShortName nvarchar(500), TargetObject varchar(50), TargetObjectID int, TargetObjectIconBackColor varchar(7), TargetObjectIconForeColor varchar(7),

			SourceHasSourceRules bit, TargetHasSourceRules bit
		)

		insert into @items
			select	O.ID,				
					O.SourceIntersectID,
					SI.SubjectTypeName,
					SI.SubjectName,
					SI.SubjectShortName,
					SI.Subject,
					SI.SubjectID,
					SI.SubjectIconBackColor,
					SI.SubjectIconForeColor,
					SI.ObjectTypeName,
					SI.ObjectName,
					SI.ObjectShortName,
					SI.Object,
					SI.ObjectID,
					SI.ObjectIconBackColor,
					SI.ObjectIconForeColor,
					O.TargetIntersectID,
					TI.SubjectTypeName,
					TI.SubjectName,
					TI.SubjectShortName,
					TI.Subject,
					TI.SubjectID,
					TI.SubjectIconBackColor,
					TI.SubjectIconForeColor,
					TI.ObjectTypeName,
					TI.ObjectName,
					TI.ObjectShortName,
					TI.Object,
					TI.ObjectID,
					TI.ObjectIconBackColor,
					TI.ObjectIconForeColor,
					case 
						when SHSR.C > 0 then cast(1 as bit)
						else cast(0 as bit)
					end as SourceHasSourceRules,
										case 
						when THSR.C > 0 then cast(1 as bit)
						else cast(0 as bit)
					end as TargetHasSourceRules
			from	@points O
					inner join IntersectDetail SI on SI.ID = O.SourceIntersectID
					inner join IntersectDetail TI ON TI.ID = O.TargetIntersectID
					cross apply (
								select	count(1) as C
								from	MapItem MI 
										inner join MapSequence MS on MS.MapItemID = MI.ID
								where MI.ID = O.ID and (
									(@type = SI.[subject] and @id = SI.subjectid and
										(
											MI.SourceIntersectID in 
											(select id from [intersect] i where 
												(i.[object] = @type and i.objectid = @id) or
												(i.[subject] = @type and i.subjectid = @id)
												)
										)
									)
									or
									(MI.SourceIntersectID in 
										(select id from [intersect] i where
												(i.[object] = @type and i.objectid = @id and i.[subject] = si.[subject] and i.subjectid = si.subjectid) or
												(i.[subject] = @type and i.subjectid = @id and i.[object] = si.[subject] and i.objectid = si.subjectid)
										)
									)

									)
									
								) SHSR
								cross apply (
								select	count(1) as C
								from	MapItem MI 
										inner join MapSequence MS on MS.MapItemID = MI.ID
								where MI.ID = O.ID and (
									(@type = TI.[subject] and @id = TI.subjectid and
										(
											MI.TargetIntersectID in 
											(select id from [intersect] i where 
												(i.[object] = @type and i.objectid = @id) or
												(i.[subject] = @type and i.subjectid = @id)
												)
										)
									)
									or
									(MI.TargetIntersectID in 
										(select id from [intersect] i where
												(i.[object] = @type and i.objectid = @id and i.[subject] = ti.[subject] and i.subjectid = ti.subjectid) or
												(i.[subject] = @type and i.subjectid = @id and i.[object] = ti.[subject] and i.objectid = ti.subjectid)
										)
									)

									)
									
								) THSR


		--if editor data is being passed
		if EXISTS (SELECT 1 FROM @rows)
		begin
			--remove deleting items
			delete I
			from @items I
			inner join @rows R on R.Deleting = 1  
				AND R.SourceSubjectID = I.SourceSubjectID 
				AND R.SourceObjectID = I.SourceObjectID
				AND R.TargetSubjectID = I.TargetSubjectID
				AND R.TargetObjectID = I.TargetObjectID;

			--insert adding items and fill in missing data
			insert into @items
			select
				R.ID,
				R.SourceIntersectID,
				SS.ObjectTypeName as SourceSubjectTypeName,
				coalesce(SS.TextPath, SS.Name) as SourceSubjectName,
				SS.Name as SourceSubjectShortName,
				R.SourceSubject,
				R.SourceSubjectID,
				SS.IconBackColor as SourceSubjectIconBackColor,
				SS.IconForeColor as SourceSubjectIconForeColor,
				SO.ObjectTypeName as SourceObjectTypeName,
				coalesce(SO.TextPath, SO.Name) as SourceObjectName,
				SO.Name as SourceObjectShortName,
				R.SourceObject,
				R.SourceObjectID,
				SO.IconBackColor as SourceObjectIconBackColor,
				SO.IconForeColor as SourceObjectIconForeColor,
				R.TargetIntersectID,
				TS.ObjectTypeName as TargetSubjectTypeName,
				coalesce(TS.TextPath, TS.Name) as TargetSubjectName,
				TS.Name as TargetSubjectShortName,
				R.TargetSubject,
				R.TargetSubjectID,
				TS.IconBackColor as TargetSubjectIconBackColor,
				TS.IconForeColor as TargetSubjectIconForeColor,
				TB.ObjectTypeName as TargetObjectTypeName,
				coalesce(TB.TextPath, TB.Name)  as TargetObjectName,
				TB.Name as TargetObjectShortName,
				R.TargetObject,
				R.TargetObjectID,
				TB.IconBackColor as TargetObjectIconBackColor,
				TB.IconForeColor as TargetObjectIconForeColor,
				0 as SourceHasSourceRules,
				0 as TargetHasSourceRules
			from @rows R 
			inner join cache.ObjectDetails SS on SS.[Object] = R.SourceSubject AND SS.ObjectID = R.SourceSubjectID
			inner join cache.ObjectDetails SO on SO.[Object] = R.SourceObject AND SO.ObjectID = R.SourceObjectID
			inner join cache.ObjectDetails TS on TS.[Object] = R.TargetSubject AND TS.ObjectID = R.TargetSubjectID
			inner join cache.ObjectDetails TB on TB.[Object] = R.TargetObject AND TB.ObjectID = R.TargetObjectID
			where R.Adding = 1;
		end
		
		if @view = 0
		begin
			select (
					select distinct
					cast(SI.IntersectTypeID as varchar) + '.' 
					+ cast(I.SourceSubjectID as varchar) + '.'
					+ cast(I.SourceObjectID as varchar) as [sourcekey],
					cast(TI.IntersectTypeID as varchar) + '.' 
					+ cast(I.TargetSubjectID as varchar) + '.'
					+ cast(I.TargetObjectID as varchar) as [targetkey],
					I.*,
					SI.IntersectTypeID as SourceIntersectTypeID,
					SIT.[Name] as SourceIntersectTypeName,
					TI.IntersectTypeID as TargetIntersectTypeID,
					TIT.[Name] as TargetIntersectTypeName
				from @items I
				inner join [Intersect] SI on SI.ID = I.SourceIntersectID
				inner join IntersectType SIT on SIT.ID = SI.IntersectTypeID
				inner join [Intersect] TI on TI.ID = I.TargetIntersectID
				inner join IntersectType TIT on TIT.ID = TI.IntersectTypeID
				for json path
			) as 'items'
			for json path, WITHOUT_ARRAY_WRAPPER
		end

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
							I.SourceSubjectShortName as shortname,
							I.SourceSubjectIconBackColor as back,
							I.SourceSubjectIconForeColor as fore,
							case 
								when I.SourceSubject = @type and I.SourceSubjectID = @id then 'Focal'
								else 'Normal'
							end as template,
							null as other,
							0 as hasSourceRules
					from	@items I;

					--perform this update separately to avoid duplicate in the above query
					update n
					set n.HasSourceRules = 1
					from @nodes n
					inner join @items i on n.[key] = (i.SourceSubject + '.' + cast(i.SourceSubjectID as varchar)) and i.SourceHasSourceRules = 1;

			--insert into @nodes
			merge	@nodes as T
			using	(
					select	distinct
							I.TargetSubject + '.' + cast(I.TargetSubjectID as varchar) as [key],
							I.TargetSubject as [obj],
							I.TargetSubjectID as [objid], 
							I.TargetSubject as [type],
							I.TargetSubjectTypeName as typeName,
							I.TargetSubjectName as name,
							I.TargetSubjectShortName as shortname,
							I.TargetSubjectIconBackColor as back,
							I.TargetSubjectIconForeColor as fore,
							case 
								when I.TargetSubject = @type and I.TargetSubjectID = @id then 'Focal'
								else 'Normal'
							end as template,
							null as other,
							I.TargetHasSourceRules as HasSourceRules
					from	@items I
					where	I.TargetSubject + '.' + cast(I.TargetSubjectID as varchar) not in (select [key] from @nodes)
					) S
			on		(T.[key] = S.[key])
			when matched then
			update	set
					T.HasSourceRules = S.HasSourceRules
			when	not matched then
			insert	([key], obj, [objid], [type], typeName, name, shortname, back, fore, template, other, HasSourceRules)
			values	(S.[key], S.obj, S.[objid], S.[type], S.typeName, S.name, S.shortname, S.back, S.fore, S.template, S.other, S.HasSourceRules);
					--where	I.TargetSubject + '.' + cast(I.TargetSubjectID as varchar) not in (select [key] from @nodes)

			if @usageOnly = 1 --Remove elements that are not tied to the current object via any Usage predicate type.
			begin
				delete	@nodes
				where	[key] not in 
					(
					--DIRECTLY related to an item via Usage relationship
					select	case 
								when (I.Subject = N.obj and I.SubjectID = N.objid and I.SubjectID <> @id and I.Object = @type and I.ObjectID = @id) then I.Subject + '.' + cast(I.SubjectID as varchar)
								else I.Object + '.' + cast(I.ObjectID as varchar)
							end as [key]
					from	[Intersect] I
							inner join @nodes N on	(
													(I.Subject = N.obj and I.SubjectID = N.objid and I.SubjectID <> @id and I.Object = @type and I.ObjectID = @id) OR
													(I.Object = N.obj and I.ObjectID = N.objid and I.ObjectID <> @id and I.Subject = @type and I.SubjectID = @id)
													)
							inner join IntersectType T on T.ID = I.IntersectTypeID and (I.Deleted = 0 or I.Deleted is null)
							inner join [Predicate] P on P.ID = T.PredicateID and P.[Type] = 10
					union
					--INDIRECTLY related to an item via Usage relationship
					select	N.obj + '.' + cast(N.objid as varchar) as [key]
					from	[Intersect] I1
							inner join [Intersect] I2 on I1.Subject = @type and I1.SubjectID = @id 
														and (
																--(I2.Subject = I1.Subject and I2.SubjectID = I1.SubjectID) --OR
																(I2.Object = I1.Object and I2.ObjectID = I1.ObjectID)
															)
														and I1.ID <> I2.ID
							inner join IntersectType T on T.ID = I2.IntersectTypeID 
														and (I1.Deleted = 0 or I1.Deleted is null)
														and (I2.Deleted = 0 or I2.Deleted is null) 
							inner join @nodes N on (I2.Subject = N.obj and I2.SubjectID = N.objid)
							inner join [Predicate] P on P.ID = T.PredicateID and P.[Type] = 10
					) and [key] <> @type + '.' + cast(@id as varchar)
			end

--select	* from	@links
--select	* from	@nodes

			select	(
					select	*
					from	@links O
					for json path			
					) as 'links',
					(
					select	I.*,
							A.actions
					from	@nodes I
							cross apply (
											select count(1) as actions, max(createdon) as createdon   
											from Issue U
											where U.ObjectID = I.objid AND U.Object = I.obj
										) A
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
						SourceSubjectShortName as shortname,
						SourceSubjectIconBackColor as back,
						SourceSubjectIconForeColor as fore,
						case 
							when SourceSubject = @type and SourceSubjectID = @id then 'Focal'
							else 'Normal'
						end as template,
						null as other,
						0 as HasSourceRules
				from	@items 

			insert into @nodes
				select	distinct
						cast(SourceIntersectID as varchar) + '.S' as [key],
						SourceObject as [obj],
						SourceObjectID as [objid], 
						SourceObject as [type],
						SourceObjectTypeName as typeName,
						SourceObjectName as name,
						SourceObjectShortName as shortname,
						SourceObjectIconBackColor as back,
						SourceObjectIconForeColor as fore,
						case 
							when SourceObject = @type and SourceObjectID = @id then 'SupportFocal'
							else 'SupportNormal'
						end as template,
						null as other,
						0 as HasSourceRules
				from	@items

				update n
				set n.HasSourceRules = 1
				from @nodes n
				inner join @items i on n.[key] = (i.SourceSubject + '.' + cast(i.SourceSubjectID as varchar)) and i.SourceHasSourceRules = 1;


			merge	@nodes as T
			using	(
					select	distinct
							cast(TargetIntersectID as varchar) + '.T' as [key],
							TargetObject as [obj],
							TargetObjectID as [objid], 
							TargetObject as [type],
							TargetObjectTypeName as typeName,
							TargetObjectName as name,
							TargetObjectShortName as shortname,
							TargetObjectIconBackColor as back,
							TargetObjectIconForeColor as fore,
							case 
								when TargetObject = @type and TargetObjectID = @id then 'SupportFocal'
								else 'SupportNormal'
							end as template,
							null as other,
							TargetHasSourceRules as HasSourceRules
					from	@items
					where	(SourceObject + cast(SourceObjectID as varchar)) <> (TargetObject + cast(TargetObjectID as varchar))
					) S
			on		(T.[key] = S.[key])
			when	matched then
			update	set
					T.HasSourceRules = S.HasSourceRules
			when	not matched then
			insert	([key], obj, [objid], [type], typeName, name, shortname, back, fore, template, other, HasSourceRules)
			values	(S.[key], S.obj, S.[objid], S.[type], S.typeName, S.name, S.shortname, S.back, S.fore, S.template, S.other, S.HasSourceRules);

			merge	@nodes as T
			using	(
					select	distinct
							TargetSubject + '.' + cast(TargetSubjectID as varchar) as [key],
							TargetSubject as [obj],
							TargetSubjectID as [objid], 
							TargetSubject as [type],
							TargetSubjectTypeName as typeName,
							TargetSubjectName as name,
							TargetSubjectShortName as shortname,
							TargetSubjectIconBackColor as back,
							TargetSubjectIconForeColor as fore,
							case 
								when TargetSubject = @type and TargetSubjectID = @id then 'Focal'
								else 'Normal'
							end as template,
							null as other,
							TargetHasSourceRules as HasSourceRules
					from	@items
					where	TargetSubject + '.' + cast(TargetSubjectID as varchar) not in (select [key] from @nodes)
					) S
			on		(T.[key] = S.[key])
			when	matched then
			update	set
					T.HasSourceRules = S.HasSourceRules
			when	not matched then
			insert	([key], obj, [objid], [type], typeName, name, shortname, back, fore, template, other, HasSourceRules)
			values	(S.[key], S.obj, S.[objid], S.[type], S.typeName, S.name, S.shortname, S.back, S.fore, S.template, S.other, S.HasSourceRules);

--select	* from	@links
--select	* from	@nodes

			if @usageOnly = 1 --Remove elements that are not tied to the current object via any Usage predicate type.
			begin
				declare @usages table ([key] varchar(250))

				insert into @usages
					--DIRECTLY related to an item via Usage relationship
					select	--*,
							case 
								when (I.Subject = N.obj and I.SubjectID = N.objid and I.SubjectID <> @id and I.Object = @type and I.ObjectID = @id) then I.Subject + '.' + cast(I.SubjectID as varchar)
								else I.Object + '.' + cast(I.ObjectID as varchar)
							end as [key]
					from	[Intersect] I
							inner join @nodes N on	(
													(I.Subject = N.obj and I.SubjectID = N.objid and I.SubjectID <> @id and I.Object = @type and I.ObjectID = @id) OR
													(I.Object = N.obj and I.ObjectID = N.objid and I.ObjectID <> @id and I.Subject = @type and I.SubjectID = @id)
													)
							inner join IntersectType T on T.ID = I.IntersectTypeID and (I.Deleted = 0 or I.Deleted is null)
							inner join [Predicate] P on P.ID = T.PredicateID and P.[Type] = 10
					union
					--INDIRECTLY related to an item via Usage relationship
					select	N.obj + '.' + cast(N.objid as varchar) as [key]
					from	[Intersect] I1
							inner join [Intersect] I2 on I1.Subject = @type and I1.SubjectID = @id 
														and (
																--(I2.Subject = I1.Subject and I2.SubjectID = I1.SubjectID) --OR
																(I2.Object = I1.Object and I2.ObjectID = I1.ObjectID)
															)
														and I1.ID <> I2.ID
							inner join IntersectType T on T.ID = I2.IntersectTypeID 
														and (I1.Deleted = 0 or I1.Deleted is null)
														and (I2.Deleted = 0 or I2.Deleted is null) 
							inner join @nodes N on (I2.Subject = N.obj and I2.SubjectID = N.objid)
							inner join [Predicate] P on P.ID = T.PredicateID and P.[Type] = 10

				delete	@nodes
				where	[key] not in 
					(
					select	[key]
					from	@usages
					) 
					and [key] <> @type + '.' + cast(@id as varchar)
					and [template] not like '%Support%'

				delete	@links
				where	[from] not in (select [key] from @nodes)
						or [to] not in (select [key] from @nodes)
				
				delete	@nodes
				where	[template] like '%Support%'
						and [key] not in (
							select	[key]
							from	@nodes 
							where	[template] like '%Support%'
									and [key] in (select [from] from @links)
									and [key] in (select [to] from @links)
						)
			end

--select	* from	@items
--select	* from	@links
--select	* from	@nodes

			select	(
					select	*
					from	@links O
					for json path			
					) as 'links',
					(
					select	I.*,
							A.actions
					from	@nodes I
							cross apply (
											select count(1) as actions, max(createdon) as createdon   
											from Issue U
											where U.ObjectID = I.objid AND U.Object = I.obj
										) A
					for json path			
					) as 'nodes'
			for json path, WITHOUT_ARRAY_WRAPPER
		end --view 2
	end

	if @view in (3,4)
	begin
	
		--declare @tFusionPoints table (	ID int, MapItemID int, SourceFusionAttributeID int, TargetFusionAttributeID int);
		--declare @tFusionPoints table (	ID int, MapItemID int, SourceFusionAttributeID int, TargetFusionAttributeID int, Depth int);

		IF OBJECT_ID('tempdb..#tFusionPoints') IS NOT NULL
			DROP TABLE #tFusionPoints;

		create table #tFusionPoints (ID int, MapItemID int, SourceFusionAttributeID int, TargetFusionAttributeID int, Depth int, Direction char null);

		CREATE CLUSTERED INDEX PK_temptFusionPoints ON #tFusionPoints ([ID] ASC,[SourceFusionAttributeID] ASC,[TargetFusionAttributeID] ASC, [Depth] ASC, [Direction] ASC);

		declare @tItems table (
			MapItemID int, --MapID int,

			SourceIntersectID int, 
			SourceSubjectTypeName nvarchar(500), SourceSubjectName nvarchar(500), SourceSubjectShortName nvarchar(500), SourceSubject varchar(50), SourceSubjectID int,
			SourceObjectTypeName nvarchar(500), SourceObjectName nvarchar(500), SourceObjectShortName nvarchar(500), SourceObject varchar(50), SourceObjectID int, 
			
			TargetIntersectID int, 
			TargetSubjectTypeName nvarchar(500), TargetSubjectName nvarchar(500), TargetSubjectShortName nvarchar(500), TargetSubject varchar(50), TargetSubjectID int, 
			TargetObjectTypeName nvarchar(500), TargetObjectName nvarchar(500), TargetObjectShortName nvarchar(500), TargetObject varchar(50), TargetObjectID int
		)
	
		if @type = 'FusionAttribute'
			begin
			

				-- iterative approach no cte
				-- insert the starting points
				insert into #tFusionPoints
					select I.ID,
							NULL,
							I.SourceFusionAttributeID,
							I.TargetFusionAttributeID, 
							0,
							'A'
					from	MapRuleItem I
							inner join FusionAttribute SFA on SFA.ID = I.SourceFusionAttributeID and SFA.Deleted = 0
							inner join FusionAttribute TFA on TFA.ID = I.TargetFusionAttributeID and TFA.Deleted = 0
					where	I.SourceFusionAttributeID = @id --or I.TargetFusionAttributeID = @id;

				insert into #tFusionPoints
					select	I.ID,
							NULL,
							I.SourceFusionAttributeID,
							I.TargetFusionAttributeID,
							0,
							'A'
					from	MapRuleItem I
							inner join FusionAttribute SFA on SFA.ID = I.SourceFusionAttributeID and SFA.Deleted = 0
							inner join FusionAttribute TFA on TFA.ID = I.TargetFusionAttributeID and TFA.Deleted = 0
					where	I.TargetFusionAttributeID = @id and 
						not exists (select 1 from #tFusionPoints pt where pt.SourceFusionAttributeID = I.TargetFusionAttributeID and pt.TargetFusionAttributeID = I.SourceFusionAttributeID)
						
						


				--insert adding items if editor data is present
				if EXISTS (SELECT 1 FROM @technicalRows)
				begin
					insert into #tFusionPoints
					select
						ID,
						MapItemID,
						SourceFusionAttributeID,
						TargetFusionAttributeID,
						0,
						'A'
					from @technicalRows
					where Adding = 1;
				end;

				--loop through until there are no more new levels
				--declare @currentDepth int = 0;
				set @currentDepth = 0;

				while( exists(select 1 from #tFusionPoints ps where ps.depth = @currentDepth) and (@currentDepth < 13))
				begin
					
					insert into #tFusionPoints
						select distinct	S.ID,
								NULL,
								S.SourceFusionAttributeID,
								S.TargetFusionAttributeID,
								@currentDepth+1,
								'F'
						from	MapRuleItem S
								inner join #tFusionPoints FP on FP.SourceFusionAttributeID = S.TargetFusionAttributeID and FP.Depth = @currentDepth and FP.Direction in('A','F')
						where not exists (select ID from #tFusionPoints where ID = S.ID)
							and not exists (select 1 from @technicalRows R where Deleting = 1 and R.ID = S.ID);

					insert into #tFusionPoints
						select distinct	S.ID,
								NULL,
								S.SourceFusionAttributeID,
								S.TargetFusionAttributeID,
								@currentDepth+1,
								'B'
						from	MapRuleItem S
								inner join #tFusionPoints FP on FP.TargetFusionAttributeID = S.SourceFusionAttributeID and FP.Depth = @currentDepth and FP.Direction in('A','B')
						where not exists (select ID from #tFusionPoints where ID = S.ID) 
								and not exists (select 1 from @technicalRows R where Deleting = 1 and R.ID = S.ID);

						set @currentDepth = @currentDepth + 1;

						print @currentDepth;
				end
				

				--remove deleting items if editor data is present
				if EXISTS (SELECT 1 FROM @technicalRows)
				begin
					
					delete I
					from #tFusionPoints I
					inner join @technicalRows R on R.Deleting = 1 AND R.ID = I.ID;
				end

				-- get all items directly tied to the focal object.
				insert into @tItems
					select	MI.ID,
					
							MI.SourceIntersectID,
							SI.SubjectTypeName,
							SI.SubjectName,
							SI.SubjectShortName,
							SI.Subject,
							SI.SubjectID,
							SI.ObjectTypeName,
							SI.ObjectName,
							SI.ObjectShortName,
							SI.Object,
							SI.ObjectID,

							MI.TargetIntersectID,
							TI.SubjectTypeName,
							TI.SubjectName,
							TI.SubjectShortName,
							TI.Subject,
							TI.SubjectID,
							TI.ObjectTypeName,
							TI.ObjectName,
							TI.ObjectShortName,
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
							SI.SubjectShortName,
							SI.Subject,
							SI.SubjectID,
							SI.ObjectTypeName,
							SI.ObjectName,
							SI.ObjectShortName,
							SI.Object,
							SI.ObjectID,

							MI.TargetIntersectID,
							TI.SubjectTypeName,
							TI.SubjectName,
							TI.SubjectShortName,
							TI.Subject,
							TI.SubjectID,
							TI.ObjectTypeName,
							TI.ObjectName,
							TI.ObjectShortName,
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
				declare @tBusinessPoints table ( ID int, SourceIntersectID int, TargetIntersectID int )

				insert into @objects values (@type, @id)

				if not exists(
					select	MI.ID
					from	MapItem MI
							inner join [Intersect] SI on SI.ID = MI.SourceIntersectID --IntersectDetail
							inner join [Intersect] TI ON TI.ID = MI.TargetIntersectID --IntersectDetail
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
								inner join [Predicate] P on P.ID = T.PredicateID and P.Type = 6
						where	(I.Subject = @type and I.SubjectID = @id) or (I.Object = @type and I.ObjectID = @id)
				end

				-- get all items directly tied to the focal object.
				insert into @tBusinessPoints
					select	MI.ID, MI.SourceIntersectID, MI.TargetIntersectID
					from	MapItem MI
							inner join [Intersect] SI on SI.ID = MI.SourceIntersectID
							inner join [Intersect] TI ON TI.ID = MI.TargetIntersectID
							inner join @objects O on	( (SI.Subject = O.Type and SI.SubjectID = O.ID) OR (SI.Object = O.Type and SI.ObjectID = O.ID)  ) OR 
														( (TI.Subject = O.Type and TI.SubjectID = O.ID) OR (TI.Object = O.Type and TI.ObjectID = O.ID)  )

				-- get all items not directly tied to the focal object, but still tied to maps involved above.
				insert into @tBusinessPoints
					select	MI.ID, MI.SourceIntersectID, MI.TargetIntersectID
					from	MapItem MI
							inner join	(
										select	ID.MapItemID
										from	MapItemMap DM
												inner join @tBusinessPoints D on D.ID = DM.MapItemID
												inner join MapItemMap ID on ID.MapID = DM.MapID and ID.MapItemID not in (
																														select ID from @tBusinessPoints
																														)
										) O on O.MapItemID = MI.ID;

				with cte as (
					select	ID,
							SourceIntersectID,
							TargetIntersectID,
							1 as [Level]
					from	@tBusinessPoints
					union all
					select	S.ID,
							S.SourceIntersectID,
							S.TargetIntersectID,
							T.[Level] + 1 as [Level]
					from	MapItem S
							inner join cte T on T.SourceIntersectID = S.TargetIntersectID and S.ID <> T.ID
					where	T.[Level] <= 25
				)
				insert into @tBusinessPoints
					select ID, SourceIntersectID, TargetIntersectID from cte where ID not in (select ID from @tBusinessPoints)

				insert into @tItems
					select	O.ID,
							--NULL,
					
							O.SourceIntersectID,
							SI.SubjectTypeName,
							SI.SubjectName,
							SI.SubjectShortName,
							SI.Subject,
							SI.SubjectID,
							SI.ObjectTypeName,
							SI.ObjectName,
							SI.ObjectShortName,
							SI.Object,
							SI.ObjectID,

							O.TargetIntersectID,
							TI.SubjectTypeName,
							TI.SubjectName,
							TI.SubjectShortName,
							TI.Subject,
							TI.SubjectID,
							TI.ObjectTypeName,
							TI.ObjectName,
							TI.ObjectShortName,
							TI.Object,
							TI.ObjectID

					from	@tBusinessPoints O
							inner join IntersectDetail SI on SI.ID = O.SourceIntersectID
							inner join IntersectDetail TI ON TI.ID = O.TargetIntersectID

							--select * from @tItems;

				insert into #tFusionPoints
					select	J.MapRuleItemID,
							J.MapItemID,
							T.SourceFusionAttributeID,
							T.TargetFusionAttributeID,
							0,
							'A'
					from	@tItems I
							inner join MapRuleItemMapItem J on J.MapItemID = I.MapItemID
							inner join MapRuleItem T on T.ID = J.MapRuleItemID
							inner join FusionAttribute SFA on SFA.ID = T.SourceFusionAttributeID and SFA.Deleted = 0
							inner join FusionAttribute TFA on TFA.ID = T.TargetFusionAttributeID and TFA.Deleted = 0;

				
				--insert adding items if editor data is passed
				if EXISTS (SELECT 1 FROM @technicalRows)
				begin
					insert into #tFusionPoints
					select
						ID,
						MapItemID,
						SourceFusionAttributeID,
						TargetFusionAttributeID,
						0,
						'A'
					from @technicalRows
					where Adding = 1;
				end;


	

				-- begin iterative version
				--loop through until there are no more new levels
				set @currentDepth = 0;
				
				while( exists(select 1 from #tFusionPoints ps where ps.depth = @currentDepth) and (@currentDepth < 13) )
				begin
					
					insert into #tFusionPoints
						select distinct	S.ID,
								NULL,
								S.SourceFusionAttributeID,
								S.TargetFusionAttributeID,
								@currentDepth+1,
								'F'
						from	MapRuleItem S
								inner join #tFusionPoints FP on FP.SourceFusionAttributeID = S.TargetFusionAttributeID and FP.Depth = @currentDepth and FP.Direction in('A','F')
						where not exists (select ID from #tFusionPoints where ID = S.ID)
							and not exists (select 1 from @technicalRows R where Deleting = 1 and R.ID = S.ID);

					insert into #tFusionPoints
						select distinct	S.ID,
								NULL,
								S.SourceFusionAttributeID,
								S.TargetFusionAttributeID,
								@currentDepth+1,
								'B'
						from	MapRuleItem S
								inner join #tFusionPoints FP on FP.TargetFusionAttributeID = S.SourceFusionAttributeID and FP.Depth = @currentDepth and FP.Direction in('A','B')
						where not exists (select ID from #tFusionPoints where ID = S.ID) 
								and not exists (select 1 from @technicalRows R where Deleting = 1 and R.ID = S.ID);

						set @currentDepth = @currentDepth + 1;

						print @currentDepth;
				end

				-- end iterative version

				--remove deleting items if editor data is present
				if EXISTS (SELECT 1 FROM @technicalRows)
				begin
					delete I
					from #tFusionPoints I
					inner join @technicalRows R on R.Deleting = 1 AND R.ID = I.ID;
				end;
			end

		if @view = 3
		begin
		--Load tables we will return to caller.
		insert into @links
			select	distinct
					cast(S.SourceFusionAttributeID as varchar),-- + '.' + coalesce(B.SourceSubject, '0') + '.' + coalesce(cast(B.SourceSubjectID as varchar), '0') as [from],
					cast(S.TargetFusionAttributeID as varchar),-- + '.' + coalesce(B.TargetSubject, '0') + '.' + coalesce(cast(B.TargetSubjectID as varchar), '0') as [to],
					'' as category
			from	#tFusionPoints S
					left join MapRuleItemMapItem J on J.MapRuleItemID = S.ID
					left join @tItems B on B.MapItemID = J.MapItemID
		insert into @nodes
			select	distinct
					cast(S.SourceFusionAttributeID as varchar),-- + '.' + coalesce(B.SourceSubject, '0') + '.' + coalesce(cast(B.SourceSubjectID as varchar), '0') as [key],
					'FusionAttribute' as [obj],
					SourceFusionAttributeID as [objid], 
					'FusionAttribute' as [type],
					T.Name as typeName,
					A.TextPath as name,
					A.Name as shortname,
					COALESCE(ST.IconBackColor, '#000') as back,
					COALESCE(ST.IconForeColor, '#fff') as fore,
					'Fusion' as template,
					B.SourceSubjectTypeName + ' : ' + B.SourceSubjectName as other,
					null
			from	#tFusionPoints S
					inner join FusionAttribute A on A.ID = S.SourceFusionAttributeID
					inner join FusionAttributeType T on T.ID = A.FusionAttributeTypeID
					left join ObjectStyle ST on ST.ObjectType = 'FusionAttributeType' and ST.ObjectID = T.ID
					left join MapRuleItemMapItem J on J.MapRuleItemID = S.ID
					left join @tItems B on B.MapItemID = J.MapItemID
		insert into @nodes
			select	distinct
					cast(S.TargetFusionAttributeID as varchar),-- + '.' + coalesce(B.TargetSubject, '0') + '.' + coalesce(cast(B.TargetSubjectID as varchar), '0') as [key],
					'FusionAttribute' as [obj],
					TargetFusionAttributeID as [objid], 
					'FusionAttribute' as [type],
					T.Name as typeName,
					A.TextPath as name,
					A.Name as shortname,
					COALESCE(ST.IconBackColor, '#000') as back,
					COALESCE(ST.IconForeColor, '#fff') as fore,
					'Fusion' as template,
					B.TargetSubjectTypeName + ' : ' + B.TargetSubjectName as other,
					null
			from	#tFusionPoints S
					inner join FusionAttribute A on A.ID = S.TargetFusionAttributeID
					inner join FusionAttributeType T on T.ID = A.FusionAttributeTypeID
					left join ObjectStyle ST on ST.ObjectType = 'FusionAttributeType' and ST.ObjectID = T.ID
					left join MapRuleItemMapItem J on J.MapRuleItemID = S.ID
					left join @tItems B on B.MapItemID = J.MapItemID
			where	cast(S.TargetFusionAttributeID as varchar) + '.' + coalesce(B.TargetSubject, '0') + '.' + coalesce(cast(B.TargetSubjectID as varchar), '0') not in (select [key] from @nodes)

			--gets rid of dupes
			delete	@nodes 
			where	other is null 
					and (obj + cast([objid] as varchar)) in (
															select	(obj + cast([objid] as varchar))
															from	@nodes 
															where	other is not null
															)
			delete	T
			from	@links T
					left join @nodes S on S.[key] = T.[from] or S.[key] = T.[to]
			where	S.[key] is null
			
			select	(
					select	*
					from	@links O
					for json path			
					) as 'links',
					(
					select	distinct
							*
					from	@nodes
					for json path			
					) as 'nodes'
			for json path, WITHOUT_ARRAY_WRAPPER
		end --view 3

		if @view = 4
		begin
			select (
				select distinct
					F.ID,
					I.MapItemID,
					F.SourceFusionAttributeID,
					FS.TextPath as SourceFusionAttributeName,
					F.TargetFusionAttributeID,
					FT.TextPath as TargetFusionAttributeName 
				from #tFusionPoints F
				left join @tItems I on I.MapItemID = F.MapItemID
				inner join FusionAttribute FS on FS.ID = F.SourceFusionAttributeID
				inner join FusionAttribute FT on FT.ID = F.TargetFusionAttributeID
				for json path
				) as 'items'
			for json path, WITHOUT_ARRAY_WRAPPER
		end --view 4
	end
end