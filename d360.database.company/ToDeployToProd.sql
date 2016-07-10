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
CREATE NONCLUSTERED INDEX [IX_FusionAttribute_FusionID_TextPath] ON [dbo].[FusionAttribute]([FusionID] ASC, [TextPath] ASC);
GO
--Pappas:  Added above on 04/27/16


--Pappas: Added below on 05/17/2016
drop table utility.ApiError
go
drop table EventType
go
drop view LookupAllocation
go
drop function utility.GetRootEventTypeID
go
drop procedure AddMapRelationship
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

CREATE TRIGGER [dbo].[Intersect_AfterInsert]
	ON [dbo].[Intersect]
	FOR INSERT
AS
BEGIN
	SET NOCOUNT ON;

	declare @tbl table(ID int identity, IntersectID int, ResourceID int, Subject varchar(50), SubjectID int, Object varchar(50), ObjectID int)
	insert into @tbl
		select ID, UpdatedBy, Subject, SubjectID, Object, ObjectID from inserted;

	declare @current int = 1,
			@max int,
			@id int,
			@r int,
			@s varchar(50),
			@sid int,
			@o varchar(50),
			@oid int,
			@date datetime = getutcdate()

	select @max =max(ID) from @tbl

	while @current <= @max
	begin
		select	@id = IntersectID,
				@r = ResourceID,
				@s = coalesce(Subject, 'Intersect'),
				@sid = coalesce(SubjectID, IntersectID),
				@o = coalesce(Object, 'Intersect'),
				@oid = coalesce(ObjectID, IntersectID)
		from	@tbl
		where	ID = @current

		exec [cache].[SynchronizeObjectDetails] 'Intersect', @id
		exec [utility].[AddAuditEntry] @s, @sid, @r, @date, 'Created', 'Intersect', @id
		exec [utility].[AddAuditEntry] @o, @oid, @r, @date, 'Created', 'Intersect', @id

		exec cache.SynchronizeResponsibilitiesForObject @s, @sid
		--exec cache.SynchronizeResponsibilitiesForObject @o, @oid

		merge cache.Relationship as T
		using (
				select	distinct
						S.IntersectID,
						S.IntersectTypeNodeID as SourceIntersectTypeNodeID, 
						S.ID as SourceIntersectNodeID,
						S.ObjectType as SourceObject,
						S.ObjectID as SourceObjectID,
						T.IntersectTypeNodeID as TargetIntersectTypeNodeID,
						T.ID as TargetIntersectNodeID,
						T.ObjectType as TargetObject,
						T.ObjectID as TargetObjectID
				from	dbo.IntersectNode S
						inner join dbo.IntersectNode T on T.IntersectID = S.IntersectID and T.ID <> S.ID
				where	S.IntersectID = @id
				) as S (
					IntersectID, 
					SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, 
					TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID
					)
		on    (T.IntersectID = S.IntersectID and T.SourceObject = S.SourceObject and T.SourceObjectID = S.SourceObjectID)
		when not matched then
			insert (
					IntersectID, 
					SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, 
					TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID
					)
			values (
					S.IntersectID, 
					S.SourceIntersectTypeNodeID, S.SourceIntersectNodeID, S.SourceObject, S.SourceObjectID, 
					S.TargetIntersectTypeNodeID, S.TargetIntersectNodeID, S.TargetObject, S.TargetObjectID
					);

		set @current = @current +1
	end;
END
GO

CREATE TRIGGER [dbo].[Intersect_AfterUpdate]
	ON [dbo].[Intersect]
	FOR UPDATE
AS
BEGIN
	SET NOCOUNT ON;

	declare @tbl table(ID int identity, IntersectID int, ResourceID int, Subject varchar(50), SubjectID int, Object varchar(50), ObjectID int)
	insert into @tbl
		select ID, UpdatedBy, Subject, SubjectID, Object, ObjectID from inserted;

	declare @current int = 1,
			@max int,
			@id int,
			@r int,
			@s varchar(50),
			@sid int,
			@o varchar(50),
			@oid int,
			@date datetime = getutcdate()

	select @max =max(ID) from @tbl

	while @current <= @max
	begin
		select	@id = IntersectID,
				@r = ResourceID,
				@s = coalesce(Subject, 'Intersect'),
				@sid = coalesce(SubjectID, IntersectID),
				@o = coalesce(Object, 'Intersect'),
				@oid = coalesce(ObjectID, IntersectID)
		from	@tbl
		where	ID = @current

		exec [cache].[SynchronizeObjectDetails] 'Intersect', @id
		exec [utility].[AddAuditEntry] @s, @sid, @r, @date, 'Updated', 'Intersect', @id
		exec [utility].[AddAuditEntry] @o, @oid, @r, @date, 'Updated', 'Intersect', @id

		merge cache.Relationship as T
		using (
				select	distinct
						S.IntersectID,
						S.IntersectTypeNodeID as SourceIntersectTypeNodeID, 
						S.ID as SourceIntersectNodeID,
						S.ObjectType as SourceObject,
						S.ObjectID as SourceObjectID,
						T.IntersectTypeNodeID as TargetIntersectTypeNodeID,
						T.ID as TargetIntersectNodeID,
						T.ObjectType as TargetObject,
						T.ObjectID as TargetObjectID
				from	dbo.IntersectNode S
						inner join dbo.IntersectNode T on T.IntersectID = S.IntersectID and T.ID <> S.ID
				where	S.IntersectID = @id
				) as S (
					IntersectID, 
					SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, 
					TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID
					)
		on    (T.IntersectID = S.IntersectID and T.SourceObject = S.SourceObject and T.SourceObjectID = S.SourceObjectID)
		when not matched then
			insert (
					IntersectID, 
					SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, 
					TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID
					)
			values (
					S.IntersectID, 
					S.SourceIntersectTypeNodeID, S.SourceIntersectNodeID, S.SourceObject, S.SourceObjectID, 
					S.TargetIntersectTypeNodeID, S.TargetIntersectNodeID, S.TargetObject, S.TargetObjectID
					);

		set @current = @current +1
	end;
END
GO


ALTER TABLE [dbo].[IntersectTypePredicate] DROP CONSTRAINT [FK_IntersectTypePredicate_IntersectType]
GO

ALTER TABLE [dbo].[IntersectTypePredicate]  WITH CHECK ADD  CONSTRAINT [FK_IntersectTypePredicate_IntersectType] FOREIGN KEY([IntersectTypeID])
REFERENCES [dbo].[IntersectType] ([ID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[IntersectTypePredicate] CHECK CONSTRAINT [FK_IntersectTypePredicate_IntersectType]
GO

ALTER TABLE Report ADD [ReportType] VARCHAR (25) CONSTRAINT [DF_Report_ReportType] DEFAULT ('legacy') NOT NULL
GO
ALTER TABLE Report ADD [PowerBIDatasetID] VARCHAR (50) NULL
GO
ALTER TABLE Report ADD [PowerBIReportID]  VARCHAR (50) NULL
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
--Pappas: Added above on 05/17/2016


--Pappas: Added below on 06/17/2016
alter table FieldTypeRelationLookupDisplayField add Show bit not null constraint DF_FieldTypeRelationLookupDisplayField_Show default(1)
alter table FieldTypeRelationLookupDisplayField add SortOrder int null 
alter table FieldTypeRelationLookupDisplayField add FilterValue nvarchar(250) null 
go
--Pappas: Added above on 06/17/2016


ALTER TRIGGER [dbo].[Artifact_AfterDelete]
	ON [dbo].[Artifact]
	AFTER DELETE
AS
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Delete', [queue].WriteIndexXml('Removed', 'ArtifactType', ArtifactTypeID, coalesce(UpdatedBy, 0)), 'Artifact', ID from deleted;

	insert into reporting.Global_Audit (Object, ObjectID, ObjectName, ResourceID, Date, Action, ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, ActionDescription)
		select 'Artifact', O.ID, O.TextPath, coalesce(O.UpdatedBy, 0), coalesce(O.UpdatedOn, getutcdate()), 'Deleted', 'Artifact', O.ID, T.Name, O.TextPath, 'This artifact has been removed.' from deleted O inner join ArtifactType T on T.ID = O.ArtifactTypeID;
GO

CREATE TRIGGER [dbo].[Artifact_AfterUpsert]
   ON  [dbo].[Artifact] 
   AFTER INSERT, UPDATE
AS 
	SET NOCOUNT ON;
	declare @ot varchar(50) = 'Artifact'

	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select	case 
					when D.ID is not null then 'Update'
					else 'Add'
				end, 
				[queue].WriteIndexXml('', @ot, I.ID, coalesce(I.UpdatedBy, 0)), 
				@ot, 
				I.ID 
		from	inserted I
				left join deleted D on D.ID = I.ID;
	
	with S as	(
				select	ID,
						ParentID
				from	inserted
				union all
				select	A.ID,
						A.ParentID
				from	Artifact A
						inner join S on S.ID = A.ParentID
				)
	update	T
	set		T.TextPath = utility.GetBreadcrumbString(@ot, S.ID, '/')
	from	Artifact T
			inner join S on S.ID = T.ID;

	merge	[cache].[Object] as T
	using	(
			select	@ot as [Object],
					ID as ObjectID,
					'ArtifactType' as ObjectType,
					ArtifactTypeID as ObjectTypeID
			from	inserted
			) as S
	on		T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
	when	matched then
			update set	T.[ObjectType] = S.[ObjectType],
						T.[ObjectTypeID] = S.[ObjectTypeID]
	when	not matched then
			insert	( [Object], [ObjectID], [ObjectType], [ObjectTypeID] )
			values	( S.[Object], S.[ObjectID], S.[ObjectType], S.[ObjectTypeID] );
GO

drop TRIGGER [dbo].[Artifact_AfterUpdate]
go

drop TRIGGER [dbo].[Artifact_AfterInsert]
go


ALTER TABLE SourceTargetRule ADD [Sequence] int CONSTRAINT [DF_SourceTargetRule_Sequence] DEFAULT ((1)) NOT NULL
go

CREATE TRIGGER [dbo].[Taxonomy_AfterUpsert]
   ON  [dbo].[Taxonomy] 
   AFTER INSERT, UPDATE
AS 
	SET NOCOUNT ON;
	declare @ot varchar(50) = 'Taxonomy'

	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select	case 
					when D.ID is not null then 'Update'
					else 'Add'
				end, 
				[queue].WriteIndexXml('', @ot, I.ID, coalesce(I.UpdatedBy, 0)), 
				@ot, 
				I.ID 
		from	inserted I
				left join deleted D on D.ID = I.ID;

		declare @tbl table (ID int);

		with d AS
		(
			SELECT	ParentID, 
					ID
			FROM	inserted
			UNION ALL
			SELECT	C.ParentID, 
					C.ID
			FROM	Taxonomy	C
					INNER JOIN d AS P ON P.ID = C.ParentID
		)

		insert into @tbl
			select ID from d

		update	T
		set		T.TextPath = utility.GetBreadcrumbStringWrapper(@ot, S.ID, '/'),
				T.[Level] = utility.GetObjectLevelWrapper(@ot, S.ID)
		from	Taxonomy T
				inner join @tbl S on S.ID = T.ID;

		merge	[cache].[Object] as T
		using	(
				select	@ot as [Object],
						ID as ObjectID,
						'TaxonomyType' as ObjectType,
						TaxonomyTypeID as ObjectTypeID
				from	inserted
				) as S
		on		T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
		when	matched then
				update set	T.[ObjectType] = S.[ObjectType],
							T.[ObjectTypeID] = S.[ObjectTypeID]
		when	not matched then
				insert	( [Object],[ObjectID], [ObjectType], [ObjectTypeID] )
				values	( S.[Object], S.[ObjectID], S.[ObjectType], S.[ObjectTypeID] );
GO

drop trigger [dbo].[Taxonomy_AfterUpdate]
go
drop trigger [dbo].[Taxonomy_AfterInsert]
go

alter VIEW [dbo].[FieldWithRelation]
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
			--left join cache.ObjectDetails D on D.[Object] = F.ObjectType and D.ObjectID = F.ObjectID
			--left join Attribute AD on F.ObjectType = 'Attribute' and AD.ID = F.ObjectID
			left join cache.ObjectDetails LD on 
				LD.[Object] = case when T.LookupObjectType = 'Lookup' then 'LookupType' when T.LookupObjectType = 'DomainItem' then 'Domain' else T.LookupObjectType end
				and LD.ObjectID = case when T.LookupObjectType = 'Lookup' then T.LookupObjectID when T.LookupObjectType = 'DomainItem' then T.LookupObjectID when T.LookupObjectType = 'Resource' then T.LookupObjectID when T.LookupObjectType is null then NULL else F.Value end
	--where	T.ObjectID = coalesce(D.ObjectTypeID, AD.AttributeTypeID)
	--		and coalesce(D.ObjectID, AD.ID) is not null
GO

ALTER procedure [cache].[SynchronizeResponsibilitiesForObject]
--declare
	@Object varchar(50),
	@ObjectID int
--set @Object = 'ArtifactType'
--set @ObjectID = 11
as
begin
	set nocount on;

	IF OBJECT_ID('tempdb.dbo.#Responsibilities', 'U') IS NOT NULL
		drop table #Responsibilities;

	create table #Responsibilities
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
	);

	CREATE CLUSTERED INDEX [IX_TempResponsibilities] ON #Responsibilities ([ID] ASC);
	CREATE NONCLUSTERED INDEX [IX_TempResponsibilities_Combined] ON #Responsibilities ([Object] ASC, [ObjectID] ASC, ContextHash ASC);

	insert into #Responsibilities
		select * from utility.GetVerticalResponsibilityList(@Object, @ObjectID, 1);

	insert into #Responsibilities
		select * from utility.GetHierarchyAssignedResponsibilityList(@Object, @ObjectID, 4);

	insert into #Responsibilities
		select * from utility.GetDirectlyAssignedResponsibilityList(@Object, @ObjectID, 7);


--select * from #Responsibilities
	--delete #Responsibilities where [Object] + cast(ObjectID as varchar) <> @Object + cast(@ObjectID as varchar)
	delete cache.ResponsibilityItem where [Object] = @Object and ObjectID = @ObjectID
	DELETE	T
	FROM	cache.ResponsibilityItem T
			INNER JOIN #Responsibilities S ON S.[Object] = T.[Object] 
											and S.[ObjectID] = T.[ObjectID] 
											and S.ResponsibilityTypeID = T.ResponsibilityTypeID 
											and S.ContextHash = T.ContextHash;

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

	select @max = max(ID) from #Responsibilities;

	while @current <= @max
	begin
		if exists(select 1 from #Responsibilities where ID = @current)
		begin
			select	@ResponsibilityID = ResponsibilityID,
					@ResponsibilityTypeID = ResponsibilityTypeID,
					@AssigningItem = AssigningItem,
					@AssigningItemID = AssigningItemID,
					@Obj = [Object],
					@ObjID = ObjectID,
					@ContextHash = ContextHash,
					@Priority = [Priority]
			from	#Responsibilities
			where	ID = @current;

			delete	#Responsibilities
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

	insert into cache.ResponsibilityItem
	(
		[ResponsibilityID], [ResponsibilityTypeID], [ResponsibilityType], 
		[AssigningItem], [AssigningItemID], 
		[Object], [ObjectID], 
		[ResponsibleObject], [ResponsibleObjectID], 
		[ContextHash], [ResponsibilityTypeGroup], Visible
	)
		select	distinct
				TR.ResponsibilityID,
				TR.ResponsibilityTypeID,
				RT.Name as ResponsibilityType,
				TR.AssigningItem,
				TR.AssigningItemID,
				TR.[Object],
				TR.ObjectID,
				R.ResponsibleObjectType as ResponsibleObject,
				R.ResponsibleObjectID,
				TR.ContextHash,
				RT.ResponsibilityTypeGroup,
				TR.Visible
		from	#Responsibilities TR
				inner join Responsibility R on R.ID = TR.ResponsibilityID
				inner join ResponsibilityType RT on RT.ID = R.ResponsibilityTypeID
end
GO

ALTER procedure [dbo].[AddRelationships]
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
						[Object], ObjectID,
						CreatedBy, CreatedOn,
						UpdatedBy, UpdatedOn		
					) 
					VALUES (
						@IntersectTypeID, 
						@Classification, 
						@Description,
						@StartObject, @StartObjectID,
						@EndObject, @EndObjectID,
						@ResourceID, @Date,
						@ResourceID, @Date
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

					--exec utility.AddAuditEntry @StartObject, @StartObjectID, @ResourceID, @Date, 'Created', 'Intersect', @IntersectID
					--exec utility.AddAuditEntry @EndObject, @EndObjectID, @ResourceID, @Date, 'Created', 'Intersect', @IntersectID
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

						--exec utility.AddAuditEntry 'Intersect', @IntersectID, @ResourceID, @Date, 'Updated', 'Intersect', @IntersectID
					end
				end
		end

		set @current = @current + 1
	end
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

			--exec utility.AddAuditEntry 'Intersect', @IntersectID, @ResourceID, @Date, 'Updated', 'Intersect', @IntersectID
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
						[Object], ObjectID,
						CreatedBy, CreatedOn,
						UpdatedBy, UpdatedOn				
					) 
					VALUES (
						@IntersectTypeID, 
						@Classification, 
						@Description,
						@Subject, @SubjectID,
						@Object, @ObjectID,
						@ResourceID, @Date,
						@ResourceID, @Date
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

					--exec utility.AddAuditEntry @Subject, @SubjectID, @ResourceID, @Date, 'Created', 'Intersect', @IntersectID
					--exec utility.AddAuditEntry @Object, @ObjectID, @ResourceID, @Date, 'Created', 'Intersect', @IntersectID
				end
		end

	select * from [Intersect] where ID = @IntersectID
end
GO

alter procedure [dbo].[AsyncAddObject]
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
		begin transaction @trans
		
		exec [cache].[SynchronizeObjectDetails] @Object, @ObjectID

		--INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID], [Priority]) values ('ObjectIndex', 'A', @Object, @ObjectID, 4)

		exec [utility].[AddAuditEntry] @ParentObject, @ParentObjectID, @ResourceID, @date, 'Created', @Object, @ObjectID

		if @Object = 'Intersect'
		begin
			declare @IDs dbo.IDTable
			insert into @IDs values (@ObjectID)
			exec [cache].[SynchronizeRelationships] @IDs
		end

		if @Object in ('AttributeTypeRelation', 'AttributeTypeRelation', 'ResponsibilityTypeRelation', 'ResponsibilityType')
		begin
			exec utility.CalculateStatistics
		end
		else
		begin
			exec utility.CalculateStatistics @Object, @ObjectID
		end

		if @Object = 'Responsibility'
		begin
			exec cache.SynchronizeResponsibilitiesForObject @ParentObject, @ParentObjectID 
		end

		if @Object = 'Artifact'
		begin
			exec cache.SynchronizeResponsibilitiesForObject @Object, @ObjectID 
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

