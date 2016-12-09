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


