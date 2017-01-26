DROP TABLE [dbo].[DomainItemXref]
GO

DROP TABLE [dbo].[DomainSourceType]
GO

DROP TABLE [dbo].[DomainItem]
GO

DROP TABLE [dbo].[DomainGroup]
GO

--DROP VIEW [dbo].[ObjectCache]

DROP TABLE [dbo].[Domain]
GO

DROP TABLE [dbo].[DomainType]
GO

DROP TABLE [dbo].[DomainClassification] 
GO

DROP TABLE [dbo].[FieldTypeRelationLookupDisplayField]
GO

DROP TABLE [dbo].[FieldTypeRelationLookupDefinition]
GO

DROP TABLE [dbo].[IntersectTypePredicate]
GO

DROP TABLE [dbo].[RelatedArtifact]
GO

DROP TABLE [dbo].[ResolutionRelation]
GO

DROP TABLE [dbo].[Resolution]
GO

ENABLE TRIGGER [dbo].[Artifact_AfterDelete] ON [dbo].[Artifact];
GO

DROP INDEX [IX_Intersect_IntersectTypeID_Subject_Object] ON [dbo].[Intersect]
GO

ALTER TABLE MapItem ALTER COLUMN [TargetIntersectID] INT NULL
GO

ALTER TABLE [Rule] ALTER COLUMN [Threshold] DECIMAL (4, 3) NULL
GO

ALTER TABLE [fusion].[RuleItem] ALTER COLUMN [RuleID] INT NULL
GO

alter view [cache].[ObjectDetails]
as
	select	D.[Object],
			D.[ObjectID],
			coalesce(O1.Name, O2.Name, O4.Name, O5.Name, O6.Name, O7.Name, O8.Name, O9.Name, O10.Name, O11.Name, O12.Name, O13.Name, case when O14.ResourceID is not null then O14.FirstName + ' ' + O14.LastName else null end, O15.Name, O16.Name, O17.Name, O18.Name, O21.Name, O22.Name, O23.Name, O24.Name, O25.DisplayValue, O26.Name, O27.Name, null) as Name,
			coalesce(O1.TextPath, O2.TextPath, O4.TextPath, O5.Name, O6.Name, O7.Name, O8.Name, O9.Name, O10.Name, O11.Name, O12.Name, O13.TextPath, case when O14.ResourceID is not null then O14.FirstName + ' ' + O14.LastName else null end, O15.Name, O16.Name, O17.TextPath, O18.Name, O21.Name, O22.Name, O23.Name, O24.Name, O25.DisplayValue, O26.Name, O27.Name, '') as TextPath,
			coalesce(O1.Description, O2.Description, O6.Description, O7.Description, O8.Description, O9.Description, O10.Description, O12.Description, O13.Description, O26.Description, NULL) as Description,
			case D.[Object]
				when 'Lookup' then dbo.GenerateNgObjectUrl('Lookup', O20.LookupTypeID, O20.ID)
				when 'LookupType' then dbo.GenerateNgObjectUrl('LookupType', O21.ID, 0)
				when 'ReferenceItem' then dbo.GenerateNgObjectUrl('ReferenceItem', O25.ReferenceItemTypeID, O25.ID)
				when 'ReferenceItemType' then dbo.GenerateNgObjectUrl('ReferenceItemType', O26.ID, 0)
				else dbo.GenerateNgObjectUrl(D.[Object], D.[ObjectTypeID], D.ObjectID) 
			end as Url,
			case 
				when P1.ID is not null then 'Artifact'
				when P2.ID is not null then 'Taxonomy'
				when P4.ID is not null then 'FusionAttribute'
				when P4.ID is not null then 'FusionAttribute'
				when P7.ID is not null then 'ArtifactType'
				when P10.ID is not null then 'AttributeType'
				when P13.ID is not null then 'PolicyType'
				when P17.ID is not null then 'FusionAttributeType'
				else NULL
			end as Parent,
			coalesce(O1.ParentID, O2.ParentID, O4.ParentID, O7.ParentID, O10.ParentID, O13.ParentID, O17.ParentID, NULL) as ParentID,
			coalesce(P1.Name, P2.Name, P4.Name, P7.Name, P10.Name, P13.Name, P17.Name, NULL) as ParentName,
			D.[ObjectType],
			D.ObjectTypeID,
			coalesce(OT1.Name, OT2.Name, OT4.TextPath, OT5.Name, OT12.Name, OT13.Name, OT14.Name, OT15.Name, OT20.Name, OT24.Name, NULL) as ObjectTypeName,
			coalesce(S.IconBackColor, '#000') as IconBackColor,
			coalesce(S.IconForeColor, '#fff') as IconForeColor,
			coalesce(S.IconText, 'leaf') as IconText,
			case D.[Object]
				when 'Lookup' then dbo.GenerateNgObjectUrl('Lookup', O20.LookupTypeID, O20.ID)
				when 'LookupType' then dbo.GenerateNgObjectUrl('LookupType', O21.ID, 0)
				when 'ReferenceItem' then dbo.GenerateNgObjectUrl('ReferenceItem', O25.ReferenceItemTypeID, O25.ID)
				when 'ReferenceItemType' then dbo.GenerateNgObjectUrl('ReferenceItemType', O26.ID, 0)
				else dbo.GenerateNgObjectUrl(D.[Object], D.[ObjectTypeID], D.ObjectID) 
			end as NgUrl
	from	cache.[Object] D with(nolock)
			left join Artifact O1 with(nolock) on D.[Object] = 'Artifact' and O1.ID = D.ObjectID
			left join ArtifactType OT1 with(nolock) on D.[Object] = 'Artifact' and OT1.ID = O1.ArtifactTypeID
			left join Artifact P1 with(nolock) on D.[Object] = 'Artifact' and P1.ID = O1.ParentID

			left join Taxonomy O2 with(nolock) on D.[Object] = 'Taxonomy' and O2.ID = D.ObjectID
			left join TaxonomyType OT2 with(nolock) on D.[Object] = 'Taxonomy' and OT2.ID = O2.TaxonomyTypeID
			left join Taxonomy P2 with(nolock) on D.[Object] = 'Taxonomy' and P2.ID = O2.ParentID

			left join FusionAttribute O4 with(nolock) on D.[Object] = 'FusionAttribute' and O4.ID = D.ObjectID
			left join FusionAttributeType OT4 with(nolock) on D.[Object] = 'FusionAttribute' and OT4.ID = O4.FusionAttributeTypeID
			left join FusionAttribute P4 with(nolock) on D.[Object] = 'FusionAttribute' and P4.ID = O4.ParentID

			left join Fusion O5 with(nolock) on D.[Object] = 'Fusion' and O5.ID = D.ObjectID
			left join FusionType OT5 with(nolock) on D.[Object] = 'Fusion' and OT5.ID = O5.FusionTypeID

			left join FusionType O6 with(nolock) on D.[Object] = 'FusionType' and O6.ID = D.ObjectID

			left join ArtifactType O7 with(nolock) on D.[Object] = 'ArtifactType' and O7.ID = D.ObjectID
			left join ArtifactType P7 with(nolock) on D.[Object] = 'ArtifactType' and P7.ID = O7.ParentID

			left join TaxonomyType O8 with(nolock) on D.[Object] = 'TaxonomyType' and O8.ID = D.ObjectID

			left join ResponsibilityType O9 with(nolock) on D.[Object] = 'ResponsibilityType' and O9.ID = D.ObjectID

			left join AttributeType O10 with(nolock) on D.[Object] = 'AttributeType' and O10.ID = D.ObjectID
			left join AttributeType P10 with(nolock) on D.[Object] = 'AttributeType' and P10.ID = O10.ParentID

			left join IntersectType O11 with(nolock) on D.[Object] = 'IntersectType' and O11.ID = D.ObjectID

			left join [Rule] O12 with(nolock) on D.[Object] = 'Rule' and O12.ID = D.ObjectID
			left join	(
						select 1 as ID, 'Informational Rule' as Name
						union
						select 2 as ID, 'Quality Check Rule' as Name
						union
						select 3 as ID, 'Metric Rule' as Name
						union
						select 4 as ID, 'Profile Rule' as Name
						) OT12 on D.[Object] = 'Rule' and OT12.ID = O12.RuleType

			left join [Policy] O13 with(nolock) on D.[Object] = 'Policy' and O13.ID = D.ObjectID
			left join PolicyType OT13 with(nolock) on D.[Object] = 'Policy' and OT13.ID = O13.PolicyTypeID
			left join [Policy] P13 with(nolock) on D.[Object] = 'Policy' and P13.ID = O13.ParentID

			left join reporting.Global_Resource O14 with(nolock) on D.[Object] = 'Resource' and O14.ResourceID = D.ObjectID --and O14.Status = 'Active'
			left join (select 1 as ID, 'User' as Name) OT14 on D.[Object] = 'Resource' and OT14.ID = D.ObjectTypeID

			left join [Group] O15 with(nolock) on D.[Object] = 'Group' and O15.ID = D.ObjectID
			left join (
						select 0 as ID, 'Group' as Name
						union
						select 1 as ID, 'Group' as Name
					  ) OT15 on D.[Object] = 'Group' and OT15.ID = D.ObjectTypeID

			left join PolicyType O16 with(nolock) on D.[Object] = 'PolicyType' and O16.ID = D.ObjectID

			left join FusionAttributeType O17 with(nolock) on D.[Object] = 'FusionAttributeType' and O17.ID = D.ObjectID
			left join FusionAttributeType P17 with(nolock) on D.[Object] = 'FusionAttributeType' and P17.ID = O17.ParentID

			left join	(
						select 1 as ID, 'Informational Rule' as Name
						union
						select 2 as ID, 'Quality Check Rule' as Name
						union
						select 3 as ID, 'Metric Rule' as Name
						union
						select 4 as ID, 'Profile Rule' as Name
						) O18 on D.[Object] = 'RuleType' and O18.ID = D.ObjectID

			left join [Lookup] O20 with(nolock) on D.[Object] = 'Lookup' and O20.ID = D.ObjectID
			left join LookupType OT20 with(nolock) on D.[Object] = 'Lookup' and OT20.ID = O20.LookupTypeID

			left join [LookupType] O21 with(nolock) on D.[Object] = 'LookupType' and O21.ID = D.ObjectID

			left join	(
						select 0 as ID, 'User' as Name
						union
						select 1 as ID, 'User' as Name
						) O22 on D.[Object] = 'ResourceType' and O22.ID = D.ObjectID

			left join	(
						select 0 as ID, 'Group' as Name
						union
						select 1 as ID, 'Group' as Name
						) O23 on D.[Object] = 'GroupType' and O22.ID = D.ObjectID

			left join [Intersect] O24 with(nolock) on D.[Object] = 'Intersect' and O24.ID = D.ObjectID
			left join IntersectType OT24 with(nolock) on D.[Object] = 'Intersect' and OT24.ID = O24.IntersectTypeID

			left join ReferenceItem O25 with(nolock) on D.[Object] = 'ReferenceItem' and O25.ID = D.ObjectID
			left join ReferenceItemType OT25 with(nolock) on D.[Object] = 'ReferenceItem' and OT25.ID = O25.ReferenceItemTypeID

			left join ReferenceItemType O26 with(nolock) on D.[Object] = 'ReferenceItemType' and O26.ID = D.ObjectID

			left join FusionQueryAttributeType O27 with(nolock) on D.[Object] = 'FusionQueryAttributeType' and O27.ID = D.ObjectID

			left join ObjectStyle S with(nolock) on S.ObjectType = D.ObjectType and S.ObjectID = D.[ObjectTypeID]
GO

alter view [cache].[Responsibilities]
as
	SELECT R.[ResponsibilityID]
		  ,R.[ResponsibilityTypeID]
		  ,RT.Name as [ResponsibilityType]
		  ,R.[AssigningItem]
		  ,R.[AssigningItemID]
		  ,A.TextPath as [AssigningItemName]
		  ,A.Url as [AssigningItemUrl]
		  ,A.ObjectType as [AssigningItemType]
		  ,A.ObjectTypeID as [AssigningItemTypeID]
		  ,A.ObjectTypeName as [AssigningTypeName]
		  ,R.[Object]
		  ,R.[ObjectID]
		  ,O.TextPath as [ObjectName]
		  ,O.[ObjectType]
		  ,O.[ObjectTypeID]
		  ,O.[ObjectTypeName]
		  ,O.Url as [ObjectUrl]
		  ,R.[ResponsibleObject]
		  ,R.[ResponsibleObjectID]
		  ,RO.Name as [ResponsibleObjectName]
		  ,RO.Url as [ResponsibleObjectUrl]
		  ,R.[ContextHash]
		  ,R.[ResponsibilityTypeGroup]
		  ,R.[Visible]
		  ,R.[TargetResponsibilityID]
	FROM	[cache].ResponsibilityItem R
			inner join ResponsibilityType RT on RT.ID = R.[ResponsibilityTypeID]
			inner join cache.ObjectDetails A on A.[Object] = R.[AssigningItem] and A.[ObjectID] = R.[AssigningItemID]
			inner join cache.ObjectDetails O on O.[Object] = R.[Object] and O.[ObjectID] = R.[ObjectID]
			inner join cache.ObjectDetails RO on RO.[Object] = R.[ResponsibleObject] and RO.[ObjectID] = R.[ResponsibleObjectID]
GO

ALTER VIEW [dbo].[FieldLookupValue]
AS
	SELECT	T.ID as FieldTypeID,
			T.LookupObjectType,
			T.LookupObjectID,
			COALESCE(A.ID, R.ResourceID, L.ID, RI.ID, RIT.ID) as Value,	
			utility.GetFormattedFieldLookupValue(T.Type, T.LookupDisplayFormat, T.LookupObjectType, T.LookupObjectID, COALESCE(A.ID, R.ResourceID, L.ID, RI.ID, RIT.ID)) as Text
	FROM	FieldType T 
			LEFT JOIN Artifact A ON T.LookupObjectType = 'Artifact' AND T.LookupObjectID = A.ArtifactTypeID
			LEFT JOIN reporting.Global_Resource R ON T.LookupObjectType = 'Resource' --AND T.LookupObjectID = R.ResourceTypeID
			LEFT JOIN Lookup L ON T.LookupObjectType = 'Lookup' AND T.LookupObjectID = L.LookupTypeID
			LEFT JOIN ReferenceItem RI ON T.LookupObjectType = 'ReferenceItem' AND T.LookupObjectID = RI.ReferenceItemTypeID
			LEFT JOIN ReferenceItemType RIT ON T.LookupObjectType = 'ReferenceItemType' --AND T.LookupObjectID = RIT.ID
	WHERE	T.LookupObjectType is not null
			AND COALESCE(A.ID, R.ResourceID, L.ID, RI.ID, RIT.ID) IS NOT NULL
GO

ALTER VIEW [dbo].[FieldTypeLookupValue]
AS
	SELECT	'Artifact' as LookupObjectType,
			ID as LookupObjectID,
			Name--'Artifact : ' + Name as Name
	FROM	ArtifactType
	UNION
	SELECT	'ReferenceItemType' as LookupObjectType,
			ID as LookupObjectID,
			Name --'Domain : ' + Name as Name
	FROM	ReferenceItemType
	UNION
	SELECT	'Taxonomy' as LookupObjectType,
			ID as LookupObjectID,
			Name --'Information Model : ' + Name as Name
	FROM	TaxonomyType
	UNION
	SELECT	'Lookup' as LookupObjectType,
			ID as LookupObjectID,
			Name --'Lookup : ' + Name as Name
	FROM	LookupType
GO

ALTER VIEW [dbo].[FieldWithRelation]
AS
	SELECT	F.FieldTypeID,
			T.Name,
			T.FriendlyName,
			T.Category,
			T.Description,
			T.DisplayDescription,
			T.FormDescription,
			T.ValidationDescription,
			T.Type,
			T.LookupObjectType,
			T.LookupObjectID,
			T.LookupDisplayFormat,
			T.MinimumLength,
			T.MaximumLength,
			T.Length,
			T.Pattern,
			T.IsListable,
			T.IsRequired,
			T.SortOrder,
			F.ObjectType,
			F.ObjectID,
			F.Value,
			F.FormattedValue,
			LD.Url as LookupUrl
	FROM	FieldType T
			inner join Field F on F.FieldTypeID = T.ID and ( 
															(F.ObjectType + 'Type' = T.[Object] and F.ObjectType <> 'Event') OR 
															(T.[Object] = 'Rule' and F.ObjectType = 'Event') 
														   )
			left join cache.ObjectDetails LD on 
				LD.[Object] = case T.LookupObjectType
									when 'ReferenceItem' then 'ReferenceItemType' 
									else T.LookupObjectType 
							  end
				and LD.ObjectID = case 
									when T.LookupObjectType = 'ReferenceItem' then T.LookupObjectID 
									when T.LookupObjectType = 'Resource' then T.LookupObjectID 
									when T.LookupObjectType is null then NULL 
									when dbo.IsInteger(F.Value) = 1 then F.Value
								end
GO

ALTER VIEW [dbo].[FollowDetail]
AS
	with ArtifactTypes as
	(
	select	ID as FollowID,
			ObjectType as [Object],
			ObjectID,
			ResourceID,
			1 as HardFollow
	from	Follow
	where	ObjectType = 'ArtifactType' and FollowTypeID = 3
	union all
	select	P.ID as FollowID,
			cast('Artifact' as varchar(50)) as [Object],
			C.ID as ObjectID,
			P.ResourceID,
			0 as HardFollow
	from	Artifact C
			inner join Follow P on P.ObjectType = 'ArtifactType' and P.ObjectID = C.ArtifactTypeID and P.FollowTypeID = 3
	),
	DomainTypes as
	(
	select	ID as FollowID,
			ObjectType as [Object],
			ObjectID,
			ResourceID,
			1 as HardFollow
	from	Follow
	where	ObjectType = 'ReferenceItemType' and FollowTypeID = 3
	),
	Groups as
	(
	select	ID as FollowID,
			ObjectType as [Object],
			ObjectID,
			ResourceID,
			1 as HardFollow
	from	Follow
	where	ObjectType = 'Group' and ObjectID = 0 and FollowTypeID = 3
	union all
	select	P.ID as FollowID,
			P.ObjectType as [Object],
			C.ID as ObjectID,
			P.ResourceID,
			0 as HardFollow
	from	[Group] C
			inner join Follow P on P.ObjectType = 'Group' and P.ObjectID = 0 and P.FollowTypeID = 3
	),
	PolicyTypes as
	(
	select	ID as FollowID,
			ObjectType as [Object],
			ObjectID,
			ResourceID,
			1 as HardFollow
	from	Follow
	where	ObjectType = 'PolicyType' and FollowTypeID = 3
	union all
	select	P.ID as FollowID,
			cast('Policy' as varchar(50)) as [Object],
			C.ID as ObjectID,
			P.ResourceID,
			0 as HardFollow
	from	Policy C
			inner join Follow P on P.ObjectType = 'PolicyType' and P.ObjectID = C.PolicyTypeID and P.FollowTypeID = 3
	),
	PolicyParents as
	(
	select	F.ID as FollowID,
			T.ID,
			T.ParentID,
			F.ResourceID,
			1 as HardFollow
	from	Policy T
			inner join Follow F on F.ObjectType = 'Policy' and F.ObjectID = T.ID and F.FollowTypeID = 3
	union all
	select	P.FollowID,
			C.ID,
			C.ParentID,
			P.ResourceID,
			0 as HardFollow
	from	Policy C
			inner join PolicyParents P on P.ID = C.ParentID
	),
	Resources as
	(
	select	ID as FollowID,
			ObjectType as [Object],
			ObjectID,
			ResourceID,
			1 as HardFollow
	from	Follow
	where	ObjectType = 'ResourceType' and FollowTypeID = 3
	union all
	select	P.ID as FollowID,
			cast('Resource' as varchar(50)) as [Object],
			C.ResourceID as ObjectID,
			P.ResourceID,
			0 as HardFollow
	from	reporting.Global_Resource C
			inner join Follow P on P.ObjectType = 'ResourceType' and P.FollowTypeID = 3
	where	C.ResourceID > 0
	),
	TaxonomyParents as
	(
	select	F.ID as FollowID,
			T.ID,
			T.ParentID,
			F.ResourceID,
			1 as HardFollow
	from	Taxonomy T
			inner join Follow F on F.ObjectType = 'Taxonomy' and F.ObjectID = T.ID and F.FollowTypeID = 3
	union all
	select	P.FollowID,
			C.ID,
			C.ParentID,
			P.ResourceID,
			0 as HardFollow
	from	Taxonomy C
			inner join TaxonomyParents P on P.ID = C.ParentID
	)

	SELECT		F.FollowID,
				F.ResourceID,
				R.Email,
				R.Email as FollowerEmail,
				R.FirstName + ' ' + R.LastName as FollowerName,
				R.FirstName as FollowerFirstName,
				R.LastName as FollowerLastName,
				'Resource' as FollowerObjectType,
				F.ResourceID as FollowerObjectID,
				dbo.GenerateObjectUrl('Resource', 1, F.ResourceID) as FollowerUrl,
				F.ObjectID,
				F.[Object] as ObjectType,
				O.ObjectID as ID,
				O.Name,
				O.TextPath,
				O.Description,
				O.ParentID,
				O.Parent as ParentType,
				O.Url,
				O.ObjectTypeID as TypeID,
				O.ObjectType as [Type],
				case O.ObjectType
					when 'ResourceType' then 'User'
					when 'Group' then 'Group'
					else O.ObjectTypeName
				end as [TypeName],
				O.IconBackColor,
				O.IconForeColor,
				O.IconText,
				0 AS OpenEventCount,
				dbo.GetObjectStatisticScore(F.[Object], F.ObjectID) as CurrentScore,
				cast(F.HardFollow as bit) as HardFollow
	FROM		(
				select	FollowID,
						[Object], 
						ObjectID, 
						ResourceID, 
						HardFollow 
				from	ArtifactTypes
				union
				select	FollowID,
						[Object], 
						ObjectID, 
						ResourceID, 
						HardFollow 
				from	DomainTypes
				union
				select	FollowID,
						[Object], 
						ObjectID, 
						ResourceID, 
						HardFollow 
				from	Groups
				union
				select	FollowID,
						[Object], 
						ObjectID, 
						ResourceID, 
						HardFollow 
				from	PolicyTypes
				union
				select	FollowID,
						'Policy', 
						ID as ObjectID, 
						ResourceID, 
						HardFollow 
				from	PolicyParents
				union
				select	FollowID,
						[Object], 
						ObjectID, 
						ResourceID, 
						HardFollow 
				from	Resources
				union
				select	FollowID,
						'Taxonomy' as [Object], 
						ID as ObjectID, 
						ResourceID, 
						HardFollow 
				from	TaxonomyParents
				union
				select	ID as FollowID,
						ObjectType as [Object], 
						ObjectID, 
						ResourceID, 
						1 as HardFollow 
				from	Follow
				where	FollowTypeID = 1	
				) F
				inner join reporting.Global_Resource R on R.ResourceID = F.ResourceID
				inner join cache.ObjectDetails O on O.[Object] = F.[Object] and O.ObjectID = F.ObjectID
GO

ALTER view [dbo].[IntersectDetail]
as
	select	I.ID,
			I.IntersectTypeID,
			case I.Classification
				when 0 then 2
				else coalesce(I.Classification, 2)
			end as Classification,
			I.Description,

			I.Subject,
			I.SubjectID,
			case I.Subject
				when 'Intersect' then utility.DeriveIntersectName(SI.ID)
				when 'Resource' then SRE.FirstName + ' ' + SRE.LastName
				else coalesce(SA.TextPath, SD.Name, SF.TextPath, SG.Name, SP.TextPath, SR.Name, ST.TextPath) 
			end as SubjectName,
			dbo.GenerateNgObjectUrl(
				I.Subject, 
				case I.Subject
					when 'Resource' then 1
					when 'Group' then 1
					when 'ReferenceItemType' then 0
					else coalesce(SA.ArtifactTypeID, SF.FusionAttributeTypeID, SI.IntersectTypeID, SP.PolicyTypeID, SR.RuleType, ST.TaxonomyTypeID) 
				end,
				I.SubjectID) as SubjectUrl,
			case I.Subject
				when 'Group' then 'GroupType'
				when 'Resource' then 'ResourceType'
				else I.Subject + 'Type'
			end as SubjectType,
			case I.Subject
				when 'Resource' then 1
				when 'Group' then 1
				when 'ReferenceItemType' then 0
				else coalesce(SA.ArtifactTypeID, SF.FusionAttributeTypeID, SI.IntersectTypeID, SP.PolicyTypeID, SR.RuleType, ST.TaxonomyTypeID) 
			end as SubjectTypeID,
			case 
				when I.Subject = 'ReferenceItemType' then 'Reference List'
				when I.Subject = 'Rule' and SR.RuleType = 1 then 'Informational Rule'
				when I.Subject = 'Rule' and SR.RuleType = 2 then 'Quality Check Rule'
				when I.Subject = 'Rule' and SR.RuleType = 3 then 'Metric Rule'
				when I.Subject = 'Rule' and SR.RuleType = 4 then 'Profile Rule'
				when I.Subject = 'Intersect' then utility.DeriveIntersectTypeName(SI.IntersectTypeID)
				else coalesce(SAT.Name, SFT.TextPath, SPT.Name, STT.Name) 
			end as SubjectTypeName,
			coalesce(SIcon.IconBackColor, '#000') as SubjectIconBackColor,
			coalesce(SIcon.IconForeColor, '#fff') as SubjectIconForeColor,
			coalesce(SIcon.IconText, substring(coalesce(SAT.Name, SD.Name, SFT.TextPath, SPT.Name, STT.Name, ''), 1, 2)) as SubjectIconText,

			I.Object,
			I.ObjectID,
			case I.Object
				when 'Intersect' then utility.DeriveIntersectName(OI.ID)
				when 'Resource' then ORE.FirstName + ' ' + ORE.LastName
				else coalesce(OA.TextPath, OD.Name, [OF].TextPath, OG.Name, OP.TextPath, [OR].Name, OT.TextPath)
			end as ObjectName,
			dbo.GenerateNgObjectUrl(
				I.Object, 
				case I.Object
					when 'Resource' then 1
					when 'Group' then 1
					when 'ReferenceItemType' then 0
					else coalesce(OA.ArtifactTypeID, OD.ID, [OF].FusionAttributeTypeID, OI.IntersectTypeID, OP.PolicyTypeID, [OR].RuleType, OT.TaxonomyTypeID)
				end,
				I.ObjectID) as ObjectUrl,
			case I.Object
				when 'Artifact' then 'ArtifactType'
				when 'FusionAttribute' then 'FusionAttributeType'
				when 'Intersect' then 'IntersectType'
				when 'Policy' then 'PolicyType'
				when 'Rule' then 'RuleType'
				when 'Taxonomy' then 'TaxonomyType'
				else I.Object
			end as ObjectType,
			case I.Object
				when 'Resource' then 1
				when 'Group' then 1
				when 'ReferenceItemType' then 0
				else coalesce(OA.ArtifactTypeID, OD.ID, [OF].FusionAttributeTypeID, OI.IntersectTypeID, OP.PolicyTypeID, [OR].RuleType, OT.TaxonomyTypeID)
			end as ObjectTypeID,
			case
				when I.Object = 'ReferenceItemType' then 'Reference List'
				when I.Object = 'Rule' and [OR].RuleType = 1 then 'Informational Rule'
				when I.Object = 'Rule' and [OR].RuleType = 2 then 'Quality Check Rule'
				when I.Object = 'Rule' and [OR].RuleType = 3 then 'Metric Rule'
				when I.Object = 'Rule' and [OR].RuleType = 4 then 'Profile Rule'
				when I.Object = 'Intersect' then utility.DeriveIntersectTypeName(OI.IntersectTypeID)
				else coalesce(OAT.Name, OD.Name, OFT.TextPath, OPT.Name, OTT.Name) 
			end as ObjectTypeName,
			coalesce(OIcon.IconBackColor, '#000') as ObjectIconBackColor,
			coalesce(OIcon.IconForeColor, '#fff') as ObjectIconForeColor,
			--coalesce(OIcon.IconText, 'leaf') as ObjectIconText,
			coalesce(OIcon.IconText, substring(coalesce(OAT.Name, OD.Name, OFT.TextPath, OPT.Name, OTT.Name, ''), 1, 2)) as ObjectIconText,

			IT.PredicateID,
			P.Name as [PredicateName],
			P.Type as PredicateType
	from	dbo.[Intersect] I with(nolock)
			inner join dbo.[IntersectType] IT with(nolock) on IT.ID = I.IntersectTypeID
			left join [Predicate] P with(nolock) on P.ID = IT.PredicateID 
			left join dbo.Artifact SA with(nolock) on I.Subject = 'Artifact' and SA.ID = I.SubjectID
			left join dbo.ArtifactType SAT with(nolock) on SAT.ID = SA.ArtifactTypeID
			left join dbo.ReferenceItemType SD with(nolock) on I.Subject = 'ReferenceItemType' and SD.ID = I.SubjectID
			left join dbo.FusionAttribute SF with(nolock) on I.Subject = 'FusionAttribute' and SF.ID = I.SubjectID
			left join dbo.FusionAttributeType SFT with(nolock) on SFT.ID = SF.FusionAttributeTypeID
			left join dbo.[Group] SG with(nolock) on I.Subject = 'Group' and SG.ID = I.SubjectID
			left join dbo.[Intersect] SI with(nolock) on I.Subject = 'Intersect' and SI.ID = I.SubjectID
			--left join dbo.[IntersectType] SIT with(nolock) on SIT.ID = SI.IntersectTypeID
			left join dbo.[Policy] SP with(nolock) on I.Subject = 'Policy' and SP.ID = I.SubjectID
			left join dbo.PolicyType SPT with(nolock) on SPT.ID = SP.PolicyTypeID
			left join reporting.Global_Resource SRE with(nolock) on I.Subject = 'Resource' and SRE.ResourceID = I.SubjectID
			left join dbo.[Rule] SR with(nolock) on I.Subject = 'Rule' and SR.ID = I.SubjectID
			left join dbo.Taxonomy ST with(nolock) on I.Subject = 'Taxonomy' and ST.ID = I.SubjectID
			left join dbo.TaxonomyType STT with(nolock) on STT.ID = ST.TaxonomyTypeID

			left join dbo.Artifact OA with(nolock) on I.Object = 'Artifact' and OA.ID = I.ObjectID
			left join dbo.ArtifactType OAT with(nolock) on OAT.ID = OA.ArtifactTypeID
			left join dbo.ReferenceItemType OD with(nolock) on I.Object = 'ReferenceItemType' and OD.ID = I.ObjectID
			left join dbo.FusionAttribute [OF] with(nolock) on I.Object = 'FusionAttribute' and [OF].ID = I.ObjectID
			left join dbo.FusionAttributeType OFT with(nolock) on OFT.ID = [OF].FusionAttributeTypeID
			left join dbo.[Group] OG with(nolock) on I.Object = 'Group' and OG.ID = I.SubjectID
			left join dbo.[Intersect] OI with(nolock) on I.Subject = 'Intersect' and OI.ID = I.SubjectID
			--left join dbo.[IntersectType] OIT with(nolock) on OIT.ID = OI.IntersectTypeID
			left join dbo.[Policy] OP with(nolock) on I.Object = 'Policy' and OP.ID = I.ObjectID
			left join dbo.PolicyType OPT with(nolock) on OPT.ID = OP.PolicyTypeID
			left join reporting.Global_Resource ORE with(nolock) on I.Object = 'Resource' and ORE.ResourceID = I.ObjectID
			left join dbo.[Rule] [OR] with(nolock) on I.Object = 'Rule' and [OR].ID = I.ObjectID
			left join dbo.Taxonomy OT with(nolock) on I.Object = 'Taxonomy' and OT.ID = I.ObjectID
			left join dbo.TaxonomyType OTT with(nolock) on OTT.ID = OT.TaxonomyTypeID

			left join ObjectStyle SIcon with(nolock) on SIcon.ObjectType =	case I.Subject
																				when 'Group' then 'GroupType'
																				when 'Resource' then 'ResourceType'
																				else I.Subject + 'Type'
																			end 
														and SIcon.ObjectID =	case I.Subject
																					when 'Resource' then 1
																					when 'Group' then 1
																					else coalesce(SA.ArtifactTypeID, SD.ID, SF.FusionAttributeTypeID, SI.IntersectTypeID, SP.PolicyTypeID, SR.RuleType, ST.TaxonomyTypeID) 
																				end
			left join ObjectStyle OIcon with(nolock) on OIcon.ObjectType =	case I.Object
																				when 'Group' then 'GroupType'
																				when 'Resource' then 'ResourceType'
																				else I.Object + 'Type'
																			end 
														and OIcon.ObjectID =	case I.Object
																					when 'Resource' then 1
																					when 'Group' then 1
																					else coalesce(OA.ArtifactTypeID, OD.ID, [OF].FusionAttributeTypeID, OI.IntersectTypeID, OP.PolicyTypeID, [OR].RuleType, OT.TaxonomyTypeID) 
																				end

	where	coalesce(SA.ID, SD.ID, SF.ID, SG.ID, SI.ID, SP.ID, SR.ID, SRE.ResourceID, ST.ID) is not null
			and coalesce(OA.ID, OD.ID, [OF].ID, OG.ID, OI.ID, OP.ID, [OR].ID, ORE.ResourceID, OT.ID) is not null
GO

alter view [dbo].[IntersectTypeDetail]
as
	select	IT.ID,
			IT.Subject,
			IT.SubjectID,
			case IT.Subject
				when 'IntersectType' then utility.DeriveIntersectTypeName(SIT.ID)
				when 'GroupType' then 'Group'
				when 'ResourceType' then 'Resource'
				else coalesce(SAT.Name, SDT.Name, SFT.TextPath, SPT.Name, SRT.Name, STT.Name) 
			end as SubjectName,
			coalesce(SIcon.IconBackColor, '#000') as SubjectIconBackColor,
			coalesce(SIcon.IconForeColor, '#fff') as SubjectIconForeColor,
			coalesce(SIcon.IconText, substring(coalesce(SAT.Name, SDT.Name, SFT.Name, SPT.Name, SRT.Name, STT.Name, ''), 1, 2)) as SubjectIconText,
			
			IT.Object,
			IT.ObjectID,
			case IT.Object
				when 'IntersectType' then utility.DeriveIntersectTypeName(OIT.ID)
				when 'GroupType' then 'Group'
				when 'ResourceType' then 'Resource'
				else coalesce(OAT.Name, ODT.Name, OFT.TextPath, OPT.Name, ORT.Name, OTT.Name) 
			end as ObjectName,
			coalesce(OIcon.IconBackColor, '#000') as ObjectIconBackColor,
			coalesce(OIcon.IconForeColor, '#fff') as ObjectIconForeColor,
			--coalesce(OIcon.IconText, 'leaf') as ObjectIconText,
			coalesce(OIcon.IconText, substring(coalesce(OAT.Name, ODT.Name, OFT.Name, OPT.Name, ORT.Name, OTT.Name, ''), 1, 2)) as ObjectIconText,

			IT.PredicateID,
			P.Name as [PredicateName],
			P.Type as PredicateType,
			
			coalesce(IT.IsSystem, cast(0 as bit)) as IsSystem
	from	IntersectType IT with(nolock) 
			left join [Predicate] P with(nolock) on P.ID = IT.PredicateID 

			left join dbo.ArtifactType SAT with(nolock)			on IT.Subject = 'ArtifactType'			and SAT.ID = IT.SubjectID
			left join dbo.ReferenceItemType SDT with(nolock)	on IT.Subject = 'ReferenceItemType'		and IT.SubjectID = 0
			left join dbo.FusionAttributeType SFT with(nolock)	on IT.Subject = 'FusionAttributeType'	and SFT.ID = IT.SubjectID
			left join dbo.IntersectType SIT with(nolock)		on IT.Subject = 'IntersectType'			and SIT.ID = IT.SubjectID
			left join dbo.PolicyType SPT with(nolock)			on IT.Subject = 'PolicyType'			and SPT.ID = IT.SubjectID
			left join (
				select 1 as ID, 'Informational' as Name
				union
				select 2 as ID, 'Quality Check' as Name
				union
				select 3 as ID, 'Metric' as Name
				union
				select 4 as ID, 'Profile' as Name
			) SRT												on IT.Subject = 'RuleType'				and SRT.ID = IT.SubjectID 
			left join dbo.TaxonomyType STT with(nolock)			on IT.Subject = 'TaxonomyType'			and STT.ID = IT.SubjectID


			left join dbo.ArtifactType OAT with(nolock)			on IT.Object = 'ArtifactType'			and OAT.ID = IT.ObjectID
			left join dbo.ReferenceItemType ODT with(nolock)	on IT.Object = 'ReferenceItemType'		and IT.ObjectID = 0
			left join dbo.FusionAttributeType OFT with(nolock)	on IT.Object = 'FusionAttributeType'	and OFT.ID = IT.ObjectID
			left join dbo.IntersectType OIT with(nolock)		on IT.Object = 'IntersectType'			and OIT.ID = IT.ObjectID
			left join dbo.PolicyType OPT with(nolock)			on IT.Object = 'PolicyType'				and OPT.ID = IT.ObjectID
			left join (
				select 1 as ID, 'Informational' as Name
				union
				select 2 as ID, 'Quality Check' as Name
				union
				select 3 as ID, 'Metric' as Name
				union
				select 4 as ID, 'Profile' as Name
			) ORT												on IT.Object = 'RuleType'				and ORT.ID = IT.ObjectID
			left join dbo.TaxonomyType OTT with(nolock)			on IT.Object = 'TaxonomyType'			and OTT.ID = IT.ObjectID

			left join ObjectStyle SIcon with(nolock) on SIcon.ObjectType = IT.Subject and SIcon.ObjectID =	IT.SubjectID
			left join ObjectStyle OIcon with(nolock) on OIcon.ObjectType = IT.Object and OIcon.ObjectID = IT.ObjectID
	where	coalesce(SAT.ID, SDT.ID, SIT.ID, SFT.ID, SPT.ID, SRT.ID, STT.ID) is not null
			and coalesce(OAT.ID, ODT.ID, [OFT].ID, OPT.ID, ORT.ID, OTT.ID) is not null
