CREATE NONCLUSTERED INDEX [IX_CacheAssetResponsibility_Asset]
    ON [cache].[AssetResponsibility]([AssetID] ASC, [Overriden] ASC)
    INCLUDE([ResponsibilityTypeID], [SecurityAsset], [SecurityAssetID]);
GO

CREATE NONCLUSTERED INDEX [IX_CacheAssetResponsibility_Object_ObjectID_Type_TypeID_SecurityAsset_SecurityAssetID]
    ON [cache].[AssetResponsibility]([Object] ASC, [ObjectID] ASC, [Type] ASC, [TypeID] ASC, [SecurityAsset] ASC, [SecurityAssetID] ASC);
GO

DROP INDEX [IX_CacheAssetResponsibility_RuleID_OverrideItemID] ON [cache].[AssetResponsibility];
GO

CREATE NONCLUSTERED INDEX [IX_Field_AssetID_Include]
    ON [dbo].[Field]([AssetID] ASC)
    INCLUDE([FieldTypeID], [Value], [FormattedValue]);
GO

alter view [dbo].[SecurityDetail]
as
	select	'R' as ResponsibleObjectType,
			RD.ResourceID as ResponsibleObjectID,
			RD.Object as Object,
			RD.ObjectID as ObjectID,
			cast(0 as bit) as IsType,
			RTC.Claim,
			RTC.ClaimObject
	from	ResponsibilityDetails RD
			inner join ResponsibilityTypeObjectClaim RTC	on RTC.ResponsibilityTypeID = RD.ResponsibilityTypeID 
																and RTC.ObjectType = RD.Type 
																and RTC.ObjectID = RD.TypeID
	where RD.ApplyToType = 0
	union all
		select	distinct 
		    'R' as ResponsibleObjectType,
			RD.ResourceID as ResponsibleObjectID,
			 RD.Type as Object,
			 RD.TypeID as ObjectID,
			cast(1 as bit) as IsType,
			RTC.Claim,
			RTC.ClaimObject
	from	ResponsibilityDetails RD
			inner join ResponsibilityTypeObjectClaim RTC	on RTC.ResponsibilityTypeID = RD.ResponsibilityTypeID 
																and RTC.ObjectType = RD.Type 
																and RTC.ObjectID = RD.TypeID
	where RD.ApplyToType = 1
GO

alter view [responsibility].[Core]
as
	select	distinct 
			RuleID,
			ResponsibilityTypeID,
			Object,
			ObjectID,
			AssetID,
			AssetTypeID,
			Type,
			TypeID,
			SecurityAsset,
			SecurityAssetID,
			Context,
			IsVisible,
			--Overriden,
			ApplyToType,
			OverrideItemID
	from	cache.AssetResponsibility
	where	Overriden = 0
GO