alter procedure [dbo].[AsyncUpdateObject]
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
		begin transaction @trans
		
		--exec [cache].[SynchronizeObjectDetails] @Object, @ObjectID
		
		--INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID], [Priority]) values ('ObjectIndex', 'U', @Object, @ObjectID, 4)
		exec [utility].[AddAuditEntry] @ParentObject, @ParentObjectID, @ResourceID, @date, 'Updated', @Object, @ObjectID

		--if @Object = 'Artifact'
		--begin
		--	with h as	(
		--				select	ID,
		--						ParentID
		--				from	Artifact
		--				where	ID = @ObjectID
		--				union all
		--				select	A.ID,
		--						A.ParentID
		--				from	Artifact A
		--						inner join h P on P.ID = A.ParentID
		--				)
		--	update	T
		--	set		T.TextPath = utility.GetBreadcrumbStringWrapper(@Object, S.ID, '/')
		--	from	Artifact T
		--			inner join h S on S.ID = T.ID;
		--end

		if @Object in ('AttributeTypeRelation', 'AttributeTypeRelation', 'ResponsibilityTypeRelation', 'ResponsibilityType')
		begin
			exec utility.CalculateStatistics
		end
		else
		begin
			exec utility.CalculateStatistics @Object, @ObjectID
		end

		if @Object = 'Responsibility'
		begin
			exec cache.SynchronizeResponsibilitiesForObject @ParentObject, @ParentObjectID 
		end

		if @Object = 'Artifact'
		begin
			exec cache.SynchronizeResponsibilitiesForObject @Object, @ObjectID 
		end

		if @Object = 'Taxonomy'
		begin
			--with h as	(
			--			select	ID,
			--					ParentID
			--			from	Taxonomy
			--			where	ID = @ObjectID
			--			union all
			--			select	A.ID,
			--					A.ParentID
			--			from	Taxonomy A
			--					inner join h P on P.ID = A.ParentID
			--			)
			--update	T
			--set		T.TextPath = utility.GetBreadcrumbStringWrapper(@Object, S.ID, '/')
			--from	Taxonomy T
			--		inner join h S on S.ID = T.ID;

			UPDATE	F
			set		F.FormattedValue = utility.GetFormattedFieldLookupValue(FT.Type, FT.LookupDisplayFormat, FT.LookupObjectType, FT.LookupObjectID, F.Value)
			FROM	Field F
					inner join FieldType FT on FT.ID = F.FieldTypeID and FT.LookupObjectType = 'Taxonomy' 
					inner join Taxonomy A on A.ID = @ObjectID and A.TaxonomyTypeID = FT.LookupObjectID

			exec [cache].[SynchronizeResponsibilities]
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

alter procedure [dbo].[GetAllowedResponsibilityTypesByObject]
--declare
	@type varchar(50),
	@id int
--set @type = 'ArtifactType'
--set @id = 1
as
begin
	declare @useFilter bit
	set @useFilter = 1

	if @type not like '%Type'
	begin
		set @useFilter = 1
		SELECT	@id = ObjectTypeID
		from	cache.ObjectDetails where [Object] = @type and ObjectID = @id

		set @type = @type + 'Type'
	end

	if @useFilter = 1
		begin
			SELECT	RT.*
			FROM	ResponsibilityType RT
					inner join ResponsibilityTypeRelation RTR	on RTR.ResponsibilityTypeID = RT.ID 
																and RTR.ObjectType = @type
																and RTR.ObjectID = @id
		end
	else
		begin
			SELECT	*
			FROM	ResponsibilityType
			where	ID > 0
		end
end
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

			--update	T
			--set		T.Name = T.Name
			--from	Artifact T
			--		inner join @ResolvedObjects S on S.ObjectID = T.ID and S.[Action] = 'INSERT';

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

		declare @sourceObjectTypeName nvarchar(1000),
				@sourceSubject nvarchar(500),
				@sourceName nvarchar(500),
					
				@targetObjectTypeName nvarchar(1000),
				@targetSubject nvarchar(500),
				@targetName nvarchar(500),
				
				@predicateID int,
				@rundate datetime = CURRENT_TIMESTAMP

		if @Action = 'L' -- LINEAGE (create lineage from input spreadsheet)
		begin
			declare @focalObject varchar(50),
					@focalObjectID int,
					@focalObjectTypeName nvarchar(1000),
					@focalName nvarchar(500),
					@intersectPredicate varchar(50),
					@focalIntersectID int,
					@focalSubject nvarchar(500),
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
				select	@focalObject = FT.Value,
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

		if @Action = 'S' -- SYNONYM (create synonyms from input spreadsheet)
		begin
			declare @synonymErrorDetailMessage varchar(200)
			
			select	@current = min(I.RowIndex),
					@max = max(I.RowIndex)
			from	LoadItem I
					inner join LoadItemColumn ST on ST.LoadID = I.LoadID and ST.RowIndex = I.RowIndex and St.ColumnIndex = 1			-- source object type
					inner join LoadItemColumn STN on STN.LoadID = I.LoadID and STN.RowIndex = I.RowIndex and StN.ColumnIndex = 2		-- source object type name
					inner join LoadItemColumn S on S.LoadID = I.LoadID and S.RowIndex = I.RowIndex and S.ColumnIndex = 4				-- source object name
					inner join LoadItemColumn TT on TT.LoadID = I.LoadID and TT.RowIndex = I.RowIndex and TT.ColumnIndex = 5			-- target object type
					inner join LoadItemColumn TTN on TTN.LoadID = I.LoadID and TTN.RowIndex = I.RowIndex and TTN.ColumnIndex = 6		-- target object type name
					inner join LoadItemColumn T on T.LoadID = I.LoadID and T.RowIndex = I.RowIndex and T.ColumnIndex = 8				-- target object name
			where	I.LoadID = @LoadID
			
			-- go row by row
			while @current <= @max
			begin
				--load the objects / id's for the focal, source, and target objects
				select	@sourceObject = ST.Value,
						@sourceObjectTypeName = STN.Value,
						@sourceName = S.Value,
						@sourceSubject = SS.Value,
						
						@targetObject = TT.Value,
						@targetObjectTypeName = TTN.Value,
						@targetName = T.Value,
						@targetSubject = TS.Value
				from	LoadItem I
						inner join LoadItemColumn ST on ST.LoadID = I.LoadID and ST.RowIndex = I.RowIndex and St.ColumnIndex = 1		-- source object type
						inner join LoadItemColumn STN on STN.LoadID = I.LoadID and STN.RowIndex = I.RowIndex and StN.ColumnIndex = 2	-- source object type name
						inner join LoadItemColumn SS on SS.LoadID = I.LoadID and SS.RowIndex = I.RowIndex and SS.ColumnIndex = 3		-- source object subject
						inner join LoadItemColumn S on S.LoadID = I.LoadID and S.RowIndex = I.RowIndex and S.ColumnIndex = 4			-- source object name
						inner join LoadItemColumn TT on TT.LoadID = I.LoadID and TT.RowIndex = I.RowIndex and TT.ColumnIndex = 5		-- target object type
						inner join LoadItemColumn TTN on TTN.LoadID = I.LoadID and TTN.RowIndex = I.RowIndex and TTN.ColumnIndex = 6	-- target object type name
						inner join LoadItemColumn TS on TS.LoadID = I.LoadID and TS.RowIndex = I.RowIndex and TS.ColumnIndex = 7		-- target object subject
						inner join LoadItemColumn T on T.LoadID = I.LoadID and T.RowIndex = I.RowIndex and T.ColumnIndex = 8			-- target object name
				where	I.LoadID = @LoadID and I.RowIndex = @current

				select @sourceObjectID = 0, @targetObjectID = 0, @predicateID = 0;

				select @predicateID = min(ID) from [Predicate] where [Type] = 6;				

				if @sourceObject = 'Artifact'
				begin
					select	top 1
							@sourceObjectID = cod.objectid										
					from	[cache].objectdetails cod
							inner join artifact a on (cod.objectid = a.id)
							inner join taxonomytype t on (a.taxonomytypeid = t.id)
					where	cod.[object] = @sourceObject and cod.textpath = @sourceName and cod.objecttypename = @sourceObjectTypeName and t.Name = @sourceSubject
				end
				else
				begin
					-- load source object
					select	top 1
							@sourceObjectID = cod.objectid						
					from	[cache].objectdetails cod
					where	cod.[object] = @sourceObject and cod.textpath = @sourceName and cod.objecttypename = @sourceObjectTypeName
				end

				if @targetObject = 'Artifact'
				begin
					-- load target object
					select	top 1
							@targetObjectID = cod.objectid												
					from	[cache].objectdetails cod
							inner join artifact a on (cod.objectid = a.id)
							inner join taxonomytype t on (a.taxonomytypeid = t.id)
					where	cod.[object] = @targetObject and cod.textpath = @targetName and cod.objecttypename = @targetObjectTypeName and t.Name = @targetSubject
				end
				else
				begin
					-- load target object
					select	top 1
							@targetObjectID = cod.objectid												
					from	[cache].objectdetails cod
					where	cod.[object] = @targetObject and cod.textpath = @targetName and cod.objecttypename = @targetObjectTypeName
				end

				--debug 
				--select @sourceObjectID, @sourceObject, @targetObjectID, @targetObject, @predicateID

				--if all are provided we are good otherwise error
				if @sourceObjectID > 0 and @targetObjectID > 0 and @predicateID > 0
					begin

					-- add intersect between source / target if one doesnt exist
					exec [dbo].[AddRelationship] @UpdatedBy, @rundate, @sourceObject, @sourceObjectID, 2, null, null, @targetObject, @targetObjectID;

					-- add intersect map between source / target if one doesnt exist for source to target intersect
					if not exists (
							select	1 
							from	intersectmap map
									inner join intersectnode node1 on ( map.subjectintersectnodeid = node1.id and node1.objectid = @sourceObjectID and node1.objecttype = @sourceObject)
									inner join intersectnode node2 on ( map.objectintersectnodeid = node2.id and node2.objectid = @targetObjectID and node2.objecttype = @targetObject)
						where map.[type] = 6)
						begin							
							insert into intersectmap
								select 
									node1.ID as SubjectIntersectNode,
									node2.ID as ObjectIntersectNode,
									@predicateID as PredicateID,
									6 as [Type]
								from						
									intersectnode node1 
									inner join intersectnode node2 on (node1.objectid = @sourceObjectID and node1.objecttype =@sourceObject and node2.objectid = @targetObjectID and node2.objecttype = @targetObject and node1.intersectid = node2.intersectid);
						end

						update	LoadItem
						set		[Status] = 1,
								StatusMessage = 'Successfully added synonym'
						where	LoadID = @LoadID
								and RowIndex = @current
					end -- if valid
				else
					begin
						set @synonymErrorDetailMessage = '';

						if @sourceObjectID = 0
						begin
							set @synonymErrorDetailMessage = @synonymErrorDetailMessage + '  Source object is invalid.';
						end

						if @targetObjectID = 0
						begin
							set @synonymErrorDetailMessage = @synonymErrorDetailMessage + '  Target object is invalid.';
						end

						if @predicateID = 0
						begin
							set @synonymErrorDetailMessage = @synonymErrorDetailMessage + '  No predicate of type synonym.';
						end

						update	LoadItem
						set		[Status] = 0,
								StatusMessage = 'Failed to add synonym. ' + @synonymErrorDetailMessage + ' [source id:' + convert(varchar(10),@sourceObjectID) + ' type:' + @sourceObject +'] [target id:' + convert(varchar(10), @targetObjectID) + ' type:' + @targetObject + ']'
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
	if @Object = 'Domain'				begin		select @objectName = Name from Domain where ID = @ObjectID					end
	if @Object = 'DomainGroup'			begin		select @objectName = Name from DomainGroup where ID = @ObjectID				end
	if @Object = 'DomainType'			begin		select @objectName = Name from DomainType where ID = @ObjectID				end
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
	
	-- Relevant ONLY to: Artifact, Domain, Fusion, FusionAttribute, Intersect, Taxonomy
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

	-- Relevant ONLY to: Domain
	if @ActionObject = 'DomainItem'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = O.Name 
		from	DomainItem O
				inner join Domain T on T.ID = O.DomainID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from DomainItem where ID = @ActionObjectID
		insert into @tbl  select 0, 'Code', Code, 0, 0 from DomainItem where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from DomainItem where ID = @ActionObjectID
	end

	-- Relevant ONLY to: Domain, DomainGroup, DomainType
	if @ActionObject = 'Domain'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = O.Name 
		from	Domain O
				inner join DomainType T on T.ID = O.DomainTypeID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from Domain where ID = @ActionObjectID
		--insert into @tbl  select 0, 'ParentID', ParentID, 0, 0 from Domain where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from Domain where ID = @ActionObjectID
		insert into @tbl  select 0, 'DomainGroupID', DomainGroupID, 0, 0 from Domain where ID = @ActionObjectID
	end

	-- Relevant ONLY to: DomainGroup, DomainType
	if @ActionObject = 'DomainGroup'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = O.Name 
		from	DomainGroup O
				inner join DomainType T on T.ID = O.DomainTypeID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from DomainGroup where ID = @ActionObjectID
		insert into @tbl  select 0, 'MasterListID', MasterListID, 0, 0 from DomainGroup where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from DomainGroup where ID = @ActionObjectID
	end

	-- Relevant ONLY to: DomainType
	if @ActionObject = 'DomainType'
	begin
		select	@actionObjectTypeName = 'Domain Type',
				@actionObjectName = O.Name
		from	DomainType O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from DomainType where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from DomainType where ID = @ActionObjectID
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

	-- Relevant ONLY to: Fusion
	if @ActionObject = 'FusionAttributeOwnerRule'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = T.Name + ' Ownership Rule ' + cast(O.ID as nvarchar(15))
		from	FusionAttributeOwnerRule O
				inner join Fusion T on T.ID = O.FusionID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'ObjectType', ObjectType, 0, 0 from FusionAttributeOwnerRule where ID = @ActionObjectID
		insert into @tbl  select 0, 'ObjectID', ObjectID, 0, 0 from FusionAttributeOwnerRule where ID = @ActionObjectID
		insert into @tbl  select 0, 'ParentObjectType', ParentObjectType, 0, 0 from FusionAttributeOwnerRule where ID = @ActionObjectID
		insert into @tbl  select 0, 'ParentObjectID', ParentObjectID, 0, 0 from FusionAttributeOwnerRule where ID = @ActionObjectID
		insert into @tbl  select 0, 'RelationshipOwnerObjectType', RelationshipOwnerObjectType, 0, 0 from FusionAttributeOwnerRule where ID = @ActionObjectID
		insert into @tbl  select 0, 'RelationshipOwnerObjectID', RelationshipOwnerObjectID, 0, 0 from FusionAttributeOwnerRule where ID = @ActionObjectID
	end

	-- Relevant ONLY to: Fusion
	if @ActionObject = 'FusionAttributePromotionRule'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = T.Name + ' Promotion Rule ' + cast(O.ID as nvarchar(15))
		from	FusionAttributePromotionRule O
				inner join Fusion T on T.ID = O.FusionID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'ObjectType', ObjectType, 0, 0 from FusionAttributePromotionRule where ID = @ActionObjectID
		insert into @tbl  select 0, 'ObjectID', ObjectID, 0, 0 from FusionAttributePromotionRule where ID = @ActionObjectID
		insert into @tbl  select 0, 'ParentObjectType', ParentObjectType, 0, 0 from FusionAttributePromotionRule where ID = @ActionObjectID
		insert into @tbl  select 0, 'ParentObjectID', ParentObjectID, 0, 0 from FusionAttributePromotionRule where ID = @ActionObjectID
		insert into @tbl  select 0, 'PromotionObjectType', PromotionObjectType, 0, 0 from FusionAttributePromotionRule where ID = @ActionObjectID
		insert into @tbl  select 0, 'PromotionObjectID', PromotionObjectID, 0, 0 from FusionAttributePromotionRule where ID = @ActionObjectID
		insert into @tbl  select 0, 'PromotionParentObjectType', PromotionParentObjectType, 0, 0 from FusionAttributePromotionRule where ID = @ActionObjectID
		insert into @tbl  select 0, 'PromotionParentObjectID', PromotionParentObjectID, 0, 0 from FusionAttributePromotionRule where ID = @ActionObjectID
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

	-- Relevant ONLY to: Artifact, Domain, FusionAttribute, Intersect, Taxonomy, Policy, Rule
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
		insert into @tbl  select 0, 'ResponseTypeID', ResponseTypeID, 0, 0 from QuestionType where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from QuestionType where ID = @ActionObjectID
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

	-- Relevant ONLY to: Artifact, ArtifactType, DomainType, Intersect, Policy, Rule, Taxonomy, TaxonomyType, Vocabulary
	if @ActionObject = 'Responsibility'
	begin
		select	@actionObjectTypeName = 'Responsibility',
				@actionObjectName = T.Name 
		from	Responsibility O
				inner join ResponsibilityType T on T.ID = O.ResponsibilityTypeID

		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Context', (
				select	D.Name + ': ' + I.Code + ' - ' + I.Name + '; '
				from	ResponsibilityContextItem C
						inner join DomainItem I on C.ObjectType = 'DomainItem' and C.ObjectID = I.ID
						inner join Domain D on D.ID = I.DomainID
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
		insert into @tbl  select 0, 'ObjectType', ObjectType, 0, 0 from SurveyType where ID = @ActionObjectID
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