GO

alter view [dbo].[ResponsibilityDetail]
as
	select	P.Visible,
			P.ResponsibilityID,
			P.ResponsibilityTypeID,
			P.AssigningItem as AssigningItemType,
			P.AssigningItemID,
			P.[Object] as ObjectType,
			P.ObjectID,
			P.ObjectName,
			P.ObjectTypeID,
			P.ObjectTypeName,
			P.ObjectUrl,
			P.ResponsibleObject as ResponsibleObjectType,
			P.ResponsibleObjectID,
			P.ResponsibleObjectName,
			P.ResponsibleObjectUrl,
			RODG.PrimaryOwnerResourceID,
			RES.FirstName + ' ' + RES.LastName as PrimaryOwnerResourceName,
			case 
				when RODG.PrimaryOwnerResourceID is null then ''
				else '#/resources/' + cast(RODG.PrimaryOwnerResourceID as varchar(10))
			end as PrimaryOwnerResourceUrl,
			P.ResponsibilityType as [Role],
			dbo.GetObjectStatisticScore(P.[Object], P.ObjectID) as CurrentScore,
			CI.ContextItems
	from	cache.Responsibilities P
			left join [Group] RODG on P.ResponsibleObject = 'Group' and RODG.ID = P.ResponsibleObjectID
			left join [reporting].[Global_Resource] RES on RES.ResourceID = RODG.PrimaryOwnerResourceID
			outer apply (
						select (
								select	D.Name + ': ' + I.Code + '; '
								from	ResponsibilityContextItem C
										inner join ReferenceItem I on C.ObjectType = 'ReferenceItem' and C.ObjectID = I.ID
										inner join ReferenceItemType D on D.ID = I.ReferenceItemTypeID
								where	ResponsibilityID = P.ResponsibilityID
								for xml path ('')--, root('items')
								) as ContextItems
						) CI
	where	[ResponsibilityTypeGroup] = 1
GO

alter view [dbo].[StatisticTypeCheckOption]
as
	select	'ArtifactType' as ObjectType,
			ID AS ObjectID,
			Name,
			'Artifact' as NamePrefix
	from	ArtifactType
	union
	select	'AttributeType' as ObjectType,
			ID AS ObjectID,
			Name,
			'Attribute' as NamePrefix
	from	AttributeType
	union
	select	'ReferenceItemType' as ObjectType,
			0 AS ObjectID,
			'Reference List',
			'ReferenceItemType' as NamePrefix
	union
	select	'IntersectType' as ObjectType,
			ID AS ObjectID,
			Name,
			'Relationship' as NamePrefix
	from	IntersectType
	union
	select	'ResponsibilityType' as ObjectType,
			ID AS ObjectID,
			Name,
			'Ownership' as NamePrefix
	from	ResponsibilityType
	union
	select	'TaxonomyType' as ObjectType,
			ID AS ObjectID,
			Name,
			'Information Model' as NamePrefix
	from	TaxonomyType
GO

alter view [utility].[ResponsibilityHierarchy]
as
	with 
		IMTH as
		(
		select	'TaxonomyType' as AssigningItemType,
				T.ID as AssigningItemID,
				cast('TaxonomyType' as varchar(25)) as ObjectType,
				T.ID,
				R.ID as ResponsibilityID,
				R.ResponsibilityTypeID
		from	TaxonomyType T 
				inner join Responsibility R on R.ObjectType = 'TaxonomyType' and R.ObjectID = T.ID
		union all
		select	
				P.AssigningItemType,
			    P.AssigningItemID,
				cast('Taxonomy' as varchar(25)) as ObjectType,
				C.ID,
				P.ResponsibilityID,
				P.ResponsibilityTypeID
		from	Taxonomy C
				inner join IMTH P on P.ID = C.TaxonomyTypeID
		),
		IMH as
		(
		select	'Taxonomy' as AssigningItemType, 
				T.ID as AssigningItemID,
				T.ID,
				T.ParentID,
				T.TaxonomyTypeID,
				R.ID as ResponsibilityID,
				R.ResponsibilityTypeID
		from	Taxonomy T 
				inner join Responsibility R on R.ObjectType = 'Taxonomy' and R.ObjectID = T.ID
		union all
		select	
				P.AssigningItemType,
			    COALESCE(R.ObjectID, P.AssigningItemID) as AssigningItemID,
				C.ID,
				C.ParentID,
				C.TaxonomyTypeID,
				COALESCE(R.ID, P.ResponsibilityID) as ResponsibilityID,
				COALESCE(R.ResponsibilityTypeID, P.ResponsibilityTypeID) as ResponsibilityTypeID
		from	Taxonomy C
				inner join IMH P on P.TaxonomyTypeID = C.TaxonomyTypeID and C.ParentID = P.ID
				outer apply (
							select	*
							from	Responsibility 
							where	ResponsibilityTypeID = P.ResponsibilityTypeID
									and ObjectType = 'Taxonomy' 
									and ObjectID = C.ID
							) R
		),
		PolicyHierarchy as
		(
		select	'Policy' as AssigningItemType, 
				P.ID as AssigningItemID,
				P.ID,
				P.ParentID,
				R.ID as ResponsibilityID,
				R.ResponsibilityTypeID
		from	Policy P 
				inner join Responsibility R on R.ObjectType = 'Policy' and R.ObjectID = P.ID --and P.ParentID is null
		union all
		select	
				P.AssigningItemType,
			    COALESCE(R.ObjectID, P.AssigningItemID) as AssigningItemID,
				C.ID,
				C.ParentID,
				COALESCE(R.ID, P.ResponsibilityID) as ResponsibilityID,
				COALESCE(R.ResponsibilityTypeID, P.ResponsibilityTypeID) as ResponsibilityTypeID
		from	Policy C
				inner join PolicyHierarchy P on C.ParentID = P.ID
				outer apply (
							select	*
							from	Responsibility 
							where	ResponsibilityTypeID = P.ResponsibilityTypeID
									and ObjectType = 'Policy' 
									and ObjectID = C.ID
							) R
		),
		PolicyHierarchyForRule as
		(
		select	'Policy' as AssigningItemType, 
				P.ID as AssigningItemID,
				P.ID,
				P.ParentID,
				R.ID as ResponsibilityID,
				R.ResponsibilityTypeID
		from	Policy P 
				inner join Responsibility R on R.ObjectType = 'Policy' and R.ObjectID = P.ID
		union all
		select	
				P.AssigningItemType,
			    COALESCE(R.ObjectID, P.AssigningItemID) as AssigningItemID,
				C.ID,
				C.ParentID,
				COALESCE(R.ID, P.ResponsibilityID) as ResponsibilityID,
				COALESCE(R.ResponsibilityTypeID, P.ResponsibilityTypeID) as ResponsibilityTypeID
		from	Policy C
				inner join PolicyHierarchyForRule P on C.ParentID = P.ID
				outer apply (
							select	*
							from	Responsibility 
							where	ResponsibilityTypeID = P.ResponsibilityTypeID
									and ObjectType = 'Policy' 
									and ObjectID = C.ID
							) R
		),
		RH as	
		(
		select	'Taxonomy' as AssigningItemType, 
				T.ID as AssigningItemID,
				T.ID,
				T.ParentID,
				T.TaxonomyTypeID,
				R.ID as ResponsibilityID,
				R.ResponsibilityTypeID
		from	Taxonomy T 
				inner join Responsibility R on R.ObjectType = 'Taxonomy' and R.ObjectID = T.ID
		union all
		select	
				P.AssigningItemType,
				P.AssigningItemID,
				C.ID,
				C.ParentID,
				C.TaxonomyTypeID,
				P.ResponsibilityID,
				P.ResponsibilityTypeID
		from	Taxonomy C
				inner join RH P on P.TaxonomyTypeID = C.TaxonomyTypeID and C.ParentID = P.ID
		)


	select	P.ResponsibilityID,
			R.ResponsibilityTypeID,
			P.AssigningItemType,
			P.AssigningItemID,
			P.ObjectType,
			P.ObjectID,
			R.ResponsibleObjectType,
			R.ResponsibleObjectID
	from	(
			select	AssigningItemType,
					AssigningItemID,
					ResponsibilityID,
					'Policy' as ObjectType,
					ID as ObjectID
			from	PolicyHierarchy
			union
			select	'Rule' as AssigningItemType,
					RU.ID as AssigningItemID,
					R.ID as ResponsibilityID,
					'Rule' as ObjectType,
					RU.ID as ObjectID
			from	[Rule] RU 
					inner join Responsibility R on R.ObjectType = 'Rule' and R.ObjectID = RU.ID
			union
			select	AssigningItemType,
					AssigningItemID,
					ResponsibilityID,
					ObjectType,
					ID as ObjectID
			from	IMTH
			union 
			select	AssigningItemType,
					AssigningItemID,
					ResponsibilityID,
					'Taxonomy' as ObjectType,
					ID as ObjectID
			from	IMH
			union
			select	'ArtifactType' as AssigningItemType,
					T.ID as AssigningItemID,
					R.ID as ResponsibilityID,
					'ArtifactType' as ObjectType,
					T.ID as ObjectID
			from	ArtifactType T 
					inner join Responsibility R on R.ObjectType = 'ArtifactType' and R.ObjectID = T.ID
			union
			select	'ArtifactType' as AssigningItemType,
					T.ID as AssigningItemID,
					R.ID as ResponsibilityID,
					'Artifact' as ObjectType,
					A.ID as ObjectID
			from	ArtifactType T 
					inner join Responsibility R on R.ObjectType = 'ArtifactType' and R.ObjectID = T.ID
					inner join Artifact A on A.ArtifactTypeID = T.ID
			) P
			inner join Responsibility R on R.ID = P.ResponsibilityID
			inner join ResponsibilityType RT on RT.ID = R.ResponsibilityTypeID and RT.ResponsibilityTypeGroup = 1
GO

alter procedure [bulkload].[BusinessLineage]
--declare
	@id int
--set @id = 237
as
begin
	set nocount on;

	declare @r int,
			@dt datetime = getutcdate(),
			@ActionColumn int = 1,
			@SourceIntersectTypeColumn int = 2,
			@SourceSubjectSubjectAreaColumn int = 3,
			@SourceSubjectColumn int = 4,
			@SourceObjectSubjectAreaColumn int = 5,
			@SourceObjectColumn int = 6,
			@SourceFusionConfigColumn int = 7,
			@SourceFusionAttributeColumn int = 8,
			@TargetIntersectTypeColumn int = 9,
			@TargetSubjectSubjectAreaColumn int = 10,
			@TargetSubjectColumn int = 11,
			@TargetObjectSubjectAreaColumn int = 12,
			@TargetObjectColumn int = 13,
			@TargetFusionConfigColumn int = 14,
			@TargetFusionAttributeColumn int = 15,
			@TransformationColumn int = 16,
			@RoleColumn int = 17

	select	@r = UpdatedBy from [Load] where ID = @id

	--Set the default Action to Add if blank or NULL.
	update	LoadItemColumn
	set		Value = 'Add'
	where	LoadID = @id and ColumnIndex = @ActionColumn and (Value is null or Value = '')

	exec bulkload.UpdateIntersectTypeColumn @id, @SourceIntersectTypeColumn																		-- source intersect type
	exec bulkload.UpdateIntersectTypeColumn @id, @TargetIntersectTypeColumn																		-- target intersect type

	exec bulkload.UpdateSubjectAreaColumn @id, @SourceSubjectSubjectAreaColumn																	-- source subject subject area
	exec bulkload.UpdateSubjectAreaColumn @id, @SourceObjectSubjectAreaColumn																	-- source object subject area
	exec bulkload.UpdateSubjectAreaColumn @id, @TargetSubjectSubjectAreaColumn																	-- target subject subject area
	exec bulkload.UpdateSubjectAreaColumn @id, @TargetObjectSubjectAreaColumn																	-- target object subject area

	exec bulkload.UpdateItemColumnByIntersectType @id, @SourceIntersectTypeColumn, 1, @SourceSubjectSubjectAreaColumn, @SourceSubjectColumn		-- source subject
	exec bulkload.UpdateItemColumnByIntersectType @id, @SourceIntersectTypeColumn, 0, @SourceObjectSubjectAreaColumn, @SourceObjectColumn		-- source object
	exec bulkload.UpdateItemColumnByIntersectType @id, @TargetIntersectTypeColumn, 1, @TargetSubjectSubjectAreaColumn, @TargetSubjectColumn		-- target subject
	exec bulkload.UpdateItemColumnByIntersectType @id, @TargetIntersectTypeColumn, 0, @TargetObjectSubjectAreaColumn, @TargetObjectColumn		-- target object

	exec bulkload.UpdateFusionConfigurationColumn @id, @SourceFusionConfigColumn																-- source fusion config
	exec bulkload.UpdateFusionConfigurationColumn @id, @TargetFusionConfigColumn																-- target fusion config

	exec bulkload.UpdateFusionAttributeColumn @id, @SourceFusionConfigColumn, @SourceFusionAttributeColumn										-- source fusion attribute
	exec bulkload.UpdateFusionAttributeColumn @id, @TargetFusionConfigColumn, @TargetFusionAttributeColumn										-- target fusion attribute

	exec bulkload.UpdateIntersectRoleColumn @id, @RoleColumn																					-- intersect role

	drop table if exists #RemoveItems
	drop table if exists #AddItems
--select * from #RemoveItems
	BEGIN TRANSACTION [Tran1]

	BEGIN TRY
		-- HANDLE THE REMOVEs

		-- Load Temp table that we are going to work from
		select	SS.RowIndex,
		
				SIT.LookupObjectID as SourceIntersectTypeID,
				SS.LookupObject as SourceSubject,
				SS.LookupObjectID as SourceSubjectID,
				SO.LookupObject as SourceObject,
				SO.LookupObjectID as SourceObjectID,

				TIT.LookupObjectID as TargetIntersectTypeID,
				TS.LookupObject as TargetSubject,
				TS.LookupObjectID as TargetSubjectID,
				[TO].LookupObject as TargetObject,
				[TO].LookupObjectID as TargetObjectID,

				SI.ID as SourceIntersectID,
				TI.ID as TargetIntersectID,
				M.ID as MapItemID,

				MRI.ID as MapRuleItemID,

				cast(0 as bit) as Status,
				cast('' as nvarchar(500)) as StatusMessage,

				@r as ResourceID  --THE USER THAT ADDED THE LOAD
		into	#RemoveItems
		from	LoadItemColumn SS
				inner join LoadItemColumn SO	on SO.LoadID = SS.LoadID	and SO.RowIndex = SS.RowIndex 	and SS.ColumnIndex = @SourceSubjectColumn 	and SO.ColumnIndex = @SourceObjectColumn
				inner join LoadItemColumn SA	on SA.LoadID = SS.LoadID	and SA.RowIndex = SS.RowIndex 	and SA.ColumnIndex = @ActionColumn and SA.Value = 'Remove'
				inner join LoadItemColumn SIT	on SIT.LoadID = SS.LoadID	and SIT.RowIndex = SS.RowIndex 	and SIT.ColumnIndex = @SourceIntersectTypeColumn
				left join [Intersect] SI		on SIT.LookupObject = 'IntersectType' and SI.IntersectTypeID = SIT.LookupObjectID 
												and SI.Subject = SS.LookupObject and SI.SubjectID = SS.LookupObjectID 
												and SI.Object = SO.LookupObject and SI.ObjectID = SO.LookupObjectID

				inner join LoadItemColumn TS 	on TS.LoadID = SS.LoadID 	and TS.RowIndex = SS.RowIndex	and TS.ColumnIndex = @TargetSubjectColumn
				inner join LoadItemColumn [TO]	on [TO].LoadID = SS.LoadID	and [TO].RowIndex = SS.RowIndex	and [TO].ColumnIndex = @TargetObjectColumn
				inner join LoadItemColumn TIT	on TIT.LoadID = SS.LoadID	and TIT.RowIndex = SS.RowIndex 	and TIT.ColumnIndex = @TargetIntersectTypeColumn
				left join [Intersect] TI		on TIT.LookupObject = 'IntersectType' and TI.IntersectTypeID = TIT.LookupObjectID 
												and TI.Subject = TS.LookupObject and TI.SubjectID = TS.LookupObjectID 
												and TI.Object = [TO].LookupObject and TI.ObjectID = [TO].LookupObjectID

				left join MapItem M				on M.SourceIntersectID = SI.ID and M.TargetIntersectID = TI.ID

				left join LoadItemColumn SFA	on SFA.LoadID = SS.LoadID	and SFA.RowIndex = SS.RowIndex 	and SFA.ColumnIndex = @SourceFusionAttributeColumn
				left join LoadItemColumn TFA	on TFA.LoadID = SS.LoadID	and TFA.RowIndex = SS.RowIndex 	and TFA.ColumnIndex = @TargetFusionAttributeColumn
				left join MapRuleItem MRI		on	SFA.LookupObject = 'FusionAttribute' and MRI.SourceFusionAttributeID = SFA.LookupObjectID and
													TFA.LookupObject = 'FusionAttribute' and MRI.TargetFusionAttributeID = TFA.LookupObjectID

		where	SS.LoadID = @id


		-- Add indexes to temp table
		CREATE NONCLUSTERED INDEX [IX_TempRemoveItems_MapItem] ON #RemoveItems ( MapItemID ASC )
		CREATE NONCLUSTERED INDEX [IX_TempRemoveItems_MapRuleItem] ON #RemoveItems ( MapRuleItemID ASC )
		CREATE NONCLUSTERED INDEX [IX_TempRemoveItems_SourceIntersect] ON #RemoveItems ( SourceIntersectID ASC )
		CREATE NONCLUSTERED INDEX [IX_TempRemoveItems_TargetIntersect] ON #RemoveItems ( TargetIntersectID ASC )

		/*	BEGIN: REMOVE TECHNICAL MAPPINGS THAT ARE TIED TO FOUND MAP ITEMS */
		declare @mapRuleItems table(MapRuleItemID int, MapRuleID int)
		insert into @mapRuleItems
			select	T.MapRuleItemID,
					TJ.MapRuleID
			from	MapRuleItemMapItem T
					inner join #RemoveItems S on S.MapItemID = T.MapItemID
					left join MapRuleItemMapRule TJ on TJ.MapRuleItemID = T.MapRuleItemID

		delete	T
		from	MapRuleItemMapItem T
				inner join @mapRuleItems S on S.MapRuleItemID = T.MapRuleItemID

		delete	T
		from	MapRuleItemMapRule T
				inner join @mapRuleItems S on S.MapRuleItemID = T.MapRuleItemID

		delete	T
		from	MapRule T
				inner join @mapRuleItems S on S.MapRuleID = T.ID
				left join MapRuleItemMapRule NTJ on NTJ.MapRuleID = S.MapRuleID and NTJ.MapRuleItemID <> S.MapRuleItemID	--get all map rules that are used only once.
		where	NTJ.MapRuleID is null
		/*	END: REMOVE TECHNICAL MAPPINGS THAT ARE TIED TO FOUND MAP ITEMS */

		/*	BEGIN: REMOVE TECHNICAL MAPPING OPTIONALLY SPECIFIED IF NOT TIED ANYWHERE ELSE */
		declare @mapRuleItemIDs table(MapRuleItemID int)
		insert into @mapRuleItemIDs
			select	S.MapRuleItemID
			from	#RemoveItems S
					left join MapRuleItemMapItem J on J.MapRuleItemID = S.MapRuleItemID
			where	S.MapRuleItemID is not null;

		delete	T
		from	MapRuleItem T
				inner join @mapRuleItemIDs S on S.MapRuleItemID = T.ID;

		/*	END: REMOVE TECHNICAL MAPPING OPTIONALLY SPECIFIED IF NOT TIED ANYWHERE ELSE */

		/*	BEGIN: MAPPINGS FOUND MAP ITEMS */
		declare @mapItems table(MapItemID int, MapID int)
		insert into @mapItems
			select	S.MapItemID,
					J.MapID
			from	#RemoveItems S
					left join MapItemMap J on J.MapItemID = S.MapItemID;

		delete	T
		from	MapItemMap T
				inner join @mapItems S on S.MapItemID = T.MapItemID;

		delete	T
		from	MapSequence T
				inner join @mapItems S on S.MapItemID = T.MapItemID;

		delete	T
		from	MapItem T
				inner join @mapItems S on S.MapItemID = T.ID;

		delete	T
		from	MapRule T
				inner join @mapRuleItems S on S.MapRuleID = T.ID
				left join MapRuleItemMapRule NTJ on NTJ.MapRuleID = S.MapRuleID and NTJ.MapRuleItemID <> S.MapRuleItemID	--get all map rules that are used only once.
		where	NTJ.MapRuleID is null;
		/*	END: REMOVE FOUND MAP ITEMS */

		/*	BEGIN: REMOVE SOURCE AND TARGET INTERSECTS THAT ARE NOT REFERENCED ANYWHERE ELSE */
		delete	T
		from	[Intersect] T
				inner join #RemoveItems S on (S.SourceIntersectID = T.ID or S.TargetIntersectID = T.ID)
				left join IntersectGroup CG on CG.IntersectID = T.ID
				left join MapItem CSM on CSM.SourceIntersectID = T.ID
				left join MapItem CTM on CTM.TargetIntersectID = T.ID
				left join [Intersect] CI on (CI.Subject = 'Intersect' and CI.SubjectID = T.ID) or (CI.Object = 'Intersect' and CI.ObjectID = T.ID)
		where	CG.ID is null and
				CSM.ID is null and 
				CTM.ID is null and
				CI.ID is null;
		/*	BEGIN: REMOVE SOURCE INTERSECTS THAT ARE NOT REFERENCED ANYWHERE ELSE */

		-- update status & status message for Items table
		
		-- SUCCESS STATUS
		update	T
		set		T.Status = 1,
				T.StatusMessage = coalesce(T.StatusMessage,'') + 'Business map removed. '
		from	#RemoveItems T
				left join MapItem S on S.ID = T.MapItemID
		where	T.MapItemID is not null and S.ID is null;

		update	T
		set		T.StatusMessage = coalesce(T.StatusMessage,'') + 'Source relationship removed. '
		from	#RemoveItems T
				left join [Intersect] S on S.ID = T.SourceIntersectID
		where	T.SourceIntersectID is not null and S.ID is null;

		update	T
		set		T.StatusMessage = coalesce(T.StatusMessage,'') + 'Target relationship removed. '
		from	#RemoveItems T
				left join [Intersect] S on S.ID = T.TargetIntersectID
		where	T.TargetIntersectID is not null and S.ID is null;

		-- FAILED STATUS
		update	T
		set		T.Status = 0,
				T.StatusMessage = coalesce(T.StatusMessage,'') + 'Could not find source relationship. '
		from	#RemoveItems T
		where	SourceIntersectID is null;

		update	T
		set		T.Status = 0,
				T.StatusMessage = coalesce(T.StatusMessage,'') + 'Could not find target relationship. '
		from	#RemoveItems T
		where	TargetIntersectID is null;

		update	T
		set		T.Status = 0,
				T.StatusMessage = coalesce(T.StatusMessage,'') + 'Could not find business map. '
		from	#RemoveItems T
		where	MapItemID is null;


		-- Now update LoadItems on original Load with status and messages created above
		update	T
		set		T.Status = S.Status,
				T.StatusMessage = S.StatusMessage,
				T.Object = case S.Status
							when 1 then 'MapItem'
							else NULL
						   end,
				T.ObjectID = case S.Status
							when 1 then S.MapItemID
							else NULL
						   end
		from	LoadItem T
				inner join #RemoveItems S on T.LoadID = @id and S.RowIndex = T.RowIndex;



		-- NOW HANDLE THE ADDs ---------------------------------------------------------------------------

		-- Load Temp table that we are going to work from
		select	SS.RowIndex,
		
				SIT.LookupObjectID as SourceIntersectTypeID,
				SS.LookupObject as SourceSubject,
				SS.LookupObjectID as SourceSubjectID,
				SO.LookupObject as SourceObject,
				SO.LookupObjectID as SourceObjectID,

				TIT.LookupObjectID as TargetIntersectTypeID,
				TS.LookupObject as TargetSubject,
				TS.LookupObjectID as TargetSubjectID,
				[TO].LookupObject as TargetObject,
				[TO].LookupObjectID as TargetObjectID,

				SFA.LookupObjectID as SourceFusionAttributeID,
				SFA.Value as SourceFusionAttributeRaw,
				TFA.LookupObjectID as TargetFusionAttributeID,
				TFA.Value as TargetFusionAttributeRaw,

				SI.ID as SourceIntersectID,
				TI.ID as TargetIntersectID,
				M.ID as MapItemID,
				MRI.ID as MapRuleItemID,

				SIFT.ID as SourceFusionIntersectTypeID,
				TIFT.ID as TargetFusionIntersectTypeID,
				SIF.ID as SourceFusionIntersectID,
				TIF.ID as TargetFusionIntersectID,

				cast(null as bit) as Status,
				cast('' as nvarchar(500)) as StatusMessage,

				@r as ResourceID  --THE USER THAT ADDED THE LOAD
		into	#AddItems
		from	LoadItemColumn SS
				inner join LoadItemColumn SO	on SO.LoadID = SS.LoadID	and SO.RowIndex = SS.RowIndex 	and SS.ColumnIndex = @SourceSubjectColumn 	and SO.ColumnIndex = @SourceObjectColumn
				inner join LoadItemColumn SA	on SA.LoadID = SS.LoadID	and SA.RowIndex = SS.RowIndex 	and SA.ColumnIndex = @ActionColumn and SA.Value = 'Add'
				inner join LoadItemColumn SIT	on SIT.LoadID = SS.LoadID	and SIT.RowIndex = SS.RowIndex 	and SIT.ColumnIndex = @SourceIntersectTypeColumn
				left join [Intersect] SI		on SIT.LookupObject = 'IntersectType' and SI.IntersectTypeID = SIT.LookupObjectID 
												and SI.Subject = SS.LookupObject and SI.SubjectID = SS.LookupObjectID 
												and SI.Object = SO.LookupObject and SI.ObjectID = SO.LookupObjectID

				inner join LoadItemColumn TS 	on TS.LoadID = SS.LoadID 	and TS.RowIndex = SS.RowIndex	and TS.ColumnIndex = @TargetSubjectColumn
				inner join LoadItemColumn [TO]	on [TO].LoadID = SS.LoadID	and [TO].RowIndex = SS.RowIndex	and [TO].ColumnIndex = @TargetObjectColumn
				inner join LoadItemColumn TIT	on TIT.LoadID = SS.LoadID	and TIT.RowIndex = SS.RowIndex 	and TIT.ColumnIndex = @TargetIntersectTypeColumn
				left join [Intersect] TI		on TIT.LookupObject = 'IntersectType' and TI.IntersectTypeID = TIT.LookupObjectID 
												and TI.Subject = TS.LookupObject and TI.SubjectID = TS.LookupObjectID 
												and TI.Object = [TO].LookupObject and TI.ObjectID = [TO].LookupObjectID

				left join MapItem M				on M.SourceIntersectID = SI.ID and M.TargetIntersectID = TI.ID

				left join LoadItemColumn SFA	on SFA.LoadID = SS.LoadID	and SFA.RowIndex = SS.RowIndex 	and SFA.ColumnIndex = @SourceFusionAttributeColumn
				left join LoadItemColumn TFA	on TFA.LoadID = SS.LoadID	and TFA.RowIndex = SS.RowIndex 	and TFA.ColumnIndex = @TargetFusionAttributeColumn

				left join MapRuleItem MRI		on	SFA.LookupObject = 'FusionAttribute' and MRI.SourceFusionAttributeID = SFA.LookupObjectID and
													TFA.LookupObject = 'FusionAttribute' and MRI.TargetFusionAttributeID = TFA.LookupObjectID

				left join FusionAttribute SFAO	on SFA.LookupObject = 'FusionAttribute' and SFAO.ID = SFA.LookupObjectID 
				outer apply (
						SELECT  MIN(ID) as ID
						FROM    IntersectType
						WHERE   Subject = 'IntersectType' and SubjectID = SIT.LookupObjectID and Object = 'FusionAttributeType' and ObjectID = SFAO.FusionAttributeTypeID
				) SIFT
				left join [Intersect] SIF		on	SIF.IntersectTypeID = SIFT.ID 
													and SIF.Subject = 'Intersect' and SIF.SubjectID = SI.ID
													and SIF.Object = SFA.LookupObject and SIF.ObjectID = SFA.LookupObjectID

				left join FusionAttribute TFAO	on TFA.LookupObject = 'FusionAttribute' and TFAO.ID = TFA.LookupObjectID 
				outer apply (
						SELECT  MIN(ID) as ID
						FROM    IntersectType
						WHERE   Subject = 'IntersectType' and SubjectID = TIT.LookupObjectID and Object = 'FusionAttributeType' and ObjectID = TFAO.FusionAttributeTypeID
				) TIFT
				left join [Intersect] TIF		on	TIF.IntersectTypeID = TIFT.ID 
													and TIF.Subject = 'Intersect' and TIF.SubjectID = TI.ID
													and TIF.Object = TFA.LookupObject and TIF.ObjectID = TFA.LookupObjectID

		where	SS.LoadID = @id

		-- Add indexes to temp table
		CREATE NONCLUSTERED INDEX [IX_SourceBusinessIntersect] ON #AddItems ( SourceIntersectTypeID ASC, SourceSubject ASC, SourceSubjectID ASC, SourceObject ASC, SourceObjectID ASC )
