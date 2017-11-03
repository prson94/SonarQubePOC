CREATE view [dbo].[IntersectDetail]
as
	select	I.ID,
			I.IntersectTypeID,

			I.Subject,
			I.SubjectID,
			case I.Subject
				when 'Intersect' then utility.DeriveIntersectName(SI.ID)
				when 'Resource' then SRE.FirstName + ' ' + SRE.LastName
				else coalesce(SA.DisplayValue, SD.Name, SF.TextPath, SQF.DisplayValue, SG.Name, 'Map', SP.TextPath, SR.DisplayValue, SRI.DisplayValue, ST.TextPath) 
			end as SubjectName,
			case I.Subject
				when 'Intersect' then utility.DeriveIntersectName(SI.ID)
				when 'Resource' then SRE.FirstName + ' ' + SRE.LastName
				else coalesce(SA.DisplayValue, SD.Name, SF.Name, SQF.DisplayValue, SG.Name, 'Map', SP.DisplayValue, SR.DisplayValue, SRI.DisplayValue, ST.DisplayValue) 
			end as SubjectShortName,
			dbo.GenerateNgObjectUrl(
				I.Subject, 
				case I.Subject
					when 'Resource' then 1
					when 'Group' then 1
					when 'ReferenceItemType' then 0
					else coalesce(SA.ArtifactTypeID, SF.FusionAttributeTypeID, SQF.FusionQueryAttributeTypeID, SI.IntersectTypeID, SM.MapTypeID, SP.PolicyTypeID, SR.RuleTypeID, SRI.ReferenceItemTypeID, ST.TaxonomyTypeID) 
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
				else coalesce(SA.ArtifactTypeID, SF.FusionAttributeTypeID, SQF.FusionQueryAttributeTypeID, SI.IntersectTypeID, SM.MapTypeID, SP.PolicyTypeID, SR.RuleTypeID, SRI.ReferenceItemTypeID, ST.TaxonomyTypeID) 
			end as SubjectTypeID,
			case 
				when I.Subject = 'ReferenceItemType' then 'Reference List'
				when I.Subject = 'Intersect' then utility.DeriveIntersectTypeName(SI.IntersectTypeID)
				else coalesce(SAT.Name, SFT.TextPath, SMT.Name, SPT.Name, SRT.Name, SRIT.Name, STT.Name) 
			end as SubjectTypeName,
			coalesce(SIcon.IconBackColor, '#000') as SubjectIconBackColor,
			coalesce(SIcon.IconForeColor, '#fff') as SubjectIconForeColor,
			coalesce(SIcon.IconText, substring(coalesce(SAT.Name, SD.Name, SFT.TextPath, SMT.Name, SPT.Name, SRT.Name, SRIT.Name, STT.Name, ''), 1, 2)) as SubjectIconText,

			I.Object,
			I.ObjectID,
			case I.Object
				when 'Intersect' then utility.DeriveIntersectName(OI.ID)
				when 'Resource' then ORE.FirstName + ' ' + ORE.LastName
				else coalesce(OA.DisplayValue, OD.Name, [OF].TextPath, OQF.DisplayValue, OG.Name, 'Map', OP.TextPath, [OR].DisplayValue, ORI.DisplayValue, OT.TextPath)
			end as ObjectName,
			case I.Object
				when 'Intersect' then utility.DeriveIntersectName(OI.ID)
				when 'Resource' then ORE.FirstName + ' ' + ORE.LastName
				else coalesce(OA.DisplayValue, OD.Name, [OF].Name, OQF.DisplayValue, OG.Name, 'Map', OP.DisplayValue, [OR].DisplayValue, ORI.DisplayValue, OT.DisplayValue)
			end as ObjectShortName,
			dbo.GenerateNgObjectUrl(
				I.Object, 
				case I.Object
					when 'Resource' then 1
					when 'Group' then 1
					when 'ReferenceItemType' then 0
					else coalesce(OA.ArtifactTypeID, OD.ID, [OF].FusionAttributeTypeID, [OQF].FusionQueryAttributeTypeID, OI.IntersectTypeID, OM.MapTypeID, OP.PolicyTypeID, [OR].RuleTypeID, ORI.ReferenceItemTypeID, OT.TaxonomyTypeID)
				end,
				I.ObjectID) as ObjectUrl,
			case I.Object
				when 'Artifact' then 'ArtifactType'
				when 'FusionAttribute' then 'FusionAttributeType'
				when 'FusionQueryAttribute' then 'FusionQueryAttributeType'
				when 'Intersect' then 'IntersectType'
				when 'Map' then 'MapType'
				when 'Policy' then 'PolicyType'
				when 'Rule' then 'RuleType'
				when 'Taxonomy' then 'TaxonomyType'
				else I.Object
			end as ObjectType,
			case I.Object
				when 'Resource' then 1
				when 'Group' then 1
				when 'ReferenceItemType' then 0
				else coalesce(OA.ArtifactTypeID, OD.ID, [OF].FusionAttributeTypeID, OQF.FusionQueryAttributeTypeID, OI.IntersectTypeID, OM.MapTypeID, OP.PolicyTypeID, [OR].RuleTypeID, ORI.ReferenceItemTypeID, OT.TaxonomyTypeID)
			end as ObjectTypeID,
			case
				when I.Object = 'ReferenceItemType' then 'Reference List'
				when I.Object = 'Intersect' then utility.DeriveIntersectTypeName(OI.IntersectTypeID)
				else coalesce(OAT.Name, OD.Name, OFT.TextPath, OMT.Name, OPT.Name, ORT.Name, ORIT.Name, OTT.Name) 
			end as ObjectTypeName,
			coalesce(OIcon.IconBackColor, '#000') as ObjectIconBackColor,
			coalesce(OIcon.IconForeColor, '#fff') as ObjectIconForeColor,
			--coalesce(OIcon.IconText, 'leaf') as ObjectIconText,
			coalesce(OIcon.IconText, substring(coalesce(OAT.Name, OD.Name, OFT.TextPath, OMT.Name, OPT.Name, ORT.Name, ORIT.Name, OTT.Name, ''), 1, 2)) as ObjectIconText,

			IT.PredicateID,
			P.Name as [PredicateName],
			P.Type as PredicateType
	from	dbo.[Intersect] I with(nolock)
			inner join dbo.[IntersectType] IT with(nolock) on IT.ID = I.IntersectTypeID and I.[Visible] = 1
			left join [Predicate] P with(nolock) on P.ID = IT.PredicateID 
			left join dbo.Artifact SA with(nolock) on I.Subject = 'Artifact' and SA.ID = I.SubjectID
			left join dbo.ArtifactType SAT with(nolock) on SAT.ID = SA.ArtifactTypeID
			left join dbo.ReferenceItemType SD with(nolock) on I.Subject = 'ReferenceItemType' and SD.ID = I.SubjectID
			left join dbo.FusionAttribute SF with(nolock) on I.Subject = 'FusionAttribute' and SF.ID = I.SubjectID
			left join dbo.FusionQueryAttribute [SQF] with(nolock) on I.Object = 'FusionQueryAttribute' and [SQF].ID = I.SubjectID
			left join dbo.FusionAttributeType SFT with(nolock) on SFT.ID = SF.FusionAttributeTypeID
			left join dbo.[Group] SG with(nolock) on I.Subject = 'Group' and SG.ID = I.SubjectID
			left join dbo.[Intersect] SI with(nolock) on I.Subject = 'Intersect' and SI.ID = I.SubjectID
			--left join dbo.[IntersectType] SIT with(nolock) on SIT.ID = SI.IntersectTypeID
			left join dbo.Map SM with(nolock) on I.Subject = 'Map' and SM.ID = I.SubjectID
			left join dbo.MapType SMT with(nolock) on SMT.ID = SM.MapTypeID
			left join dbo.[Policy] SP with(nolock) on I.Subject = 'Policy' and SP.ID = I.SubjectID
			left join dbo.PolicyType SPT with(nolock) on SPT.ID = SP.PolicyTypeID
			left join reporting.Global_Resource SRE with(nolock) on I.Subject = 'Resource' and SRE.ResourceID = I.SubjectID
			left join ReferenceItem SRI with(nolock) on I.Subject = 'ReferenceItem' and SRI.ID = I.SubjectID
			left join ReferenceItemType SRIT with(nolock) on SRIT.ID = SRI.ReferenceItemTypeID
			left join dbo.[Rule] SR with(nolock) on I.Subject = 'Rule' and SR.ID = I.SubjectID
			left join dbo.RuleType SRT with(nolock) on SRT.ID = [SR].RuleTypeID
			left join dbo.Taxonomy ST with(nolock) on I.Subject = 'Taxonomy' and ST.ID = I.SubjectID
			left join dbo.TaxonomyType STT with(nolock) on STT.ID = ST.TaxonomyTypeID

			left join dbo.Artifact OA with(nolock) on I.Object = 'Artifact' and OA.ID = I.ObjectID
			left join dbo.ArtifactType OAT with(nolock) on OAT.ID = OA.ArtifactTypeID
			left join dbo.ReferenceItemType OD with(nolock) on I.Object = 'ReferenceItemType' and OD.ID = I.ObjectID
			left join dbo.FusionAttribute [OF] with(nolock) on I.Object = 'FusionAttribute' and [OF].ID = I.ObjectID
			left join dbo.FusionQueryAttribute [OQF] with(nolock) on I.Object = 'FusionQueryAttribute' and [OQF].ID = I.ObjectID
			left join dbo.FusionAttributeType OFT with(nolock) on OFT.ID = [OF].FusionAttributeTypeID
			left join dbo.[Group] OG with(nolock) on I.Object = 'Group' and OG.ID = I.SubjectID
			left join dbo.[Intersect] OI with(nolock) on I.Subject = 'Intersect' and OI.ID = I.SubjectID
			--left join dbo.[IntersectType] OIT with(nolock) on OIT.ID = OI.IntersectTypeID
			left join dbo.Map OM with(nolock) on I.Object = 'Map' and OM.ID = I.ObjectID
			left join dbo.MapType OMT with(nolock) on OMT.ID = OM.MapTypeID
			left join dbo.[Policy] OP with(nolock) on I.Object = 'Policy' and OP.ID = I.ObjectID
			left join dbo.PolicyType OPT with(nolock) on OPT.ID = OP.PolicyTypeID
			left join reporting.Global_Resource ORE with(nolock) on I.Object = 'Resource' and ORE.ResourceID = I.ObjectID
			left join ReferenceItem ORI with(nolock) on I.Object = 'ReferenceItem' and ORI.ID = I.ObjectID
			left join ReferenceItemType ORIT with(nolock) on ORIT.ID = ORI.ReferenceItemTypeID
			left join dbo.[Rule] [OR] with(nolock) on I.Object = 'Rule' and [OR].ID = I.ObjectID
			left join dbo.RuleType ORT with(nolock) on ORT.ID = [OR].RuleTypeID
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
																					else coalesce(SA.ArtifactTypeID, SD.ID, SF.FusionAttributeTypeID, SQF.FusionQueryAttributeTypeID, SI.IntersectTypeID, SM.MapTypeID, SP.PolicyTypeID, SR.RuleTypeID, ST.TaxonomyTypeID) 
																				end
			left join ObjectStyle OIcon with(nolock) on OIcon.ObjectType =	case I.Object
																				when 'Group' then 'GroupType'
																				when 'Resource' then 'ResourceType'
																				else I.Object + 'Type'
																			end 
														and OIcon.ObjectID =	case I.Object
																					when 'Resource' then 1
																					when 'Group' then 1
																					else coalesce(OA.ArtifactTypeID, OD.ID, [OF].FusionAttributeTypeID, OQF.FusionQueryAttributeTypeID, OI.IntersectTypeID, OM.MapTypeID, OP.PolicyTypeID, [OR].RuleTypeID, OT.TaxonomyTypeID) 
																				end

	where	coalesce(SA.ID, SD.ID, SF.ID, SQF.ID, SG.ID, SI.ID, SM.ID, SP.ID, SR.ID, SRI.ID, SRE.ResourceID, ST.ID) is not null
			and coalesce(OA.ID, OD.ID, [OF].ID, OQF.ID, OG.ID, OI.ID, OM.ID, OP.ID, [OR].ID, ORI.ID, ORE.ResourceID, OT.ID) is not null

GO