alter procedure [utility].[CalculateStatistics]
--declare
	@Type varchar(50) = NULL,
	@ID int = NULL,
	@TargetStatisticTypeID int = NULL
as
begin
	SET NOCOUNT ON;

	declare @current int, @max int
	declare @relations table (ID int identity, [Object] varchar(50), ObjectID int)

	IF OBJECT_ID('tempdb..#StatisticTypes') IS NOT NULL
	BEGIN
		DROP TABLE #StatisticTypes
	END
	create table #StatisticTypes (ID int identity, StatisticTypeID int)

	insert into #StatisticTypes
		select ID from StatisticType where (@TargetStatisticTypeID is not null and ID = @TargetStatisticTypeID) OR @TargetStatisticTypeID is null order by ID

	set		@current	= 1
	select	@max		= MAX(ID) from #StatisticTypes

	IF OBJECT_ID('tempdb..#Statistics') IS NOT NULL
	BEGIN
		DROP TABLE #Statistics
	END
	create table #Statistics (StatisticTypeID int, ObjectType varchar(50), ObjectID int, Score int)

--select * from #StatisticTypes

	while @current <= @max
	begin
		declare @StatisticTypeID int,
				@CheckType int,
				@CheckObjectType varchar(25),
				@CheckObjectID int,
				@Object varchar(25),
				@ObjectID int,
				@Score int,
				@PropertyName varchar(250),
				@Value nvarchar(4000),
				@PredicateID int,
				@Configuration xml

		select	@StatisticTypeID = S.ID,
				@CheckType = S.CheckType,
				@Configuration = S.Configuration,
				@Object = [Object],
				@ObjectID = ObjectID,
				@Score = Score 
		from	#StatisticTypes T
				inner join StatisticType S on S.ID = T.StatisticTypeID
		where	T.ID = @current
				
		delete @relations
		
		insert into @relations
			select	[Object],
					ObjectID
			from	cache.[Object]
			where	ObjectType = @Object
					and ObjectTypeID = @ObjectID
					and (
						(@Type is not null and [Object] = @Type and ObjectID = @ID) OR (@Type is null) 
						)
		
		
		-- EXISTENCE
		if (@CheckType = 1)
		begin
			select	@CheckObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)'),
					@CheckObjectID = f.value('(ObjectID/text())[1]', 'int')
			from	@Configuration.nodes('/fields') as F(f)

			if @CheckObjectType = 'AttributeType'
			begin
				insert into #Statistics
					select	@StatisticTypeID as StatisticTypeID,
							R.[Object],
							R.ObjectID,
							case 
								when O.ValueExists <> 0 then @Score
								else 0
							end as Score
					from	@relations R
							outer apply (
										select		ISNULL(AttributeTypeID, 0) as ValueExists
										from		Attribute 
										where		ObjectType = R.[Object] and ObjectID = R.ObjectID and AttributeTypeID = @CheckObjectID
										group by	AttributeTypeID, ObjectType, ObjectID
										) O
			end

			if @CheckObjectType = 'ResponsibilityType'
			begin
				insert into #Statistics
					select	@StatisticTypeID as StatisticTypeID,
							R.[Object],
							R.ObjectID,
							case 
								when P.ValueExists <> 0 then @Score
								else 0
							end as Score
					from	@relations R
							outer apply (
										select		ISNULL(ResponsibilityTypeID, 0) as ValueExists
										from		[cache].[ResponsibilityItem]
										where		[Object] = R.[Object] and ObjectID = R.ObjectID and ResponsibilityTypeID = @CheckObjectID
										group by	ResponsibilityTypeID, [Object], ObjectID
										) P
			end
		end

		-- COUNT (instead of score)
		if (@CheckType = 2)	--COUNT
		begin
			select	@CheckObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)'),
					@CheckObjectID = f.value('(ObjectID/text())[1]', 'int')
			from	@Configuration.nodes('/fields') as F(f)

			if @CheckObjectType = 'AttributeType'
			begin
				insert into #Statistics
					select	@StatisticTypeID as StatisticTypeID,
							R.[Object],
							R.ObjectID,
							COALESCE(O.Score, 0) as Score
					from	@relations R
							outer apply (
										select		COUNT(1) as Score
										from		Attribute 
										where		ObjectType = R.[Object] and ObjectID = R.ObjectID and AttributeTypeID = @CheckObjectID
										group by	AttributeTypeID, ObjectType, ObjectID
										) O
			end

			if @CheckObjectType = 'ResponsibilityType'
			begin
				insert into #Statistics
					select	@StatisticTypeID as StatisticTypeID,
							R.[Object],
							R.ObjectID,
							COALESCE(O.Score, 0) as Score
					from	@relations R
							outer apply (
										select		COUNT(1) as Score
										from		[cache].[ResponsibilityItem]
										where		[Object] = R.[Object] and ObjectID = R.ObjectID and ResponsibilityTypeID = @CheckObjectID
										group by	ResponsibilityTypeID, [Object], ObjectID
										) O
			end

			-- This does a count on relationships
			if @CheckObjectType <> 'AttributeType' and @CheckObjectType <> 'ResponsibilityType'
			begin
				insert into #Statistics
					select	@StatisticTypeID as StatisticTypeID,
							R.[Object],
							R.ObjectID,
							COALESCE(O.Score, 0) as Score
					from	@relations R
							outer apply (
										select		COUNT(1) as Score
										from		[cache].[Relationship] IR
													inner join cache.[Object] ID on ID.[Object] = IR.TargetObject and ID.ObjectID = IR.TargetObjectID 
																				and ID.ObjectType = @CheckObjectType and ID.ObjectTypeID = @CheckObjectID 
																				and IR.SourceObject = R.[Object] and IR.SourceObjectID = R.ObjectID
										group by	ID.ObjectType, ID.ObjectTypeID
										) O
			end
		end

		-- PROPERTY VALUE CHECK
		if (@CheckType = 3)
		begin
			select	@PropertyName = f.value('(PropertyName/text())[1]', 'varchar(250)'),
					@Value = f.value('(PropertyValue/text())[1]', 'nvarchar(4000)')
			from	@Configuration.nodes('/fields') as F(f)

			if @Object = 'ArtifactType' and @PropertyName = 'Status'
				begin
					insert into #Statistics
						select	@StatisticTypeID as StatisticTypeID,
								R.[Object],
								R.ObjectID,
								case 
									when O.ValueExists <> 0 then @Score
									else 0
								end as Score
						from	@relations R
								outer apply (
											select		CASE 
															when [Status] = @Value then 1
															else 0
														END as ValueExists
											from		Artifact
											where		R.[Object] = 'Artifact' and ID = R.ObjectID
											) O
				end
			else
				begin
					-- A dynamic field to check.
					insert into #Statistics
						select	@StatisticTypeID as StatisticTypeID,
								R.[Object],
								R.ObjectID,
								case 
									when O.ValueExists <> 0 then @Score
									else 0
								end as Score
						from	@relations R
								outer apply (
											select	CASE 
														when F.FormattedValue = @Value then 1
														else 0
													END as ValueExists									
											from	Field F
													inner join FieldType FT on FT.[Object] = @Object and FT.ObjectID = @ObjectID 
																			and F.[ObjectType] = R.[Object] and F.ObjectID = R.ObjectID
																			and FT.Name = @PropertyName 
											) O
				end
		end

		-- PROPERTY POPULATED
		if (@CheckType = 4)
		begin
			select	@PropertyName = f.value('(PropertyName/text())[1]', 'varchar(250)')
			from	@Configuration.nodes('/fields') as F(f)

			if @PropertyName = 'Description'
				begin
					insert into #Statistics
						select	@StatisticTypeID as StatisticTypeID,
								R.[Object],
								R.ObjectID,
								case 
									when D.Description is null then 0
									when LEN(D.Description) < 25 then 0
									else @Score
								end as Score
						from	@relations R
								left join cache.ObjectDetails D on D.[Object] = R.[Object] and D.ObjectID = R.ObjectID
				end
			else
				begin
					-- A dynamic field to check.
					insert into #Statistics
						select	@StatisticTypeID as StatisticTypeID,
								R.[Object],
								R.ObjectID,
								case 
									when O.ValueExists <> 0 then @Score
									else 0
								end as Score
						from	@relations R
								outer apply (
											select	case
														when F.FormattedValue is not null then 1
														else 0
													END as ValueExists
											from	Field F
													inner join FieldType FT on FT.[Object] = @Object and FT.ObjectID = @ObjectID 
																			and F.[ObjectType] = R.[Object] and F.ObjectID = R.ObjectID
																			and FT.Name = @PropertyName 
											) O
				end
		end

		-- RELATIONSHIP
		if (@CheckType = 5)
		begin
			declare @checkRelationshipObjects table (Object varchar(50), ObjectID int)

			-- first, check legacy format
			select	@CheckObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)'),
					@CheckObjectID = f.value('(ObjectID/text())[1]', 'int')
			from	@Configuration.nodes('/fields') as F(f)

			if @CheckObjectType is not null and @CheckObjectID is not null
				begin
					insert into @checkRelationshipObjects values (@CheckObjectType, @CheckObjectID)
				end
			else
				begin
					--check new format of multiple options
					insert into @checkRelationshipObjects
						select	f.value('(Object/Type/text())[1]', 'varchar(50)'),
								f.value('(Object/ID/text())[1]', 'int')
						from	@Configuration.nodes('/fields/CheckObjects') as F(f)
				end


			insert into #Statistics
				select	@StatisticTypeID as StatisticTypeID,
						R.[Object],
						R.ObjectID,
						case 
							when O.[Count] > 0 then @Score
							else 0
						end as Score
				from	@relations R
						outer apply (
									select		COUNT(1) as [Count]
									from		[cache].[Relationship] IR
												inner join cache.[Object] D on D.[Object] = IR.TargetObject and D.ObjectID = IR.TargetObjectID 
																			and IR.SourceObject = R.[Object] and IR.SourceObjectID = R.ObjectID
												inner join @checkRelationshipObjects TT on TT.[Object] = D.ObjectType and TT.ObjectID = D.ObjectTypeID
									) O

		end

		-- FUSION OWNERSHIP
		if (@CheckType = 6)
		begin
			--select	@CheckObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)'),
			--		@CheckObjectID = f.value('(ObjectID/text())[1]', 'int')
			--from	@Configuration.nodes('/fields') as F(f)

			insert into #Statistics
				select	@StatisticTypeID as StatisticTypeID,
						R.[Object],
						R.ObjectID,
						case 
							when O.ValueExists <> 0 then @Score
							else 0
						end as Score
				from	@relations R
						outer apply (
									select		ISNULL(RelationshipOwnerObjectID, 0) as ValueExists
									from		FusionAttributeOwnerRule
									where		RelationshipOwnerObjectType = R.[Object] and RelationshipOwnerObjectID = R.ObjectID
									group by	RelationshipOwnerObjectType, RelationshipOwnerObjectID
									) O
		end

		-- ROLLUP VIA RELATIONSHIPS
		if (@CheckType = 7)
		begin
			select	@CheckObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)'),
					@CheckObjectID = f.value('(ObjectID/text())[1]', 'int')
			from	@Configuration.nodes('/fields') as F(f)

			insert into #Statistics
				select	@StatisticTypeID as StatisticTypeID,
						R.[Object],
						R.ObjectID,
						round((T.Total/C.[Count]) * @Score, 0) Score
				from	@relations R
						cross apply (
									select	count(1) as [Count] 
									from	cache.Relationships
									where	SourceObject = R.[Object] and SourceObjectID = R.ObjectID
											and TargetType = @CheckObjectType and TargetTypeID = @CheckObjectID
									) C
						outer apply (
									select	sum(dbo.GetObjectStatisticScore(TargetObject, TargetObjectID)) as Total
									from	cache.Relationships 
									where	SourceObject = R.[Object] and SourceObjectID = R.ObjectID 
											and TargetType = @CheckObjectType and TargetTypeID = @CheckObjectID
									) T
				where C.[Count] > 0
		end

		-- ROLLUP VIA OWNERSHIP
		if (@CheckType = 8)
		begin
			select	@CheckObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)'),
					@CheckObjectID = f.value('(ObjectID/text())[1]', 'int')
			from	@Configuration.nodes('/fields') as F(f)

			insert into #Statistics
				select	@StatisticTypeID as StatisticTypeID,
						R.[Object],
						R.ObjectID,
						round((T.Total/C.[Count]) * @Score, 0) Score
				from	@relations R
						cross apply (
									select	count(1) as [Count] 
									from	cache.Responsibilities
									where	ResponsibleObject = R.[Object] and ResponsibleObjectID = R.ObjectID
											and ObjectType = @CheckObjectType and ObjectTypeID = @CheckObjectID
									) C
						outer apply (
									select	sum(dbo.GetObjectStatisticScore([Object], ObjectID)) as Total
									from	cache.Responsibilities 
									where	ResponsibleObject = R.[Object] and ResponsibleObjectID = R.ObjectID 
											and ObjectType = @CheckObjectType and ObjectTypeID = @CheckObjectID
									) T
				where C.[Count] > 0
		end
		

		-- EVENT METRIC CHECK
		if (@CheckType = 9)
		begin
			declare @ValidField nvarchar(250),-- = 'ValidCount',
					@InvalidField nvarchar(250),-- = 'InvalidCount',
					@Threshold decimal(9,2),-- = 0.10,
					@TotalValid float,
					@TotalInvalid float

			select	@ValidField = f.value('(ValidField/text())[1]', 'nvarchar(250)'),
					@InvalidField = f.value('(InvalidField/text())[1]', 'nvarchar(250)'),
					@Threshold = f.value('(Threshold/text())[1]', 'decimal(9,2)')
			from	@Configuration.nodes('/fields') as F(f)


			select	@TotalValid = sum(cast(V.ValidCount as int)),
					@TotalInvalid = sum(cast(I.InvalidCount as int))
			from	cache.Relationships REL
					inner join [Rule] R on R.ID = REL.TargetObjectID and REL.TargetObject = 'Rule' and R.RuleType in (3,4)
					inner join EventGroup EG on EG.RuleID = R.ID
					inner join [Event] E on E.EventGroupID = EG.ID 
					inner join (
								select	R.ID,
										max(E.Date) as [Date]
								from	cache.Relationships REL
										inner join [Rule] R on R.ID = REL.TargetObjectID and REL.TargetObject = 'Rule' and R.RuleType in (3,4)
										inner join EventGroup EG on EG.RuleID = R.ID
										inner join [Event] E on E.EventGroupID = EG.ID
								group by R.ID					
								) F on F.ID = R.ID and F.[Date] = E.[Date]
					cross apply (
								select	Value as ValidCount
								from	FieldWithRelation
								where	ObjectType = 'Event' and ObjectID = E.ID and Name = @ValidField
								) V
					cross apply (
								select	Value as InvalidCount
								from	FieldWithRelation
								where	ObjectType = 'Event' and ObjectID = E.ID and Name = @InvalidField
								) I

			insert into #Statistics
				select	@StatisticTypeID as StatisticTypeID,
						R.[Object],
						R.ObjectID,
						case 
							when cast(@TotalInvalid / @TotalValid as decimal(9,2)) < @Threshold then @Score
							else 0
						end as Score
				from	@relations R
		end

		-- PREDICATE CHECK
		if (@CheckType = 10)
		begin
			select	@PredicateID = f.value('(Predicate/text())[1]', 'int')
			from	@Configuration.nodes('/fields') as F(f)

			insert into #Statistics
				select	@StatisticTypeID as StatisticTypeID,
						R.[Object],
						R.ObjectID,
						case 
							when O.[Count] > 0 then @Score
							else 0
						end as Score
				from	@relations R
						outer apply (
									select	count(1) as [Count]
									from	IntersectMap M
											inner join cache.Relationship IR on IR.[SourceIntersectNodeID] = M.SubjectIntersectNodeID 
																			and IR.[TargetIntersectNodeID] = M.ObjectIntersectNodeID 
																			and M.PredicateID = @PredicateID
											inner join cache.Relationship T1 on T1.SourceObject = R.[Object] 
																			and T1.SourceObjectID = R.ObjectID
																			and T1.TargetObject = IR.SourceObject 
																			and T1.TargetObjectID = IR.SourceObjectID
											inner join cache.Relationship T2 on T2.SourceObject = R.[Object] 
																			and T2.SourceObjectID = R.ObjectID
																			and T2.TargetObject = IR.TargetObject 
																			and T2.TargetObjectID = IR.TargetObjectID
									) O
		end

		set @current = @current + 1
	end

	
	-- now merge the Statistics table
	MERGE	Statistic AS T
	USING	(
			select	distinct
					S.*,
					MS.DateStart
			from	#Statistics S
					outer apply (
								select		StatisticTypeID,
											ObjectType,
											ObjectID,
											MAX(DateStart) as DateStart
								from		Statistic
								where		StatisticTypeID = S.StatisticTypeID
											and ObjectType = S.ObjectType
											and ObjectID = S.ObjectID
								group by	StatisticTypeID,
											ObjectType,
											ObjectID
								) MS
			) AS S
	ON		(
			T.StatisticTypeID = S.StatisticTypeID
			and T.ObjectType = S.ObjectType
			and T.ObjectID = S.ObjectID
			and T.DateStart = S.DateStart
			and T.Score = S.Score
			)
		WHEN MATCHED THEN 
			UPDATE SET T.DateEnd = getutcdate()
		WHEN NOT MATCHED THEN	
			INSERT	
			VALUES	(
					S.StatisticTypeID, 
					S.ObjectType, 
					S.ObjectID,
					getutcdate(), 
					getutcdate(), 
					S.Score
					);
	