/*
update LoadItemColumn set Value = 'Bloomberg LP/Back Office Data License' where LoadID =  270 and RowIndex = 2 and ColumnIndex = 4
select * from LoadItemColumn where LoadID = 270
select * from #AddItems
select * from LoadItem where LoadID = 270

select I.LoadID, I.RowIndex, case I.[Status] when 1 then 'Complete' when 0 then 'Failed' else 'Queued' end as [Status], I.StatusMessage
from LoadItem I
where I.LoadID = 270
order by I.RowIndex
*/
		-- ERROR OUT THE ROWS THAT DO NOT HAVE THE APPROPRIATE FUSION INTERSECT TYPE IDs.
		update	#AddItems
		set		Status = 0,
				StatusMessage = coalesce(StatusMessage,'') +
								IIF(SourceFusionIntersectTypeID is null, 'Could not find source fusion relationship type. ', '') + 
								IIF(SourceFusionAttributeID is null, 'Could not find source fusion path. ', '') + 
								IIF(TargetFusionIntersectTypeID is null, 'Could not find target fusion relationship type. ', '') + 
								IIF(TargetFusionAttributeID is null, 'Could not find target fusion path. ', '')
		where	(SourceFusionAttributeRaw is not null and SourceFusionIntersectTypeID is null) OR (TargetFusionAttributeRaw is not null and TargetFusionIntersectTypeID is null);

		-- ERROR OUT THE ROWS THAT DO NOT HAVE THE APPROPRIATE SOURCEs.
		update	#AddItems
		set		Status = 0,
				StatusMessage = coalesce(StatusMessage,'') +
								IIF(SourceSubjectID is null, 'Could not find source subject. ', '') + 
								IIF(SourceObjectID is null, 'Could not find source object. ', '')
		where	(SourceSubjectID is null) OR (SourceObjectID is null);

		-- ERROR OUT THE ROWS THAT DO NOT HAVE THE APPROPRIATE TARGETs.
		update	#AddItems
		set		Status = 0,
				StatusMessage = coalesce(StatusMessage,'') +
								IIF(TargetSubjectID is null, 'Could not find target subject. ', '') + 
								IIF(TargetObjectID is null, 'Could not find target object. ', '')
		where	(TargetSubjectID is null) OR (TargetObjectID is null);




		/*	BEGIN: SOURCE BUSINESS INTERSECT LOGIC */

		-- insert source business relationships
		insert into [Intersect] (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
			select	SourceIntersectTypeID, 
					SourceSubject, SourceSubjectID, 
					SourceObject, SourceObjectID,
					0, ResourceID, @dt, ResourceID, @dt
			from	(
					select		SourceIntersectTypeID, SourceSubject, SourceSubjectID, SourceObject, SourceObjectID, ResourceID
					from		#AddItems
					where		Status is null 
								and SourceIntersectID is null
					group by	SourceIntersectTypeID, SourceSubject, SourceSubjectID, SourceObject, SourceObjectID, ResourceID
					) O


		-- update rows with existing source business intersect
		update	T
		set		T.SourceIntersectID = S.ID,
				T.StatusMessage = coalesce(T.StatusMessage,'') + ' Source business relationship created.'
		from	#AddItems T
				inner join [Intersect] S on S.IntersectTypeID = T.SourceIntersectTypeID 
											and T.SourceSubject = S.Subject and T.SourceSubjectID = S.SubjectID 
											and T.SourceObject = S.Object and T.SourceObjectID = S.ObjectID
											and T.SourceIntersectID is null
											and T.Status is null;
		
		-- update rows with existing target business intersect
		update	T
		set		T.TargetIntersectID = S.ID
		from	#AddItems T
				inner join [Intersect] S on S.IntersectTypeID = T.TargetIntersectTypeID 
											and T.TargetSubject = S.Subject and T.TargetSubjectID = S.SubjectID 
											and T.TargetObject = S.Object and T.TargetObjectID = S.ObjectID
											and T.TargetIntersectID is null
											and T.Status is null;

		/*	END: SOURCE BUSINESS INTERSECT LOGIC */


		/*	BEGIN: TARGET BUSINESS INTERSECT LOGIC */

		-- insert target business relationships
		insert into [Intersect] (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
			select	TargetIntersectTypeID, 
					TargetSubject, TargetSubjectID, 
					TargetObject, TargetObjectID,
					0, ResourceID, @dt, ResourceID, @dt
			from	(
					select		TargetIntersectTypeID, TargetSubject, TargetSubjectID, TargetObject, TargetObjectID, ResourceID
					from		#AddItems
					where		Status is null 
								and TargetIntersectID is null
					group by	TargetIntersectTypeID, TargetSubject, TargetSubjectID, TargetObject, TargetObjectID, ResourceID
					) O

		-- update rows with existing target business intersect
		update	T
		set		T.TargetIntersectID = S.ID,
				T.StatusMessage = coalesce(T.StatusMessage,'') + ' Target business relationship created.'
		from	#AddItems T
				inner join [Intersect] S on S.IntersectTypeID = T.TargetIntersectTypeID 
											and T.TargetSubject = S.Subject and T.TargetSubjectID = S.SubjectID 
											and T.TargetObject = S.Object and T.TargetObjectID = S.ObjectID
											and T.TargetIntersectID is null
											and T.Status is null;

		/*	END: TARGET BUSINESS INTERSECT LOGIC */


		/*	BEGIN: SOURCE TECHNICAL INTERSECT LOGIC */

		-- insert source technical relationships
		insert into [Intersect] (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
			select	SourceFusionIntersectTypeID, 
					'Intersect', SourceIntersectID, 'FusionAttribute', SourceFusionAttributeID,
					0, ResourceID, @dt, ResourceID, @dt
			from	(
					select		SourceFusionIntersectTypeID, SourceIntersectID, SourceFusionAttributeID, ResourceID
					from		#AddItems
					where		Status is null
								and SourceFusionIntersectTypeID is not null
								and SourceFusionIntersectID is null
								and SourceIntersectID is not null
								and SourceFusionAttributeID is not null
					group by	SourceFusionIntersectTypeID, SourceIntersectID, SourceFusionAttributeID, ResourceID
					) O;

		-- update rows with new source technical intersect
		update	T
		set		T.SourceFusionIntersectID = S.ID,
				T.StatusMessage = coalesce(T.StatusMessage,'') + ' Source technical relationship created.'
		from	#AddItems T
				inner join [Intersect] S on S.IntersectTypeID = T.SourceFusionIntersectTypeID 
											and S.Subject = 'Intersect' and S.SubjectID = T.SourceIntersectID 
											and S.Object = 'FusionAttribute' and S.ObjectID = T.SourceFusionAttributeID
											and T.SourceFusionIntersectID is null 
											and T.Status is null;

		-- update rows with new target technical intersect
		update	T
		set		T.TargetFusionIntersectID = S.ID
		from	#AddItems T
				inner join [Intersect] S on S.IntersectTypeID = T.TargetFusionIntersectTypeID 
											and S.Subject = 'Intersect' and S.SubjectID = T.TargetIntersectID 
											and S.Object = 'FusionAttribute' and S.ObjectID = T.TargetFusionAttributeID
											and T.TargetFusionIntersectID is null 
											and T.Status is null;

		/*	END: SOURCE TECHNICAL INTERSECT LOGIC */


		/*	BEGIN: TARGET TECHNICAL INTERSECT LOGIC */
		
		-- insert target technical relationships
		insert into [Intersect] (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
			select	TargetFusionIntersectTypeID, 
					'Intersect', TargetIntersectID, 'FusionAttribute', TargetFusionAttributeID,
					0, ResourceID, @dt, ResourceID, @dt
			from	(
					select		TargetFusionIntersectTypeID, TargetIntersectID, TargetFusionAttributeID, ResourceID
					from		#AddItems
					where		Status is null
								and TargetFusionIntersectTypeID is not null
								and TargetFusionIntersectID is null
								and TargetIntersectID is not null
								and TargetFusionAttributeID is not null			
					group by	TargetFusionIntersectTypeID, TargetIntersectID, TargetFusionAttributeID, ResourceID
					) O;

		-- update rows with new target technical intersect
		update	T
		set		T.TargetFusionIntersectID = S.ID,
				T.StatusMessage = coalesce(T.StatusMessage,'') + ' Target technical relationship created.'
		from	#AddItems T
				inner join [Intersect] S on S.IntersectTypeID = T.TargetFusionIntersectTypeID 
											and S.Subject = 'Intersect' and S.SubjectID = T.TargetIntersectID 
											and S.Object = 'FusionAttribute' and S.ObjectID = T.TargetFusionAttributeID
											and T.TargetFusionIntersectID is null 
											and T.Status is null;

		/*	END: TARGET TECHNICAL INTERSECT LOGIC */

		-- insert new map items
		insert into MapItem (SourceIntersectID, TargetIntersectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
			select	distinct
					SourceIntersectID, 
					TargetIntersectID,
					ResourceID,
					@dt, 
					ResourceID,
					@dt
			from	#AddItems
			where	SourceIntersectID is not null 
					and TargetIntersectID is not null 
					and MapItemID is null
					and Status is null;

		-- update source data with newly created map item IDs
		update	T
		set		T.MapItemID = S.ID,
				T.StatusMessage = coalesce(T.StatusMessage,'') + ' Business map created.'
		from	#AddItems T
				inner join [MapItem] S on	S.SourceIntersectID = T.SourceIntersectID 
											and S.TargetIntersectID = T.TargetIntersectID 
											and T.MapItemID is null 
											and T.Status is null;

		-- insert new map rule items
		insert into MapRuleItem (SourceFusionAttributeID, TargetFusionAttributeID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
			select	distinct
					SourceFusionAttributeID, 
					TargetFusionAttributeID,
					ResourceID,
					@dt, 
					ResourceID,
					@dt
			from	#AddItems
			where	SourceIntersectID is not null 
					and TargetIntersectID is not null
					and SourceFusionAttributeID is not null 
					and TargetFusionAttributeID is not null
					and Status is null;

		-- update source data with newly created map rule item IDs
		update	T
		set		T.MapRuleItemID = S.ID,
				T.StatusMessage = coalesce(T.StatusMessage,'') + ' Technical map created.'
		from	#AddItems T
				inner join [MapRuleItem] S on	S.SourceFusionAttributeID = T.SourceFusionAttributeID 
												and S.TargetFusionAttributeID = T.TargetFusionAttributeID 
												and T.MapRuleItemID is null 
												and Status is null;

		-- MERGE MapRuleItemMapItem with all the IDs above
		merge	MapRuleItemMapItem as T
		using	(
				select		MapItemID, 
							MapRuleItemID
				from		#AddItems
				where		MapItemID is not null
							and MapRuleItemID is not null
				group by	MapItemID, 
							MapRuleItemID
				) as S
		on		T.MapRuleItemID = S.MapRuleItemID and T.MapItemID = S.MapItemID
		when	not matched by target then
				insert (MapRuleItemID, MapItemID)
				values (S.MapRuleItemID, S.MapItemID);

		
		-- CALCULATE STATUS BASED ON POPULATED IDs
		update	#AddItems
		set		Status = 1
		where	MapItemID is not null 
				and (
					(SourceFusionAttributeRaw is not null and TargetFusionAttributeRaw is not null and MapRuleItemID is not null) 
					or 
					(SourceFusionAttributeRaw is null and TargetFusionAttributeRaw is null)
				);

		-- Now update LoadItems on original Load with status and messages created above
		update	T
		set		T.Status = S.Status,
				T.StatusMessage = S.StatusMessage,
				T.Object = case S.Status
							when 1 then 'MapItem'
							else NULL
						   end,
				T.ObjectID = case S.Status
							when 1 then S.MapItemID
							else NULL
						   end
		from	LoadItem T
				inner join #AddItems S on T.LoadID = @id and S.RowIndex = T.RowIndex;


--select *,  case [Status] when 1 then 'Complete' when 0 then 'Failed' else 'Queued' end as [Status] from LoadItem where LoadID = 270

		-- NOW, Close out the Load job ----------------------------------------------------------------------------------
		update	LoadItem
		set		Status = cast(0 as bit),
				StatusMessage = 'Incomplete : ' + coalesce(StatusMessage,''),
				Object = null,
				ObjectID = null
		where	LoadID = @id and Status is null;

		update	[Load]
		set		DateCompleted = getutcdate()
		where	ID = @id;

		COMMIT TRANSACTION [Tran1]
	END TRY
	BEGIN CATCH
		ROLLBACK TRANSACTION [Tran1]
		select ERROR_MESSAGE()
		update	[Load]
		set		Notes = Notes + '<br/> ' + ERROR_MESSAGE()
		where	ID = @id;
	END CATCH
end
GO

alter procedure [bulkload].[UpdateDynamicLookupFieldColumns]
	@id int,
	@startColumnIndex int,
	@endColumnIndex int
as
begin
	set nocount on;
	update	T
	set		T.LookupObject = S.LookupObject,
			T.LookupObjectID = S.LookupObjectID
	from	LoadItemColumn T
			inner join	(
						select	IC.LoadID,
								IC.RowIndex,
								IC.ColumnIndex,
								case 
									when L_A.ID is not null then 'Artifact'
									when L_D.ID is not null then 'ReferenceItemType'
									when L_DI.ID is not null then 'ReferenceItem'
									when L_F.ID is not null then 'FusionAttribute'
									when L_L.Value is not null then 'Lookup'
									when L_T.ID is not null then 'Taxonomy'
									else NULL
								end as LookupObject,
								coalesce(L_A.ID, L_D.ID, L_DI.ID, L_F.ID, L_L.Value, L_T.ID) as LookupObjectID -- L_I.ID,
						from	FieldType F
								inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
								inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
								inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
								
								left join Artifact L_A on F.LookupObjectType in ('Artifact', 'ArtifactType') and L_A.ArtifactTypeID = F.LookupObjectID and (L_A.[Name] = IC.Value OR L_A.TextPath = IC.Value)
								left join ReferenceItemType L_D on F.LookupObjectType = 'ReferenceItemType' and L_D.ID = F.LookupObjectID and L_D.[Name] = IC.Value
								left join ReferenceItem L_DI on F.LookupObjectType = 'ReferenceItem' and L_DI.ReferenceItemTypeID = F.LookupObjectID and L_DI.[Code] = IC.Value
								left join FusionAttribute L_F on F.LookupObjectType = 'FusionAttributeType' and L_F.FusionAttributeTypeID = F.LookupObjectID and (L_F.[Name] = IC.Value OR L_F.TextPath = IC.Value)
								left join [FieldLookupValue] L_L on F.ID = L_L.FieldTypeID and F.LookupObjectType = 'Lookup' and L_L.LookupObjectType = 'Lookup' and L_L.LookupObjectID = F.LookupObjectID and L_L.[Text] = IC.Value
								left join Taxonomy L_T on F.LookupObjectType in ('Taxonomy', 'TaxonomyType') and L_T.TaxonomyTypeID = F.LookupObjectID and (L_T.[Name] = IC.Value OR L_T.TextPath = IC.Value)
						where	C.ColumnIndex between @startColumnIndex and @endColumnIndex
						) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

	while @startColumnIndex <= @endColumnIndex
	begin
		update	T
		set		T.StatusMessage = coalesce(T.StatusMessage, '') + S.StatusMessage
		from	LoadItem T
				inner join	(
							select	IC.LoadID,
									IC.RowIndex,
									case 
										when IC.LookupObjectID is null and IC.Value is not null and IC.Value <> '' then ' ' + F.Name + ' does not contain a valid value.'
										else ''
									end StatusMessage
							from	FieldType F
									inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
									inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
									inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex and IC.columnIndex = @startColumnIndex and IC.LookupObjectID is null
							) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex
		set @startColumnIndex = @startColumnIndex + 1
	end
end
GO

alter procedure [bulkload].[UpdateItemColumn]
	@id int,
	@globalTypeColumn int, 
	@typeColumn int, 
	@subjectAreaColumn int, 
	@itemColumn int
as
begin
	set nocount on;
	update	T
	set		T.LookupObject = TTT.Value,
			T.LookupObjectID = coalesce(A.ID, D.ID, I.ID, P.ID, R.ID, TA.ID)
	from	LoadItemColumn T
			inner join LoadItemColumn TT on TT.LoadID = T.LoadID and T.LoadID = @id and TT.RowIndex = T.RowIndex and TT.ColumnIndex = @typeColumn and T.ColumnIndex = @itemColumn
			inner join LoadItemColumn TS on TS.LoadID = T.LoadID and TS.RowIndex = T.RowIndex and TS.ColumnIndex = @subjectAreaColumn
			inner join LoadItemColumn TTT on TTT.LoadID = T.LoadID and TTT.RowIndex = T.RowIndex and TTT.ColumnIndex = @globalTypeColumn
			left join Artifact A on lower(A.TextPath) = lower(T.Value) and A.TaxonomyTypeID = TS.LookupObjectID and A.ArtifactTypeID = TT.LookupObjectID and TTT.Value = 'Artifact'
			left join ReferenceItemType D on lower(D.Name) = lower(T.Value) and TTT.Value = 'ReferenceItemType'
			left join [Intersect] I on lower(I.Name) = lower(T.Value) and I.IntersectTypeID = TT.LookupObjectID and TTT.Value = 'Intersect'
			left join [Policy] P on lower(P.TextPath) = lower(T.Value) and P.PolicyTypeID = TT.LookupObjectID and TTT.Value = 'Policy'
			left join [Rule] R on lower(R.Name) = lower(T.Value) and R.RuleType = TT.LookupObjectID and TTT.Value = 'Rule'
			left join [Taxonomy] TA on lower(TA.TextPath) = lower(T.Value) and TA.TaxonomyTypeID = TT.LookupObjectID and TTT.Value = 'Taxonomy'
	where	coalesce(A.ID, D.ID, I.ID, P.ID, R.ID, TA.ID) is not null
end
GO

alter procedure [bulkload].[UpdateItemColumnByIntersectType]
	@id int,
	@intersectTypeColumn int, 
	@isSubject bit,
	@subjectAreaColumn int, 
	@itemColumn int
as
begin
	set nocount on;
	update	T
	set		T.LookupObject = replace(case when @isSubject = 1 then IT.Subject else IT.Object end, 'Type', ''),
			T.LookupObjectID = coalesce(A.ID, D.ID, F.ID, I.ID, P.ID, R.ID, TA.ID)
	from	LoadItemColumn T
			inner join LoadItemColumn TI on TI.LoadID = T.LoadID and TI.RowIndex = T.RowIndex and TI.ColumnIndex = @intersectTypeColumn and T.ColumnIndex = @itemColumn
			inner join IntersectType IT on TI.LookupObject = 'IntersectType' and IT.ID = TI.LookupObjectID
			left join LoadItemColumn TS on TS.LoadID = T.LoadID and TS.RowIndex = T.RowIndex and TS.ColumnIndex = @subjectAreaColumn
			left join Artifact A on lower(A.TextPath) = lower(T.Value) and A.TaxonomyTypeID = TS.LookupObjectID and A.ArtifactTypeID = case when @isSubject = 1 then IT.SubjectID else IT.ObjectID end and 'ArtifactType' = case when @isSubject = 1 then IT.Subject else IT.Object end
			left join ReferenceItemType D on lower(D.Name) = lower(T.Value) and 'ReferenceItemType' = case when @isSubject = 1 then IT.Subject else IT.Object end
			left join FusionAttribute F on lower(F.TextPath) = lower(T.Value) and F.FusionAttributeTypeID = case when @isSubject = 1 then IT.SubjectID else IT.ObjectID end and 'FusionAttributeType' = case when @isSubject = 1 then IT.Subject else IT.Object end
			left join [Intersect] I on lower(I.Name) = lower(T.Value) and I.IntersectTypeID = case when @isSubject = 1 then IT.SubjectID else IT.ObjectID end and 'IntersectType' = case when @isSubject = 1 then IT.Subject else IT.Object end
			left join [Policy] P on lower(P.TextPath) = lower(T.Value) and P.PolicyTypeID = case when @isSubject = 1 then IT.SubjectID else IT.ObjectID end and 'PolicyType' = case when @isSubject = 1 then IT.Subject else IT.Object end
			left join [Rule] R on lower(R.Name) = lower(T.Value) and R.RuleType = case when @isSubject = 1 then IT.SubjectID else IT.ObjectID end and 'RuleType' = case when @isSubject = 1 then IT.Subject else IT.Object end
			left join [Taxonomy] TA on lower(TA.TextPath) = lower(T.Value) and TA.TaxonomyTypeID = case when @isSubject = 1 then IT.SubjectID else IT.ObjectID end and 'TaxonomyType' = case when @isSubject = 1 then IT.Subject else IT.Object end
	where	T.LoadID = @id and coalesce(A.ID, D.ID, F.ID, I.ID, P.ID, R.ID, TA.ID) is not null
end
GO

alter procedure [bulkload].[UpdateItemColumnByType]
	@id int,
	@ObjectType varchar(50), 
	@ObjectTypeID int,
	@subjectAreaColumn int, 
	@itemColumn int
as
begin
	set nocount on;
	update	T
	set		T.LookupObject = replace(@ObjectType, 'Type', ''),
			T.LookupObjectID = coalesce(A.ID, D.ID, F.ID, I.ID, P.ID, R.ID, TA.ID)
	from	LoadItemColumn T
			left join LoadItemColumn TS on TS.LoadID = T.LoadID and TS.RowIndex = T.RowIndex and TS.ColumnIndex = @subjectAreaColumn and T.ColumnIndex = @itemColumn
			left join Artifact A on lower(A.TextPath) = lower(T.Value) and A.TaxonomyTypeID = TS.LookupObjectID and A.ArtifactTypeID = @ObjectTypeID and @ObjectType = 'ArtifactType'
			left join ReferenceItemType D on lower(D.Name) = lower(T.Value) and @ObjectType = 'ReferenceItemType'
			left join FusionAttribute F on lower(F.TextPath) = lower(T.Value) and F.FusionAttributeTypeID = @ObjectTypeID and @ObjectType = 'FusionAttributeType'
			left join [Intersect] I on lower(I.Name) = lower(T.Value) and I.IntersectTypeID = @ObjectTypeID and @ObjectType = 'IntersectType'
			left join [Policy] P on lower(P.TextPath) = lower(T.Value) and P.PolicyTypeID = @ObjectTypeID and @ObjectType = 'PolicyType'
			left join [Rule] R on lower(R.Name) = lower(T.Value) and R.RuleType = @ObjectTypeID and @ObjectType = 'RuleType'
			left join [Taxonomy] TA on lower(TA.TextPath) = lower(T.Value) and TA.TaxonomyTypeID = @ObjectTypeID and @ObjectType = 'TaxonomyType'
	where	T.LoadID = @id and coalesce(A.ID, D.ID, F.ID, I.ID, P.ID, R.ID, TA.ID) is not null
end
GO

alter procedure [bulkload].[UpdateTypeColumn]
	@id int,
	@typeColumn int,
	@typeNameColumn int
as
begin
	set nocount on;
	update	T2
	set		T2.LookupObject = T1.Value + 'Type',
			T2.LookupObjectID = coalesce(A.ID, D.ID, P.ID, T.ID, R.ID)
	from	LoadItemColumn T2
			inner join LoadItemColumn T1 on T1.LoadID = T2.LoadID and T1.RowIndex = T2.RowIndex and T1.ColumnIndex = @typeColumn and T2.LoadID = @id and T2.ColumnIndex = @typeNameColumn
			left join ArtifactType A on lower(A.Name) = lower(T2.Value) and T1.Value = 'Artifact'
			left join ReferenceItemType D on lower(D.Name) = lower(T2.Value) and T1.Value = 'ReferenceItemType'
			left join IntersectType I on lower(I.Name) = lower(T2.Value) and T1.Value = 'Intersect'
			left join PolicyType P on lower(P.Name) = lower(T2.Value) and T1.Value = 'Policy'
			left join TaxonomyType T on lower(T.Name) = lower(T2.Value) and T1.Value = 'Taxonomy'
			left join	(
						select 1 as ID, 'informational' as Name
						union
						select 2 as ID, 'quality check' as Name
						union
						select 3 as ID, 'metric' as Name
						union
						select 4 as ID, 'profile' as Name
						) R on lower(R.Name) = lower(T2.Value) and T1.Value = 'Rule'	
end
GO

alter procedure [cache].[ReSynchronizeAllObjectDetails]
as
begin
	set nocount on;

	IF OBJECT_ID('tempdb..#Recache') IS NOT NULL
    DROP TABLE #Recache

	create table #Recache (
		[Object] varchar(50) not null,
		ObjectID int not null,
		ObjectType varchar(25) not null,
		ObjectTypeID int not null
	);

	declare @type varchar(50);
	
	begin
		set @type = 'Artifact'
		insert into #Recache
			SELECT	@type, ID, 'ArtifactType', ArtifactTypeID FROM Artifact;
	end;

	begin
		set @type = 'ArtifactType'
		insert into #Recache
			SELECT	@type, ID, @type, ID FROM ArtifactType;
	end;

	begin
		set @type = 'AttributeType';
		insert into #Recache
			SELECT	@type, ID, 'AttributeType', ID FROM AttributeType;
	end;

	begin
		set @type = 'Group';
		insert into #Recache
			SELECT	@type, ID, 'GroupType', 1 FROM [Group];
	end;

	begin
		set @type = 'Intersect';
		insert into #Recache
			SELECT	@type, ID, 'IntersectType', IntersectTypeID FROM [Intersect];
	end;

	begin
		set @type = 'IntersectType';
		insert into #Recache
			SELECT	@type, ID, @type, ID FROM IntersectType;
	end;

	begin
		set @type = 'Event';
		insert into #Recache
			SELECT	@type, O.ID, 'Rule', R.ID
			FROM	[Event] O
					INNER JOIN EventGroup G on G.ID = O.EventGroupID
					INNER JOIN [Rule] R on R.ID = G.RuleID;
	end;

	begin
		set @type = 'EventGroup';
		insert into #Recache
			SELECT	@type, ID, 'Rule', RuleID FROM EventGroup;
	end;

	begin
		set @type = 'Lookup';
		insert into #Recache
			SELECT	@type, ID, 'LookupType', LookupTypeID FROM [Lookup];
	end;

	begin
		set @type = 'LookupType';
		insert into #Recache
			SELECT	@type, ID, @type, ID FROM LookupType;
	end;

	begin
		set @type = 'Fusion';
		insert into #Recache
			SELECT	@type, ID, 'FusionType', FusionTypeID FROM Fusion;
	end;

	begin
		set @type = 'FusionType';
		insert into #Recache
			SELECT	@type, ID, @type, ID FROM FusionType;
	end;

	begin
		set @type = 'FusionAttribute';
		insert into #Recache
			SELECT	@type, ID, 'FusionAttributeType', FusionAttributeTypeID FROM FusionAttribute;
	end;
 
	begin
		set @type = 'FusionAttributeType';
		insert into #Recache
			SELECT	@type, ID, 'FusionType', FusionTypeID FROM FusionAttributeType;
	end;

	begin
		set @type = 'GroupType';
		insert into #Recache values (@type, 0, @type, 0);
		insert into #Recache values (@type, 1, @type, 0);
	end;

	begin
		set @type = 'Policy';
		insert into #Recache
			SELECT	@type, ID, 'PolicyType', PolicyTypeID FROM [Policy];
	end;

	begin
		set @type = 'PolicyType';
		insert into #Recache
			SELECT	@type, ID, 'PolicyType', ID FROM [PolicyType];
	end;

	begin
		set @type = 'ReferenceItemType';
		insert into #Recache
			SELECT	@type, ID, 'ReferenceItemType', ID FROM ReferenceItemType;
	end;

	begin
		set @type = 'Resource';
		insert into #Recache
			select	@type, ResourceID, 'ResourceType', 1 from reporting.Global_Resource;
	end;

	begin
		set @type = 'ResourceType';
		insert into #Recache values (@type, 0, @type, 0)
		insert into #Recache values (@type, 1, @type, 0)
	end;

	begin
		set @type = 'ResponsibilityType';
		insert into #Recache
			SELECT	@type, ID, @type, 0 FROM ResponsibilityType;
	end;

	begin
		INSERT INTO #Recache VALUES ('RuleType', 1, 'RuleType', 1)
		INSERT INTO #Recache VALUES ('RuleType', 2, 'RuleType', 2)
		INSERT INTO #Recache VALUES ('RuleType', 3, 'RuleType', 3)
		INSERT INTO #Recache VALUES ('RuleType', 4, 'RuleType', 4)

		set @type = 'Rule';
		insert into #Recache
			SELECT	@type, ID, 'RuleType', RuleType FROM [Rule];
	end;

	begin
		set @type = 'Taxonomy';
		insert into #Recache
			SELECT	@type, ID, 'TaxonomyType', TaxonomyTypeID FROM Taxonomy
	end;

	begin
		set @type = 'TaxonomyType';
		insert into #Recache
			SELECT	@type, ID, @type, ID FROM TaxonomyType;
	end;

	-- upsert the individual object into the cache table.
	merge	cache.[Object] as T
	using	(
			SELECT	*
			FROM	#Recache
			) as S
	on		(
			T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
			)
	when matched then
			update	
			set		T.ObjectType = S.ObjectType,
					T.ObjectTypeID = S.ObjectTypeID
	when not matched then
			insert ( [Object], ObjectID, ObjectType, ObjectTypeID )
			values ( S.[Object], S.ObjectID, S.ObjectType, S.ObjectTypeID );
end
GO

alter procedure [cache].[SynchronizeObjectDetails]
--declare 
	@type varchar(50),
	@id int
--set @type = 'IntersectType'
--set @id = 27
as
begin
	set nocount on;

	declare @item table (
		[Object] varchar(50) not null,
		ObjectID int not null,
		ObjectType varchar(25) not null,
		ObjectTypeID int not null
	);

	if @type = 'Artifact'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, 'ArtifactType', O.ArtifactTypeID
			FROM	Artifact O
			WHERE	O.ID = @id
	end;

	if @type = 'ArtifactType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, @type, 0
			FROM	ArtifactType O
			WHERE	O.ID = @id;
	end;

	if @type = 'AttributeType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, 
					'AttributeType', 0
			FROM	AttributeType O
			WHERE	O.ID = @id;
	end;

	if @type = 'Group'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, ID, 'GroupType', 0
			FROM	[Group]
			WHERE	ID = @id;
	end;

	if @type = 'GroupType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			select	@type, 0, @type, 0
	end;


	if @type = 'Intersect'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, 'IntersectType', O.IntersectTypeID
			FROM	[Intersect] O
			WHERE	O.ID = @id;
	end;

	if @type = 'IntersectType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, ID, @type, 0
			FROM	IntersectType
			WHERE	ID = @id;
	end;

	if @type = 'Event'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, 'Rule', R.ID
			FROM	[Event] O
					INNER JOIN EventGroup G on G.ID = O.EventGroupID AND O.ID = @id
					INNER JOIN [Rule] R on R.ID = G.RuleID;
	end;

	if @type = 'EventGroup'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, 'Rule', R.ID
			FROM	EventGroup O
					inner join [Rule] R on R.ID = O.RuleID and O.ID = @id;
	end;

	if @type = 'Lookup'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, 'LookupType', O.LookupTypeID
			FROM	[Lookup] O
					INNER JOIN LookupType T ON O.LookupTypeID = T.ID AND O.ID = @id;
	end;

	if @type = 'LookupType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, ID, @type, 0
			FROM	LookupType
			WHERE	ID = @id;
	end;

	if @type = 'Fusion'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, 'FusionType', O.FusionTypeID
			FROM	Fusion O
			WHERE	O.ID = @id;
	end;

	if @type = 'FusionType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, ID, @type, 0
			FROM	FusionType
			WHERE	ID = @id;
	end;

	if @type = 'FusionAttribute'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, 'FusionAttributeType', O.FusionAttributeTypeID
			FROM	FusionAttribute O
			WHERE	O.ID = @id;
	end;

	if @type = 'FusionAttributeType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, 'FusionType', O.FusionTypeID
			FROM	FusionAttributeType O
			WHERE	O.ID = @id;
	end;

	if @type = 'Policy'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, 'PolicyType', O.PolicyTypeID
			FROM	[Policy] O
			WHERE	O.ID = @id;
	end;

	if @type = 'PolicyType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, T.ID, @type, T.PolicyTypeClassID
			FROM	PolicyType T
			WHERE	T.ID = @id;
	end;

	if @type = 'ReferenceItemType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, T.ID, @type, 0
			FROM	ReferenceItemType T
			WHERE	T.ID = @id;
	end;

	if @type = 'Resource'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			select	@type, ResourceID, 'ResourceType', 1
			from	reporting.Global_Resource 
			where	ResourceID = @id;
	end;

	if @type = 'ResourceType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			select	@type, 1, @type, 0
	end;

	if @type = 'ResponsibilityType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, ID, @type, 0
			FROM	ResponsibilityType
			WHERE	ID = @id;

		--UPDATE	T
		--SET		T.ResponsibilityType = S.Name
		--FROM	cache.ResponsibilityItem T INNER JOIN @item S ON S.[Object] = @type and S.ObjectID = T.ResponsibilityTypeID
	end;

	if @type = 'Rule'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, 'RuleType', O.RuleType
			FROM	[Rule] O
			WHERE	O.ID = @id;
	end;

	if @type = 'Taxonomy'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, 'TaxonomyType', O.TaxonomyTypeID
			FROM	Taxonomy O
			WHERE	O.ID = @id;
	end;

	if @type = 'TaxonomyType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, T.ID, @type, T.TaxonomyTypeClassID
			FROM	TaxonomyType T
			WHERE	T.ID = @id;
	end;

	-- upsert the individual object into the cache table.
	merge	cache.[Object] as T
	using	(
			SELECT	*
			FROM	@item
			) as S
	on		(
			T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
			)
	when matched then
			update	
			set		T.ObjectType = S.ObjectType, 
					T.ObjectTypeID = S.ObjectTypeID
	when not matched then
			insert ( [Object], ObjectID, ObjectType, ObjectTypeID )
			values ( S.[Object], S.ObjectID, S.ObjectType, S.ObjectTypeID );
end
GO

alter procedure [dbo].[AsyncDeleteObject]
	@Object varchar(50),
	@ObjectID int,
	@ParentObject varchar(50),
	@ParentObjectID int,
	@ResourceID int
as
begin
	set nocount on;
	
	declare @trans varchar(25) = 'Trans',
			@current int = 1,
			@max int,
			@date datetime = getutcdate()

	begin try
		exec [utility].[AddAuditEntry] @ParentObject, @ParentObjectID, @ResourceID, @date, 'Removed', @Object, @ObjectID
	end try
	begin catch

	end catch

	begin try
		begin transaction @trans

		--INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID], [Priority])
		--values ('ObjectIndex', 'D', @Object, @ObjectID, 4)

		--COMMON
		delete CommentRelation					where ObjectType = @Object and ObjectID = @ObjectID
		delete Field							where ObjectType = @Object and ObjectID = @ObjectID
		delete Follow							where ObjectType = @Object and ObjectID = @ObjectID
		delete Responsibility					where ObjectType = @Object and ObjectID = @ObjectID
		--delete SurveyObjectCache				where ObjectType = @Object and ObjectID = @ObjectID
		delete cache.[Object]					where [Object] = @Object and ObjectID = @ObjectID

		if charindex('Type', @Object) > 0
		begin
			delete AttributeTypeRelation			where ObjectType = @Object AND ObjectID = @ObjectID
			delete FieldType						where [Object] = @Object AND ObjectID = @ObjectID
			delete ResponsibilityTypeRelation		where ObjectType = @Object and ObjectID = @ObjectID
			delete ResponsibilityTypeObjectClaim	where ObjectType = @Object and ObjectID = @ObjectID
			delete StatisticType					where [Object] = @Object and ObjectID = @ObjectID
			delete WorkflowTypeRelation				where [Object] = @Object and ObjectID = @ObjectID

			if @Object in ('AttributeTypeRelation', 'AttributeTypeRelation', 'ResponsibilityTypeRelation', 'ResponsibilityType')
			begin
				exec utility.CalculateStatistics
			end

			if @Object = 'ArtifactType'
			begin
				declare @ah table (ID int);
				with ah as	(
							select	ID, 
									ParentID
							from	Artifact
							where	ArtifactTypeID = @ObjectID
							union all
							select	C.ID,
									C.ParentID
							from	Artifact C
									inner join ah P on P.ID = C.ParentID
							)
				insert into @ah 
					select ID from ah
			
				delete Artifact where ID in (select ID from @ah)
			end

			if @Object = 'AttributeType'
			begin
				delete AttributeTypeRelation		where AttributeTypeID = @ObjectID
			
				declare @ath table (RowID int identity, ID int, ParentID int null, [Level] int);
				with ath as	(
							select	ID,
									ParentID,
									1 as [Level]
							from	AttributeType
							where	ID = @ObjectID
							union all
							select	C.ID,
									C.ParentID,
									P.[Level] + 1 as [Level]
							from	AttributeType C
									inner join ath P on P.ID = C.ParentID
							)
				insert into @ath 
					select ID, ParentID, [Level] from ath order by [Level] desc

				select @max = max(RowID) from @ath

				while @current <= @max
				begin
					declare @attributeTypeID int
					select @attributeTypeID = ID from @ath where RowID = @current
					delete Attribute where AttributeTypeID = @attributeTypeID
					delete AttributeType where ID = @attributeTypeID
					set @current = @current + 1
				end
			end

			if @Object = 'FieldType'
			begin
				delete Field where FieldTypeID = @ObjectID
				delete FieldTypeFusionLookupDisplayField where FieldTypeID = @ObjectID
				delete FieldType where ID = @ObjectID
			end

			if @Object = 'FusionAttributeType'
			begin
				declare @fath table (RowID int identity, ID int, ParentID int null, [Level] int);
				with fath as	(
							select	ID,
									ParentID,
									1 as [Level]
							from	FusionAttributeType
							where	ID = @ObjectID
							union all
							select	C.ID,
									C.ParentID,
									P.[Level] + 1 as [Level]
							from	FusionAttributeType C
									inner join fath P on P.ID = C.ParentID
							)
				insert into @fath 
					select ID, ParentID, [Level] from fath order by [Level] desc

				select @max = max(RowID) from @fath

				while @current <= @max
				begin
					declare @fusionAttributeTypeID int
					select @fusionAttributeTypeID = ID from @fath where RowID = @current
					delete FusionAttribute where FusionAttributeTypeID = @fusionAttributeTypeID
					delete FusionAttributeType where ID = @fusionAttributeTypeID
					set @current = @current + 1
				end
			end

			if @Object = 'FusionType'
			begin
				declare @fth table (RowID int identity, ID int, ParentID int null, [Level] int);
				with fth as	(
							select	ID,
									ParentID,
									1 as [Level]
							from	FusionAttributeType
							where	FusionTypeID = @ObjectID and ParentID is null
							union all
							select	C.ID,
									C.ParentID,
									P.[Level] + 1 as [Level]
							from	FusionAttributeType C
									inner join fth P on P.ID = C.ParentID
							)
				insert into @fth 
					select ID, ParentID, [Level] from fth order by [Level] desc

				select @max = max(RowID) from @fth

				while @current <= @max
				begin
					declare @fattributeTypeID int
					select @fattributeTypeID = ID from @fth where RowID = @current
					delete FusionAttribute where FusionAttributeTypeID = @fattributeTypeID
					delete FusionAttributeType where ID = @fattributeTypeID
					set @current = @current + 1
				end
				delete FusionType where ID = @ObjectID
			end

			if @Object = 'IntersectType'
			begin
				delete [Intersect] where IntersectTypeID = @ObjectID
				delete IntersectType where ID = @ObjectID
			end

			if @Object = 'LookupType'
			begin
				delete [Lookup] where LookupTypeID = @ObjectID
			end

			if @Object = 'PolicyType'
			begin
				delete Policy where PolicyTypeID = @ObjectID
				delete PolicyTypeLevel where PolicyTypeID = @ObjectID
			end

			if @Object = 'ResponsibilityType'
			begin
				delete Responsibility where ResponsibilityTypeID = @ObjectID
				delete ResponsibilityType where ID = @ObjectID
			end

			if @Object = 'SurveyType'
			begin
				--delete SurveyObjectCache where SurveyTypeID = @ObjectID
				delete Survey where SurveyTypeID = @ObjectID
				delete SurveyType where ID = @ObjectID
			end

			if @Object = 'TaxonomyType'
			begin
				delete Taxonomy where TaxonomyTypeID = @ObjectID
				delete TaxonomyTypeLevel where TaxonomyTypeID = @ObjectID
				delete TaxonomyType where ID = @ObjectID
			end

		end
		else
		begin
			delete Attribute							where ObjectType = @Object and ObjectID = @ObjectID

			BEGIN TRY
				DECLARE @tblIntersectIDs table (ID int)

				INSERT INTO @tblIntersectIDs
					SELECT	ID
					FROM	[Intersect]
					WHERE	(Subject = @Object and SubjectID = @ObjectID) OR (Object = @Object and ObjectID = @ObjectID)

				delete	MapItem 
				where	SourceIntersectID in (select ID from @tblIntersectIDs) OR
						TargetIntersectID in (select ID from @tblIntersectIDs)

				delete [Intersect] where ID in (select ID from @tblIntersectIDs)
			END TRY
			BEGIN CATCH

			END CATCH

			if @Object = 'Responsibility'
			begin
				exec cache.SynchronizeResponsibilitiesForObject @ParentObject, @ParentObjectID 
			end

			if @Object = 'Taxonomy'
			begin
				declare @th table (ID int);
				with th as	(
							select	ID, 
									ParentID
							from	Taxonomy
							where	ID = @ObjectID
							union all
							select	C.ID,
									C.ParentID
							from	Taxonomy C
									inner join th P on P.ID = C.ParentID
							)
				insert into @th 
					select ID from th
			
				delete Taxonomy where ID in (select ID from @th)

				exec cache.SynchronizeResponsibilities
			end
		end
		
		commit transaction @trans
	end try
	begin catch
		DECLARE @ErrorMessage NVARCHAR(4000);
		DECLARE @ErrorSeverity INT;
		DECLARE @ErrorState INT;

		SELECT 
			@ErrorMessage = ERROR_MESSAGE(),
			@ErrorSeverity = ERROR_SEVERITY(),
			@ErrorState = ERROR_STATE();

		-- Use RAISERROR inside the CATCH block to return error
		-- information about the original error that caused
		-- execution to jump to the CATCH block.
		RAISERROR (@ErrorMessage, -- Message text.
				   @ErrorSeverity, -- Severity.
				   @ErrorState -- State.
				   );

		rollback transaction @trans
	end catch
end
GO

alter procedure [dbo].[DeleteObject]
	@Obj varchar(50),
	@ObjectID int,
	@ResourceID int
as
begin
	set nocount on;
	
	declare @Object varchar(50) = @Obj,
			@trans varchar(25) = 'Trans',
			@current int = 1,
			@max int

	begin try
		begin transaction @trans

		INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        VALUES (
				'ObjectVersion', 
				'<fields>
				 <Action>Removed</Action>
				 <ActionObject>' + @Obj + '</ActionObject>
				 <ActionObjectID>' + cast(@ObjectID as varchar) + '</ActionObjectID>
				 <ResourceID>' + cast(@ResourceID as varchar) + '</ResourceID>
				</fields>', 
				@Obj, 
				@ObjectID)

		INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		values ('ObjectIndex', 'D', @Obj, @ObjectID)

		--COMMON
		delete CommentRelation					where ObjectType = @Object and ObjectID = @ObjectID
		delete Field							where ObjectType = @Object and ObjectID = @ObjectID
		delete Follow							where ObjectType = @Object and ObjectID = @ObjectID
		delete Responsibility					where ObjectType = @Object and ObjectID = @ObjectID
		--delete SurveyObjectCache				where ObjectType = @Object and ObjectID = @ObjectID
		delete cache.[Object]					where [Object] = @Object and ObjectID = @ObjectID

		if charindex('Type', @Object) > 0
		begin
			delete AttributeTypeRelation			where ObjectType = @Object AND ObjectID = @ObjectID
			delete FieldType						where [Object] = @Object AND ObjectID = @ObjectID
			delete ResponsibilityTypeRelation		where ObjectType = @Object and ObjectID = @ObjectID
			delete ResponsibilityTypeObjectClaim	where ObjectType = @Object and ObjectID = @ObjectID
			delete StatisticType					where [Object] = @Object and [ObjectID] = @ObjectID
			delete WorkflowTypeRelation				where [Object] = @Object and ObjectID = @ObjectID

			if @Object = 'ArtifactType'
			begin
				declare @ah table (ID int);
				with ah as	(
							select	ID, 
									ParentID
							from	Artifact
							where	ArtifactTypeID = @ObjectID
							union all
							select	C.ID,
									C.ParentID
							from	Artifact C
									inner join ah P on P.ID = C.ParentID
							)
				insert into @ah 
					select ID from ah
			
				delete Artifact where ID in (select ID from @ah)
			end

			if @Object = 'AttributeType'
			begin
				delete AttributeTypeRelation		where AttributeTypeID = @ObjectID
			
				declare @ath table (RowID int identity, ID int, ParentID int null, [Level] int);
				with ath as	(
							select	ID,
									ParentID,
									1 as [Level]
							from	AttributeType
							where	ID = @ObjectID
							union all
							select	C.ID,
									C.ParentID,
									P.[Level] + 1 as [Level]
							from	AttributeType C
									inner join ath P on P.ID = C.ParentID
							)
				insert into @ath 
					select ID, ParentID, [Level] from ath order by [Level] desc

				select @max = max(RowID) from @ath

				while @current <= @max
				begin
					declare @attributeTypeID int
					select @attributeTypeID = ID from @ath where RowID = @current
					delete Attribute where AttributeTypeID = @attributeTypeID
					delete AttributeType where ID = @attributeTypeID
					set @current = @current + 1
				end
			end

			if @Object = 'FieldType'
			begin
				delete Field where FieldTypeID = @ObjectID
				delete FieldType where ID = @ObjectID
			end

			if @Object = 'FusionAttributeType'
			begin
				declare @fath table (RowID int identity, ID int, ParentID int null, [Level] int);
				with fath as	(
							select	ID,
									ParentID,
									1 as [Level]
							from	FusionAttributeType
							where	ID = @ObjectID
							union all
							select	C.ID,
									C.ParentID,
									P.[Level] + 1 as [Level]
							from	FusionAttributeType C
									inner join fath P on P.ID = C.ParentID
							)
				insert into @fath 
					select ID, ParentID, [Level] from fath order by [Level] desc

				select @max = max(RowID) from @fath

				while @current <= @max
				begin
					declare @fusionAttributeTypeID int
					select @fusionAttributeTypeID = ID from @fath where RowID = @current
					delete FusionAttribute where FusionAttributeTypeID = @fusionAttributeTypeID
					delete FusionAttributeType where ID = @fusionAttributeTypeID
					set @current = @current + 1
				end
			end

			if @Object = 'FusionType'
			begin
				declare @fth table (RowID int identity, ID int, ParentID int null, [Level] int);
				with fth as	(
							select	ID,
									ParentID,
									1 as [Level]
							from	FusionAttributeType
							where	FusionTypeID = @ObjectID and ParentID is null
							union all
							select	C.ID,
									C.ParentID,
									P.[Level] + 1 as [Level]
							from	FusionAttributeType C
									inner join fth P on P.ID = C.ParentID
							)
				insert into @fth 
					select ID, ParentID, [Level] from fth order by [Level] desc

				select @max = max(RowID) from @fth

				while @current <= @max
				begin
					declare @fattributeTypeID int
					select @fattributeTypeID = ID from @fth where RowID = @current
					delete FusionAttribute where FusionAttributeTypeID = @fattributeTypeID
					delete FusionAttributeType where ID = @fattributeTypeID
					set @current = @current + 1
				end
				delete FusionType where ID = @ObjectID
			end

			if @Object = 'IntersectType'
			begin
				delete [Intersect] where IntersectTypeID = @ObjectID
				delete IntersectType where ID = @ObjectID
			end

			if @Object = 'LookupType'
			begin
				delete [Lookup] where LookupTypeID = @ObjectID
			end

			if @Object = 'PolicyType'
			begin
				delete Policy where PolicyTypeID = @ObjectID
				delete PolicyTypeLevel where PolicyTypeID = @ObjectID
			end

			if @Object = 'ResponsibilityType'
			begin
				delete Responsibility where ResponsibilityTypeID = @ObjectID
				delete ResponsibilityType where ID = @ObjectID
			end

			--if @Object = 'StatisticType'
			--begin
			--	delete [Statistic] where StatisticTypeID = @ObjectID
			--end

			if @Object = 'SurveyType'
			begin
				--delete SurveyObjectCache where SurveyTypeID = @ObjectID
				delete Survey where SurveyTypeID = @ObjectID
				delete SurveyType where ID = @ObjectID
			end

			if @Object = 'TaxonomyType'
			begin
				delete Taxonomy where TaxonomyTypeID = @ObjectID
				delete TaxonomyTypeLevel where TaxonomyTypeID = @ObjectID
				delete TaxonomyType where ID = @ObjectID
			end

		end
		else
		begin
			delete Attribute							where ObjectType = @Object and ObjectID = @ObjectID

			BEGIN TRY
				DECLARE @tblIntersectIDs table (ID int)

				INSERT INTO @tblIntersectIDs
					SELECT	ID
					FROM	[Intersect]
					WHERE	(Subject = @Object and SubjectID = @ObjectID) OR (Object = @Object and ObjectID = @ObjectID)

				delete	MapItem 
				where	SourceIntersectID in (select ID from @tblIntersectIDs) OR
						TargetIntersectID in (select ID from @tblIntersectIDs)

				delete [Intersect] where ID in (select ID from @tblIntersectIDs)
			END TRY
			BEGIN CATCH

			END CATCH

			if @Object = 'Taxonomy'
			begin
				declare @th table (ID int);
				with th as	(
							select	ID, 
									ParentID
							from	Taxonomy
							where	ID = @ObjectID
							union all
							select	C.ID,
									C.ParentID
							from	Taxonomy C
									inner join th P on P.ID = C.ParentID
							)
				insert into @th 
					select ID from th
			
				delete Taxonomy where ID in (select ID from @th)
			end
		end
		
		commit transaction @trans
	end try
	begin catch
		 DECLARE @ErrorMessage NVARCHAR(4000);
    DECLARE @ErrorSeverity INT;
    DECLARE @ErrorState INT;

    SELECT 
        @ErrorMessage = ERROR_MESSAGE(),
        @ErrorSeverity = ERROR_SEVERITY(),
        @ErrorState = ERROR_STATE();

    -- Use RAISERROR inside the CATCH block to return error
    -- information about the original error that caused
    -- execution to jump to the CATCH block.
    RAISERROR (@ErrorMessage, -- Message text.
               @ErrorSeverity, -- Severity.
               @ErrorState -- State.
               );

		rollback transaction @trans
	end catch
end
GO

ALTER PROCEDURE [dbo].[GetRenderedTemplateBody]-- 'Tooltip', 'Resource', 2, 'Preview'
--declare
	@TemplateType varchar(25),
	@Type varchar(50),
	@ID int,
	@Action varchar(50)
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

	if @TemplateType = 'Email'
	begin
		select	@html = TemplateBody
		from	EmailTemplate
		where	Name = @Type
				and [Action] = @Action
	end

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
	select	@typeID = ObjectTypeID,
			@icon = '<div title=''' + ObjectTypeName + ''' class=''tooltip-icon'' style=''background-color: ' + IconBackColor + '; color: ' + IconForeColor + '''><i class=''fa fa-' + IconText + '''></i></div>',
			@n = Name,
			@t = ObjectTypeName,
			@d = Description,
			@link = Url
	from	cache.ObjectDetails
	where	[Object] = @Type
			and ObjectID = @ID;

	if @n is not null
	begin
		if @link is null
		begin
			insert into @tbl values ('Name', @n)
		end
		else
		begin
			insert into @tbl values ('Name', '<a href="' + @link + '">' + @n + '</a>')
		end
		insert into @tbl values ('Description', @d)
	end
	insert into @tbl values ('Type', @t)

	if @Action = 'AssigningItemPreview'
	begin
		set @html = '<h3>{Name}</h3>'
	end

	if @Action = 'Certificate'
	begin
		set @html = '<h3>{Name}</h3>'

		declare @workflowID uniqueidentifier,
				@dateCertifiedOn varchar(10),
				@certifiers nvarchar(2500),
				@status varchar(50),
				@certIconColor varchar(10)

		select	@dateCertifiedOn = CONVERT(VARCHAR(10), DateLastCertified, 101),
				@status = Status
		from	Artifact A
		where	A.ID = @ID

		SELECT	@workflowID = W.ID,
				@certifiers = COALESCE(@certifiers + ', ', '') + R.FirstName + ' ' + R.LastName 
		from	(
				select		top 1
							ID,
							Data.value('(/fields/ArtifactID)[1]', 'int') as ArtifactID,
							DateCompleted
				from		Workflow
				where		WorkflowType = 2
							and Data.exist('/fields/ArtifactID[text() = sql:variable("@ID")]') = 1
				order by	DateCompleted desc
				) W
				inner join WorkflowResource WR on WR.WorkflowID = W.ID
				inner join reporting.Global_Resource R on R.ResourceID = WR.ResourceID

		if @dateCertifiedOn is null and @status != 'Certified'
			begin
				set @showIcon = 0

				set @html = @html + '<div><b>Not yet certified</b></div>'
				if @certifiers is not null
				begin
					set @html = @html + '<div>Certifying Users: {Certifiers}</div>'
				end
				if @workflowID is not null
				begin
					set @html = @html + '<div><a class=''btn btn-info'' href=''#/workflow/' + cast(@workflowID as varchar(50)) + '/status''>Go to this workflow status</a>.</div>'
				end
			end
		else
			begin
				if @status = 'Certified'
					begin
						set @certIconColor = '#EFC43D'
					end
				else 
					begin
						set @certIconColor = '#FFE183'
					end
				select	@icon = '<div style="background-color: transparent; color: ' + @certIconColor + '"><i class="fa fa-2x fa-certificate"></i></div>'
				set @html = @html + '<div>Last Certified On: ';
				if @dateCertifiedOn is null
					begin
						set @html = @html + 'Manually Certified';
					end
				else
					begin
						set @html = @html + '{CertifiedOn}';
					end
				set @html = @html + '</div>';
				if @status = 'Certified'
					begin
						if @Certifiers is not null
						begin
							set @html = @html + '<div>Certified By: {Certifiers}</div>'
						end
					end
				else 
					begin
						set @html = @html + '<div>Currently Under Certification Review</div>'
						set @html = @html + '<div>Certifying Users: {Certifiers}</div>'
						if @workflowID is not null
						begin
							set @html = @html + '<div><a class=''btn btn-info'' href=''#/workflow/' + cast(@workflowID as varchar(50)) + '/status''>Go to this workflow status</a>.</div>'
						end
					end
			end

		insert into @tbl values ('CertifiedOn', @dateCertifiedOn)
		insert into @tbl values ('Certifiers', @certifiers)
	end
	if @Action = 'JoinRequest'
	begin
		set @html = ''
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

	end
	
	if @Action = 'None'
	begin
		set @html = '<h3>{Name}</h3><div>'
	end

	if @Action = 'Preview'
	begin
		set @html = '<h3>{Name} <small>{Type}</small></h3><div>{Description}</div>'
		set @showIcon = 0

		if @Type = 'Artifact'
		begin
			insert into @tbl
			select	'Status', [Status]
			from	Artifact
			where	ID = @ID

			insert into @tbl
			select	'Path', TextPath
			from	Artifact
			where	ID = @ID

			set @html = @html + '<div><b>Status:</b> {Status}</div>'
			set @html = @html + '<div><b>Path:</b> {Path}</div>'

			set @hasDynamicFields = 1
		end;

		if @Type = 'Event'
		begin
			declare @so nvarchar(250)
			select	@so = SourceID, 
					@s = [Status]
			from	[Event]
			where	ID = @ID

			insert into @tbl values ('Status', @s)
			insert into @tbl values ('SourceID', @so)

			set @html = @html + '<div><b>Status:</b> {Status}</div>'
			set @html = @html + '<div><b>SourceID:</b> {SourceID}</div>'

			set @hasDynamicFields = 1
		end;

		if @Type = 'EventGroup'
		begin
			insert into @tbl
				select	'Key', PublicID
				from	EventGroup
				where	ID = @ID

			-- BUILD EVENT LIST HTML -----------------------------------------
			declare @events table (ID int, SourceID nvarchar(250), Status varchar(25))
			insert into @events 
				select top 10 ID, SourceID, Status from [Event] where EventGroupID = @ID order by ID desc
		
			declare @eventFieldTypes table (ID int identity, Name nvarchar(250))
			insert into @eventFieldTypes
				select FriendlyName from FieldType where [Object] = 'Rule' and ObjectID = @typeID order by SortOrder asc
			insert into @eventFieldTypes values ('Source ID')
			insert into @eventFieldTypes values ('Status')

			declare @eventHtml nvarchar(max)

			set @eventHtml = '<table class="hoverable bordered striped" style="width:100%">'

			-- Loop through field name list ---------
			set @eventHtml = @eventHtml + '<thead>'
			set		@current = 1
			select	@max = max(ID) from @eventFieldTypes
			while @current <= @max
			begin
				select	@name = Name
				from	@eventFieldTypes
				where	ID = @current

				set @eventHtml = @eventHtml + '<th>' + @name  + '</th>'

				set @current = @current + 1
			end
			set @eventHtml = @eventHtml + '</thead>'
			-----------------------------------------

			set @eventHtml = @eventHtml + '<tbody>'

			-- Loop through event list --------------
			select	@current = min(ID) from @events
			select	@max = max(ID) from @events

			while @current <= @max
			begin
				set @eventHtml = @eventHtml + '<tr>'	-- Open row for selected event.

				declare @eventFields table (Name nvarchar(250), Value nvarchar(4000))
			
				insert into @eventFields
					select		FriendlyName,
								FormattedValue
					from		FieldWithRelation
					where		ObjectType = 'Event' 
								and ObjectID = @current

					-- Loop through each field for this selected event --
					declare @fCurrent int,
							@fMax int
					set		@fCurrent = 1
					select	@fMax = max(ID) from @eventFieldTypes
					while @fCurrent <= @fMax
					begin
						select	@name = Name from @eventFieldTypes where ID = @fCurrent

						select @eventHtml = @eventHtml + '<td>' + coalesce(Value, '') + '</td>' from @eventFields where Name = @name

						set @fCurrent = @fCurrent + 1
					end
					-----------------------------------------------------

					select @eventHtml = @eventHtml	+ 
										'<td>' + [SourceID] + '</td>' + 
										'<td>' + [Status] + '</td>' 
					from	@events 
					where	ID = @current

				delete @eventFields

				set @eventHtml = @eventHtml + '</tr>'	-- Close off row for selected event.

				set @current = @current + 1
			end
			-----------------------------------------

			set @eventHtml = @eventHtml + '</tbody>'

			set @eventHtml = @eventHtml + '</table>'

			insert into @tbl values ('Items', @eventHtml)

			set @html = @html + '<div><b>Key:</b> {Key}</div>'
			set @html = @html + '<div>Items: {Items}</div>'
			------------------------------------------------------------------
		end;

		if @Type = 'Intersect'
		begin
			insert into @tbl
				select	'Classification',
						case Classification
							when 1 then 'Critical'
							else 'Normal'
						end
				from	[Intersect]
				where	ID = @ID

			set @html = @html + '<div><b>Classification:</b> {Classification}</div>'
		end;

		if @Type = 'Responsibility'
		begin
			select	@n = T.Name, 
					@t = T.Name,
					@d = T.[Description]
			from	Responsibility O
					inner join ResponsibilityType T on T.ID = O.ResponsibilityTypeID
			where	O.ID = @ID

			declare @contextsHtml nvarchar(max)
			set @contextsHtml = '<table class="hoverable bordered striped" style="width:100%">' + 
								'<thead><th>List</th><th>Code</th></thead>' + 
								'<tbody>' + 
								(
								select		(select D.Name as 'td' for xml path(''), type),
											(select I.Code as 'td' for xml path(''), type)
								from		ResponsibilityContextItem R
											inner join ReferenceItem I on R.ResponsibilityID = @ID and R.ObjectType = 'ReferenceItem' and I.ID = R.ObjectID
											inner join ReferenceItemType D on D.ID = I.ReferenceItemTypeID
								FOR XML RAW('tr'), ELEMENTS
								) +
								'</tbody>' + 
								'</table>'

			insert into @tbl values ('Name', @n)
			insert into @tbl values ('Type', @t)
			insert into @tbl values ('Description', @d)
			insert into @tbl values ('Contexts', @contextsHtml)

			set @html = @html + '<div><b>Contexts:</b> {Contexts}</div>'
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
				select	'TextPath', TextPath
				from	Taxonomy O
				where	ID = @ID

			set @html = @html + '<div><b>Path:</b> {TextPath}</div>'

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
			insert into @tbl
				select	'TextPath', TextPath
				from	Taxonomy O
				where	ID = @ID

			set @html = @html + '<div><b>Path:</b> {TextPath}</div>'

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

			declare @stats table (ID int identity, Name nvarchar(250), Score int)
			insert into @stats 
				select	T.Name,
						coalesce(S.SCore, 0) as Score
				from	StatisticType T
						outer apply (
									select	top 1
											*
									from	Statistic
									where	StatisticTypeID = T.ID
											and ObjectType = @Type
											and ObjectID = @ID
									order by DateStart desc
									) S
				where	T.[Object] = @Type + 'Type' 
						and T.ObjectID = @typeID
						and T.PartOfScore = 1

			set @statsHtml = '<table class="hoverable bordered striped" style="width:100%">'

			-- Loop through field name list ---------
			set @statsHtml = @statsHtml + '<tbody>'
			set		@current = 1
			select	@max = max(ID) from @stats
			while @current <= @max
			begin
				select	@statsHtml = @statsHtml + '<tr><td>' + Name  + '</td>' + '<td>' + cast(Score as varchar(5))  + ' Points</td></tr>'
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

	-- Return the properly formatted values.
	select	'' as Title,
			@html as Body;
END
GO

ALTER PROCEDURE [dbo].[GetRenderedTemplateBodyNg]-- 'Tooltip', 'Resource', 2, 'Preview'
--declare
	@TemplateType varchar(25),
	@Type varchar(50),
	@ID int,
	@Action varchar(50)
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

	if @TemplateType = 'Email'
	begin
		select	@html = TemplateBody
		from	EmailTemplate
		where	Name = @Type
				and [Action] = @Action
	end

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
	select	@typeID = ObjectTypeID,
			@icon = '<div title=''' + ObjectTypeName + ''' class=''tooltip-icon'' style=''background-color: ' + IconBackColor + '; color: ' + IconForeColor + '''><i class=''fa fa-' + IconText + '''></i></div>',
			@n = Name,
			@t = ObjectTypeName,
			@d = Description,
			@link = NgUrl
	from	cache.ObjectDetails
	where	[Object] = @Type
			and ObjectID = @ID;

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

	if @Action = 'Certificate'
	begin
		set @html = '<h3>{Name}</h3>'

		declare @workflowID uniqueidentifier,
				@dateCertifiedOn varchar(10),
				@certifiers nvarchar(2500),
				@status varchar(50),
				@certIconColor varchar(10)

		select	@dateCertifiedOn = CONVERT(VARCHAR(10), DateLastCertified, 101),
				@status = Status
		from	Artifact A
		where	A.ID = @ID

		SELECT	@workflowID = W.ID,
				@certifiers = COALESCE(@certifiers + ', ', '') + R.FirstName + ' ' + R.LastName 
		from	(
				select		top 1
							ID,
							Data.value('(/fields/ArtifactID)[1]', 'int') as ArtifactID,
							DateCompleted
				from		Workflow
				where		WorkflowType = 2
							and Data.exist('/fields/ArtifactID[text() = sql:variable("@ID")]') = 1
				order by	DateCompleted desc
				) W
				inner join WorkflowResource WR on WR.WorkflowID = W.ID
				inner join reporting.Global_Resource R on R.ResourceID = WR.ResourceID

		if @dateCertifiedOn is null and @status != 'Certified'
			begin
				set @showIcon = 0

				set @html = @html + '<div><b>Not yet certified</b></div>'
				if @certifiers is not null
				begin
					set @html = @html + '<div>Certifying Users: {Certifiers}</div>'
				end
				if @workflowID is not null
				begin
					set @html = @html + '<div><a class=''btn btn-info'' routerLink=''/workflow/status/' + cast(@workflowID as varchar(50)) + '''>Go to this workflow status</a>.</div>'
				end
			end
		else
			begin
				if @status = 'Certified'
					begin
						set @certIconColor = '#EFC43D'
					end
				else 
					begin
						set @certIconColor = '#FFE183'
					end
				select	@icon = '<div style="background-color: transparent; color: ' + @certIconColor + '"><i class="fa fa-2x fa-certificate"></i></div>'
				set @html = @html + '<div>Last Certified On: ';
				if @dateCertifiedOn is null
					begin
						set @html = @html + 'Manually Certified';
					end
				else
					begin
						set @html = @html + '{CertifiedOn}';
					end
				set @html = @html + '</div>';
				if @status = 'Certified'
					begin
						if @Certifiers is not null
						begin
							set @html = @html + '<div>Certified By: {Certifiers}</div>'
						end
					end
				else 
					begin
						set @html = @html + '<div>Currently Under Certification Review</div>'
						set @html = @html + '<div>Certifying Users: {Certifiers}</div>'
						if @workflowID is not null
						begin
							set @html = @html + '<div><a class=''btn btn-info'' routerLink=''/workflow/status/' + cast(@workflowID as varchar(50)) + '''>Go to this workflow status</a>.</div>'
						end
					end
			end

		insert into @tbl values ('CertifiedOn', @dateCertifiedOn)
		insert into @tbl values ('Certifiers', @certifiers)
	end
	if @Action = 'JoinRequest'
	begin
		set @html = ''
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
		set @html = '<h3>{Name} <small style="right: 5px;">{Type}</small></h3><div>{Description}</div>'
		set @showIcon = 0

		if @Type = 'Artifact'
		begin
			insert into @tbl
			select	'Status', [Status]
			from	Artifact
			where	ID = @ID

			insert into @tbl
			select	'Path', TextPath
			from	Artifact
			where	ID = @ID

			set @html = @html + '<div><b>Status:</b> {Status}</div>'
			set @html = @html + '<div><b>Path:</b> {Path}</div>'

			set @hasDynamicFields = 1
		end;

		if @Type = 'Event'
		begin
			declare @so nvarchar(250)
			select	@so = SourceID, 
					@s = [Status]
			from	[Event]
			where	ID = @ID

			insert into @tbl values ('Status', @s)
			insert into @tbl values ('SourceID', @so)

			set @html = @html + '<div><b>Status:</b> {Status}</div>'
			set @html = @html + '<div><b>SourceID:</b> {SourceID}</div>'

			set @hasDynamicFields = 1
		end;

		if @Type = 'EventGroup'
		begin
			insert into @tbl
				select	'Key', PublicID
				from	EventGroup
				where	ID = @ID

			-- BUILD EVENT LIST HTML -----------------------------------------
			declare @events table (ID int, SourceID nvarchar(250), Status varchar(25))
			insert into @events 
				select top 10 ID, SourceID, Status from [Event] where EventGroupID = @ID order by ID desc
		
			declare @eventFieldTypes table (ID int identity, Name nvarchar(250))
			insert into @eventFieldTypes
				select FriendlyName from FieldType where [Object] = 'Rule' and ObjectID = @typeID order by SortOrder asc
			insert into @eventFieldTypes values ('Source ID')
			insert into @eventFieldTypes values ('Status')

			declare @eventHtml nvarchar(max)

			set @eventHtml = '<table class="hoverable bordered striped" style="width:100%">'

			-- Loop through field name list ---------
			set @eventHtml = @eventHtml + '<thead>'
			set		@current = 1
			select	@max = max(ID) from @eventFieldTypes
			while @current <= @max
			begin
				select	@name = Name
				from	@eventFieldTypes
				where	ID = @current

				set @eventHtml = @eventHtml + '<th>' + @name  + '</th>'

				set @current = @current + 1
			end
			set @eventHtml = @eventHtml + '</thead>'
			-----------------------------------------

			set @eventHtml = @eventHtml + '<tbody>'

			-- Loop through event list --------------
			select	@current = min(ID) from @events
			select	@max = max(ID) from @events

			while @current <= @max
			begin
				set @eventHtml = @eventHtml + '<tr>'	-- Open row for selected event.

				declare @eventFields table (Name nvarchar(250), Value nvarchar(4000))
			
				insert into @eventFields
					select		FriendlyName,
								FormattedValue
					from		FieldWithRelation
					where		ObjectType = 'Event' 
								and ObjectID = @current

					-- Loop through each field for this selected event --
					declare @fCurrent int,
							@fMax int
					set		@fCurrent = 1
					select	@fMax = max(ID) from @eventFieldTypes
					while @fCurrent <= @fMax
					begin
						select	@name = Name from @eventFieldTypes where ID = @fCurrent

						select @eventHtml = @eventHtml + '<td>' + coalesce(Value, '') + '</td>' from @eventFields where Name = @name

						set @fCurrent = @fCurrent + 1
					end
					-----------------------------------------------------

					select @eventHtml = @eventHtml	+ 
										'<td>' + [SourceID] + '</td>' + 
										'<td>' + [Status] + '</td>' 
					from	@events 
					where	ID = @current

				delete @eventFields

				set @eventHtml = @eventHtml + '</tr>'	-- Close off row for selected event.

				set @current = @current + 1
			end
			-----------------------------------------

			set @eventHtml = @eventHtml + '</tbody>'

			set @eventHtml = @eventHtml + '</table>'

			insert into @tbl values ('Items', @eventHtml)

			set @html = @html + '<div><b>Key:</b> {Key}</div>'
			set @html = @html + '<div>Items: {Items}</div>'
			------------------------------------------------------------------
		end;

		if @Type = 'Intersect'
		begin
			insert into @tbl
				select	'Classification',
						case Classification
							when 1 then 'Critical'
							else 'Normal'
						end
				from	[Intersect]
				where	ID = @ID

			set @html = @html + '<div><b>Classification:</b> {Classification}</div>'
		end;

		if @Type = 'Responsibility'
		begin
			select	@n = T.Name, 
					@t = T.Name,
					@d = T.[Description]
			from	Responsibility O
					inner join ResponsibilityType T on T.ID = O.ResponsibilityTypeID
			where	O.ID = @ID
			
			declare @contextsHtml nvarchar(max)

			set @contextsHtml = '<table class="hoverable bordered striped" style="width:100%">' + 
								'<thead><th>List</th><th>Code</th></thead>' + 
								'<tbody>' + 
								(
								select		(select D.Name as 'td' for xml path(''), type),
											(select I.Code as 'td' for xml path(''), type)
								from		ResponsibilityContextItem R
											inner join ReferenceItem I on R.ResponsibilityID = @ID and R.ObjectType = 'ReferenceItem' and I.ID = R.ObjectID
											inner join ReferenceItemType D on D.ID = I.ReferenceItemTypeID
								FOR XML RAW('tr'), ELEMENTS
								) +
								'</tbody>' + 
								'</table>'

			insert into @tbl values ('Name', @n)
			insert into @tbl values ('Type', @t)
			insert into @tbl values ('Description', @d)
			insert into @tbl values ('Contexts', @contextsHtml)

			set @html = @html + '<div><b>Contexts:</b> {Contexts}</div>'
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
				select	'Name', Name
				from	[Rule] O
				where	ID = @ID
			insert into @tbl
				select	'Description', Description
				from	[Rule] O
				where	ID = @ID
			--insert into @tbl
			--	select	'Status', Status
			--	from	[Rule] O
			--	where	ID = @ID

			--set @html = @html + '<div><b>Status:</b> {Status}</div>'

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
			insert into @tbl
				select	'TextPath', TextPath
				from	Taxonomy O
				where	ID = @ID

			set @html = @html + '<div><b>Path:</b> {TextPath}</div>'

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

			declare @stats table (ID int identity, Name nvarchar(250), Score int)
			insert into @stats 
				select	T.Name,
						coalesce(S.SCore, 0) as Score
				from	StatisticType T
						outer apply (
									select	top 1
											*
									from	Statistic
									where	StatisticTypeID = T.ID
											and ObjectType = @Type
											and ObjectID = @ID
									order by DateStart desc
									) S
				where	T.[Object] = @Type + 'Type' 
						and T.ObjectID = @typeID
						and T.PartOfScore = 1

			set @statsHtml = '<table class="hoverable bordered striped" style="width:100%">'

			-- Loop through field name list ---------
			set @statsHtml = @statsHtml + '<tbody>'
			set		@current = 1
			select	@max = max(ID) from @stats
			while @current <= @max
			begin
				select	@statsHtml = @statsHtml + '<tr><td>' + Name  + '</td>' + '<td>' + cast(Score as varchar(5))  + ' Points</td></tr>'
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

	-- Return the properly formatted values.
	select	'' as Title,
			@html as Body;
END
GO

ALTER PROCEDURE [dbo].[GetSiteNavigation]
AS
BEGIN
	SET NOCOUNT ON;

SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		NULL AS Items	
FROM SiteNav n
WHERE n.Name = '#Monitor'
UNION ALL

SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		NULL AS Items		
FROM SiteNav n
WHERE n.Name = '#Home'
UNION ALL

SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		(
			SELECT	name,
					url,
					0 as feature,
					dbo.ArtifactNgSiteNavigation(id) as items
			FROM	(
					SELECT		TOP 1000
								a.id,
								a.name,
								dbo.GenerateNgObjectUrl('ArtifactType', a.ID, 0) As url
					FROM		ArtifactType a
					left join SiteNav v on v.ObjectID = a.ID and v.Object = 'ArtifactType'
					WHERE		a.ParentID IS NULL and v.ObjectID is null
					ORDER BY	a.name
					) BG
					FOR XML PATH('nav'), TYPE
		) AS Items
FROM SiteNav n
WHERE n.Name = '#Glossary'

UNION ALL

SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		(
		SELECT	ft.name, 
				'model/classification/' + ft.name As url,
				0 as feature,
				(

				SELECT	t.name, 
						dbo.GenerateNgObjectUrl('TaxonomyType', 0, t.ID)  As url,
						0 as feature
				FROM	TaxonomyType t
				LEFT JOIN SiteNav v on v.ObjectID = t.ID and v.Object = 'TaxonomyType'
				WHERE	TaxonomyTypeClassID = FT.ID and v.ObjectID is null
				FOR XML PATH('nav'), TYPE
				) AS items	
		FROM	(
                select top 100 percent ID, name from TaxonomyTypeClass C where exists(select 1 from TaxonomyType where TaxonomyTypeClassID = C.ID) order by name
				) FT
		LEFT JOIN SiteNav v on v.ObjectID = FT.ID and v.Object ='TaxonomyTypeClass'
		WHERE v.ObjectID IS NULL
		FOR XML PATH('nav'), TYPE
		) AS Items
FROM SiteNav n
WHERE n.Name = '#Models'

UNION ALL

SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		(
		SELECT	ft.name, 				
				'policy/classification/' + cast(ft.id as varchar(15)) As url,
				0 as feature,
				(
				SELECT	t.name, 
						dbo.GenerateNgObjectUrl('PolicyType', t.ID, 0)  As url,
						0 as feature
				FROM	PolicyType t
				LEFT JOIN SiteNav v on v.ObjectID = t.ID and v.Object = 'PolicyType'
				WHERE	PolicyTypeClassID = FT.ID and v.ObjectID is null
				FOR XML PATH('nav'), TYPE
				) AS items	
		FROM	(
                select top 100 percent ID, name from PolicyTypeClass C where exists(select 1 from PolicyType where PolicyTypeClassID = C.ID) order by name
				) FT
		LEFT JOIN SiteNav v on v.ObjectID = FT.ID and v.Object ='PolicyTypeClass'
		WHERE v.ObjectID IS NULL
		FOR XML PATH('nav'), TYPE
		) AS Items
		FROM SiteNav n
WHERE n.Name = '#Policy'
		
UNION ALL

SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		null AS Items
FROM SiteNav n
WHERE n.Name = '#Reference'

UNION ALL

SELECT	n.Name as MenuID,
		n.SortOrder,
		2 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		(
		SELECT		name, 
					dbo.GenerateNgObjectUrl('FusionType', FT.ID, 0)  As url,
					2 as feature,
					(
					SELECT		name, 
								dbo.GenerateObjectUrl('Fusion', FT.ID, Fusion.ID)  As url,
								'F' + cast(Fusion.ID as varchar(15)) as menuID,
								2 as feature
					FROM		Fusion
					WHERE		Fusion.FusionTypeID = FT.ID
					ORDER BY	name
					FOR XML PATH('nav'), TYPE
					) AS items	
		FROM		FusionType FT
		ORDER BY	name
		FOR XML PATH('nav'), TYPE
		) AS Items	
	FROM SiteNav n
WHERE n.Name = '#Fusion'
		
UNION ALL

SELECT	n.Name as MenuID, 
		n.SortOrder,
		4 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		(
        SELECT	'People' AS name, --'#People' as MenuID,
                'community/groups' AS url, 		        
                0 as feature,
		        NULL AS Items
        FOR XML PATH('nav'), TYPE
        ) AS Items
FROM SiteNav n
WHERE n.Name = '#Community'
UNION ALL

SELECT	'#Admin' as MenuID,
		999 as SortOrder,
		0 as Feature,
		'fa-cogs' as Icon,
		'Administration' as Title,
		(
			select	*
			from	(
					SELECT	'Security' AS name, 
							'#/' AS url, 
							0 as feature,
							(
							select	*
							from	(
									SELECT	'Groups' AS name, 
											'#/groups/administration' AS url, 
											--'Menu_A_S_G' as menuID,
											0 as feature,
											NULL AS items
									union all
									SELECT	'Users' AS name, 
											'#/resources/administration' AS url, 
											--'Menu_A_S_R' as menuID,
											0 as feature,
											NULL AS items
									union all
									SELECT	'Responsibilities' AS name, 
											'#/governance/administration' AS url, 
											0 as feature,
											NULL AS items
                            ) bg
							FOR XML PATH('nav'), TYPE
							) AS items
						
					union all

					SELECT	'MetaModel' AS name, 
							'#/' AS url,
							0 as feature, 
							(
							select	*
							from	(
									SELECT	'Artifacts' AS name, 
											'#/artifacts/administration' AS url, 
											0 as feature,
											NULL AS items
									union all
									SELECT	'Attributes' AS name, 
											'#/attributes/administration' AS url, 
											0 as feature,
											NULL AS items
									union all
									SELECT	'Lookups' AS name, 
											'#/lookups/administration' AS url, 
											0 as feature,
											NULL AS items
									union all
									SELECT	'Models' AS name, 
											'#/catalogs/administration' AS url, 
											0 as feature,
											NULL AS items
                                    union all
									SELECT	'Policies' AS name, 
											'#/policies/administration' AS url, 
											1 as feature,
											NULL AS items
                                    union all
									SELECT	'Relationships' AS name, 
											'#/relations/administration' AS url, 
											0 as feature,
											NULL AS items
                                    union all
                                    SELECT	'Rules' AS name, 
											'#/rules/administration' AS url, 
											0 as feature,
											NULL AS items
									) bg
							FOR XML PATH('nav'), TYPE
							) AS items
						
					union all

					SELECT	'Metrics' AS name, 
							'#/' AS url,
							0 as feature, 
							(
							select	*
							from	(
									SELECT	'Analytics' AS name, 
											'#/analytics/administration' AS url, 
											5 as feature,
											NULL AS items
									union all
					                SELECT	'Dashboards' AS name, 
							                '#/reporting/administration' AS url, 
							                0 as feature,
							                NULL AS items
                                    union all
					                SELECT	'Surveys' AS name, 
							                '#/surveys/administration' AS url, 
							                7 as feature,
							                (
							                SELECT	'Response Types' AS name, 
									                '#/surveyresponsetypes/administration' AS url, 
									                7 as feature,
									                NULL AS items
							                FOR XML PATH('nav'), TYPE
							                ) AS items
									) bg
							FOR XML PATH('nav'), TYPE
							) AS items
						
					union all

					SELECT	'Reference' AS name, 
							'#/domains/administration' AS url, 
							0 as feature,
							NULL AS items

					union all

					SELECT	'Workflow' AS name, 
							'#/workflow/administration' AS url, 
							0 as feature,
							NULL AS items

                    union all

                    SELECT	'Templates' AS name, 
							'#/templates/administration' AS url, 
							0 as feature,
							NULL AS items

					union all

					SELECT	'Integration' AS name, 
							'#/' AS url, 
							0 as feature,
							(
							select	*
							from	(
									SELECT	'Bulk Loader' AS name, 
											'#/load' AS url, 
											0 as feature,
											NULL AS items
									union all
									SELECT	'Fusion' AS name, 
											'#/fusion/administration' AS url, 
											2 as feature,
											NULL AS items
									union all
									SELECT	'API' AS name, 
											'/swagger' AS url, 
											0 as feature,
											NULL AS items
									) bg
							FOR XML PATH('nav'), TYPE
							) AS items

                    union all

                    SELECT	'Settings' AS name, 
							'#/settings' AS url, 
							0 as feature,
							NULL AS items
            ) bg
			for xml path('nav'), type
		) as Items

	where 1 = 1

	UNION ALL

	SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		(
		SELECT	'Rules' AS name, 
		'quality/rule' AS url, 
		0 as feature,
		NULL AS items
		for xml path('nav'), type
		) AS Items
	FROM SiteNav n
	WHERE n.Name = '#Data Quality'

	UNION ALL

	SELECT 
		'~' + Name AS MenuID,
		s.SortOrder,
		0 AS Feature,
		s.Icon as Icon,
		s.Title as Title,
		dbo.CustomSiteNavigation(ID) AS Items
	from SiteNav s
	where ParentID IS NULL and Name not like '#%'

	order by sortorder
END
GO

ALTER PROCEDURE [fusion].[Rules] 
AS
BEGIN
	SET NOCOUNT ON;

	declare @d datetime = getutcdate(),
			@r int = 0,
			@RuleID int,
			@FusionID int,
			@FusionAttributeID int,
			@ExecutionID int,
			@NumberOfRules int,			
			@NumberOfNewTaxonomies int,
			@NumberOfNewReferenceItems int,
			@NumberOfNewReferences int,
			@NumberOfNewArtifacts int,
			@NumberOfAttributesTotal int,
			@NumberOfNewRelations int,
			@promotionNeedsToRun bit
	
	set	@NumberOfRules = 0;	
	set @NumberOfNewTaxonomies = 0;
	set @NumberOfNewReferenceItems = 0;
	set @NumberOfNewReferences = 0;
	set @NumberOfNewArtifacts = 0;
	set @promotionNeedsToRun = 1;

	--First check if there is anything to do
	EXEC @promotionNeedsToRun = [utility].[ShouldPromotionRun]

	if(@promotionNeedsToRun <= 0)
	BEGIN
		PRINT 'NO REASON TO RUN THE PROMOTION RULES WAS DETECTED';
		return;
	END;

	--Log this run get a new id from the fusion.promotion table
	insert into [fusion].[RuleLog] ( DateStarted ) values ( CURRENT_TIMESTAMP)
	select @ExecutionID =  SCOPE_IDENTITY()

	IF OBJECT_ID('tempdb..#rules') IS NOT NULL
		DROP TABLE #rules;

	create table #rules (
		ID int identity,
		RuleID int,
		FusionID int,
		ObjectType varchar(25),
		ObjectID int,
		FilterFusionAttributeID int,
		FilterFusionAttributeTypeID int
	);

	IF OBJECT_ID('tempdb..#attributes') IS NOT NULL
		DROP TABLE #attributes;

	create table #attributes (
		ID int identity,
		RuleID int,
		RuleStepID int,
		[Action] varchar(25),
		FusionAttributeID int
	);

	IF OBJECT_ID('tempdb..#fields') IS NOT NULL
		DROP TABLE #fields;

	create table #fields (
		ID int, 
		RuleID int,
		RuleStepID int,
		SourceFieldName nvarchar(250), 
		SourceFieldTypeID int, 
		TargetFieldName nvarchar(250), 
		TargetFieldTypeID int, 
		Value nvarchar(4000)
	);

	IF OBJECT_ID('tempdb..#fieldValues') IS NOT NULL
		DROP TABLE #fieldValues;

	create table #fieldValues (
		ObjectType varchar(50), 
		ObjectID int, 
		FieldTypeID int, 
		Value nvarchar(4000)
	);
	
	insert into #rules
		select	R.ID,
				R.FusionID,
				R.ObjectType,
				R.ObjectID,
				I.FusionAttributeID as FilterFusionAttributeID,
				coalesce(A.FusionAttributeTypeID, R.ObjectID) as FilterFusionAttributeTypeID
		from	[fusion].[Rule] R
				inner join [fusion].[RuleItem] I on I.RuleID = R.ID and R.[Enabled] = 1
				left join FusionAttribute A on A.ID = I.FusionAttributeID
	
	declare	@currentID int,
			@maxID int

	set		@currentID = 1
	select	@maxID = MAX(ID) from #rules

	select @NumberOfRules = count(1) from #rules;

	--BEGIN: Determine the target fusion attributes to promote.
	while (@currentID <= @maxID)
	begin
		declare @FusionObjectType varchar(25),
				@FusionObjectID int,
				@FilterFusionAttributeID int,
				@FilterFusionAttributeTypeID int


		select	@RuleID = RuleID,
				@FusionObjectType = ObjectType,
				@FusionObjectID = ObjectID,
				@FusionID = FusionID,
				@FilterFusionAttributeID = FilterFusionAttributeID,
				@FilterFusionAttributeTypeID = FilterFusionAttributeTypeID
		from	#rules
		where	ID = @currentID

		if @FusionObjectID = @FilterFusionAttributeTypeID AND @FilterFusionAttributeID is not null
			begin
				-- You are on a specific nodes of same type.  Just copy to target table.
				insert into #attributes 
					select	@RuleID, 
							S.ID,
							S.[Action],
							@FilterFusionAttributeID
					from	[fusion].[RuleStep] S
					where	S.RuleID = @RuleID
					order by S.Step
			end
		else
			begin
				-- You are on an attribute higher up in hierarchy.
				if @FilterFusionAttributeID is null
					begin
						--  If there is NO filtered attribute ID, then you need to get every attribute in system for the partiular fusion instance.
						insert into #attributes
							select	@RuleID, 
									S.ID,
									S.[Action],
									FA.ID
							from	FusionAttribute FA 
									inner join [fusion].[RuleStep] S on S.RuleID = @RuleID and FA.FusionID = @FusionID and FA.FusionAttributeTypeID = @FusionObjectID
									left join #attributes A on A.FusionAttributeID = FA.ID and A.RuleID = S.RuleID and A.ID is null
							order by FA.ID, S.Step
					end
				else
					begin
						-- If there is a filter attribute ID, then traverse the hierarchy and get all attributes of the specified type.
						with FA as	(
									select	ID,
											ParentID,
											FusionAttributeTypeID
									from	FusionAttribute
									where	ID = @FilterFusionAttributeID
											and FusionID = @FusionID
									union all
									select	C.ID,
											C.ParentID,
											C.FusionAttributeTypeID
									from	FusionAttribute C
											inner join fa P on C.ParentID = P.ID --and P.ID <> C.ID
									)
	
						insert into #attributes
							select	@RuleID, 
									S.ID,
									S.[Action],
									FA.ID
							from	FA 
									inner join [fusion].[RuleStep] S on S.RuleID = @RuleID and FA.FusionAttributeTypeID = @FusionObjectID
									left join #attributes A on A.FusionAttributeID = FA.ID and A.RuleID = S.RuleID and A.ID is null
							where	FA.FusionAttributeTypeID = @FusionObjectID
							order by FA.ID, S.Step
					end
			end

		set @currentID = @currentID + 1
	end --end while loop
	--END: Determine the target fusion attributes to promote.

	-- Load field values we are working with, first starting with the Name.
	insert into #fields
		select	A.ID,
				RS.RuleID,
				M.RuleStepID,
				M.SourceFieldName,
				M.SourceFieldTypeID,
				M.TargetFieldName,
				M.TargetFieldTypeID,
				case 
					when M.SourceFieldName = 'ID' then cast(FA.ID as nvarchar)
					when M.SourceFieldName = 'Name' then FA.Name
					when M.SourceFieldName = 'TextPath' then FA.TextPath
					when M.IsConstantValue = 1 then M.ConstantValue
				end				
		from	[fusion].[RuleStepMapping] M
				inner join [fusion].[RuleStep] RS on M.RuleStepID = RS.ID
				inner join #attributes A on A.RuleID = RS.RuleID
				inner join FusionAttribute FA on FA.ID = A.FusionAttributeID 

	
	-- Update the fields table above with values for all dynamic fields.
	update	T
	set		T.Value = S.Value
	from	#fields T
			inner join #attributes A on A.ID = T.ID
			inner join Field S on S.ObjectType = 'FusionAttribute' and S.ObjectID = A.FusionAttributeID and S.FieldTypeID = T.SourceFieldTypeID 

