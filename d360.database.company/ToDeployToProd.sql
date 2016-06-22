--Pappas:  Added these on 04/27/16
ALTER TABLE [dbo].[FusionAttributePromotionRuleItem] DROP CONSTRAINT [PK_FusionAttributePromotionRuleItem]
GO
ALTER TABLE [dbo].[FusionAttributePromotionRuleItem] ADD  CONSTRAINT [PK_FusionAttributePromotionRuleItem] PRIMARY KEY NONCLUSTERED  ( [ID] ASC )
GO
CREATE CLUSTERED INDEX CIX_FusionAttributePromotionRuleItem ON dbo.FusionAttributePromotionRuleItem ( FusionAttributePromotionRuleID ASC )
GO

delete FusionAttributePromotionRuleItem where FusionAttributePromotionRuleID not in (select ID from FusionAttributePromotionRule)
go
ALTER TABLE [dbo].[FusionAttributePromotionRuleItem]  WITH CHECK ADD  CONSTRAINT [FK_FusionAttributePromotionRuleItem_FusionAttributePromotionRule] FOREIGN KEY([FusionAttributePromotionRuleID]) REFERENCES [dbo].[FusionAttributePromotionRule] ([ID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[FusionAttributePromotionRuleItem] CHECK CONSTRAINT [FK_FusionAttributePromotionRuleItem_FusionAttributePromotionRule]
GO


ALTER TABLE [dbo].[FusionAttributePromotionRuleMapping] DROP CONSTRAINT [PK_FusionAttributePromotionRuleMapping]
GO
ALTER TABLE [dbo].[FusionAttributePromotionRuleMapping] ADD  CONSTRAINT [PK_FusionAttributePromotionRuleMapping] PRIMARY KEY NONCLUSTERED  ( [ID] ASC )
GO
CREATE CLUSTERED INDEX CIX_FusionAttributePromotionRuleMapping ON dbo.FusionAttributePromotionRuleMapping ( FusionAttributePromotionRuleID ASC )
GO

delete FusionAttributePromotionRuleMapping where FusionAttributePromotionRuleID not in (select ID from FusionAttributePromotionRule)
go
ALTER TABLE [dbo].[FusionAttributePromotionRuleMapping]  WITH CHECK ADD  CONSTRAINT [FK_FusionAttributePromotionRuleMapping_FusionAttributePromotionRule] FOREIGN KEY([FusionAttributePromotionRuleID]) REFERENCES [dbo].[FusionAttributePromotionRule] ([ID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[FusionAttributePromotionRuleMapping] CHECK CONSTRAINT [FK_FusionAttributePromotionRuleMapping_FusionAttributePromotionRule]
GO

ALTER TABLE [dbo].[FusionAttributeOwnerRuleItem] DROP CONSTRAINT [PK_FusionAttributeOwnerRuleItem]
GO
ALTER TABLE [dbo].[FusionAttributeOwnerRuleItem] ADD  CONSTRAINT [PK_FusionAttributeOwnerRuleItem] PRIMARY KEY NONCLUSTERED  ( [ID] ASC )
GO
CREATE CLUSTERED INDEX CIX_FusionAttributeOwnerRuleItem ON dbo.FusionAttributeOwnerRuleItem ( [FusionAttributeOwnerRuleID] ASC )
GO

delete FusionAttributeOwnerRuleItem where FusionAttributeOwnerRuleID not in (select ID from FusionAttributeOwnerRule)
go
ALTER TABLE [dbo].[FusionAttributeOwnerRuleItem]  WITH CHECK ADD  CONSTRAINT [FK_FusionAttributeOwnerRuleItem_FusionAttributeOwnerRule] FOREIGN KEY([FusionAttributeOwnerRuleID]) REFERENCES [dbo].[FusionAttributeOwnerRule] ([ID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[FusionAttributeOwnerRuleItem] CHECK CONSTRAINT [FK_FusionAttributeOwnerRuleItem_FusionAttributeOwnerRule]
GO

ALTER TABLE [dbo].[FusionAttributeOwnerRule] DROP CONSTRAINT [FK_FusionAttributeOwnerRule_Fusion]
GO
ALTER TABLE [dbo].[FusionAttributePromotionRule] DROP CONSTRAINT [FK_FusionAttributePromotionRule_Fusion]
GO
ALTER TABLE [dbo].[FusionAttributePromotion] DROP CONSTRAINT [FK_FusionAttributePromotion_FusionAttribute]
GO

ALTER TABLE [dbo].[FusionAttribute] DROP CONSTRAINT [PK_FusionAttribute]
GO
ALTER TABLE [dbo].[FusionAttribute] ADD  CONSTRAINT [PK_FusionAttribute] PRIMARY KEY NONCLUSTERED ( [ID] ASC )
GO

ALTER TABLE [dbo].[FusionAttributeOwnerRule]  WITH CHECK ADD  CONSTRAINT [FK_FusionAttributeOwnerRule_Fusion] FOREIGN KEY([FusionID]) REFERENCES [dbo].[Fusion] ([ID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[FusionAttributeOwnerRule] CHECK CONSTRAINT [FK_FusionAttributeOwnerRule_Fusion]
GO
ALTER TABLE [dbo].[FusionAttributePromotionRule]  WITH CHECK ADD  CONSTRAINT [FK_FusionAttributePromotionRule_Fusion] FOREIGN KEY([FusionID]) REFERENCES [dbo].[Fusion] ([ID])
GO
ALTER TABLE [dbo].[FusionAttributePromotionRule] CHECK CONSTRAINT [FK_FusionAttributePromotionRule_Fusion]
GO
ALTER TABLE [dbo].[FusionAttributePromotion]  WITH CHECK ADD  CONSTRAINT [FK_FusionAttributePromotion_FusionAttribute] FOREIGN KEY([FusionAttributeID]) REFERENCES [dbo].[FusionAttribute] ([ID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[FusionAttributePromotion] CHECK CONSTRAINT [FK_FusionAttributePromotion_FusionAttribute]
GO

DROP INDEX [IX_FusionAttribute_FusionID-FusionAttributeTypeID-ParentID] ON [dbo].[FusionAttribute]
GO
CREATE CLUSTERED INDEX [CIX_FusionAttribute] ON [dbo].[FusionAttribute] ( [FusionID] ASC, [FusionAttributeTypeID] ASC, [ParentID] ASC )
GO
--Pappas:  Added above on 04/27/16


--Pappas: Added below on 05/17/2016
drop table EventType
go
drop view LookupAllocation
go
drop function utility.GetRootEventTypeID
go

alter table Domain add [SourceArtifactID] INT NULL
go
alter table Domain add  [DomainClassificationID] INT CONSTRAINT [DF_Domain_DomainClassification] DEFAULT ((1)) NOT NULL
go

alter table IntersectType add [Subject] varchar(50) NULL
alter table IntersectType add [SubjectID] int NULL
alter table IntersectType add [Object] varchar(50) NULL
alter table IntersectType add [ObjectID] int NULL
alter table IntersectType add PredicateID int null
alter table IntersectType add [IsSystem] bit NULL
alter table IntersectType add CreatedBy int null
alter table IntersectType add CreatedOn datetime null
go

alter table [Intersect] add [Subject] varchar(50) NULL
alter table [Intersect] add [SubjectID] int NULL
alter table [Intersect] add [Object] varchar(50) NULL
alter table [Intersect] add [ObjectID] int NULL
alter table [Intersect] add [Deleted] bit NULL
alter table [Intersect] add CreatedBy int null
alter table [Intersect] add CreatedOn datetime null
alter table [Intersect] add [UpdatedBy] int null
alter table [Intersect] add [UpdatedOn] datetime null
go

alter table [Intersect] drop column IntersectTypeRoleID
go
--alter table [Intersect] drop column PredicateID
--go

CREATE INDEX IX_Intersect_Subject ON [Intersect] ( [Subject] asc, [SubjectID] asc )
GO
CREATE INDEX IX_Intersect_Object ON [Intersect] ( [Object] asc, [ObjectID] asc )
GO

alter table Predicate add [IsSystem] BIT CONSTRAINT [DF_Predicate_IsSystem] DEFAULT ((0)) NOT NULL
go

update	T
set		T.Subject = S.Subject,
		T.SubjectID = S.SubjectID,
		T.Object = S.Object,
		T.ObjectID = S.ObjectID,
		T.IsSystem = S.IsSystem
from	IntersectType T
		inner join	(
					select	distinct
							S.ObjectType as [Subject],
							S.ObjectID as SubjectID,
							--S.ID,
							O.ObjectType as [Object],
							O.ObjectID as ObjectID,
							--O.ID,
							--coalesce(ITP.[PredicateType], 7) as PredicateType,
							S.IntersectTypeID,
							case
								when S.ObjectType = 'FusionAttributeType' and O.ObjectType = 'FusionAttributeType' then 1 
								else 0
							end as IsSystem--,
							--0 as CreatedBy,
							--null as CreatedOn,
							--0 as UpdatedBy,
							--null as UpdatedOn
					from	IntersectTypeNode S
							inner join IntersectTypeNode O on O.ID <> S.ID and S.[Order] = 1 and O.[Order] = 2 and S.IntersectTypeID = O.IntersectTypeID
							--left join IntersectTypePredicate ITP on ITP.IntersectTypeID = S.IntersectTypeID
							--left join Predicate P on P.ID = ITP.PredicateID
					) S on S.IntersectTypeID = T.ID
where T.SubjectID is null
go

ALTER TABLE [dbo].[Intersect] DISABLE TRIGGER [Intersect_AfterUpsert]
GO

-- load all relationships from intersect table
update	T
set		T.Subject = S.Subject,
		T.SubjectID = S.SubjectID,
		T.Object = S.Object,
		T.ObjectID = S.ObjectID,
		T.Deleted = 0
from	[Intersect] T
		inner join	(
					select	distinct
							S.ObjectType as [Subject],
							S.ObjectID as SubjectID,
							O.ObjectType as [Object],
							O.ObjectID as ObjectID,
							--RT.ID as RelationTypeID,
							--coalesce(M.PredicateID, 21) as PredicateID,
							--0 as Deleted,
							S.IntersectID--,
							--0 as CreatedBy,
							--null as CreatedOn,
							--0 as UpdatedBy,
							--null as UpdatedOn
					from	[IntersectNode] S
							inner join [IntersectNode] O on O.ID <> S.ID and O.IntersectID = S.IntersectID
							inner join IntersectTypeNode ST on ST.ID = S.IntersectTypeNodeID and ST.[Order] = 1
							--left join IntersectMap M on M.[SubjectIntersectNodeID] = S.ID and M.[ObjectIntersectNodeID] = O.ID
							--left join Predicate P on P.ID = M.PredicateID
							--inner join [RelationType] RT on RT.OldIntersectTypeID = ST.IntersectTypeID and ( (RT.PredicateType = P.[Type]) OR (RT.PredicateType = 7) ) and RT.[OldSubjectNodeTypeID] = S.IntersectTypeNodeID and RT.[OldObjectNodeTypeID] = O.IntersectTypeNodeID
					) S on S.IntersectID = T.ID
where T.SubjectID is null
go

select * from [Intersect] where Subject is null
delete [Intersect] where Subject is null
go

ALTER TABLE [dbo].[Intersect] ENABLE TRIGGER [Intersect_AfterUpsert]
GO


ALTER TABLE [dbo].[IntersectTypePredicate] DROP CONSTRAINT [FK_IntersectTypePredicate_IntersectType]
GO

ALTER TABLE [dbo].[IntersectTypePredicate]  WITH CHECK ADD  CONSTRAINT [FK_IntersectTypePredicate_IntersectType] FOREIGN KEY([IntersectTypeID])
REFERENCES [dbo].[IntersectType] ([ID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[IntersectTypePredicate] CHECK CONSTRAINT [FK_IntersectTypePredicate_IntersectType]
GO

create view [dbo].[IntersectDetail]
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
			dbo.GenerateObjectUrl(
				I.Subject, 
				case I.Subject
					when 'Resource' then 1
					when 'Group' then 1
					else coalesce(SA.ArtifactTypeID, SD.DomainTypeID, SF.FusionAttributeTypeID, SI.IntersectTypeID, SP.PolicyTypeID, SR.RuleType, ST.TaxonomyTypeID) 
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
				else coalesce(SA.ArtifactTypeID, SD.DomainTypeID, SF.FusionAttributeTypeID, SI.IntersectTypeID, SP.PolicyTypeID, SR.RuleType, ST.TaxonomyTypeID) 
			end as SubjectTypeID,
			case 
				when I.Subject = 'Rule' and SR.RuleType = 1 then 'Informational Rule'
				when I.Subject = 'Rule' and SR.RuleType = 2 then 'Quality Check Rule'
				when I.Subject = 'Rule' and SR.RuleType = 3 then 'Metric Rule'
				when I.Subject = 'Rule' and SR.RuleType = 4 then 'Profile Rule'
				when I.Subject = 'Intersect' then utility.DeriveIntersectTypeName(SI.IntersectTypeID)
				else coalesce(SAT.Name, SDT.Name, SFT.TextPath, SPT.Name, STT.Name) 
			end as SubjectTypeName,
			coalesce(SIcon.IconBackColor, '#000') as SubjectIconBackColor,
			coalesce(SIcon.IconForeColor, '#fff') as SubjectIconForeColor,
			coalesce(SIcon.IconText, substring(coalesce(SAT.Name, SDT.Name, SFT.TextPath, SPT.Name, STT.Name, ''), 1, 2)) as SubjectIconText,

			I.Object,
			I.ObjectID,
			case I.Object
				when 'Intersect' then utility.DeriveIntersectName(OI.ID)
				when 'Resource' then ORE.FirstName + ' ' + ORE.LastName
				else coalesce(OA.TextPath, OD.Name, [OF].TextPath, OG.Name, OP.TextPath, [OR].Name, OT.TextPath)
			end as ObjectName,
			dbo.GenerateObjectUrl(
				I.Object, 
				case I.Object
					when 'Resource' then 1
					when 'Group' then 1
					else coalesce(OA.ArtifactTypeID, OD.DomainTypeID, [OF].FusionAttributeTypeID, OI.IntersectTypeID, OP.PolicyTypeID, [OR].RuleType, OT.TaxonomyTypeID)
				end,
				I.ObjectID) as ObjectUrl,
			case I.Object
				when 'Artifact' then 'ArtifactType'
				when 'Domain' then 'DomainType'
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
				else coalesce(OA.ArtifactTypeID, OD.DomainTypeID, [OF].FusionAttributeTypeID, OI.IntersectTypeID, OP.PolicyTypeID, [OR].RuleType, OT.TaxonomyTypeID)
			end as ObjectTypeID,
			case
				when I.Object = 'Rule' and [OR].RuleType = 1 then 'Informational Rule'
				when I.Object = 'Rule' and [OR].RuleType = 2 then 'Quality Check Rule'
				when I.Object = 'Rule' and [OR].RuleType = 3 then 'Metric Rule'
				when I.Object = 'Rule' and [OR].RuleType = 4 then 'Profile Rule'
				when I.Object = 'Intersect' then utility.DeriveIntersectTypeName(OI.IntersectTypeID)
				else coalesce(OAT.Name, ODT.Name, OFT.TextPath, OPT.Name, OTT.Name) 
			end as ObjectTypeName,
			coalesce(OIcon.IconBackColor, '#000') as ObjectIconBackColor,
			coalesce(OIcon.IconForeColor, '#fff') as ObjectIconForeColor,
			--coalesce(OIcon.IconText, 'leaf') as ObjectIconText,
			coalesce(OIcon.IconText, substring(coalesce(OAT.Name, ODT.Name, OFT.TextPath, OPT.Name, OTT.Name, ''), 1, 2)) as ObjectIconText,

			IT.PredicateID,
			P.Name as [PredicateName],
			P.Type as PredicateType
	from	dbo.[Intersect] I with(nolock)
			inner join dbo.[IntersectType] IT with(nolock) on IT.ID = I.IntersectTypeID
			left join [Predicate] P with(nolock) on P.ID = IT.PredicateID 
			left join dbo.Artifact SA with(nolock) on I.Subject = 'Artifact' and SA.ID = I.SubjectID
			left join dbo.ArtifactType SAT with(nolock) on SAT.ID = SA.ArtifactTypeID
			left join dbo.Domain SD with(nolock) on I.Subject = 'Domain' and SD.ID = I.SubjectID
			left join dbo.DomainType SDT with(nolock) on SDT.ID = SD.DomainTypeID
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
			left join dbo.Domain OD with(nolock) on I.Object = 'Domain' and OD.ID = I.ObjectID
			left join dbo.DomainType ODT with(nolock) on ODT.ID = OD.DomainTypeID
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
																					else coalesce(SA.ArtifactTypeID, SD.DomainTypeID, SF.FusionAttributeTypeID, SI.IntersectTypeID, SP.PolicyTypeID, SR.RuleType, ST.TaxonomyTypeID) 
																				end
			left join ObjectStyle OIcon with(nolock) on OIcon.ObjectType =	case I.Object
																				when 'Group' then 'GroupType'
																				when 'Resource' then 'ResourceType'
																				else I.Object + 'Type'
																			end 
														and OIcon.ObjectID =	case I.Object
																					when 'Resource' then 1
																					when 'Group' then 1
																					else coalesce(OA.ArtifactTypeID, OD.DomainTypeID, [OF].FusionAttributeTypeID, OI.IntersectTypeID, OP.PolicyTypeID, [OR].RuleType, OT.TaxonomyTypeID) 
																				end

	where	coalesce(SA.ID, SD.ID, SF.ID, SG.ID, SI.ID, SP.ID, SR.ID, SRE.ResourceID, ST.ID) is not null
			and coalesce(OA.ID, OD.ID, [OF].ID, OG.ID, OI.ID, OP.ID, [OR].ID, ORE.ResourceID, OT.ID) is not null
GO

alter procedure [dbo].[AddRelationships]
--declare
	@ResourceID int,
	@Date datetime,
	@Type varchar(50),				-- The start object type.
	@ID int,						-- The start object ID.
	@Classification int,
	@IntersectRole int,
	@Description nvarchar(4000),
	@Objects ObjectsTable READONLY
	
--set @ResourceID = 1
--set @Date = getutcdate()
--set @Type = 'Artifact'
--set @ID = 3
--set @Classification = 1
--set @IntersectRole = NULL
--set @Description = ''
--insert into @Objects VALUES ('Artifact', 2)
as
begin
	set nocount on;

	declare @current int,
			@max int,
			@ErrorMessage nvarchar(2500),
			@IntersectID int,
			
			@StartType varchar(50),	@StartTypeID int,
			@EndType varchar(50),	@EndTypeID int,	
			@IntersectTypeID int
	
	/*	Get the relationship types we need to check or create.	*/
	declare @RelationTypes table (
		ID int identity, 
		StartType varchar(50), StartTypeID int, 
		EndType varchar(50), EndTypeID int, 
		IntersectTypeID int
	)

	insert into @RelationTypes
		select	* 
		from	(
				select	distinct 
						S.ObjectType as StartType, S.ObjectTypeID as StartTypeID, 
						E.ObjectType as EndType, E.ObjectTypeID as EndTypeID, 
						RT.IntersectTypeID
				from	@Objects O
						inner join cache.[Object] S on S.[Object] = @Type and S.ObjectID = @ID
						inner join cache.[Object] E on E.[Object] = O.ObjectType and E.ObjectID = O.ObjectID
						left join utility.RelationshipTypes RT on RT.SourceObjectType = S.ObjectType and RT.SourceObjectID = S.ObjectTypeID and RT.TargetObjectType = E.ObjectType and RT.TargetObjectID = E.ObjectTypeID
				) O where IntersectTypeID is null

	set @current = 1
	select @max = MAX(ID) from @RelationTypes
	while @current <= @max
	begin
		select	@StartType = StartType,
				@StartTypeID = StartTypeID,	

				@EndType = EndType,
				@EndTypeID = EndTypeID,	

				@IntersectTypeID = IntersectTypeID
		from	@RelationTypes
		where	ID = @current

		-- Relationship does not yet exist, so CREATE.
		INSERT INTO [IntersectType] (UpdatedOn, UpdatedBy, Subject, SubjectID, Object, ObjectID, IsSystem) VALUES (getutcdate(), 0, @StartType, @StartTypeID, @EndType, @EndTypeID, 0)

		SELECT @IntersectTypeID = SCOPE_IDENTITY()

		INSERT INTO IntersectTypeNode	(IntersectTypeID, ObjectType, ObjectID, [Order]) 
		VALUES							(@IntersectTypeID, @StartType, @StartTypeID, 1)

		INSERT INTO IntersectTypeNode	(IntersectTypeID, ObjectType, ObjectID, [Order])
		VALUES							(@IntersectTypeID, @EndType, @EndTypeID, 2)

		set @current = @current + 1
	end


	-- Now deal with the objects themselves.
	declare @Relations table (
		ID int identity, 
			
		StartObject varchar(50), StartObjectID int, StartName nvarchar(500), StartType varchar(50), StartTypeID int, StartIntersectNodeID int, StartIntersectNodeTypeID int,
		EndObject varchar(50), EndObjectID int, EndName nvarchar(500), EndType varchar(50), EndTypeID int, EndIntersectNodeID int, EndIntersectNodeTypeID int,

		IntersectTypeID int, IntersectID int, [Action] varchar(1)
	)

	insert into @Relations
		select	distinct 
				O.ObjectType, O.ObjectID, OD.Name, OD.ObjectType, OD.ObjectTypeID, R.StartIntersectNodeID, RT.SourceIntersectTypeNodeID, 
				@Type, @ID, D.Name, D.ObjectType, D.ObjectTypeID, R.EndIntersectNodeID, RT.TargetIntersectTypeNodeID,
				RT.IntersectTypeID, R.IntersectID, CASE WHEN R.IntersectID IS NULL THEN 'C' ELSE 'U' END
		from	@Objects O
				left join cache.ObjectDetails OD on OD.[Object] = @Type and OD.ObjectID = @ID
				left join cache.ObjectDetails D on D.[Object] = O.ObjectType and D.ObjectID = O.ObjectID
				left join utility.RelationshipTypes RT on RT.SourceObjectType = OD.ObjectType and RT.SourceObjectID = OD.ObjectTypeID and RT.TargetObjectType = D.ObjectType and RT.TargetObjectID = D.ObjectTypeID
				outer apply (
							select	i.ID as IntersectID,
									N2.ID as StartIntersectNodeID,
									N1.ID as EndIntersectNodeID
							from	[Intersect] I
									inner join IntersectNode N1 on N1.IntersectID = I.ID and N1.ObjectType = @Type and N1.ObjectID = @ID
									inner join IntersectNode N2 on N2.IntersectID = I.ID and N2.ObjectType = O.ObjectType and N2.ObjectID = O.ObjectID
							where	i.IntersectTypeID = RT.IntersectTypeID
							) R

	set @current = 1
	select @max = MAX(ID) from @Relations
	while @current <= @max
	begin
		declare @StartObject varchar(50),	@StartObjectID int, @StartName nvarchar(500),	@StartIntersectNodeID int,	@StartIntersectNodeTypeID int, 
				@EndObject varchar(50),		@EndObjectID int,	@EndName nvarchar(500),		@EndIntersectNodeID int,	@EndIntersectNodeTypeID int,
				@Action varchar(1)

		set @IntersectID = null	--reset here

		select	@StartObject = StartObject,
				@StartObjectID = StartObjectID,
				@StartName = StartName,	
				@StartTypeID = StartTypeID,	
				@StartIntersectNodeID = StartIntersectNodeID,
				@StartIntersectNodeTypeID = StartIntersectNodeTypeID, 

				@EndObject = EndObject,
				@EndObjectID = EndObjectID,	
				@EndName = EndName,	
				@EndTypeID = EndTypeID,	
				@EndIntersectNodeID = EndIntersectNodeID,
				@EndIntersectNodeTypeID = EndIntersectNodeTypeID,

				@IntersectTypeID = IntersectTypeID, 
				@IntersectID = IntersectID, 
				@Action = [Action]
		from	@Relations
		where	ID = @current

		if @ID > 0
		begin
			-- Relationship does not yet exist, so CREATE.
			if @IntersectID is null and @StartIntersectNodeTypeID is not null and @EndIntersectNodeTypeID is not null
				begin
					INSERT INTO [Intersect] (
						IntersectTypeID, 
						Classification, 
						[Description],
						[Subject], SubjectID,
						[Object], ObjectID 					
					) 
					VALUES (
						@IntersectTypeID, 
						@Classification, 
						@Description,
						@StartObject, @StartObjectID,
						@EndObject, @EndObjectID
					)

					SELECT @IntersectID = SCOPE_IDENTITY()

					INSERT INTO IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID) 
					VALUES						(@StartIntersectNodeTypeID, @IntersectID, @StartObject, @StartObjectID)

					SELECT @StartIntersectNodeID = SCOPE_IDENTITY()

					INSERT INTO IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
					VALUES						(@EndIntersectNodeTypeID, @IntersectID, @EndObject, @EndObjectID)

					SELECT @EndIntersectNodeID = SCOPE_IDENTITY()

					update	@Relations
					set		IntersectID = @IntersectID,
							StartIntersectNodeID = @StartIntersectNodeID,
							EndIntersectNodeID = @EndIntersectNodeID
					where	(StartObject = @StartObject and StartObjectID = @StartObjectID and EndObject = @EndObject and EndObjectID = @EndObjectID) 
							or (StartObject = @EndObject and StartObjectID = @EndObjectID and EndObject = @StartObject and EndObjectID = @StartObjectID)
							--ID = @current

					insert into cache.[Object] ( [Object], [ObjectID], [ObjectType], [ObjectTypeID] )
					values	( 'Intersect', @IntersectID, 'IntersectType', @IntersectTypeID );

					insert into cache.Relationship ( IntersectID, SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID )
					values	( @IntersectID, @StartIntersectNodeTypeID, @StartIntersectNodeID, @StartObject, @StartObjectID, @EndIntersectNodeTypeID, @EndIntersectNodeID, @EndObject, @EndObjectID );
					insert into cache.Relationship ( IntersectID, SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID )
					values	( @IntersectID, @EndIntersectNodeTypeID, @EndIntersectNodeID, @EndObject, @EndObjectID, @StartIntersectNodeTypeID, @StartIntersectNodeID, @StartObject, @StartObjectID );

					--Update the responsibilities of the object that should inherit form the other (Taxonomy can push relationships down to artifact)
					if ( (@StartObject = 'Taxonomy' and @EndObject = 'Artifact') OR (@StartObject = 'Artifact' and @EndObject = 'Taxonomy') )
					begin
						if @StartObject = 'Artifact'
						begin
							exec [cache].[SynchronizeResponsibilitiesForObject] @StartObject, @StartObjectID
						end
						if @EndObject = 'Artifact'
						begin
							exec [cache].[SynchronizeResponsibilitiesForObject] @EndObject, @EndObjectID
						end
					end

					exec utility.AddAuditEntry @StartObject, @StartObjectID, @ResourceID, @Date, 'Created', 'Intersect', @IntersectID
					exec utility.AddAuditEntry @EndObject, @EndObjectID, @ResourceID, @Date, 'Created', 'Intersect', @IntersectID
				end
			else
				begin
					-- Update the Classification and Description only if the relationship already exists.
					if @IntersectID is not null
					begin
						update	[Intersect]
						set		Classification = @Classification,
								Description = @Description
						where	ID = @IntersectID

						exec utility.AddAuditEntry 'Intersect', @IntersectID, @ResourceID, @Date, 'Updated', 'Intersect', @IntersectID
					end
				end
		end

		set @current = @current + 1
	end
end
GO


alter procedure [utility].[AddRelationDiagramRelations]
	@diagramRelations [utility].[DiagramRelationshipTable] readonly,
	@NumberOfIntersectsAdded int OUTPUT,
	@NumberOfObjectsUpdated int OUTPUT
as
begin
	set nocount on;

	If EXISTS (SELECT 1 FROM @diagramRelations)		
			begin
				Declare @IDList Table(IntersectID int,RelID Int);
				Declare @SourceIntersectNodeList Table(IntersectNodeID int,Item Int);
				Declare @TargetIntersectNodeList Table(IntersectNodeID int,Item Int);
				Declare @IntersectMapTemp Table(SubjectIntersectNode int, ObjectIntersectNode int, PredicateID int, [Type] int);
				Declare @Intersects IDTable;

				select @NumberOfIntersectsAdded = @NumberOfIntersectsAdded + (select count(1) from @diagramRelations);

				--insert intersect records and save there id's
				-- trick is to use merge to keep the sequence id and staging row ids
				-- http://stackoverflow.com/questions/15614261/using-output-clause-to-insert-value-not-in-inserted
				MERGE
							INTO    [Intersect] d
							USING   (
										select	rel.IntersectTypeID, 
												2 as Classification,
												rel.SourceObject,
												rel.SourceObjectID,
												rel.TargetObject,
												rel.TargetObjectID,
												rel.ItemID as srID 
										from	@diagramRelations rel						
									) s
							ON      (1 = 0)
							WHEN NOT MATCHED THEN
							INSERT  (IntersectTypeID, Classification, Description, Subject, SubjectID, Object, ObjectID)
							VALUES  (s.IntersectTypeID, s.Classification, NULL, s.SourceObject, s.SourceObjectID, s.TargetObject, s.TargetObjectID)
							OUTPUT  INSERTED.ID, s.srID into @IDList;
							
				--insert start records into intersect node track the id that it gets 
				MERGE
							INTO    IntersectNode d
							USING   (
										select	sr.SourceIntersectTypeNodeID, 
												il.IntersectID, 
												sr.SourceObject,
												sr.SourceObjectID, 
												sr.ItemID as itemID 
										from	@diagramRelations sr 
												inner join @IDList il on (sr.ItemID = il.RelID)											
									) s
							ON      (1 = 0)
							WHEN NOT MATCHED THEN
							INSERT  (IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
							VALUES  (s.SourceIntersectTypeNodeID, s.IntersectID, s.SourceObject, s.SourceObjectID)
							OUTPUT  INSERTED.ID, s.itemID into @SourceIntersectNodeList;
					
				MERGE
							INTO    IntersectNode d
							USING   (
										select	sr.TargetIntersectTypeNodeID, 
												il.IntersectID, 
												sr.TargetObject,
												sr.TargetObjectID, 
												sr.ItemID as itemID 
										from	@diagramRelations sr 
												inner join @IDList il on (sr.ItemID = il.RelID)											
									) s
							ON      (1 = 0)
							WHEN NOT MATCHED THEN
							INSERT  (IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
							VALUES  (s.TargetIntersectTypeNodeID, s.IntersectID, s.TargetObject, s.TargetObjectID)
							OUTPUT  INSERTED.ID, s.itemID into @TargetIntersectNodeList;
					
				--add record for each to intersectmap table
				insert into intersectmap
					select 
						sList.IntersectNodeID as SubjectIntersectNode,
						tList.IntersectNodeID as ObjectIntersectNode,
						itemList.[predicateid] as PredicateID,
						itemList.[type] as [Type]
					from
						@diagramRelations itemList
						inner join @SourceIntersectNodeList sList on (itemList.ItemID = sList.Item)
						inner join @TargetIntersectNodeList tList on (itemList.ItemID = tList.Item)
					where 
						itemList.needsMapRecord = 1
					
				select @NumberOfObjectsUpdated = @NumberOfObjectsUpdated + 1;
					
				insert into @Intersects select idl.intersectid from @IDList idl;

				if exists (select 1 from @Intersects)
				begin
					EXEC cache.SynchronizeRelationships @Intersects		
				end
			end	-- end if intersects are needed

end
GO

alter procedure [dbo].[AddSingleIntersect]
	@ResourceID int,
	@IntersectTypeID int,
	@Subject varchar(50),			-- The start object type.
	@SubjectID int,					-- The start object ID.
	@Object varchar(50),			-- The end object type.
	@ObjectID int,					-- The end object ID.	
	@Classification int,
	@Description nvarchar(4000)
as
begin
	set nocount on;

	declare @Date datetime = getutcdate(),
			@ErrorMessage nvarchar(2500),
			@IntersectID int,
			@SubjectIntersectTypeNodeID int,
			@SubjectIntersectNodeID int,
			@ObjectIntersectTypeNodeID int,
			@ObjectIntersectNodeID int

	select	@IntersectID = I.ID,
			@SubjectIntersectTypeNodeID = N1.IntersectTypeNodeID,	@SubjectIntersectNodeID = N1.ID,
			@ObjectIntersectTypeNodeID = N2.IntersectTypeNodeID,	@ObjectIntersectNodeID = N2.ID
	from	[Intersect] I
			inner join IntersectNode N1 on N1.IntersectID = I.ID and N1.ObjectType = @Subject and N1.ObjectID = @SubjectID
			inner join IntersectNode N2 on N2.IntersectID = I.ID and N2.ObjectType = @Object and N2.ObjectID = @ObjectID

	if @IntersectID is not null and @IntersectID > 0
		begin
			-- Update

			update	[Intersect]
			set		Classification = @Classification,
					Description = @Description
			where	ID = @IntersectID

			exec utility.AddAuditEntry 'Intersect', @IntersectID, @ResourceID, @Date, 'Updated', 'Intersect', @IntersectID
		end
	else
		begin
			-- Create

			declare @SubjectType varchar(50),
					@SubjectTypeID int,
					@ObjectType varchar(50),
					@ObjectTypeID int

			select	@SubjectType = ObjectType, @SubjectTypeID = ObjectTypeID	from cache.[Object] where [Object] = @Subject and ObjectID = @SubjectID 
			select	@ObjectType = ObjectType, @ObjectTypeID = ObjectTypeID		from cache.[Object] where [Object] = @Object and ObjectID = @ObjectID 

			select	distinct 
					@SubjectIntersectTypeNodeID = SourceIntersectTypeNodeID, 
					@ObjectIntersectTypeNodeID = TargetIntersectTypeNodeID
			from	utility.RelationshipTypes R 
			where	SourceObjectType = @SubjectType and SourceObjectID = @SubjectTypeID 
					and TargetObjectType = @ObjectType and TargetObjectID = @ObjectTypeID

			if @SubjectIntersectTypeNodeID is not null and @ObjectIntersectTypeNodeID is not null
				begin
					INSERT INTO [Intersect] (
						IntersectTypeID, 
						Classification, 
						[Description],
						[Subject], SubjectID,
						[Object], ObjectID 					
					) 
					VALUES (
						@IntersectTypeID, 
						@Classification, 
						@Description,
						@Subject, @SubjectID,
						@Object, @ObjectID
					)

					SELECT @IntersectID = SCOPE_IDENTITY()

					INSERT INTO IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID) 
					VALUES						(@SubjectIntersectTypeNodeID, @IntersectID, @Subject, @SubjectID)

					SELECT @SubjectIntersectNodeID = SCOPE_IDENTITY()

					INSERT INTO IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
					VALUES						(@ObjectIntersectTypeNodeID, @IntersectID, @Object, @ObjectID)

					SELECT @ObjectIntersectNodeID = SCOPE_IDENTITY()

					insert into cache.[Object] ( [Object], [ObjectID], [ObjectType], [ObjectTypeID] )
					values	( 'Intersect', @IntersectID, 'IntersectType', @IntersectTypeID );

					insert into cache.Relationship ( IntersectID, SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID )
					values	( @IntersectID, @SubjectIntersectTypeNodeID, @SubjectIntersectNodeID, @Subject, @SubjectID, @ObjectIntersectTypeNodeID, @ObjectIntersectNodeID, @Object, @ObjectID );
					insert into cache.Relationship ( IntersectID, SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID )
					values	( @IntersectID, @ObjectIntersectTypeNodeID, @ObjectIntersectNodeID, @Object, @ObjectID, @SubjectIntersectTypeNodeID, @SubjectIntersectNodeID, @Subject, @SubjectID );

					--Update the responsibilities of the object that should inherit form the other (Taxonomy can push relationships down to artifact)
					if ( (@Subject = 'Taxonomy' and @Object = 'Artifact') OR (@Subject = 'Artifact' and @Object = 'Taxonomy') )
						begin
							if @Subject = 'Artifact'
							begin
								exec [cache].[SynchronizeResponsibilitiesForObject] @Subject, @SubjectID
							end
							if @Object = 'Artifact'
							begin
								exec [cache].[SynchronizeResponsibilitiesForObject] @Object, @ObjectID
							end
						end

					exec utility.AddAuditEntry @Subject, @SubjectID, @ResourceID, @Date, 'Created', 'Intersect', @IntersectID
					exec utility.AddAuditEntry @Object, @ObjectID, @ResourceID, @Date, 'Created', 'Intersect', @IntersectID
				end
		end

	select * from [Intersect] where ID = @IntersectID
end
GO

ALTER PROCEDURE [dbo].[GetCommentDetailByID]
	@id int
AS
BEGIN
	with i (owner1, owner2) 
	as
	(
		select primaryownerresourceid as owner1,secondaryownerresourceid as owner2 from responsibility r
		join responsibilitytype rt on rt.id = r.responsibilitytypeid
		join [group] g on g.id = rt.responsibilitytypegroup
		where r.objecttype = (select ownerobjecttype from comment where id = @id)
		and r.objectid = (select ownerobjectid from comment where id = @id)
	),
	P (ID, ParentID)
	AS
	(
		SELECT		C.ID,
					C.ParentID
		FROM		Comment C
		WHERE		ID = @id
		UNION ALL
		SELECT	C.ID, 
				C.ParentID
		FROM	Comment C
				INNER JOIN P PAR ON PAR.ID = C.ParentID
	)

	SELECT		C.*,
				C.CreatingResourceID,
				O.Name as ObjectName,
				O.Url as ObjectUrl,
				case
					WHEN C.ParentID IS NULL THEN C.OwnerObjectType
					ELSE 'Resource'
				end as ObjectType,
				case 
					WHEN C.ParentID IS NULL THEN C.OwnerObjectID
					ELSE C.CreatingResourceID
				end as ObjectID,
				(
				select	CRD.Object,
						CRD.ObjectID,
						CRD.TextPath,
						CRD.ObjectTypeName,
						CRD.Url,
						CRD.IconForeColor,
						CRD.IconBackColor
				from	CommentRelation CR
						inner join cache.ObjectDetails CRD on CR.CommentID = C.ID and CR.ObjectType = CRD.[Object] and CR.ObjectID = CRD.ObjectID
				for xml path('tag'), root('tags'), type
				) as TagsXml,
										(
				select CommentID,
						ResourceID,
						vote as VoteValue
				from commentvote
				where commentid = p.ID
					for xml path('vote'), root('votes'), type
			) as VotesXML,
			CASE WHEN (select count(*) from i where owner1 = C.CreatingResourceID) > 0  THEN
				cast(1 as bit)
			WHEN (select count(*) from i where owner2 = C.CreatingResourceID) > 0  THEN
				cast(1 as bit)
			ELSE
				cast(0 as bit)
			END as CreatorIsOwner
	FROM		Comment C
				--INNER JOIN CommentRelation CR ON CR.CommentID = C.ID
				left join cache.ObjectDetails O on O.[Object] = C.OwnerObjectType and O.ObjectID = C.OwnerObjectID
				INNER JOIN P ON C.ID = P.ID
	ORDER BY	C.ParentID, C.DateCreated DESC
END
GO

alter procedure [dbo].[ProcessBulkLoad]
--declare
	@LoadID int
--set @LoadID = 29
as
begin
	set nocount on;

	declare @Object varchar(50),
			@ObjectID int,
			@Action varchar(1),
			@UpdatedBy int = 0

	select	@Object = [Object],
			@ObjectID = ObjectID,
			@Action = [Action],
			@UpdatedBy = UpdatedBy
	from	[Load]
	where	ID = @LoadID

	-- PARSE any dynamic fields that are specifically lookups.
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
									when L_D.ID is not null then 'Domain'
									when L_DI.ID is not null then 'DomainItem'
									when L_F.ID is not null then 'FusionAttribute'
									when L_I.ID is not null then 'Intersect'
									when L_L.Value is not null then 'Lookup'
									when L_T.ID is not null then 'Taxonomy'
									else NULL
								end as LookupObject,
								coalesce(L_A.ID, L_D.ID, L_DI.ID, L_F.ID, L_I.ID, L_L.Value, L_T.ID) as LookupObjectID
						from	FieldType F
								inner join [Load] L on L.ID = @LoadID and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
								inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
								inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
								
								left join Artifact L_A on F.LookupObjectType in ('Artifact', 'ArtifactType') and L_A.ArtifactTypeID = F.LookupObjectID and (L_A.[Name] = IC.Value OR L_A.TextPath = IC.Value)
								left join Domain L_D on F.LookupObjectType in ('Domain', 'DomainType') and L_D.DomainTypeID = F.LookupObjectID and L_D.[Name] = IC.Value
								left join DomainItem L_DI on F.LookupObjectType = 'DomainItem' and L_DI.DomainID = F.LookupObjectID and L_DI.[Name] = IC.Value
								left join FusionAttribute L_F on F.LookupObjectType = 'FusionAttributeType' and L_F.FusionAttributeTypeID = F.LookupObjectID and (L_F.[Name] = IC.Value OR L_F.TextPath = IC.Value)
								left join [Intersect] L_I on F.LookupObjectType = 'IntersectType' and L_I.IntersectTypeID = F.LookupObjectID and L_I.[Name] = IC.Value
								left join [FieldLookupValue] L_L on F.ID = L_L.FieldTypeID and F.LookupObjectType = 'Lookup' and L_L.LookupObjectID = F.LookupObjectID and L_L.[Text] = IC.Value
								left join Taxonomy L_T on F.LookupObjectType in ('Taxonomy', 'TaxonomyType') and L_T.TaxonomyTypeID = F.LookupObjectID and (L_T.[Name] = IC.Value OR L_T.TextPath = IC.Value)
						where	F.[Type] = 'Lookup'
						) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

		-- PARSE any Subject AREA fields.  This is only in the case of artifacts.
		update	T
		set		T.LookupObject = S.LookupObject,
				T.LookupObjectID = S.LookupObjectID
		from	LoadItemColumn T
				inner join	(
							select	IC.LoadID,
									IC.RowIndex,
									IC.ColumnIndex,
									'TaxonomyType' as LookupObject,
									T.ID as LookupObjectID
							from	[Load] L 
									inner join [LoadColumn] C on L.ID = @LoadID and L.[Object] = 'ArtifactType' and C.LoadID = L.ID and C.Name = 'Subject Area'
									inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
									inner join TaxonomyType T on T.[Name] = IC.Value
							) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

		-- PARSE any Domain Group fields.  This is only in the case of domains.
		update	T
		set		T.LookupObject = S.LookupObject,
				T.LookupObjectID = S.LookupObjectID
		from	LoadItemColumn T
				inner join	(
							select	IC.LoadID,
									IC.RowIndex,
									IC.ColumnIndex,
									'DomainGroup' as LookupObject,
									T.ID as LookupObjectID
							from	[Load] L 
									inner join [LoadColumn] C on L.ID = @LoadID and L.[Object] = 'DomainType' and C.LoadID = L.ID and C.Name = 'Domain Group'
									inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
									inner join DomainGroup T on T.[Name] = IC.Value and T.DomainTypeID = @ObjectID
							) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

		-- PARSE any Parent Artifact fields.  This is only in the case of artifacts.
		update	T
		set		T.LookupObject = S.LookupObject,
				T.LookupObjectID = S.LookupObjectID
		from	LoadItemColumn T
				inner join	(
							select	IC.LoadID,
									IC.RowIndex,
									IC.ColumnIndex,
									'Artifact' as LookupObject,
									P.ID as LookupObjectID
							from	[Load] L 
									inner join ArtifactType T on L.ID = @LoadID and L.[Object] = 'ArtifactType' and L.ObjectID = T.ID
									inner join ArtifactType PT on PT.ID = T.ParentID
									inner join [LoadColumn] C on C.LoadID = L.ID and C.Name = 'Parent ' + PT.Name
									inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
									inner join Artifact P on P.ArtifactTypeID = PT.ID and (P.[TextPath] = IC.Value or P.[Name] = IC.Value)
							) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex


	if @Action = 'P'	--PROMOTION
	begin
		if @Object = 'AttributeType'
		begin
			-- Clean Owner Type field.
			update	LoadItemColumn
			set		Value = case when charindex('Type', Value) > 0 then Value else Value + 'Type' end
			where	LoadID = @LoadID and ColumnIndex = 1

			-- PARSE Owner Type fields.
			update	T
			set		T.LookupObject = S.LookupObject,
					T.LookupObjectID = S.LookupObjectID
			from	LoadItemColumn T
					inner join	(
								select	LI.LoadID,
										LI.RowIndex,
										C2.ColumnIndex,
										D.[Object] as LookupObject,
										D.ObjectID as LookupObjectID
								from	[Load] L
										inner join LoadItem LI on LI.LoadID = L.ID and L.ID = @LoadID
										inner join [LoadItemColumn] C1 on C1.LoadID = LI.LoadID and C1.RowIndex = LI.RowIndex and C1.ColumnIndex = 1 --'Owner Type' 
										inner join [LoadItemColumn] C2 on C2.LoadID = LI.LoadID and C2.RowIndex = LI.RowIndex and C2.ColumnIndex = 2 --'Owner Type Name'
										inner join cache.ObjectDetails D on D.[Object] = C1.Value and D.[Name] = C2.Value
								) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

			-- PARSE Owner fields.
			update	T
			set		T.LookupObject = S.LookupObject,
					T.LookupObjectID = S.LookupObjectID
			from	LoadItemColumn T
					inner join	(
								select	LI.LoadID,
										LI.RowIndex,
										C3.ColumnIndex,
										D.[Object] as LookupObject,
										D.ObjectID as LookupObjectID
								from	[Load] L
										inner join LoadItem LI on LI.LoadID = L.ID and L.ID = @LoadID
										--inner join [LoadItemColumn] C1 on	C1.LoadID = LI.LoadID	and C1.RowIndex = LI.RowIndex	and C1.ColumnIndex = 1 --'Owner Type' 
										inner join [LoadItemColumn] C2 on C2.LoadID = LI.LoadID and C2.RowIndex = LI.RowIndex and C2.ColumnIndex = 2 --'Owner Type Name'
										inner join [LoadItemColumn] C3 on C3.LoadID = LI.LoadID	and C3.RowIndex = LI.RowIndex and C3.ColumnIndex = 3 --'Owner Name'
										inner join cache.ObjectDetails D on D.[ObjectType] = C2.[LookupObject] and D.ObjectTypeID = C2.LookupObjectID and D.[Name] = C3.Value
								) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex
		end

		declare @ResolvedObjects table ([Object] varchar(50), ObjectID int, [Action] varchar(25), LoadID int, RowIndex int)	--This captures the INSERTED/UPDATED objects from the merge statements below.

		if @Object = 'ArtifactType'
		begin
			declare @RequiresParent bit
			select	@RequiresParent =		case
												when ParentID is null then cast(0 as bit)
												else cast(1 as bit)
											end
									  from	ArtifactType 
									  where	ID = @ObjectID

			merge	Artifact T
			using	(
					select	O.LoadID,
							O.RowIndex,
							O.ArtifactTypeID,
							O.Name,
							D.Description,
							O.ParentID,
							O.TaxonomyTypeID
					from	(
							select	LI.LoadID,
									MIN(LI.RowIndex) as RowIndex,
									@ObjectID as ArtifactTypeID,
									IC_N.Value as Name,
									P.ParentID,
									IC_T.LookupObjectID as TaxonomyTypeID
							from	[LoadItem] LI
									inner join [LoadItemColumn] IC_N on IC_N.LoadID = LI.LoadID and IC_N.RowIndex = LI.RowIndex inner join LoadColumn C_N on C_N.LoadID = LI.LoadID and C_N.ColumnIndex = IC_N.ColumnIndex and C_N.Name = 'Name'
									inner join [LoadItemColumn] IC_T on IC_T.LoadID = LI.LoadID and IC_T.RowIndex = LI.RowIndex inner join LoadColumn C_T on C_T.LoadID = LI.LoadID and C_T.ColumnIndex = IC_T.ColumnIndex and C_T.Name = 'Subject Area' and IC_T.LookupObjectID is not null
									outer apply (
												select	I.LookupObjectID as ParentID
												from	[LoadItemColumn] I
														inner join LoadColumn C on I.LoadID = LI.LoadID and I.RowIndex = LI.RowIndex 
																						and C.LoadID = LI.LoadID and C.ColumnIndex = I.ColumnIndex and C.Name like 'Parent %'
												) P
							where	LI.LoadID = @LoadID
									and (
											(@RequiresParent = 1 and P.ParentID is not null) or
											@RequiresParent = 0
										)
							group by LI.LoadID,
									IC_N.Value,
									P.ParentID,
									IC_T.LookupObjectID
							) O
							outer apply (
								select	I.Value as Description
								from	[LoadItemColumn] I
										inner join LoadColumn C on I.LoadID = O.LoadID and I.RowIndex = O.RowIndex 
																		and C.LoadID = O.LoadID and C.ColumnIndex = I.ColumnIndex and C.Name = 'Description'
							) D
					) S
			on		(T.ArtifactTypeID = S.ArtifactTypeID and T.TaxonomyTypeID = S.TaxonomyTypeID and ((T.ParentID = S.ParentID and S.ParentID is not null) or (T.ParentID is null and S.ParentID is null)) and T.Name = S.Name)
			when	matched then
					update	set T.[Description] = IsNull(S.[Description], T.[Description]),
								T.[ParentID] = S.[ParentID],
								T.[Status] = 'Draft',
								T.TaxonomyTypeID = S.TaxonomyTypeID,
								T.UpdatedBy = @UpdatedBy,
								T.UpdatedOn = getutcdate()
			when	not matched then
					insert (ArtifactTypeID, TaxonomyTypeID, ParentID, Name, [Description], [Status], UpdatedOn, UpdatedBy)
					values (S.ArtifactTypeID, S.TaxonomyTypeID, S.ParentID, S.Name, S.[Description], 'Draft', getutcdate(), @UpdatedBy)
			output	'Artifact', inserted.ID, $action, S.LoadID, S.RowIndex into @ResolvedObjects;

			if @RequiresParent = 1
			begin
				-- Update the LoadItem table with the IDs we recieved in the merge statements above.
				update	T
				set		T.StatusMessage = 'Parent could not be found.'
				from	LoadItem T
						left join @ResolvedObjects S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex
				where	S.ObjectID is null
			end

		end
		else if @Object = 'AttributeType'
		begin
			merge	[Attribute] T
			using	(
					select	I.LoadID,
							I.RowIndex,
							@ObjectID as AttributeTypeID,
							C.LookupObject as [Object],
							C.LookupObjectID as ObjectID
					from	[LoadItem] I
							inner join [LoadItemColumn] C on I.LoadID = @LoadID and C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and C.ColumnIndex = 3
							and C.LookupObject is not null
							and C.LookupObjectID is not null
					) S
			on		(T.AttributeTypeID = S.AttributeTypeID and T.[ObjectType] = S.[Object] and T.[ObjectID] = S.[ObjectID] and T.ParentID = NULL)-- and T.Name = S.Name)
			when	matched then
					update	set T.[UpdatedOn] = getutcdate(),
								T.UpdatedBy = @UpdatedBy
			when	not matched then
					insert (AttributeTypeID, ObjectType, ObjectID, UpdatedOn, UpdatedBy)
					values (S.AttributeTypeID, S.[Object], S.ObjectID, getutcdate(), @UpdatedBy)
			output	'Attribute', inserted.ID, $action, S.LoadID, S.RowIndex into @ResolvedObjects;		
		end
		else if @Object = 'Domain'
		begin
			merge	DomainItem T
			using	(
					select	distinct
							LI.LoadID,
							LI.RowIndex,
							@ObjectID as DomainID,
							IC_C.Value as Code,
							IC_N.Value as Name,
							D.[Description]
					from	[LoadItem] LI
							inner join [LoadItemColumn] IC_C on IC_C.LoadID = LI.LoadID and IC_C.RowIndex = LI.RowIndex inner join LoadColumn C_C on C_C.LoadID = LI.LoadID and C_C.ColumnIndex = IC_C.ColumnIndex and C_C.Name = 'Code'
							inner join [LoadItemColumn] IC_N on IC_N.LoadID = LI.LoadID and IC_N.RowIndex = LI.RowIndex inner join LoadColumn C_N on C_N.LoadID = LI.LoadID and C_N.ColumnIndex = IC_N.ColumnIndex and C_N.Name = 'Name'
							outer apply (
										select	I.Value as Description
										from	[LoadItemColumn] I
												inner join LoadColumn C on I.LoadID = LI.LoadID and I.RowIndex = LI.RowIndex 
																			 and C.LoadID = LI.LoadID and C.ColumnIndex = I.ColumnIndex and C.Name = 'Description'
										) D
					where	LI.LoadID = @LoadID
					) S
			on		(T.DomainID = S.DomainID and T.Code = S.Code)
			when	matched then
					update	set T.[Name] = S.[Name],
								T.[Description] = IsNull(S.[Description],T.[Description]),
								T.[DomainID] = S.[DomainID],
								T.UpdatedBy = @UpdatedBy,
								T.UpdatedOn = getutcdate()
			when	not matched then
					insert (DomainID, Code, Name, [Description], UpdatedOn, UpdatedBy)
					values (S.DomainID, S.Code, S.Name, S.[Description], getutcdate(), @UpdatedBy)
			output	'DomainItem', inserted.ID, $action, S.LoadID, S.RowIndex into @ResolvedObjects;
		end
		else if @Object = 'DomainType'
		begin
			merge	Domain T
			using	(
					select	distinct
							LI.LoadID,
							LI.RowIndex,
							@ObjectID as DomainTypeID,
							IC_N.Value as Name,
							D.[Description],
							IC_G.LookupObjectID as DomainGroupID
					from	[LoadItem] LI
							inner join [LoadItemColumn] IC_N on IC_N.LoadID = LI.LoadID and IC_N.RowIndex = LI.RowIndex inner join LoadColumn C_N on C_N.LoadID = LI.LoadID and C_N.ColumnIndex = IC_N.ColumnIndex and C_N.Name = 'Name'
							outer apply (
										select	I.Value as Description
										from	[LoadItemColumn] I
												inner join LoadColumn C on I.LoadID = LI.LoadID and I.RowIndex = LI.RowIndex 
																			 and C.LoadID = LI.LoadID and C.ColumnIndex = I.ColumnIndex and C.Name = 'Description'
										) D
							inner join [LoadItemColumn] IC_G on IC_G.LoadID = LI.LoadID and IC_G.RowIndex = LI.RowIndex inner join LoadColumn C_G on C_G.LoadID = LI.LoadID and C_G.ColumnIndex = IC_G.ColumnIndex and C_G.Name = 'Domain Group'
					where	LI.LoadID = @LoadID
					) S
			on		(T.DomainTypeID = S.DomainTypeID and T.Name = S.Name)
			when	matched then
					update	set T.[Description] = IsNull(S.[Description],T.[Description]),
								T.[DomainGroupID] = S.[DomainGroupID],
								T.UpdatedOn = getutcdate(),
								T.UpdatedBy = @UpdatedBy
			when	not matched then
					insert (DomainTypeID, DomainGroupID, Name, [Description], UpdatedOn, UpdatedBy)
					values (S.DomainTypeID, S.DomainGroupID, S.Name, S.[Description], getutcdate(), @UpdatedBy)
			output	'Domain', inserted.ID, $action, S.LoadID, S.RowIndex into @ResolvedObjects;
		end
		else if @Object = 'FusionAttributeType'
		begin
			select 1;
		end
		else if @Object = 'TaxonomyType'
		begin
		--begin tran

			declare @currentLevel int,
			@maxLevel int,
			@rowCount int,
			@rowCurr int;

			select 
				@currentLevel = 0
				,@maxLevel = max(
					case when isnumeric(replace(Name,'Level','')) = 1 then
						replace(Name,'Level','') 
					else 
						0 
					end) 
			from 
				LoadColumn 
			where 
				LoadID = @LoadID and Name like 'Level%';
			

			declare @levels table (id int, ColumnIndex int, RowIndex int, [Level] varchar(50), Value varchar(250),MaxLevel int, TaxonomyID int, ParentID int, [Status] varchar(50));
			with v as
			(
				select L.ID, L.Object, L.ObjectID, LC.Name, LC.ColumnIndex, IC.RowIndex, IC.Value, replace(LC.Name,'Level','') as [Level], T.ID as TaxonomyID from [Load] L
				join LoadColumn LC on LC.LoadID = L.ID
				join LoadItemColumn IC on IC.LoadID = LC.LoadID AND IC.ColumnIndex = LC.ColumnIndex
				left join Taxonomy T on T.TaxonomyTypeID = L.ObjectID and T.[Level] = replace(LC.Name,'Level','') and T.Name = IC.Value
				where L.ID = @LoadID AND ltrim(rtrim(IC.Value)) != '' and LC.Name like 'Level%'  
			)
			insert into @levels
			select distinct
				row_number() over (partition by 1 order by v.[Level]) as ID,
				v.ColumnIndex
				,v.RowIndex
				,v.[Level]
				,v.Value
				,m.[Level] as MaxLevel
				,v.TaxonomyID
				,p.TaxonomyID as ParentID 
				,'UPDATE' as [Status]
			from v
			left join v p 
				on p.RowIndex = v.RowIndex and v.TaxonomyID is null and p.ColumnIndex = (v.ColumnIndex - 1)
			inner join v m on m.RowIndex = v.RowIndex and m.[Level] = (select max([Level]) from v where RowIndex = m.RowIndex)
			order by v.[Level] asc;

			--calculate hierarchy
			while @currentLevel <= @maxLevel
			begin
				set @currentLevel = @currentLevel + 1;
				
				update LV
				set LV.ParentID = P.ID
				from @levels LV
				left join @levels P on P.[Level] = (LV.[Level] - 1) AND LV.RowIndex = P.RowIndex
				where LV.[Level] = @currentLevel;
			end 

			--delete records that have a level > 1 and no parentid, missing info
			--delete from @levels where parentid is null and level > 1;

			select @rowCurr = 0, @rowCount = count(*) from @levels;

			while @rowCurr <= @rowCount
			begin
				set @rowCurr = @rowCurr + 1;

				--parent does not exist or leading columns were not filled
				if (select ParentID from @levels where id = @rowCurr) IS NULL AND (select Level from @levels where id = @rowCurr) > 1
				begin
					update @levels set [Status] = 'ERROR' where rowIndex = (select rowindex from @levels where id = @rowCurr);
					continue;
				end


				--update the TaxonomyID for records that do not yet have it
				if (select level from @levels where id = @rowCurr) = 1
				begin
					update LV
					set TaxonomyID = T.ID
					from @levels LV
					join Load L on L.ID = @LoadID
					join Taxonomy T on T.Name = LV.Value and T.ParentID is NULL and T.Level = LV.Level and T.TaxonomyTypeID = L.ObjectID
					where LV.ID = @rowCurr;
				end
				else
				begin
					update LV
					set TaxonomyID = T.ID
					from @levels LV
					left join @levels P on P.ID = LV.ParentID
					join Taxonomy T on T.Name = LV.Value and T.ParentID = P.TaxonomyID and T.Level = LV.Level
					where LV.ID = @rowCurr;
				end

				if (select TaxonomyID from @levels where id = @rowCurr) IS NULL
				begin
					--insert the new taxonomy
					insert into Taxonomy (TaxonomyTypeID, ParentID, Name, [Description], UpdatedOn, UpdatedBy)
					select	distinct
							L.ObjectID as TaxonomyTypeID
						,LVP.TaxonomyID as ParentID
						,LV.Value as Name
						,case when LV.Level = LV.MaxLevel then
							LI.Value
						else
							''
						END as Description
						,getdate() as UpdatedOn
						,@UpdatedBy as UpdatedBy
					from 
						@levels LV
					left join @levels LVP on LVP.ID = LV.ParentID
					join [Load] L on L.ID = @LoadID
					inner join LoadColumn LC on LC.Name = 'Description' and LC.LoadID = @LoadID
					inner join LoadItemColumn LI on LI.RowIndex = LV.RowIndex AND LI.ColumnIndex = LC.ColumnIndex AND LI.LoadID = @LoadID
					where
						LV.ID = @rowCurr

					update @levels set [Status] = 'INSERT' where id = @rowCurr;

					--set the levels taxonomy id after insert
					update LV
					set TaxonomyID = T.ID
					from @levels LV
					left join @levels P on P.ID = LV.ParentID
					join Taxonomy T on T.Name = LV.Value and coalesce(T.ParentID,-1) = coalesce(P.TaxonomyID,-1) and T.Level = LV.Level
					where LV.ID = @rowCurr;
				end
				
				--if level = max, update the description
				if (select level from @levels where id = @rowCurr) = (select maxlevel from @levels where id = @rowCurr)
				begin
					update	T
					set		T.Description = case when LI.Value = '' then T.Description else LI.Value end,
							T.UpdatedOn = getutcdate(),
							T.UpdatedBy = @UpdatedBy
					from	Taxonomy T
							join @levels LV on LV.ID = @rowCurr and T.ID = LV.TaxonomyID
							inner join LoadColumn LC on LC.Name = 'Description' and LC.LoadID = @LoadID
							inner join LoadItemColumn LI on LI.RowIndex = LV.RowIndex AND LI.ColumnIndex = LC.ColumnIndex AND LI.LoadID = @LoadID;

				end
			end --end while
			

			--remove error rows
			delete from @levels
			where rowindex in (select rowindex from @levels where status is null or status = 'ERROR');

						--insert object statuses
			insert into @ResolvedObjects ([Object], ObjectID, [Action], LoadID, RowIndex)
			select
				'Taxonomy',
				TaxonomyID,
				[Status],
				@LoadID,
				RowIndex
			from 
			@levels;

		end

		-- Update the LoadItem table with the IDs we recieved in the merge statements above.
		update	T
		set		T.[Object] = S.[Object],
				T.ObjectID = S.ObjectID,
				T.[Status] = 1,
				T.StatusMessage = case S.[Action]
									when 'INSERT' then 'Added item'
									when 'UPDATE' then 'Updated item'
									else NULL
									end
		from	LoadItem T
				inner join	@ResolvedObjects S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex

		-- Update the LoadItems that were not successfully added or updated.
		update	LoadItem
		set		[Status] = 0,
				[StatusMessage] = coalesce([StatusMessage], '') + ' Item could not be added nor updated.'
		where	LoadID = @LoadID
				and [ObjectID] is null
	end
	else
	begin
		-- This is for actions: R, U, L
		declare @current int,
				@max int,
				@sourceObject varchar(50),
				@sourceObjectID int,
				@sourceIntersectTypeNodeID int,
				@targetObject varchar(50),
				@targetObjectID int,
				@targetIntersectTypeNodeID int,
				@intersectID int = null,
				@date datetime = getutcdate()

		declare @Intersects IDTable

		if @Action = 'L' -- LINEAGE (create lineage from input spreadsheet)
		begin
			declare @focalObject varchar(50),
					@focalObjectID int,
					@focalObjectTypeName nvarchar(1000),
					@focalName nvarchar(500),
					@sourceObjectTypeName nvarchar(1000),
					@sourceName nvarchar(500),
					@targetObjectTypeName nvarchar(1000),
					@targetName nvarchar(500),
					@intersectPredicate varchar(50),
					@predicateID int,
					@focalIntersectID int,
					@rundate datetime = CURRENT_TIMESTAMP,
					@focalSubject nvarchar(500),
					@sourceSubject nvarchar(500),
					@targetSubject nvarchar(500),
					@lineageErrorDetailMessage varchar(200)
			
			select	@current = min(I.RowIndex),
					@max = max(I.RowIndex)
			from	LoadItem I
					inner join LoadItemColumn FT on FT.LoadID = I.LoadID and FT.RowIndex = I.RowIndex and FT.ColumnIndex = 1  --focal point object type
						inner join LoadItemColumn FTN on FTN.LoadID = I.LoadID and FTN.RowIndex = I.RowIndex and FTN.ColumnIndex = 2   --focal point object type name
						inner join LoadItemColumn F on F.LoadID = I.LoadID and F.RowIndex = I.RowIndex and F.ColumnIndex = 4--focal point name		
						inner join LoadItemColumn ST on ST.LoadID = I.LoadID and ST.RowIndex = I.RowIndex and St.ColumnIndex = 5 --source object type
						inner join LoadItemColumn STN on STN.LoadID = I.LoadID and STN.RowIndex = I.RowIndex and StN.ColumnIndex = 6 --source object type name
						inner join LoadItemColumn S on S.LoadID = I.LoadID and S.RowIndex = I.RowIndex and S.ColumnIndex = 8 --source object name
						inner join LoadItemColumn TT on TT.LoadID = I.LoadID and TT.RowIndex = I.RowIndex and TT.ColumnIndex = 9 --target object type
						inner join LoadItemColumn TTN on TTN.LoadID = I.LoadID and TTN.RowIndex = I.RowIndex and TTN.ColumnIndex = 10 --target object type name
						inner join LoadItemColumn T on T.LoadID = I.LoadID and T.RowIndex = I.RowIndex and T.ColumnIndex = 12 --source object name
						inner join LoadItemColumn P on P.LoadID = I.LoadID and P.RowIndex = I.RowIndex and P.ColumnIndex = 13 --predicate
			where	I.LoadID = @LoadID
			
			-- go row by row
			while @current <= @max
			begin
				--load the objects / id's for the focal, source, and target objects
				select	
					@focalObject = FT.Value,
					@focalObjectTypeName = FTN.Value,
					@focalName = F.Value,
					@focalSubject = FS.Value,
					@sourceObject = ST.Value,
					@sourceObjectTypeName = STN.Value,
					@sourceName = S.Value,
					@sourceSubject = SS.Value,
					@targetObject = TT.Value,
					@targetObjectTypeName = TTN.Value,
					@targetName = T.Value,
					@targetSubject = TS.Value,
					@intersectPredicate = P.Value
				from	LoadItem I
						inner join LoadItemColumn FT on FT.LoadID = I.LoadID and FT.RowIndex = I.RowIndex and FT.ColumnIndex = 1  --focal point object type
						inner join LoadItemColumn FTN on FTN.LoadID = I.LoadID and FTN.RowIndex = I.RowIndex and FTN.ColumnIndex = 2  --focal point object type name
						inner join LoadItemColumn FS on FS.LoadID = I.LoadID and FS.RowIndex = I.RowIndex and FS.ColumnIndex = 3 --focal point subject area		
						inner join LoadItemColumn F on F.LoadID = I.LoadID and F.RowIndex = I.RowIndex and F.ColumnIndex = 4 --focal point name		
						inner join LoadItemColumn ST on ST.LoadID = I.LoadID and ST.RowIndex = I.RowIndex and St.ColumnIndex = 5 --source object type
						inner join LoadItemColumn STN on STN.LoadID = I.LoadID and STN.RowIndex = I.RowIndex and StN.ColumnIndex = 6 --source object type name
						inner join LoadItemColumn SS on SS.LoadID = I.LoadID and SS.RowIndex = I.RowIndex and SS.ColumnIndex = 7 --source object subject
						inner join LoadItemColumn S on S.LoadID = I.LoadID and S.RowIndex = I.RowIndex and S.ColumnIndex = 8 --source object name
						inner join LoadItemColumn TT on TT.LoadID = I.LoadID and TT.RowIndex = I.RowIndex and TT.ColumnIndex = 9 --target object type
						inner join LoadItemColumn TTN on TTN.LoadID = I.LoadID and TTN.RowIndex = I.RowIndex and TTN.ColumnIndex = 10 --target object type name
						inner join LoadItemColumn TS on TS.LoadID = I.LoadID and TS.RowIndex = I.RowIndex and TS.ColumnIndex = 11 --target object subject
						inner join LoadItemColumn T on T.LoadID = I.LoadID and T.RowIndex = I.RowIndex and T.ColumnIndex = 12 --source object name
						inner join LoadItemColumn P on P.LoadID = I.LoadID and P.RowIndex = I.RowIndex and P.ColumnIndex = 13 --predicate
				where	I.LoadID = @LoadID and I.RowIndex = @current

				select @focalObjectID = 0, @sourceObjectID = 0, @targetObjectID = 0, @predicateID = 0;

				select @predicateID = id from predicate where name = @intersectPredicate;				

				-- load focal object
				if @focalObject = 'Artifact'
				begin
					select top 1
						@focalObjectID = cod.objectid										
					from 
						[cache].objectdetails cod
						inner join artifact a on (cod.objectid = a.id)
						inner join taxonomytype t on (a.taxonomytypeid = t.id)
					where 
						cod.[object] = @focalObject and cod.textpath = @focalName and cod.objecttypename = @focalObjectTypeName and t.Name = @focalSubject
				end
				else
				begin
					select top 1
							@focalObjectID = cod.objectid										
					from 
						[cache].objectdetails cod
					where 
						cod.[object] = @focalObject and cod.textpath = @focalName and cod.objecttypename = @focalObjectTypeName
				end

				if @sourceObject = 'Artifact'
				begin
					select top 1
						@sourceObjectID = cod.objectid										
					from 
						[cache].objectdetails cod
						inner join artifact a on (cod.objectid = a.id)
						inner join taxonomytype t on (a.taxonomytypeid = t.id)
					where 
						cod.[object] = @sourceObject and cod.textpath = @sourceName and cod.objecttypename = @sourceObjectTypeName and t.Name = @sourceSubject
				end
				else
				begin
					-- load source object
					select top 1
							@sourceObjectID = cod.objectid						
					from 
						[cache].objectdetails cod
					where 
						cod.[object] = @sourceObject and cod.textpath = @sourceName and cod.objecttypename = @sourceObjectTypeName
				end

				if @targetObject = 'Artifact'
				begin
					-- load target object
					select top 1
							@targetObjectID = cod.objectid												
					from 
						[cache].objectdetails cod
						inner join artifact a on (cod.objectid = a.id)
						inner join taxonomytype t on (a.taxonomytypeid = t.id)
					where 
						cod.[object] = @targetObject and cod.textpath = @targetName and cod.objecttypename = @targetObjectTypeName and t.Name = @targetSubject
				end
				else
				begin
					-- load target object
					select top 1
							@targetObjectID = cod.objectid												
					from 
						[cache].objectdetails cod
					where 
						cod.[object] = @targetObject and cod.textpath = @targetName and cod.objecttypename = @targetObjectTypeName
				end

				--debug 
				--select @focalObjectID, @focalObject, @sourceObjectID, @sourceObject, @targetObjectID, @targetObject, @predicateID

				--if all are provided we are good otherwise error
				if @focalObjectID > 0 and @sourceObjectID > 0 and @targetObjectID > 0 and @predicateID > 0
					begin

					-- add intersect between focal object and source if one doesnt exist					
					exec [dbo].[AddRelationship] @UpdatedBy,@rundate,@focalObject,@focalObjectID,2,null,null,@sourceObject,@sourceObjectID;

					-- add intersect between focal object and target if one doesnt exist
					exec [dbo].[AddRelationship] @UpdatedBy,@rundate,@focalObject,@focalObjectID,2,null,null,@targetObject,@targetObjectID;
					
					-- add intersect between source / target if one doesnt exist
					exec [dbo].[AddRelationship] @UpdatedBy,@rundate,@sourceObject,@sourceObjectID,2,null,null,@targetObject,@targetObjectID;

					-- add intersect map between source / target if one doesnt exist for source to target intersect
					if not exists (select 1 from intersectmap map
							inner join intersectnode node1 on ( map.subjectintersectnodeid = node1.id and node1.objectid = @sourceObjectID and node1.objecttype = @sourceObject)
							inner join intersectnode node2 on ( map.objectintersectnodeid = node2.id and node2.objectid = @targetObjectID and node2.objecttype = @targetObject)
						where map.[type] = 1)
						begin							
							insert into intersectmap
								select 
									node1.ID as SubjectIntersectNode,
									node2.ID as ObjectIntersectNode,
									@predicateID as PredicateID,
									1 as [Type]
								from						
									intersectnode node1 
									inner join intersectnode node2 on (node1.objectid = @sourceObjectID and node1.objecttype =@sourceObject and node2.objectid = @targetObjectID and node2.objecttype = @targetObject and node1.intersectid = node2.intersectid);
						end


						update	LoadItem
						set		[Status] = 1,
								StatusMessage = 'Successfully added item to lineage'
						where	LoadID = @LoadID
								and RowIndex = @current
					end -- if valid
				else
					begin
						set @lineageErrorDetailMessage = '';

						if @focalObjectID = 0
						begin
							set @lineageErrorDetailMessage = '  Focal point is invalid.';
						end

						if @sourceObjectID = 0
						begin
							set @lineageErrorDetailMessage = @lineageErrorDetailMessage + '  Source object is invalid.';
						end

						if @targetObjectID = 0
						begin
							set @lineageErrorDetailMessage = @lineageErrorDetailMessage + '  Target object is invalid.';
						end

						update	LoadItem
						set		[Status] = 0,
								StatusMessage = 'Failed to add item to lineage.' + @lineageErrorDetailMessage + ' [focal id:' + convert(varchar(10), @focalObjectID) + ' type:' + @focalObject + '] [source id:' + convert(varchar(10),@sourceObjectID) + ' type:' + @sourceObject +'] [target id:' + convert(varchar(10), @targetObjectID) + ' type:' + @targetObject + ']'
						where	LoadID = @LoadID
								and RowIndex = @current
					end -- else not valid
				
				set @current = @current + 1
			end

		end

		if @Action = 'R' OR @Action = 'U'	--UNRELATION (Remove existing relation)
		begin
			-- PARSE both sides.
			update	T
			set		T.LookupObject = S.LookupObject,
					T.LookupObjectID = S.LookupObjectID
			from	LoadItemColumn T
					inner join	(
								select	IC.LoadID,
										IC.RowIndex,
										IC.ColumnIndex,
										T.[Object] as LookupObject,
										T.ObjectID as LookupObjectID
								from	[Load] L
										inner join [LoadColumn] C on C.LoadID = L.ID and L.ID = @LoadID
										inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
										inner join IntersectTypeNode IT on IT.IntersectTypeID = @ObjectID and IT.[Order] = IC.[ColumnIndex]
										inner join cache.ObjectDetails T on (T.[TextPath] = IC.Value or T.Name = IC.Value) and T.[ObjectType] = IT.[ObjectType] and T.ObjectTypeID = IT.ObjectID
								) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex
			update	T
			set		T.[Status] = 0,
					T.StatusMessage =	REPLACE(REPLACE(
											STUFF(
											(
											select	LIC.Value + ' could not be located in the <a href="' + T.Url + '">' + T.Name + '</a> list, '
											from	[Load] L
													inner join [IntersectTypeNode] ITN on ITN.IntersectTypeID = L.ObjectID and L.ID = @LoadID
													inner join [LoadItemColumn] LIC on LIC.LoadID = L.ID and LIC.ColumnIndex = ITN.[Order] and LIC.ColumnIndex = IC.ColumnIndex and LIC.RowIndex = IC.RowIndex and LIC.LookupObject is null
													inner join cache.ObjectDetails T on T.[Object] = ITN.[ObjectType] and T.ObjectID = ITN.ObjectID
											for xml path('')
											), 1, 0, ''),
										'&lt;', '<'), '&gt;', '>')
			from	[LoadItem] T
					inner join [LoadItemColumn] IC on T.LoadID = @LoadID and IC.LoadID = T.LoadID and IC.RowIndex = T.RowIndex and IC.LookupObject IS NULL and IC.LookupObjectID is null

			select	@current = min(I.RowIndex),
					@max = max(I.RowIndex)
			from	LoadItem I
					inner join LoadItemColumn S on S.LoadID = I.LoadID and S.RowIndex = I.RowIndex and S.ColumnIndex = 1 and S.LookupObject is not null
					inner join LoadItemColumn T on T.LoadID = I.LoadID and T.RowIndex = I.RowIndex and T.ColumnIndex = 2 and T.LookupObject is not null
			where	I.LoadID = @LoadID



		end

		while @current <= @max
		begin
			select	@sourceObject = S.LookupObject,
					@sourceObjectID = S.LookupObjectID,
					@targetObject = T.LookupObject,
					@targetObjectID = T.LookupObjectID
			from	LoadItem I
					inner join LoadItemColumn S on S.LoadID = I.LoadID and S.RowIndex = I.RowIndex and S.ColumnIndex = 1 and S.LookupObject is not null
					inner join LoadItemColumn T on T.LoadID = I.LoadID and T.RowIndex = I.RowIndex and T.ColumnIndex = 2 and T.LookupObject is not null
			where	I.LoadID = @LoadID and I.RowIndex = @current

			set		@intersectID = null

			select	@IntersectID = SN.IntersectID 
			from	[IntersectNode] SN 
					inner join IntersectNode TN on	SN.IntersectID = TN.IntersectID 
													and SN.ID <> TN.ID 
													and SN.ObjectType = @sourceObject 
													and SN.ObjectID = @sourceObjectID 
													and TN.ObjectType = @targetObject 
													and TN.ObjectID = @targetObjectID
			if @Action = 'R'	--RELATION
			begin
				if @intersectID is null
				begin
					-- Get the node type IDs
					select	@sourceIntersectTypeNodeID = S.ID,
							@targetIntersectTypeNodeID = T.ID
					from	IntersectTypeNode S 
							inner join IntersectTypeNode T on S.IntersectTypeID = T.IntersectTypeID and S.[Order] = 1 and T.[Order] = 2 and S.ID <> T.ID and S.IntersectTypeID = @ObjectID

					insert into [Intersect] (IntersectTypeID, Classification, Subject, SubjectID, Object, ObjectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn) 
					values		(@ObjectID, 2, @sourceObject, @sourceObjectID, @targetObject, @targetObjectID, 0, @date, 0, @date)

					set @intersectID = SCOPE_IDENTITY()

					insert into [IntersectNode] (IntersectTypeNodeID, IntersectID, ObjectType, ObjectID) 
					values						(@sourceIntersectTypeNodeID, @intersectID, @sourceObject, @sourceObjectID)
					insert into [IntersectNode] (IntersectTypeNodeID, IntersectID, ObjectType, ObjectID) 
					values						(@targetIntersectTypeNodeID, @intersectID, @targetObject, @targetObjectID)

					exec utility.AddAuditEntry @sourceObject, @sourceObjectID, 0, @date, 'Created', 'Intersect', @intersectID
					exec utility.AddAuditEntry @targetObject, @targetObjectID, 0, @date, 'Created', 'Intersect', @intersectID
				end

				if @intersectID is not null
				begin
					update	LoadItem
					set		[Object] = 'Intersect',
							ObjectID = @intersectID,
							[Status] = 1,
							StatusMessage = 'Successfully created/updated relationship'
					where	LoadID = @LoadID
							and RowIndex = @current
				end
				else
				begin
					update	LoadItem
					set		[Status] = 0,
							StatusMessage = 'Failed to create relationship'
					where	LoadID = @LoadID
							and RowIndex = @current
				end
			end --end R

			if @Action = 'U'	--UNRELATION
			begin
				if @intersectID is not null
				begin
					begin try
						if exists(	select 1 
									from	[cache].[Relationships] SR
											inner join Responsibility RE on RE.ResponsibleObjectType = SR.SourceObject and RE.ResponsibleObjectID = SR.SourceObjectID
											inner join [cache].[Relationships] TR on RE.ObjectType = 'Intersect' and RE.ObjectID = TR.IntersectID and TR.TargetObject = SR.TargetObject and TR.TargetObjectID = SR.TargetObjectID
									where	SR.IntersectID = @intersectID
								 )
						begin
							DECLARE @Targets VARCHAR(8000) 
							SELECT	@Targets = COALESCE(@Targets + ', ', '') + TR.SourceObjectName 
							from	[cache].[Relationships] SR
									inner join Responsibility RE on RE.ResponsibleObjectType = SR.SourceObject and RE.ResponsibleObjectID = SR.SourceObjectID
									inner join [cache].[Relationships] TR on RE.ObjectType = 'Intersect' and RE.ObjectID = TR.IntersectID and TR.TargetObject = SR.TargetObject and TR.TargetObjectID = SR.TargetObjectID
							where	SR.IntersectID = @intersectID

							update	LoadItem
							set		[Object] = 'Intersect',
									ObjectID = @intersectID,
									[Status] = 0,
									StatusMessage = 'Unable to remove relationship as it acts as a source for: ' + @Targets
							where	LoadID = @LoadID
									and RowIndex = @current
						end
						else
						begin
							delete [Intersect] where ID = @intersectID

							update	LoadItem
							set		[Object] = 'Intersect',
									ObjectID = @intersectID,
									[Status] = 1,
									StatusMessage = 'Successfully removed relationship'
							where	LoadID = @LoadID
									and RowIndex = @current
						end
					end try
					begin catch
							update	LoadItem
							set		[Object] = 'Intersect',
									ObjectID = @intersectID,
									[Status] = 0,
									StatusMessage = 'Unable to remove relationship due to the following error: ' + ERROR_MESSAGE()
							where	LoadID = @LoadID
									and RowIndex = @current
					end catch
				end
				else
				begin
					update	LoadItem
					set		[Object] = 'Intersect',
							ObjectID = NULL,
							[Status] = 0,
							StatusMessage = 'Relationship not found'
					where	LoadID = @LoadID
							and RowIndex = @current
				end
			end --end U

			insert into @Intersects values (@intersectID)

			set @current = @current + 1
		end

		if @Action = 'R'
		begin
			exec cache.SynchronizeRelationships @Intersects
		end

	end --end IF statement to check if action = P or NOT

	if @Action = 'P' or @Action = 'R'
	begin
		-- Load custom fields for the inserted/updated object above.
		merge	Field T
		using	(
				select	distinct
						FT.ID as FieldTypeID,
						L.[Object],
						L.ObjectID,
						IC.LookupObjectID--max(IC.LookupObjectID) as LookupObjectID
				from	LoadItem L
						inner join LoadColumn C on C.LoadID = L.LoadID
						inner join LoadItemColumn IC on IC.LoadID = C.LoadID and L.RowIndex = IC.RowIndex and IC.ColumnIndex = C.ColumnIndex and IC.LookupObjectID is not null
						inner join FieldType FT on FT.[Object] = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name
				where	L.ObjectID is not null
						and L.LoadID = @LoadID
				--group by	FT.ID,
				--			L.[Object],
				--			L.ObjectID
				) S
		on		(T.FieldTypeID = S.FieldTypeID and T.ObjectType = S.[Object] and T.ObjectID = S.ObjectID)
		when	matched then
				update	set Value = S.LookupObjectID
		when	not matched then
				insert (ObjectType, ObjectID, FieldTypeID, Value)
				values (S.[Object], S.ObjectID, S.FieldTypeID, S.LookupObjectID);

		merge	Field T
		using	(
				select	distinct
						FT.ID as FieldTypeID,
						L.[Object],
						L.ObjectID,
						case 
							when FT.[Type] = 'Boolean' and LOWER(IC.Value) in ('y', 'yes', 'true', 't', '1') then 'true'
							when FT.[Type] = 'Boolean' and LOWER(IC.Value) not in ('y', 'yes', 'true', 't', '1') then 'false'
							else IC.Value
						end as Value
				from	LoadItem L
						inner join LoadColumn C on C.LoadID = L.LoadID
						inner join LoadItemColumn IC on IC.LoadID = C.LoadID and L.RowIndex = IC.RowIndex and IC.ColumnIndex = C.ColumnIndex and IC.LookupObjectID is null
						inner join FieldType FT on FT.[Object] = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and FT.[Type] <> 'Lookup'
				where	L.ObjectID is not null
						and L.LoadID = @LoadID
				) S
		on		(T.FieldTypeID = S.FieldTypeID and T.ObjectType = S.[Object] and T.ObjectID = S.ObjectID)
		when	matched then
				update	set Value = S.Value
		when	not matched then
				insert (ObjectType, ObjectID, FieldTypeID, Value)
				values (S.[Object], S.ObjectID, S.FieldTypeID, S.Value);
	end

	update	[Load] 
	set		DateCompleted = getutcdate()
	where	ID = @LoadID
end
GO


ALTER PROCEDURE [utility].[PromoteFusionAttributes]
AS
BEGIN
	SET NOCOUNT ON;

	declare @d datetime = getutcdate(),
			@r int = 0,
			@RuleID int,
			@PromotionObjectType varchar(50),
			@PromotionObjectID int,
			@PromotionParentObjectType varchar(50),
			@PromotionParentObjectID int,
			@FusionID int,
			@FusionAttributeID int,
			@ExecutionID int,
			@NumberOfRules int,			
			@NumberOfNewTaxonomies int,
			@NumberOfNewDomainItems int,
			@NumberOfNewDomains int,
			@NumberOfNewArtifacts int,
			@NumberOfAttributesTotal int,
			@NumberOfNewRelations int,
			@promotionNeedsToRun bit
	

	set	@NumberOfRules = 0;	
	set @NumberOfNewTaxonomies = 0;
	set @NumberOfNewDomainItems = 0;
	set @NumberOfNewDomains = 0;
	set @NumberOfNewArtifacts = 0;
	set @promotionNeedsToRun = 0;

	--First check if there is anything to do

	EXEC @promotionNeedsToRun = [utility].[ShouldPromotionRun]

	if(@promotionNeedsToRun <= 0)
	BEGIN
		PRINT 'NO REASON TO RUN THE PROMOTION RULES WAS DETECTED';
		return;
	END;


	--Log this run get a new id from the fusion.promotion table
	insert into [dbo].[FusionAttributePromotionLogSummary] ( DateStarted )
									values ( CURRENT_TIMESTAMP)

	select @ExecutionID =  SCOPE_IDENTITY()

	IF OBJECT_ID('tempdb..#rules') IS NOT NULL
		DROP TABLE #rules;

	create table #rules (
		ID int identity,
		RuleID int,
		FusionID int,
		ObjectType varchar(25),
		ObjectID int,
		PromotionObjectType varchar(25),
		PromotionObjectID int,
		PromotionParentObjectType varchar(25),
		PromotionParentObjectID int,
		FilterFusionAttributeID int,
		FilterFusionAttributeTypeID int
	);

	IF OBJECT_ID('tempdb..#attributes') IS NOT NULL
		DROP TABLE #attributes;

	create table #attributes (
		ID int identity,
		RuleID int,
		FusionAttributeID int
	);

	IF OBJECT_ID('tempdb..#fields') IS NOT NULL
		DROP TABLE #fields;

	create table #fields (
		ID int, 
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
				R.PromotionObjectType,
				R.PromotionObjectID,
				R.PromotionParentObjectType,
				R.PromotionParentObjectID,
				I.FusionAttributeID as FilterFusionAttributeID,
				coalesce(A.FusionAttributeTypeID, R.ObjectID) as FilterFusionAttributeTypeID
		from	FusionAttributePromotionRule R
				inner join FusionAttributePromotionRuleItem I on I.FusionAttributePromotionRuleID = R.ID and R.[Enabled] = 1
				left join FusionAttribute A on A.ID = I.FusionAttributeID

	
	declare	@currentID int,
			@maxID int

	set		@currentID = 1
	select	@maxID = MAX(ID) from #rules

	select @NumberOfRules = count(1) from FusionAttributePromotionRule where [Enabled] = 1;

	while (@currentID <= @maxID)
	begin
		declare @ObjectType varchar(25),
				@ObjectID int,
				@FilterFusionAttributeID int,
				@FilterFusionAttributeTypeID int


		select	@RuleID = RuleID,
				@ObjectType = ObjectType,
				@ObjectID = ObjectID,
				@PromotionObjectType = PromotionObjectType,
				@PromotionObjectID = PromotionObjectID,
				@PromotionParentObjectType = PromotionParentObjectType,
				@PromotionParentObjectID = PromotionParentObjectID,
				@FusionID = FusionID,
				@FilterFusionAttributeID = FilterFusionAttributeID,
				@FilterFusionAttributeTypeID = FilterFusionAttributeTypeID
		from	#rules
		where	ID = @currentID

		if @ObjectID = @FilterFusionAttributeTypeID AND @FilterFusionAttributeID is not null
			begin
				-- You are on a specific nodes of same type.  Just copy to target table.
				insert into #attributes values (@RuleID, @FilterFusionAttributeID)
			end
		else
			begin
				-- You are on an attribute higher up in hierarchy.
				if @FilterFusionAttributeID is null
					begin
						--  If there is NO filtered attribute ID, then you need to get every attribute in system for the partiular fusion instance.
						insert into #attributes
							select	@RuleID, FA.ID 
							from	FusionAttribute FA 
									left join #attributes A on A.FusionAttributeID = FA.ID and A.RuleID = @RuleID and A.ID is null
							where	FA.FusionID = @FusionID 
									and FA.FusionAttributeTypeID = @ObjectID
					end
				else
					begin
						-- If there is a filter attribute ID, then traverse the hierarchy and get all attributes of the specified type.
						with fa as	(
									select	ID,
											ParentID,
											FusionAttributeTypeID
									from	FusionAttribute
									where	ID = @FilterFusionAttributeID
									union all
									select	C.ID,
											C.ParentID,
											C.FusionAttributeTypeID
									from	FusionAttribute C
											inner join fa P on C.ParentID = P.ID --and P.ID <> C.ID
									)
	
						insert into #attributes
							select	@RuleID, fa.ID 
							from	fa 
									left join #attributes A on A.FusionAttributeID = fa.ID and A.RuleID = @RuleID and A.ID is null
							where	fa.FusionAttributeTypeID = @ObjectID
					end
			end

		set @currentID = @currentID + 1
	end


	-- Load field values we are working with, first starting with the Name.
	insert into #fields
		select	A.ID,
				M.SourceFieldName,
				M.SourceFieldTypeID,
				M.TargetFieldName,
				M.TargetFieldTypeID,
				case 
					when M.SourceFieldName = 'Name' then FA.Name					
					when M.IsConstantValue = 1 then M.ConstantValue
				end				
		from	FusionAttributePromotionRuleMapping M
				inner join #attributes A on A.RuleID = M.FusionAttributePromotionRuleID
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

			declare @FusionAttributeTypeID int,
					@PromotedType varchar(50),
					@PromotedID int

			declare @fields table (SourceFieldName nvarchar(250), SourceFieldTypeID int, TargetFieldName nvarchar(250), TargetFieldTypeID int, Value nvarchar(4000))

			select	@RuleID = R.RuleID,
					@FusionID = R.FusionID,
					@FusionAttributeTypeID = R.ObjectID,
					@PromotionObjectType = R.PromotionObjectType,
					@PromotionObjectID = R.PromotionObjectID,
					@PromotionParentObjectType = R.PromotionParentObjectType,
					@PromotionParentObjectID = R.PromotionParentObjectID,
					@FusionAttributeID = A.FusionAttributeID,
					@PromotedType = P.ObjectType,
					@PromotedID = P.ObjectID
			from	#rules R
					inner join #attributes A on A.RuleID = R.RuleID and A.ID = @currentID
					left join FusionAttributePromotion P on P.FusionAttributeID = A.FusionAttributeID and P.FusionAttributePromotionRuleID = R.RuleID

			--Load up fields were are working with for this loop instance.
			insert into @fields
				select SourceFieldName, SourceFieldTypeID, TargetFieldName, TargetFieldTypeID, Value from #fields where ID = @currentID

			if exists(select 1 from @fields where TargetFieldName = 'Name')
				begin
					declare @code nvarchar(50) = null,
							@name nvarchar(250) = null,
							@description nvarchar(4000) = null

					select @code = Value from @fields where TargetFieldName = 'Code'
					select @name = Value from @fields where TargetFieldName = 'Name'
					select @description = coalesce(Value, '') from @fields where TargetFieldName = 'Description'

					if @PromotionObjectType = 'ArtifactType'
						begin
							set @PromotedType = 'Artifact'

							if @PromotedID is null
								begin
									select	@PromotedID = ID
									from	Artifact
									where	ArtifactTypeID = @PromotionObjectID
											and lower(Name) = lower(@name)
								end

							declare @modelTypeID int
							select @modelTypeID = min(ID) from TaxonomyType

							if @PromotedID is null
								begin
									insert into Artifact ( ParentID, ArtifactTypeID, TaxonomyTypeID, Name, Description, Status, UpdatedOn, UpdatedBy )
									values ( @PromotionParentObjectID, @PromotionObjectID, @modelTypeID, @name, @description, 'Draft', getutcdate(), 0 )

									select @PromotedID =  SCOPE_IDENTITY()

									set @NumberOfNewArtifacts = @NumberOfNewArtifacts +1;
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
									where	ID = @PromotedID

									if (@testArtifactName <> @name) 
										OR (@testArtifactDescription <> @description) 
										OR (@testArtifactParentID <> @PromotionParentObjectID) 
										--OR (@testArtifactTaxonomyTypeID <> @modelTypeID)
									begin
										update	Artifact
										set		Name = @name,
												Description = @description,
												ParentID = @PromotionParentObjectID--,
												--TaxonomyTypeID = @modelTypeID
										where	ID = @PromotedID
									end
								end
						end
 
					if @PromotionObjectType = 'DomainType'
						begin
							if @PromotionParentObjectType is null and @PromotionParentObjectID is null
								begin
									set @PromotedType = 'Domain'
									
									-- You are promoting to a Domain (creating a list)
									if @PromotedID is null
										begin
											select	@PromotedID = ID
											from	Domain
											where	DomainTypeID = @PromotionObjectID
													and lower(Name) = lower(@name)
										end
 
									if @PromotedID is null
										begin
											insert into Domain  ( DomainTypeID, Name, Description ) 
											values ( @PromotionObjectID, @name, @description )

											select @PromotedID =  SCOPE_IDENTITY()

											set @NumberOfNewDomains = @NumberOfNewDomains +1;
										end
									else
										begin
											update	Domain
											set		Name = @name,
													Description = @description
											where	ID = @PromotedID
										end
								end
							else
								begin
									-- You are promoting domain items to a specific domain (list)
									set @PromotedType = 'DomainItem'

									if @PromotedType is null and @PromotedID is null
										begin
											select	@PromotedID = ID
											from	DomainItem
											where	DomainID = @PromotionParentObjectID
													and lower(Code) = lower(@code)
										end
 
									if @PromotedID is not null
										begin
											update	DomainItem
											set		Name = @name,
													Code = coalesce(@code, @name),
													Description = @description
											where	ID = @PromotedID
										end
									else
										begin
											insert into DomainItem ( DomainID, Name, Code, Description )
											values ( @PromotionParentObjectID, @name, coalesce(@code, @name), @description )

											select @PromotedID =  SCOPE_IDENTITY()

											set @NumberOfNewDomainItems = @NumberOfNewDomainItems +1;
										end
								end
						end

					if @PromotionObjectType = 'TaxonomyType'
						begin
							set @PromotedType = 'Taxonomy'

							if @PromotedID is null
								begin
									select	@PromotedID = ID
									from	Taxonomy
									where	TaxonomyTypeID = @PromotionObjectID
											and ParentID = @PromotionParentObjectID
											and lower(Name) = lower(@name)
								end

							if @PromotedID is null
								begin
									insert into Taxonomy	( ParentID, TaxonomyTypeID, Name, Description )
									values					( @PromotionParentObjectID, @PromotionObjectID, @name, @description )

									select @PromotedID =  SCOPE_IDENTITY()

									set @NumberOfNewTaxonomies = @NumberOfNewTaxonomies +1;
								end
							else
								begin
									update	Taxonomy
									set		Name = @Name,
											Description = @Description--,
											--ParentID = @PromotionParentObjectID
									where	ID = @PromotedID
 								end
						end

					-- Add/Update the promotion record to keep track of the auto-promotions
					if @PromotedType is not null and @PromotedID is not null
						begin
							-- Insert/Update the FusionAttributePromotion table to keep track of previously promoted objects.
							
							MERGE	FusionAttributePromotion AS T
							USING	(
									SELECT	@FusionAttributeID as FusionAttributeID, 
											@PromotedType as ObjectType, 
											@PromotedID as ObjectID, 
											@RuleID as RuleID,
											@PromotionObjectID as PromotedObjectTypeID
									) as S
							ON		T.FusionAttributeID = S.FusionAttributeID 
									and T.ObjectType = S.ObjectType 
									and T.ObjectID = S.ObjectID
							WHEN	MATCHED THEN
									UPDATE SET T.FusionAttributePromotionRuleID = S.RuleID, ObjectTypeID = S.PromotedObjectTypeID
							WHEN	NOT MATCHED THEN
									INSERT (FusionAttributeID, ObjectType, ObjectID, FusionAttributePromotionRuleID, ObjectTypeID) 
									VALUES (S.FusionAttributeID, S.ObjectType, S.ObjectID, S.RuleID, S.PromotedObjectTypeID);
						end

					-- Add/Update the dynamic fields involved.
					if @PromotedType is not null and @PromotedID is not null
						begin
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
											if @lookupObjectType = 'Domain'
												begin
													select	top 1
															@objectResultID = ID
													from	DomainItem
													where	DomainID = @lookupObjectID and Name = @fieldValue
												end
											if @lookupObjectType = 'Lookup'
												begin
													select	top 1
															@objectResultID = L.ID
													from	[Lookup] L
															inner join Field F on F.ObjectType = @lookupObjectType and F.ObjectID = L.ID and L.LookupTypeID = @lookupObjectID and F.FieldTypeID = @targetFieldTypeID and F.FormattedValue = @fieldValue
												end
											
											if @PromotedID is not null and @objectResultID is not null
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
											If not EXISTS (SELECT 1 FROM #fieldValues where ObjectType = @PromotedType and ObjectID = @PromotedID and FieldTypeID = @targetFieldTypeID) --avoid duplicates this happens in gmo
											begin
												insert into #fieldValues (ObjectType, ObjectID, FieldTypeID, Value) values(@PromotedType, @PromotedID, @targetFieldTypeID, @fieldValue)
											end
										end
						
									-- Delete the field we just finished processing.
									delete @fields where TargetFieldTypeID = @targetFieldTypeID
								end 
						end
				end -- Check to see if Target Field called NAME is present
								
		end try
		begin catch
			--SELECT 
				--ERROR_NUMBER() AS ErrorNumber
				--,ERROR_MESSAGE() AS ErrorMessage;
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
					select f.ObjectType as ObjectType,
							f.ObjectID as ObjectID,
							f.FieldTypeID as FieldTypeID,
							f.Value as Value
					from #fieldValues f inner join dbo.FieldType ft on (ft.ID = f.FieldTypeID)
				) as S
				on		T.ObjectType = S.ObjectType and T.ObjectID = S.ObjectID and T.FieldTypeID = S.FieldTypeID
				when	matched then
					update set T.Value = S.Value
				when	not matched then
					insert (ObjectTYpe, OBjectID, FieldTypeID, Value)
					values (S.ObjectType, S.ObjectID, S.FieldTypeID, S.Value);
	end

	-- Add new relations as needed
	--exec [utility].[PromoteFusionAttributesRelations] @NumberOfNewRelations output

	-- Handle any fusionlookup fields
	--exec [utility].[PromoteFusionAttributeLookups]
	
		
	--Log this run done
	update [dbo].[FusionAttributePromotionLogSummary]
	set DateCompleted = CURRENT_TIMESTAMP, 
		[PromotedTaxonomies] = @NumberOfNewTaxonomies, 
		[PromotedDomainItems] = @NumberOfNewDomainItems,  
		[PromotedDomains] = @NumberOfNewDomains,
		[PromotedArtifacts] = @NumberOfNewArtifacts,
		[TotalNewPromotions] = (@NumberOfNewTaxonomies + @NumberOfNewDomainItems + @NumberOfNewDomains + @NumberOfNewArtifacts),
		[AttributesConsidered]= @NumberOfAttributesTotal,
		[NumberOfRules] = @NumberOfRules ,
		[RelationshipsAdded] = @NumberOfNewRelations
	where ID = @ExecutionID;
	
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

	IF (@Type = 'Domain')
	BEGIN
		WITH H (ParentID, ID, [level])
		AS
		(
			SELECT	ParentID, 
					ID, 
					1
			FROM	Domain
			WHERE	ID = @ID		
			UNION ALL
			SELECT	P.ParentID, 
					P.ID, 
					C.[level] + 1
			FROM	Domain	P
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

CREATE TABLE [dbo].[DomainClassification] (
    [ID]   INT          IDENTITY (1, 1) NOT NULL,
    [Name] VARCHAR (50) NOT NULL
);
GO

insert into [DomainClassification] values ('Internal')
insert into [DomainClassification] values ('House')
insert into [DomainClassification] values ('Other')
go

CREATE TABLE [dbo].[DomainItemXref] (
    [ID]                INT IDENTITY (1, 1) NOT NULL,
    [HouseDomainItemID] INT NOT NULL,
    [DomainItemID]      INT NOT NULL,
    [LanguageID]        INT NULL,
    CONSTRAINT [PK_DomainItemXref] PRIMARY KEY CLUSTERED ([ID] ASC),
    FOREIGN KEY ([DomainItemID]) REFERENCES [dbo].[DomainItem] ([ID]),
    FOREIGN KEY ([DomainItemID]) REFERENCES [dbo].[DomainItem] ([ID]),
    FOREIGN KEY ([HouseDomainItemID]) REFERENCES [dbo].[DomainItem] ([ID]),
    FOREIGN KEY ([HouseDomainItemID]) REFERENCES [dbo].[DomainItem] ([ID])
);
GO

CREATE TABLE [dbo].[DomainSourceType] (
    [ArtifactTypeID] INT NOT NULL,
    CONSTRAINT [FK_DomainSourceType_ArtifactType] FOREIGN KEY ([ArtifactTypeID]) REFERENCES [dbo].[ArtifactType] ([ID])
);
GO

--select * from [DomainSourceType]

CREATE TABLE [dbo].[Language] (
    [ID]      INT            IDENTITY (1, 1) NOT NULL,
    [Name]    NVARCHAR (250) NOT NULL,
    [Alpha2]  VARCHAR (2)    NOT NULL,
    [Alpha3b] VARCHAR (3)    NOT NULL
);
GO


--select 'insert into [Language] values(''' + Name + ''', ''' + Alpha2 + ''', ''' + Alpha3b + ''')' from [Language]

insert into [Language] values('Afar', 'aa', 'aar')
insert into [Language] values('Abkhazian', 'ab', 'abk')
insert into [Language] values('Afrikaans', 'af', 'afr')
insert into [Language] values('Akan', 'ak', 'aka')
insert into [Language] values('Albanian', 'sq', 'alb')
insert into [Language] values('Amharic', 'am', 'amh')
insert into [Language] values('Arabic', 'ar', 'ara')
insert into [Language] values('Aragonese', 'an', 'arg')
insert into [Language] values('Armenian', 'hy', 'arm')
insert into [Language] values('Assamese', 'as', 'asm')
insert into [Language] values('Avaric', 'av', 'ava')
insert into [Language] values('Avestan', 'ae', 'ave')
insert into [Language] values('Aymara', 'ay', 'aym')
insert into [Language] values('Azerbaijani', 'az', 'aze')
insert into [Language] values('Bashkir', 'ba', 'bak')
insert into [Language] values('Bambara', 'bm', 'bam')
insert into [Language] values('Basque', 'eu', 'baq')
insert into [Language] values('Belarusian', 'be', 'bel')
insert into [Language] values('Bengali', 'bn', 'ben')
insert into [Language] values('Bihari languages', 'bh', 'bih')
insert into [Language] values('Bislama', 'bi', 'bis')
insert into [Language] values('Bosnian', 'bs', 'bos')
insert into [Language] values('Breton', 'br', 'bre')
insert into [Language] values('Bulgarian', 'bg', 'bul')
insert into [Language] values('Burmese', 'my', 'bur')
insert into [Language] values('Catalan; Valencian', 'ca', 'cat')
insert into [Language] values('Chamorro', 'ch', 'cha')
insert into [Language] values('Chechen', 'ce', 'che')
insert into [Language] values('Chinese', 'zh', 'chi')
insert into [Language] values('Church Slavic; Old Slavonic; Church Slavonic; Old Bulgarian; Old Church Slavonic', 'cu', 'chu')
insert into [Language] values('Chuvash', 'cv', 'chv')
insert into [Language] values('Cornish', 'kw', 'cor')
insert into [Language] values('Corsican', 'co', 'cos')
insert into [Language] values('Cree', 'cr', 'cre')
insert into [Language] values('Czech', 'cs', 'cze')
insert into [Language] values('Danish', 'da', 'dan')
insert into [Language] values('Divehi; Dhivehi; Maldivian', 'dv', 'div')
insert into [Language] values('Dutch; Flemish', 'nl', 'dut')
insert into [Language] values('Dzongkha', 'dz', 'dzo')
insert into [Language] values('English', 'en', 'eng')
insert into [Language] values('Esperanto', 'eo', 'epo')
insert into [Language] values('Estonian', 'et', 'est')
insert into [Language] values('Ewe', 'ee', 'ewe')
insert into [Language] values('Faroese', 'fo', 'fao')
insert into [Language] values('Fijian', 'fj', 'fij')
insert into [Language] values('Finnish', 'fi', 'fin')
insert into [Language] values('French', 'fr', 'fre')
insert into [Language] values('Western Frisian', 'fy', 'fry')
insert into [Language] values('Fulah', 'ff', 'ful')
insert into [Language] values('Georgian', 'ka', 'geo')
insert into [Language] values('German', 'de', 'ger')
insert into [Language] values('Gaelic; Scottish Gaelic', 'gd', 'gla')
insert into [Language] values('Irish', 'ga', 'gle')
insert into [Language] values('Galician', 'gl', 'glg')
insert into [Language] values('Manx', 'gv', 'glv')
insert into [Language] values('Greek, Modern (1453-)', 'el', 'gre')
insert into [Language] values('Guarani', 'gn', 'grn')
insert into [Language] values('Gujarati', 'gu', 'guj')
insert into [Language] values('Haitian; Haitian Creole', 'ht', 'hat')
insert into [Language] values('Hausa', 'ha', 'hau')
insert into [Language] values('Hebrew', 'he', 'heb')
insert into [Language] values('Herero', 'hz', 'her')
insert into [Language] values('Hindi', 'hi', 'hin')
insert into [Language] values('Hiri Motu', 'ho', 'hmo')
insert into [Language] values('Croatian', 'hr', 'hrv')
insert into [Language] values('Hungarian', 'hu', 'hun')
insert into [Language] values('Igbo', 'ig', 'ibo')
insert into [Language] values('Icelandic', 'is', 'ice')
insert into [Language] values('Ido', 'io', 'ido')
insert into [Language] values('Sichuan Yi; Nuosu', 'ii', 'iii')
insert into [Language] values('Inuktitut', 'iu', 'iku')
insert into [Language] values('Interlingue; Occidental', 'ie', 'ile')
insert into [Language] values('Interlingua (International Auxiliary Language Association)', 'ia', 'ina')
insert into [Language] values('Indonesian', 'id', 'ind')
insert into [Language] values('Inupiaq', 'ik', 'ipk')
insert into [Language] values('Italian', 'it', 'ita')
insert into [Language] values('Javanese', 'jv', 'jav')
insert into [Language] values('Japanese', 'ja', 'jpn')
insert into [Language] values('Kalaallisut; Greenlandic', 'kl', 'kal')
insert into [Language] values('Kannada', 'kn', 'kan')
insert into [Language] values('Kashmiri', 'ks', 'kas')
insert into [Language] values('Kanuri', 'kr', 'kau')
insert into [Language] values('Kazakh', 'kk', 'kaz')
insert into [Language] values('Central Khmer', 'km', 'khm')
insert into [Language] values('Kikuyu; Gikuyu', 'ki', 'kik')
insert into [Language] values('Kinyarwanda', 'rw', 'kin')
insert into [Language] values('Kirghiz; Kyrgyz', 'ky', 'kir')
insert into [Language] values('Komi', 'kv', 'kom')
insert into [Language] values('Kongo', 'kg', 'kon')
insert into [Language] values('Korean', 'ko', 'kor')
insert into [Language] values('Kuanyama; Kwanyama', 'kj', 'kua')
insert into [Language] values('Kurdish', 'ku', 'kur')
insert into [Language] values('Lao', 'lo', 'lao')
insert into [Language] values('Latin', 'la', 'lat')
insert into [Language] values('Latvian', 'lv', 'lav')
insert into [Language] values('Limburgan; Limburger; Limburgish', 'li', 'lim')
insert into [Language] values('Lingala', 'ln', 'lin')
insert into [Language] values('Lithuanian', 'lt', 'lit')
insert into [Language] values('Luxembourgish; Letzeburgesch', 'lb', 'ltz')
insert into [Language] values('Luba-Katanga', 'lu', 'lub')
insert into [Language] values('Ganda', 'lg', 'lug')
insert into [Language] values('Macedonian', 'mk', 'mac')
insert into [Language] values('Marshallese', 'mh', 'mah')
insert into [Language] values('Malayalam', 'ml', 'mal')
insert into [Language] values('Maori', 'mi', 'mao')
insert into [Language] values('Marathi', 'mr', 'mar')
insert into [Language] values('Malay', 'ms', 'may')
insert into [Language] values('Malagasy', 'mg', 'mlg')
insert into [Language] values('Maltese', 'mt', 'mlt')
insert into [Language] values('Mongolian', 'mn', 'mon')
insert into [Language] values('Nauru', 'na', 'nau')
insert into [Language] values('Navajo; Navaho', 'nv', 'nav')
insert into [Language] values('Ndebele, South; South Ndebele', 'nr', 'nbl')
insert into [Language] values('Ndebele, North; North Ndebele', 'nd', 'nde')
insert into [Language] values('Ndonga', 'ng', 'ndo')
insert into [Language] values('Nepali', 'ne', 'nep')
insert into [Language] values('Norwegian Nynorsk; Nynorsk, Norwegian', 'nn', 'nno')
insert into [Language] values('Bokmål, Norwegian; Norwegian Bokmål', 'nb', 'nob')
insert into [Language] values('Norwegian', 'no', 'nor')
insert into [Language] values('Chichewa; Chewa; Nyanja', 'ny', 'nya')
insert into [Language] values('Occitan (post 1500); Provençal', 'oc', 'oci')
insert into [Language] values('Ojibwa', 'oj', 'oji')
insert into [Language] values('Oriya', 'or', 'ori')
insert into [Language] values('Oromo', 'om', 'orm')
insert into [Language] values('Ossetian; Ossetic', 'os', 'oss')
insert into [Language] values('Panjabi; Punjabi', 'pa', 'pan')
insert into [Language] values('Persian', 'fa', 'per')
insert into [Language] values('Pali', 'pi', 'pli')
insert into [Language] values('Polish', 'pl', 'pol')
insert into [Language] values('Portuguese', 'pt', 'por')
insert into [Language] values('Pushto; Pashto', 'ps', 'pus')
insert into [Language] values('Quechua', 'qu', 'que')
insert into [Language] values('Romansh', 'rm', 'roh')
insert into [Language] values('Romanian; Moldavian; Moldovan', 'ro', 'rum')
insert into [Language] values('Rundi', 'rn', 'run')
insert into [Language] values('Russian', 'ru', 'rus')
insert into [Language] values('Sango', 'sg', 'sag')
insert into [Language] values('Sanskrit', 'sa', 'san')
insert into [Language] values('Sinhala; Sinhalese', 'si', 'sin')
insert into [Language] values('Slovak', 'sk', 'slo')
insert into [Language] values('Slovenian', 'sl', 'slv')
insert into [Language] values('Northern Sami', 'se', 'sme')
insert into [Language] values('Samoan', 'sm', 'smo')
insert into [Language] values('Shona', 'sn', 'sna')
insert into [Language] values('Sindhi', 'sd', 'snd')
insert into [Language] values('Somali', 'so', 'som')
insert into [Language] values('Sotho, Southern', 'st', 'sot')
insert into [Language] values('Spanish; Castilian', 'es', 'spa')
insert into [Language] values('Sardinian', 'sc', 'srd')
insert into [Language] values('Serbian', 'sr', 'srp')
insert into [Language] values('Swati', 'ss', 'ssw')
insert into [Language] values('Sundanese', 'su', 'sun')
insert into [Language] values('Swahili', 'sw', 'swa')
insert into [Language] values('Swedish', 'sv', 'swe')
insert into [Language] values('Tahitian', 'ty', 'tah')
insert into [Language] values('Tamil', 'ta', 'tam')
insert into [Language] values('Tatar', 'tt', 'tat')
insert into [Language] values('Telugu', 'te', 'tel')
insert into [Language] values('Tajik', 'tg', 'tgk')
insert into [Language] values('Tagalog', 'tl', 'tgl')
insert into [Language] values('Thai', 'th', 'tha')
insert into [Language] values('Tibetan', 'bo', 'tib')
insert into [Language] values('Tigrinya', 'ti', 'tir')
insert into [Language] values('Tonga (Tonga Islands)', 'to', 'ton')
insert into [Language] values('Tswana', 'tn', 'tsn')
insert into [Language] values('Tsonga', 'ts', 'tso')
insert into [Language] values('Turkmen', 'tk', 'tuk')
insert into [Language] values('Turkish', 'tr', 'tur')
insert into [Language] values('Twi', 'tw', 'twi')
insert into [Language] values('Uighur; Uyghur', 'ug', 'uig')
insert into [Language] values('Ukrainian', 'uk', 'ukr')
insert into [Language] values('Urdu', 'ur', 'urd')
insert into [Language] values('Uzbek', 'uz', 'uzb')
insert into [Language] values('Venda', 've', 'ven')
insert into [Language] values('Vietnamese', 'vi', 'vie')
insert into [Language] values('Volapük', 'vo', 'vol')
insert into [Language] values('Welsh', 'cy', 'wel')
insert into [Language] values('Walloon', 'wa', 'wln')
insert into [Language] values('Wolof', 'wo', 'wol')
insert into [Language] values('Xhosa', 'xh', 'xho')
insert into [Language] values('Yiddish', 'yi', 'yid')
insert into [Language] values('Yoruba', 'yo', 'yor')
insert into [Language] values('Zhuang; Chuang', 'za', 'zha')
insert into [Language] values('Zulu', 'zu', 'zul')
go

CREATE VIEW [dbo].[WorkflowChallenge]
AS
select		W.ID as WorkflowID
		    ,W.Data.value('(fields/CommentID)[1]', 'int') as CommentID
			,W.Data.value('(fields/RequestingResourceID)[1]', 'int') as CreatingResourceID
			,W.Data.value('(fields/ArtifactTypeID)[1]', 'int') as ArtifactTypeID
			,W.Data.value('(fields/ArtifactTypeName)[1]', 'nvarchar(250)') as ArtifactTypeName
			,W.Data.value('(fields/ArtifactID)[1]', 'int') as ArtifactID
			,W.Data.value('(fields/Name)[1]', 'nvarchar(250)') as Name
			,'#/artifacts/' + cast(W.Data.value('(fields/ArtifactTypeID)[1]', 'int') as varchar) + '/' + cast(W.Data.value('(fields/ArtifactID)[1]', 'int') as varchar) as Url
			,W.DateStarted
			,W.DateCompleted	
			,W.Step						
			,R.FirstName + ' ' + R.LastName as RaisedBy			
			,case when w.dateCompleted is null then cast(0 as bit) else cast(1 as bit) end as IsCompleted	
			,ws.Data.value('(fields/Approved)[1]', 'bit') as Approved		
			,ws.Data.value('(fields/Note)[1]', 'nvarchar(500)') as ClosingNotes
			,R_a.FirstName + ' ' + R_a.LastName as ClosedBy			
			,ws.Data.value('(fields/ApproverResourceID)[1]', 'int') as ClosedByResourceID
from	    Workflow W		
			inner join Comment C on C.ID = W.Data.value('(fields/CommentID)[1]', 'int')
			inner join CommentRelation CR on CR.CommentID = C.ID and CR.ObjectType not in ('Resource', 'Group')			
			left outer join reporting.Global_Resource R on R.ResourceID = W.Data.value('(fields/RequestingResourceID)[1]', 'int')
			left outer join workflowstatus ws on w.id = ws.workflowid and ws.activityname = 'Read Approval'
			left outer join reporting.Global_Resource R_a on R_a.ResourceID = ws.Data.value('(fields/ApproverResourceID)[1]', 'int')
            where  W.WorkflowType = 4
GO

CREATE VIEW [dbo].[WorkflowIssue]
AS
select		W.ID as WorkflowID
		    ,W.Data.value('(fields/CommentID)[1]', 'int') as CommentID
			,W.Data.value('(fields/ResourceID)[1]', 'int') as CreatingResourceID
			,W.DateStarted
			,W.DateCompleted	
			,W.Step
			,A.ObjectID
			,A.Name
			,A.[Object]
			,A.Url
			,R.FirstName + ' ' + R.LastName as RaisedBy			
			,case when w.dateCompleted is null then cast(0 as bit) else cast(1 as bit) end as IsCompleted	
			,ws.data.value('(fields/Comment)[1]','nvarchar(500)') as Comments					
from	    Workflow W		
			inner join Comment C on C.ID = W.Data.value('(fields/CommentID)[1]', 'int')
			left outer join CommentRelation CR on CR.CommentID = C.ID and CR.ObjectType not in ('Resource', 'Group')
			left outer join workflowstatus ws on w.id = ws.workflowid and ws.recordnumber = 7
			left outer join cache.ObjectDetails A on A.[Object] = CR.ObjectType and A.ObjectID = CR.ObjectID            		
			left outer join reporting.Global_Resource R on R.ResourceID = W.Data.value('(fields/ResourceID)[1]', 'int')			
            where  W.WorkflowType = 3
GO
--Pappas: Added above on 05/17/2016


--Pappas: Added below on 06/17/2016
alter table FieldTypeRelationLookupDisplayField add Show bit not null constraint DF_FieldTypeRelationLookupDisplayField_Show default(1)
alter table FieldTypeRelationLookupDisplayField add SortOrder int null 
alter table FieldTypeRelationLookupDisplayField add FilterValue nvarchar(250) null 
go
--Pappas: Added above on 06/17/2016