alter procedure [dbo].[GetLineage]
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
		assetId int,
		[key] varchar(250), 
		obj varchar(50), [objid] int, [type] varchar(50), typeName nvarchar(250), name nvarchar(500), shortname nvarchar(500),
		back varchar(7), fore varchar(7), template varchar(50), other varchar(500),

		HasSourceRules bit
		)
	declare @objects table (Type varchar(50), ID int)
	declare @currentDepth int = 0;
	declare @maxDepth int = 15;
	declare @maxItems int = 500;
	declare @itemCount int = 0;
	
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

		IF OBJECT_ID('tempdb..#points') IS NOT NULL DROP TABLE #points;
		create table #points ( ID int, SourceIntersectID int, TargetIntersectID int, Depth int );
		declare @forwardPoints table ( ID int, SourceIntersectID int, TargetIntersectID int );

		-- get all items directly tied to the focal object.
		insert into #points
			select	top (@maxItems)
				MI.ID, MI.SourceIntersectID, MI.TargetIntersectID, 0
			from	MapItem MI
					inner join [Intersect] SI on SI.ID = MI.SourceIntersectID
					inner join [Intersect] TI ON TI.ID = MI.TargetIntersectID
					inner join @objects O on	( (SI.Subject = O.Type and SI.SubjectID = O.ID) OR (SI.Object = O.Type and SI.ObjectID = O.ID)  ) OR 
												( (TI.Subject = O.Type and TI.SubjectID = O.ID) OR (TI.Object = O.Type and TI.ObjectID = O.ID)  )
			where not exists (select 1 from @rows R where R.Deleting = 1 and R.ID = MI.ID)

			set @maxItems = @maxItems - (select count(*) from #points);

		-- get all items not directly tied to the focal object, but still tied to maps involved above.
		if (@maxItems > 0)
		begin
			insert into #points
				select	top (@maxItems)
					MI.ID, MI.SourceIntersectID, MI.TargetIntersectID, 0
				from	MapItem MI
						inner join	(
									select	ID.MapItemID
									from	MapItemMap DM
											inner join #points D on D.ID = DM.MapItemID
											inner join MapItemMap ID on ID.MapID = DM.MapID and ID.MapItemID not in (
																													select ID from #points
																													)
									) O on O.MapItemID = MI.ID
				where not exists (select 1 from @rows R where R.Deleting = 1 and R.ID = MI.ID);

				set @maxItems = @maxItems - (select count(*) from #points);
		end

		insert into @forwardPoints
			select ID,SourceIntersectID,TargetIntersectID from #points

			--join editor rows to any existing intersects
			if exists(select 1 from @rows)
			begin
				insert into #points
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
					where	R.Adding = 1 and not exists (select 1 from #points P where P.SourceIntersectID = D1.ID and P.TargetIntersectID = D2.ID)
			end;

		set @currentDepth = 0;

		while( exists(select 1 from #points ps where ps.depth = @currentDepth) and (@currentDepth < @maxDepth) and (@maxItems > 0))
		begin

			set @itemCount = (select count(*) from #points);

			insert into #points
				select	top (@maxItems) 
				    S.ID,
					S.SourceIntersectID,
					S.TargetIntersectID,
					@currentDepth+1
				from	MapItem S
						inner join #points T on T.TargetIntersectID = S.SourceIntersectID and S.ID <> T.ID and T.Depth = @currentDepth
				where	not exists (select 1 from @rows R where R.Deleting = 1 and R.ID = S.ID) and not exists (select ID from #points where ID = S.ID)

			set @maxItems = @maxItems - ((select count(*) from #points) - @itemCount);
			set @itemCount = (select count(*) from #points);

			if (@maxItems > 0)
			begin
				

				insert into #points
					select	top (@maxItems)
						S.ID,
						S.SourceIntersectID,
						S.TargetIntersectID,
						@currentDepth+1
					from	MapItem S
							inner join #points T on T.SourceIntersectID = S.TargetIntersectID and S.ID <> T.ID and T.Depth = @currentDepth
					where	not exists (select 1 from @rows R where R.Deleting = 1 and R.ID = S.ID)
						and not exists (select ID from #points where ID = S.ID)
				set @maxItems = @maxItems - ((select count(*) from #points) - @itemCount);
			end

			set @currentDepth = @currentDepth + 1;
		end
				
		IF @view in (0,2)
		BEGIN

			IF OBJECT_ID('tempdb..#items') IS NOT NULL DROP TABLE #items;
			create table #items (
				ID int,
				SourceIntersectID int, 
				SourceSubjectTypeName nvarchar(500), SourceSubjectName nvarchar(500), SourceSubjectShortName nvarchar(500), SourceSubject varchar(50), SourceSubjectID int, SourceSubjectIconBackColor varchar(7), SourceSubjectIconForeColor varchar(7), 
				SourceObjectTypeName nvarchar(500), SourceObjectName nvarchar(500), SourceObjectShortName nvarchar(500), SourceObject varchar(50), SourceObjectID int, SourceObjectIconBackColor varchar(7), SourceObjectIconForeColor varchar(7),
			
				TargetIntersectID int, 
				TargetSubjectTypeName nvarchar(500), TargetSubjectName nvarchar(500), TargetSubjectShortName nvarchar(500), TargetSubject varchar(50), TargetSubjectID int, TargetSubjectIconBackColor varchar(7), TargetSubjectIconForeColor varchar(7), 
				TargetObjectTypeName nvarchar(500), TargetObjectName nvarchar(500), TargetObjectShortName nvarchar(500), TargetObject varchar(50), TargetObjectID int, TargetObjectIconBackColor varchar(7), TargetObjectIconForeColor varchar(7),

				SourceHasSourceRules bit, TargetHasSourceRules bit
			);

			CREATE CLUSTERED INDEX IX_TempItems ON #items (id, sourceintersectid, targetintersectid); --vastly improves performance

			insert into #items
				select	O.ID,				
						O.SourceIntersectID,
						SS.TypeName as SubjectTypeName,
						SSD.DisplayValue as SubjectName,
						SSD.DisplayValue as SubjectShortName,
						SI.[Subject],
						SI.SubjectID,
						SS.BackColor as SubjectIconBackColor,
						SS.ForeColor as SubjectIconForeColor,
						SO.TypeName as ObjectTypeName,
						SOD.DisplayValue as ObjectName,
						SOD.DisplayValue as ObjectShortName,
						SI.[Object],
						SI.ObjectID,
						SO.BackColor as ObjectIconBackColor,
						SO.ForeColor as ObjectIconForeColor,
						O.TargetIntersectID,
						TS.TypeName as SubjectTypeName,
						TSD.DisplayValue as SubjectName,
						TSD.DisplayValue as SubjectShortName,
						TI.Subject,
						TI.SubjectID,
						TS.BackColor,
						TS.ForeColor,
						TB.TypeName as ObjectTypeName,
						TBD.DisplayValue as ObjectName,
						TBD.DisplayValue as ObjectShortName,
						TI.Object,
						TI.ObjectID,
						TB.BackColor,
						TB.ForeColor,
						case 
							when SHSR.C > 0 then cast(1 as bit)
							else cast(0 as bit)
						end as SourceHasSourceRules,
											case 
							when THSR.C > 0 then cast(1 as bit)
							else cast(0 as bit)
						end as TargetHasSourceRules
				from	#points O
					inner join [Intersect] SI on SI.ID = O.SourceIntersectID
					inner join [Intersect] TI on TI.ID = O.TargetIntersectID
					inner join AssetWithType SS on SS.[Object] = SI.[Subject] and SS.ObjectID = SI.SubjectID 
					inner join AssetWithType SO on SO.[Object] = SI.[Object] and SO.ObjectID = SI.ObjectID
					inner join AssetWithType TS on TS.[Object] = TI.[Subject] and TS.ObjectID = TI.SubjectID
					inner join AssetWithType TB on TB.[Object] = TI.[Object] and TB.ObjectID = TI.ObjectID
					cross apply dbo.GetAssetDisplayValueById(SS.ID) SSD
					cross apply dbo.GetAssetDisplayValueById(SO.ID) SOD
					cross apply dbo.GetAssetDisplayValueById(TS.ID) TSD
					cross apply dbo.GetAssetDisplayValueById(TB.ID) TBD
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
				from #items I
				inner join @rows R on
					R.SourceSubjectID = I.SourceSubjectID 
					AND R.SourceObjectID = I.SourceObjectID
					AND R.TargetSubjectID = I.TargetSubjectID
					AND R.TargetObjectID = I.TargetObjectID;

				--insert adding items and fill in missing data
				insert into #items
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
				where R.Adding = 1
				and not exists (select 1 from #items i where i.SourceIntersectID = r.SourceIntersectID and i.TargetIntersectID = r.TargetIntersectID);
			end
		
		end -- end view 0,2

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
					--I.*,
					I.ID
					,I.SourceIntersectID
					,I.SourceSubjectTypeName
					,coalesce(SST.TextPath,I.SourceSubjectName) as SourceSubjectName
					,I.SourceSubjectShortName
					,I.SourceSubject
					,I.SourceSubjectID
					,I.SourceSubjectIconBackColor
					,I.SourceSubjectIconForeColor
					,I.SourceObjectTypeName
					,coalesce(SOT.TextPath,I.SourceObjectName) as SourceObjectName
					,I.SourceObjectShortName
					,I.SourceObject
					,I.SourceObjectID
					,I.SourceObjectIconBackColor
					,I.SourceObjectIconForeColor
					,I.TargetIntersectID
					,I.TargetSubjectTypeName
					,coalesce(TST.TextPath, I.TargetSubjectName) as TargetSubjectName
					,I.TargetSubjectShortName
					,I.TargetSubject
					,I.TargetSubjectID
					,I.TargetSubjectIconBackColor
					,I.TargetSubjectIconForeColor
					,I.TargetObjectTypeName
					,coalesce(OTT.TextPath, I.TargetObjectName) as TargetObjectName
					,I.TargetObjectShortName
					,I.TargetObject
					,I.TargetObjectID
					,I.TargetObjectIconBackColor
					,I.TargetObjectIconForeColor
					,I.SourceHasSourceRules 
					,I.TargetHasSourceRules,
					SI.IntersectTypeID as SourceIntersectTypeID,
					utility.DeriveIntersectTypeName(SIT.ID) as SourceIntersectTypeName,
					TI.IntersectTypeID as TargetIntersectTypeID,
					utility.DeriveIntersectTypeName(TIT.ID) as TargetIntersectTypeName
				from #items I
				inner join [Intersect] SI on SI.ID = I.SourceIntersectID
				left join Asset SS on SS.Object = SI.Subject and SS.ObjectID = SI.SubjectID
				outer apply dbo.GetAssetTextPathById(SS.ID, '/') SST
				left join Asset SO on SO.Object = SI.Object and SO.ObjectID = SI.ObjectID
				outer apply dbo.GetAssetTextPathById(SO.ID, '/') SOT
				inner join IntersectType SIT on SIT.ID = SI.IntersectTypeID
				inner join [Intersect] TI on TI.ID = I.TargetIntersectID
				left join Asset TS on TS.Object = TI.Subject and TS.ObjectID = TI.SubjectID
				outer apply dbo.GetAssetTextPathById(TS.ID, '/') TST
				left join Asset OT on OT.Object = TI.Object and OT.ObjectID = TI.ObjectID
				outer apply dbo.GetAssetTextPathById(OT.ID, '/') OTT
				inner join IntersectType TIT on TIT.ID = TI.IntersectTypeID
				for json path
			) as 'items'
			for json path, WITHOUT_ARRAY_WRAPPER
		end

		if @view = 1
		begin

		IF OBJECT_ID('tempdb..#systemItems') IS NOT NULL DROP TABLE #systemItems;
		create table #systemItems (
			ID int,
			SourceSubjectTypeName nvarchar(500), SourceSubjectName nvarchar(500), SourceSubjectShortName nvarchar(500), SourceSubject varchar(50), SourceSubjectID int, SourceSubjectIconBackColor varchar(7), SourceSubjectIconForeColor varchar(7), 
			TargetSubjectTypeName nvarchar(500), TargetSubjectName nvarchar(500), TargetSubjectShortName nvarchar(500), TargetSubject varchar(50), TargetSubjectID int, TargetSubjectIconBackColor varchar(7), TargetSubjectIconForeColor varchar(7), 
			SourceHasSourceRules bit, TargetHasSourceRules bit
		);

		CREATE CLUSTERED INDEX IX_TempSystemItems ON #systemItems (id, sourcesubject, sourcesubjectid, targetsubject, targetsubjectid); --vastly improves performance

		insert into #systemItems (ID, SourceSubjectTypeName, SourceSubjectName, SourceSubjectShortName, SourceSubject, SourceSubjectID, SourceSubjectIconBackColor,SourceSubjectIconForeColor,
		TargetSubjectTypeName, TargetSubjectName, TargetSubjectShortName,  TargetSubject, TargetSubjectID, TargetSubjectIconBackColor, TargetSubjectIconForeColor, 
		SourceHasSourceRules, TargetHasSourceRules)
			select	
					O.ID as ID,				
					SS.TypeName as SourceSubjectTypeName,
					SSD.DisplayValue as SourceSubjectName,
					SSD.DisplayValue as SourceSubjectShortName,
					SI.[Subject] as SourceSubject,
					SI.SubjectID as SourceSubjectID,
					SS.BackColor as SourceSubjectIconBackColor,
					SS.ForeColor as SourceSubjectIconForeColor,
					TS.TypeName as TargetSubjectTypeName,
					TSD.DisplayValue as TargetSubjectName,
					TSD.DisplayValue as TargetSubjectShortName,
					TI.[Subject] as TargetSubject,
					TI.SubjectID as TargetSubjectID,
					TS.BackColor as TargetSubjectIconBackColor,
					TS.ForeColor as TargetSubjectIconForeColor,
					case 
						when SHSR.C > 0 then cast(1 as bit)
						else cast(0 as bit)
					end as SourceHasSourceRules,
										case 
						when THSR.C > 0 then cast(1 as bit)
						else cast(0 as bit)
					end as TargetHasSourceRules
			from	#points O
				inner join [Intersect] SI on SI.ID = O.SourceIntersectID
				inner join [Intersect] TI on TI.ID = O.TargetIntersectID
				inner join AssetWithType SS on SS.[Object] = SI.[Subject] and SS.ObjectID = SI.SubjectID 
				inner join AssetWithType TS on TS.[Object] = TI.[Subject] and TS.ObjectID = TI.SubjectID
				cross apply dbo.GetAssetDisplayValueById(SS.ID) SSD
				cross apply dbo.GetAssetDisplayValueById(TS.ID) TSD
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

			insert into @links
					select	distinct
							S.SourceSubject + '.' + cast(S.SourceSubjectID as varchar) as [from],
							S.TargetSubject + '.' + cast(S.TargetSubjectID as varchar) as [to],
							'' as category
					from	#systemItems S
			insert into @nodes
					select	distinct
							A.ID as assetId,
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
					from	#systemItems I
					left join Asset A on A.[Object] = I.SourceSubject and A.ObjectID = I.SourceSubjectID;;

					--perform this update separately to avoid duplicate in the above query
					update n
					set n.HasSourceRules = 1
					from @nodes n
					inner join #systemItems i on n.[key] = (i.SourceSubject + '.' + cast(i.SourceSubjectID as varchar)) and i.SourceHasSourceRules = 1;

			--insert into @nodes
			merge	@nodes as T
			using	(
					select	distinct
							A.ID as assetId,
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
					from	#systemItems I
					left join Asset A on A.[Object] = I.TargetSubject and A.ObjectID = I.TargetSubjectID
					where	I.TargetSubject + '.' + cast(I.TargetSubjectID as varchar) not in (select [key] from @nodes)
					) S
			on		(T.[key] = S.[key])
			when matched then
			update	set
					T.HasSourceRules = S.HasSourceRules
			when	not matched then
			insert	(assetId, [key], obj, [objid], [type], typeName, name, shortname, back, fore, template, other, HasSourceRules)
			values	(S.assetId, S.[key], S.obj, S.[objid], S.[type], S.typeName, S.name, S.shortname, S.back, S.fore, S.template, S.other, S.HasSourceRules);
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
				from	#items
				union
				select	distinct
						cast(SourceIntersectID as varchar) + '.S' as 'from',
						TargetSubject + '.' + cast(TargetSubjectID as varchar) as 'to',
						'' as category
				from	#items O
				where	(SourceObject + cast(SourceObjectID as varchar)) = (TargetObject + cast(TargetObjectID as varchar))
				union
				select	distinct
						cast(SourceIntersectID as varchar) + '.S' as 'from',
						cast(TargetIntersectID as varchar) + '.T' as 'to',
						'' as category
				from	#items O
				where	(SourceObject + cast(SourceObjectID as varchar)) <> (TargetObject + cast(TargetObjectID as varchar))
				--where	TargetIntersectID in (select SourceIntersectID from #items)
				union
				select	distinct
						cast(TargetIntersectID as varchar) + '.T' as 'from',
						TargetSubject + '.' + cast(TargetSubjectID as varchar) as 'to',
						'Support' as category
				from	#items
				where	(SourceObject + cast(SourceObjectID as varchar)) <> (TargetObject + cast(TargetObjectID as varchar))

			insert into @nodes
				select	distinct
						A.ID as assetId,
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
				from	#items 
				left join Asset A on A.[Object] = SourceSubject and A.ObjectID = SourceSubjectID

			insert into @nodes
				select	distinct
						A.ID as assetId,
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
				from	#items
				left join Asset A on A.[Object] = SourceObject and A.ObjectID = SourceObjectID

				update n
				set n.HasSourceRules = 1
				from @nodes n
				inner join #items i on n.[key] = (i.SourceSubject + '.' + cast(i.SourceSubjectID as varchar)) and i.SourceHasSourceRules = 1;


			merge	@nodes as T
			using	(
					select	distinct
							A.ID as assetId,
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
					from	#items
					left join Asset A on A.[Object] = TargetObject and A.ObjectID = TargetObjectID
					where	(SourceObject + cast(SourceObjectID as varchar)) <> (TargetObject + cast(TargetObjectID as varchar))
					) S
			on		(T.[key] = S.[key])
			when	matched then
			update	set
					T.HasSourceRules = S.HasSourceRules
			when	not matched then
			insert	(assetId, [key], obj, [objid], [type], typeName, name, shortname, back, fore, template, other, HasSourceRules)
			values	(S.assetId, S.[key], S.obj, S.[objid], S.[type], S.typeName, S.name, S.shortname, S.back, S.fore, S.template, S.other, S.HasSourceRules);

			merge	@nodes as T
			using	(
					select	distinct
							A.ID as assetId,
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
					from	#items
					left join Asset A on A.[Object] = TargetSubject and A.ObjectID = TargetSubjectID
					where	TargetSubject + '.' + cast(TargetSubjectID as varchar) not in (select [key] from @nodes)
					) S
			on		(T.[key] = S.[key])
			when	matched then
			update	set
					T.HasSourceRules = S.HasSourceRules
			when	not matched then
			insert	(assetId, [key], obj, [objid], [type], typeName, name, shortname, back, fore, template, other, HasSourceRules)
			values	(S.assetId, S.[key], S.obj, S.[objid], S.[type], S.typeName, S.name, S.shortname, S.back, S.fore, S.template, S.other, S.HasSourceRules);

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

--select	* from	#items
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
					select  top (@maxItems) 
							I.ID,
							NULL,
							I.SourceFusionAttributeID,
							I.TargetFusionAttributeID, 
							0,
							'A'
					from	MapRuleItem I
							inner join FusionAttribute SFA on SFA.ID = I.SourceFusionAttributeID and SFA.Deleted = 0
							inner join FusionAttribute TFA on TFA.ID = I.TargetFusionAttributeID and TFA.Deleted = 0
					where	I.SourceFusionAttributeID = @id --or I.TargetFusionAttributeID = @id;

				set @maxItems = @maxItems - (select count(*) from #tFusionPoints);

				if (@maxItems > 0)
					begin
						insert into #tFusionPoints
						select	top (@maxItems)
							    I.ID,
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

						set @maxItems = @maxItems - (select count(*) from #tFusionPoints);
					end


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
				set @currentDepth = 0;

				while(exists(select 1 from #tFusionPoints ps where ps.depth = @currentDepth) and (@currentDepth < @maxDepth) and (@maxItems > 0))
				begin
					set @itemCount = (select count(*) from #tFusionPoints)

					insert into #tFusionPoints
						select distinct	top (@maxItems)
								S.ID,
								NULL,
								S.SourceFusionAttributeID,
								S.TargetFusionAttributeID,
								@currentDepth+1,
								'F'
						from	MapRuleItem S
								inner join #tFusionPoints FP on FP.SourceFusionAttributeID = S.TargetFusionAttributeID and FP.Depth = @currentDepth and FP.Direction in('A','F')
						where not exists (select ID from #tFusionPoints where ID = S.ID)
							and not exists (select 1 from @technicalRows R where Deleting = 1 and R.ID = S.ID);

						set @maxItems = @maxItems - ((select count(*) from #tFusionPoints) - @itemCount);
						set @itemCount = (select count(*) from #tFusionPoints);

						if @maxItems > 0
						begin
							insert into #tFusionPoints
							select distinct top (@maxItems)	
									S.ID,
									NULL,
									S.SourceFusionAttributeID,
									S.TargetFusionAttributeID,
									@currentDepth+1,
									'B'
							from	MapRuleItem S
									inner join #tFusionPoints FP on FP.TargetFusionAttributeID = S.SourceFusionAttributeID and FP.Depth = @currentDepth and FP.Direction in('A','B')
							where not exists (select ID from #tFusionPoints where ID = S.ID) 
									and not exists (select 1 from @technicalRows R where Deleting = 1 and R.ID = S.ID);

							set @maxItems = @maxItems - ((select count(*) from #tFusionPoints) - @itemCount);
							set @itemCount = (select count(*) from #tFusionPoints);
						end


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
				select
							MI.ID,

							MI.SourceIntersectID,
							SIS.TypeName as SourceSubjectTypeName,
							FSIS.TextPath as SourceSubjectName,
							SIS.DisplayValue as SourceSubjectShortName,
							SIS.Object as SourceSubject,
							SIS.ObjectID as SourceSubjectID,
							SIO.TypeName as SourceObjectTypeName,
							FSIO.TextPath as SourceObjectName,
							SIO.DisplayValue as SourceObjectShortName,
							SIO.Object as SourceObject,
							SIO.ObjectID as SourceObjectID,

							MI.TargetIntersectID,
							TIS.TypeName as TargetSubjectTypeName,
							FTIS.TextPath as TargetSubjectName,
							TIS.DisplayValue as TargetSubjectShortName,
							TIS.Object as TargetSubject,
							TIS.ObjectID as TargetSubjectID,
							TIO.TypeName as TargetObjectTypeName,
							FTIO.TextPath as TargetObjectName,
							TIO.DisplayValue as TargetObjectShortName,
							TIO.Object as TargetObject,
							TIO.ObjectID as TargetObjectID
					from	#tFusionPoints F
					inner join MapRuleItemMapItem J on J.MapRuleItemID = F.ID
					inner join MapItem MI on MI.ID = J.MapItemID
					inner join [Intersect] SI on SI.ID = MI.SourceIntersectID
					inner join [Intersect] TI on TI.ID = MI.TargetIntersectID
					inner join AssetDetail SIS on SIS.Object = SI.Subject and SIS.ObjectID = SI.SubjectID
					inner join AssetDetail SIO on SIO.Object = SI.Object and SIO.ObjectID = SI.ObjectID
					inner join AssetDetail TIS on TIS.Object = TI.Subject and TIS.ObjectID = TI.SubjectID
					inner join AssetDetail TIO on TIO.Object = TI.Object and TIO.ObjectID = TI.ObjectID
					inner join FusionAttribute FSIS on SIS.Object = 'FusionAttribute' and FSIS.ID = SIS.ObjectID
					inner join FusionAttribute FSIO on SIO.Object = 'FusionAttribute' and FSIO.ID = SIO.ObjectID
					inner join FusionAttribute FTIS on TIS.Object = 'FusionAttribute' and FTIS.ID = TIS.ObjectID
					inner join FusionAttribute FTIO on TIO.Object = 'FusionAttribute' and FTIO.ID = TIO.ObjectID;

				-- get all items not directly tied to the focal object, but still tied to maps involved above.
				insert into @tItems
					select	
							MI.ID,

							MI.SourceIntersectID,
							SIS.TypeName as SourceSubjectTypeName,
							FSIS.TextPath as SourceSubjectName,
							SIS.DisplayValue as SourceSubjectShortName,
							SIS.Object as SourceSubject,
							SIS.ObjectID as SourceSubjectID,
							SIO.TypeName as SourceObjectTypeName,
							FSIO.TextPath as SourceObjectName,
							SIO.DisplayValue as SourceObjectShortName,
							SIO.Object as SourceObject,
							SIO.ObjectID as SourceObjectID,

							MI.TargetIntersectID,
							TIS.TypeName as TargetSubjectTypeName,
							FTIS.TextPath as TargetSubjectName,
							TIS.DisplayValue as TargetSubjectShortName,
							TIS.Object as TargetSubject,
							TIS.ObjectID as TargetSubjectID,
							TIO.TypeName as TargetObjectTypeName,
							FTIO.TextPath as TargetObjectName,
							TIO.DisplayValue as TargetObjectShortName,
							TIO.Object as TargetObject,
							TIO.ObjectID as TargetObjectID
					from	MapItem MI
							inner join	(
										select	ID.MapItemID
										from	MapItemMap DM
												inner join @tItems D on D.MapItemID = DM.MapItemID
												inner join MapItemMap ID on ID.MapID = DM.MapID and ID.MapItemID not in (
																														select MapItemID from @tItems
																														)
										) O on O.MapItemID = MI.ID
					inner join [Intersect] SI on SI.ID = MI.SourceIntersectID
					inner join [Intersect] TI on TI.ID = MI.TargetIntersectID
					inner join AssetDetail SIS on SIS.Object = SI.Subject and SIS.ObjectID = SI.SubjectID
					inner join AssetDetail SIO on SIO.Object = SI.Object and SIO.ObjectID = SI.ObjectID
					inner join AssetDetail TIS on TIS.Object = TI.Subject and TIS.ObjectID = TI.SubjectID
					inner join AssetDetail TIO on TIO.Object = TI.Object and TIO.ObjectID = TI.ObjectID
					inner join FusionAttribute FSIS on SIS.Object = 'FusionAttribute' and FSIS.ID = SIS.ObjectID
					inner join FusionAttribute FSIO on SIO.Object = 'FusionAttribute' and FSIO.ID = SIO.ObjectID
					inner join FusionAttribute FTIS on TIS.Object = 'FusionAttribute' and FTIS.ID = TIS.ObjectID
					inner join FusionAttribute FTIO on TIO.Object = 'FusionAttribute' and FTIO.ID = TIO.ObjectID;

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

							O.SourceIntersectID,
							SIS.TypeName as SourceSubjectTypeName,
							SIS.DisplayValue as SourceSubjectName,
							SIS.DisplayValue as SourceSubjectShortName,
							SIS.Object as SourceSubject,
							SIS.ObjectID as SourceSubjectID,
							SIO.TypeName as SourceObjectTypeName,
							SIO.DisplayValue as SourceObjectName,
							SIO.DisplayValue as SourceObjectShortName,
							SIO.Object as SourceObject,
							SIO.ObjectID as SourceObjectID,

							O.TargetIntersectID,
							TIS.TypeName as TargetSubjectTypeName,
							TIS.DisplayValue as TargetSubjectName,
							TIS.DisplayValue as TargetSubjectShortName,
							TIS.Object as TargetSubject,
							TIS.ObjectID as TargetSubjectID,
							TIO.TypeName as TargetObjectTypeName,
							TIO.DisplayValue as TargetObjectName,
							TIO.DisplayValue as TargetObjectShortName,
							TIO.Object as TargetObject,
							TIO.ObjectID as TargetObjectID
					from	@tBusinessPoints O
					inner join [Intersect] SI on SI.ID = O.SourceIntersectID
					inner join [Intersect] TI on TI.ID = O.TargetIntersectID
					inner join AssetDetail SIS on SIS.Object = SI.Subject and SIS.ObjectID = SI.SubjectID
					inner join AssetDetail SIO on SIO.Object = SI.Object and SIO.ObjectID = SI.ObjectID
					inner join AssetDetail TIS on TIS.Object = TI.Subject and TIS.ObjectID = TI.SubjectID
					inner join AssetDetail TIO on TIO.Object = TI.Object and TIO.ObjectID = TI.ObjectID


				insert into #tFusionPoints
					select	top (@maxItems) 
							J.MapRuleItemID,
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

				set @maxItems = @maxItems - (select count(*) from #tFusionPoints);
				
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
				
				while( exists(select 1 from #tFusionPoints ps where ps.depth = @currentDepth) and (@currentDepth < @maxDepth) and (@maxItems > 0))
				begin	
					set @itemCount = (select count(*) from #tFusionPoints);

					insert into #tFusionPoints
						select distinct top (@maxItems)	
							    S.ID,
								NULL,
								S.SourceFusionAttributeID,
								S.TargetFusionAttributeID,
								@currentDepth+1,
								'F'
						from	MapRuleItem S
								inner join #tFusionPoints FP on FP.SourceFusionAttributeID = S.TargetFusionAttributeID and FP.Depth = @currentDepth and FP.Direction in('A','F')
						where not exists (select ID from #tFusionPoints where ID = S.ID)
							and not exists (select 1 from @technicalRows R where Deleting = 1 and R.ID = S.ID);

					set @maxItems = @maxItems - ((select count(*) from #tFusionPoints) - @itemCount);
					set @itemCount = (select count(*) from #tFusionPoints);

					if (@maxItems > 0)
					begin
						insert into #tFusionPoints
							select distinct	top (@maxItems) 
							        S.ID,
									NULL,
									S.SourceFusionAttributeID,
									S.TargetFusionAttributeID,
									@currentDepth+1,
									'B'
							from	MapRuleItem S
									inner join #tFusionPoints FP on FP.TargetFusionAttributeID = S.SourceFusionAttributeID and FP.Depth = @currentDepth and FP.Direction in('A','B')
							where not exists (select ID from #tFusionPoints where ID = S.ID) 
									and not exists (select 1 from @technicalRows R where Deleting = 1 and R.ID = S.ID);

					set @maxItems = @maxItems - ((select count(*) from #tFusionPoints) - @itemCount);
					end


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
					A.ID as assetId,
					cast(S.SourceFusionAttributeID as varchar),-- + '.' + coalesce(B.SourceSubject, '0') + '.' + coalesce(cast(B.SourceSubjectID as varchar), '0') as [key],
					'FusionAttribute' as [obj],
					SourceFusionAttributeID as [objid], 
					'FusionAttribute' as [type],
					T.Name as typeName,
					FA.TextPath as name,
					FA.Name as shortname,
					COALESCE(ST.IconBackColor, '#000') as back,
					COALESCE(ST.IconForeColor, '#fff') as fore,
					'Fusion' as template,
					B.SourceSubjectTypeName + ' : ' + B.SourceSubjectName as other,
					null
			from	#tFusionPoints S
					inner join FusionAttribute FA on FA.ID = S.SourceFusionAttributeID
					inner join FusionAttributeType T on T.ID = FA.FusionAttributeTypeID
					left join ObjectStyle ST on ST.ObjectType = 'FusionAttributeType' and ST.ObjectID = T.ID
					left join MapRuleItemMapItem J on J.MapRuleItemID = S.ID
					left join @tItems B on B.MapItemID = J.MapItemID
					left join Asset A on A.[Object] = 'FusionAttribute' and A.ObjectID = SourceFusionAttributeID
		insert into @nodes
			select	distinct
					A.ID as assetId,
					cast(S.TargetFusionAttributeID as varchar),-- + '.' + coalesce(B.TargetSubject, '0') + '.' + coalesce(cast(B.TargetSubjectID as varchar), '0') as [key],
					'FusionAttribute' as [obj],
					TargetFusionAttributeID as [objid], 
					'FusionAttribute' as [type],
					T.Name as typeName,
					FA.TextPath as name,
					FA.Name as shortname,
					COALESCE(ST.IconBackColor, '#000') as back,
					COALESCE(ST.IconForeColor, '#fff') as fore,
					'Fusion' as template,
					B.TargetSubjectTypeName + ' : ' + B.TargetSubjectName as other,
					null
			from	#tFusionPoints S
					inner join FusionAttribute FA on FA.ID = S.TargetFusionAttributeID
					inner join FusionAttributeType T on T.ID = FA.FusionAttributeTypeID
					left join ObjectStyle ST on ST.ObjectType = 'FusionAttributeType' and ST.ObjectID = T.ID
					left join MapRuleItemMapItem J on J.MapRuleItemID = S.ID
					left join @tItems B on B.MapItemID = J.MapItemID
					left join Asset A on A.[Object] = 'FusionAttribute' and A.ObjectID = TargetFusionAttributeID
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
GO


ALTER TABLE [dbo].[Intersect] DROP CONSTRAINT [UQ_Intersect]
GO

DROP INDEX [IX_Intersect_Subject_Object_Include] ON [dbo].[Intersect]
GO

DROP INDEX [IX_Intersect_Object_Type_Subject_Include] ON [dbo].[Intersect]
GO

DROP INDEX [CIX_Intersect] ON [dbo].[Intersect]
GO

DROP VIEW [utility].[ArtifactAssetParent]
GO

DROP VIEW [utility].[ArtifactAssetParentIntermediate]
GO


DROP VIEW [utility].[IntersectAsset]
GO

ALTER TABLE [dbo].[Intersect] alter column Subject varchar(50) not null
ALTER TABLE [dbo].[Intersect] alter column SubjectID int not null
ALTER TABLE [dbo].[Intersect] alter column Object varchar(50) not null
ALTER TABLE [dbo].[Intersect] alter column ObjectID int not null



ALTER TABLE [dbo].[Intersect] ADD CONSTRAINT [UQ_Intersect] UNIQUE  
(
	[IntersectTypeID] ASC,
	[Subject] ASC,
	[SubjectID] ASC,
	[Object] ASC,
	[ObjectID] ASC
)
GO

CREATE CLUSTERED INDEX [CIX_Intersect] ON [dbo].[Intersect]
(
	[IntersectTypeID] ASC,
	[Subject] ASC,
	[SubjectID] ASC,
	[Object] ASC,
	[ObjectID] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF)
GO

--CREATE NONCLUSTERED INDEX [IX_Intersect_Object_Type_Subject_Include] ON [dbo].[Intersect]
--(
--	[Object] ASC,
--	[ObjectID] ASC,
--	[IntersectTypeID] ASC,
--	[SubjectID] ASC
--)
--INCLUDE ( 	[ID]) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF)
--GO


--CREATE NONCLUSTERED INDEX [IX_Intersect_Subject_Object_Include] ON [dbo].[Intersect]
--(
--	[Subject] ASC,
--	[Object] ASC,
--	[SubjectID] ASC
--)
--INCLUDE ( 	[ID]) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF)
--GO

create VIEW [utility].[IntersectAsset]
WITH SCHEMABINDING  
AS  
    select
	I.ID,
	I.ID as IntersectID,
	I.IntersectTypeID as IntersectTypeID,
	P.Type as PredicateType,
	a_o.ID as ObjectAssetID,
	I.[Object] as [Object],
	I.ObjectID as [ObjectID],	
	I.[Subject] as [Subject],
	I.SubjectID as [SubjectID]
from 
	dbo.[Intersect] I
	inner join dbo.Asset a_o on I.[Object] = a_o.[Object] and I.[ObjectID] = a_o.ObjectID	
	inner join dbo.[IntersectType] IT on I.IntersectTypeID = IT.ID
	inner join dbo.[Predicate] P on P.ID = IT.PredicateID
GO

SET ARITHABORT ON
SET CONCAT_NULL_YIELDS_NULL ON
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
SET ANSI_PADDING ON
SET ANSI_WARNINGS ON
SET NUMERIC_ROUNDABORT OFF
GO

CREATE UNIQUE CLUSTERED INDEX [IDEX_IntersectAsset_ObjectAsset_Predicate_IntersectType] ON [utility].[IntersectAsset]
(
	[ID] ASC,
	[ObjectAssetID] ASC,
	[PredicateType] ASC,
	[IntersectTypeID] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF)
GO


--Recreate with latest: [utility].[ArtifactAssetParentIntermediate]
--CREATE VIEW [utility].[ArtifactAssetParentIntermediate]
--WITH SCHEMABINDING  
--AS  
--    select	a_o.ID as AssetID,		
--			I.SubjectID as ParentArtifactID
--	from
--		dbo.[Intersect] I
--		inner join dbo.Asset a_o on I.[Object] = a_o.[Object] and I.[ObjectID] = a_o.ObjectID	
--		inner join dbo.[IntersectType] IT on I.IntersectTypeID = IT.ID
--		inner join dbo.[Predicate] P on P.ID = IT.PredicateID and P.[Type] = 3		
--	where I.[Object] = 'Artifact'
--GO

--Recreate with latest: [utility].[ArtifactAssetParent]
--create VIEW [utility].[ArtifactAssetParent]
--WITH SCHEMABINDING  
--AS  
--    select	
--		aim.AssetID,
--		aim.ParentArtifactID,
--		IA.ID as ParentAssetID
--	from [utility].[ArtifactAssetParentIntermediate] aim
--		inner join dbo.Asset IA on IA.Object = 'Artifact' and aim.ParentArtifactID = IA.ObjectID 	
--GO

--Responsibility Logic Re-work --------------------------------------
ALTER TABLE ResponsibilityTypeRelation DROP CONSTRAINT [DF_ResponsibilityTypeRelation_DeleteAttributes]
ALTER TABLE ResponsibilityTypeRelation DROP CONSTRAINT [DF_ResponsibilityTypeRelation_DeleteObject]
ALTER TABLE ResponsibilityTypeRelation DROP CONSTRAINT [DF_ResponsibilityTypeRelation_DeleteRelationships]
ALTER TABLE ResponsibilityTypeRelation DROP CONSTRAINT [DF_ResponsibilityTypeRelation_DeleteSocial]
ALTER TABLE ResponsibilityTypeRelation DROP CONSTRAINT [DF_ResponsibilityTypeRelation_ModifyAttributes]
ALTER TABLE ResponsibilityTypeRelation DROP CONSTRAINT [DF_ResponsibilityTypeRelation_ModifyObject]
ALTER TABLE ResponsibilityTypeRelation DROP CONSTRAINT [DF_ResponsibilityTypeRelation_ModifyRelationships]
ALTER TABLE ResponsibilityTypeRelation DROP CONSTRAINT [DF_ResponsibilityTypeRelation_ModifySocial]
ALTER TABLE ResponsibilityTypeRelation DROP CONSTRAINT [DF_ResponsibilityTypeRelation_ReadAttributes]
ALTER TABLE ResponsibilityTypeRelation DROP CONSTRAINT [DF_ResponsibilityTypeRelation_ReadAudit]
ALTER TABLE ResponsibilityTypeRelation DROP CONSTRAINT [DF_ResponsibilityTypeRelation_ReadDashboards]
ALTER TABLE ResponsibilityTypeRelation DROP CONSTRAINT [DF_ResponsibilityTypeRelation_ReadObject]
ALTER TABLE ResponsibilityTypeRelation DROP CONSTRAINT [DF_ResponsibilityTypeRelation_ReadRelationships]
ALTER TABLE ResponsibilityTypeRelation DROP CONSTRAINT [DF_ResponsibilityTypeRelation_ReadSocial]
GO
alter table ResponsibilityTypeRelation drop column [ReadObject]
alter table ResponsibilityTypeRelation drop column [ReadAttributes]
alter table ResponsibilityTypeRelation drop column [ReadAudit]
alter table ResponsibilityTypeRelation drop column [ReadDashboards]
alter table ResponsibilityTypeRelation drop column [ReadRelationships]
alter table ResponsibilityTypeRelation drop column [ReadSocial]
alter table ResponsibilityTypeRelation drop column [ModifyObject]
alter table ResponsibilityTypeRelation drop column [ModifyAttributes]
alter table ResponsibilityTypeRelation drop column [ModifyRelationships]
alter table ResponsibilityTypeRelation drop column [ModifySocial]
alter table ResponsibilityTypeRelation drop column [DeleteObject]
alter table ResponsibilityTypeRelation drop column [DeleteAttributes]
alter table ResponsibilityTypeRelation drop column [DeleteRelationships]
alter table ResponsibilityTypeRelation drop column [DeleteSocial]
GO
alter table ResponsibilityTypeRelation add PermissionsBitMask int constraint DF_ResponsibilityTypeRelation_PermissionsBitMask default(0) not null
GO

DROP TABLE [dbo].[ResponsibilityContextItem]
GO
DROP TABLE [dbo].[Responsibility]
GO
DROP TABLE [cache].[ResponsibilityItem]
GO
DROP PROCEDURE [dbo].[GetAllowedResponsibilityTypesByObject]
GO
DROP TABLE [dbo].[ResponsibilityTypeClaim]
GO
DROP VIEW [dbo].[DomainAllocationDetail]
GO
---------------------------------------------------------------------



-- add assetid column to queue task
alter table [queue].task add AssetID bigint not null default(0)
go

-- update asset trigger to add asset id
ALTER TRIGGER [dbo].[Asset_AfterDelete]
       ON [dbo].[Asset]
       AFTER DELETE
AS
       SET NOCOUNT ON;
       INSERT INTO [queue].[Task] ([Action], [Object], [ObjectID],[AssetID])
        select       'Delete', 
                           Object, 
                           ObjectID,
                           ID
              from   deleted;
go


ALTER TRIGGER [dbo].[Asset_AfterInsert]
   ON  [dbo].[Asset] 
   AFTER INSERT
AS 
       SET NOCOUNT ON;
       INSERT INTO [queue].[Task] ([Action], [Object], [ObjectID], [Custom], [AssetID])
        select       'Add', 
                           I.Object, 
                           I.ObjectID,
                           [queue].WriteIndexXml('', I.[Object], I.ObjectID, coalesce(I.CreatedBy, 0)),
                           I.ID
              from   inserted I where  I.Object not in('FusionAttribute');

GO



ALTER TRIGGER [dbo].[Asset_AfterUpdate]
   ON  [dbo].[Asset] 
   AFTER UPDATE
AS 
       SET NOCOUNT ON;
       INSERT INTO [queue].[Task] ([Action], [Object], [ObjectID], [Custom], [AssetID])
        select       'Update', 
                           I.Object, 
                           I.ObjectID,
                           [queue].WriteIndexXml('', I.[Object], I.ObjectID, coalesce(I.UpdatedBy, 0)),
                           I.ID
              from   inserted I where I.Object not in('FusionAttribute')
go


-- Migrate from legacy claims / claim objects
update	T
set		T.PermissionsBitMask = S.PermissionsBitMask
from	ResponsibilityTypeRelation T
		inner join (
					select	[ResponsibilityTypeID], [ObjectType], [ObjectID], sum(Permission) as PermissionsBitMask
					from	(
							select	distinct
									[ResponsibilityTypeID], [ObjectType], [ObjectID],
									case 
										when ClaimObject = 1 and Claim = 1 then 1
										when ClaimObject = 1 and Claim = 2 then 2
										when ClaimObject = 1 and Claim = 3 then 2
										when ClaimObject = 1 and Claim = 4 then 4
										when ClaimObject = 2 and Claim = 1 then 8
										when ClaimObject = 2 and Claim = 2 then 16
										when ClaimObject = 2 and Claim = 3 then 16
										when ClaimObject = 2 and Claim = 4 then 32
										when ClaimObject = 3 and Claim = 1 then 64
										when ClaimObject = 3 and Claim = 2 then 128
										when ClaimObject = 3 and Claim = 3 then 128
										when ClaimObject = 3 and Claim = 4 then 256
										when ClaimObject = 4 and Claim = 1 then 512
										when ClaimObject = 4 and Claim = 2 then 1024
										when ClaimObject = 4 and Claim = 3 then 1024
										when ClaimObject = 4 and Claim = 4 then 2048
									end
									as Permission
							from	ResponsibilityTypeObjectClaim
							) O
					group by [ResponsibilityTypeID], [ObjectType], [ObjectID]
					) as S on T.[ResponsibilityTypeID] = S.[ResponsibilityTypeID] and T.ObjectType = S.ObjectType and T.ObjectID = T.ObjectID

/*
ALTER TABLE ResponsibilityTypeRelationRuleResult SET (SYSTEM_VERSIONING = OFF);  
DROP TABLE [dbo].[ResponsibilityTypeRelationRuleResult_History]
DROP TABLE [dbo].[ResponsibilityTypeRelationRuleResult]
ALTER TABLE ResponsibilityTypeRelationRuleResult SET ( SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[ResponsibilityTypeRelationRuleResult_History]) );
*/

CREATE TABLE [dbo].[ResponsibilityTypeRelationRuleResult](
	--ID uniqueidentifier constraint DF_ResponsibilityTypeRelationRuleResult_ID default(newid()) not null,
	RuleID int, 
	ResponsibilityTypeID int, 
	AssetID bigint, 
	AssetTypeID int, 
	SecurityAsset char(1), 
	SecurityAssetID int, 
	Context nvarchar(max),
	ApplyToType bit, 
	PermissionsBitMask int, 
	IsVisible bit,
	Overridden bit, 
	OverrideID bigint,
	[EffectiveStartDate] [datetime2](0) GENERATED ALWAYS AS ROW START NOT NULL,
	[EffectiveEndDate] [datetime2](0) GENERATED ALWAYS AS ROW END NOT NULL,
	CONSTRAINT [PK_ResponsibilityTypeRelationRuleResult] PRIMARY KEY NONCLUSTERED (
		RuleID, ResponsibilityTypeID, AssetID, AssetTypeID, SecurityAsset, SecurityAssetID, ApplyToType, Overridden, OverrideID	
	 ), --ID  ),
	PERIOD FOR SYSTEM_TIME ([EffectiveStartDate], [EffectiveEndDate])
)
WITH ( SYSTEM_VERSIONING = ON ( HISTORY_TABLE = [dbo].[ResponsibilityTypeRelationRuleResult_History] ) )
GO

CREATE NONCLUSTERED INDEX [IX_ResponsibilityTypeRelationRuleResult] ON [dbo].[ResponsibilityTypeRelationRuleResult] ([AssetID],[Overridden])
GO

ALTER VIEW [dbo].[ResponsibilityDetail] AS
	select	O.RuleID,
			O.ResponsibilityTypeID,
			RT.Name as ResponsibilityTypeName,
			O.AssetID,
			O.AssetTypeID,
			coalesce(RD.ResourceID, RG.ResourceID, O.SecurityAssetID) as ResourceID,
			R.FirstName + ' ' + R.LastName as ResourceName,
			O.SecurityAsset,
			O.SecurityAssetID,
			coalesce(D.Name, G.Name, R.FirstName + ' ' + R.LastName) as SecurityAssetName,  
			O.Context,
			O.ApplyToType,
			O.PermissionsBitMask,
			O.IsVisible,
			O.OverrideID,
			A.Object,
			A.ObjectID,
			T.Object as [Type],
			T.ObjectID as TypeID
	from	ResponsibilityTypeRelationRuleResult O
			inner join ResponsibilityType RT on RT.ID = O.ResponsibilityTypeID
			inner join Asset A on A.ID = O.AssetID
			inner join AssetType T on T.ID = A.AssetTypeID
			left join [Group] G on O.SecurityAsset = 'G' and G.ID = O.SecurityAssetID
			left join ResourceGroup RG on RG.GroupID = G.ID
			left join [Organization] D on O.SecurityAsset = 'O' and D.ID = O.SecurityAssetID
			left join OrganizationResource RD on RD.OrganizationID = D.ID
			inner join reporting.Global_Resource R on R.ResourceID = coalesce(RD.ResourceID, RG.ResourceID, O.SecurityAssetID)
	where	O.Overridden = 0
GO

ALTER proc [dbo].[GetPageInformation]
--declare 
	@o varchar(50),-- = 'Artifact',
	@oid int,-- = 23450,
	@rid int --= 1
as
begin
	declare @breadcrumbsRaw table ([Level] int, [TypeName] nvarchar(500), [Name] nvarchar(max), [TypeUrl] nvarchar(2500), [Url] nvarchar(2500));
	declare @breadcrumbs table ([Name] nvarchar(max), [Url] nvarchar(2500), Active bit, IsType bit);

	with h as
		(
		select	A.ID,
				A.[ObjectID], 
				A.AssetTypeID,
				I.SubjectID as [ParentID], 
				0 as [Level]
		from	Asset A
				left join PredicateIntersect I on I.Object = A.Object and I.ObjectID = A.ObjectID and I.PredicateType = 3
		where	A.[Object] = @o and A.ObjectID = @oid
		union all
		select	P.ID,
				P.[ObjectID] as ID, 
				P.AssetTypeID,
				I.SubjectID as ParentID, 
				h.[Level]-1 as [Level]
		from	Asset P
				inner join h on P.[Object] = @o and P.ObjectID = h.ParentID
				outer apply (
							select	SubjectID
							from	PredicateIntersect 
							where	Object = P.Object 
									and ObjectID = P.ObjectID 
									and PredicateType = 3
							) I
		)

	insert into @breadcrumbsRaw
		select		distinct	
					[Level],
					ltrim(rtrim(T.Name)),
					ltrim(rtrim(D.DisplayValue)),
					UT.Url,
					U.Url
		from		h 
					inner join AssetType T on T.ID = h.AssetTypeID
					left join dbo.GetAssetDisplayValue() D on D.ID = h.ID
					cross apply dbo.GetAssetUrl(@o, T.ObjectID, h.ObjectID) U
					cross apply dbo.GetAssetUrl(T.Object, T.ObjectID, T.ObjectID) UT
		where		ltrim(rtrim(T.Name)) is not null
					and ltrim(rtrim(D.DisplayValue)) is not null
		order by	[Level]

	declare @max int = 0,
			@min int
	select	@min = min([Level]) from @breadcrumbsRaw

	insert into @breadcrumbs values ('Glossary', null, 0, 0)

	while @min <= @max
	begin
		insert into @breadcrumbs
			select	TypeName, TypeUrl, 0, 1 from @breadcrumbsRaw where [Level] = @min

		insert into @breadcrumbs
			select	Name, 
					Url, 
					case @min when 0 then 1 else 0 end, 
					0 
			from	@breadcrumbsRaw 
			where	[Level] = @min

		set @min = @min + 1
	end

	select	distinct
			A.ID,
			O.ID as AssetID,
			O.AssetTypeID,
			OD.DisplayValue,
			T.Name as [TypeName],
			case 
				when Dash.[Count] > 0 then cast(1 as bit)
				else cast(0 as bit)
			end as HasDashboards,
			case 
				when Work.[Count] > 0 then cast(1 as bit)
				else cast(0 as bit)
			end as HasWorkflow,
			case 
				when Child.[Count] > 0 then cast(1 as bit)
				else cast(0 as bit)
			end as HasChildArtifacts,
			case 
				when Attr.[Count] > 0 then cast(1 as bit)
				else cast(0 as bit)
			end as AllowAttributes,
			case 
				when Hier.[Count] > 0 then cast(1 as bit)
				else cast(0 as bit)
			end as AllowPredicateHierarchies,
			(
			select	*
			from	(
					select	P.ID as [ID],
							P.Name as [Name]
					from	[Predicate] P
					where	exists(SELECT * FROM IntersectType IT WHERE P.[type] = 6 and P.ID = IT.PredicateID and ((IT.Subject = T.Object and IT.SubjectID = T.ObjectID) OR (IT.Object = T.Object and IT.ObjectID =T.ObjectID)))
					union	
					select	P.ID as [ID], 
							P.Name as [Name] 
					from	[NymRelation] R 
							inner join [dbo].[predicate] P on P.ID = R.PredicateID where R.[Object] = T.Object and R.ObjectID = T.ObjectID
					) NMT
			for		json path
			)
			as NymTypes,
			(
			select	* 
			from	@breadcrumbs
			for		json path
			) as Breadcrumbs
	from	Artifact A 
			inner join Asset O on O.Object = @o and O.ObjectID = A.ID 
			inner join AssetType T on T.ID = O.AssetTypeID
			left join dbo.GetAssetDisplayValue() OD on OD.ID = O.ID
			--cross apply [dbo].GetAssetDisplayValueById(O.ID) as OD
			cross apply (
						select	count(1) as [Count]
						from	Report
						where	ObjectType = O.Object
								and ObjectID = T.ObjectID
						) Dash
			cross apply (
						select	count(1) as [Count]
						from	workflow.EventRegistration WER
								inner join workflow.Type WT on WER.TypeID = WT.ID and WT.PublishedVersionID is not null and WT.[State] = 1 and WER.ChangeType = 8 --ACTIVE
						where	WER.Object = T.Object
								and WER.ObjectID = T.ObjectID
						) Work
			cross apply (
						select	count(1) as [Count]
						from	[PredicateIntersect]
						where	Subject = O.Object
								and SubjectID = O.ObjectID
								and PredicateType = 3
						) Child
			cross apply (
						select	count(1) as [Count]
						from	AttributeTypeRelation
						where	ObjectType = T.Object and ObjectID = T.ObjectID
						) Attr
			cross apply (
						select	count(1) as [Count]
						from	IntersectType IT
								inner join [Predicate] P on P.ID = IT.PredicateID and P.[Type] = 3 -- TypeOf
						where	((IT.Subject = T.Object and IT.SubjectID = T.ObjectID) OR (IT.Object = T.Object and IT.ObjectID = T.ObjectID))
						) Hier
	where   A.ID = @oid 
			and A.[Visible] = 1 
			and A.ID not in (select AssetID from ResponsibilityDetail where ResourceID = @rid and PermissionsBitMask & 1 <> 1)
	for json path, WITHOUT_ARRAY_WRAPPER
end
GO

DROP TRIGGER [dbo].[ResponsibilityTypeRelationOverrideItem_AfterInsert]
GO

DROP TRIGGER [dbo].[ResponsibilityTypeRelationOverrideItem_AfterUpdate]
GO

CREATE TRIGGER [dbo].[ResponsibilityTypeRelationOverrideItem_AfterUpsert]
   ON  [dbo].[ResponsibilityTypeRelationOverrideItem]
   AFTER INSERT, UPDATE
AS 
BEGIN
	SET NOCOUNT ON;

	-- 1. Override rule assignments
	update	T
	set		T.Overridden = 1,
			T.OverrideID = S.ID
	from	ResponsibilityTypeRelationRuleResult T
			inner join inserted S on S.AssetID = T.AssetID and S.ResponsibilityTypeID = T.ResponsibilityTypeID and T.RuleID <> 0;

	-- 2. Load Override assignments
	merge	ResponsibilityTypeRelationRuleResult as T
	using	(
			select	0 as RuleID,
					I.ID,
					I.ResponsibilityTypeID,
					A.ID as AssetID,
					A.Object,
					A.ObjectID,
					A.AssetTypeID,
					T.Object as Type,
					T.ObjectID as TypeID,
					I.SecurityAsset,
					I.SecurityAssetID,
					R.PermissionsBitMask,
					I.Context
			from	Asset A
					inner join AssetType T on T.ID = A.AssetTypeID
					inner join ResponsibilityTypeRelation R on R.ObjectType = T.Object and R.ObjectID = T.ObjectID
					inner join inserted I on I.AssetID = A.ID and I.ResponsibilityTypeID = R.ResponsibilityTypeID
			) as S 
	on		(
			S.RuleID = T.RuleID
			and S.ID = T.OverrideID
			)
	when	matched then
	update	set
			T.SecurityAsset = S.SecurityAsset,
			T.SecurityAssetID = S.SecurityAssetID,
			T.PermissionsBitMask = S.PermissionsBitMask,
			T.Context = S.Context
	when	not matched by target then
			insert (RuleID, ResponsibilityTypeID, AssetID, AssetTypeID, SecurityAsset, SecurityAssetID, PermissionsBitMask, Context, ApplyToType, IsVisible, Overridden, OverrideID)
			values (S.RuleID, S.ResponsibilityTypeID, S.AssetID, S.AssetTypeID, S.SecurityAsset, S.SecurityAssetID, S.PermissionsBitMask, S.Context, 0, 1, 0, S.ID);
END
GO

ALTER TRIGGER [dbo].[ResponsibilityTypeRelationOverrideItem_AfterDelete]
   ON  [dbo].[ResponsibilityTypeRelationOverrideItem]
   AFTER DELETE
AS 
BEGIN
	SET NOCOUNT ON;

	delete	T
	from	ResponsibilityTypeRelationRuleResult T
			inner join deleted S on T.RuleID = 0 and T.OverrideID = S.ID;

	update	T
	set		T.Overridden = 0,
			T.OverrideID = 0
	from	ResponsibilityTypeRelationRuleResult T
			inner join deleted S on T.RuleID <> 0 and T.OverrideID = S.ID;
END
GO

drop table [cache].[Object]
GO


ALTER PROCEDURE [dbo].[GetRenderedTemplateBodyNg]
--declare
	@TemplateType varchar(25),
	@Type varchar(50),
	@ID int,
	@Action varchar(50),
	@SubjectName VARCHAR (200) = 'Governing Domain',
	@resourceId int = -1
--set @TemplateType = 'Lookup'
--set @Type = 'Artifact'
--set @ID = 7004--16435
--set @Action = 'Preview'--'Certificate'
AS
BEGIN
	SET NOCOUNT ON;

	declare @html nvarchar(max),
			@link nvarchar(2500),
			@icon nvarchar(250),
			@hasDynamicFields bit = 0,
			@hasStats bit = 0,
			@typeID int,

			@showIcon bit = 1,

			@current int,
			@max int,
			@name nvarchar(250),
			@value nvarchar(max);

	declare @tbl table (ID int identity, Name nvarchar(250), Value nvarchar(max));

	if @TemplateType = 'Tooltip'
	begin
		select	@html = TemplateBody
		from	TooltipTemplate
		where	Name = @Type
				and [Action] = @Action
	end

	-- Get the static tokens, depending on the type.
	declare @n nvarchar(250), @t nvarchar(250), @s nvarchar(25), @v int, @dc datetime, @du datetime, @d nvarchar(4000);
		
	-- Get common fields
	select	@typeID = C_D.ObjectTypeID,
			@icon = '<div title=''' + C_D.Name + ''' class=''tooltip-icon'' style=''background-color: ' + C_D.IconBackColor + '; color: ' + C_D.IconForeColor + '''><i class=''fa fa-' + C_D.IconText + '''></i></div>',
			@n = C_D.Name,
			@t = C_D.ObjectTypeName,
			@d = f.formattedvalue,
			@link = C_D.Url
	from	cache.objectdetails C_D			
			left join fieldtype ft on (ft.[object] = C_D.[objecttype] and ft.objectid = C_D.objecttypeid and ft.name = 'Description')
			left join field f on (f.fieldtypeid = ft.id and f.[objecttype] = C_D.[object] and f.objectid = C_D.objectid)
	where	C_D.[Object] = @Type
			and C_D.ObjectID = @ID;

	--fusion attributes arent in cache
	if @Type = 'FusionAttribute'
	begin		
		select 
			@typeID = fa.fusionattributetypeid,
			@n = fa.name,
			@t = fat.Name,
			@link = dbo.GenerateNgObjectUrl('FusionAttribute', fat.id, fa.id) 
		from fusionattribute fa 
			inner join fusionattributetype fat on (fa.fusionattributetypeid = fat.id) 
		where fa.id = @ID
	end

	if @n is not null
	begin
		if @link is null
		begin
			insert into @tbl values ('Name', @n)
		end
		else
		begin
			insert into @tbl values ('Name', '<a routerLink="/' + @link + '">' + @n + '</a>')
		end
		insert into @tbl values ('Description', @d)
	end
	insert into @tbl values ('Type', @t)

	if @Action = 'AssigningItemPreview'
	begin
		set @html = '<h3>{Name}</h3>'
	end

	
	if @Action = 'LookupPreview'
	begin
		set @html = '{Items}'
		
		if @Type = 'FusionAttribute'
		begin
			-- BUILD LIST HTML -----------------------------------------
			declare @fusionAttributeItemsHtml nvarchar(max)

			set @fusionAttributeItemsHtml = '<div style="height: 200px; overflow-y: scroll"><table class="hoverable bordered striped" style="width:100%"><thead>'
			set @fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '<th style="margin-right: 15px">Name</th>'
			set @fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '</thead><tbody>'

			select		--top 10 
						@fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '<tr>' 
											+ '<td>' + Name + '</td>'
											+ '</tr>'
			from		FusionAttribute
			where		ParentID = @ID
			order by	Name asc

			set @fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '</tbody>'
			set @fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '</table></div>'
 
			insert into @tbl values ('Items', @fusionAttributeItemsHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'LookupType' OR @Type = 'Lookup'
		begin
			-- BUILD LOOKUP LIST HTML -----------------------------------------
			declare @lookups table (RowID int identity, ID int)

			declare @MyLookupTypeID int
			if @Type = 'Lookup'
				begin
					select @MyLookupTypeID = LookupTypeID from [Lookup] where ID = @ID 
				end
			else
				begin
					set @MyLookupTypeID = @ID
				end

			insert into @lookups 
				select top 10 ID from [Lookup] where LookupTypeID = @MyLookupTypeID order by ID desc
		
			declare @lookupFieldTypes table (ID int identity, Name nvarchar(250))
			insert into @lookupFieldTypes
				select FriendlyName from FieldType where [Object] = 'LookupType' and ObjectID = @MyLookupTypeID order by SortOrder asc

			declare @lookupHtml nvarchar(max)

			set @lookupHtml = '<table class="hoverable bordered striped" style="width:100%">'

			-- Loop through field name list ---------
			set @lookupHtml = @lookupHtml + '<thead>'
			set		@current = 1
			select	@max = max(ID) from @lookupFieldTypes
			while @current <= @max
			begin
				select	@name = Name
				from	@lookupFieldTypes
				where	ID = @current

				set @lookupHtml = @lookupHtml + '<th style="margin-right: 15px">' + @name  + '</th>'

				set @current = @current + 1
			end
			set @lookupHtml = @lookupHtml + '</thead>'
			-----------------------------------------

			set @lookupHtml = @lookupHtml + '<tbody>'

			-- Loop through event list --------------
			select	@current = min(RowID) from @lookups
			select	@max = max(RowID) from @lookups

			while @current <= @max
			begin
				set @lookupHtml = @lookupHtml + '<tr>'	-- Open row for selected event.

				declare @lookupFields table (Name nvarchar(250), Value nvarchar(4000))
			
				declare @lookupID int

				select	@lookupID = ID from @lookups where RowID = @current

				insert into @lookupFields
					select		FriendlyName,
								FormattedValue
					from		FieldWithRelation
					where		ObjectType = 'Lookup' 
								and ObjectID = @lookupID

					-- Loop through each field for this selected event --
					declare @lfCurrent int,
							@lfMax int
					set		@lfCurrent = 1
					select	@lfMax = max(ID) from @lookupFieldTypes
					while @lfCurrent <= @lfMax
					begin
						select	@name = Name from @lookupFieldTypes where ID = @lfCurrent

						select @lookupHtml = @lookupHtml + '<td>' + coalesce(Value, '') + '</td>' from @lookupFields where Name = @name

						set @lfCurrent = @lfCurrent + 1
					end
					-----------------------------------------------------

				delete @lookupFields

				set @lookupHtml = @lookupHtml + '</tr>'	-- Close off row for selected lookup.

				set @current = @current + 1
			end
			-----------------------------------------

			set @lookupHtml = @lookupHtml + '</tbody>'

			set @lookupHtml = @lookupHtml + '</table>'

			insert into @tbl values ('Items', @lookupHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'ReferenceItemType' OR @Type = 'ReferenceItem'
		begin
			-- BUILD LOOKUP LIST HTML -----------------------------------------
			declare @refs table (RowID int identity, ID int)
			declare @isHierarchy bit = 0;

			declare @MyRefTypeID int
			if @Type = 'ReferenceItem'
				begin
					select @MyRefTypeID = ReferenceItemTypeID from ReferenceItem where ID = @ID 

					-- check if this item is in a hierarchy if so set the flag as true
					select  @isHierarchy = count(1) from intersecttypedetail where [object] = 'ReferenceItemType' and [objectid] = @MyRefTypeID and predicatetype = 3
				end
			else
				begin
					set @MyRefTypeID = @ID
				end

			if @isHierarchy = 1 
				begin
				insert into @refs 					
					select	top 500 
							ri.ID 
					from	[ReferenceItem] ri
							inner join Asset ast on (ri.id = ast.objectid and ast.[object] = 'ReferenceItem')
							inner join [intersect] id on (id.objectid = ri.id and id.[object] = 'ReferenceItem')
							inner join [intersect] id_2 on (id_2.[object] = 'ReferenceItem' and id_2.[objectid] = @id and id_2.subjectid = id.subjectid)
							inner join [intersecttypedetail] it on (it.id = id.intersecttypeid and it.id = id_2.intersecttypeid and it.[object]='ReferenceItemType' and it.predicatetype = 3)
					where	ri.ReferenceItemTypeID = @MyRefTypeID 
							and ast.[State] = 1 
							and ast.ID not in (select AssetID from ResponsibilityDetail where ((PermissionsBitMask & 1) = 0) and ResourceID = @resourceId)
					order by DisplayValue asc
				end
			else
				begin
				insert into @refs 
					select	top 500 
							ri.ID 
					from	[ReferenceItem] ri
							inner join Asset ast on (ri.id = ast.objectid and ast.[object] = 'ReferenceItem')
					where	ri.ReferenceItemTypeID = @MyRefTypeID 
							and ast.[State] = 1 
							and ast.ID not in (select AssetID from ResponsibilityDetail where ((PermissionsBitMask & 1) = 0) and ResourceID = @resourceId)
					order by DisplayValue asc
				end
		
			declare @refFieldTypes table (ID int identity, Name nvarchar(250))
			insert into @refFieldTypes values ('Code')
			insert into @refFieldTypes
				select FriendlyName from FieldType where [Object] = 'ReferenceItemType' and ObjectID = @MyRefTypeID order by SortOrder asc

			declare @refHtml nvarchar(max)

			set @refHtml = '<table class="hoverable bordered striped" style="width:100%; min-width: 400px">'

			-- Loop through field name list ---------
			set @refHtml = @refHtml + '<thead>'
			set		@current = 1
			select	@max = max(ID) from @refFieldTypes
			while @current <= @max
			begin
				select	@name = Name
				from	@refFieldTypes
				where	ID = @current

				set @refHtml = @refHtml + '<th style="margin-right: 15px">' + @name  + '</th>'

				set @current = @current + 1
			end
			set @refHtml = @refHtml + '</thead>'
			-----------------------------------------

			set @refHtml = @refHtml + '<tbody>'

			-- Loop through event list --------------
			select	@current = min(RowID) from @refs
			select	@max = max(RowID) from @refs

			while @current <= @max
			begin
				set @refHtml = @refHtml + '<tr>'	-- Open row for selected event.

				declare @refFields table (Name nvarchar(250), Value nvarchar(4000))
			
				declare @refID int

				select	@refID = ID from @refs where RowID = @current

				insert into @refFields
					select	'Code', Code from ReferenceItem where ID = @refID

				insert into @refFields
					select		FriendlyName,
								FormattedValue
					from		FieldWithRelation
					where		ObjectType = 'ReferenceItem' 
								and ObjectID = @refID

					-- Loop through each field for this selected event --
					declare @rfCurrent int,
							@rfMax int,
							@rfCurrentVal nvarchar(max);

					set		@rfCurrent = 1
					select	@rfMax = max(ID) from @refFieldTypes
					while @rfCurrent <= @rfMax
					begin
						select	@name = Name from @refFieldTypes where ID = @rfCurrent

						if exists (select 1 from @refFields where Name = @name)
						begin
							select @refHtml = @refHtml + '<td>' + coalesce(Value, '1') + '</td>' from @refFields where Name = @name;
						end
						else
						begin
							set @refHtml = @refHtml + '<td>&nbsp;</td>';
						end

						set @rfCurrent = @rfCurrent + 1
					end
					-----------------------------------------------------

				delete @refFields

				set @refHtml = @refHtml + '</tr>'	-- Close off row for selected lookup.

				set @current = @current + 1
			end
						
			-----------------------------------------

			set @refHtml = @refHtml + '</tbody>'

			set @refHtml = @refHtml + '</table>'

			if @max >= 500
			begin
				set @refHtml = @refHtml + '<div style="font-weight:bold;padding-top:10px">Showing top 500 items</div>'	
			end

			insert into @tbl values ('Items', @refHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'Resource' OR @Type = 'ResourceType'
		begin
			-- BUILD Resource LIST HTML -----------------------------------------
			declare @resourceItemsHtml nvarchar(max)

			set @resourceItemsHtml = '<table class="hoverable bordered striped" style="width:100%"><thead>'
			set @resourceItemsHtml = @resourceItemsHtml + '<th style="margin-right: 15px">First Name</th><th style="margin-right: 15px">Last Name</th><th>Email</th>'
			set @resourceItemsHtml = @resourceItemsHtml + '</thead><tbody>'

			select		top 10 
						@resourceItemsHtml = @resourceItemsHtml + '<tr>' + 
											'<td>' + FirstName + '</td>' + 
											'<td>' + LastName + '</td>' + 
											'<td>' + Email + '</td>'
											+ '</tr>'
			from		reporting.Global_Resource
			order by	LastName, FirstName asc

			set @resourceItemsHtml = @resourceItemsHtml + '</tbody>'
			set @resourceItemsHtml = @resourceItemsHtml + '</table>'
 
			insert into @tbl values ('Items', @resourceItemsHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'ReferenceItem'
		begin

			declare @myReferenceListID int

			select	@myReferenceListID = ReferenceItemTypeID from ReferenceItem where ID = @ID
			-- BUILD LIST HTML -----------------------------------------
			declare @referenceItemHtml nvarchar(max)

			set @referenceItemHtml = '<table class="hoverable bordered striped" style="width:100%">'
			set @referenceItemHtml = @referenceItemHtml + '<thead><th style="margin-right: 15px">Name</th></thead>'
			set @referenceItemHtml = @referenceItemHtml + '<tbody>'



			select		top 10 
						@referenceItemHtml = @referenceItemHtml + '<tr>' + '<td>' + DisplayValue + '</td>' + '</tr>'             
			from		ReferenceItem
			where		ReferenceItemTypeID = @myReferenceListID
			order by	DisplayValue desc

			set @referenceItemHtml = @referenceItemHtml + '</tbody>'
			set @referenceItemHtml = @referenceItemHtml + '</table>'
 
			insert into @tbl values ('Items', @referenceItemHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'ReferenceItemType'
		begin

		--	declare @myReferenceListID int

			--select	@myReferenceListID = ReferenceItemTypeID from ReferenceItem where ID = @ID
			-- BUILD LIST HTML -----------------------------------------
			declare @referenceItemTypeHtml nvarchar(max)

			set @referenceItemTypeHtml = '<table class="hoverable bordered striped" style="width:100%">'
			set @referenceItemTypeHtml = @referenceItemTypeHtml + '<thead><th style="margin-right: 15px">Display Value</th></thead>'
			set @referenceItemTypeHtml = @referenceItemTypeHtml + '<tbody>'



			select		top 10 
						@referenceItemTypeHtml = @referenceItemTypeHtml + '<tr>' + '<td>' + DisplayValue + '</td>' + '</tr>'             
			from		ReferenceItem
			where		ReferenceItemTypeID = @ID
			order by	DisplayValue desc

			set @referenceItemTypeHtml = @referenceItemTypeHtml + '</tbody>'
			set @referenceItemTypeHtml = @referenceItemTypeHtml + '</table>'
 
			insert into @tbl values ('Items', @referenceItemTypeHtml)
			------------------------------------------------------------------
		end;

	end
	
	if @Action = 'None'
	begin
		set @html = '<h3>{Name}</h3><div>'
	end

	if @Action = 'Preview'
	begin
		set @html = '<h3 style="positon: relative">{Name} <small style="background-color: #fff; float:right;font-size:65%;">{Type}</small></h3><div>{Description}</div>'
		set @showIcon = 0

		if @Type = 'Artifact'
		begin
			declare @artifactPathHtml nvarchar(2500) = '<table>';
			declare @artLevelResult table(ID int identity, LevelName nvarchar(250), DisplayValue nvarchar(250), Url varchar(1000));

			with ap as (
				select	O.ID,
						O.ParentID,
						'INVALID' as DisplayValue,
						L.Name as LevelName,
						dbo.GenerateNgObjectUrl('Artifact', L.ID, O.ID) as Url,
						1 as [Level]
				from	Artifact O
						inner join ArtifactType L on L.ID = O.ArtifactTypeID
				where	O.ID = @ID
				union all
				select	O.ID,
						O.ParentID,
						'INVALID' as DisplayValue,
						L.Name as LevelName,
						dbo.GenerateNgObjectUrl('Artifact', L.ID, O.ID) as Url,
						C.[Level] + 1 as [Level]
				from	Artifact O
						inner join ArtifactType L on L.ID = O.ArtifactTypeID
						inner join ap as C on C.ParentID = O.ID
			)

			insert into @artLevelResult
				select LevelName, DisplayValue, Url from ap order by [Level] desc

			select		@artifactPathHtml = coalesce(@artifactPathHtml + '', '') + '<tr><td style="width: 15px">' +  cast([ID] as varchar) + '</td><td>' +  LevelName + '</td><td><b><a href="' + Url + '">' + DisplayValue + '</a></b>' + '</td></tr>'
			from		@artLevelResult
			
			set @artifactPathHtml =  @artifactPathHtml + '</table>'

			set @html = @html + '<div><b>Path:</b></div><div>' + coalesce(@artifactPathHtml,'') + '</div>'

			set @hasDynamicFields = 1
		end;

		if @Type = 'FusionAttribute'
		begin
			declare @faPathHtml nvarchar(2500) = '<table>';
			declare @faLevelResult table(ID int identity, LevelName nvarchar(250), Name nvarchar(250));

			with fap as (
				select	O.ID,
						O.ParentID,
						O.Name,
						L.Name as LevelName,
						1 as [Level]
				from	FusionAttribute O
						inner join FusionAttributeType L on L.ID = O.FusionAttributeTypeID
				where	O.ID = @ID
				union all
				select	O.ID,
						O.ParentID,
						O.Name,
						L.Name as LevelName,
						C.[Level] + 1 as [Level]
				from	FusionAttribute O
						inner join FusionAttributeType L on L.ID = O.FusionAttributeTypeID
						inner join fap as C on C.ParentID = O.ID
			)

			insert into @faLevelResult
				select LevelName, Name from fap order by [Level] desc

			select		@faPathHtml = @faPathHtml + 
						'<tr><td colspan="2">Configuration</td><td><b><a href="/fusion/' + cast(F.ID as nvarchar) + '">' + coalesce(F.Name,'') + '</a></b></td></tr>' 
			from		Fusion F 
						inner join FusionAttribute A on A.FusionID = F.ID and A.ID = @ID

			select		@faPathHtml = coalesce(@faPathHtml + '', '') + '<tr><td style="width: 15px">' +  cast(ID as varchar) + '</td><td>' +  LevelName + '</td><td><b>' + Name + '</b>' + '</td></tr>'
			from		@faLevelResult

			set @faPathHtml =  @faPathHtml + '</table>'

			set @html = @html + '<div><b>Path:</b></div><div>' + coalesce(@faPathHtml,'') + '</div>'

			set @hasDynamicFields = 1
		end

		if @Type = 'Intersect'
		begin
			set @hasDynamicFields = 1
		end;

		
		if @Type = 'Issue'
		begin
			insert into @tbl values('Name', '')
			insert into @tbl values('Description', '')
					
			if exists (select id from issue where id = @ID)
			begin			
				set @html = @html + '<div><b>Issue Type:</b> {IssueType}</div>'
				set @html = @html + '<div><b>Criticality:</b> {Criticality}</div>'
						
				insert into @tbl 
					select 'IssueType', it.name 
					from issuetype it inner join issue i on(i.issuetypeid = it.id) 
					where i.id = @ID

				insert into @tbl 
					select 'Criticality', case when i.Criticality = 0 then 'Negligible' when i.Criticality = 1 then 'Low' when i.Criticality = 2 then 'Medium' when i.Criticality = 3 then 'High'  when i.Criticality = 4 then 'Critical' else 'N/A' end
					from issuetype it inner join issue i on(i.issuetypeid = it.id) 
					where i.id = @ID

				set @hasDynamicFields = 1
			end			
		end;

		if @Type = 'Resource'
		begin
			--declare @e nvarchar(500), @fn nvarchar(250), @ln nvarchar(250)
			--select	@e = Email, @fn = FirstName, @ln = LastName
			--from	reporting.Global_Resource
			--where	ResourceID = @ID

			--insert into @tbl values ('Email', @e)
			--insert into @tbl values ('FirstName', @fn)
			--insert into @tbl values ('LastName', @ln)
			--insert into @tbl values ('Role', '')

			--set @html = @html + '<div><b>Email:</b> {Email}</div>'
			--set @html = @html + '<div><b>First Name:</b> {FirstName}</div>'
			--set @html = @html + '<div><b>Last Name:</b> {LastName}</div>'

			set @hasDynamicFields = 1
		end;

		if @Type = 'Rule'
		begin
			insert into @tbl
				select	'Name', DisplayValue
				from	[Rule] O
				where	ID = @ID

			set @hasDynamicFields = 1
		end;

		if @Type = 'RuleDimension'
		begin
			insert into @tbl
				select	'Description', [Description]
				from	RuleDimension
				where	ID = @ID
			insert into @tbl
				select	'Name', [Name]
				from	RuleDimension
				where	ID = @ID

			--set @html = @html + '<div><b>Path:</b> {Description}</div>'
						
		end;

		if @Type = 'Taxonomy'
		begin
			declare @taxonomyPathHtml nvarchar(2500) = '<table>';

			with tp as (
				select	O.ID,
						O.ParentID,
						O.DisplayValue,
						coalesce(L.Name, 'Level ' + cast(O.[Level] as varchar)) as LevelName,
						O.[Level]
				from	[Taxonomy] O
						left join TaxonomyTypeLevel L on L.TaxonomyTypeID = O.TaxonomyTypeID and L.[Level] = O.[Level]
				where	O.ID = @ID
				union all
				select	O.ID,
						O.ParentID,
						O.DisplayValue,
						coalesce(L.Name, 'Level ' + cast(O.[Level] as varchar)) as LevelName,
						O.[Level]
				from	[Taxonomy] O
						outer apply (
									select Name from TaxonomyTypeLevel where TaxonomyTypeID = O.TaxonomyTypeID and [Level] = O.[Level]
									) L
						--left join TaxonomyTypeLevel L on L.TaxonomyTypeID = O.TaxonomyTypeID and L.[Level] = O.[Level]
						inner join tp as C on C.ParentID = O.ID
			)

			select		@taxonomyPathHtml = coalesce(@taxonomyPathHtml + '', '') + '<tr><td style="width: 15px">' +  cast([Level] as varchar) + '</td><td>' +  LevelName + '</td><td><b>' + DisplayValue + '</b>' + '</td></tr>'
			from		tp
			order by	[Level]
			
			set @taxonomyPathHtml =  @taxonomyPathHtml + '</table>'

			set @html = @html + '<div><b>Path:</b></div><div>' + coalesce(@taxonomyPathHtml,'') + '</div>'

			set @hasDynamicFields = 1
		end;

		if @Type = 'TaxonomyType'
		begin
			insert into @tbl
				select	'Name', Name
				from	TaxonomyType O
				where	ID = @ID

			set @hasDynamicFields = 1
		end;
		
		-- If required, get dynamic fields to add to list.
		if @hasDynamicFields = 1
		begin
			select	@html = @html + '<div><b>' + FriendlyName + '</b>: ' + '{' + Name + '}' + '</div>' 
			from	FieldWithRelation
			where	ObjectType = @Type
					and ObjectID = @ID
					and Name not in (select Name from @tbl)

			insert into @tbl
				select	Name,
						FormattedValue
				from	FieldWithRelation
				where	ObjectType = @Type
						and ObjectID = @ID
						and Name not in (select Name from @tbl)
		end;
	end

	if @Action = 'Statistics'
	begin
		set @html = '<h3>{Name}</h3><div>{Statistics}</div>'

		set @hasStats = case @Type
							when 'Artifact' then 1
							when 'Taxonomy' then 1
							else 0
						end

		-- If required, build statistics table
		if @hasStats = 1
		begin
			-- BUILD STATS LIST HTML -----------------------------------------
			declare @statsHtml nvarchar(max)

			declare @stats table (ID int identity, Name nvarchar(250), Score bit)

			insert into @stats 
				select		G.Name + ': ' + I.Name,
							MR.Value
				from		metrics.Score S
							inner join metrics.MapResult MR on MR.ScoreID = S.ID and S.EffectiveEndDate = '12/31/9999' and S.Object = @Type and S.ObjectID = @ID
							inner join metrics.Map M on M.ID = MR.MapID
							inner join metrics.[Group] G on G.ID = M.GroupID
							inner join metrics.Item I on I.ID = M.ItemID
				order by	G.Name + ': ' + I.Name

			set @statsHtml = '<table class="hoverable bordered striped" style="width:100%">'

			-- Loop through field name list ---------
			set @statsHtml = @statsHtml + '<tbody>'
			set		@current = 1
			select	@max = max(ID) from @stats
			while @current <= @max
			begin
				select	@statsHtml = @statsHtml + '<tr><td>' + Name  + '</td>' + '<td>' + case when Score = 1 then 'Pass' else 'Fail' end  + ' </td></tr>'
				from	@stats
				where	ID = @current

				set @current = @current + 1
			end
			set @statsHtml = @statsHtml + '</tbody>'
			-----------------------------------------

			insert into @tbl values ('Statistics', @statsHtml)

			------------------------------------------------------------------
		end;
	end

	if exists (select 1 from ResponsibilityDetail where ((PermissionsBitMask & 1) = 0) and resourceid = @resourceId and [object] = @Type and objectid = @ID)
	begin
		set @html = 'This item either does not exist or you do not have access to its details.';

		-- Return the properly formatted values.
		select	'' as Title,
				@html as Body;
	end
	else
	begin
		-- Replace the fields in the template with the appropriate text value.
		set		@current = 1
		select	@max = max(ID) from @tbl

		while @current <= @max
		begin
			select	@name = '{' + Name + '}',
					@value = COALESCE(Value, '')
			from	@tbl 
			where	ID = @current

			if @showIcon = 1
			begin
				if @name = '{Name}' and @icon is not null
				begin
					update	@tbl 
					set		Value = '<div class="pull-left" style="width: 30px">' + @icon + '</div>' + '<div class="pull-right">' + @value + '</div>'
					where	ID = @current
					--set @usedIconAlready = 1
				end
			end

			set @html = REPLACE(@html, @name, @value)

			set @current = @current + 1
		end

		--if @showIcon = 1 and @icon is not null
		--begin
		--	set @html = @icon + '<br/>' + @html
		--end

		set @html = '<div style="max-height: 500px; min-width: 400px; overflow-y: auto">' + @html + '</div>'

		-- Return the properly formatted values.
		select	'' as Title,
				@html as Body;
	end
END
GO

CREATE NONCLUSTERED INDEX IX_ResponsibilityTypeRelationRuleResult_Asset_PermissionsBitMask_SecurityAsset ON ResponsibilityTypeRelationRuleResult (AssetID ASC, PermissionsBitMask ASC, SecurityAsset ASC, SecurityAssetID ASC)
GO


ALTER procedure [dbo].[DeleteObject]
 @ObjTemp varchar(50),
 @ObjectIDTemp int,
 @ResourceIDTemp int
as 
begin
	set nocount on
	declare    @Obj varchar(50) = @ObjTemp,
		@ObjectID int = @ObjectIDTemp,
		@ResourceID int = @ResourceIDTemp
	
	declare    @Object varchar(50) = @Obj,
		@CurrentDate datetime = getutcdate(),
		@predicateType int = 0,
		@trans varchar(25) = 'Trans',
		@current int = 1,
		@max int,
		@IsType bit = 0

	declare @h table (IntersectID int, ID bigint, ObjectID int, Processed bit null)
	declare @ht table (IntersectTypeID int, ID int, ObjectID int, Processed bit null)

	declare @ClearAttributes bit = 0,
			@ClearComments bit = 0,
			@ClearIntersects bit = 0,
			@ClearFavorites bit = 0,
			@ClearFields bit = 0,
			@ClearFollows bit = 0,
			@ClearIssues bit = 0,
			@ClearNyms bit = 0,
			@ClearResponsibilities bit = 0,
			@ClearSiteNav bit = 0,
			@ClearPromotion bit = 0

	if charindex('Type', @Object) > 0
	begin
		set @IsType = 1
	end

	begin try
		begin transaction @trans

		if @Obj = 'Artifact' or @Obj = 'ArtifactType' or @Obj = 'FusionAttribute' or @Obj = 'FusionAttributeType' or @Obj = 'ReferenceItem' or @Obj = 'ReferenceItemType'
		begin
			set @predicateType = 3
		end
		if @Obj = 'Policy' or @Obj = 'PolicyType' or @Obj = 'Taxonomy' or @Obj = 'TaxonomyType'
		begin
			set @predicateType = 4
		end

		if @predicateType > 0
		begin
			if @IsType = 1
				begin
					insert into @ht
						select	null,
								ID,
								ObjectID,
								0
						from	AssetType
						where	[Object] = @Obj and ObjectID = @ObjectID

					while exists(select 1 from @ht where Processed = 0)
					begin
						insert into @ht
							select	I.ID,
									C.ID,
									C.ObjectID,
									null
							from	AssetType C
									inner join IntersectType I on I.Subject = @Obj and I.Object = @Obj and I.ObjectID = C.ObjectID
									inner join [Predicate] PR on PR.ID = I.PredicateID and PR.[Type] = @predicateType
									inner join AssetType P on P.Object = I.Subject and P.ObjectID = I.SubjectID
									inner join @ht T on T.ID = P.ID and T.Processed = 0

						update	@ht set Processed = 1 where Processed = 0
						update	@ht set Processed = 0 where Processed is null
					end

					-- Get all assets based on the types found above.
					insert into @h 
						select null, ID, ObjectID, 1 from Asset where AssetTypeID in (select ID from @ht)
				end
			else
				begin
					insert into @h
						select	null,
								ID,
								ObjectID,
								0
						from	Asset
						where	[Object] = @Obj and ObjectID = @ObjectID

					while exists(select 1 from @h where Processed = 0)
					begin
						insert into @h
							select	I.IntersectID,
									C.ID,
									C.ObjectID,
									null
							from	Asset C
									inner join PredicateIntersect I on I.PredicateType = @predicateType and I.Subject = @Obj and I.Object = @Obj and I.ObjectID = C.ObjectID
									inner join Asset P on P.Object = I.Subject and P.ObjectID = I.SubjectID
									inner join @h T on T.ID = P.ID and T.Processed = 0

						update	@h set Processed = 1 where Processed = 0
						update	@h set Processed = 0 where Processed is null
					end
				end
		end
		
		-- INDEX
		INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID],[AssetID])
			select	'ObjectIndex', 
					'D',
					O.Object, 
					O.ObjectID,
					O.ID
			from	Asset O
					inner join @h I on O.ID = I.ID

		-- AUDIT
		insert into reporting.Global_Audit (Object, ObjectID, ObjectName, ResourceID, Date, Action, ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, ActionDescription)
			select	O.Object, 
					O.ObjectID, 
					O.DisplayValue, 
					@ResourceID, 
					@CurrentDate, 
					'Deleted', 
					O.Object, 
					O.ObjectID, 
					O.TypeName, 
					O.DisplayValue, 
					'This asset has been removed.' 
			from	AssetDetail O
					inner join @h I on O.ID = I.ID
			union
			select	O.Object, 
					O.ObjectID, 
					O.Name, 
					@ResourceID, 
					@CurrentDate, 
					'Deleted', 
					O.Object, 
					O.ObjectID, 
					O.Name, 
					O.Name, 
					'This asset type has been removed.' 
			from	AssetType O
					inner join @ht I on O.ID = I.ID

		-- WORKFLOW

		if @Object = 'Artifact'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1
			set @ClearPromotion = 1

			delete Artifact where ID in (select ObjectID from @h)
		end

		if @Object = 'ArtifactType'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1
			set @ClearSiteNav = 1
			
			delete	T
			from	ArtifactTypeExportTemplate T
					inner join @ht h on h.ObjectID = T.ID

			delete	Artifact
			where	ID in (select ObjectID from @h)

			delete	ArtifactType			
			where	ID in (select ObjectID from @ht)
		end

		if @Object = 'AttributeType'
		begin
			declare @at table (ID int)
			declare @a table (ID int);

			with ht as	(
						select	ID, 
								ParentID
						from	AttributeType
						where	ID = @ObjectID
						union all
						select	C.ID,
								C.ParentID
						from	AttributeType C
								inner join ht P on P.ID = C.ParentID
						)

			insert into @at 
				select ID from ht

			insert into @a
				select ID from Attribute where AttributeTypeID in (select ID from @at)

			-- AUDIT
			insert into reporting.Global_Audit (Object, ObjectID, ObjectName, ResourceID, Date, Action, ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, ActionDescription)
				select	A.Object, 
						A.ObjectID, 
						A.DisplayValue, 
						@ResourceID, 
						@CurrentDate, 
						'Deleted', 
						'Attribute', 
						O.ID, 
						O.Name, 
						O.FormattedValue, 
						'This attribute has been removed.' 
				from	AttributeDetail O
						inner join AssetDetail A on A.Object = O.ObjectType and A.ObjectID = O.ObjectID
						inner join @a I on O.ID = I.ID
				union
				select	A.Object, 
						A.ObjectID, 
						A.Name, 
						@ResourceID, 
						@CurrentDate, 
						'Deleted', 
						'AttributeType', 
						O.ID, 
						'Attribute Type', 
						O.Name, 
						'This attribute type has been removed.' 
				from	AttributeType O
						inner join @at I on O.ID = I.ID
						inner join AttributeTypeRelation R on R.AttributeTypeID = O.ID
						inner join AssetType A on A.Object = R.ObjectType and A.ObjectID = R.ObjectID

			update	T
			set		T.UpdatedBy = @ResourceID,
					T.UpdatedOn = @CurrentDate
			from	Asset T
					inner join [Attribute] S on S.ObjectType = T.Object and S.ObjectID = T.ObjectID and S.ID in (select ID from @a)

			update	T
			set		T.UpdatedBy = @ResourceID,
					T.UpdatedOn = @CurrentDate
			from	AssetType T
					inner join AttributeTypeRelation S on S.ObjectType = T.Object and S.ObjectID = T.ObjectID 
					inner join AttributeType A on A.ID = S.AttributeTypeID and A.ID in (select ID from @at)

			delete Field					where ObjectType = 'Attribute' and ObjectID in (select ID from @a)
			delete Attribute				where ID in (select ID from @a)
			delete FieldType				where Object = 'AttributeType' and ObjectID in (select ID from @at)
			delete AttributeTypeRelation	where AttributeTypeID in (select ID from @at)
			delete AttributeType			where ID in (select ID from @at)
		end

		if @Object = 'FieldType'
		begin
			-- AUDIT
			insert into reporting.Global_Audit (Object, ObjectID, ObjectName, ResourceID, Date, Action, ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, ActionDescription)
				select	A.Object, 
						A.ObjectID, 
						A.DisplayValue, 
						@ResourceID, 
						@CurrentDate, 
						'Deleted', 
						A.Object, 
						A.ObjectID, 
						T.Name, 
						O.FormattedValue, 
						'This field has been removed.' 
				from	Field O
						inner join FieldType T on T.ID = O.FieldTypeID and T.ID = @ObjectID
						inner join AssetDetail A on A.Object = O.ObjectType and A.ObjectID = O.ObjectID
				union
				select	A.Object, 
						A.ObjectID, 
						A.Name, 
						@ResourceID, 
						@CurrentDate, 
						'Deleted', 
						'FieldType', 
						O.ID, 
						'Field Type', 
						O.Name, 
						'This field type has been removed.' 
				from	FieldType O
						inner join AssetType A on A.Object = O.Object and A.ObjectID = O.ObjectID and O.ID = @ObjectID

			update	T
			set		T.UpdatedBy = @ResourceID,
					T.UpdatedOn = @CurrentDate
			from	Asset T
					inner join Field S on S.ObjectType = T.Object and S.ObjectID = T.ObjectID and S.FieldTypeID = @ObjectID

			update	T
			set		T.UpdatedBy = @ResourceID,
					T.UpdatedOn = @CurrentDate
			from	AssetType T
					inner join FieldType S on S.Object = T.Object and S.ObjectID = T.ObjectID and S.ID = @ObjectID

			delete	Field 
			where	FieldTypeID = @ObjectID
			
			update	FieldType 
			set		ParentFieldTypeID = null 
			where	ParentFieldTypeID = @ObjectID

			delete	FieldType 
			where	ID = @ObjectID
		end

		if @Object = 'FusionAttribute'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1
			set @ClearPromotion = 1

			delete FusionAttribute where ID in (select ObjectID from @h)
		end

		if @Object = 'FusionAttributeType'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1

			delete FusionAttribute		where ID in (select ObjectID from @h)
			delete FusionAttributeType	where ID in (select ObjectID from @ht)
		end

		if @Object = 'Fusion'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1

			--insert into @h
			--	select	I.ID, null, F.ID, null 
			--	from	[IntersectDetail] I
			--			inner join FusionAttribute F on I.Subject = 'FusionAttribute' 
			--											and I.Object = 'FusionAttribute' 
			--											and (F.ID = I.SubjectID OR F.ID = I.ObjectID) 
			--											and F.FusionID = @ObjectID
			--											and I.PredicateType = 3

			insert into @h								
				select I.ID, null, F.ID, null 
				from [Intersect] I
				inner join IntersectType T on T.ID = I.IntersectTypeID
				inner join [Predicate] P on P.ID = T.PredicateID and P.[Type] = 3
				inner join FusionAttribute F on I.[Subject] = 'FusionAttribute' and I.[Object] = 'FusionAttribute'
					and (I.SubjectID = F.ID or I.ObjectID = F.ID) and F.FusionID = @ObjectID;

			delete FusionAttribute where FusionID = @ObjectID
			delete Fusion where ID = @ObjectID
		end

		if @Object = 'FusionType'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1

			insert into @ht
				select	ID, null, null, null
				from	IntersectType
				where	Subject = 'FusionAttributeType' 
						and Object = 'FusionAttributeType' 
						and (
							SubjectID in (select ID from FusionAttributeType where FusionTypeID = @ObjectID)
							or ObjectID in (select ID from FusionAttributeType where FusionTypeID = @ObjectID)
							)

			insert into @h
				select ID, null, null, null from [Intersect] where IntersectTypeID in (select IntersectTypeID from @ht)

			delete FusionAttribute where FusionAttributeTypeID in (select ID from FusionAttributeType where FusionTypeID = @ObjectID)
			delete Fusion where FusionTypeID = @ObjectID
			delete FusionAttributeType where FusionTypeID = @ObjectID
			delete FusionType where ID = @ObjectID
		end

		if @Object = 'Intersect'
		begin
			update [Intersect] set Deleted = 1 where ID = @ObjectID
		end

		if @Object = 'IntersectType'
		begin
			set @ClearAttributes = 1
			set @ClearFields = 1

			delete [Intersect] where IntersectTypeID = @ObjectID
			delete IntersectType where ID = @ObjectID
		end

		if @Object = 'LookupType'
		begin
			set @ClearFields = 1

			delete [Lookup] where LookupTypeID = @ObjectID
			delete  LookupType where ID=@ObjectID
		end

		if @Object = 'Policy'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1
			set @ClearPromotion = 1

			delete [Policy] where ID in (select ObjectID from @h)
		end

		if @Object = 'PolicyType'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1
			set @ClearSiteNav = 1

			delete [Policy] where PolicyTypeID in (select ObjectID from @ht)
			delete PolicyTypeLevel where PolicyTypeID in (select ObjectID from @ht)
			delete PolicyType where ID in (select ObjectID from @ht)
		end

		if @Object = 'ReferenceItem'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1

			delete ReferenceItem where ID = @ObjectID			
		end

		if @Object = 'ReferenceItemType'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1

			delete ReferenceItem where ReferenceItemTypeID = @ObjectID
			delete ReferenceItemType where ID = @ObjectID
		end

		if @Object = 'ResponsibilityType'
		begin
			delete ResponsibilityTypeRelationOverrideItem where ResponsibilityTypeID = @ObjectID
			delete ResponsibilityTypeRelationRuleResult where ResponsibilityTypeID = @ObjectID
			delete ResponsibilityTypeRelationRule where ResponsibilityTypeID = @ObjectID
			delete ResponsibilityType where ID = @ObjectID
		end

		if @Object = 'Rule'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1

			-- DELETE
			delete T
			from   RuleResultQualifierType T
				   inner join RuleImplementation I on I.ID = T.RuleImplementationID and I.RuleID = @ObjectID

			delete	Q
			from	RuleResultQualifier Q 
					inner join RuleResult S on S.ID = Q.RuleResultID
					inner join RuleImplementation I on I.ID = S.RuleImplementationID and I.RuleID = @ObjectID

			delete	F
			from	RuleResultFusionAttribute F
					inner join RuleResult S on S.ID = F.RuleResultID
					inner join RuleImplementation I on I.ID = S.RuleImplementationID and I.RuleID = @ObjectID

			delete	RuleImplementation where RuleID = @ObjectID

			delete	[Rule] where ID = @ObjectID
		end

		if @Object = 'RuleType'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1

			delete	Q
			from	RuleResultQualifier Q 
					inner join RuleResult S on S.ID = Q.RuleResultID
					inner join RuleImplementation I on I.ID = S.RuleImplementationID
					inner join [Rule] R on R.ID = I.RuleID and R.RuleTypeID = @ObjectID

			delete	F
			from	RuleResultQualifierType F
					inner join RuleImplementation I on I.ID = F.RuleImplementationID
					inner join [Rule] R on R.ID = I.RuleID and R.RuleTypeID = @ObjectID

			delete	F
			from	RuleResultFusionAttribute F
					inner join RuleResult S on S.ID = F.RuleResultID
					inner join RuleImplementation I on I.ID = S.RuleImplementationID
					inner join [Rule] R on R.ID = I.RuleID and R.RuleTypeID = @ObjectID

			delete	S
			from	RuleResult S
					inner join RuleImplementation I on I.ID = S.RuleImplementationID
					inner join [Rule] R on R.ID = I.RuleID and R.RuleTypeID = @ObjectID

			delete [RuleImplementation] where RuleID in (select ID from [Rule] where RuleTypeID = @ObjectID)

			delete [Rule] where RuleTypeID = @ObjectID

			delete RuleType where ID = @ObjectID
		end

		if @Object = 'SurveyType'
		begin
			delete Survey where SurveyTypeID = @ObjectID
			delete SurveyType where ID = @ObjectID
		end

		if @Object = 'Taxonomy'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1
			set @ClearPromotion = 1

			delete Taxonomy where ID in (select ObjectID from @h)
		end

		if @Object = 'TaxonomyType'
		begin
			set @ClearAttributes = 1
			set @ClearComments = 1
			set @ClearIntersects = 1
			set @ClearFavorites = 1
			set @ClearFields = 1
			set @ClearFollows = 1
			set @ClearIssues = 1
			set @ClearNyms = 1
			set @ClearResponsibilities = 1
			set @ClearSiteNav = 1

			delete Taxonomy where TaxonomyTypeID = @ObjectID
			delete TaxonomyTypeLevel where TaxonomyTypeID = @ObjectID
			delete TaxonomyType where ID = @ObjectID
		end

		-- DELETE (Supporting Tables) ---------------------------------------------------------

		-- Attribute deletion
		IF @ClearAttributes = 1 AND @IsType = 0
		BEGIN
			delete Field where ObjectType = 'Attribute' and ObjectID in (select ID from Attribute where ObjectType = @Object and ObjectID in (select ObjectID from @h))
			delete Attribute where ObjectType = @Object and ObjectID in (select ObjectID from @h)
		END

		-- Intersect deletion
		IF @ClearIntersects = 1
		BEGIN
			DECLARE @tblIntersectIDs table (ID int)

			INSERT INTO @tblIntersectIDs
				SELECT	ID
				FROM	[Intersect]
				WHERE	(Subject = @Object and SubjectID in (select ObjectID from @h)) OR (Object = @Object and ObjectID in (select ObjectID from @h))

			--delete	MapItem 
			--where	SourceIntersectID in (select ID from @tblIntersectIDs) OR
			--		TargetIntersectID in (select ID from @tblIntersectIDs)

			delete [Intersect] where ID in (select ID from @tblIntersectIDs)
		END

		-- Comment deletion
		IF @ClearComments = 1 AND @IsType = 0
		BEGIN
			delete	CommentRelation
			where	ObjectType = @Object 
					and ObjectID in (select ObjectID from @h)

			delete	CommentVote
			where	CommentID in (
								select	ID
								from	Comment
								where	OwnerObjectType = @Object 
										and OwnerObjectID in (select ObjectID from @h)			
								)

			delete	Comment
			where	OwnerObjectType = @Object 
					and OwnerObjectID in (select ObjectID from @h)
		END

		-- Site menu deletion
		IF @ClearSiteNav = 1
		BEGIN
			delete sitenav where objectid = @ObjectID and [object] = @Object
		END

		IF @ClearPromotion = 1
		BEGIN
			delete from fusion.rulepromotion where objecttype = @Object and objectid = @ObjectID
		END 


		-- Favorite deletion
		IF @ClearFavorites = 1
		BEGIN
			IF @IsType = 1
				BEGIN
					delete	Favorite
					where	Object = @Object 
							and ObjectID in (select ObjectID from @ht)
				END
			ELSE
				BEGIN 
					delete	Favorite
					where	Object = @Object
							and ObjectID in (select ObjectID from @h)
				END
		END

		-- Field deletion
		IF @ClearFields = 1
		BEGIN
			IF @IsType = 1
				BEGIN
					delete	FieldType
					where	[Object] = @Object 
							and ObjectID in (select ObjectID from @ht)
				END
			ELSE
				BEGIN
					delete	Field
					where	ObjectType = @Object 
							and ObjectID in (select ObjectID from @h)
				END
		END

		-- Follow deletion
		IF @ClearFollows = 1
		BEGIN
			IF @IsType = 1
				BEGIN
					delete	Follow
					where	ObjectType = @Object 
							and ObjectID in (select ObjectID from @ht)
				END
			ELSE
				BEGIN 
					delete	Follow
					where	ObjectType = @Object
							and ObjectID in (select ObjectID from @h)
				END
		END

		-- Issue deletion
		IF @ClearIssues = 1 AND @IsType = 0
		BEGIN
			delete	Issue
			where	Object = @Object 
					and ObjectID in (select ObjectID from @h)
		END

		-- Nym deletion
		IF @ClearNyms = 1 AND @IsType = 0
		BEGIN
			IF @IsType = 1
				BEGIN 
					delete	NymRelation
					where	Object = @Object 
							and ObjectID in (select ObjectID from @ht)			
				END
			ELSE
				BEGIN
					delete	Nym
					where	Object = @Object 
							and ObjectID in (select ObjectID from @h)
				END
		END

		-- Responsibility deletion
		IF @ClearResponsibilities = 1 AND @IsType = 0
		BEGIN
			IF @IsType = 1
				BEGIN
					delete ResponsibilityTypeRelation		where ObjectType = @Obj and ObjectID in (select ObjectID from @ht)
					delete ResponsibilityTypeObjectClaim	where ObjectType = @Obj and ObjectID in (select ObjectID from @ht)
				END
			ELSE
				BEGIN
					delete	T
					from	ResponsibilityTypeRelationOverrideItem T
							inner join Asset A on A.ID = T.AssetID and A.ID in (select ID from @h)

					delete	T
					from	ResponsibilityTypeRelationRuleResult T
							inner join Asset A on A.ID = T.AssetID and A.ID in (select ID from @h)
				END
		END
		---------------------------------------------------------------------------------------

		if @IsType = 1
		begin
			delete AttributeTypeRelation			where ObjectType = @Obj AND ObjectID in (select ObjectID from @ht)
			delete IntersectType					where (Object = @Obj and ObjectID in (select ObjectID from @ht)) OR (Subject = @Obj and SubjectID in (select ObjectID from @ht))
			delete ObjectStyle						where ObjectType = @Obj AND ObjectID in (select ObjectID from @ht)
		end
		
		commit transaction @trans
	end try
	begin catch
		DECLARE @ErrorMessage NVARCHAR(4000)
		DECLARE @ErrorSeverity INT
	    DECLARE @ErrorState INT

		SELECT 
			@ErrorMessage = ERROR_MESSAGE(),
			@ErrorSeverity = ERROR_SEVERITY(),
			@ErrorState = ERROR_STATE()

		-- Use RAISERROR inside the CATCH block to return error
		-- information about the original error that caused
		-- execution to jump to the CATCH block.
		RAISERROR (@ErrorMessage, -- Message text.
				   @ErrorSeverity, -- Severity.
				   @ErrorState -- State.
				   )

		rollback transaction @trans
	end catch
end
GO

DROP TABLE [cache].[AssetDelete]
GO
DROP TABLE [cache].[AssetEdit]
GO
DROP TABLE [cache].[AssetResponsibility]
GO
DROP TABLE [cache].[NoRead]
GO
ALTER TABLE [dbo].[ResponsibilityTypeRelationItem] SET ( SYSTEM_VERSIONING = OFF  )
GO

DROP TABLE [dbo].[ResponsibilityTypeRelationItem]
GO

DROP TABLE [dbo].[ResponsibilityTypeRelationItem_History]
GO
ALTER TABLE [dbo].[ResponsibilityTypeRelationTypeItem] SET ( SYSTEM_VERSIONING = OFF  )
GO

DROP TABLE [dbo].[ResponsibilityTypeRelationTypeItem]
GO

DROP TABLE [dbo].[ResponsibilityTypeRelationTypeItem_History]
GO
DROP VIEW [dbo].[ResponsibilityDetails]
GO

DROP VIEW [responsibility].[ClaimCore]
GO

DROP VIEW [responsibility].[Core]
GO
DROP PROCEDURE [cache].[SecurityProcessor]
GO