--BEGIN: TESTING ---------------------------------------
/*
select * from #rules
select * from #attributes
select * from #fields
select * from FusionAttributePromotion where RuleID = 6

select * from IntersectMap where ID = 1424
select * from IntersectNode where ID = 720728
select * from [Intersect] where ID = 362728
delete FusionAttributePromotion where RuleID = 34
select	A.ID,
		R.RuleID,
		R.FusionID,
		R.ObjectID as FusionAttributeTypeID,
		R.PromotionObjectType,
		R.PromotionObjectID,
		R.PromotionParentObjectType,
		R.PromotionParentObjectID,
		A.FusionAttributeID
from	#rules R
		inner join #attributes A on A.RuleID = R.RuleID
*/
--END: TESTING ------------------------------------------

	set		@currentID = 1
	select	@maxID = MAX(ID) from #attributes

	set @NumberOfAttributesTotal = @maxID;
	
	while (@currentID <= @maxID)
	begin
		begin try

			declare @FusionAttributeTypeID int = null,
					@RuleStepID int = null,
					@Action varchar(25) = null,
					@ResultObject varchar(50) = null,
					@ResultObjectID int = null

			declare @fields table (SourceFieldName nvarchar(250), SourceFieldTypeID int, TargetFieldName nvarchar(250), TargetFieldTypeID int, Value nvarchar(4000))
			declare @settings table (Name nvarchar(100), Value nvarchar(250))
			
			select	@RuleID = R.RuleID,
					@RuleStepID = A.RuleStepID,
					@Action = A.[Action],
					@FusionID = R.FusionID,
					@FusionAttributeTypeID = R.ObjectID,
					@FusionAttributeID = A.FusionAttributeID,
					@ResultObject = P.ObjectType,
					@ResultObjectID = P.ObjectID
			from	#rules R
					inner join #attributes A on A.RuleID = R.RuleID and A.ID = @currentID
					left join [Fusion].RulePromotion P on P.FusionAttributeID = A.FusionAttributeID and P.RuleID = R.RuleID and P.RuleStepID = A.RuleStepID

			delete from @fields -- clear out previous fields
			--Load fields were are working with for this loop instance.
			insert into @fields
				select SourceFieldName, SourceFieldTypeID, TargetFieldName, TargetFieldTypeID, Value from #fields where ID = @currentID and RuleStepID = @RuleStepID

			delete from @settings -- clear out previous settings
			--Load settings were are working with for this loop instance.
			insert into @settings
				select Name, Value from [fusion].[RuleStepSetting] RSS inner join [fusion].[RuleStep] RS on (RSS.RuleStepID = RS.ID) where RS.RuleID = @RuleID and RS.ID = @RuleStepID
				
			--BEGIN: Promote action
			if @Action = 'Promote'
			begin
				declare @ObjectTypeToPromoteTo varchar(50) = null,
						@ObjectTypeIDToPromoteTo int = null,
						@ParentObjectSearchType nvarchar(250) = null,
						@ParentSearchObject varchar(50) = null,
						@ParentSearchObjectID int = null,
						@ParentObject varchar(50) = null,
						@ParentObjectID int = null

				select	@ObjectTypeToPromoteTo		= Value from @settings where Name = 'Object'
				select	@ObjectTypeIDToPromoteTo	= Value from @settings where Name = 'ObjectID'
				select	@ParentObjectSearchType		= Value from @settings where Name = 'ParentObjectSearch'
				select	@ParentSearchObject			= Value from @settings where Name = 'ParentObject'
				select	@ParentSearchObjectID		= Value from @settings where Name = 'ParentObjectID'

				if exists(select 1 from @fields where TargetFieldName = 'Name')
				begin
					declare @code nvarchar(50) = null,
							@name nvarchar(250) = null,
							@description nvarchar(4000) = null

					select @code = Value from @fields where TargetFieldName = 'Code'
					select @name = Value from @fields where TargetFieldName = 'Name'
					select @description = coalesce(Value, '') from @fields where TargetFieldName = 'Description'

					--BEGIN: Find parent based on search type
					if @ParentObjectSearchType = 'Direct'
					begin
						set @ParentObject = @ParentSearchObject
						set @ParentObjectID = @ParentSearchObjectID
					end

					if @ParentObjectSearchType = 'FusionOwner'
					begin
						select	@ParentObject = 'Artifact',
								@ParentObjectID = ArtifactID
						from	FusionOwner
						where	@ParentSearchObject = 'Owner'
								and FusionID = @FusionID
								--and ID = @ParentSearchObjectID
					end

					if @ParentObjectSearchType = 'ResultFromStep'
					begin
						select	@ParentObject = ObjectType,
								@ParentObjectID = ObjectID
						from	[fusion].[RulePromotion]
						where	@ParentSearchObject = 'Step'
								and RuleID = @RuleID
								and RuleStepID = @ParentSearchObjectID
								and FusionAttributeID = @FusionAttributeID
					end
					--END: Find parent based on search type

					print @ParentObject
					print @ParentObjectID

					--BEGIN: Determine object type to promote as
					if @ObjectTypeToPromoteTo = 'ArtifactType'
					begin
						set @ResultObject = 'Artifact'

						if @ResultObjectID is null
						begin
							select	@ResultObjectID = ID
							from	Artifact
							where	ArtifactTypeID = @ObjectTypeIDToPromoteTo
									and lower(Name) = lower(@name)
						end

						declare @modelTypeID int = null
						declare @taxonomyTypeValue nvarchar(250)

						select @taxonomyTypeValue = Value from @fields where TargetFieldName = 'TaxonomyTypeID'

