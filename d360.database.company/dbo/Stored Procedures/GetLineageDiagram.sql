CREATE procedure GetLineageDiagram
--declare 
	@type varchar(50),
	@id int

--set @type = 'Artifact'
--set @id = 11808
as
begin
	declare @tbl table	(
						IntersectID int, IntersectTypeID int, ID int, 
						SubjectNodeID int, SubjectTypeName nvarchar(1000), SourceType varchar(50), SourceTypeID int, SubjectObjectName nvarchar(1000), Subject varchar(50), SubjectID int, SubjectBackColor varchar(10), SubjectForeColor varchar(10),  
						ObjectNodeID int, ObjectTypeName nvarchar(1000), ObjectType varchar(50), ObjectTypeID int, ObjectObjectName nvarchar(1000), Object varchar(50), ObjectID int, ObjectBackColor varchar(10), ObjectForeColor varchar(10),
						PredicateID int, Predicate nvarchar(250), MappingRuleCount int
						)
    insert into @tbl
	    select	--distinct
			    R.IntersectID,
				R.IntersectTypeID,
			    M.ID,
			    M.SubjectIntersectNodeID,
			    R.SourceTypeName,
			    R.SourceType,
			    R.SourceTypeID,
			    R.SourceObjectName,
			    R.SourceObject,
			    R.SourceObjectID,
				coalesce(SD.IconBackColor, '#000') as SourceIconBackColor,
				coalesce(SD.IconForeColor, '#fff') as SourceIconForeColor,
			    M.ObjectIntersectNodeID,
			    R.TargetTypeName,
			    R.TargetType,
			    R.TargetTypeID,
			    R.TargetObjectName,
			    R.TargetObject,
			    R.TargetObjectID,
				coalesce(TD.IconBackColor, '#000') as TargetIconBackColor,
				coalesce(TD.IconForeColor, '#fff') as TargetIconForeColor,
			    M.PredicateID,
			    P.Name as Predicate,
			    0
	    from	IntersectMap M
			    inner join [cache].[Relationships] R on M.SubjectIntersectNodeID = R.SourceIntersectNodeID and M.ObjectIntersectNodeID = R.TargetintersectNodeID and M.[Type] = 1
			    left join ObjectStyle SD with(nolock) on SD.ObjectType = R.SourceType and SD.ObjectID = R.[SourceTypeID]
				left join ObjectStyle TD with(nolock) on TD.ObjectType = R.TargetType and TD.ObjectID = R.[TargetTypeID]
			    inner join Predicate P on P.ID = M.PredicateID
			    inner join [cache].[Relationship] SR on SR.SourceObject = @type and SR.SourceObjectID = @id and SR.TargetObject = R.SourceObject and SR.TargetObjectID = R.SourceObjectID
			    inner join [cache].[Relationship] TR on TR.SourceObject = @type and TR.SourceObjectID = @id and TR.TargetObject = R.TargetObject and TR.TargetObjectID = R.TargetObjectID
	    union
	    select	--distinct
			    R.IntersectID,
				R.IntersectTypeID,
			    M.ID,
			    M.SubjectIntersectNodeID,
			    R.SourceTypeName,
			    R.SourceType,
			    R.SourceTypeID,
			    R.SourceObjectName,
			    R.SourceObject,
			    R.SourceObjectID,
				coalesce(SD.IconBackColor, '#000') as SourceIconBackColor,
				coalesce(SD.IconForeColor, '#fff') as SourceIconForeColor,
			    M.ObjectIntersectNodeID,
			    R.TargetTypeName,
			    R.TargetType,
			    R.TargetTypeID,
			    R.TargetObjectName,
			    R.TargetObject,
			    R.TargetObjectID,
				coalesce(TD.IconBackColor, '#000') as TargetIconBackColor,
				coalesce(TD.IconForeColor, '#fff') as TargetIconForeColor,
			    M.PredicateID,
			    P.Name as Predicate,
			    0
	    from	IntersectMap M
			    inner join [cache].[Relationships] R on M.SubjectIntersectNodeID = R.SourceIntersectNodeID and M.ObjectIntersectNodeID = R.TargetintersectNodeID and R.SourceObject = @type and R.SourceObjectID = @id and M.[Type] = 1
			    left join ObjectStyle SD with(nolock) on SD.ObjectType = R.SourceType and SD.ObjectID = R.[SourceTypeID]
				left join ObjectStyle TD with(nolock) on TD.ObjectType = R.TargetType and TD.ObjectID = R.[TargetTypeID]
			    inner join Predicate P on P.ID = M.PredicateID
	    union
	    select	--distinct
			    R.IntersectID,
				R.IntersectTypeID,
			    M.ID,
			    M.SubjectIntersectNodeID,
			    R.SourceTypeName,
			    R.SourceType,
			    R.SourceTypeID,
			    R.SourceObjectName,
			    R.SourceObject,
			    R.SourceObjectID,
				coalesce(SD.IconBackColor, '#000') as SourceIconBackColor,
				coalesce(SD.IconForeColor, '#fff') as SourceIconForeColor,
			    M.ObjectIntersectNodeID,
			    R.TargetTypeName,
			    R.TargetType,
			    R.TargetTypeID,
			    R.TargetObjectName,
			    R.TargetObject,
			    R.TargetObjectID,
				coalesce(TD.IconBackColor, '#000') as TargetIconBackColor,
				coalesce(TD.IconForeColor, '#fff') as TargetIconForeColor,
			    M.PredicateID,
			    P.Name as Predicate,
			    0
	    from	IntersectMap M
			    inner join [cache].[Relationships] R on M.SubjectIntersectNodeID = R.SourceIntersectNodeID and M.ObjectIntersectNodeID = R.TargetintersectNodeID and R.TargetObject = @type and R.TargetObjectID = @id and M.[Type] = 1
			    left join ObjectStyle SD with(nolock) on SD.ObjectType = R.SourceType and SD.ObjectID = R.[SourceTypeID]
				left join ObjectStyle TD with(nolock) on TD.ObjectType = R.TargetType and TD.ObjectID = R.[TargetTypeID]
			    inner join Predicate P on P.ID = M.PredicateID

    update	r
    set		r.mappingrulecount = l.[Count]
    from	@tbl r
			cross apply (
							select count(1) as [Count]
							from SourceTargetRule
							where FocalObjectID = @id and FocalObject = @type and SourceObject = r.Subject and SourceObjectID = r.SubjectID and TargetObject = r.Object and TargetObjectID = r.ObjectID
						) l;

    declare @h table	(
					    ID int, [Type] varchar(1), IsStart bit, IsEnd bit,
					    [Level] int, NodeID int, TypeName nvarchar(1000), [ObjectType] varchar(50), ObjectTypeID int, ObjectName nvarchar(1000), O varchar(50), OID int, BackColor varchar(10), ForeColor varchar(10),
					    IntersectID int, IntersectTypeID int,  PredicateID int, Predicate nvarchar(250),
					    RawSourceRuleCount int, RawMappingRuleCount int, LinkMappingRuleCount int, ChallengeCount int, OpenEventCount int, OpenIssueCount int
					    )

    insert into @h
	    select	ID, 'S', 0, 0, 0, 
				SubjectNodeID, 
				SubjectTypeName, SourceType, SourceTypeID, SubjectObjectName, 
				Subject, SubjectID, SubjectBackColor, SubjectForeColor, 
				IntersectID, IntersectTypeID, 
				PredicateID, Predicate, 
				R.[Count], M.[Count], S.MappingRuleCount, C.[Count], dbo.EventCountByObject(Subject, SubjectID, 'Open'), I.[Count]
	    from	@tbl S
			    cross apply (
						    select	count(1) as [Count]
						    from	SourceRule
						    where	AppliesToObject = @type and AppliesToObjectID = @id and Object = S.Subject and ObjectID = S.SubjectID
						    ) R
			    cross apply (
						        select count(1) as [Count]
						        from SourceTargetRule
						        where FocalObjectID = @id and FocalObject = @type and SourceObject = S.Subject and SourceObjectID = S.SubjectID and TargetObject = S.Subject and TargetObjectID = S.SubjectID
						    ) M
				cross apply (
								select count(1) as [Count]     
								from Workflow W            			                          
								where W.WorkflowType = 4 and W.Data.exist('/fields/ArtifactID[text() = sql:column("S.SubjectID")]') = 1 and W.DateCompleted is null   
							) C
				cross apply (
								select count(1) as [Count]     
								from Workflow W            			                          
								where W.WorkflowType = 3 and W.Data.exist('/fields/ArtifactID[text() = sql:column("S.SubjectID")]') = 1 and W.DateCompleted is null   
							) I

    insert into @h
        select	ID, 
				'O', 0, 0, 0, 
				ObjectNodeID, 
				ObjectTypeName, ObjectType, ObjectTypeID, ObjectObjectName, 
				Object, ObjectID, ObjectBackColor, ObjectForeColor, 
				IntersectID, IntersectTypeID, 
				PredicateID, Predicate, 
				R.[Count], M.[Count], S.MappingRuleCount, C.[Count], dbo.EventCountByObject(Object, ObjectID, 'Open'), I.[Count]
        from	@tbl S
                cross apply	(
                            select  count(1) as [Count]
                            from	SourceRule
                            where	AppliesToObject = @type 
									and AppliesToObjectID = @id 
									and Object = S.Object 
									and ObjectID = S.ObjectID
							) R
                cross apply	(
                            select	count(1) as [Count]
                            from	SourceTargetRule
                            where	FocalObjectID = @id 
									and FocalObject = @type 
									and SourceObject = S.Object 
									and SourceObjectID = S.ObjectID 
									and TargetObject = S.Object 
									and TargetObjectID = S.ObjectID
                            ) M
                cross apply	(
                            select	count(1) as [Count]
                            from	Workflow W
                            where	W.WorkflowType = 4 
									and W.Data.exist('/fields/ArtifactID[text() = sql:column("S.ObjectID")]') = 1 
									and W.DateCompleted is null
                            ) C
                cross apply	(
                            select	count(1) as [Count]
                            from	Workflow W
                            where	W.WorkflowType = 3 
									and W.Data.exist('/fields/ArtifactID[text() = sql:column("S.ObjectID")]') = 1 
									and W.DateCompleted is null
                            ) I

    update  T
    set     T.[Level] = 1,
		    T.IsStart = 1
    from	@h T
            left join @h S on S.O = T.O and S.OID = T.OID and S.[Type] = 'O'
    where	T.[Type] = 'S' and S.ID is null

    update T
    set		T.IsEnd = 1
    from	@h T
            left join @h S on S.O = T.O and S.OID = T.OID and S.[Type] = 'S'
    where	T.[Type] = 'O' and S.ID is null

    select	*
	from	@h
end
go