end
GO

alter procedure [utility].[GetOwnersForWorkflow]
--declare 
	@workflowID uniqueidentifier
--set @workflowID = '387A8094-565E-45AF-B049-01329EEF2209' --=> wt 1
--set @workflowID = '0C573C9B-D237-4468-8822-7D515750675B'--'CEE2AF0D-DAB8-432B-AF08-00E52B808C52' --=> wt 2
--set @workflowID = 'FD3C4A3D-C9BB-477A-B5CD-BC99C62AF53F' --=> wt 3
as
begin
	declare @workflowType int,
			@fields xml
	declare @tbl table (ID int, FirstName nvarchar(250), LastName nvarchar(250), Email nvarchar(500), Username nvarchar(500), DateLastLoggedIn datetime null, ResourceTypeID int, Status nvarchar(25))

	select	@workflowType = WorkflowType,
			@fields = Data
	from	Workflow
	where	ID = @workflowID

	if @workflowType = 1
	begin
		--1. Check for vocabulary owners
		insert into @tbl
			select	R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
			from	ResponsibilityDetail RD 
					inner join WorkflowTypeRelation WTR on WTR.Parent = 'TaxonomyType' and WTR.ParentID = @fields.value('(/fields/TaxonomyTypeID)[1]', 'int') and WTR.WorkflowType = @workflowType and WTR.ResponsibilityTypeID = RD.ResponsibilityTypeID
					inner join reporting.Global_Resource R 
						on RD.ObjectType = 'TaxonomyType' 
						and RD.ObjectID = @fields.value('(/fields/TaxonomyTypeID)[1]', 'int')
						and	(
								(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
								(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
							)
						and R.Email not like '%?subject=%' and R.Status = 'Active'

		if not exists(select * from @tbl)
		begin
			insert into @tbl
				select	R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from	ResponsibilityDetail RD 
						inner join WorkflowTypeRelation WTR on WTR.[Object] = 'ArtifactType' and WTR.ObjectID = @fields.value('(/fields/ArtifactTypeID)[1]', 'int') and WTR.Parent is null and WTR.WorkflowType = @workflowType and WTR.ResponsibilityTypeID = RD.ResponsibilityTypeID
						inner join reporting.Global_Resource R 
							on RD.ObjectType = 'ArtifactType' 
							and RD.ObjectID = @fields.value('(/fields/ArtifactTypeID)[1]', 'int')
							and (
									(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
									(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
								)
							and R.Email not like '%?subject=%' and R.Status = 'Active'
		end
	end

	if @workflowType = 2
	begin
		insert into @tbl
			select	R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
			from	ResponsibilityDetail RD 
					inner join Artifact A on RD.ObjectType = 'Artifact' and RD.ObjectID = A.ID and A.ID = @fields.value('(/fields/ArtifactID)[1]', 'int')
					inner join WorkflowTypeRelation WTR		on WTR.[Object] = 'ArtifactType' and WTR.ObjectID = A.ArtifactTypeID 
															and WTR.Parent = 'TaxonomyType' and WTR.ParentID = A.TaxonomyTypeID
															and WTR.WorkflowType = @workflowType 
															and WTR.ResponsibilityTypeID = RD.ResponsibilityTypeID
					inner join reporting.Global_Resource R 
						on	(
								(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
								(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
							)
						and R.Email not like '%?subject=%' and R.Status = 'Active' 

		if not exists(select * from @tbl)
		begin
			insert into @tbl
				select	R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from	ResponsibilityDetail RD 
						inner join Artifact A on RD.ObjectType = 'Artifact' and RD.ObjectID = A.ID and A.ID = @fields.value('(/fields/ArtifactID)[1]', 'int')
						inner join WorkflowTypeRelation WTR		on WTR.[Object] = 'ArtifactType' and WTR.ObjectID = A.ArtifactTypeID 
																and WTR.WorkflowType = @workflowType 
																and WTR.ResponsibilityTypeID = RD.ResponsibilityTypeID
						inner join reporting.Global_Resource R 
							on	(
									(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
									(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
								)
							and R.Email not like '%?subject=%' and R.Status = 'Active' 
		end
	end

	if @workflowType = 3
	begin

		insert into @tbl
			select	distinct
						R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from	Comment C
						inner join CommentRelation CR on CR.CommentID = C.ID and C.ID = @fields.value('(fields/CommentID)[1]', 'int') and CR.ObjectType not in ('Resource', 'Group')
						inner join ResponsibilityDetail RD on RD.ObjectType = CR.ObjectType and RD.ObjectID = CR.ObjectID 
						inner join WorkflowTypeRelation WTR		on WTR.[Object] = RD.ObjectType +'Type' and WTR.ObjectID = RD.ObjectTypeID 
																and WTR.WorkflowType = @workflowType 
																and WTR.ResponsibilityTypeID = RD.ResponsibilityTypeID
																and WTR.[Enabled] = 1
						inner join reporting.Global_Resource R 
							on	(
									(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
									(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
								) 
								and R.Email not like '%?subject=%' and R.Status = 'Active'

		if not exists (select 1 from @tbl)
		begin
			insert into @tbl
				select	distinct
						R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from	Comment C
						inner join CommentRelation CR on CR.CommentID = C.ID and C.ID = @fields.value('(fields/CommentID)[1]', 'int') and CR.ObjectType not in ('Resource', 'Group')
						inner join ResponsibilityDetail RD on RD.ObjectType = CR.ObjectType and RD.ObjectID = CR.ObjectID 
						inner join reporting.Global_Resource R 
							on	(
									(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
									(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
								) 
								and R.Email not like '%?subject=%' and R.Status = 'Active'
		end

		if not exists (select 1 from @tbl)
		begin
			insert into @tbl
				select 
					R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from 
					reporting.Global_Resource R where isadministrator = 1 and status = 'Active'
		end
	end

	if @workflowType = 4
	begin
		insert into @tbl
				select	R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from	ResponsibilityDetail RD 
						inner join Artifact A on RD.ObjectType = 'Artifact' and RD.ObjectID = A.ID and A.ID = @fields.value('(/fields/ArtifactID)[1]', 'int')
						inner join WorkflowTypeRelation WTR		on WTR.[Object] = 'ArtifactType' and WTR.ObjectID = A.ArtifactTypeID 
																and WTR.WorkflowType = @workflowType 
																and WTR.ResponsibilityTypeID = RD.ResponsibilityTypeID
																and WTR.[Enabled] = 1
						inner join reporting.Global_Resource R 
							on	(
									(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
									(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
								)
							and R.Email not like '%?subject=%' and R.Status = 'Active' 

		if not exists (select 1 from @tbl)
		begin
			insert into @tbl
				select	distinct
						R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from	Comment C
						inner join CommentRelation CR on CR.CommentID = C.ID and C.ID = @fields.value('(fields/CommentID)[1]', 'int') and CR.ObjectType not in ('Resource', 'Group')
						inner join ResponsibilityDetail RD on RD.ObjectType = CR.ObjectType and RD.ObjectID = CR.ObjectID 
						inner join reporting.Global_Resource R 
							on	(
									(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
									(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
								) 
								and R.Email not like '%?subject=%' and R.Status = 'Active'
		end

		if not exists (select 1 from @tbl)
		begin
			insert into @tbl
				select 
					R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from 
					reporting.Global_Resource R where isadministrator = 1 and status = 'Active'
		end
	end

	select * from @tbl
end
GO

ALTER PROCEDURE [utility].[ProcessIntersectTemplates]
AS
BEGIN
	SET NOCOUNT ON;

	Declare @ExecutionID int = 0,			
			@NumberOfObjectsUpdated int = 0,
			@currentTemplateID int = 1,
			@maxTemplateID int = 0,
			@NumberOfObjectsConsidered int = 0,
			@NumberOfIntersectsAdded int = 0;		

	declare @intersectTable table	(
		IntersectTypeID int, IntersectID int, ID int, SourceObject varchar(50),
		SourceObjectID int, SourceIntersectTypeNodeID int, [TargetObject] varchar(50), TargetObjectID int, TargetIntersectTypeNodeID int,
		[type] int, predicateid int	
	)

	declare @intersectToItemTable table	(
		IntersectTypeID int, IntersectID int, ID int, SourceObject varchar(50),
		SourceObjectID int, SourceIntersectTypeNodeID int, [TargetObject] varchar(50), TargetObjectID int, TargetIntersectTypeNodeID int,
		[type] int, predicateid int				
	)

	declare @itemsWeNeedIntersectsTO table	(
		SourceObject varchar(50),SourceObjectID int
	)

	declare @intersectToItemTempTable [utility].[DiagramRelationshipTable];
	
	declare @intersectToItemNotInDiagramTempTable table	(
		IntersectTypeID int, SourceObject varchar(50),SourceObjectID int, SourceIntersectTypeNodeID int, 
		[TargetObject] varchar(50), TargetObjectID int, TargetIntersectTypeNodeID int							
	)


	declare @intersectToItemNotInDiagramTable table	(
		IntersectTypeID int, SourceObject varchar(50),SourceObjectID int, SourceIntersectTypeNodeID int, 
		[TargetObject] varchar(50), TargetObjectID int, TargetIntersectTypeNodeID int							
	)


	Declare @TemplateTable Table(ID int identity,TemplateID int,Query varchar(max), [Object] varchar(50), [ObjectID] int);

	IF OBJECT_ID('tempdb..#itemsToCopyToTable') IS NOT NULL
		DROP TABLE #itemsToCopyToTable;

	create table #itemsToCopyToTable (ID int identity, ObjectID int, [Object] varchar(50))
	
	-- check if there is any work
		-- any templates?
	insert into @TemplateTable
		select	ID, Query,[Object],[ObjectID]
		from	[dbo].[IntersectMapTemplate]
		where [Enabled] = 1;
	
	if not exists(select 1 from @TemplateTable)
	begin
		Print 'No enabled templates'
		return;
	end;

	-- log start
	--Log this run get a new id from the fusion.promotion table
	insert into [dbo].[IntersectMapTemplateLogSummary] ( DateStarted )
									values ( CURRENT_TIMESTAMP)

	select @ExecutionID =  SCOPE_IDENTITY()


	-- loop through the templates
	select @maxTemplateID = max(ID) from @TemplateTable;

	while (@currentTemplateID <= @maxTemplateID)
	begin
		
		declare @objectID int,
				@object varchar(50),
				@query nvarchar(max)

		select	@objectID = [ObjectID],
				@object = [Object],
				@query = Query					
			from	@TemplateTable
			where	ID = @currentTemplateID

		
		--load relations for item in object/objectid
		
		delete from @intersectTable;
		delete from @intersectToItemTable;
		truncate table #itemsToCopyToTable;
		delete from @itemsWeNeedIntersectsTO;
		delete from @intersectToItemNotInDiagramTable;
		
		insert into @intersectTable			
			select	distinct
					R.IntersectTypeID,
					R.IntersectID,
					M.ID,	
					R.SourceObject,		
					R.SourceObjectID,						
					R.SourceIntersectTypeNodeID,
					R.TargetObject,
					R.TargetObjectID,
					R.TargetIntersecttypeNodeID,
					m.[type],
					m.[predicateid]
			from	IntersectMap M
					inner join [cache].[Relationships] R on M.SubjectIntersectNodeID = R.SourceIntersectNodeID and M.ObjectIntersectNodeID = R.TargetintersectNodeID and M.[Type] = 1
					inner join [cache].ObjectDetails SD on SD.[Object] = R.SourceObject and SD.ObjectID = R.SourceObjectID
					inner join [cache].ObjectDetails TD on TD.[Object] = R.TargetObject and TD.ObjectID = R.TargetObjectID
					inner join Predicate P on P.ID = M.PredicateID
					inner join [cache].[Relationship] SR on SR.SourceObject = @object and SR.SourceObjectID = @objectID and SR.TargetObject = R.SourceObject and SR.TargetObjectID = R.SourceObjectID
					inner join [cache].[Relationship] TR on TR.SourceObject = @object and TR.SourceObjectID = @objectID and TR.TargetObject = R.TargetObject and TR.TargetObjectID = R.TargetObjectID
			union
			select	distinct
					R.IntersectTypeID,
					R.IntersectID,
					M.ID,	
					R.SourceObject,		
					R.SourceObjectID,
					R.SourceIntersectTypeNodeID,						
					R.TargetObject,
					R.TargetObjectID,
					R.TargetIntersecttypeNodeID,
					m.[type],
					m.[predicateid]				
			from	IntersectMap M
					inner join [cache].[Relationships] R on M.SubjectIntersectNodeID = R.SourceIntersectNodeID and M.ObjectIntersectNodeID = R.TargetintersectNodeID and R.SourceObject = @object and R.SourceObjectID = @objectID and M.[Type] = 1
					inner join [cache].ObjectDetails SD on SD.[Object] = R.SourceObject and SD.ObjectID = R.SourceObjectID
					inner join [cache].ObjectDetails TD on TD.[Object] = R.TargetObject and TD.ObjectID = R.TargetObjectID
					inner join Predicate P on P.ID = M.PredicateID
			union
			select	distinct
					R.IntersectTypeID,
					R.IntersectID,
					M.ID,		
					R.SourceObject,	
					R.SourceObjectID,	
					R.SourceIntersectTypeNodeID,					
					R.TargetObject,
					R.TargetObjectID,
					R.TargetIntersecttypeNodeID,
					m.[type],
					m.[predicateid]
			from	IntersectMap M
					inner join [cache].[Relationships] R on M.SubjectIntersectNodeID = R.SourceIntersectNodeID and M.ObjectIntersectNodeID = R.TargetintersectNodeID and R.TargetObject = @object and R.TargetObjectID = @objectID and M.[Type] = 1
					inner join [cache].ObjectDetails SD on SD.[Object] = R.SourceObject and SD.ObjectID = R.SourceObjectID
					inner join [cache].ObjectDetails TD on TD.[Object] = R.TargetObject and TD.ObjectID = R.TargetObjectID
					inner join Predicate P on P.ID = M.PredicateID
							
		-- insert the intersect to the items that will be replaced into separate table
		insert into @intersectToItemTable select * from @intersectTable where (sourceobject = @object and sourceobjectid = @objectid) or (targetobject = @object and targetobjectid = @objectid)
		
		--delete the intersects that need to be updated
		delete from @intersectTable where (sourceobject = @object and sourceobjectid = @objectid) or (targetobject = @object and targetobjectid = @objectid)

		
		insert into @itemsWeNeedIntersectsTO
			select distinct sourceobject, sourceobjectid from @intersectTable
			union
			select distinct targetobject, targetobjectid from @intersectTable

		-- remove items that will be added as result of relation in diagram
		delete w
			from @itemsWeNeedIntersectsTO w
			inner join @intersectToItemTable it
			on w.sourceobject = it.sourceobject and w.sourceobjectid = it.sourceobjectid

		delete w
			from @itemsWeNeedIntersectsTO w
			inner join @intersectToItemTable it
			on w.sourceobject = it.targetobject and w.sourceobjectid = it.targetobjectid

		
		-- load intersects between above objects and the source object
		
		-- load the intersect type info for the intersects to all items in the diagram
		insert into @intersectToItemNotInDiagramTable
			select
				inter.intersecttypeid as IntersectTypeID,
				inode1.objecttype as SourceObject,
				inode1.objectid as SourceObjectID,
				inode1.intersecttypenodeid as SourceIntersectTypeNodeID,			
				inode2.objecttype as TargetObject,
				inode2.objectid as TargetObjectID,
				inode2.intersecttypenodeid as TargetIntersectTypeNodeID		
			from 
				intersectnode inode1
				inner join @itemsWeNeedIntersectsTO objs on(inode1.objectid = objs.sourceobjectid and inode1.objecttype = objs.sourceobject)
				inner join intersectnode inode2 on(inode1.intersectid = inode2.intersectid and inode2.objectid = @objectID and inode2.objecttype = @object)
				inner join [intersect] inter on (inter.id = inode2.intersectid)
			
		
		-- execute the query which will give us the objects we need to copy the above intersects to

		select @query = 'INSERT INTO #itemsToCopyToTable ' + @query;
		
		exec sp_executesql @query;
		
		declare @currentItemToUpdate int = 1,
				@maxItemToUpdate int = 0;

		select @currentItemToUpdate = min(ID) from #itemsToCopyToTable; -- table variable cleared but cant be truncated
		select @maxItemToUpdate = max(ID) from #itemsToCopyToTable;
		
		select @NumberOfObjectsConsidered = @NumberOfObjectsConsidered + @maxItemToUpdate;
		
		-- loop through the items we are going to clone too
		while (@currentItemToUpdate <= @maxItemToUpdate)
		begin			
			declare @currentObjectID int,
					@currentObjectType varchar(50);

			delete from @intersectToItemTempTable;
			delete from @intersectToItemNotInDiagramTempTable;

			select @currentObjectID = ObjectID,
					@currentObjectType = [Object]
				from #itemsToCopyToTable where id = @currentItemToUpdate;

			-- for each item in the query we need to 
			-- replace object/objectid with current object and insert new relations in
			insert into @intersectToItemTempTable select *,1 from @intersectToItemTable;
				
			if exists (select 1 from @intersectToItemTempTable)
			begin
				-- udpate any items in diagram to have right ids
				update @intersectToItemTempTable set sourceobjectid = @currentObjectID, sourceobject = @currentObjectType where sourceobjectid = @objectid and sourceobject = @object;
				update @intersectToItemTempTable set targetobjectid = @currentObjectID, targetobject = @currentObjectType where targetobjectid = @objectid and targetobject = @object;
			end

			if exists (select 1 from @intersectToItemNotInDiagramTable)
			begin
				insert into @intersectToItemNotInDiagramTempTable select * from @intersectToItemNotInDiagramTable;

				-- update any items in referenced in diagram to have right ids
				update @intersectToItemNotInDiagramTempTable set targetobjectid = @currentObjectID, targetobject = @currentObjectType where targetobjectid = @objectid and targetobject = @object;
				
				--add relations that dont need map records
				insert into @intersectToItemTempTable
					select
						IntersectTypeID,
						-1,
						-1,
						SourceObject,
						SourceObjectID,
						SourceIntersectTypeNodeID,
						TargetObject,
						TargetObjectID,
						TargetIntersectTypeNodeID,
						-1,
						-1,
						0
					from @intersectToItemNotInDiagramTempTable
				
				--debug print out what we are gonna add
				--select * from @intersectToItemTempTable
			end
						
			-- delete relations that already exist for the item from what we are about to insert												
			delete w
				from @intersectToItemTempTable w
				inner join intersectnode inode1 on(w.sourceobject = inode1.objecttype and w.sourceobjectid = inode1.objectid)
				inner join intersectnode inode2 on(inode1.intersectid = inode2.intersectid and inode2.objectid = @currentObjectID and inode2.objecttype = @currentObjectType)
							
			-- call proce to add the relations for this item
			exec [utility].[AddRelationDiagramRelations] @intersectToItemTempTable, @NumberOfIntersectsAdded, @NumberOfObjectsUpdated
					
			--next item
			select @currentItemToUpdate = @currentItemToUpdate +1;
		end	-- end of this target item

		select @currentTemplateID = @currentTemplateID +1;

	end -- end of templates loop

	-- log finish
	update [dbo].[IntersectMapTemplateLogSummary]
	set DateCompleted = CURRENT_TIMESTAMP, 
		[NumberOfTemplatesProcessed] = @maxTemplateID, 
		[NumberOfObjectsUpdated] = @NumberOfObjectsUpdated,
		[NumberOfObjectsConsidered] = @NumberOfObjectsConsidered,
		[NumberOfIntersectsAdded] = @NumberOfIntersectsAdded	
	where ID = @ExecutionID;
END
GO

ALTER PROCEDURE [utility].[PromoteFusionAttributesRelations]
	 @numberNewRelations int = 0 output
AS
BEGIN
	SET NOCOUNT ON;


	IF OBJECT_ID('tempdb..#relations') IS NOT NULL
		DROP TABLE #relations;

	create table #relations (
		ID int identity,
		StartFusionAttributeID int,
		StartPromotedObjectType varchar(25),
		StartPromotedObjectID int,
		StartIntersectTypeNodeID int,
		StartPromotedObjectTypeID int,
		EndFusionAttributeID int,
		EndPromotedObjectType varchar(25),
		EndPromotedObjectID int,
		EndIntersectTypeNodeID int,
		EndPromotedObjectTypeID int,
		IntersectTypeID int,
		IntersectID int
	);

	--insert existing relations between promoted items into temp table
	insert into #relations
		select
			fap.fusionattributeid as StartFusionAttributeID,
			fap.ObjectType as StartPromotedObjectType,
			fap.ObjectID as StartPromotedObjectID,
			-1 as StartIntersectTypeNodeID,
			fap.ObjectTypeID as StartPromotedObjectTypeID,
			fap2.fusionattributeid as EndFusionAttributeID,
			fap2.ObjectType as EndPromotedObjectType,
			fap2.ObjectID as EndPromotedObjectID,		
			-1 as EndIntersectTypeNodeID,
			fap2.ObjectTypeID as EndPromotedObjectTypeID,
			-1 as IntersectTypeID,
			inter.id as IntersectID
		from 
			dbo.fusionattributepromotion fap 
			inner join intersectnode inod on (inod.objectid = fap.fusionattributeid and inod.objecttype = 'FusionAttribute' and fap.objecttype != 'Intersect')	
			inner join intersectnode inod2 on (inod2.intersectid = inod.intersectid and inod2.objectid != inod.objectid and inod2.objecttype = 'FusionAttribute')
			inner join dbo.fusionattributepromotion fap2 on (inod2.objectid = fap2.fusionattributeid and fap2.objecttype != 'Intersect')
			inner join dbo.[intersect] inter on ( inter.id = inod2.intersectid)	
		where not exists
			( select 1 from dbo.fusionattributepromotion fapEx
				inner join intersectnode inodEx on (fapEx.ObjectID = inodEx.IntersectID and fapEx.ObjectType = 'Intersect' and inodEx.ObjectID = fap.ObjectID and inodEx.ObjectType = fap.ObjectType)
				inner join intersectnode inodEx2 on (inodEx.intersectID = inodEx2.intersectID and inodEx2.ObjectID = fap2.ObjectID and inodEx2.ObjectType = fap2.ObjectType)			
			)
			and fap.ObjectID != fap2.ObjectID;

	-- delete any objects we cant figure out the objecttypeid of 
	delete from #relations where EndPromotedObjectTypeID < 0 or StartPromotedObjectTypeID < 0;

	-- there will be two relations for each intersect on with either field starting .  Take just one.
	delete from #relations where ID in (
						select 
							a.ID 
						from 
							#relations a 
							inner join( select distinct intersectid, min(id) as id from #relations group by intersectid ) as b on (a.id = b.id) ) ;

	-- delete any duplicated relations if there are any
	delete from #relations where ID in (
						select 
							a.ID 
						from 
							#relations a 
							inner join( select
												StartFusionAttributeID,
												StartPromotedObjectType,
												StartPromotedObjectID,
												StartIntersectTypeNodeID,
												EndFusionAttributeID,
												EndPromotedObjectType,
												EndPromotedObjectID,
												EndIntersectTypeNodeID,
												IntersectTypeID, min(id) as id from #relations group by StartFusionAttributeID, StartPromotedObjectType, StartPromotedObjectID, StartIntersectTypeNodeID, EndFusionAttributeID, EndPromotedObjectType, EndPromotedObjectID, EndIntersectTypeNodeID, IntersectTypeID  having count(1) > 1) as b on (a.id = b.id) ) ;

	--load the intersect info for the promoted types
	
	update R
	set
		R.StartIntersectTypeNodeID = RelTypes.SourceIntersectTypeNodeID, 
		R.EndIntersectTypeNodeID = RelTypes.TargetIntersectTypeNodeID,
		R.IntersectTypeID = RelTypes.IntersectTypeID
	from #relations as R
	inner join utility.RelationshipTypes RelTypes on (RelTypes.SourceObjectType = R.StartPromotedObjectType + 'Type' and RelTypes.TargetObjectType = R.EndPromotedObjectType + 'Type' and RelTypes.SourceObjectID = R.StartPromotedObjectTypeID and RelTypes.TargetObjectID = R.EndPromotedObjectTypeID)
		
	
	-- create an relations that we still have -1 start / end type node ids
	declare @unresolvedrelations RelationshipTypeTable;

	insert into @unresolvedrelations select distinct startpromotedobjecttype, startpromotedobjecttypeid, endpromotedobjecttype, endpromotedobjecttypeid from #relations;
	
	-- create any new relations as needed
	exec [dbo].[AddRelationshipTypesBulk] @unresolvedrelations
	
	-- rerun query to set the start end id's etc for newly created
	update R
	set
		R.StartIntersectTypeNodeID = RelTypes.SourceIntersectTypeNodeID, 
		R.EndIntersectTypeNodeID = RelTypes.TargetIntersectTypeNodeID,
		R.IntersectTypeID = RelTypes.IntersectTypeID
	from #relations as R
	inner join utility.RelationshipTypes RelTypes on (RelTypes.SourceObjectType = R.StartPromotedObjectType + 'Type' and RelTypes.TargetObjectType = R.EndPromotedObjectType + 'Type' and RelTypes.SourceObjectID = R.StartPromotedObjectTypeID and RelTypes.TargetObjectID = R.EndPromotedObjectTypeID)
		

	select @numberNewRelations = count(1) from #relations
	

	-- add new relations for promoted items
	If EXISTS (SELECT 1 FROM #relations)		
	begin
		Declare @IDList Table(IntersectID int,RelID Int);
		--insert intersect records and save there id's
		-- trick is to use merge to keep the sequence id and staging row ids
		-- http://stackoverflow.com/questions/15614261/using-output-clause-to-insert-value-not-in-inserted
		MERGE
					INTO    [Intersect] d
					USING   (
								select rel.IntersectTypeID as isectid, 2 as class, rel.ID as srID from #relations rel						
							) s
					ON      (1 = 0)
					WHEN NOT MATCHED THEN
					INSERT  (IntersectTypeID, Classification, Description)
					VALUES  (isectid, class, NULL)
					OUTPUT  INSERTED.ID, s.srID into @IDList;

		--insert start records into intersect node
		INSERT INTO IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
				select sr.StartIntersectTypeNodeID, il.IntersectID, sr.StartPromotedObjectType,sr.StartPromotedObjectID from #relations sr inner join @IDList il on (sr.ID = il.RelID);
					
		--insert end records into intersect node
		INSERT INTO IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
				select sr.EndIntersectTypeNodeID, il.IntersectID, sr.EndPromotedObjectType,sr.EndPromotedObjectID from #relations sr inner join @IDList il on (sr.ID = il.RelID);
				

		declare @Intersects IDTable;
		insert into @Intersects select idl.intersectid from @IDList idl;
			
		declare @IntersectCount int
		select @IntersectCount = count(1) from @Intersects
		if @IntersectCount > 0 
		begin
			EXEC cache.SynchronizeRelationships @Intersects
		end
	
		-- log the relations into the fusionattributepromotion table so  they dont get readded and we know we added them

		--start fusion id
		insert into dbo.fusionattributepromotion select r.StartFusionAttributeID as FusionAttributeID, 'Intersect', il.IntersectID,null,0,-1  from #relations r inner join @IDLIst il on (r.ID = il.RelID)
		-- end fusion id
		insert into dbo.fusionattributepromotion select r.EndFusionAttributeID as FusionAttributeID, 'Intersect', il.IntersectID,null,0,-1  from #relations r inner join @IDLIst il on (r.ID = il.RelID)
	end


	IF OBJECT_ID('tempdb..#relations') IS NOT NULL
		DROP TABLE #relations;
	
END
GO

ALTER FUNCTION [utility].[GetHierarchyAssignedResponsibilityList]
(
--declare
	@Object varchar(50),
	@ObjectID int,
	@Priority int
--set @Object = 'Taxonomy'
--set @ObjectID = 524--226
--set @Priority = 4;
)
RETURNS 
@tbl TABLE 
(
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
AS
BEGIN
	declare @tblModelHierarchy table (
		Visible bit,
		ResponsibilityID int,
		ResponsibilityTypeID int,
		AssigningItem varchar(50),
		AssigningItemID int,
		[Object] varchar(50),
		ObjectID int,
		ContextHash varchar(50),
		[Level] int
	);

	if @Object = 'Artifact'
		begin
			with ModelRelationHierarchy as
			(
			select	R.Visible,
					'Taxonomy' as AssigningItemType, 
					T.ID as AssigningItemID,
					T.ID,
					T.ParentID,
					T.TaxonomyTypeID,
					R.ID as ResponsibilityID,
					R.ResponsibilityTypeID,
					utility.GetResponsibilityContextHash(R.ID) as ContextHash,
					1 as [Level]
			from	Taxonomy T 
					left join Responsibility R on R.ObjectType = 'Taxonomy' and R.ObjectID = T.ID 
			union all
			select	
					COALESCE(R.Visible, P.Visible) as Visible,
					P.AssigningItemType,
					COALESCE(R.ObjectID, P.AssigningItemID) as AssigningItemID,
					C.ID,
					C.ParentID,
					C.TaxonomyTypeID,
					COALESCE(R.ID, P.ResponsibilityID) as ResponsibilityID,
					COALESCE(R.ResponsibilityTypeID, P.ResponsibilityTypeID) as ResponsibilityTypeID,
					coalesce(R.ContextHash, P.ContextHash) as ContextHash,
					P.[Level] + 1 as [Level]
			from	Taxonomy C
					inner join ModelRelationHierarchy P on P.TaxonomyTypeID = C.TaxonomyTypeID and C.ParentID = P.ID
					outer apply (
								select	*,
										utility.GetResponsibilityContextHash(ID) as ContextHash
								from	Responsibility 
								where	ResponsibilityTypeID = P.ResponsibilityTypeID
										and ObjectType = 'Taxonomy' 
										and ObjectID = C.ID
								) R
			)

			insert into @tblModelHierarchy
				select		P.Visible,
							P.ResponsibilityID,
							P.ResponsibilityTypeID,
							P.AssigningItemType,
							P.AssigningItemID,
							R.TargetObject, 
							R.TargetObjectID,
							P.ContextHash,
							P.[Level]
				from		ModelRelationHierarchy P
							inner join cache.Relationship R on 
								R.SourceObject = 'Taxonomy' and R.SourceObjectID = P.ID 
								and R.TargetObject = 'Artifact'
							inner join Artifact A on A.ID = R.TargetObjectID and A.TaxonomyTypeID = P.TaxonomyTypeID
								and (
									(A.ID = @ObjectID and @ObjectID is not null) or (@ObjectID is null)
									)
							inner join ResponsibilityTypeRelation RTR on RTR.ResponsibilityTypeID = P.ResponsibilityTypeID and RTR.ObjectType = 'ArtifactType' and RTR.ObjectID = A.ArtifactTypeID
				where		P.ResponsibilityID is not null;


			insert into @tbl
				select	'Hierarchy Assigned' as [Source],
						O.Visible,
						O.ResponsibilityID,
						O.ResponsibilityTypeID,
						O.AssigningItem,
						O.AssigningItemID,
						O.[Object],
						O.ObjectID,
						O.ContextHash,
						2 as [Priority]
				from	@tblModelHierarchy O
						inner join	(
									select		ResponsibilityTypeID,
												[Object],
												ObjectID,
												ContextHash,
												Max([Level]) as [Level]
									from		@tblModelHierarchy
									group by	ResponsibilityTypeID,
												[Object],
												ObjectID,
												ContextHash
									) M on M.ResponsibilityTypeID = O.ResponsibilityTypeID and M.[Object] = O.[Object] and M.ObjectID = O.ObjectID and M.ContextHash = O.ContextHash and M.[Level] = O.[Level];
		end

	if @Object = 'Policy'
		begin
			with PolicyHierarchy as
			(
			select	R.Visible,
					P.ID as AssigningItemID,
					P.ID,
					P.ParentID,
					R.ID as ResponsibilityID,
					R.ResponsibilityTypeID,
					utility.GetResponsibilityContextHash(R.ID) as ContextHash,
					1 as [Level]
			from	Policy P 
					left join Responsibility R on R.ObjectType = 'Policy' and R.ObjectID = P.ID
			where	P.ParentID is null
			union all
			select	
					COALESCE(R.Visible, P.Visible) as Visible,
					COALESCE(R.ObjectID, P.AssigningItemID) as AssigningItemID,
					C.ID,
					C.ParentID,
					COALESCE(R.ID, P.ResponsibilityID) as ResponsibilityID,
					COALESCE(R.ResponsibilityTypeID, P.ResponsibilityTypeID) as ResponsibilityTypeID,
					coalesce(R.ContextHash, P.ContextHash) as ContextHash,
					P.[Level] + 1 as [Level] 
			from	Policy C
					inner join PolicyHierarchy P on C.ParentID = P.ID
					outer apply (
								select	*,
										utility.GetResponsibilityContextHash(ID) as ContextHash
								from	Responsibility 
								where	ResponsibilityTypeID = P.ResponsibilityTypeID
										and ObjectType = 'Policy' 
										and ObjectID = C.ID
								) R
			)

			insert into @tblModelHierarchy
				select	Visible,
						ResponsibilityID,
						ResponsibilityTypeID,
						'Policy' as AssigningItemType,
						AssigningItemID,
						'Policy' as TargetObject, 
						ID,
						ContextHash,
						[Level]
				from	PolicyHierarchy
				where	ResponsibilityID is not null;

			insert into @tbl
				select	'Hierarchy Assigned' as [Source],
						O.Visible,
						O.ResponsibilityID,
						O.ResponsibilityTypeID,
						O.AssigningItem,
						O.AssigningItemID,
						O.[Object],
						O.ObjectID,
						O.ContextHash,
						1 as [Priority]
				from	@tblModelHierarchy O
						inner join	(
									select		ResponsibilityTypeID,
												[Object],
												ObjectID,
												ContextHash,
												Max([Level]) as [Level]
									from		@tblModelHierarchy
									group by	ResponsibilityTypeID,
												[Object],
												ObjectID,
												ContextHash
									) M on M.ResponsibilityTypeID = O.ResponsibilityTypeID and M.[Object] = O.[Object] and M.ObjectID = O.ObjectID and M.ContextHash = O.ContextHash and M.[Level] = O.[Level]
											and (
												(O.ObjectID = @ObjectID and @ObjectID is not null) or (@ObjectID is null)
												)
		end
	if @Object = 'Taxonomy'
		begin
			declare @IDs table (TaxonomyID int, ParentID int, ResponsibilityID int);
			
			with C as
			(
			select	T.ID,
					T.ParentID
			from	Taxonomy T 
			where	T.ID = @ObjectID
			union all
			select	P.ID,
					P.ParentID
			from	C
					inner join Taxonomy P on P.ID = C.ParentID
			)
			insert into @IDs
				select	@ObjectID,
						C.ID,
						R.ID 
				from	C
						inner join Responsibility R on R.ObjectType = 'Taxonomy' and R.ObjectID = C.ID;

			with ModelHierarchy as
			(
			select	R.Visible,
					Q.ParentID as AssigningItemID,
					T.ID,
					T.ParentID,
					T.TaxonomyTypeID,
					R.ID as ResponsibilityID,
					R.ResponsibilityTypeID,
					utility.GetResponsibilityContextHash(R.ID) as ContextHash,
					1 as [Level]
			from	Taxonomy T 
					inner join Responsibility R on R.ObjectType = 'Taxonomy' --and R.ObjectID = T.ID
					inner join @IDs Q on Q.TaxonomyID = T.ID and Q.ResponsibilityID = R.ID
			--where	T.ID = @ObjectID
			union all
			select	COALESCE(R.Visible, P.Visible) as Visible,
					COALESCE(R.ObjectID, P.AssigningItemID) as AssigningItemID,
					C.ID,
					C.ParentID,
					C.TaxonomyTypeID,
					COALESCE(R.ID, P.ResponsibilityID) as ResponsibilityID,
					COALESCE(R.ResponsibilityTypeID, P.ResponsibilityTypeID) as ResponsibilityTypeID,
					coalesce(R.ContextHash, P.ContextHash) as ContextHash,
					P.[Level] + 1 as [Level]
			from	Taxonomy C
					inner join ModelHierarchy P on P.TaxonomyTypeID = C.TaxonomyTypeID and C.ParentID = P.ID
					outer apply (
								select	*,
										utility.GetResponsibilityContextHash(ID) as ContextHash
								from	Responsibility 
								where	ResponsibilityTypeID = P.ResponsibilityTypeID
										and ObjectType = 'Taxonomy' 
										and ObjectID = C.ID
								) R
			)

			insert into @tblModelHierarchy
				select	Visible,
						ResponsibilityID,
						ResponsibilityTypeID,
						'Taxonomy' as AssigningItemType,
						AssigningItemID,
						'Taxonomy' as TargetObject, 
						ID,
						ContextHash,
						[Level]
				from	ModelHierarchy;

			-- Load for taxonomies.
			insert into @tbl
				select	'Hierarchy Assigned' as [Source],
						O.Visible,
						O.ResponsibilityID,
						O.ResponsibilityTypeID,
						O.AssigningItem,
						O.AssigningItemID,
						O.[Object],
						O.ObjectID,
						O.ContextHash,
						@Priority as [Priority]
				from	@tblModelHierarchy O
						inner join	(
									select		ResponsibilityTypeID,
												[Object],
												ObjectID,
												ContextHash,
												Max([Level]) as [Level]
									from		@tblModelHierarchy
									group by	ResponsibilityTypeID,
												[Object],
												ObjectID,
												ContextHash
									) M on M.ResponsibilityTypeID = O.ResponsibilityTypeID and M.[Object] = O.[Object] and M.ObjectID = O.ObjectID and M.ContextHash = O.ContextHash and M.[Level] = O.[Level]
											and (
												(O.ObjectID = @ObjectID and @ObjectID is not null) or (@ObjectID is null)
												);
			-- Load for artifacts.
			insert into @tbl
				select	'Hierarchy Assigned' as [Source],
						O.Visible,
						O.ResponsibilityID,
						O.ResponsibilityTypeID,
						O.AssigningItem,
						O.AssigningItemID,
						'Artifact',
						A.ID,
						O.ContextHash,
						@Priority
				from	@tblModelHierarchy O
						inner join Taxonomy T on T.ID = O.ObjectID
						inner join [Intersect] R on 
								(R.Subject = O.[Object] and R.SubjectID = O.ObjectID and R.Object = 'Artifact') OR
								(R.Object = O.[Object] and R.ObjectID = O.ObjectID and R.Subject = 'Artifact')
						inner join Artifact A on A.ID = case 
															when R.Object = 'Artifact' then R.ObjectID
															else R.SubjectID
														end 
												and A.TaxonomyTypeID = T.TaxonomyTypeID
		end
	RETURN 
END
GO

ALTER FUNCTION [utility].[GetVerticalResponsibilityList]
(
	@Object varchar(50),
	@ObjectID int,
	@Priority int
)
RETURNS 
@tbl TABLE 
(
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
AS
BEGIN

	if @Object = 'ArtifactType' OR @Object = 'Artifact'
		begin
			insert into @tbl
				select	'Artifact Vertical' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						'ArtifactType' as AssigningItemType,
						T.ID as AssigningItemID,
						'Artifact' as ObjectType,
						A.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	ArtifactType T 
						inner join Responsibility R on R.ObjectType = 'ArtifactType' and R.ObjectID = T.ID
						inner join Artifact A on A.ArtifactTypeID = T.ID 
													and (
															(
																(
																(@Object = 'ArtifactType' and A.ArtifactTypeID = @ObjectID) OR 
																(@Object = 'Artifact' and A.ID = @ObjectID)
																)
																and @ObjectID is not null 
															)
															OR @ObjectID is null 
														);

			insert into @tbl
				select	'Taxonomy Vertical' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						'TaxonomyType' as AssigningItemType,
						T.ID as AssigningItemID,
						'Artifact' as ObjectType,
						A.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority+1 as [Priority]
				from	TaxonomyType T 
						inner join Responsibility R on R.ObjectType = 'TaxonomyType' and R.ObjectID = T.ID
						inner join Artifact A on A.TaxonomyTypeID = T.ID
												  and	(
															(
																(
																(@Object = 'ArtifactType' and A.ArtifactTypeID = @ObjectID) OR 
																(@Object = 'Artifact' and A.ID = @ObjectID)
																)
																and @ObjectID is not null 
															)
															OR @ObjectID is null 
														)
						inner join ResponsibilityTypeRelation RTR on RTR.ResponsibilityTypeID = R.ResponsibilityTypeID and RTR.ObjectType = 'ArtifactType' and RTR.ObjectID = A.ArtifactTypeID;
		end
	if @Object = 'DomainType' OR @Object = 'Domain'
		begin
			insert into @tbl
				select	'Domain Vertical' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						'DomainType' as AssigningItemType,
						T.ID as AssigningItemID,
						'Domain' as ObjectType,
						A.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	DomainType T 
						inner join Responsibility R on R.ObjectType = 'DomainType' and R.ObjectID = T.ID
						inner join Domain A on A.DomainTypeID = T.ID 
												and (
														(
															(
															(@Object = 'DomainType' and T.ID = @ObjectID) 
															OR (@Object = 'Domain' and A.ID = @ObjectID) 
															)
															and @ObjectID is not null
														)
														or (@ObjectID is null)
													);
		end
	if @Object = 'FusionType' OR @Object = 'Fusion'
		begin
			insert into @tbl
				select	'Fusion Vertical' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						'FusionType' as AssigningItemType,
						T.ID as AssigningItemID,
						'Fusion' as ObjectType,
						A.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	FusionType T 
						inner join Responsibility R on R.ObjectType = 'FusionType' and R.ObjectID = T.ID
						inner join Fusion A on A.FusionTypeID = T.ID 
												and (
														(
															(
															(@Object = 'FusionType' and T.ID = @ObjectID) 
															OR (@Object = 'Fusion' and A.ID = @ObjectID) 
															)
															and @ObjectID is not null
														)
														or (@ObjectID is null)
													);																		 
		end
	if @Object = 'TaxonomyType' OR @Object = 'Taxonomy'
		begin
			insert into @tbl
				select	'Taxonomy Vertical' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						'TaxonomyType' as AssigningItemType,
						T.ID as AssigningItemID,
						'Taxonomy' as ObjectType,
						A.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	TaxonomyType T 
						inner join Responsibility R on R.ObjectType = 'TaxonomyType' and R.ObjectID = T.ID
						inner join Taxonomy A on A.TaxonomyTypeID = T.ID 
												and (
														(
															(
															(@Object = 'TaxonomyType' and T.ID = @ObjectID) 
															OR (@Object = 'Taxonomy' and A.ID = @ObjectID)
															)
															and @ObjectID is not null
														)
														or (@ObjectID is null)
													);


			if @Object = 'TaxonomyType'
			begin
				insert into @tbl
					select	'Taxonomy Vertical' as [Source],
								R.Visible,
								R.ID,
								R.ResponsibilityTypeID,
								'TaxonomyType' as AssigningItemType,
								T.ID as AssigningItemID,
								'Artifact' as ObjectType,
								A.ID as ObjectID,
								utility.GetResponsibilityContextHash(R.ID),
								@Priority as [Priority]
						from	TaxonomyType T 
								inner join Responsibility R on R.ObjectType = 'TaxonomyType' and R.ObjectID = T.ID
								inner join Artifact A on A.TaxonomyTypeID = T.ID and (@Object = 'TaxonomyType' and A.TaxonomyTypeID = @ObjectID)
								inner join ResponsibilityTypeRelation RTR on RTR.ResponsibilityTypeID = R.ResponsibilityTypeID and RTR.ObjectType = 'ArtifactType' and RTR.ObjectID = A.ArtifactTypeID;
					
			end
		end
	RETURN 
END
GO

ALTER  FUNCTION [utility].[GetObjectLevel]
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

CREATE TABLE [dbo].[IntersectGroup] (
    [ID]          INT      IDENTITY (1, 1) NOT NULL,
    [IntersectID] INT      NOT NULL,
    [GroupNumber] INT      NOT NULL,
    [CreatedBy]   INT      NOT NULL,
    [CreatedOn]   DATETIME NOT NULL,
    [UpdatedBy]   INT      NOT NULL,
    [UpdatedOn]   DATETIME NOT NULL,
    CONSTRAINT [PK_IntersectGroup] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_IntersectGroup_Intersect] FOREIGN KEY ([IntersectID]) REFERENCES [dbo].[Intersect] ([ID]) ON DELETE CASCADE
);
GO

CREATE TABLE [dbo].[IntersectRole] (
    [ID]          INT             IDENTITY (1, 1) NOT NULL,
    [Name]        NVARCHAR (250)  NOT NULL,
    [Description] NVARCHAR (4000) NULL,
    [CreatedBy]   INT             NOT NULL,
    [CreatedOn]   DATETIME        NOT NULL,
    [UpdatedBy]   INT             NOT NULL,
    [UpdatedOn]   DATETIME        NOT NULL,
    CONSTRAINT [PK_IntersectRole] PRIMARY KEY NONCLUSTERED ([ID] ASC)
);
GO

CREATE TABLE [dbo].[Map] (
    [ID]              INT             IDENTITY (1, 1) NOT NULL,
    [Name]            NVARCHAR (250)  NOT NULL,
    [IntersectRoleID] INT             NULL,
    [Transformation]  NVARCHAR (4000) NULL,
    [CreatedBy]       INT             CONSTRAINT [DF_Map_CreatedBy] DEFAULT ((0)) NOT NULL,
    [CreatedOn]       DATETIME        CONSTRAINT [DF_Map_CreatedOn] DEFAULT (getutcdate()) NOT NULL,
    [UpdatedBy]       INT             CONSTRAINT [DF_Map_UpdatedBy] DEFAULT ((0)) NOT NULL,
    [UpdatedOn]       DATETIME        CONSTRAINT [DF_Map_UpdatedOn] DEFAULT (getutcdate()) NOT NULL,
    CONSTRAINT [PK_Map] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Map_IntersectRole] FOREIGN KEY ([IntersectRoleID]) REFERENCES [dbo].[IntersectRole] ([ID])
);
GO

CREATE TABLE [dbo].[MapItem] (
    [ID]          INT          IDENTITY (1, 1) NOT NULL,
    [MapID]       INT          NOT NULL,
    [IntersectID] INT          NOT NULL,
    [IsSource]    BIT          NOT NULL,
    [CreatedBy]   INT          NOT NULL,
    [CreatedOn]   DATETIME     NOT NULL,
    [UpdatedBy]   INT          NOT NULL,
    [UpdatedOn]   DATETIME     NOT NULL,
    [DiagramKey]  VARCHAR (25) NULL,
    CONSTRAINT [PK_MapItem] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_MapItem_Map] FOREIGN KEY ([MapID]) REFERENCES [dbo].[Map] ([ID]) ON DELETE CASCADE
);
GO

CREATE TABLE [dbo].[MapRule] (
    [ID]             INT             IDENTITY (1, 1) NOT NULL,
    [Name]           NVARCHAR (250)  NULL,
    [Transformation] NVARCHAR (4000) NULL,
    [CreatedBy]      INT             NOT NULL,
    [CreatedOn]      DATETIME        NOT NULL,
    [UpdatedBy]      INT             NOT NULL,
    [UpdatedOn]      DATETIME        NOT NULL,
    CONSTRAINT [PK_MapRule] PRIMARY KEY NONCLUSTERED ([ID] ASC)
);
GO

CREATE TABLE [dbo].[MapRuleItem] (
    [ID]                INT      IDENTITY (1, 1) NOT NULL,
    [MapRuleID]         INT      NOT NULL,
    [FusionAttributeID] INT      NOT NULL,
    [IsSource]          BIT      NOT NULL,
    [CreatedBy]         INT      NOT NULL,
    [CreatedOn]         DATETIME NOT NULL,
    [UpdatedBy]         INT      NOT NULL,
    [UpdatedOn]         DATETIME NOT NULL,
    CONSTRAINT [PK_MapRuleItem] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_MapRuleItem_MapRule] FOREIGN KEY ([MapRuleID]) REFERENCES [dbo].[MapRule] ([ID]) ON DELETE CASCADE
);
GO

CREATE TABLE [dbo].[MapRuleMap] (
    [MapRuleID] INT NOT NULL,
    [MapID]     INT NOT NULL,
    CONSTRAINT [PK_MapRuleMap] PRIMARY KEY CLUSTERED ([MapID] ASC, [MapRuleID] ASC),
    CONSTRAINT [FK_MapRuleMap_Map] FOREIGN KEY ([MapID]) REFERENCES [dbo].[Map] ([ID]),
    CONSTRAINT [FK_MapRuleMap_MapRule] FOREIGN KEY ([MapRuleID]) REFERENCES [dbo].[MapRule] ([ID])
);
GO

CREATE TABLE [dbo].[MapSequence] (
    [ID]          INT             IDENTITY (1, 1) NOT NULL,
    [MapID]       INT             NOT NULL,
    [Sequence]    INT             NOT NULL,
    [Description] NVARCHAR (4000) NULL,
    [CreatedBy]   INT             NOT NULL,
    [CreatedOn]   DATETIME        NOT NULL,
    [UpdatedBy]   INT             NOT NULL,
    [UpdatedOn]   DATETIME        NOT NULL,
    CONSTRAINT [PK_MapSequence] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_MapSequence_Map] FOREIGN KEY ([MapID]) REFERENCES [dbo].[Map] ([ID]) ON DELETE CASCADE
);
GO

CREATE TABLE [dbo].[MapSequenceContext] (
    [MapSequenceID] INT          NOT NULL,
    [Object]        VARCHAR (50) NOT NULL,
    [ObjectID]      INT          NOT NULL,
    CONSTRAINT [PK_MapSequenceContext] PRIMARY KEY NONCLUSTERED ([MapSequenceID] ASC, [Object] ASC, [ObjectID] ASC),
    CONSTRAINT [FK_MapSequenceContext_MapSequence] FOREIGN KEY ([MapSequenceID]) REFERENCES [dbo].[MapSequence] ([ID]) ON DELETE CASCADE
);
GO

CREATE TABLE [fusion].[Rule] (
    [ID]          INT            IDENTITY (1, 1) NOT NULL,
    [Description] NVARCHAR (500) NULL,
    [Enabled]     BIT            NOT NULL,
    [FusionID]    INT            NOT NULL,
    [ObjectType]  VARCHAR (25)   NOT NULL,
    [ObjectID]    INT            NOT NULL,
    [UpdatedOn]   DATETIME       NOT NULL,
    [UpdatedBy]   INT            NOT NULL,
    PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FusionRule_Fusion] FOREIGN KEY ([FusionID]) REFERENCES [dbo].[Fusion] ([ID])
);
GO

CREATE TABLE [fusion].[RuleItem] (
    [ID]                INT IDENTITY (1, 1) NOT NULL,
    [RuleID]            INT NOT NULL,
    [FusionAttributeID] INT NULL,
    CONSTRAINT [PK_RuleItem] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FusionRuleItem_FusionRule] FOREIGN KEY ([RuleID]) REFERENCES [fusion].[Rule] ([ID]) ON DELETE CASCADE
);
GO

CREATE TABLE [fusion].[RulePromotion] (
    [ID]                INT          IDENTITY (1, 1) NOT NULL,
    [FusionAttributeID] INT          NOT NULL,
    [ObjectType]        VARCHAR (25) NOT NULL,
    [ObjectID]          INT          NOT NULL,
    [RuleID]            INT          NOT NULL,
    [RuleStepID]        INT          NULL,
    [ObjectTypeID]      INT          DEFAULT ((-1)) NOT NULL,
    CONSTRAINT [PK_FusionRulePromotion] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FusionRulePromotion_FusionAttribute] FOREIGN KEY ([FusionAttributeID]) REFERENCES [dbo].[FusionAttribute] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_FusionRulePromotion_FusionRule] FOREIGN KEY ([RuleID]) REFERENCES [fusion].[Rule] ([ID]) ON DELETE CASCADE
);
GO

CREATE TABLE [fusion].[RuleStep] (
    [ID]          INT             IDENTITY (1, 1) NOT NULL,
    [RuleID]      INT             NOT NULL,
    [Step]        INT             NOT NULL,
    [Action]      VARCHAR (25)    NOT NULL,
    [Description] NVARCHAR (4000) NULL,
    PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FusionRuleStep_FusionRule] FOREIGN KEY ([RuleID]) REFERENCES [fusion].[Rule] ([ID]) ON DELETE CASCADE
);
GO

CREATE TABLE [fusion].[RuleStepMapping] (
    [ID]                INT            IDENTITY (1, 1) NOT NULL,
    [RuleStepID]        INT            NOT NULL,
    [SourceFieldName]   NVARCHAR (250) NULL,
    [SourceFieldTypeID] INT            NOT NULL,
    [TargetFieldName]   NVARCHAR (250) NULL,
    [TargetFieldTypeID] INT            NOT NULL,
    [IsConstantValue]   BIT            DEFAULT ((0)) NOT NULL,
    [ConstantValue]     NVARCHAR (250) NULL,
    CONSTRAINT [PK_FusionRuleStepMapping] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FusionRuleStepMapping_FusionRuleStep] FOREIGN KEY ([RuleStepID]) REFERENCES [fusion].[RuleStep] ([ID]) ON DELETE CASCADE
);
GO

CREATE TABLE [fusion].[RuleStepSetting] (
    [RuleStepID] INT            NOT NULL,
    [Name]       NVARCHAR (100) NOT NULL,
    [Value]      NVARCHAR (250) NULL,
    CONSTRAINT [PK_FusionRuleStepSetting] PRIMARY KEY CLUSTERED ([RuleStepID] ASC, [Name] ASC),
    CONSTRAINT [FK_FusionRuleStepSetting_FusionRuleStep] FOREIGN KEY ([RuleStepID]) REFERENCES [fusion].[RuleStep] ([ID]) ON DELETE CASCADE
);
GO

CREATE view [dbo].[IntersectDetail]
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

CREATE view [dbo].[IntersectTypeDetail]
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
			left join dbo.DomainType SDT with(nolock)			on IT.Subject = 'DomainType'			and SDT.ID = IT.SubjectID
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
			left join dbo.DomainType ODT with(nolock)			on IT.Object = 'DomainType'				and ODT.ID = IT.ObjectID
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
	where	coalesce(SAT.ID, SDT.ID, SFT.ID, SPT.ID, SRT.ID, STT.ID) is not null
			and coalesce(OAT.ID, ODT.ID, [OFT].ID, OPT.ID, ORT.ID, OTT.ID) is not null
GO

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

CREATE procedure [dbo].[GetLineage]
--declare 
	@type varchar(50),
	@id int

--set @type = 'Artifact'
--set @id = 4651;
as
begin
	declare @items table (
		IntersectID int, IntersectTypeID int,
		SubjectTypeName nvarchar(500), SubjectName nvarchar(500), Subject varchar(50), SubjectID int, SubjectIconBackColor varchar(7), SubjectIconForeColor varchar(7), 
		ObjectTypeName nvarchar(500), ObjectName nvarchar(500), Object varchar(50), ObjectID int, 
		MapID int, MapItemID int, Transformation nvarchar(4000), IntersectRoleID int, IntersectRole nvarchar(250),
		IsSource bit, [DiagramKey] varchar(25)
	)
	
	-- get all items directly tied to the focal object.
	insert into @items
		select	MI.IntersectID,
				I.IntersectTypeID,
				
				I.SubjectTypeName,
				I.SubjectName,
				I.Subject,
				I.SubjectID,
				I.SubjectIconBackColor,
				I.SubjectIconForeColor,

				I.ObjectTypeName,
				I.ObjectName,
				I.Object,
				I.ObjectID,
				
				MI.MapID,
				MI.ID as MapItemID,
				M.Transformation,
				M.IntersectRoleID,
				IR.Name,
				MI.IsSource,
				MI.[DiagramKey]
		from	[IntersectDetail] I
				inner join MapItem MI on MI.IntersectID = I.ID 
											and ( 
												(I.Subject = @type and I.SubjectID = @id) 
												OR (I.Object = @type and I.ObjectID = @id) 
												)
				inner join Map M on M.ID = MI.MapID
				left join IntersectRole IR on IR.ID = M.IntersectRoleID

	-- get all items not directly tied to the focal object, but still tied to maps involved above.
	insert into @items
		select	MI.IntersectID,
				I.IntersectTypeID,
				
				I.SubjectTypeName,
				I.SubjectName,
				I.Subject,
				I.SubjectID,
				I.SubjectIconBackColor,
				I.SubjectIconForeColor,

				I.ObjectTypeName,
				I.ObjectName,
				I.Object,
				I.ObjectID,

				MI.MapID,
				MI.ID as MapItemID,
				M.Transformation,
				M.IntersectRoleID,
				IR.Name,
				MI.IsSource,
				MI.[DiagramKey]
		from	[IntersectDetail] I
				inner join MapItem MI on MI.IntersectID = I.ID
				inner join Map M on M.ID = MI.MapID
				left join IntersectRole IR on IR.ID = M.IntersectRoleID
				inner join @items IT on IT.MapID = MI.MapID and MI.ID not in (select MapItemID from @items)
	
	select	(
			select	S.MapID as id,
					S.DiagramKey as 'from',
					S.IntersectID as 'fromIntersectId',
					T.DiagramKey as 'to',
					T.IntersectID as 'toIntersectId',
					S.IntersectRole as 'role', 
					S.intersectRoleId, 
					0 as mappingRuleCount, 
					S.Transformation as transformation,
					S.intersectTypeId
			from	@items S
					inner join @items T on T.MapID = S.MapID and T.MapItemID <> S.MapItemID and S.IsSource = 1 and T.IsSource = 0
			for json path			
			) as 'links',
			(
			select	I1.DiagramKey as [key],
					I1.Subject as [obj],
					I1.SubjectID as [objid], 
					I1.Subject as [type],
					I1.Subject as objecttype,
					I1.SubjectID as objecttypeid, 
					I1.SubjectTypeName as typeName,
					I1.SubjectName as name,
					I1.SubjectIconBackColor as back,
					I1.SubjectIconForeColor as fore,
					I1.IntersectID as intersectId,
					0 as sourceRuleCount,
					0 as mappingRuleCount,
					C.challengeCount,
					0 as openEventCount,
					I.openIssueCount,
					(
					select	MapID,
							MapItemID
					from	@items
					where	DiagramKey = I1.DiagramKey
					for json auto
					) as mapItems
			from	(
					select	distinct
							IntersectID,
							SubjectTypeName,
							SubjectName,
							Subject,
							SubjectID,
							SubjectIconBackColor,
							SubjectIconForeColor,
							ObjectName,
							Object,
							ObjectID,
							DiagramKey
					from	@items 
					) I1
					cross apply (
									select count(1) as challengeCount     
									from Workflow W            			                          
									where W.WorkflowType = 4 and W.Data.exist('/fields/ArtifactID[text() = sql:column("I1.SubjectID")]') = 1 and W.DateCompleted is null   
								) C
					cross apply (
									select count(1) as openIssueCount   
									from Workflow W            			                          
									where W.WorkflowType = 3 and W.Data.exist('/fields/ArtifactID[text() = sql:column("I1.SubjectID")]') = 1 and W.DateCompleted is null   
								) I
		for json path			
			) as 'nodes'
	for json path, WITHOUT_ARRAY_WRAPPER
end
GO

CREATE procedure [dbo].[GetTypeHierarchyByObject]
--declare 
	@type varchar(50),
	@id int
--set @type = 'Artifact'
--set @id = 16441--11808
as
begin
	declare @predicateType int = 3

	declare @rawResults table (
		ID int, 
		[Subject] varchar(50), SubjectID int, SubjectLevel int,
		[Object] varchar(50), ObjectID int, ObjectLevel int
	);

	declare @results table (
		--ID int, 
		[Object] varchar(50), 
		ObjectID int, 
		[Level] int,
		GroupNumber int
	);

	with u as		--Get parent hierarchy from current item.
	(
	select	I.ID,
			I.[Subject], 
			I.[SubjectID],
			1 as [SubjectLevel],
			I.[Object],
			I.[ObjectID],
			0 as [ObjectLevel]
	from	[Intersect] I
			inner join IntersectType IT on IT.ID = I.IntersectTypeID and I.Object = @type and I.ObjectID = @id
			inner join [Predicate] P on P.ID = IT.PredicateID and P.Type = @predicateType

	union all

	select	I.ID,
			I.[Subject], 
			I.[SubjectID],
			u.[SubjectLevel] + 1 as [SubjectLevel],
			I.[Object],
			I.[ObjectID], 
			u.[ObjectLevel] + 1 as [ObjectLevel]
	from	[Intersect] I
			inner join IntersectType IT on IT.ID = I.IntersectTypeID 
			inner join [Predicate] P on P.ID = IT.PredicateID and P.[Type] = @predicateType
			inner join u on u.[Subject] = I.Object and u.[SubjectID] = I.ObjectID
	),
	d as		--Get child hierarchy from current item.
	(
	select	I.ID,
			I.[Subject], 
			I.[SubjectID],
			0 as [SubjectLevel],
			I.[Object],
			I.[ObjectID],
			-1 as [ObjectLevel]
	from	[Intersect] I
			inner join IntersectType IT on IT.ID = I.IntersectTypeID and I.Subject = @type and I.SubjectID = @id
			inner join [Predicate] P on P.ID = IT.PredicateID and P.Type = @predicateType

	union all

	select	I.ID,
			I.[Subject], 
			I.[SubjectID],
			d.SubjectLevel - 1 as [SubjectLevel],
			I.[Object],
			I.[ObjectID], 
			d.[ObjectLevel] - 1 as [ObjectLevel]
	from	[Intersect] I
			inner join IntersectType IT on IT.ID = I.IntersectTypeID 
			inner join [Predicate] P on P.ID = IT.PredicateID and P.[Type] = @predicateType
			inner join d on d.[Object] = I.Subject and d.[ObjectID] = I.SubjectID
	)

	insert into @rawResults
		select * from u
		union
		select * from d;



	insert into @results
		select	distinct
				--ID, 
				Subject, 
				SubjectID, 
				SubjectLevel,
				NULL
		from	@rawResults

	insert into @results
		select	distinct
				--ID, 
				Object, 
				ObjectID, 
				ObjectLevel,
				NULL
		from	@rawResults
		where	cast(ObjectLevel as varchar) + [Object] + cast(ObjectID as varchar) 
					not in (
							select	cast([Level] as varchar) + [Object] + cast(ObjectID as varchar)
							from @results
							)

	select		R.Object,
				R.ObjectID,
				D.ObjectType,
				D.ObjectTypeID,
				D.Name,
				D.Url,
				D.ObjectTypeName,
				R.[Level],
				R.GroupNumber
	from		@results R
				inner join cache.ObjectDetails D on D.Object = R.Object and D.ObjectID = R.ObjectID
	order by	R.[Level] desc
end
GO

CREATE PROCEDURE [fusion].[Rules]
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

	--EXEC @promotionNeedsToRun = [utility].[ShouldPromotionRun]

	--if(@promotionNeedsToRun <= 0)
	--BEGIN
	--	PRINT 'NO REASON TO RUN THE PROMOTION RULES WAS DETECTED';
	--	return;
	--END;


	--Log this run get a new id from the fusion.promotion table
	--insert into [dbo].[FusionAttributePromotionLogSummary] ( DateStarted ) values ( CURRENT_TIMESTAMP)
	--select @ExecutionID =  SCOPE_IDENTITY()

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
					when M.SourceFieldName = 'Name' then FA.Name					
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
						select	@ParentObject = RelationshipOwnerObjectType,
								@ParentObjectID = RelationshipOwnerObjectID
						from	FusionAttributeOwnerRule
						where	@ParentSearchObject = 'Owner'
								and FusionID = @FusionID
								and ID = @ParentSearchObjectID
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

						declare @modelTypeID int
						select @modelTypeID = min(ID) from TaxonomyType

						if @ResultObjectID is null
						begin
							if @ParentObjectID = 0
							begin
								set @ParentObjectID = null
							end

							insert into Artifact ( ParentID, ArtifactTypeID, TaxonomyTypeID, Name, Description, Status, UpdatedOn, UpdatedBy )
							values ( @ParentObjectID, @ObjectTypeIDToPromoteTo, @modelTypeID, @name, @description, 'Draft', getutcdate(), 0 )

							select @ResultObjectID =  SCOPE_IDENTITY()
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
							where	ID = @ResultObjectID

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
					--END: IF ArtifactType

					if @ObjectTypeToPromoteTo = 'DomainType'
					begin
						if @ParentObject is null and @ParentObjectID is null
							begin
								set @ResultObject = 'Domain'
									
								-- You are promoting to a Domain (creating a list)
								if @ResultObjectID is null
									begin
										select	@ResultObjectID = ID
										from	Domain
										where	DomainTypeID = @ObjectTypeIDToPromoteTo
												and lower(Name) = lower(@name)
									end
 
								if @ResultObjectID is null
									begin
										insert into Domain  ( DomainTypeID, Name, Description ) 
										values ( @ObjectTypeIDToPromoteTo, @name, @description )

										select @ResultObjectID =  SCOPE_IDENTITY()

										set @NumberOfNewDomains = @NumberOfNewDomains +1;
									end
								else
									begin
										update	Domain
										set		Name = @name,
												Description = @description
										where	ID = @ResultObjectID
									end
							end
						else
							begin
								-- You are promoting domain items to a specific domain (list)
								set @ResultObject = 'DomainItem'

								if @ResultObject is null and @ResultObjectID is null
									begin
										select	@ResultObjectID = ID
										from	DomainItem
										where	DomainID = @ParentObjectID
												and lower(Code) = lower(@code)
									end
 
								if @ResultObjectID is not null
									begin
										update	DomainItem
										set		Name = @name,
												Code = coalesce(@code, @name),
												Description = @description
										where	ID = @ResultObjectID
									end
								else
									begin
										insert into DomainItem ( DomainID, Name, Code, Description )
										values ( @ParentObject, @name, coalesce(@code, @name), @description )

										select @ResultObjectID =  SCOPE_IDENTITY()

										set @NumberOfNewDomainItems = @NumberOfNewDomainItems +1;
									end
							end
					end
					--END: IF DomainType

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
						@FindParent int = null

				select	@FindSearchType			= Value from @settings where Name = 'ObjectSearch'
				select	@FindSearchObject		= Value from @settings where Name = 'Object'
				select	@FindSearchObjectID		= Value from @settings where Name = 'ObjectID'
				select	@FindFilterField		= Value from @settings where Name = 'FilterField'
				select	@FindTargetField		= Value from @settings where Name = 'TargetField'
				select	@FindParent		= Value from @settings where Name = 'FindParent'
																
				if @FindSearchType = 'Fusion'
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
						select	top 1
								@ResultObject = 'FusionAttribute',
								@ResultObjectID = ID
						from	FusionAttribute
						where	@FindSearchObject = 'FusionAttributeType'
								and FusionAttributeTypeID = @FindSearchObjectID
								and (TextPath = @FindFilterFieldValue or Name = @FindFilterFieldValue)
					end

				end

				--BEGIN: Find based on search type
				if @FindSearchType = 'FusionOwner'
				begin
					select	@ResultObject = RelationshipOwnerObjectType,
							@ResultObjectID = RelationshipOwnerObjectID
					from	FusionAttributeOwnerRule
					where	@FindSearchObject = 'Owner'
							and FusionID = @FusionID
							and ID = @FindSearchObjectID
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

--select @ResultObjectID
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
				--END: Find based on search type
			end --END: Find Action
			
			--BEGIN: Lineage Action
			if @Action = 'Lineage'
			begin
				declare @IntersectTypeID int = null,
						@SubjectSearchType nvarchar(250) = null,
						@SubjectSearchObject varchar(50) = null,
						@SubjectSearchObjectID int = null,
						@Subject varchar(50) = null,
						@SubjectID int = null,
						@ObjectSearchType nvarchar(250) = null,
						@ObjectSearchObject varchar(50) = null,
						@ObjectSearchObjectID int = null,
						@Object varchar(50) = null,
						@ObjectID int = null,
						@FocalSearchType nvarchar(250) = null,
						@FocalSearchObject varchar(50) = null,
						@FocalSearchObjectID int = null,
						@Focal varchar(50) = null,
						@FocalID int = null,
						@PredicateID int = null,
						@IntersectID int = null

				select	@IntersectTypeID			= Value from @settings where Name = 'IntersectType'
				select	@SubjectSearchType			= Value from @settings where Name = 'SubjectSearch'
				select	@SubjectSearchObject		= Value from @settings where Name = 'Subject'
				select	@SubjectSearchObjectID		= Value from @settings where Name = 'SubjectID'
				select	@ObjectSearchType			= Value from @settings where Name = 'ObjectSearch'
				select	@ObjectSearchObject			= Value from @settings where Name = 'Object'
				select	@ObjectSearchObjectID		= Value from @settings where Name = 'ObjectID'
				select	@FocalSearchType			= Value from @settings where Name = 'FocalSearch'
				select	@FocalSearchObject			= Value from @settings where Name = 'Focal'
				select	@FocalSearchObjectID		= Value from @settings where Name = 'FocalID'
				select	@PredicateID				= Value from @settings where Name = 'Predicate'
				
				--BEGIN: Find subject based on search type
				if @SubjectSearchType = 'Direct'
				begin
					set @Subject = @SubjectSearchObject
					set @SubjectID = @SubjectSearchObjectID
				end

				if @SubjectSearchType = 'FusionOwner'
				begin
					select	@Subject = RelationshipOwnerObjectType,
							@SubjectID = RelationshipOwnerObjectID
					from	FusionAttributeOwnerRule
					where	@SubjectSearchObject = 'Owner'
							and FusionID = @FusionID
							and ID = @SubjectSearchObjectID
				end

				if @SubjectSearchType = 'ResultFromStep'
				begin
					select	@Subject = ObjectType,
							@SubjectID = ObjectID
					from	[Fusion].[RulePromotion]
					where	@SubjectSearchObject = 'Step'
							and RuleID = @RuleID
							and RuleStepID = @SubjectSearchObjectID
							and FusionAttributeID = @FusionAttributeID
				end

				if @SubjectSearchType = 'Self'
				begin
					set @Subject = 'FusionAttribute'
					set @SubjectID = @FusionAttributeID
				end
				--END: Find subject based on search type

				--BEGIN: Find object based on search type
				if @ObjectSearchType = 'Direct'
				begin
					set @Object = @ObjectSearchObject
					set @ObjectID = @ObjectSearchObjectID
				end

				if @ObjectSearchType = 'FusionOwner'
				begin
					select	@Object = RelationshipOwnerObjectType,
							@ObjectID = RelationshipOwnerObjectID
					from	FusionAttributeOwnerRule
					where	@ObjectSearchObject = 'Owner'
							and FusionID = @FusionID
							and ID = @ObjectSearchObjectID
				end

				if @ObjectSearchType = 'ResultFromStep'
				begin
					select	@Object = ObjectType,
							@ObjectID = ObjectID
					from	[fusion].[RulePromotion]
					where	@ObjectSearchObject = 'Step'
							and RuleID = @RuleID
							and RuleStepID = @ObjectSearchObjectID
							and FusionAttributeID = @FusionAttributeID
				end

				if @ObjectSearchType = 'Self'
				begin
					set @Object = 'FusionAttribute'
					set @ObjectID = @FusionAttributeID
				end
				--END: Find object based on search type

				--BEGIN: Find focal based on search type
				if @FocalSearchType = 'Direct'
				begin
					set @Focal = @FocalSearchObject
					set @FocalID = @FocalSearchObjectID
				end

				if @FocalSearchType = 'FusionOwner'
				begin
					select	@Focal = RelationshipOwnerObjectType,
							@FocalID = RelationshipOwnerObjectID
					from	FusionAttributeOwnerRule
					where	@FocalSearchObject = 'Owner'
							and FusionID = @FusionID
							and ID = @FocalSearchObjectID
				end

				if @FocalSearchType = 'ResultFromStep'
				begin
					select	@Focal = ObjectType,
							@FocalID = ObjectID
					from	[fusion].[RulePromotion]
					where	@FocalSearchObject = 'Step'
							and RuleID = @RuleID
							and RuleStepID = @FocalSearchObjectID
							and FusionAttributeID = @FusionAttributeID
				end

				if @FocalSearchType = 'Self'
				begin
					set @Focal = 'FusionAttribute'
					set @FocalID = @FusionAttributeID
				end
				--END: Find focal based on search type

				declare @SubjectType varchar(50) = null,
						@SubjectTypeID int = null,
						@SubjectIntersectNodeID int = null,
						@SubjectIntersectTypeNodeID int = null,

						@ObjectType varchar(50) = null,
						@ObjectTypeID int = null,
						@ObjectIntersectNodeID int = null,
						@ObjectIntersectTypeNodeID int = null,

						@PredicateType int = null

				--BEGIN: Relate Subject to Object
				--Check to see if we have all the required data to create the relationship.
				if @IntersectTypeID is not null and @subject is not null and @SubjectID is not null and @Object is not null and @ObjectID is not null
				begin					
					-- Validate that intersect type exists.
					if exists(select 1 from IntersectType where ID = @IntersectTypeID)
					begin
--select @Subject, @SubjectID, @Object, @ObjectID
						select	@IntersectID = isect.ID,
								@SubjectIntersectNodeID = inode2.ID,
								@ObjectIntersectNodeID = inode1.ID
						from	[Intersect] isect
								inner join [intersectnode] inode1 on(isect.id = inode1.intersectid and inode1.objecttype = isect.object and inode1.objectid = isect.objectid)
								inner join [intersectnode] inode2 on(isect.id = inode2.intersectid and inode2.objecttype = isect.subject and inode2.objectid = isect.subjectid)
						where	Subject = @Subject 
								and isect.SubjectID = @SubjectID 
								and isect.Object = @Object 
								and isect.ObjectID = @ObjectID
								and isect.IntersectTypeID = @IntersectTypeID							
--select @IntersectID
						if @IntersectID is null
						begin
							select	@SubjectType = ObjectType, @SubjectTypeID = ObjectTypeID from cache.[object] where Object = @Subject and ObjectID = @SubjectID
							select	@ObjectType = ObjectType, @ObjectTypeID = ObjectTypeID from cache.[object] where Object = @Object and ObjectID = @ObjectID

							select	@SubjectIntersectTypeNodeID = SourceIntersectTypeNodeID, 
									@ObjectIntersectTypeNodeID = TargetIntersectTypeNodeID
							from	utility.RelationshipTypes R 
							where	SourceObjectType = @SubjectType and SourceObjectID = @SubjectTypeID 
									and TargetObjectType = @ObjectType and TargetObjectID = @ObjectTypeID
									and IntersectTypeID = @IntersectTypeID

							if @SubjectIntersectTypeNodeID is not null and @ObjectIntersectTypeNodeID is not null
							begin
								begin try


									insert into [Intersect] (IntersectTypeID, Classification, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
									values					(@IntersectTypeID, 2, @Subject, @SubjectID, @Object, @ObjectID, 0, @r, @d, @r, @d)  

									select @IntersectID = SCOPE_IDENTITY()

									insert into IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
									values						(@SubjectIntersectTypeNodeID, @IntersectID, @Subject, @SubjectID)

									select @SubjectIntersectNodeID = SCOPE_IDENTITY()

									insert into IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
									values						(@ObjectIntersectTypeNodeID, @IntersectID, @Object, @ObjectID)

									select @ObjectIntersectNodeID = SCOPE_IDENTITY()

									--cache logic
									insert into cache.[Object] ( [Object], [ObjectID], [ObjectType], [ObjectTypeID] ) values	( 'Intersect', @IntersectID, 'IntersectType', @IntersectTypeID );
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

									exec utility.AddAuditEntry @Subject, @SubjectID, @r, @d, 'Created', 'Intersect', @IntersectID
									exec utility.AddAuditEntry @Object, @ObjectID, @r, @d, 'Created', 'Intersect', @IntersectID
																											
									set @ResultObjectID = @IntersectID
								end try
								begin catch
									select ERROR_MESSAGE()
								end catch

							end
						end
					end
				end
				--END: Relate Subject to Object

				--BEGIN: Add IntersectMap
				if @SubjectIntersectNodeID is not null and @ObjectIntersectNodeID is not null
				begin					
					select @PredicateType = Type from Predicate where ID = @PredicateID
					if @PredicateType is not null
					begin
						declare @intersectMap table (ID int)
						MERGE	IntersectMap AS T
						USING	(
								SELECT	@SubjectIntersectNodeID as SubjectIntersectNodeID, 
										@ObjectIntersectNodeID as ObjectIntersectNodeID, 
										@PredicateID as PredicateID, 
										@PredicateType as Type
								) as S
						ON		T.SubjectIntersectNodeID = S.SubjectIntersectNodeID
								and T.ObjectIntersectNodeID = S.ObjectIntersectNodeID 
								and T.PredicateID = S.PredicateID 
						WHEN	MATCHED THEN
								UPDATE SET	T.Type = S.Type
						WHEN	NOT MATCHED THEN
								INSERT (SubjectIntersectNodeID, ObjectIntersectNodeID, PredicateID, Type) 
								VALUES (S.SubjectIntersectNodeID, S.ObjectIntersectNodeID, S.PredicateID, S.Type)
						OUTPUT inserted.ID into @intersectMap;
					
						set @ResultObject = 'IntersectMap'
						select top 1 @ResultObjectID = ID from @intersectMap
						delete from @intersectMap				
					end
				end
				--END: Add IntersectMap


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
					select	@R_Subject = RelationshipOwnerObjectType,
							@R_SubjectID = RelationshipOwnerObjectID
					from	FusionAttributeOwnerRule
					where	@R_SubjectSearchObject = 'Owner'
							and FusionID = @FusionID
							and ID = @R_SubjectSearchObjectID
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

--select @R_Subject, @R_SubjectID
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
					select	@R_Object = RelationshipOwnerObjectType,
							@R_ObjectID = RelationshipOwnerObjectID
					from	FusionAttributeOwnerRule
					where	@R_ObjectSearchObject = 'Owner'
							and FusionID = @FusionID
							and ID = @R_ObjectSearchObjectID
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
				if @R_IntersectTypeID is not null and @R_subject is not null and @R_SubjectID is not null and @R_Object is not null and @R_ObjectID is not null
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

							select	@R_SubjectIntersectTypeNodeID = SourceIntersectTypeNodeID, 
									@R_ObjectIntersectTypeNodeID = TargetIntersectTypeNodeID
							from	utility.RelationshipTypes R 
							where	SourceObjectType = @R_SubjectType and SourceObjectID = @R_SubjectTypeID 
									and TargetObjectType = @R_ObjectType and TargetObjectID = @R_ObjectTypeID
									and IntersectTypeID = @R_IntersectTypeID


							if @R_SubjectIntersectTypeNodeID is not null and @R_ObjectIntersectTypeNodeID is not null
							begin
								begin try
									declare @R_SubjectIntersectNodeID int = null,
											@R_ObjectIntersectNodeID int = null

									insert into [Intersect] (IntersectTypeID, Classification, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
									values					(@R_IntersectTypeID, 2, @R_Subject, @R_SubjectID, @R_Object, @R_ObjectID, 0, @r, @d, @r, @d)  

									select @R_IntersectID = SCOPE_IDENTITY()

									insert into IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
									values						(@R_SubjectIntersectTypeNodeID, @R_IntersectID, @R_Subject, @R_SubjectID)

									select @R_SubjectIntersectNodeID = SCOPE_IDENTITY()

									insert into IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
									values						(@R_ObjectIntersectTypeNodeID, @R_IntersectID, @R_Object, @R_ObjectID)

									select @R_ObjectIntersectNodeID = SCOPE_IDENTITY()

									--cache logic
									insert into cache.[Object] ( [Object], [ObjectID], [ObjectType], [ObjectTypeID] ) values	( 'Intersect', @R_IntersectID, 'IntersectType', @R_IntersectTypeID );
									insert into cache.Relationship ( IntersectID, SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID )
									values	( @R_IntersectID, @R_SubjectIntersectTypeNodeID, @R_SubjectIntersectNodeID, @R_Subject, @R_SubjectID, @R_ObjectIntersectTypeNodeID, @R_ObjectIntersectNodeID, @R_Object, @R_ObjectID );
									insert into cache.Relationship ( IntersectID, SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID )
									values	( @R_IntersectID, @R_ObjectIntersectTypeNodeID, @R_ObjectIntersectNodeID, @R_Object, @R_ObjectID, @R_SubjectIntersectTypeNodeID, @R_SubjectIntersectNodeID, @R_Subject, @R_SubjectID );

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
	--update [dbo].[FusionAttributePromotionLogSummary]
	--set	DateCompleted = CURRENT_TIMESTAMP, 
	--	[PromotedTaxonomies] = @NumberOfNewTaxonomies, 
	--	[PromotedDomainItems] = @NumberOfNewDomainItems,  
	--	[PromotedDomains] = @NumberOfNewDomains,
	--	[PromotedArtifacts] = @NumberOfNewArtifacts,
	--	[TotalNewPromotions] = (@NumberOfNewTaxonomies + @NumberOfNewDomainItems + @NumberOfNewDomains + @NumberOfNewArtifacts),
	--	[AttributesConsidered]= @NumberOfAttributesTotal,
	--	[NumberOfRules] = @NumberOfRules ,
	--	[RelationshipsAdded] = @NumberOfNewRelations
	--where ID = @ExecutionID;
END
go

CREATE FUNCTION GetFusionAttributesByOwningArtifact
(
	@ArtifactID int
)
RETURNS 
@tbl TABLE 
(
	ID int
)
AS
BEGIN
		declare @h table (ID int);

		with h as	(
					select	ID,
							ParentID
					from	Artifact
					where	ID = @ArtifactID
					union all
					select	P.ID,
							P.ParentID
					from	Artifact P
							inner join h as C on C.ParentID = P.ID
					)
		insert into @h
			select ID from h;
	
		--with fa as	(
		--			select	A.ID,
		--					A.ParentID
		--			from	FusionAttributeOwnerRule R
		--					inner join FusionAttributeOwnerRuleItem RI on RI.FusionAttributeOwnerRuleID = R.ID and R.RelationshipOwnerObjectType = 'Artifact'
		--					inner join @h H on H.ID = R.RelationshipOwnerObjectID
		--					inner join FusionAttribute A on (
		--													(RI.FusionAttributeID is not null and A.ID = RI.FusionAttributeID) OR 
		--													(RI.FusionAttributeID is null and A.FusionAttributeTypeID = R.ObjectID)
		--													)
		--													AND A.FusionID = R.FusionID
		--			union all
		--			select	C.ID,
		--					C.ParentID
		--			from	FusionAttribute C
		--					inner join fa P on C.ParentID = P.ID
		--			)

		with f as	(
					select	R.FusionID
					from	FusionAttributeOwnerRule R
							inner join @h H on H.ID = R.RelationshipOwnerObjectID and R.RelationshipOwnerObjectType = 'Artifact'
					)

		--INSERT INTO @tbl
		--	SELECT	ID
		--	FROM	fa

		INSERT INTO @tbl
			SELECT	distinct
					FusionID
			FROM	f
	
	RETURN 
END
GO