--fusion.Rules
						if (@taxonomyTypeValue <> '' and @taxonomyTypeValue is not null)
						begin
							select @modelTypeID = ID from TaxonomyType where Name = ltrim(rtrim(@taxonomyTypeValue))
						end

						if @taxonomyTypeValue is null
						begin
							select @modelTypeID = min(ID) from TaxonomyType
						end

						if @ResultObjectID is null
							begin
								if @ParentObjectID = 0
								begin
									set @ParentObjectID = null
								end

								if @modelTypeID is not null
									begin
										insert into Artifact ( ParentID, ArtifactTypeID, TaxonomyTypeID, Name, Description, Status, UpdatedOn, UpdatedBy )
										values ( @ParentObjectID, @ObjectTypeIDToPromoteTo, @modelTypeID, @name, @description, 'Draft', getutcdate(), 0 )

										select @ResultObjectID =  SCOPE_IDENTITY()
										set @NumberOfNewArtifacts = @NumberOfNewArtifacts +1;
									end
							end
						else
							begin
								declare @testArtifactName nvarchar(250) = null,
										@testArtifactDescription nvarchar(4000) = null,
										@testArtifactParentID int = null,
										@testArtifactTaxonomyTypeID int = null

								select	@testArtifactName = Name,
										@testArtifactDescription = Description,
										@testArtifactParentID = ParentID,
										@testArtifactTaxonomyTypeID = TaxonomyTypeID
								from	Artifact
								where	ID = @ResultObjectID

								if @modelTypeID is not null
									begin
										if (@testArtifactName <> @name) 
											OR (@testArtifactDescription <> @description) 
											OR (@testArtifactParentID <> @ParentObjectID) 
											OR (@testArtifactTaxonomyTypeID <> @modelTypeID)
										begin
											update	Artifact
											set		Name = @name,
													Description = @description,
													ParentID = @ParentObjectID,
													TaxonomyTypeID = @modelTypeID
											where	ID = @ResultObjectID
										end
									end
							end
					end
					--END: IF ArtifactType

					if @ObjectTypeToPromoteTo = 'ReferenceItemType' OR @ObjectTypeToPromoteTo = 'ReferenceItem'
					begin
						-- You are promoting Reference items to a specific Reference (list)
						set @ResultObject = 'ReferenceItem'

						if @ResultObject is null and @ResultObjectID is null
							begin
								select	@ResultObjectID = ID
								from	ReferenceItem
								where	ReferenceItemTypeID = @ParentObjectID
										and lower(Code) = lower(@code)
							end
 
						if @ResultObjectID is null
							begin
								insert into ReferenceItem ( ReferenceItemTypeID, Code )
								values ( @ParentObject, @code )

								select @ResultObjectID =  SCOPE_IDENTITY()

								set @NumberOfNewReferenceItems = @NumberOfNewReferenceItems +1;
							end
					end
					--END: IF ReferenceType

					if @ObjectTypeToPromoteTo = 'TaxonomyType'
					begin
						set @ResultObject = 'Taxonomy'

						if @ResultObjectID is null
							begin
								select	@ResultObjectID = ID
								from	Taxonomy
								where	TaxonomyTypeID = @ObjectTypeIDToPromoteTo
										and ParentID = @ParentObjectID
										and lower(Name) = lower(@name)
							end

						if @ResultObjectID is null
							begin
								insert into Taxonomy	( ParentID, TaxonomyTypeID, Name, Description )
								values					( @ParentObjectID, @ObjectTypeIDToPromoteTo, @name, @description )

								select @ResultObjectID =  SCOPE_IDENTITY()

								set @NumberOfNewTaxonomies = @NumberOfNewTaxonomies +1;
							end
						else
							begin
								update	Taxonomy
								set		Name = @Name,
										Description = @Description--,
										--ParentID = @PromotionParentObjectID
								where	ID = @ResultObjectID
 							end
					end
					--END: IF TaxonomyType

					--END: Determine object type to promote as

				end -- END: Check to see if Target Field called NAME is present

			end --END: Promote action

			--BEGIN: Find Action
			if @Action = 'Find'
			begin
				declare @FindSearchType nvarchar(250) = null,
						@FindSearchObject varchar(50) = null,
						@FindSearchObjectID int = null,
						@FindFilterField int = null,
						@FindFilterFieldValue nvarchar(250) = null,
						@FindTargetField int = null,
						@FindParent int = null,
						@PromotionRuleStepID int = null

				select	@FindSearchType			= Value from @settings where Name = 'ObjectSearch'
				select	@FindSearchObject		= Value from @settings where Name = 'Object'
				select	@FindSearchObjectID		= Value from @settings where Name = 'ObjectID'
				select	@FindFilterField		= Value from @settings where Name = 'FilterField'
				select	@FindTargetField		= Value from @settings where Name = 'TargetField'
				select	@FindParent				= Value from @settings where Name = 'FindParent'
																
				if @FindSearchType = 'Fusion'
				begin					
					if @FindFilterField > 0
						begin
							if not exists(select 1 from @fields where SourceFieldTypeID = @FindFilterField)
								begin
									select	@FindFilterFieldValue = Value
									from	FieldWithRelation
									where	FieldTypeID = @FindFilterField
											and ObjectType = 'FusionAttribute'
											and ObjectID = @FusionAttributeID
								end
							else
								begin
									select	@FindFilterFieldValue = Value
									from	@fields
									where	SourceFieldTypeID = @FindFilterField
								end
						end
					else
						begin
							if not exists(select 1 from @fields where SourceFieldName = 'Name')
								begin
									select	@FindFilterFieldValue = TextPath
									from	FusionAttribute
									where	ID = @FusionAttributeID
								end
							else
								begin
									select	@FindFilterFieldValue = Value
									from	@fields
									where	SourceFieldName = 'Name'
								end
						end
					
					if @FindFilterFieldValue is not null
					begin
						select	top 1
								@ResultObject = 'FusionAttribute',
								@ResultObjectID = ID
						from	FusionAttribute
						where	@FindSearchObject = 'FusionAttributeType'
								and FusionAttributeTypeID = @FindSearchObjectID
								and (SourceID = @FindFilterFieldValue or TextPath = @FindFilterFieldValue or Name = @FindFilterFieldValue)
					end
				end

				--BEGIN: Find based on search type
				if @FindSearchType = 'FusionOwner'
				begin
					set	@ResultObject = 'Artifact'
					set @ResultObjectID = @FindSearchObjectID
				end

				if @FindSearchType = 'Glossary'					
				begin									
					if @FindFilterField > 0
					begin
						select	@FindFilterFieldValue = Value
						from	@fields
						where	SourceFieldTypeID = @FindFilterField
					end
					else
					begin
						select	@FindFilterFieldValue = Value
						from	@fields
						where	SourceFieldName = 'Name'	
					end

					if @FindFilterFieldValue is not null
					begin
						if @FindSearchObject = 'ArtifactType' and  ( @FindTargetField is null or @FindTargetField <= 0)
						begin							
							select	top 1
									@ResultObject = 'Artifact',
									@ResultObjectID = ID
							from	Artifact
							where	ArtifactTypeID = @FindSearchObjectID
									and (TextPath = @FindFilterFieldValue or Name = @FindFilterFieldValue)
						end

						if @FindSearchObject = 'ArtifactType' and @FindTargetField > 0
						begin							
							select	top 1
									@ResultObject = 'Artifact',
									@ResultObjectID = a.ID
							from	Artifact a
									inner join field f on(a.ID = f.ObjectID and f.Objecttype = 'Artifact' and f.fieldtypeid = @FindTargetField)
							where	a.ArtifactTypeID = @FindSearchObjectID									
									and (f.FormattedValue = @FindFilterFieldValue)
						end

						if @FindSearchObject = 'TaxonomyType'
						begin
							select	top 1
									@ResultObject = 'Taxonomy',
									@ResultObjectID = ID
							from	Taxonomy
							where	TaxonomyTypeID = @FindSearchObjectID
									and (TextPath = @FindFilterFieldValue or Name = @FindFilterFieldValue)
						end
					end
				end

				if @FindSearchType = 'ResultFromStep' and @FindParent is not null
				begin
					select	@ResultObject = co.parent,
							@ResultObjectID = co.parentid
					from	[fusion].[RulePromotion] rp
							inner join [cache].[objectdetails] co on(co.[object] = rp.objecttype and co.objectid = rp.objectid)
					where	@FindSearchObject = 'Step'
							and rp.RuleID = @RuleID
							and rp.RuleStepID = @FindSearchObjectID
							and rp.FusionAttributeID = @FusionAttributeID
				end

				if @FindSearchType = 'ResultFromStep' and @FindParent is null
				begin
					select	@ResultObject = ObjectType,
							@ResultObjectID = ObjectID
					from	[fusion].[RulePromotion]
					where	@FindSearchObject = 'Step'
							and RuleID = @RuleID
							and RuleStepID = @FindSearchObjectID
							and FusionAttributeID = @FusionAttributeID
				end

				if @FindSearchType = 'Promotion' and @FindTargetField is null --by parent
				begin
					select	@ResultObject = ObjectType,
						    @ResultObjectID = ObjectID
					from	[fusion].[RulePromotion]
					join	FusionAttribute A on A.ID = @FusionAttributeID
					join	FusionAttribute AP on AP.ID = A.ParentID
					where	RuleStepID = @PromotionRuleStepID
							and FusionAttributeID = AP.ID
				end

				if @FindSearchType = 'Promotion' and @FindTargetField is not null -- by field
				begin
					select	@ResultObject = R.ObjectType, 
							@ResultObjectID = R.ObjectID 
					from	[fusion].[RulePromotion] R
					join	FusionAttribute SA on SA.ID = R.FusionAttributeID
					join	Field SF on SF.ObjectType = 'FusionAttribute' 
							and SF.ObjectID = SA.ID 
							and SF.FieldTypeID = @FindFilterField
					join	FusionAttribute TA on TA.ID = @FusionAttributeID
					join	Field TF on TF.ObjectType = 'FusionAttribute' 
							and TF.ObjectID = TA.ID 
							and TF.FieldTypeID = @FindTargetField
					where	R.RuleStepID = @PromotionRuleStepID 
							and SF.Value = TF.Value
				end

				--END: Find based on search type
			end --END: Find Action


			--BEGIN: FindRelation Action
			if @Action = 'FindRelation'
			begin
				declare @IntersectTypeID		int = null,
						@SearchType				nvarchar(250) = null,
						@FindRelationObject		varchar(50) = null,
						@FindRelationObjectID	int = null

				select	@IntersectTypeID		= Value from @settings where Name = 'IntersectType'
				select	@SearchType				= Value from @settings where Name = 'Search'
				select	@FindSearchObjectID		= Value from @settings where Name = 'ID'

				--BEGIN: Find based on search type

				if @SearchType = 'ResultFromStep'
				begin
					select	@FindRelationObject = ObjectType,
							@FindRelationObjectID = ObjectID
					from	[fusion].[RulePromotion]
					where	RuleID = @RuleID
							and RuleStepID = @FindSearchObjectID
							and FusionAttributeID = @FusionAttributeID
				end

				if @SearchType = 'Self'
				begin
					set @FindRelationObject = 'FusionAttribute'
					set @FindRelationObjectID = @FusionAttributeID
				end

				if @FindRelationObject is not null and @FindRelationObjectID is not null
				begin
					select	top 1
							@ResultObject = case 
												when (Subject = @FindRelationObject and SubjectID = @FindRelationObjectID) then Object
												else Subject
											end,
							@ResultObjectID = case 
												when (Subject = @FindRelationObject and SubjectID = @FindRelationObjectID) then ObjectID
												else SubjectID
											end
					from	[Intersect]
					where	IntersectTypeID = @IntersectTypeID
							and (
									(Subject = @FindRelationObject and SubjectID = @FindRelationObjectID) 
									OR (Object = @FindRelationObject and ObjectID = @FindRelationObjectID)
								)
				end

				--END: Find based on search type

			end --END: FindRelation Action

			
			--BEGIN: Lineage Action
			if @Action = 'Lineage'
			begin
				declare @SubjectSearchID int = null,
						@ObjectSearchID int = null,
						@Subject varchar(50) = null,
						@SubjectID int = null,
						@Object varchar(50) = null,
						@ObjectID int = null,

						@TechnicalSubjectSearchID int = null,
						@TechnicalObjectSearchID int = null,
						@RoleID int = null,

						@TechnicalSubject varchar(50) = null,
						@TechnicalSubjectID int  = null,
						@TechnicalObject varchar(50) = null,
						@TechnicalObjectID int  = null

				select	@SubjectSearchID			= Value from @settings where Name = 'SubjectID'
				select	@ObjectSearchID				= Value from @settings where Name = 'ObjectID'

				select	@TechnicalSubjectSearchID	= Value from @settings where Name = 'TechnicalSubjectID'
				select	@TechnicalObjectSearchID	= Value from @settings where Name = 'TechnicalObjectID'

				select	@RoleID						= Value from @settings where Name = 'Role'
				
				--BEGIN: Find subject based on search type, ALWAYS ResultFromStep
				select	@Subject = ObjectType,
						@SubjectID = ObjectID
				from	[Fusion].[RulePromotion]
				where	RuleID = @RuleID
						and RuleStepID = @SubjectSearchID
						and FusionAttributeID = @FusionAttributeID
				--END: Find subject based on search type

				--BEGIN: Find object based on search type
				select	@Object = ObjectType,
						@ObjectID = ObjectID
				from	[fusion].[RulePromotion]
				where	RuleID = @RuleID
						and RuleStepID = @ObjectSearchID
						and FusionAttributeID = @FusionAttributeID
				--END: Find object based on search type

				declare @Map table (ID int)

				--BEGIN: Add Map
				if @Subject = 'Intersect' and @SubjectID is not null and @Object = 'Intersect' and @ObjectID is not null
				begin
					MERGE	MapItem AS T
					USING	(
							SELECT	@SubjectID as SourceIntersectID, 
									@ObjectID as TargetIntersectID
							) as S
					ON		T.SourceIntersectID = S.SourceIntersectID
							and T.TargetIntersectID = S.TargetIntersectID 
					WHEN	NOT MATCHED THEN
							INSERT (SourceIntersectID, TargetIntersectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn) 
							VALUES (S.SourceIntersectID, S.TargetIntersectID, 0, getutcdate(), 0, getutcdate())
					OUTPUT inserted.ID into @Map;
					
					set @ResultObject = 'MapItem'
					select top 1 @ResultObjectID = ID from @Map
				end
				--END: Add Map

				--BEGIN: Find subject based on search type, ALWAYS ResultFromStep
				if @TechnicalSubjectSearchID is not null and @TechnicalObjectSearchID is not null
				begin
					select	@TechnicalSubject = ObjectType,
							@TechnicalSubjectID = ObjectID
					from	[Fusion].[RulePromotion]
					where	RuleID = @RuleID
							and RuleStepID = @TechnicalSubjectSearchID
							and FusionAttributeID = @FusionAttributeID

					select	@TechnicalObject = ObjectType,
							@TechnicalObjectID = ObjectID
					from	[fusion].[RulePromotion]
					where	RuleID = @RuleID
							and RuleStepID = @TechnicalObjectSearchID
							and FusionAttributeID = @FusionAttributeID
				end
				--END: Find object based on search type

				declare @MapRule table (ID int)

				--BEGIN: Add Map
				if	@TechnicalSubject = 'FusionAttribute' and @TechnicalSubjectID is not null 
					and @TechnicalObject = 'FusionAttribute' and @TechnicalObjectID is not null
				begin
					MERGE	MapRuleItem AS T
					USING	(
							SELECT	@TechnicalSubjectID as SourceFusionAttributeID, 
									@TechnicalObjectID as TargetFusionAttributeID
							) as S
					ON		T.SourceFusionAttributeID = S.SourceFusionAttributeID
							and T.TargetFusionAttributeID = S.TargetFusionAttributeID 
					WHEN	NOT MATCHED THEN
							INSERT (SourceFusionAttributeID, TargetFusionAttributeID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn) 
							VALUES (S.SourceFusionAttributeID, S.TargetFusionAttributeID, 0, getutcdate(), 0, getutcdate())
					OUTPUT inserted.ID into @Map;
					
					set @ResultObject = 'MapRuleItem'
					select top 1 @ResultObjectID = ID from @MapRule
				end
				--END: Add Map

				if exists(select ID from @Map) and exists(select ID from @MapRule)
				begin
					merge	MapRuleItemMapItem as T
					using	(
							select	B.ID as MapItemID,
									T.ID as MapRuleItemID
							from	@Map B
									inner join @MapRule T on 1=1
							) as S
					on		T.MapRuleItemID = S.MapRuleItemID and T.MapItemID = S.MapItemID
					when	not matched then
							insert (MapRuleItemID, MapItemID)
							values (S.MapRuleItemID, S.MapItemID);

					delete from @Map
					delete from @MapRule
				end

			end --END: Lineage Action

			--BEGIN: Relate Action
			if @Action = 'Relate'
			begin
				declare @R_IntersectTypeID int = null,
						@R_SubjectSearchType nvarchar(250) = null,
						@R_SubjectSearchObject varchar(50) = null,
						@R_SubjectSearchObjectID int = null,
						@R_Subject varchar(50) = null,
						@R_SubjectID int = null,
						@R_ObjectSearchType nvarchar(250) = null,
						@R_ObjectSearchObject varchar(50) = null,
						@R_ObjectSearchObjectID int = null,
						@R_Object varchar(50) = null,
						@R_ObjectID int = null,
						@R_IntersectID int = null

				select	@R_SubjectSearchType		= Value from @settings where Name = 'SubjectSearch'
				select	@R_SubjectSearchObject		= Value from @settings where Name = 'Subject'
				select	@R_SubjectSearchObjectID	= Value from @settings where Name = 'SubjectID'
				select	@R_ObjectSearchType			= Value from @settings where Name = 'ObjectSearch'
				select	@R_ObjectSearchObject		= Value from @settings where Name = 'Object'
				select	@R_ObjectSearchObjectID		= Value from @settings where Name = 'ObjectID'
				select	@R_IntersectTypeID			= Value from @settings where Name = 'IntersectType'


				--BEGIN: Find subject based on search type
				if @R_SubjectSearchType = 'Direct'
				begin
					set @R_Subject = @R_SubjectSearchObject
					set @R_SubjectID = @R_SubjectSearchObjectID
				end

				if @R_SubjectSearchType = 'FusionOwner'
				begin
					set	@R_Subject = 'Artifact'
					set @R_SubjectID = @R_ObjectSearchObjectID
				end

				if @R_SubjectSearchType = 'ResultFromStep'
				begin
					select	@R_Subject = ObjectType,
							@R_SubjectID = ObjectID
					from	[fusion].RulePromotion
					where	@R_SubjectSearchObject = 'Step'
							and RuleID = @RuleID
							and RuleStepID = @R_SubjectSearchObjectID
							and FusionAttributeID = @FusionAttributeID
				end

				if @R_SubjectSearchType = 'Self'
				begin
					set @R_Subject = 'FusionAttribute'
					set @R_SubjectID = @FusionAttributeID
				end
				--END: Find subject based on search type
				
				--BEGIN: Find object based on search type
				if @R_ObjectSearchType = 'Direct'
				begin
					set @R_Object = @R_ObjectSearchObject
					set @R_ObjectID = @R_ObjectSearchObjectID
				end

				if @R_ObjectSearchType = 'FusionOwner'
				begin
					set	@R_Object = 'Artifact'
					set @R_ObjectID = @R_ObjectSearchObjectID
				end

				if @R_ObjectSearchType = 'ResultFromStep'
				begin
					select	@R_Object = ObjectType,
							@R_ObjectID = ObjectID
					from	[Fusion].[RulePromotion]
					where	@R_ObjectSearchObject = 'Step'
							and RuleID = @RuleID
							and RuleStepID = @R_ObjectSearchObjectID
							and FusionAttributeID = @FusionAttributeID
				end

				if @R_ObjectSearchType = 'Self'
				begin
					set @R_Object = 'FusionAttribute'
					set @R_ObjectID = @FusionAttributeID

				end
				--END: Find object based on search type


				--Check to see if we have all the required data to create the relationship.
				if @R_IntersectTypeID is not null and @R_Subject is not null and @R_SubjectID is not null and @R_Object is not null and @R_ObjectID is not null
				begin
					-- Validate that intersect type exists.
					if exists(select 1 from IntersectType where ID = @R_IntersectTypeID)
					begin
						set @ResultObject = 'Intersect'
--select @Subject, @SubjectID, @Object, @ObjectID
						select	@R_IntersectID = ID
						from	[Intersect]
						where	Subject = @R_Subject 
								and SubjectID = @R_SubjectID 
								and Object = @R_Object 
								and ObjectID = @R_ObjectID
								and IntersectTypeID = @R_IntersectTypeID

						if @R_IntersectID is null
						begin
							declare @R_SubjectType varchar(50) = null,
									@R_SubjectTypeID int = null,
									@R_SubjectIntersectTypeNodeID int = null,
									@R_ObjectType varchar(50) = null,
									@R_ObjectTypeID int = null,
									@R_ObjectIntersectTypeNodeID int = null

							select	@R_SubjectType = ObjectType, @R_SubjectTypeID = ObjectTypeID from cache.[object] where Object = @R_Subject and ObjectID = @R_SubjectID
							select	@R_ObjectType = ObjectType, @R_ObjectTypeID = ObjectTypeID from cache.[object] where Object = @R_Object and ObjectID = @R_ObjectID

							select	@R_IntersectTypeID = ID
							from	[IntersectType] R 
							where	Subject = @R_SubjectType and SubjectID = @R_SubjectTypeID 
									and Object = @R_ObjectType and ObjectID = @R_ObjectTypeID;


							if @R_IntersectTypeID is not null
							begin
								begin try
									insert into [Intersect] (IntersectTypeID, Classification, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
									values					(@R_IntersectTypeID, 2, @R_Subject, @R_SubjectID, @R_Object, @R_ObjectID, 0, @r, @d, @r, @d)  

									select @R_IntersectID = SCOPE_IDENTITY()

									--cache logic
									insert into cache.[Object] ( [Object], [ObjectID], [ObjectType], [ObjectTypeID] ) values	( 'Intersect', @R_IntersectID, 'IntersectType', @R_IntersectTypeID );

									--Update the responsibilities of the object that should inherit form the other (Taxonomy can push relationships down to artifact)
									if ( (@R_Subject = 'Taxonomy' and @R_Object = 'Artifact') OR (@R_Subject = 'Artifact' and @R_Object = 'Taxonomy') )
									begin
										if @R_Subject = 'Artifact'
										begin
											exec [cache].[SynchronizeResponsibilitiesForObject] @R_Subject, @R_SubjectID
										end
										if @R_Object = 'Artifact'
										begin
											exec [cache].[SynchronizeResponsibilitiesForObject] @R_Object, @R_ObjectID
										end
									end

									exec utility.AddAuditEntry @R_Subject, @R_SubjectID, @r, @d, 'Created', 'Intersect', @R_IntersectID
									exec utility.AddAuditEntry @R_Object, @R_ObjectID, @r, @d, 'Created', 'Intersect', @R_IntersectID

									set @NumberOfNewRelations = @NumberOfNewRelations + 1

									set @ResultObjectID = @R_IntersectID
								end try
								begin catch
									select ERROR_MESSAGE()
								end catch

							end
						end
						else
						begin
							set @ResultObjectID = @R_IntersectID
						end
					end
				end


			end --END: Relate Action


			-- Add/Update the promotion record to keep track of the auto-promotions
			if @ResultObject is not null and @ResultObjectID is not null
			begin
				-- Insert/Update the FusionAttributePromotion table to keep track of previously promoted objects.
				MERGE	[fusion].[RulePromotion] AS T
				USING	(
						SELECT	@FusionAttributeID as FusionAttributeID, 
								@ResultObject as ObjectType, 
								@ResultObjectID as ObjectID, 
								@RuleID as RuleID,
								0 as PromotedObjectTypeID,
								@RuleStepID as RuleStepID
						) as S
				ON		T.RuleID = S.RuleID
						and T.RuleStepID = S.RuleStepID 
						and T.FusionAttributeID = S.FusionAttributeID 
						and T.ObjectType = S.ObjectType 
						and T.ObjectID = S.ObjectID
				WHEN	MATCHED THEN
						UPDATE SET	T.RuleID = S.RuleID, 
									T.ObjectTypeID = S.PromotedObjectTypeID
				WHEN	NOT MATCHED THEN
						INSERT (FusionAttributeID, ObjectType, ObjectID, RuleID, RuleStepID, ObjectTypeID) 
						VALUES (S.FusionAttributeID, S.ObjectType, S.ObjectID, S.RuleID, S.RuleStepID, S.PromotedObjectTypeID);


				-- Add/Update the dynamic fields involved.

				-- First, clean up fields table variable of static fields to prepare for dynamic field work below.
				delete @fields where TargetFieldTypeID = 0

				-- Now insert the dynamic fields
				while exists (select 1 from @fields)
				begin
					declare @targetFieldTypeID int,
							@field_Type varchar(25),
							@lookupObjectType varchar(25),
							@lookupObjectID int,
							@fieldValue nvarchar(4000),
							@shouldInsert bit = 0

					select	top 1 
							@targetFieldTypeID = TargetFieldTypeID,
							@fieldValue = Value
					from	@fields
									
					select	@field_Type = [Type],
							@lookupObjectType = LookupObjectType,
							@lookupObjectID = LookupObjectID									
						from	FieldType 
						where	ID = @targetFieldTypeID

					if @field_Type = 'Lookup'
					begin
						declare @objectResultID int

						if @lookupObjectType = 'Artifact'
							begin
								select	top 1
										@objectResultID = ID
								from	Artifact
								where	ArtifactTypeID = @lookupObjectID and Name = @fieldValue
							end
						if @lookupObjectType = 'ReferenceItemType'
							begin
								select	top 1
										@objectResultID = ID
								from	ReferenceItem
								where	ReferenceItemTypeID = @lookupObjectID and Code = @fieldValue
							end
						if @lookupObjectType = 'Lookup'
							begin
								select	top 1
										@objectResultID = L.ID
								from	[Lookup] L
										inner join Field F on F.ObjectType = @lookupObjectType and F.ObjectID = L.ID and L.LookupTypeID = @lookupObjectID and F.FieldTypeID = @targetFieldTypeID and F.FormattedValue = @fieldValue
							end
											
						if @ResultObjectID is not null and @objectResultID is not null
							begin
								-- Lookup values properly resolved, so you can now insert the Field record.
													
								set @shouldInsert = 1
								set @fieldValue = cast(@objectResultID as nvarchar(4000))
							end
					end									
					else
					begin
						-- This is a text value, so just insert it into the Field table for the promoted object.
						set @shouldInsert = 1
					end

					if @shouldInsert = 1
					begin
						If not EXISTS (SELECT 1 FROM #fieldValues where ObjectType = @ResultObject and ObjectID = @ResultObjectID and FieldTypeID = @targetFieldTypeID) --avoid duplicates this happens in gmo
						begin
							insert into #fieldValues (ObjectType, ObjectID, FieldTypeID, Value) values(@ResultObject, @ResultObjectID, @targetFieldTypeID, @fieldValue)
						end
					end
						
					-- Delete the field we just finished processing.
					delete @fields where TargetFieldTypeID = @targetFieldTypeID
				end --END: while

			end --END: IF when checking for promotiontype


		end try
		begin catch
			SELECT 
				ERROR_NUMBER() AS ErrorNumber
				,ERROR_MESSAGE() AS ErrorMessage;
		end catch

		set @currentID = @currentID + 1
	end


	-- write the field values from the temp table to the field table
	-- the field table has a trigger doing this once outside the loop causes the trigger to only fire this one time.
	If EXISTS (SELECT 1 FROM #fieldValues)
	begin
		--debug shows values 
		--select * from #fieldValues

		merge	Field as T
		using	(
				select	f.ObjectType as ObjectType,
						f.ObjectID as ObjectID,
						f.FieldTypeID as FieldTypeID,
						f.Value as Value
				from	#fieldValues f 
						inner join dbo.FieldType ft on (ft.ID = f.FieldTypeID)
				) as S
		on		T.ObjectType = S.ObjectType and T.ObjectID = S.ObjectID and T.FieldTypeID = S.FieldTypeID
		when	matched then
				update set T.Value = S.Value
		when	not matched then
				insert (ObjectType, ObjectID, FieldTypeID, Value) values (S.ObjectType, S.ObjectID, S.FieldTypeID, S.Value);
	end

	---- Add new relations as needed
	--exec [utility].[PromoteFusionAttributesRelations] @NumberOfNewRelations output

	---- Handle any fusionlookup fields
	--exec [utility].[PromoteFusionAttributeLookups]


	----Log this run done
	update	[fusion].[RuleLog]
	set		DateCompleted = CURRENT_TIMESTAMP, 
			[PromotedTaxonomies] = @NumberOfNewTaxonomies, 
			[PromotedDomainItems] = @NumberOfNewReferenceItems,  
			[PromotedDomains] = @NumberOfNewReferences,
			[PromotedArtifacts] = @NumberOfNewArtifacts,
			[TotalNewPromotions] = (@NumberOfNewTaxonomies + @NumberOfNewReferenceItems + @NumberOfNewReferences + @NumberOfNewArtifacts),
			[AttributesConsidered]= @NumberOfAttributesTotal,
			[NumberOfRules] = @NumberOfRules ,
			[RelationshipsAdded] = @NumberOfNewRelations
	where	ID = @ExecutionID;
END
GO

alter procedure [utility].[AddAuditEntry]
--declare
	@Object varchar(50),
	@ObjectID int,
	@ResourceID int,
	@Date datetime,
	@Action varchar(15),
	@ActionObject varchar(50),
	@ActionObjectID int
--set @Object = 'Taxonomy'--'Artifact'
--set @ObjectID = 229--733
--set @ResourceID = 1
--set @Action = 'Updated'
--set @ActionObject = 'Taxonomy' --'Artifact'
--set @ActionObjectID = 229 --733
as
begin
	set nocount on;
	declare @objectName nvarchar(250),
			@actionObjectTypeName nvarchar(250),
			@actionObjectName nvarchar(250),
			@actionDescription nvarchar(max)
	
	declare @tbl table (ID int identity, FieldTypeID int, FieldName nvarchar(250), NewValue nvarchar(max), MostRecentVersion int, Updated bit)

	-- Object Resolution --------------------------------------------------
	if @Object = 'Artifact'				begin		select @objectName = Name from Artifact where ID = @ObjectID				end
	if @Object = 'ArtifactType'			begin		select @objectName = Name from ArtifactType where ID = @ObjectID			end
	if @Object = 'AttributeType'		begin		select @objectName = Name from AttributeType where ID = @ObjectID			end
	if @Object = 'Fusion'				begin		select @objectName = Name from Fusion where ID = @ObjectID					end
	if @Object = 'FusionAttribute'		begin		select @objectName = TextPath from FusionAttribute where ID = @ObjectID		end
	if @Object = 'FusionAttributeType'	begin		select @objectName = Name from FusionAttributeType where ID = @ObjectID		end
	if @Object = 'FusionType'			begin		select @objectName = Name from FusionType where ID = @ObjectID				end
	if @Object = 'Group'				begin		select @objectName = Name from [Group] where ID = @ObjectID					end
	if @Object = 'Intersect'			begin		select @objectName = Name from [Intersect] where ID = @ObjectID				end
	if @Object = 'IntersectType'		begin		select @objectName = Name from IntersectType where ID = @ObjectID			end
	if @Object = 'LoadType'				begin		select @objectName = Name from LoadType where ID = @ObjectID				end
	if @Object = 'LookupType'			begin		select @objectName = Name from LookupType where ID = @ObjectID				end
	if @Object = 'Policy'				begin		select @objectName = Name from Policy where ID = @ObjectID					end
	if @Object = 'ReferenceItemType'	begin		select @objectName = Name from ReferenceItemType where ID = @ObjectID		end
	if @Object = 'Report'				begin		select @objectName = Name from Report where ID = @ObjectID					end
	if @Object = 'ResponsibilityType'	begin		select @objectName = Name from ResponsibilityType where ID = @ObjectID		end
	if @Object = 'Rule'					begin		select @objectName = Name from [Rule] where ID = @ObjectID					end
	if @Object = 'StatisticType'		begin		select @objectName = Name from StatisticType where ID = @ObjectID			end
	if @Object = 'SurveyType'			begin		select @objectName = Name from SurveyType where ID = @ObjectID				end
	if @Object = 'Taxonomy'				begin		select @objectName = Name from Taxonomy where ID = @ObjectID				end
	if @Object = 'TaxonomyType'			begin		select @objectName = Name from TaxonomyType where ID = @ObjectID			end
	----------------------------------------------------------------------

	-- Action Object Resolution ------------------------------------------

	-- Relevant ONLY to: Artifact, ArtifactType
	if @ActionObject = 'Artifact'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = O.TextPath
		from	Artifact O
				inner join ArtifactType T on T.ID = O.ArtifactTypeID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from Artifact where ID = @ActionObjectID
		insert into @tbl  select 0, 'ParentID', ParentID, 0, 0 from Artifact where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from Artifact where ID = @ActionObjectID
		insert into @tbl  select 0, 'TaxonomyTypeID', TaxonomyTypeID, 0, 0 from Artifact where ID = @ActionObjectID
		insert into @tbl  select 0, 'Status', Status, 0, 0 from Artifact where ID = @ActionObjectID
		insert into @tbl  select 0, 'DateLastCertified', DateLastCertified, 0, 0 from Artifact where ID = @ActionObjectID
	end

	-- Relevant ONLY to: ArtifactType
	if @ActionObject = 'ArtifactType'
	begin
		select	@actionObjectTypeName = 'Artifact Type',
				@actionObjectName = O.Name 
		from	ArtifactType O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from ArtifactType where ID = @ActionObjectID
		insert into @tbl  select 0, 'ParentID', ParentID, 0, 0 from ArtifactType where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from ArtifactType where ID = @ActionObjectID
		insert into @tbl  select 0, 'CanOwnFusion', CanOwnFusion, 0, 0 from ArtifactType where ID = @ActionObjectID
		--insert into @tbl  select 0, 'SourcingApplies', SourcingApplies, 0, 0 from ArtifactType where ID = @ActionObjectID
		insert into @tbl  select 0, 'AllowRelatedArtifacts', AllowRelatedArtifacts, 0, 0 from ArtifactType where ID = @ActionObjectID
	end
	
	-- Relevant ONLY to: Artifact, Fusion, FusionAttribute, Intersect, Taxonomy
	if @ActionObject = 'Attribute'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = T.Name + ' Attribute ' + cast(O.ID as nvarchar(15)) 
		from	Attribute O
				inner join AttributeType T on T.ID = O.AttributeTypeID
		where	O.ID = @ActionObjectID
	end

	-- Relevant ONLY to: AttributeType
	if @ActionObject = 'AttributeType'
	begin
		select	@actionObjectTypeName = 'Attribute Type',
				@actionObjectName = O.Name
		from	AttributeType O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from AttributeType where ID = @ActionObjectID
		insert into @tbl  select 0, 'ParentID', ParentID, 0, 0 from AttributeType where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from AttributeType where ID = @ActionObjectID
		insert into @tbl  select 0, 'TextFormatString', TextFormatString, 0, 0 from AttributeType where ID = @ActionObjectID
		insert into @tbl  select 0, 'AttributeTypeCategoryID', AttributeTypeCategoryID, 0, 0 from AttributeType where ID = @ActionObjectID
	end

	-- Relevant ONLY to: Rule
	if @ActionObject = 'EventGroup'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = O.Name 
		from	EventGroup O
				inner join [Rule] T on T.ID = O.RuleID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from EventGroup where ID = @ActionObjectID
		insert into @tbl  select 0, 'PublicID', PublicID, 0, 0 from EventGroup where ID = @ActionObjectID
	end

	-- Relevant ONLY to: Fusion
	if @ActionObject = 'Fusion'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = O.Name 
		from	Fusion O
				inner join FusionType T on T.ID = O.FusionTypeID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from Fusion where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from Fusion where ID = @ActionObjectID
		insert into @tbl  select 0, 'Enabled', Enabled, 0, 0 from Fusion where ID = @ActionObjectID
		insert into @tbl  select 0, 'Manual', Manual, 0, 0 from Fusion where ID = @ActionObjectID
		insert into @tbl  select 0, 'LockPromotedItems', LockPromotedItems, 0, 0 from Fusion where ID = @ActionObjectID
		insert into @tbl  select 0, 'IntervalType', IntervalType, 0, 0 from Fusion where ID = @ActionObjectID
		insert into @tbl  select 0, 'Interval', Interval, 0, 0 from Fusion where ID = @ActionObjectID
		insert into @tbl  select 0, 'ForceRefresh', ForceRefresh, 0, 0 from Fusion where ID = @ActionObjectID
	end

	-- Relevant ONLY to: FusionAttributeType, FusionType
	if @ActionObject = 'FusionAttributeType'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = O.Name 
		from	FusionAttributeType O
				inner join FusionType T on T.ID = O.FusionTypeID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from FusionAttributeType where ID = @ActionObjectID
		insert into @tbl  select 0, 'Assignable', Assignable, 0, 0 from FusionAttributeType where ID = @ActionObjectID
	end

	-- Relevant ONLY to: FusionType
	if @ActionObject = 'FusionType'
	begin
		select	@actionObjectTypeName = 'Fusion Type',
				@actionObjectName = O.Name 
		from	FusionType O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from FusionType where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from FusionType where ID = @ActionObjectID
	end

	-- Relevant ONLY to: Group
	if @ActionObject = 'Group'
	begin
		select	@actionObjectTypeName = 'Group',
				@actionObjectName = O.Name 
		from	[Group] O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from [Group] where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from [Group] where ID = @ActionObjectID
		insert into @tbl  select 0, 'PrimaryOwnerResourceID', PrimaryOwnerResourceID, 0, 0 from [Group] where ID = @ActionObjectID
		insert into @tbl  select 0, 'SecondaryOwnerResourceID', SecondaryOwnerResourceID, 0, 0 from [Group] where ID = @ActionObjectID
	end

	-- Relevant ONLY to: Artifact, FusionAttribute, Intersect, Taxonomy, Policy, Rule
	if @ActionObject = 'Intersect'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = O.Name 
		from	[Intersect] O
				inner join [IntersectType] T on T.ID = O.IntersectTypeID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Classification', Classification, 0, 0 from [Intersect] where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from [Intersect] where ID = @ActionObjectID
	end

	-- Relevant ONLY to: IntersectType
	if @ActionObject = 'IntersectType'
	begin
		select	@actionObjectTypeName = 'Intersect Type',
				@actionObjectName = O.Name 
		from	IntersectType O
		where	O.ID = @ActionObjectID

		--insert into @tbl  select 0, 'ReadOnly', [ReadOnly], 0, 0 from IntersectType where ID = @ActionObjectID
		--insert into @tbl  select 0, 'IsTechnical', IsTechnical, 0, 0 from IntersectType where ID = @ActionObjectID
		--insert into @tbl  select 0, 'AllowContext', AllowContext, 0, 0 from IntersectType where ID = @ActionObjectID
	end

	-- Relevant ONLY to: LoadType
	if @ActionObject = 'LoadType'
	begin
		select	@actionObjectTypeName = 'Load Type',
				@actionObjectName = O.Name 
		from	LoadType O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from LoadType where ID = @ActionObjectID
	end

	-- Relevant ONLY to: LoadType
	if @ActionObject = 'LoadTypeField'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = O.Name 
		from	LoadTypeField O
				inner join LoadType T on T.ID = O.LoadTypeID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from LoadTypeField where ID = @ActionObjectID
		insert into @tbl  select 0, 'SortOrder', SortOrder, 0, 0 from LoadTypeField where ID = @ActionObjectID
		insert into @tbl  select 0, 'LookupObjectType', LookupObjectType, 0, 0 from LoadTypeField where ID = @ActionObjectID
		insert into @tbl  select 0, 'LookupObjectID', LookupObjectID, 0, 0 from LoadTypeField where ID = @ActionObjectID
		insert into @tbl  select 0, 'LookupFieldName', LookupFieldName, 0, 0 from LoadTypeField where ID = @ActionObjectID
	end

	-- Relevant ONLY to: LoadType
	if @ActionObject = 'LoadTypeRule'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = T.Name + ' Rule ' + cast(O.ID as nvarchar(15))
		from	LoadTypeRule O
				inner join LoadType T on T.ID = O.LoadTypeID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'LoadTypeRuleGroup', case LoadTypeRuleGroup when 1 then 'Promotion' when 2 then 'Relation' else 'Unknown' end, 0, 0 from LoadTypeRule where ID = @ActionObjectID
		insert into @tbl  select 0, 'SortOrder', SortOrder, 0, 0 from LoadTypeRule where ID = @ActionObjectID
		insert into @tbl  select 0, 'ObjectType', ObjectType, 0, 0 from LoadTypeRule where ID = @ActionObjectID
		insert into @tbl  select 0, 'ObjectID', ObjectID, 0, 0 from LoadTypeRule where ID = @ActionObjectID
		insert into @tbl  select 0, 'UniqueLoadTypeFieldID', UniqueLoadTypeFieldID, 0, 0 from LoadTypeRule where ID = @ActionObjectID
	end

	-- Relevant ONLY to: LoadType
	if @ActionObject = 'LoadTypeRuleItem'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = 'Rule Field ' + cast(O.ID as nvarchar(15))
		from	LoadTypeRuleItem O
				inner join LoadTypeRule R on R.ID = O.LoadTypeRuleID
				inner join LoadType T on T.ID = R.LoadTypeID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'SourceLoadTypeFieldID', SourceLoadTypeFieldID, 0, 0 from LoadTypeRuleItem where ID = @ActionObjectID
		insert into @tbl  select 0, 'TargetFieldName', TargetFieldName, 0, 0 from LoadTypeRuleItem where ID = @ActionObjectID
		insert into @tbl  select 0, 'IsCustomField', IsCustomField, 0, 0 from LoadTypeRuleItem where ID = @ActionObjectID
	end

	-- Relevant ONLY to: LookupType
	if @ActionObject = 'Lookup'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = T.Name + ' Lookup ' + cast(O.ID as nvarchar(15))
		from	[Lookup] O
				inner join LookupType T on T.ID = O.LookupTypeID
		where	O.ID = @ActionObjectID
	end

	-- Relevant ONLY to: LookupType
	if @ActionObject = 'LookupType'
	begin
		select	@actionObjectTypeName = 'Lookup Type',
				@actionObjectName = O.Name 
		from	LookupType O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from LookupType where ID = @ActionObjectID
	end

	-- Relevant ONLY to: Policy
	if @ActionObject = 'Policy'
	begin
		select	@actionObjectTypeName = 'Policy',
				@actionObjectName = O.Name 
		from	[Policy] O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from [Policy] where ID = @ActionObjectID
		insert into @tbl  select 0, 'ParentID', ParentID, 0, 0 from [Policy] where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from [Policy] where ID = @ActionObjectID
	end

	-- Relevant ONLY to: SurveyType
	if @ActionObject = 'QuestionType'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = O.Name 
		from	QuestionType O
				inner join SurveyType T on T.ID = O.SurveyTypeID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from QuestionType where ID = @ActionObjectID
		insert into @tbl  select 0, 'DisplayStyle', DisplayStyle, 0, 0 from QuestionType where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from QuestionType where ID = @ActionObjectID
	end

	-- Relevant ONLY to: ReferenceItem
	if @ActionObject = 'ReferenceItem'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = O.Code 
		from	ReferenceItem O
				inner join ReferenceItemType T on T.ID = O.ReferenceItemTypeID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Code', Code, 0, 0 from ReferenceItem where ID = @ActionObjectID
	end

	-- Relevant ONLY to: ReferenceItemType
	if @ActionObject = 'ReferenceItemType'
	begin
		select	@actionObjectTypeName = 'Reference Item Type',
				@actionObjectName = O.Name
		from	ReferenceItemType O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from ReferenceItemType where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from ReferenceItemType where ID = @ActionObjectID
		insert into @tbl  select 0, 'DisplayFormat', DisplayFormat, 0, 0 from ReferenceItemType where ID = @ActionObjectID
	end

	-- Relevant ONLY to: Report
	if @ActionObject = 'Report'
	begin
		select	@actionObjectTypeName = 'Report',
				@actionObjectName = O.Name
		from	Report O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from Report where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from Report where ID = @ActionObjectID
		insert into @tbl  select 0, 'ObjectType', ObjectType, 0, 0 from Report where ID = @ActionObjectID
		insert into @tbl  select 0, 'ObjectID', ObjectID, 0, 0 from Report where ID = @ActionObjectID
		insert into @tbl  select 0, 'ReportLayoutID', ReportLayoutID, 0, 0 from Report where ID = @ActionObjectID
	end

	-- Relevant ONLY to: Report
	if @ActionObject = 'ReportTile'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = O.Name 
		from	ReportTile O
				inner join Report T on T.ID = O.ReportID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from ReportTile where ID = @ActionObjectID
		insert into @tbl  select 0, 'ReportTileType', ReportTileType, 0, 0 from ReportTile where ID = @ActionObjectID
		insert into @tbl  select 0, 'ContentAreaNumber', ContentAreaNumber, 0, 0 from ReportTile where ID = @ActionObjectID
		insert into @tbl  select 0, 'CommandText', CommandText, 0, 0 from ReportTile where ID = @ActionObjectID
		insert into @tbl  select 0, 'Settings', cast(Settings as nvarchar(max)), 0, 0 from ReportTile where ID = @ActionObjectID
	end

	-- Relevant ONLY to: Artifact, ArtifactType, Intersect, Policy, Rule, Taxonomy, TaxonomyType, Vocabulary
	if @ActionObject = 'Responsibility'
	begin
		select	@actionObjectTypeName = 'Responsibility',
				@actionObjectName = T.Name 
		from	Responsibility O
				inner join ResponsibilityType T on T.ID = O.ResponsibilityTypeID

		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Context', (
				select	D.Name + ': ' + I.Code + '; '
				from	ResponsibilityContextItem C
						inner join ReferenceItem I on C.ObjectType = 'ReferenceItem' and C.ObjectID = I.ID
						inner join ReferenceItemType D on D.ID = I.ReferenceItemTypeID
				where	ResponsibilityID = @ActionObjectID
				for xml path ('')--, root('items')
				), 0, 0 from Responsibility where ID = @ActionObjectID
		insert into @tbl  select 0, 'ObjectType', ObjectType, 0, 0 from Responsibility where ID = @ActionObjectID
		insert into @tbl  select 0, 'ObjectID', ObjectID, 0, 0 from Responsibility where ID = @ActionObjectID
		insert into @tbl  select 0, 'ResponsibleObjectType', ResponsibleObjectType, 0, 0 from Responsibility where ID = @ActionObjectID
		insert into @tbl  select 0, 'ResponsibleObjectID', ResponsibleObjectID, 0, 0 from Responsibility where ID = @ActionObjectID
	end

	-- Relevant ONLY to: ResponsibilityType
	if @ActionObject = 'ResponsibilityType'
	begin
		select	@actionObjectTypeName = 'Responsibility Type',
				@actionObjectName = O.Name 
		from	ResponsibilityType O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from ResponsibilityType where ID = @ActionObjectID
		insert into @tbl  select 0, 'ResponsibilityTypeGroup', ResponsibilityTypeGroup, 0, 0 from ResponsibilityType where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from ResponsibilityType where ID = @ActionObjectID
	end

	-- Relevant ONLY to: Rule
	if @ActionObject = 'Rule'
	begin
		select	@actionObjectTypeName = 'Rule',
				@actionObjectName = O.Name 
		from	[Rule] O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from [Rule] where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from [Rule] where ID = @ActionObjectID
		insert into @tbl  select 0, 'RuleType', RuleType, 0, 0 from [Rule] where ID = @ActionObjectID
	end

	-- Relevant ONLY to: StatisticType
	if @ActionObject = 'StatisticType'
	begin
		select	@actionObjectTypeName = 'Statistic Type',
				@actionObjectName = O.Name 
		from	StatisticType O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from StatisticType where ID = @ActionObjectID
		insert into @tbl  select 0, 'CheckType', CheckType, 0, 0 from StatisticType where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from StatisticType where ID = @ActionObjectID
		insert into @tbl  select 0, 'PartOfScore', PartOfScore, 0, 0 from StatisticType where ID = @ActionObjectID
		insert into @tbl  select 0, 'Configuration', cast(Configuration as nvarchar(max)), 0, 0 from StatisticType where ID = @ActionObjectID
	end

	-- Relevant ONLY to: SurveyType
	if @ActionObject = 'SurveyType'
	begin
		select	@actionObjectTypeName = 'Survey Type',
				@actionObjectName = O.Name 
		from	SurveyType O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from SurveyType where ID = @ActionObjectID
		insert into @tbl  select 0, 'Object', Object, 0, 0 from SurveyType where ID = @ActionObjectID
		insert into @tbl  select 0, 'ObjectID', ObjectID, 0, 0 from SurveyType where ID = @ActionObjectID
	end

	-- Relevant ONLY to: Taxonomy, TaxonomyType
	if @ActionObject = 'Taxonomy'
	begin
		select	@actionObjectTypeName = T.Name + ' model',
				@actionObjectName = O.TextPath
		from	Taxonomy O
				inner join TaxonomyType T on T.ID = O.TaxonomyTypeID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from Taxonomy where ID = @ActionObjectID
		insert into @tbl  select 0, 'ParentID', ParentID, 0, 0 from Taxonomy where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from Taxonomy where ID = @ActionObjectID
		insert into @tbl  select 0, 'Level', [Level], 0, 0 from Taxonomy where ID = @ActionObjectID
	end

	-- Relevant ONLY to: TaxonomyType
	if @ActionObject = 'TaxonomyType'
	begin
		select	@actionObjectTypeName = 'Model Type',
				@actionObjectName = O.Name
		from	TaxonomyType O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from TaxonomyType where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from TaxonomyType where ID = @ActionObjectID
		insert into @tbl  select 0, 'MaximumDepth', MaximumDepth, 0, 0 from TaxonomyType where ID = @ActionObjectID
		--insert into @tbl  select 0, 'Class', Class, 0, 0 from TaxonomyType where ID = @ActionObjectID
	end

	-- Get the dynamic fields for the actional object, if available for this type.
	if @ActionObject in ('Artifact', 'Attribute', 'Event', 'Fusion', 'FusionAttribute', 'Lookup', 'Resource', 'Taxonomy') 
	begin
		insert into @tbl  
			select	FieldTypeID, 
					FriendlyName, 
					FormattedValue, 
					0, 
					0 
			from	FieldWithRelation
			where	ObjectType = @ActionObject 
					and ObjectID = @ActionObjectID
	end
	----------------------------------------------------------------------


	-- Now, determine the description, and whether to create audit row ---

	update	T
	set		T.MostRecentVersion = coalesce(S.[Version], 0),
			T.Updated = case 
							when T.NewValue = S.Value then 0
							when T.NewValue is null and S.Value is null then 0
							else 1
						end
	from	@tbl T
			left join (
						select	V.*,
								F.Value
						from	reporting.Global_Audit A
								inner join reporting.Global_FieldAudit F on F.AuditID = A.ID and A.[Object] = @Object and A.ObjectID = @ObjectID and A.ActionObject = @ActionObject and A.ActionObjectID = @ActionObjectID
								inner join (
											select		F.FieldTypeID,
														F.FieldName,
														max([Version]) as [Version]
											from		reporting.Global_Audit A
														inner join reporting.Global_FieldAudit F on F.AuditID = A.ID and A.[Object] = @Object and A.ObjectID = @ObjectID and A.ActionObject = @ActionObject and A.ActionObjectID = @ActionObjectID
											group by	F.FieldTypeID,
														F.FieldName
											) V on V.FieldTypeID = F.FieldTypeID and V.FieldName = F.FieldNAme and V.[Version] = F.[Version]
						) S on (S.FieldTypeID = 0 and S.FieldTypeID = T.FieldTypeID and S.FieldName = T.FieldName) or (S.FieldTypeID > 0 and S.FieldTypeID = T.FieldTypeID)

	declare	@auditID bigint,
			@current int = 1, 
			@max int,
			@fieldTypeID int,
			@fieldName nvarchar(250),
			@version int,
			@value nvarchar(max),
			@updated bit
	select	@max = max(ID) from @tbl

	if @Action = 'Created'
		begin
			set @actionDescription = @actionObjectTypeName + ' created.'
		end
	else
		begin
			while @current <= @max
			begin
				select	@fieldTypeID = FieldTypeID,
						@fieldName = FieldName,
						@version = MostRecentVersion,
						@value = NewValue,
						@updated = Updated
				from	@tbl
				where	ID = @current

				if @updated = 1
				begin
					set @actionDescription = coalesce(@actionDescription + ', ', '') + @fieldName + case when @version > 0 then ' updated' else ' added' end
				end

				set @current = @current + 1
			end
		end
	
	--select @Object, @ObjectID, @ObjectName, @ResourceID, @Date, @Action, @ActionObject, @ActionObjectID, @actionObjectTypeName, @actionObjectName, @actionDescription

	if @actionDescription is not null and @objectName is not null
	begin
		set @actionDescription = @actionDescription + '.'

		insert into [reporting].[Global_Audit] values (@Object, @ObjectID, @objectName, coalesce(@ResourceID, 0), @Date, @Action, @ActionObject, @ActionObjectID, @actionObjectTypeName, @actionObjectName, @actionDescription)
		select @auditID = SCOPE_IDENTITY()

		set @current = 1
		while @current <= @max
		begin
			select	@fieldTypeID = FieldTypeID,
					@fieldName = FieldName,
					@version = MostRecentVersion,
					@value = NewValue,
					@updated = Updated
			from	@tbl
			where	ID = @current

			if @updated = 1
			begin
				insert into [reporting].[Global_FieldAudit] 
				values	(
						@auditID, 
						@fieldTypeID, 
						@fieldName, 
						@version + 1, 
						@value --case FormattedValue when '' then 'EMPTY' else coalesce(FormattedValue, 'NULL') end
						) 		
			end

			set @current = @current + 1
		end
	end
	----------------------------------------------------------------------
end
GO

ALTER  FUNCTION [utility].[ObjectDetail]
(
--declare
	@type varchar(50), 
	@id int
--set @type = 'Domain'
--set @id = 1
)
RETURNS @tbl TABLE 
(
	ID int,
	Name nvarchar(250),
	TextPath nvarchar(2500),
	Description nvarchar(4000),
	ParentID int null,
	ParentType nvarchar(250),
	Url nvarchar(2500),
	TypeID int,
	[Type] varchar(25),
	[TypeName] nvarchar(250),
	IconBackColor varchar(15),
	IconForeColor varchar(15),
	IconText varchar(15),
	Status nvarchar(25) null
) 
AS
BEGIN
	if @type = 'Artifact'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,													TypeID,				[Type],			TypeName, Status)
			SELECT			O.ID,	O.Name,	O.TextPath,	O.Description,	O.ParentID,	@type,		dbo.GenerateObjectUrl(@type, O.ArtifactTypeID, O.ID),	O.ArtifactTypeID,	'ArtifactType',	T.Name, O.Status
			FROM	Artifact O
					INNER JOIN ArtifactType T ON O.ArtifactTypeID = T.ID and O.ID = @id
	end

	if @type = 'ArtifactType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		Name,	Name,		Description,	NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Artifact Type'
			FROM	ArtifactType O
			WHERE	ID = @id
	end

	if @type = 'Attribute'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,													TypeID,				[Type],				TypeName)
			SELECT			O.ID,	'',		'',			'',				O.ParentID,	@type,		D.Url,	O.AttributeTypeID,	'AttributeType',	T.Name
			FROM	[Attribute] O
					INNER JOIN AttributeType T ON O.AttributeTypeID = T.ID and O.ID = @id
					cross apply  utility.ObjectDetail(O.ObjectType, O.ObjectID) D
	end

	if @type = 'AttributeType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		Name,	Name,		Description,	ParentID,	@type,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Attribute Type'
			FROM	AttributeType
			WHERE	ID = @id
	end

	if @type = 'Group'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		Name,	Name,		Description,	NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	0,		@type,	'Group'
			FROM	[Group]
			WHERE	ID = @id
	end

	if @type = 'Intersect'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,													TypeID,				[Type],				TypeName)
			SELECT			O.ID,	O.Name,	O.Name,		'',				NULL,		@type,		dbo.GenerateObjectUrl(@type, O.IntersectTypeID, O.ID),	O.IntersectTypeID,	'IntersectType',	T.Name
			FROM	[Intersect] O
					INNER JOIN IntersectType T ON O.IntersectTypeID = T.ID and O.ID = @id
	end

	if @type = 'IntersectType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		Name,	Name,		'',				NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Intersect Type'
			FROM	IntersectType
			WHERE	ID = @id
	end

	if @type = 'Event'
	begin
		insert into @tbl (	ID,		Name,				TextPath,	[Description],	ParentID,	ParentType, Url,												TypeID,			[Type],			TypeName)
			SELECT			O.ID,	T.Name + ' event',	T.Name,		'',				NULL,		NULL,		dbo.GenerateObjectUrl(@type, T.RuleID, O.ID),	T.RuleID,	'Rule',	T.Name
			FROM	[Event] O
					INNER JOIN EventGroup T ON O.EventGroupID = T.ID AND O.ID = @id
	end

	if @type = 'EventGroup'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID,			[Type], TypeName)
			SELECT			ID,		Name,	Name,		'',				NULL,		@type,		dbo.GenerateObjectUrl(@type, 0, ID),	RuleID,	'Rule',	'Rule'
			FROM	EventGroup O
			WHERE	ID = @id
	end

	if @type = 'Lookup'
	begin
		insert into @tbl (	ID,		Name,				TextPath,	[Description],	ParentID,	ParentType, Url,												TypeID,			[Type],			TypeName)
			SELECT			O.ID,	T.Name + ' Item',	T.Name,		'',				NULL,		NULL,		dbo.GenerateObjectUrl(@type, O.LookupTypeID, O.ID),	O.LookupTypeID,	'LookupType',	T.Name
			FROM	[Lookup] O
					INNER JOIN LookupType T ON O.LookupTypeID = T.ID AND O.ID = @id
	end

	if @type = 'LookupType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		Name,	Name,		'',				0,			@type,		dbo.GenerateObjectUrl(@type, ID, 0),	ID,		@type,	'Lookup Type'
			FROM	LookupType O
			WHERE	ID = @id
	end

	if @type = 'Fusion'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,												TypeID,			[Type],			TypeName)
			SELECT			O.ID,	O.Name,	O.Name,		'',				NULL,		@type,		dbo.GenerateObjectUrl(@type, O.FusionTypeID, O.ID),	O.FusionTypeID,	'FusionType',	T.Name
			FROM	Fusion O
					INNER JOIN FusionType T ON O.FusionTypeID = T.ID and O.ID = @id
	end

	if @type = 'FusionType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		Name,	Name,		'',				NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Fusion Type'
			FROM	FusionType O
			WHERE	ID = @id
	end

	if @type = 'FusionAttribute'
	begin
		insert into @tbl (	ID,		Name,		TextPath,	[Description],	ParentID,	ParentType, Url,	TypeID,						[Type],					TypeName)
			SELECT			O.ID,	coalesce(O.TextPath, O.Name),	O.TextPath,	'',				O.ParentID,	@type,		dbo.GenerateObjectUrl(@type, FT.ID, O.ID),
																											O.FusionAttributeTypeID,	'FusionAttributeType',	T.Name
			FROM	FusionAttribute O
					INNER JOIN FusionAttributeType T ON O.FusionAttributeTypeID = T.ID and O.ID = @id
					INNER JOIN FusionType FT ON T.FusionTypeID = FT.ID
	end

	if @type = 'FusionAttributeType'
	begin
		insert into @tbl (	ID, Name,		TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,	O.Name,	O.TextPath,	'',				NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Fusion Attribute Type'
			FROM	FusionAttributeType O
			WHERE	ID = @id
	end

	if @type = 'FusionQueryAttributeType'
	begin
		insert into @tbl (	ID, Name,		TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,	O.Name,	O.Name,	'',				NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Fusion Query Attribute Type'
			FROM	FusionQueryAttributeType O
			WHERE	ID = @id
	end

	if @type = 'Policy'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,	TypeID,				[Type],			TypeName)
			SELECT			O.ID,	O.Name,	O.TextPath,	O.Description,	NULL,		@type,		dbo.GenerateObjectUrl(@type, 0, O.ID),	T.ID,	'PolicyType',	T.Name
			FROM	[Policy] O
					INNER JOIN PolicyType T ON O.PolicyTypeID = T.ID AND O.ID = @id
	end

	if @type = 'PolicyType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID,	[Type],	TypeName)
			SELECT			O.ID,	O.Name,	O.Name,		O.Description,	NULL,		NULL,		dbo.GenerateObjectUrl(@type, O.ID, O.ID),	C.ID,	@type,	C.Name
			FROM	PolicyType O
					inner join PolicyTypeClass C on C.ID = O.PolicyTypeClassID
			WHERE	O.ID = @id
	end

	if @type = 'ReferenceItemType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		Name,	Name,		Description,	NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Reference Item Type'
			FROM	ReferenceItemType
			WHERE	ID = @id
	end

	if @type = 'Report'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,	TypeID,				[Type],			TypeName)
			SELECT			O.ID,	O.Name,	O.Name,	O.Description,	NULL,		@type,		'#',	0,	'Report',	'Report'
			FROM	Report O
			WHERE	O.ID = @id
	end

	if @type = 'Resource'
	begin
		insert into @tbl (ID, Name, Url, TypeID, [Type], TypeName)
			select	ResourceID, FirstName + ' ' + LastName, dbo.GenerateObjectUrl(@type, 1, @id), 1, 'ResourceType', 'Employee'
			from	reporting.Global_Resource 
			where	ResourceID = @id
	end

		if @type = 'ResponsibilityType'
	begin
		insert into @tbl (	ID, Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,	O.Name,	NULL,		Description,	NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Responsibility Type'
			FROM	ResponsibilityType O
			WHERE	ID = @id
	end

	if @type = 'ResourceType'
	begin
		insert into @tbl (ID, Name, Url, TypeID, [Type], TypeName)
		values			(@id, 'Resource Type', '#/resources/administration', @id, @type, 'Resource Type')
	end

	if @type = 'Rule'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,	TypeID,				[Type],			TypeName, Status)
			SELECT			O.ID,	O.Name,	O.Name,	O.Description,	NULL,		@type,		dbo.GenerateObjectUrl(@type, 0, O.ID),	O.RuleType,	'RuleType',	'Rule', case O.Status when 1 then 'Draft' when 2 then 'Active' else 'Inactive' end
			FROM	[Rule] O
			WHERE	O.ID = @id
	end

	if @type = 'StatisticType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		Name,	Name,		Description,	NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Analytic Type'
			FROM	StatisticType O
			WHERE	ID = @id
	end

	if @type = 'Taxonomy'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,													TypeID,				[Type],			TypeName)
			SELECT			O.ID,	O.Name,	O.TextPath,	O.Description,	O.ParentID,	@type,		dbo.GenerateObjectUrl(@type, O.TaxonomyTypeID, O.ID),	O.TaxonomyTypeID,	'TaxonomyType',	C.Name + ' Model'
			FROM	Taxonomy O
					INNER JOIN TaxonomyType T ON O.TaxonomyTypeID = T.ID AND O.ID = @id
					inner join TaxonomyTypeClass C on C.ID = T.TaxonomyTypeClassID
	end

	if @type = 'TaxonomyType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID,	[Type],	TypeName)
			SELECT			O.ID,	O.Name,	O.Name,		O.Description,	NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, O.ID),	C.ID,	@type,	C.Name
			FROM	TaxonomyType O
					inner join TaxonomyTypeClass C on C.ID = O.TaxonomyTypeClassID
			WHERE	O.ID = @id
	end

	update	T
	set		T.IconBackColor = coalesce(S.IconBackColor, '#000000'),
			T.IconForeColor = coalesce(S.IconForeColor, '#ffffff'),
			T.IconText =	--case @type
							--	when 'Taxonomy' then 'IM'
							--	when 'TaxonomyType' then 'IM'
								--else 
								COALESCE(S.IconText, 'leaf') 
							--end
	from	@tbl T
			left join ObjectStyle S ON S.ObjectType = T.[Type] and S.ObjectID = T.TypeID

	RETURN
END
GO

ALTER FUNCTION [dbo].[ArtifactNgSiteNavigation](@id int)
RETURNS XML
WITH RETURNS NULL ON NULL INPUT
BEGIN 
	RETURN 
	(
	SELECT	name,
			url,
			'Menu_AT' + cast(id as varchar(15)) as menuID,
			0 as feature,
			dbo.ArtifactNgSiteNavigation(id) as items
	FROM	(
			--SELECT	A.name,
			--		A.url,
			--		NULL AS items
			--FROM	(
					SELECT		TOP 1000
								a.id,
								a.name,
								dbo.GenerateNgObjecturl('ArtifactType', a.ID, 0) As url
					FROM		ArtifactType a
					LEFT JOIN SiteNav v on v.ObjectID = a.ID and v.Object = 'ArtifactType'
					WHERE		a.ParentID = @id AND v.ObjectID IS NULL
					ORDER BY	a.name
			--		) A
			) BG
			FOR XML PATH('nav'), TYPE
	)
END
GO

ALTER FUNCTION [dbo].[GenerateNgObjectUrl] 
(
	@Type varchar(50),
	@TypeID int,
	@ObjectID int = 0
)
RETURNS varchar(500)
AS
BEGIN
	DECLARE @Prefix varchar(5) = ''--'a/'
	DECLARE @Url varchar(500)
	SET @Url = @Prefix

	SET @Url = CASE @Type
		WHEN 'Artifact' THEN 'artifact/' +  + CAST(@TypeID as varchar) + '/' + CAST(@ObjectID as varchar)
		WHEN 'ArtifactType' THEN 'artifact/' + CAST(@TypeID as varchar)
		WHEN 'Domain' THEN 'domain/' +  + CAST(@TypeID as varchar) + '/' +  + CAST(@ObjectID as varchar)
		WHEN 'DomainType' THEN 'domain/' + CAST(@TypeID as varchar)
		WHEN 'ReferenceItem' THEN 'reference/' +  + CAST(@TypeID as varchar)-- + '/' +  + CAST(@ObjectID as varchar)
		WHEN 'ReferenceItemType' THEN 'reference/' + CAST(@TypeID as varchar)
		WHEN 'FusionAttribute' THEN 'fusion/fusionattribute/' + CAST(@TypeID as varchar) + '/' + CAST(@ObjectID as varchar)		
		WHEN 'Fusion' THEN 'fusion/' + CAST(@TypeID as varchar) + '/' + + CAST(@ObjectID as varchar)
		WHEN 'FusionType' THEN 'fusion/' + CAST(@TypeID as varchar)
		WHEN 'Group' THEN 'groups/' + CAST(@ObjectID as varchar)	
		WHEN 'Lookup' THEN 'admin/lookups/' + CAST(@TypeID as varchar) + '/' + + CAST(@ObjectID as varchar)
		WHEN 'LookupType' THEN 'admin/lookups/' + CAST(@TypeID as varchar)
		WHEN 'Policy' THEN 'policy/' + CAST(@TypeID as varchar(15)) + '/id/' + CAST(@ObjectID as varchar)
		WHEN 'PolicyType' THEN 'policy/' + CAST(@TypeID as varchar) + '/structure'		
		WHEN 'Resource' THEN 'resource/' + CAST(@ObjectID as varchar)
		WHEN 'ResourceType' THEN 'resource/list/' + CAST(@TypeID as varchar)
		WHEN 'Rule' THEN 'quality/rule/' + CAST(@ObjectID as varchar)
		WHEN 'Taxonomy' THEN 'model/' + CAST(@TypeID as varchar) + '/id/' + CAST(@ObjectID as varchar)
		WHEN 'TaxonomyType' THEN 'model/' + CAST(@ObjectID as varchar) + '/structure'		
	END

	SET @Url = @Prefix + @Url

	RETURN @Url
END
GO

ALTER FUNCTION [utility].[DeriveIntersectName] 
(	
	@id int
)
RETURNS nvarchar(500)
AS
BEGIN
	DECLARE @result nvarchar(500)

	SET @result =	(
					SELECT	COALESCE(SA.TextPath, SD.Name, SF.TextPath, SP.TextPath, SR.Name, ST.TextPath, SI.Name, '') + ' / ' + COALESCE(OA.TextPath, OD.Name, [OF].TextPath, OP.TextPath, [OR].Name, OT.TextPath, '')
					FROM	[Intersect] I
							left join Artifact SA on I.Subject = 'Artifact' and SA.ID = I.SubjectID
							left join Artifact OA on I.Object = 'Artifact' and OA.ID = I.ObjectID

							left join ReferenceItemType SD on I.Subject = 'ReferenceItemType' and SD.ID = I.SubjectID
							left join ReferenceItemType OD on I.Object = 'ReferenceItemType' and OD.ID = I.ObjectID

							left join [FusionAttribute] SF on I.Subject = 'FusionAttribute' and SF.ID = I.SubjectID
							left join [FusionAttribute] [OF] on I.Object = 'FusionAttribute' and [OF].ID = I.ObjectID


							left join [Intersect] SI on I.Subject = 'Intersect' and SI.ID = I.SubjectID

							left join [Policy] SP on I.Subject = 'Policy' and SP.ID = I.SubjectID
							left join [Policy] OP on I.Object = 'Policy' and OP.ID = I.ObjectID

							left join [Rule] SR on I.Subject = 'Rule' and SR.ID = I.SubjectID
							left join [Rule] [OR] on I.Object = 'Rule' and [OR].ID = I.ObjectID

							left join [Taxonomy] ST on I.Subject = 'Taxonomy' and ST.ID = I.SubjectID
							left join [Taxonomy] OT on I.Object = 'Taxonomy' and OT.ID = I.ObjectID

					WHERE	I.ID = @id
					FOR XML PATH('')
					)

	RETURN @result
END
GO

ALTER FUNCTION [utility].[DeriveIntersectTypeName] 
(
--declare
	@id int
--set @id = 17
)
RETURNS nvarchar(500)
AS
BEGIN
	DECLARE @result nvarchar(500)

	SET @result =	(
					SELECT	COALESCE(SA.Name, SD.Name, SF.TextPath, SP.Name, ST.Name, SI.Name, case I.Subject when 'RuleType' then 'Rule' else '' end) + 
							' ' + coalesce(P.Name,'/') + ' ' + 
							COALESCE(OA.Name, OD.Name, [OF].TextPath, OP.Name, OT.Name, case I.Object when 'RuleType' then 'Rule' else '' end)
					FROM	[IntersectType] I
							left join ArtifactType SA on I.Subject = 'ArtifactType' and SA.ID = I.SubjectID
							left join ArtifactType OA on I.Object = 'ArtifactType' and OA.ID = I.ObjectID

							left join ReferenceItemType SD on I.Subject = 'ReferenceItemType' and SD.ID = I.SubjectID
							left join ReferenceItemType OD on I.Object = 'ReferenceItemType' and OD.ID = I.ObjectID

							left join [FusionAttributeType] SF on I.Subject = 'FusionAttributeType' and SF.ID = I.SubjectID
							left join [FusionAttributeType] [OF] on I.Object = 'FusionAttributeType' and [OF].ID = I.ObjectID


							left join [IntersectType] SI on I.Subject = 'IntersectType' and SI.ID = I.SubjectID

							left join [PolicyType] SP on I.Subject = 'PolicyType' and SP.ID = I.SubjectID
							left join [PolicyType] OP on I.Object = 'PolicyType' and OP.ID = I.ObjectID

							left join [TaxonomyType] ST on I.Subject = 'TaxonomyType' and ST.ID = I.SubjectID
							left join [TaxonomyType] OT on I.Object = 'TaxonomyType' and OT.ID = I.ObjectID

							left join [Predicate] P on P.ID = I.PredicateID
					WHERE	I.ID = @id
					FOR XML PATH('')
					)

	RETURN @result
END
GO

ALTER FUNCTION [utility].[GetBreadcrumb]
(
	@Type varchar(50),
	@ID int
)
RETURNS XML
AS
BEGIN
	-- Declare the return variable here
	DECLARE @breadcrumb xml
	SET @breadcrumb = '<root/>'

	IF (@Type = 'Artifact')
	BEGIN
		WITH H (Name, ParentID, ID, [level])
		AS
		(
			SELECT	Name, 
					ParentID, 
					ID, 
					0
			FROM	Artifact
			WHERE	ID = @ID		
			UNION ALL
			SELECT	P.Name, 
					P.ParentID, 
					P.ID, 
					C.[level] + 1
			FROM	Artifact	P
					INNER JOIN H AS C ON C.ParentID = P.ID and @@NESTLEVEL < 6
		)
	
		SELECT @breadcrumb =	(
								SELECT (
										SELECT	H.ID as "node/@id",
												H.Name as "node/@name"
										FROM	H
										ORDER BY H.level DESC
										FOR XML PATH(''), type
										) AS hierachy
								FOR XML PATH('')
								)
	END

	IF (@Type = 'FusionAttribute')
	BEGIN
		WITH H
		AS
		(
			SELECT	A.Name, 
					T.Name as [Type],
					A.ParentID, 
					A.ID, 
					0 as [Level]
			FROM	FusionAttribute A
					inner join FusionAttributeType T on T.ID = A.FusionAttributeTypeID
			WHERE	A.ID = @ID		
			UNION ALL
			SELECT	P.Name, 
					T.Name as [Type],
					P.ParentID, 
					P.ID, 
					C.[level] + 1 as [Level]
			FROM	FusionAttribute	P
					inner join FusionAttributeType T on T.ID = P.FusionAttributeTypeID and @@NESTLEVEL < 6
					INNER JOIN H AS C ON C.ParentID = P.ID
		)
	
		SELECT @breadcrumb =	(
								SELECT (
										SELECT	H.ID as "node/@id",
												H.Name as "node/@name",
												H.Type as "node/@type"
										FROM	H
										ORDER BY H.level DESC
										FOR XML PATH(''), type
										) AS hierachy
								FOR XML PATH('')
								)
	END

	IF (@Type = 'FusionAttributeType')
	BEGIN
		WITH H (Name, ParentID, ID, [level])
		AS
		(
			SELECT	Name, 
					ParentID, 
					ID, 
					0
			FROM	FusionAttributeType
			WHERE	ID = @ID		
			UNION ALL
			SELECT	P.Name, 
					P.ParentID, 
					P.ID, 
					C.[level] + 1
			FROM	FusionAttributeType	P
					INNER JOIN H AS C ON C.ParentID = P.ID and @@NESTLEVEL < 6
		)
	
		SELECT @breadcrumb =	(
								SELECT (
										SELECT	H.ID as "node/@id",
												H.Name as "node/@name"
										FROM	H
										ORDER BY H.level DESC
										FOR XML PATH(''), type
										) AS hierachy
								FOR XML PATH('')
								)
	END

	IF (@Type = 'Taxonomy')
	BEGIN
		WITH H (Name, CatalogID, ParentID, ID, [level])
		AS
		(
			SELECT	Name, 
					TaxonomyTypeID, 
					ParentID, 
					ID, 
					0
			FROM	Taxonomy
			WHERE	ID = @ID		
			UNION ALL
			SELECT	P.Name, 
					P.TaxonomyTypeID, 
					P.ParentID, 
					P.ID, 
					C.[level] + 1
			FROM	Taxonomy	P
					INNER JOIN H AS C ON C.ParentID = P.ID and @@NESTLEVEL < 6
		)
	
		SELECT @breadcrumb =	(
								SELECT (
										SELECT	H.ID as "node/@id",
												H.Name as "node/@name"
										FROM	H
										ORDER BY H.level DESC
										FOR XML PATH(''), type
										) AS hierachy
								FOR XML PATH('')
								)

		DECLARE @cName nvarchar(250)
		DECLARE @cID int
		SELECT	@cID = TT.ID,
				@cName = TT.Name
		FROM	TaxonomyType TT
				INNER JOIN Taxonomy T ON T.TaxonomyTypeID = TT.ID
		WHERE	T.ID = @ID
		SET @breadcrumb.modify('insert <catalog id="" name="" /> as first into (/hierachy)[1]') 
		SET @breadcrumb.modify(
		'replace value of (//catalog/@id)[1] 
		 with sql:variable("@cID")'
		)
		SET @breadcrumb.modify(
		'replace value of (//catalog/@name)[1] 
		 with sql:variable("@cName")'
		)
	END

	RETURN @breadcrumb
END
GO

ALTER FUNCTION [utility].[GetBreadcrumbString]
(
	@Type varchar(50),
	@ID int,
	@Delimiter varchar(10)
)
RETURNS nvarchar(1000)
AS
BEGIN
	-- Declare the return variable here
	DECLARE @breadcrumb nvarchar(1000)

	IF (@Type = 'Artifact')
	BEGIN
		WITH H
		AS
		(
			SELECT	Name, 
					ParentID, 
					ID, 
					0 as [Level]
			FROM	Artifact
			WHERE	ID = @ID		
			UNION ALL
			SELECT	P.Name, 
					P.ParentID, 
					P.ID, 
					C.[level] + 1
			FROM	Artifact	P
					INNER JOIN H AS C ON C.ParentID = P.ID and @@NESTLEVEL < 6
		)

		SELECT @breadcrumb =	COALESCE(@breadcrumb + @Delimiter, '') + H.Name
								FROM	H
								ORDER BY H.level DESC

	END

	IF (@Type = 'FusionAttribute')
	BEGIN
		WITH H (Name, ParentID, ID, [level])
		AS
		(
			SELECT	Name, 
					ParentID, 
					ID, 
					0
			FROM	FusionAttribute
			WHERE	ID = @ID		
			UNION ALL
			SELECT	P.Name, 
					P.ParentID, 
					P.ID, 
					C.[level] + 1
			FROM	FusionAttribute	P
					INNER JOIN H AS C ON C.ParentID = P.ID and @@NESTLEVEL < 6
		)
	
		SELECT @breadcrumb =	COALESCE(@breadcrumb + @Delimiter, '') + H.Name
								FROM	H
								ORDER BY H.level DESC
	END

	IF (@Type = 'FusionAttributeType')
	BEGIN
		WITH H (Name, ParentID, ID, [level])
		AS
		(
			SELECT	Name, 
					ParentID, 
					ID, 
					0
			FROM	FusionAttributeType
			WHERE	ID = @ID		
			UNION ALL
			SELECT	P.Name, 
					P.ParentID, 
					P.ID, 
					C.[level] + 1
			FROM	FusionAttributeType	P
					INNER JOIN H AS C ON C.ParentID = P.ID and @@NESTLEVEL < 6
		)
	
		SELECT @breadcrumb =	COALESCE(@breadcrumb + @Delimiter, '') + H.Name
								FROM	H
								ORDER BY H.level DESC

		SELECT	@breadcrumb = FT.Name + @Delimiter + @breadcrumb
		FROM	FusionAttributeType FAT
				inner join FusionType FT on FAT.FusionTypeID = FT.ID and FAT.ID = @ID
	END

	IF (@Type = 'Policy')
	BEGIN
		WITH H
		AS
		(
			SELECT	Name, 
					ParentID, 
					ID, 
					0 as [Level]
			FROM	Policy
			WHERE	ID = @ID		
			UNION ALL
			SELECT	P.Name, 
					P.ParentID, 
					P.ID, 
					C.[level] + 1
			FROM	Policy	P
					INNER JOIN H AS C ON C.ParentID = P.ID and @@NESTLEVEL < 6
		)

		SELECT @breadcrumb =	COALESCE(@breadcrumb + @Delimiter, '') + H.Name
								FROM	H
								ORDER BY H.level DESC

	END

	IF (@Type = 'Taxonomy')
	BEGIN
		WITH H (Name, CatalogID, ParentID, ID, [level])
		AS
		(
			SELECT	Name, 
					TaxonomyTypeID, 
					ParentID, 
					ID, 
					0
			FROM	Taxonomy
			WHERE	ID = @ID		
			UNION ALL
			SELECT	P.Name, 
					P.TaxonomyTypeID, 
					P.ParentID, 
					P.ID, 
					C.[level] + 1
			FROM	Taxonomy	P
					INNER JOIN H AS C ON C.ParentID = P.ID and @@NESTLEVEL < 6
		)
	
		SELECT @breadcrumb =	COALESCE(@breadcrumb + @Delimiter, '') + H.Name
								FROM	H
								ORDER BY H.level DESC

		SELECT	@breadcrumb = T.Name + @Delimiter +  @breadcrumb
		FROM	TaxonomyType T 
				INNER JOIN Taxonomy O ON T.ID = O.TaxonomyTypeID WHERE O.ID = @ID 
	END

	RETURN @breadcrumb
END
GO

ALTER FUNCTION [utility].[GetFormattedFieldLookupValue]
(
	@Type varchar(25),
	@DisplayFormat nvarchar(250),
	@LookupObjectType varchar(25),
	@LookupObjectID int,
	@Value nvarchar(max)
)
RETURNS nvarchar(max)
AS
BEGIN
	declare @formattedValue nvarchar(max)
	
	if @LookupObjectType is null
	begin
		set @formattedValue  = @Value

		if @Type = 'Link' OR @Type = 'UncLink'
		begin
			declare @linkName nvarchar(max),
					@linkUrl nvarchar(max)

			if charindex('|', @Value, 1) > 1
				begin
					SELECT @linkName = SUBSTRING(@Value, 1, PATINDEX('%|%', @Value)-1)
					SELECT @linkUrl = SUBSTRING(@Value, PATINDEX('%|%', @Value)+1, LEN(@Value))

					set @formattedValue = '<a href="' + @linkUrl + '" target="_blank">' + @linkName + '</a>'
				end
			else
				begin
					if @Value <> '' AND @Value <> '|' AND @Value IS NOT NULL
						begin
							if LEFT(@Value, 1) = '|'
								begin
									--no name, default to url
									set @formattedValue = '<a href="' + SUBSTRING(@Value,2, LEN(@Value)) + '" target="_blank">' + SUBSTRING(@Value,2, LEN(@Value)) + '</a>'
								end
							else
								begin
									set @formattedValue = '<a href="' + @Value + '" target="_blank">' + @Value + '</a>'
								end
						end
					else
						begin
							set @formattedValue = null
						end
				end
		end

	end	
	else
	begin
		if @LookupObjectType = 'ReferenceItemType'
		begin
			select @formattedValue = Name from ReferenceItemType where id = @Value;		
		end
		else
		begin
			declare @tokens table(ID int identity(1,1), Token nvarchar(100), Field nvarchar(100))
			declare @fieldValues table(Field nvarchar(100), Value nvarchar(max), LookupObjectType nvarchar(250), LookupObjectID int, LookupDisplayFormat nvarchar(250))

			set @formattedValue = @DisplayFormat
	
			while patindex('%{%',@formattedValue) > 0
			 begin
				declare @txt nvarchar(100) = SUBSTRING(@formattedValue, patindex('%{%',@formattedValue), PATINDEX('%}%', @formattedValue))
				insert into @tokens Values (@txt, REPLACE(REPLACE(@txt,'{',''),'}',''))
				set @formattedValue = replace(@formattedValue, @txt, '')
			end

			insert into @fieldValues
				select	distinct
						V.Name,
						V.Value,
						V.LookupObjectType,
						V.LookupObjectID,
						V.LookupDisplayFormat
				from	(
						SELECT	ID,
								Name,
								'Artifact' as ObjectType
						FROM	ArtifactType
						WHERE	@LookupObjectType = 'Artifact' and ID = @LookupObjectID
						UNION
						SELECT	ID,
								Name,
								'Lookup' as ObjectType
						FROM	[LookupType]
						WHERE	@LookupObjectType = 'Lookup' and ID = @LookupObjectID
						UNION
						SELECT	ID,
								Name,
								'ReferenceItem' as ObjectType
						FROM	[ReferenceItemType]
						WHERE	@LookupObjectType = 'ReferenceItem' and ID = @LookupObjectID
						UNION										
						SELECT	1 as ID,
								'User' as Name,
								'Resource' as ObjectType
						WHERE	@LookupObjectType = 'Resource'-- and ID = @LookupObjectID
						UNION
						SELECT	ID,
								Name,
								'Taxonomy' as ObjectType
						FROM	TaxonomyType
						WHERE	@LookupObjectType = 'Taxonomy' and ID = @LookupObjectID
						) L
						outer apply (

									SELECT	IT.Name,
											[IF].Value,
											[IT].LookupObjectType,
											COALESCE([IT].LookupObjectID, 0) as LookupObjectID,
											[IT].LookupDisplayFormat
									FROM	Field [IF]
											inner join FieldType IT ON [IF].FieldTypeID = IT.ID 
																	and [IF].ObjectType = L.ObjectType
																	and [IF].ObjectID = case 
																							when dbo.IsInteger(@Value) = 1 then @Value
																							else 0
																						end
								
									UNION

									SELECT	P.FieldName as Name,
											p.FieldValue as Value,
											NULL as LookupObjectType,
											NULL as LookupObjectID,
											NULL as LookupDisplayFormat
									FROM	(
											SELECT	ID,
													CAST(Name as nvarchar(max)) as Name,
													CAST(Description as nvarchar(max)) as Description,
													CAST(TextPath as nvarchar(max)) as TextPath
											FROM	Artifact A
											WHERE	A.ID = CAST(@Value as int)
													and L.ObjectType = 'Artifact'
											) A
											unpivot	(
													FieldValue for FieldName in (Name, Description, TextPath)
													) p

									UNION

									SELECT	P.FieldName as Name,
											p.FieldValue as Value,
											NULL as LookupObjectType,
											NULL as LookupObjectID,
											NULL as LookupDisplayFormat
									FROM	(
											SELECT	ID,
													CAST(Name as nvarchar(max)) as Name,
													CAST(Description as nvarchar(max)) as Description,
													CAST(TextPath as nvarchar(max)) as TextPath
											FROM	Taxonomy A
											WHERE	A.ID = CAST(@Value as int)
													and L.ObjectType = 'Taxonomy'
											) A
											unpivot	(
													FieldValue for FieldName in (Name, Description, TextPath)
													) p

									UNION

									SELECT	P.FieldName as Name,
											p.FieldValue as Value,
											NULL as LookupObjectType,
											NULL as LookupObjectID,
											NULL as LookupDisplayFormat
									FROM	(
											SELECT	ID,
													CAST(Code as nvarchar(max)) as Code
											FROM	ReferenceItem A
											WHERE	A.ID = @Value
													and L.ObjectType = 'ReferenceItem'
											) A
											unpivot	(
													FieldValue for FieldName in (Code)
													) p

									UNION

									SELECT	P.FieldName as Name,
											p.FieldValue as Value,
											NULL as LookupObjectType,
											NULL as LookupObjectID,
											NULL as LookupDisplayFormat
									FROM	(
											SELECT	ID,
													CAST(Name as nvarchar(max)) as Name,
													CAST(Description as nvarchar(max)) as Description
											FROM	ReferenceItemType A
											WHERE	A.ID = @Value
													and L.ObjectType = 'ReferenceItemType'
											) A
											unpivot	(
													FieldValue for FieldName in (Name, Description)
													) p

									UNION

									SELECT	P.FieldName as Name,
											p.FieldValue as Value,
											NULL as LookupObjectType,
											NULL as LookupObjectID,
											NULL as LookupDisplayFormat
									FROM	(
											SELECT	ResourceID as ID,
													CAST(FirstName as nvarchar(max)) as FirstName,
													CAST(LastName as nvarchar(max)) as LastName,
													CAST(Email as nvarchar(max)) as Email
											FROM	reporting.Global_Resource A
											WHERE	A.ResourceID = @Value
													and L.ObjectType = 'Resource'
											) A
											unpivot	(
													FieldValue for FieldName in (FirstName, LastName, Email)
													) p
									) V

			declare @current int,
					@max int

			set @current = 1
			select @max = Max(ID) from @tokens

			set @formattedValue = @DisplayFormat

			while(@current <= @max)
			begin
				declare @currentToken nvarchar(100) = null,
						@currentField nvarchar(100) = null,
						@currentValue nvarchar(max) = null,
						@lkpType nvarchar(250) = null, 
						@lkpID int = null, 
						@lkpFormat nvarchar(250) = null

				select	@currentField = Field, 
						@currentToken = Token 
				from	@tokens
				where	ID = @current

				select	@currentValue = Value,
						@lkpType = LookupObjectType,
						@lkpID = LookupObjectID,
						@lkpFormat = LookupDisplayFormat
				from	@fieldValues 
				where	Field = @currentField

				if @currentValue is not null
				begin
					if @lookupObjectType is not null and @lkpID is not null
					begin
						select @currentValue = utility.GetFormattedFieldLookupValue(@Type, @lkpFormat, @lkpType, @lkpID, @currentValue)
					end

					SET @formattedValue = REPLACE(@formattedValue, @currentToken, @currentValue)
				end

				SET @current = @current + 1
			end
		end
	end

	return @formattedValue
END
GO

ALTER FUNCTION [utility].[GetObjectLevel]
(
	@Type varchar(50),
	@ID int
)
RETURNS int
AS
BEGIN
	DECLARE @level int

	IF (@Type = 'Artifact')
	BEGIN
		WITH H (ParentID, ID, [level])
		AS
		(
			SELECT	ParentID, 
					ID, 
					1
			FROM	Artifact
			WHERE	ID = @ID		
			UNION ALL
			SELECT	P.ParentID, 
					P.ID, 
					C.[level] + 1
			FROM	Artifact	P
					INNER JOIN H AS C ON C.ParentID = P.ID	
		)
		SELECT @level =	MAX([level]) FROM H
	END

	IF (@Type = 'FusionAttribute')
	BEGIN
		WITH H (ParentID, ID, [level])
		AS
		(
			SELECT	ParentID, 
					ID, 
					1
			FROM	FusionAttribute
			WHERE	ID = @ID		
			UNION ALL
			SELECT	P.ParentID, 
					P.ID, 
					C.[level] + 1
			FROM	FusionAttribute	P
					INNER JOIN H AS C ON C.ParentID = P.ID	
		)
		SELECT @level =	MAX([level]) FROM H
	END

	IF (@Type = 'FusionAttributeType')
	BEGIN
		WITH H (ParentID, ID, [level])
		AS
		(
			SELECT	ParentID, 
					ID, 
					1
			FROM	FusionAttributeType
			WHERE	ID = @ID		
			UNION ALL
			SELECT	P.ParentID, 
					P.ID, 
					C.[level] + 1
			FROM	FusionAttributeType	P
					INNER JOIN H AS C ON C.ParentID = P.ID	
		)
		SELECT @level =	MAX([level]) FROM H
	END

	IF (@Type = 'Policy')
	BEGIN
		WITH H (ParentID, ID, [level])
		AS
		(
			SELECT	ParentID, 
					ID, 
					1
			FROM	Policy
			WHERE	ID = @ID		
			UNION ALL
			SELECT	P.ParentID, 
					P.ID, 
					C.[level] + 1
			FROM	Policy	P
					INNER JOIN H AS C ON C.ParentID = P.ID
		)
		SELECT @level =	MAX([level]) FROM H
	END

	IF (@Type = 'Taxonomy')
	BEGIN
		WITH H (ParentID, ID, [level])
		AS
		(
			SELECT	ParentID, 
					ID, 
					1
			FROM	Taxonomy
			WHERE	ID = @ID		
			UNION ALL
			SELECT	P.ParentID, 
					P.ID, 
					C.[level] + 1
			FROM	Taxonomy	P
					INNER JOIN H AS C ON C.ParentID = P.ID
		)
		SELECT @level =	MAX([level]) FROM H
	END

	RETURN @level
END
GO


ALTER FUNCTION [utility].[GetResponsibilityContextHash]
(
	@ID int
)
RETURNS varchar(50)
AS
BEGIN
	DECLARE @hash varchar(50)


	DECLARE @HashThis nvarchar(1000);
	SELECT @HashThis = coalesce(STUFF((SELECT ';' + cast(DI.ID as nvarchar(10))
			  FROM ResponsibilityContextItem RCI
					inner join ReferenceItem DI on DI.ID = RCI.ObjectID and RCI.ObjectType = 'ReferenceItem' and RCI.ResponsibilityID = @ID
			  ORDER BY DI.ID
			  FOR XML PATH('')), 1, 1, ''), '')

	SELECT @hash = CONVERT(Char,HashBytes('SHA1', @HashThis),2) --SUBSTRING(master.dbo.fn_varbintohexstr(HashBytes('SHA1', @HashThis)), 3, 32)

	RETURN @hash
END
GO

CREATE TABLE [dbo].[RuleResultQualifierType] (
    [ID]                      INT            IDENTITY (1, 1) NOT NULL,
    [RuleID]                  INT            NOT NULL,
    [Name]                    NVARCHAR (250) NOT NULL,
    [Order]                   INT            NOT NULL,
    [ResolutionObject]        VARCHAR (50)   NULL,
    [ResolutionObjectID]      INT            NULL,
    [ResolutionFieldTypeID]   INT            NULL,
    [ResolutionFieldTypeName] NVARCHAR (250) NULL,
    CONSTRAINT [PK_RuleResultQualifierType] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_RuleResultQualifierType_Rule] FOREIGN KEY ([RuleID]) REFERENCES [dbo].[Rule] ([ID])
);
GO

CREATE TABLE [dbo].[RuleResultQualifier] (
    [RuleResultID]              INT             NOT NULL,
    [RuleResultQualifierTypeID] INT             NOT NULL,
    [Value]                     NVARCHAR (1000) NULL,
    [ResolvedObject]            VARCHAR (50)    NULL,
    [ResolvedObjectID]          INT             NULL,
    CONSTRAINT [PK_RuleResultQualifier] PRIMARY KEY NONCLUSTERED ([RuleResultID] ASC, [RuleResultQualifierTypeID] ASC),
    CONSTRAINT [FK_RuleResultQualifier_RuleResult] FOREIGN KEY ([RuleResultID]) REFERENCES [dbo].[RuleResult] ([ID]),
    CONSTRAINT [FK_RuleResultQualifier_RuleResultQualifierType] FOREIGN KEY ([RuleResultQualifierTypeID]) REFERENCES [dbo].[RuleResultQualifierType] ([ID])
);
GO

alter table cache.ResponsibilityItem alter column [ResponsibilityType] NVARCHAR(250) NULL
go

alter table [Field] alter column [ObjectType] VARCHAR (50) NOT NULL
go

alter TRIGGER [dbo].[Field_AfterUpsert]
	ON [dbo].[Field]
	FOR INSERT, UPDATE
AS
	SET NOCOUNT ON;

	
	UPDATE	T
	SET		T.FormattedValue = utility.GetFormattedFieldLookupValue(FT.Type, FT.LookupDisplayFormat, FT.LookupObjectType, FT.LookupObjectID, F.Value)
	FROM	Field T 
			inner join inserted F on F.FieldTypeID = T.FieldTypeID and F.ObjectType = T.ObjectType and F.ObjectID = T.ObjectID
			INNER JOIN FieldType FT ON FT.ID = T.FieldTypeID


	UPDATE	TF
	SET		TF.FormattedValue = utility.GetFormattedFieldLookupValue(FT.Type, FT.LookupDisplayFormat, FT.LookupObjectType, FT.LookupObjectID, TF.Value)
	from	Field TF
			inner join FieldType FT on FT.ID = TF.FieldTypeID
			inner join	inserted SF on FT.LookupObjectType = SF.ObjectType and TF.Value = cast(SF.ObjectID as varchar(50))
GO

DROP INDEX [IX_FusionAttribute_FusionAttributeTypeID] ON [dbo].[FusionAttribute]
GO

CREATE NONCLUSTERED INDEX [IX_FusionAttribute_FusionAttributeTypeID]
    ON [dbo].[FusionAttribute]([FusionAttributeTypeID] ASC)
    INCLUDE([ID], [Name]);
GO


ALTER PROCEDURE [dbo].[GetRenderedTemplateBodyNg]-- 'Tooltip', 'Resource', 2, 'Preview'
--declare
	@TemplateType varchar(25),
	@Type varchar(50),
	@ID int,
	@Action varchar(50)
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

	if @TemplateType = 'Email'
	begin
		select	@html = TemplateBody
		from	EmailTemplate
		where	Name = @Type
				and [Action] = @Action
	end

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
	select	@typeID = ObjectTypeID,
			@icon = '<div title=''' + ObjectTypeName + ''' class=''tooltip-icon'' style=''background-color: ' + IconBackColor + '; color: ' + IconForeColor + '''><i class=''fa fa-' + IconText + '''></i></div>',
			@n = Name,
			@t = ObjectTypeName,
			@d = Description,
			@link = NgUrl
	from	cache.ObjectDetails
	where	[Object] = @Type
			and ObjectID = @ID;

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

	if @Action = 'Certificate'
	begin
		set @html = '<h3>{Name}</h3>'

		declare @workflowID uniqueidentifier,
				@dateCertifiedOn varchar(10),
				@certifiers nvarchar(2500),
				@status varchar(50),
				@certIconColor varchar(10)

		select	@dateCertifiedOn = CONVERT(VARCHAR(10), DateLastCertified, 101),
				@status = Status
		from	Artifact A
		where	A.ID = @ID

		SELECT	@workflowID = W.ID,
				@certifiers = COALESCE(@certifiers + ', ', '') + R.FirstName + ' ' + R.LastName 
		from	(
				select		top 1
							ID,
							Data.value('(/fields/ArtifactID)[1]', 'int') as ArtifactID,
							DateCompleted
				from		Workflow
				where		WorkflowType = 2
							and Data.exist('/fields/ArtifactID[text() = sql:variable("@ID")]') = 1
				order by	DateCompleted desc
				) W
				inner join WorkflowResource WR on WR.WorkflowID = W.ID
				inner join reporting.Global_Resource R on R.ResourceID = WR.ResourceID

		if @dateCertifiedOn is null and @status != 'Certified'
			begin
				set @showIcon = 0

				set @html = @html + '<div><b>Not yet certified</b></div>'
				if @certifiers is not null
				begin
					set @html = @html + '<div>Certifying Users: {Certifiers}</div>'
				end
				if @workflowID is not null
				begin
					set @html = @html + '<div><a class=''btn btn-info'' routerLink=''/workflow/status/' + cast(@workflowID as varchar(50)) + '''>Go to this workflow status</a>.</div>'
				end
			end
		else
			begin
				if @status = 'Certified'
					begin
						set @certIconColor = '#EFC43D'
					end
				else 
					begin
						set @certIconColor = '#FFE183'
					end
				select	@icon = '<div style="background-color: transparent; color: ' + @certIconColor + '"><i class="fa fa-2x fa-certificate"></i></div>'
				set @html = @html + '<div>Last Certified On: ';
				if @dateCertifiedOn is null
					begin
						set @html = @html + 'Manually Certified';
					end
				else
					begin
						set @html = @html + '{CertifiedOn}';
					end
				set @html = @html + '</div>';
				if @status = 'Certified'
					begin
						if @Certifiers is not null
						begin
							set @html = @html + '<div>Certified By: {Certifiers}</div>'
						end
					end
				else 
					begin
						set @html = @html + '<div>Currently Under Certification Review</div>'
						set @html = @html + '<div>Certifying Users: {Certifiers}</div>'
						if @workflowID is not null
						begin
							set @html = @html + '<div><a class=''btn btn-info'' routerLink=''/workflow/status/' + cast(@workflowID as varchar(50)) + '''>Go to this workflow status</a>.</div>'
						end
					end
			end

		insert into @tbl values ('CertifiedOn', @dateCertifiedOn)
		insert into @tbl values ('Certifiers', @certifiers)
	end
	if @Action = 'JoinRequest'
	begin
		set @html = ''
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
		set @html = '<h3>{Name} <small style="right: 5px;">{Type}</small></h3><div>{Description}</div>'
		set @showIcon = 0

		if @Type = 'Artifact'
		begin
			insert into @tbl
			select	'Status', [Status]
			from	Artifact
			where	ID = @ID

			insert into @tbl
			select	'Path', TextPath
			from	Artifact
			where	ID = @ID

			set @html = @html + '<div><b>Status:</b> {Status}</div>'
			set @html = @html + '<div><b>Path:</b> {Path}</div>'

			set @hasDynamicFields = 1
		end;

		if @Type = 'Event'
		begin
			declare @so nvarchar(250)
			select	@so = SourceID, 
					@s = [Status]
			from	[Event]
			where	ID = @ID

			insert into @tbl values ('Status', @s)
			insert into @tbl values ('SourceID', @so)

			set @html = @html + '<div><b>Status:</b> {Status}</div>'
			set @html = @html + '<div><b>SourceID:</b> {SourceID}</div>'

			set @hasDynamicFields = 1
		end;

		if @Type = 'EventGroup'
		begin
			insert into @tbl
				select	'Key', PublicID
				from	EventGroup
				where	ID = @ID

			-- BUILD EVENT LIST HTML -----------------------------------------
			declare @events table (ID int, SourceID nvarchar(250), Status varchar(25))
			insert into @events 
				select top 10 ID, SourceID, Status from [Event] where EventGroupID = @ID order by ID desc
		
			declare @eventFieldTypes table (ID int identity, Name nvarchar(250))
			insert into @eventFieldTypes
				select FriendlyName from FieldType where [Object] = 'Rule' and ObjectID = @typeID order by SortOrder asc
			insert into @eventFieldTypes values ('Source ID')
			insert into @eventFieldTypes values ('Status')

			declare @eventHtml nvarchar(max)

			set @eventHtml = '<table class="hoverable bordered striped" style="width:100%">'

			-- Loop through field name list ---------
			set @eventHtml = @eventHtml + '<thead>'
			set		@current = 1
			select	@max = max(ID) from @eventFieldTypes
			while @current <= @max
			begin
				select	@name = Name
				from	@eventFieldTypes
				where	ID = @current

				set @eventHtml = @eventHtml + '<th>' + @name  + '</th>'

				set @current = @current + 1
			end
			set @eventHtml = @eventHtml + '</thead>'
			-----------------------------------------

			set @eventHtml = @eventHtml + '<tbody>'

			-- Loop through event list --------------
			select	@current = min(ID) from @events
			select	@max = max(ID) from @events

			while @current <= @max
			begin
				set @eventHtml = @eventHtml + '<tr>'	-- Open row for selected event.

				declare @eventFields table (Name nvarchar(250), Value nvarchar(4000))
			
				insert into @eventFields
					select		FriendlyName,
								FormattedValue
					from		FieldWithRelation
					where		ObjectType = 'Event' 
								and ObjectID = @current

					-- Loop through each field for this selected event --
					declare @fCurrent int,
							@fMax int
					set		@fCurrent = 1
					select	@fMax = max(ID) from @eventFieldTypes
					while @fCurrent <= @fMax
					begin
						select	@name = Name from @eventFieldTypes where ID = @fCurrent

						select @eventHtml = @eventHtml + '<td>' + coalesce(Value, '') + '</td>' from @eventFields where Name = @name

						set @fCurrent = @fCurrent + 1
					end
					-----------------------------------------------------

					select @eventHtml = @eventHtml	+ 
										'<td>' + [SourceID] + '</td>' + 
										'<td>' + [Status] + '</td>' 
					from	@events 
					where	ID = @current

				delete @eventFields

				set @eventHtml = @eventHtml + '</tr>'	-- Close off row for selected event.

				set @current = @current + 1
			end
			-----------------------------------------

			set @eventHtml = @eventHtml + '</tbody>'

			set @eventHtml = @eventHtml + '</table>'

			insert into @tbl values ('Items', @eventHtml)

			set @html = @html + '<div><b>Key:</b> {Key}</div>'
			set @html = @html + '<div>Items: {Items}</div>'
			------------------------------------------------------------------
		end;

		if @Type = 'Intersect'
		begin
			insert into @tbl
				select	'Classification',
						case Classification
							when 1 then 'Critical'
							else 'Normal'
						end
				from	[Intersect]
				where	ID = @ID

			set @html = @html + '<div><b>Classification:</b> {Classification}</div>'
		end;

		if @Type = 'Responsibility'
		begin
			select	@n = T.Name, 
					@t = T.Name,
					@d = T.[Description]
			from	Responsibility O
					inner join ResponsibilityType T on T.ID = O.ResponsibilityTypeID
			where	O.ID = @ID
			
			declare @contextsHtml nvarchar(max)

			set @contextsHtml = '<table class="hoverable bordered striped" style="width:100%">' + 
								'<thead><th>List</th><th>Code</th></thead>' + 
								'<tbody>' + 
								(
								select		(select D.Name as 'td' for xml path(''), type),
											(select I.Code as 'td' for xml path(''), type)
								from		ResponsibilityContextItem R
											inner join ReferenceItem I on R.ResponsibilityID = @ID and R.ObjectType = 'ReferenceItem' and I.ID = R.ObjectID
											inner join ReferenceItemType D on D.ID = I.ReferenceItemTypeID
								FOR XML RAW('tr'), ELEMENTS
								) +
								'</tbody>' + 
								'</table>'

			insert into @tbl values ('Name', @n)
			insert into @tbl values ('Type', @t)
			insert into @tbl values ('Description', @d)
			insert into @tbl values ('Contexts', @contextsHtml)

			set @html = @html + '<div><b>Contexts:</b> {Contexts}</div>'
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
				select	'Name', Name
				from	[Rule] O
				where	ID = @ID
			insert into @tbl
				select	'Description', Description
				from	[Rule] O
				where	ID = @ID
			--insert into @tbl
			--	select	'Status', Status
			--	from	[Rule] O
			--	where	ID = @ID

			--set @html = @html + '<div><b>Status:</b> {Status}</div>'

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
			insert into @tbl
				select	'TextPath', TextPath
				from	Taxonomy O
				where	ID = @ID

			set @html = @html + '<div><b>Path:</b> {TextPath}</div>'

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

			declare @stats table (ID int identity, Name nvarchar(250), Score int)
			insert into @stats 
				select	T.Name,
						coalesce(S.SCore, 0) as Score
				from	StatisticType T
						outer apply (
									select	top 1
											*
									from	Statistic
									where	StatisticTypeID = T.ID
											and ObjectType = @Type
											and ObjectID = @ID
									order by DateStart desc
									) S
				where	T.[Object] = @Type + 'Type' 
						and T.ObjectID = @typeID
						and T.PartOfScore = 1

			set @statsHtml = '<table class="hoverable bordered striped" style="width:100%">'

			-- Loop through field name list ---------
			set @statsHtml = @statsHtml + '<tbody>'
			set		@current = 1
			select	@max = max(ID) from @stats
			while @current <= @max
			begin
				select	@statsHtml = @statsHtml + '<tr><td>' + Name  + '</td>' + '<td>' + cast(Score as varchar(5))  + ' Points</td></tr>'
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

	-- Return the properly formatted values.
	select	'' as Title,
			@html as Body;
END
GO

ALTER FUNCTION [dbo].[GenerateNgObjectUrl] 
(
	@Type varchar(50),
	@TypeID int,
	@ObjectID int = 0
)
RETURNS varchar(500)
AS
BEGIN
	DECLARE @Prefix varchar(5) = ''--'a/'
	DECLARE @Url varchar(500)
	SET @Url = @Prefix

	SET @Url = CASE @Type
		WHEN 'Artifact' THEN 'artifact/' +  + CAST(@TypeID as varchar) + '/' + CAST(@ObjectID as varchar)
		WHEN 'ArtifactType' THEN 'artifact/' + CAST(@TypeID as varchar)
		WHEN 'Domain' THEN 'domain/' +  + CAST(@TypeID as varchar) + '/' +  + CAST(@ObjectID as varchar)
		WHEN 'DomainType' THEN 'domain/' + CAST(@TypeID as varchar)
		WHEN 'ReferenceItem' THEN 'reference/' +  + CAST(@TypeID as varchar)-- + '/' +  + CAST(@ObjectID as varchar)
		WHEN 'ReferenceItemType' THEN 'reference/' + CAST(@TypeID as varchar)
		WHEN 'FusionAttribute' THEN 'fusion/fusionattribute/' + CAST(@TypeID as varchar) + '/' + CAST(@ObjectID as varchar)		
		WHEN 'Fusion' THEN 'fusion/' + CAST(@TypeID as varchar) + '/' + + CAST(@ObjectID as varchar)
		WHEN 'FusionType' THEN 'fusion/' + CAST(@TypeID as varchar)
		WHEN 'Group' THEN 'groups/' + CAST(@ObjectID as varchar)	
		WHEN 'Lookup' THEN 'admin/lookups/' + CAST(@TypeID as varchar) + '/' + + CAST(@ObjectID as varchar)
		WHEN 'LookupType' THEN 'admin/lookups/' + CAST(@TypeID as varchar)
		WHEN 'Policy' THEN 'policy/' + CAST(@TypeID as varchar(15)) + '/id/' + CAST(@ObjectID as varchar)
		WHEN 'PolicyType' THEN 'policy/' + CAST(@TypeID as varchar) + '/structure'		
		WHEN 'Resource' THEN 'resource/' + CAST(@ObjectID as varchar)
		WHEN 'ResourceType' THEN 'resource/list/' + CAST(@TypeID as varchar)
		WHEN 'Rule' THEN 'quality/rule/' + CAST(@ObjectID as varchar)
		WHEN 'Taxonomy' THEN 'model/' + CAST(@TypeID as varchar) + '/id/' + CAST(@ObjectID as varchar)
		WHEN 'TaxonomyType' THEN 'model/' + CAST(@ObjectID as varchar) + '/structure'		
	END

	SET @Url = @Prefix + @Url

	RETURN @Url
END
GO


ALTER FUNCTION [utility].[GetFormattedFieldLookupValue]
(
	@Type varchar(25),
	@DisplayFormat nvarchar(250),
	@LookupObjectType varchar(25),
	@LookupObjectID int,
	@Value nvarchar(max)
)
RETURNS nvarchar(max)
AS
BEGIN
	declare @formattedValue nvarchar(max)
	
	if @LookupObjectType is null
	begin
		set @formattedValue  = @Value

		if @Type = 'Link' OR @Type = 'UncLink'
		begin
			declare @linkName nvarchar(max),
					@linkUrl nvarchar(max)

			if charindex('|', @Value, 1) > 1
				begin
					SELECT @linkName = SUBSTRING(@Value, 1, PATINDEX('%|%', @Value)-1)
					SELECT @linkUrl = SUBSTRING(@Value, PATINDEX('%|%', @Value)+1, LEN(@Value))

					set @formattedValue = '<a href="' + @linkUrl + '" target="_blank">' + @linkName + '</a>'
				end
			else
				begin
					if @Value <> '' AND @Value <> '|' AND @Value IS NOT NULL
						begin
							if LEFT(@Value, 1) = '|'
								begin
									--no name, default to url
									set @formattedValue = '<a href="' + SUBSTRING(@Value,2, LEN(@Value)) + '" target="_blank">' + SUBSTRING(@Value,2, LEN(@Value)) + '</a>'
								end
							else
								begin
									set @formattedValue = '<a href="' + @Value + '" target="_blank">' + @Value + '</a>'
								end
						end
					else
						begin
							set @formattedValue = null
						end
				end
		end

	end	
	else
	begin
		if @LookupObjectType = 'ReferenceItemType'
		begin
			select @formattedValue = Name from ReferenceItemType where id = @Value;		
		end
		else
		begin
			declare @tokens table(ID int identity(1,1), Token nvarchar(100), Field nvarchar(100))
			declare @fieldValues table(Field nvarchar(100), Value nvarchar(max), LookupObjectType nvarchar(250), LookupObjectID int, LookupDisplayFormat nvarchar(250))

			set @formattedValue = @DisplayFormat
	
			while patindex('%{%',@formattedValue) > 0
			 begin
				declare @txt nvarchar(100) = SUBSTRING(@formattedValue, patindex('%{%',@formattedValue), PATINDEX('%}%', @formattedValue))
				insert into @tokens Values (@txt, REPLACE(REPLACE(@txt,'{',''),'}',''))
				set @formattedValue = replace(@formattedValue, @txt, '')
			end

			insert into @fieldValues
				select	distinct
						V.Name,
						V.Value,
						V.LookupObjectType,
						V.LookupObjectID,
						V.LookupDisplayFormat
				from	(
						SELECT	ID,
								Name,
								'Artifact' as ObjectType
						FROM	ArtifactType
						WHERE	@LookupObjectType = 'Artifact' and ID = @LookupObjectID
						UNION
						SELECT	ID,
								Name,
								'Lookup' as ObjectType
						FROM	[LookupType]
						WHERE	@LookupObjectType = 'Lookup' and ID = @LookupObjectID
						UNION
						SELECT	ID,
								Name,
								'ReferenceItem' as ObjectType
						FROM	[ReferenceItemType]
						WHERE	@LookupObjectType = 'ReferenceItem' and ID = @LookupObjectID
						UNION										
						SELECT	1 as ID,
								'User' as Name,
								'Resource' as ObjectType
						WHERE	@LookupObjectType = 'Resource'-- and ID = @LookupObjectID
						UNION
						SELECT	ID,
								Name,
								'Taxonomy' as ObjectType
						FROM	TaxonomyType
						WHERE	@LookupObjectType = 'Taxonomy' and ID = @LookupObjectID
						) L
						outer apply (

									SELECT	IT.Name,
											[IF].Value,
											[IT].LookupObjectType,
											COALESCE([IT].LookupObjectID, 0) as LookupObjectID,
											[IT].LookupDisplayFormat
									FROM	Field [IF]
											inner join FieldType IT ON [IF].FieldTypeID = IT.ID 
																	and [IF].ObjectType = L.ObjectType
																	and [IF].ObjectID = case 
																							when dbo.IsInteger(@Value) = 1 then @Value
																							else 0
																						end
								
									UNION

									SELECT	P.FieldName as Name,
											p.FieldValue as Value,
											NULL as LookupObjectType,
											NULL as LookupObjectID,
											NULL as LookupDisplayFormat
									FROM	(
											SELECT	ID,
													CAST(Name as nvarchar(max)) as Name,
													CAST(Description as nvarchar(max)) as Description,
													CAST(TextPath as nvarchar(max)) as TextPath
											FROM	Artifact A
											WHERE	A.ID = CAST(@Value as int)
													and L.ObjectType = 'Artifact'
											) A
											unpivot	(
													FieldValue for FieldName in (Name, Description, TextPath)
													) p

									UNION

									SELECT	P.FieldName as Name,
											p.FieldValue as Value,
											NULL as LookupObjectType,
											NULL as LookupObjectID,
											NULL as LookupDisplayFormat
									FROM	(
											SELECT	ID,
													CAST(Name as nvarchar(max)) as Name,
													CAST(Description as nvarchar(max)) as Description,
													CAST(TextPath as nvarchar(max)) as TextPath
											FROM	Taxonomy A
											WHERE	A.ID = CAST(@Value as int)
													and L.ObjectType = 'Taxonomy'
											) A
											unpivot	(
													FieldValue for FieldName in (Name, Description, TextPath)
													) p

									UNION

									SELECT	P.FieldName as Name,
											p.FieldValue as Value,
											NULL as LookupObjectType,
											NULL as LookupObjectID,
											NULL as LookupDisplayFormat
									FROM	(
											SELECT	ID,
													CAST(Code as nvarchar(max)) as Code
											FROM	ReferenceItem A
											WHERE	A.ID = @Value
													and L.ObjectType = 'ReferenceItem'
											) A
											unpivot	(
													FieldValue for FieldName in (Code)
													) p

									UNION

									SELECT	P.FieldName as Name,
											p.FieldValue as Value,
											NULL as LookupObjectType,
											NULL as LookupObjectID,
											NULL as LookupDisplayFormat
									FROM	(
											SELECT	ID,
													CAST(Name as nvarchar(max)) as Name,
													CAST(Description as nvarchar(max)) as Description
											FROM	ReferenceItemType A
											WHERE	A.ID = @Value
													and L.ObjectType = 'ReferenceItemType'
											) A
											unpivot	(
													FieldValue for FieldName in (Name, Description)
													) p

									UNION

									SELECT	P.FieldName as Name,
											p.FieldValue as Value,
											NULL as LookupObjectType,
											NULL as LookupObjectID,
											NULL as LookupDisplayFormat
									FROM	(
											SELECT	ResourceID as ID,
													CAST(FirstName as nvarchar(max)) as FirstName,
													CAST(LastName as nvarchar(max)) as LastName,
													CAST(Email as nvarchar(max)) as Email
											FROM	reporting.Global_Resource A
											WHERE	A.ResourceID = @Value
													and L.ObjectType = 'Resource'
											) A
											unpivot	(
													FieldValue for FieldName in (FirstName, LastName, Email)
													) p
									) V

			declare @current int,
					@max int

			set @current = 1
			select @max = Max(ID) from @tokens

			set @formattedValue = @DisplayFormat

			while(@current <= @max)
			begin
				declare @currentToken nvarchar(100) = null,
						@currentField nvarchar(100) = null,
						@currentValue nvarchar(max) = null,
						@lkpType nvarchar(250) = null, 
						@lkpID int = null, 
						@lkpFormat nvarchar(250) = null

				select	@currentField = Field, 
						@currentToken = Token 
				from	@tokens
				where	ID = @current

				select	@currentValue = Value,
						@lkpType = LookupObjectType,
						@lkpID = LookupObjectID,
						@lkpFormat = LookupDisplayFormat
				from	@fieldValues 
				where	Field = @currentField

				if @currentValue is not null
				begin
					if @lookupObjectType is not null and @lkpID is not null
					begin
						select @currentValue = utility.GetFormattedFieldLookupValue(@Type, @lkpFormat, @lkpType, @lkpID, @currentValue)
					end

					SET @formattedValue = REPLACE(@formattedValue, @currentToken, @currentValue)
				end

				SET @current = @current + 1
			end
		end
	end

	return @formattedValue
END
GO

BEGIN TRANSACTION

IF NOT EXISTS (SELECT 1 FROM [INFORMATION_SCHEMA].[TABLES] WHERE [TABLE_NAME] = N'WorkflowProcessScheme')
BEGIN
	CREATE TABLE [WorkflowProcessScheme](
		[Id] [uniqueidentifier] NOT NULL,
		[Scheme] [ntext] NOT NULL,
		[DefiningParameters] [ntext] NOT NULL,
		[DefiningParametersHash] [nvarchar](1024) NOT NULL,
		[SchemeCode] [nvarchar](max) NOT NULL,
		[IsObsolete] [bit] NOT NULL DEFAULT (0),
		[RootSchemeCode] nvarchar (max) NULL,
		[RootSchemeId]  uniqueidentifier NULL,
		[AllowedActivities] nvarchar (max) NULL,
		[StartingTransition] nvarchar (max) NULL,
		CONSTRAINT [PK_WorkflowProcessScheme] PRIMARY KEY CLUSTERED([Id] ASC)
	 )

	PRINT 'WorkflowProcessScheme CREATE TABLE'
END

IF NOT EXISTS (SELECT 1 FROM [INFORMATION_SCHEMA].[TABLES] WHERE [TABLE_NAME] = N'WorkflowProcessInstance')
BEGIN
	CREATE TABLE [WorkflowProcessInstance](
		[Id] [uniqueidentifier] NOT NULL,
		[StateName] [nvarchar](max) NOT NULL,
		[ActivityName] [nvarchar](max) NOT NULL,
		[SchemeId] [uniqueidentifier] NULL,
		[PreviousState] [nvarchar](max) NULL,
		[PreviousStateForDirect] [nvarchar](max) NULL,
		[PreviousStateForReverse] [nvarchar](max) NULL,
		[PreviousActivity] [nvarchar](max) NULL,
		[PreviousActivityForDirect] [nvarchar](max) NULL,
		[PreviousActivityForReverse] [nvarchar](max) NULL,
		[ParentProcessId] uniqueidentifier NULL,
		[RootProcessId] uniqueidentifier NOT NULL,
		[IsDeterminingParametersChanged] [bit] NOT NULL DEFAULT ((0)),
		CONSTRAINT [PK_WorkflowProcessInstance_1] PRIMARY KEY CLUSTERED ([Id] ASC)
	)

	PRINT 'WorkflowProcessInstance CREATE TABLE'
END

IF NOT EXISTS (SELECT 1 FROM [INFORMATION_SCHEMA].[TABLES] WHERE [TABLE_NAME] = N'WorkflowProcessInstancePersistence')
BEGIN
	CREATE TABLE [WorkflowProcessInstancePersistence](
		[Id] [uniqueidentifier] NOT NULL,
		[ProcessId] [uniqueidentifier] NOT NULL,
		[ParameterName] [nvarchar](max) NOT NULL,
		[Value] [ntext] NOT NULL,
		CONSTRAINT [PK_WorkflowProcessInstancePersistence] PRIMARY KEY CLUSTERED ([Id] ASC)
	 )

	PRINT 'WorkflowProcessInstancePersistence CREATE TABLE'
END

IF NOT EXISTS (SELECT 1 FROM [INFORMATION_SCHEMA].[TABLES] WHERE [TABLE_NAME] = N'WorkflowProcessTransitionHistory')
BEGIN
	CREATE TABLE [WorkflowProcessTransitionHistory](
		[Id] [uniqueidentifier] NOT NULL,
		[ProcessId] [uniqueidentifier] NOT NULL,
		[ExecutorIdentityId] [nvarchar](max) NOT NULL,
		[ActorIdentityId] [nvarchar](max) NOT NULL,
		[FromActivityName] [nvarchar](max) NOT NULL,
		[ToActivityName] [nvarchar](max) NOT NULL,
		[ToStateName] [nvarchar](max) NULL,
		[TransitionTime] [datetime] NOT NULL,
		[TransitionClassifier] [nvarchar](max) NOT NULL,
		[IsFinalised] [bit] NOT NULL,
		[FromStateName] [nvarchar](max) NULL,
		[TriggerName] [nvarchar](max) NULL,
		CONSTRAINT [PK_WorkflowProcessTransitionHistory] PRIMARY KEY CLUSTERED ([Id] ASC)
	 )

	PRINT 'WorkflowProcessTransitionHistory CREATE TABLE'
END

IF NOT EXISTS (SELECT 1 FROM [INFORMATION_SCHEMA].[TABLES] WHERE [TABLE_NAME] = N'WorkflowProcessInstanceStatus')
BEGIN
	CREATE TABLE [WorkflowProcessInstanceStatus](
		[Id] [uniqueidentifier] NOT NULL,
		[Status] [tinyint] NOT NULL,
		[Lock] [uniqueidentifier] NOT NULL,
		CONSTRAINT [PK_WorkflowProcessInstanceStatus] PRIMARY KEY CLUSTERED ([Id] ASC)
	 )

	PRINT 'WorkflowProcessInstanceStatus CREATE TABLE'
END

IF NOT EXISTS (SELECT 1 FROM sys.procedures WHERE name = N'spWorkflowProcessResetRunningStatus')
BEGIN
	EXECUTE('CREATE PROCEDURE [spWorkflowProcessResetRunningStatus]
	AS
	BEGIN
		UPDATE [WorkflowProcessInstanceStatus] SET [WorkflowProcessInstanceStatus].[Status] = 2 WHERE [WorkflowProcessInstanceStatus].[Status] = 1
	END')

	PRINT 'spWorkflowProcessResetRunningStatus CREATE PROCEDURE'
END


IF NOT EXISTS (SELECT 1 FROM [INFORMATION_SCHEMA].[TABLES] WHERE [TABLE_NAME] = N'WorkflowRuntime')
BEGIN
	CREATE TABLE [WorkflowRuntime](
		[RuntimeId] [uniqueidentifier] NOT NULL,
		[Timer] [nvarchar](max) NOT NULL,
		CONSTRAINT [PK_WorkflowRuntime] PRIMARY KEY CLUSTERED([RuntimeId] ASC)
	)
	PRINT 'WorkflowRuntime CREATE TABLE'
END

IF NOT EXISTS (SELECT 1 FROM [INFORMATION_SCHEMA].[TABLES] WHERE [TABLE_NAME] = N'WorkflowScheme')
BEGIN
	-- Simple schemestorage
	CREATE TABLE [WorkflowScheme](
	 [Code] [nvarchar](256) NOT NULL,
	 [Scheme] [nvarchar](max) NOT NULL,
	 CONSTRAINT [PK_WorkflowScheme] PRIMARY KEY CLUSTERED([Code] ASC)
	)
	PRINT 'WorkflowScheme CREATE TABLE'
END

IF NOT EXISTS (SELECT 1 FROM sys.procedures WHERE name = N'DropWorkflowProcess')
BEGIN
	EXECUTE('CREATE PROCEDURE [DropWorkflowProcess] 
		@id uniqueidentifier
	AS
	BEGIN
		BEGIN TRAN
	
		DELETE FROM dbo.WorkflowProcessInstance WHERE Id = @id
		DELETE FROM dbo.WorkflowProcessInstanceStatus WHERE Id = @id
		DELETE FROM dbo.WorkflowProcessInstancePersistence  WHERE ProcessId = @id
	
		COMMIT TRAN
	END')
	PRINT 'DropWorkflowProcess CREATE PROCEDURE'
END

IF NOT EXISTS (SELECT 1 FROM sys.procedures WHERE name = N'DropWorkflowProcesses')
BEGIN
	EXECUTE('CREATE TYPE IdsTableType AS TABLE 
	( Id uniqueidentifier );')

	PRINT 'IdsTableType CREATE TYPE'

	EXECUTE('CREATE PROCEDURE [DropWorkflowProcesses] 
		@Ids  IdsTableType	READONLY
	AS	
	BEGIN
		BEGIN TRAN
	
		DELETE dbo.WorkflowProcessInstance FROM dbo.WorkflowProcessInstance wpi  INNER JOIN @Ids  ids ON wpi.Id = ids.Id 
		DELETE dbo.WorkflowProcessInstanceStatus FROM dbo.WorkflowProcessInstanceStatus wpi  INNER JOIN @Ids  ids ON wpi.Id = ids.Id 
		DELETE dbo.WorkflowProcessInstanceStatus FROM dbo.WorkflowProcessInstancePersistence wpi  INNER JOIN @Ids  ids ON wpi.ProcessId = ids.Id 
	

		COMMIT TRAN
	END')
	PRINT 'DropWorkflowProcesses CREATE PROCEDURE'
END

IF NOT EXISTS (SELECT 1 FROM [INFORMATION_SCHEMA].[TABLES] WHERE [TABLE_NAME] = N'WorkflowInbox')
BEGIN
	CREATE TABLE [WorkflowInbox](
		[Id] [uniqueidentifier] NOT NULL,
		[ProcessId] [uniqueidentifier] NOT NULL,
		[IdentityId] [uniqueidentifier] NOT NULL,
		CONSTRAINT [PK_WorkflowInbox] PRIMARY KEY CLUSTERED([Id] ASC)
	 )
	PRINT 'WorkflowInbox CREATE TABLE'
END

IF NOT EXISTS (SELECT 1 FROM sys.procedures WHERE name = N'DropWorkflowInbox')
BEGIN
	EXECUTE('CREATE PROCEDURE [DropWorkflowInbox] 
		@processId uniqueidentifier
	AS
	BEGIN
		BEGIN TRAN	
		DELETE FROM dbo.WorkflowInbox WHERE ProcessId = @processId	
		COMMIT TRAN
	END')
	PRINT 'DropWorkflowInbox CREATE PROCEDURE'
END

IF NOT EXISTS (SELECT 1 FROM [INFORMATION_SCHEMA].[TABLES] WHERE [TABLE_NAME] = N'WorkflowProcessTimer')
BEGIN
	CREATE TABLE [dbo].[WorkflowProcessTimer](
		[Id] [uniqueidentifier] NOT NULL,
		[ProcessId] [uniqueidentifier] NOT NULL,
		[Name] [nvarchar](max) NOT NULL,
		[NextExecutionDateTime] [datetime] NOT NULL,
		[Ignore] [bit] NOT NULL,
	 CONSTRAINT [PK_WorkflowProcessTimer] PRIMARY KEY CLUSTERED ([Id] ASC)
	 )

	PRINT 'WorkflowProcessTimer CREATE TABLE'
END

IF NOT EXISTS (SELECT 1 FROM [INFORMATION_SCHEMA].[TABLES] WHERE [TABLE_NAME] = N'WorkflowGlobalParameter')
BEGIN
CREATE TABLE [dbo].[WorkflowGlobalParameter](
	[Id] [uniqueidentifier] NOT NULL,
	[Type] [nvarchar](max) NOT NULL,
	[Name] [nvarchar](max) NOT NULL,
	[Value]  [nvarchar](max) NOT NULL
 CONSTRAINT [PK_WorkflowGlobalParameter] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

PRINT 'WorkflowGlobalParameter CREATE TABLE'

END

COMMIT TRANSACTION


create function [cache].[SynchronizeObjectResponsibilities]
(
--declare
	@Object varchar(50),
	@ObjectID int
--set @Object = 'ArtifactType'
--set @ObjectID = 11
)
returns	@Responsibilities table
(
	ID int identity,
	[Source] varchar(50), 
	Visible bit,
	ResponsibilityID int,
	ResponsibilityTypeID int,
	AssigningItem varchar(50),
	AssigningItemID int,
	[Object] varchar(50),
	ObjectID int,
	ContextHash varchar(50),
	[Priority] int
)
as
begin
	insert into @Responsibilities
		select * from utility.GetVerticalResponsibilityList(@Object, @ObjectID, 1);
	insert into @Responsibilities
		select * from utility.GetHierarchyAssignedResponsibilityList(@Object, @ObjectID, 4);
	insert into @Responsibilities
		select * from utility.GetDirectlyAssignedResponsibilityList(@Object, @ObjectID, 7);

	--delete cache.ResponsibilityItem where [Object] = @Object and ObjectID = @ObjectID
	--DELETE	T
	--FROM	cache.ResponsibilityItem T
	--		INNER JOIN @Responsibilities S ON S.[Object] = T.[Object] 
	--										and S.[ObjectID] = T.[ObjectID] 
	--										and S.ResponsibilityTypeID = T.ResponsibilityTypeID 
	--										and S.ContextHash = T.ContextHash;

	declare @current int = 1,
			@max int,
			@ResponsibilityID int,
			@ResponsibilityTypeID int,
			@AssigningItem varchar(50),
			@AssigningItemID int,
			@Obj varchar(50),
			@ObjID int,
			@ContextHash varchar(50),
			@Priority int;

	select @max = max(ID) from @Responsibilities;

	while @current <= @max
	begin
		if exists(select 1 from @Responsibilities where ID = @current)
		begin
			select	@ResponsibilityID = ResponsibilityID,
					@ResponsibilityTypeID = ResponsibilityTypeID,
					@AssigningItem = AssigningItem,
					@AssigningItemID = AssigningItemID,
					@Obj = [Object],
					@ObjID = ObjectID,
					@ContextHash = ContextHash,
					@Priority = [Priority]
			from	@Responsibilities
			where	ID = @current;

			delete	@Responsibilities
			where	ResponsibilityTypeID = @ResponsibilityTypeID
					and [Object] = @Obj
					and ObjectID = @ObjID
					and ContextHash = @ContextHash
					and [Priority] < @Priority
					and ResponsibilityTypeID <> 0;
		end
		set @current = @current + 1
	end;

--select * from #Responsibilities

	--insert into cache.ResponsibilityItem
	--(
	--	[ResponsibilityID], [ResponsibilityTypeID], [ResponsibilityType], 
	--	[AssigningItem], [AssigningItemID], 
	--	[Object], [ObjectID], 
	--	[ResponsibleObject], [ResponsibleObjectID], 
	--	[ContextHash], [ResponsibilityTypeGroup], Visible
	--)
	--	select	distinct
	--			TR.ResponsibilityID,
	--			TR.ResponsibilityTypeID,
	--			RT.Name as ResponsibilityType,
	--			TR.AssigningItem,
	--			TR.AssigningItemID,
	--			TR.[Object],
	--			TR.ObjectID,
	--			R.ResponsibleObjectType as ResponsibleObject,
	--			R.ResponsibleObjectID,
	--			TR.ContextHash,
	--			RT.ResponsibilityTypeGroup,
	--			TR.Visible
	--	from	@Responsibilities TR
	--			inner join Responsibility R on R.ID = TR.ResponsibilityID
	--			inner join ResponsibilityType RT on RT.ID = R.ResponsibilityTypeID;
	
	return;
end
GO


--insert referenceitemtype into cache object so it shows up correctly on relationshiptype def screen
insert into cache.[object] ([object],[objectid],[objecttype],[objecttypeid]) values('ReferenceItemType',0,'ReferenceItemType',0)
go

-----Remove unique guid from fusion results table for performance / as it is not used and is slow------------------

-- drop old primary key
ALTER TABLE [fusion].[result]
DROP CONSTRAINT PK_FusionResult;
GO

-- drop the constraint that generates the id
ALTER TABLE [fusion].[result]
DROP CONSTRAINT DF_FusionResult_ID;
GO

-- make the unique id column nullable
ALTER TABLE [fusion].[result] ALTER COLUMN [ID] uniqueidentifier NULL ;
GO


-- add index on fusion id and parent id to fusion attribute table
CREATE INDEX IX_FusionID_ParentID 
	ON FusionAttribute (FusionID, ParentID);  
GO